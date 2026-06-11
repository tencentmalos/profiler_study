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
		TracyTraceMetadata metadata = TryReadMetadata(decodedBlocks, payloadByteCount);
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
				["metadataDecoded"] = metadata != null
			}
		};
		return new TracyEventStream(header, decodedBlocks.Count, compressedByteCount, decodedByteCount, payloadByteCount, metadata, diagnostics);
	}

	private static TracyTraceMetadata TryReadMetadata(List<byte[]> decodedBlocks, long payloadByteCount)
	{
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
			long firstStart = 0;
			long lastEnd = 0;
			for (ulong frameIndex = 0; frameIndex < frameCount; frameIndex++)
			{
				long start = ReadTimeOffset(reader, ref refTime);
				long end = continuous ? -1 : ReadTimeOffset(reader, ref refTime);
				reader.Skip(4); // frameImage
				if (frameIndex == 0)
				{
					firstStart = start;
				}
				lastEnd = end;
			}
			frameSets.Add(new TracyFrameSetSummary(name, continuous, checked((int)frameCount), firstStart, lastEnd));
		}

		Dictionary<ulong, string> pointerMap = new Dictionary<ulong, string>();
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
		}

		SkipPointerMap(reader, pointerMap, out int stringCount);
		SkipPointerMap(reader, pointerMap, out int threadNameCount);
		SkipExternalNameMap(reader);

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
