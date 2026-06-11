using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProfilerStudy.Tracy;

public static class Tracy010FileReader
{
	private const int MaxBlockSize = 1024 * 1024;
	private const int MaxDecodedBlockSize = 64 * 1024;
	private const int MaxBlockCount = 1_000_000;

	public static TracyFileHeader ReadHeader(string path)
	{
		return Read(path).Header;
	}

	public static TracyEventStream Read(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Tracy file path is required.", nameof(path));
		}

		using FileStream stream = File.OpenRead(path);
		byte[] compressionHeader = ReadExactly(stream, 4);
		string compression = Encoding.ASCII.GetString(compressionHeader);
		if (compression != "tlZ4")
		{
			if (compression == "tZst")
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Zstd-compressed Tracy files are not supported by the initial C# reader.");
			}
			throw new TracyFileFormatException("TracyFileFormatInvalid", "File is not a Tracy LZ4 dump.");
		}

		List<byte[]> decodedBlocks = new List<byte[]>();
		long compressedByteCount = 0L;
		long decodedByteCount = 0L;
		while (TryReadUInt32(stream, out uint blockSize))
		{
			if (blockSize == 0 || blockSize > MaxBlockSize)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Invalid Tracy block size.");
			}
			if (decodedBlocks.Count >= MaxBlockCount)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy file contains too many compressed blocks.");
			}
			byte[] compressedBlock = ReadExactly(stream, checked((int)blockSize));
			byte[] decodedBlock = TracyLz4BlockDecoder.Decode(compressedBlock, MaxDecodedBlockSize);
			decodedBlocks.Add(decodedBlock);
			compressedByteCount += blockSize;
			decodedByteCount += decodedBlock.Length;
		}
		if (decodedBlocks.Count == 0)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy dump does not contain any compressed blocks.");
		}

		byte[] firstBlock = decodedBlocks[0];
		if (firstBlock.Length < 8)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy dump does not contain a complete file header.");
		}
		if (firstBlock[0] != (byte)'t'
			|| firstBlock[1] != (byte)'r'
			|| firstBlock[2] != (byte)'a'
			|| firstBlock[3] != (byte)'c'
			|| firstBlock[4] != (byte)'y')
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy dump inner header is invalid.");
		}

		string version = firstBlock[5] + "." + firstBlock[6] + "." + firstBlock[7];
		if (version != TracyVersionRegistry.LockedVersion)
		{
			throw new TracyFileFormatException(
				"TracyUnsupportedFileVersion",
				"Unsupported Tracy file version " + version + ". Supported version is " + TracyVersionRegistry.LockedVersion + ".");
		}
		TracyFileHeader header = new TracyFileHeader(version, "lz4");
		long payloadByteCount = Math.Max(0, decodedByteCount - 8L);
		TracyTraceMetadata metadata = TryReadMetadata(decodedBlocks, payloadByteCount, out List<TracyCpuZoneSummary> cpuZones, out List<TracyPlotSummary> plots, out int threadCount, out ArrayList readerDiagnostics);
		ArrayList diagnostics = new ArrayList
		{
			new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyLz4StreamRead",
				["message"] = "Tracy LZ4 block stream was decoded for event parsing.",
				["compressedBlockCount"] = decodedBlocks.Count,
				["compressedByteCount"] = compressedByteCount,
				["decodedByteCount"] = decodedByteCount,
				["payloadByteCount"] = payloadByteCount,
				["metadataDecoded"] = metadata != null,
				["threadCount"] = threadCount,
				["cpuZoneCount"] = cpuZones.Count,
				["plotCount"] = plots.Count
			}
		};
		foreach (object diagnostic in readerDiagnostics)
		{
			diagnostics.Add(diagnostic);
		}
		return new TracyEventStream(header, decodedBlocks.Count, compressedByteCount, decodedByteCount, payloadByteCount, metadata, cpuZones, plots, threadCount, diagnostics);
	}

	private static TracyTraceMetadata TryReadMetadata(List<byte[]> decodedBlocks, long payloadByteCount, out List<TracyCpuZoneSummary> cpuZones, out List<TracyPlotSummary> plots, out int threadCount, out ArrayList readerDiagnostics)
	{
		cpuZones = new List<TracyCpuZoneSummary>();
		plots = new List<TracyPlotSummary>();
		threadCount = 0;
		readerDiagnostics = new ArrayList();
		if (payloadByteCount <= 0)
		{
			return null;
		}

		DecodedBlockReader reader = new DecodedBlockReader(decodedBlocks);
		reader.Skip(8);

		long delay = reader.ReadInt64();
		long resolution = reader.ReadInt64();
		double timerMultiplier = reader.ReadDouble();
		long lastTime = reader.ReadInt64();
		long frameOffset = reader.ReadInt64();
		ulong processId = reader.ReadUInt64();
		long samplingPeriod = reader.ReadInt64();
		byte cpuArchitecture = reader.ReadByte();
		uint cpuId = reader.ReadUInt32();
		string cpuManufacturer = reader.ReadFixedAscii(12);
		bool onDemand = reader.ReadByte() != 0;
		string captureName = reader.ReadSizedAsciiString(1024);
		string captureProgram = reader.ReadSizedAsciiString(1024);
		long captureTime = reader.ReadInt64();
		long executableTime = reader.ReadInt64();
		string hostInfo = reader.ReadSizedAsciiString(1024);

		SkipCpuTopology(reader);
		reader.Skip(28); // CrashEvent in Tracy 0.10.0.

		ulong frameSetCount = reader.ReadUInt64();
		if (frameSetCount > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many frame sets.");
		}
		List<TracyFrameSetSummary> frameSets = new List<TracyFrameSetSummary>();
		for (ulong i = 0; i < frameSetCount; i++)
		{
			ulong name = reader.ReadUInt64();
			bool continuous = reader.ReadByte() != 0;
			ulong frameCount = reader.ReadUInt64();
			if (frameCount > 10_000_000)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many frames.");
			}

			long refTime = 0;
			List<long> starts = new List<long>();
			List<long> ends = new List<long>();
			for (ulong frameIndex = 0; frameIndex < frameCount; frameIndex++)
			{
				long start = ReadTimeOffset(reader, ref refTime);
				long end = continuous ? -1 : ReadTimeOffset(reader, ref refTime);
				reader.Skip(4); // frameImage
				starts.Add(start);
				ends.Add(end);
			}
			List<TracyFrameSummary> frames = new List<TracyFrameSummary>();
			for (int frameIndex = 0; frameIndex < starts.Count; frameIndex++)
			{
				long end = ends[frameIndex];
				if (continuous && frameIndex + 1 < starts.Count)
				{
					end = starts[frameIndex + 1];
				}
				frames.Add(new TracyFrameSummary(frameIndex, starts[frameIndex], end));
			}
			frameSets.Add(new TracyFrameSetSummary(name, continuous, frames));
		}

		Dictionary<ulong, string> pointerMap = new Dictionary<ulong, string>();
		List<string> stringData = new List<string>();
		ulong stringDataCount = reader.ReadUInt64();
		if (stringDataCount > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many strings.");
		}
		for (ulong i = 0; i < stringDataCount; i++)
		{
			ulong pointer = reader.ReadUInt64();
			string value = reader.ReadSizedAsciiString(1024 * 1024);
			pointerMap[pointer] = value;
			stringData.Add(value);
		}

		SkipPointerMap(reader, pointerMap, out int stringCount);
		SkipPointerMap(reader, pointerMap, out int threadNameCount);
		SkipExternalNameMap(reader);

		if (!reader.HasRemaining)
		{
			return new TracyTraceMetadata(
				delay,
				resolution,
				timerMultiplier,
				lastTime,
				frameOffset,
				processId,
				samplingPeriod,
				cpuArchitecture,
				cpuId,
				cpuManufacturer,
				onDemand,
				captureName,
				captureProgram,
				captureTime,
				executableTime,
				hostInfo,
				frameSets,
				stringCount,
				threadNameCount);
		}

		List<string> sourceLocationNames = ReadSourceLocationsAndSkipToLocks(reader, pointerMap, stringData);
		SkipLocks(reader);
		SkipMessages(reader);
		SkipZoneExtra(reader);
		cpuZones = ReadCpuZones(reader, sourceLocationNames, out threadCount);
		if (reader.HasRemaining)
		{
			SkipGpuZones(reader, out ulong totalGpuZoneCount, out ulong gpuDataCount, out ulong gpuTimelineCount);
			if (totalGpuZoneCount > 0 || gpuDataCount > 0)
			{
				readerDiagnostics.Add(new Dictionary<string, object>
				{
					["severity"] = "warning",
					["code"] = "TracyGpuZonesUnsupported",
					["message"] = "GPU zones are present but are not exported by the initial Tracy normalized schema.",
					["gpuZoneCount"] = totalGpuZoneCount,
					["gpuContextCount"] = gpuDataCount,
					["gpuTimelineCount"] = gpuTimelineCount
				});
			}
		}
		if (reader.HasRemaining)
		{
			plots = ReadPlots(reader, pointerMap);
		}

		return new TracyTraceMetadata(
			delay,
			resolution,
			timerMultiplier,
			lastTime,
			frameOffset,
			processId,
			samplingPeriod,
			cpuArchitecture,
			cpuId,
			cpuManufacturer,
			onDemand,
			captureName,
			captureProgram,
			captureTime,
			executableTime,
			hostInfo,
			frameSets,
			stringCount,
			threadNameCount);
	}

	private static List<string> ReadSourceLocationsAndSkipToLocks(DecodedBlockReader reader, Dictionary<ulong, string> pointerMap, List<string> stringData)
	{
		SkipThreadCompress(reader);
		SkipThreadCompress(reader);
		SkipSourceLocationMap(reader);
		SkipUInt64Array(reader);

		ulong payloadCount = reader.ReadUInt64();
		if (payloadCount > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy file contains too many source locations.");
		}
		List<string> names = new List<string>();
		for (ulong i = 0; i < payloadCount; i++)
		{
			names.Add(ReadSourceLocationName(reader, pointerMap, stringData));
		}

		SkipSourceLocationZoneReservations(reader);
		SkipSourceLocationZoneReservations(reader);
		return names;
	}

	private static void SkipThreadCompress(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy thread compression table is too large.");
		}
		reader.Skip(checked((long)count * 8L));
	}

	private static void SkipSourceLocationMap(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy source location map is too large.");
		}
		for (ulong i = 0; i < count; i++)
		{
			reader.Skip(8);  // pointer
			reader.Skip(35); // SourceLocationBase
		}
	}

	private static void SkipUInt64Array(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy uint64 array section is too large.");
		}
		reader.Skip(checked((long)count * 8L));
	}

	private static string ReadSourceLocationName(DecodedBlockReader reader, Dictionary<ulong, string> pointerMap, List<string> stringData)
	{
		string name = ReadStringRef(reader, pointerMap, stringData);
		ReadStringRef(reader, pointerMap, stringData);
		ReadStringRef(reader, pointerMap, stringData);
		reader.Skip(4); // line
		reader.Skip(4); // color
		return string.IsNullOrWhiteSpace(name) ? "<unknown>" : name;
	}

	private static string ReadStringRef(DecodedBlockReader reader, Dictionary<ulong, string> pointerMap, List<string> stringData)
	{
		ulong value = reader.ReadUInt64();
		byte flags = reader.ReadByte();
		bool isIndex = (flags & 1) != 0;
		bool active = (flags & 2) != 0;
		if (!active)
		{
			return string.Empty;
		}
		if (isIndex)
		{
			return value < (ulong)stringData.Count ? stringData[(int)value] : string.Empty;
		}
		return pointerMap.TryGetValue(value, out string text) ? text : string.Empty;
	}

	private static void SkipSourceLocationZoneReservations(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy source-location zone reservation section is too large.");
		}
		reader.Skip(checked((long)count * 10L));
	}

	private static void SkipLocks(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy lock section is too large.");
		}
		for (ulong i = 0; i < count; i++)
		{
			reader.Skip(4 + 3 + 2 + 1 + 1 + 8 + 8);
			ulong threadCount = reader.ReadUInt64();
			reader.Skip(checked((long)threadCount * 8L));
			ulong eventCount = reader.ReadUInt64();
			reader.Skip(checked((long)eventCount * 12L));
		}
	}

	private static void SkipMessages(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy message section is too large.");
		}
		reader.Skip(checked((long)count * 32L));
	}

	private static void SkipZoneExtra(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy zone extra section is too large.");
		}
		reader.Skip(checked((long)count * 12L));
	}

	private static List<TracyCpuZoneSummary> ReadCpuZones(DecodedBlockReader reader, List<string> sourceLocationNames, out int threadCount)
	{
		List<TracyCpuZoneSummary> zones = new List<TracyCpuZoneSummary>();
		ulong totalZoneCount = reader.ReadUInt64();
		if (totalZoneCount > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy zone section is too large.");
		}
		reader.ReadUInt64(); // zoneChildren count
		ulong threadSectionCount = reader.ReadUInt64();
		if (threadSectionCount > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy thread section is too large.");
		}
		threadCount = checked((int)threadSectionCount);
		for (ulong threadIndex = 0; threadIndex < threadSectionCount; threadIndex++)
		{
			ulong threadId = reader.ReadUInt64();
			reader.ReadUInt64(); // thread zone count
			reader.ReadUInt64(); // kernelSampleCnt
			reader.Skip(1);      // isFiber
			uint timelineSize = reader.ReadUInt32();
			if (timelineSize > 0)
			{
				ReadCpuTimeline(reader, threadId, timelineSize, 0L, sourceLocationNames, zones);
			}
			ulong messageCount = reader.ReadUInt64();
			reader.Skip(checked((long)messageCount * 8L));
			ulong ctxSwitchSampleCount = reader.ReadUInt64();
			reader.Skip(checked((long)ctxSwitchSampleCount * 11L));
			ulong sampleCount = reader.ReadUInt64();
			reader.Skip(checked((long)sampleCount * 11L));
		}
		return zones;
	}

	private static void SkipGpuZones(DecodedBlockReader reader, out ulong totalGpuZoneCount, out ulong gpuDataCount, out ulong gpuTimelineCount)
	{
		totalGpuZoneCount = reader.ReadUInt64();
		if (totalGpuZoneCount > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU zone section is too large.");
		}
		reader.ReadUInt64(); // gpuChildren count
		gpuDataCount = reader.ReadUInt64();
		if (gpuDataCount > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU context section is too large.");
		}
		gpuTimelineCount = 0;
		for (ulong contextIndex = 0; contextIndex < gpuDataCount; contextIndex++)
		{
			reader.Skip(8);  // context thread
			reader.Skip(1);  // hasCalibration
			reader.Skip(8);  // context zone count
			reader.Skip(4);  // period
			reader.Skip(1);  // GpuContextType
			reader.Skip(4);  // name StringIdx
			reader.Skip(8);  // overflow
			ulong threadDataCount = reader.ReadUInt64();
			if (threadDataCount > 1_000_000)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU thread data section is too large.");
			}

			for (ulong threadIndex = 0; threadIndex < threadDataCount; threadIndex++)
			{
				reader.Skip(8); // thread id
				ulong timelineSize = reader.ReadUInt64();
				if (timelineSize > 10_000_000)
				{
					throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU timeline section is too large.");
				}
				gpuTimelineCount += timelineSize;
				SkipGpuTimeline(reader, timelineSize, 0);
			}
		}
	}

	private static void SkipGpuTimeline(DecodedBlockReader reader, ulong timelineSize, int depth)
	{
		if (depth > 512)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU timeline nesting is too deep.");
		}

		for (ulong i = 0; i < timelineSize; i++)
		{
			reader.Skip(8); // CPU start time offset
			reader.Skip(8); // GPU start time offset
			reader.Skip(2); // source location
			reader.Skip(3); // callstack
			reader.Skip(2); // thread id
			ulong childSize = reader.ReadUInt64();
			if (childSize > 10_000_000)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy GPU child timeline section is too large.");
			}
			SkipGpuTimeline(reader, childSize, depth + 1);
			reader.Skip(8); // CPU end time offset
			reader.Skip(8); // GPU end time offset
		}
	}

	private static List<TracyPlotSummary> ReadPlots(DecodedBlockReader reader, Dictionary<ulong, string> pointerMap)
	{
		ulong plotCount = reader.ReadUInt64();
		if (plotCount > 1_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy plot section is too large.");
		}
		List<TracyPlotSummary> plots = new List<TracyPlotSummary>();
		for (ulong i = 0; i < plotCount; i++)
		{
			byte type = reader.ReadByte();
			byte format = reader.ReadByte();
			reader.Skip(1); // showSteps
			reader.Skip(1); // fill
			reader.Skip(4); // color
			ulong namePointer = reader.ReadUInt64();
			double min = reader.ReadDouble();
			double max = reader.ReadDouble();
			double sum = reader.ReadDouble();
			ulong sampleCount = reader.ReadUInt64();
			if (sampleCount > 10_000_000)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy plot has too many samples.");
			}
			List<TracyPlotSample> samples = new List<TracyPlotSample>();
			long refTime = 0;
			for (ulong sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
			{
				long delta = reader.ReadInt64();
				refTime += delta;
				double value = reader.ReadDouble();
				samples.Add(new TracyPlotSample(refTime, value));
			}
			string name = pointerMap.TryGetValue(namePointer, out string resolvedName) ? resolvedName : namePointer.ToString("X");
			plots.Add(new TracyPlotSummary(name, type, format, min, max, sum, samples));
		}
		return plots;
	}

	private static long ReadCpuTimeline(DecodedBlockReader reader, ulong threadId, uint size, long refTime, List<string> sourceLocationNames, List<TracyCpuZoneSummary> zones)
	{
		for (uint i = 0; i < size; i++)
		{
			short sourceLocation = reader.ReadInt16();
			long start = ReadTimeOffset(reader, ref refTime);
			reader.Skip(4); // extra
			uint childSize = reader.ReadUInt32();
			if (childSize > 0)
			{
				refTime = ReadCpuTimeline(reader, threadId, childSize, refTime, sourceLocationNames, zones);
			}
			long end = ReadTimeOffset(reader, ref refTime);
			string name = sourceLocation >= 0 && sourceLocation < sourceLocationNames.Count
				? sourceLocationNames[sourceLocation]
				: "<unknown>";
			zones.Add(new TracyCpuZoneSummary(threadId, sourceLocation, name, start, end));
		}
		return refTime;
	}

	private static void SkipCpuTopology(DecodedBlockReader reader)
	{
		ulong packageCount = reader.ReadUInt64();
		if (packageCount > 4096)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many CPU packages.");
		}
		for (ulong packageIndex = 0; packageIndex < packageCount; packageIndex++)
		{
			reader.Skip(4); // packageId
			ulong coreCount = reader.ReadUInt64();
			if (coreCount > 1_000_000)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many CPU cores.");
			}
			for (ulong coreIndex = 0; coreIndex < coreCount; coreIndex++)
			{
				reader.Skip(4); // coreId
				ulong threadCount = reader.ReadUInt64();
				if (threadCount > 1_000_000)
				{
					throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many CPU threads.");
				}
				reader.Skip(checked((long)threadCount * 4L));
			}
		}
	}

	private static void SkipPointerMap(DecodedBlockReader reader, Dictionary<ulong, string> pointerMap, out int resolvedCount)
	{
		resolvedCount = 0;
		ulong count = reader.ReadUInt64();
		if (count > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many pointer map entries.");
		}
		for (ulong i = 0; i < count; i++)
		{
			reader.Skip(8); // id
			ulong pointer = reader.ReadUInt64();
			if (pointerMap.ContainsKey(pointer))
			{
				resolvedCount++;
			}
		}
	}

	private static void SkipExternalNameMap(DecodedBlockReader reader)
	{
		ulong count = reader.ReadUInt64();
		if (count > 10_000_000)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata contains too many external name entries.");
		}
		reader.Skip(checked((long)count * 24L));
	}

	private static long ReadTimeOffset(DecodedBlockReader reader, ref long refTime)
	{
		long offset = reader.ReadInt64();
		refTime += offset;
		return refTime;
	}

	private static byte[] ReadExactly(Stream stream, int size)
	{
		byte[] buffer = new byte[size];
		int offset = 0;
		while (offset < size)
		{
			int read = stream.Read(buffer, offset, size - offset);
			if (read == 0)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Unexpected end of Tracy file.");
			}
			offset += read;
		}
		return buffer;
	}

	private static bool TryReadUInt32(Stream stream, out uint value)
	{
		byte[] bytes = new byte[4];
		int offset = 0;
		while (offset < bytes.Length)
		{
			int read = stream.Read(bytes, offset, bytes.Length - offset);
			if (read == 0)
			{
				if (offset == 0)
				{
					value = 0;
					return false;
				}
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy block size was truncated.");
			}
			offset += read;
		}
		value = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
		return true;
	}

	private sealed class DecodedBlockReader
	{
		private readonly List<byte[]> m_Blocks;
		private int m_BlockIndex;
		private int m_BlockOffset;

		public DecodedBlockReader(List<byte[]> blocks)
		{
			m_Blocks = blocks;
		}

		public bool HasRemaining
		{
			get
			{
				int blockIndex = m_BlockIndex;
				int blockOffset = m_BlockOffset;
				while (blockIndex < m_Blocks.Count && blockOffset >= m_Blocks[blockIndex].Length)
				{
					blockIndex++;
					blockOffset = 0;
				}
				return blockIndex < m_Blocks.Count;
			}
		}

		public byte ReadByte()
		{
			MoveToAvailableBlock();
			if (m_BlockIndex >= m_Blocks.Count)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Unexpected end of Tracy decoded stream.");
			}
			return m_Blocks[m_BlockIndex][m_BlockOffset++];
		}

		public void Skip(long size)
		{
			if (size < 0)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Invalid Tracy metadata skip size.");
			}
			for (long i = 0; i < size; i++)
			{
				ReadByte();
			}
		}

		public long ReadInt64()
		{
			return unchecked((long)ReadUInt64());
		}

		public ulong ReadUInt64()
		{
			ulong value = 0;
			for (int i = 0; i < 8; i++)
			{
				value |= (ulong)ReadByte() << (8 * i);
			}
			return value;
		}

		public uint ReadUInt32()
		{
			uint value = 0;
			for (int i = 0; i < 4; i++)
			{
				value |= (uint)ReadByte() << (8 * i);
			}
			return value;
		}

		public short ReadInt16()
		{
			return unchecked((short)(ReadByte() | (ReadByte() << 8)));
		}

		public double ReadDouble()
		{
			byte[] bytes = ReadBytes(8);
			return BitConverter.ToDouble(bytes, 0);
		}

		public string ReadFixedAscii(int size)
		{
			byte[] bytes = ReadBytes(size);
			int length = Array.IndexOf(bytes, (byte)0);
			if (length < 0)
			{
				length = bytes.Length;
			}
			return Encoding.ASCII.GetString(bytes, 0, length);
		}

		public string ReadSizedAsciiString(int maxSize)
		{
			ulong size = ReadUInt64();
			if (size > (ulong)maxSize)
			{
				throw new TracyFileFormatException("TracyFileFormatInvalid", "Tracy metadata string is too large.");
			}
			byte[] bytes = ReadBytes(checked((int)size));
			return Encoding.ASCII.GetString(bytes);
		}

		private byte[] ReadBytes(int size)
		{
			byte[] bytes = new byte[size];
			for (int i = 0; i < size; i++)
			{
				bytes[i] = ReadByte();
			}
			return bytes;
		}

		private void MoveToAvailableBlock()
		{
			while (m_BlockIndex < m_Blocks.Count && m_BlockOffset >= m_Blocks[m_BlockIndex].Length)
			{
				m_BlockIndex++;
				m_BlockOffset = 0;
			}
		}
	}
}
