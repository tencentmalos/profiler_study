using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProfilerStudy.Tracy;

public static class Tracy010LiveCaptureClient
{
	public const uint ProtocolVersion = 64;

	private const int WelcomeMessageSize = 1178;
	private const int OnDemandPayloadMessageSize = 16;
	private const int TargetFrameSize = 256 * 1024;
	private const int MaxLiveBlockSize = 1024 * 1024;
	private const int ReadTimeoutMilliseconds = 500;
	private static readonly byte[] HandshakeShibboleth = Encoding.ASCII.GetBytes("TracyPrf");

	public static TracyLiveCaptureResult Capture(string host, int port, int durationSeconds)
	{
		return Capture(host, port, durationSeconds, CancellationToken.None);
	}

	public static TracyLiveCaptureResult Capture(string host, int port, int durationSeconds, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			throw new ArgumentException("Tracy host is required.", nameof(host));
		}
		if (port <= 0 || port > 65535)
		{
			throw new ArgumentOutOfRangeException(nameof(port), "Tracy port is outside the TCP port range.");
		}
		cancellationToken.ThrowIfCancellationRequested();

		Stopwatch connectStopwatch = Stopwatch.StartNew();
		using TcpClient client = new TcpClient();
		try
		{
			IAsyncResult connect = client.BeginConnect(host, port, null, null);
			int waitIndex = WaitHandle.WaitAny(new[] { connect.AsyncWaitHandle, cancellationToken.WaitHandle }, System.TimeSpan.FromSeconds(5));
			if (waitIndex == WaitHandle.WaitTimeout)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Timed out connecting to Tracy target.");
			}
			cancellationToken.ThrowIfCancellationRequested();
			client.EndConnect(connect);
		}
		catch (TracyFileFormatException)
		{
			throw;
		}
		catch (SocketException ex)
		{
			throw new TracyFileFormatException("TracyConnectFailed", "Failed to connect to Tracy target " + host + ":" + port + ": " + ex.Message + ".");
		}
		connectStopwatch.Stop();
		client.ReceiveTimeout = ReadTimeoutMilliseconds;
		client.SendTimeout = 5000;
		cancellationToken.ThrowIfCancellationRequested();
		TracyVersionRegistry.ResolveLiveAdapter(ProtocolVersion);

		using NetworkStream stream = client.GetStream();
		stream.Write(HandshakeShibboleth, 0, HandshakeShibboleth.Length);
		WriteUInt32(stream, ProtocolVersion);
		cancellationToken.ThrowIfCancellationRequested();
		int status = ReadByteOrEnd(stream, cancellationToken, DateTime.UtcNow.AddSeconds(5));
		if (status < 0)
		{
			throw CreateProtocolMismatch("Tracy target closed the connection before handshake status.", "closed");
		}
		if (status != 1)
		{
			throw CreateProtocolMismatch("Tracy target rejected protocol version " + ProtocolVersion + " with handshake status " + status + ".", status.ToString());
		}

		byte[] welcomeBytes = ReadExactly(stream, WelcomeMessageSize, cancellationToken, DateTime.UtcNow.AddSeconds(5));
		LiveWelcomeData welcome = ReadWelcomeData(welcomeBytes);
		TracyTraceMetadata metadata = welcome.Metadata;
		if (metadata.OnDemand)
		{
			byte[] onDemandPayload = ReadExactly(stream, OnDemandPayloadMessageSize, cancellationToken, DateTime.UtcNow.AddSeconds(5));
			ulong frameOffset = ReadUInt64(onDemandPayload, 0);
			long currentTime = ToTime(ReadInt64(onDemandPayload, 8), welcome.InitBegin, metadata.TimerMultiplier);
			metadata = CopyMetadata(metadata, currentTime, (long)frameOffset);
			welcome = new LiveWelcomeData(metadata, welcome.InitBegin, welcome.InitEnd);
		}
		ArrayList diagnostics = new ArrayList
		{
			new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyLiveWelcome",
				["message"] = "Tracy live target accepted protocol 64 and returned WelcomeMessage metadata.",
				["protocolVersion"] = ProtocolVersion,
				["captureProgram"] = metadata.CaptureProgram,
				["hostInfo"] = metadata.HostInfo,
				["processId"] = metadata.ProcessId
			}
		};

		int boundedDurationSeconds = Math.Max(0, Math.Min(durationSeconds, 300));
		DateTime endUtc = DateTime.UtcNow.AddSeconds(boundedDurationSeconds);
		Tracy010LiveEventDecoder decoder = new Tracy010LiveEventDecoder(metadata, welcome.InitBegin, (queryType, pointer, extra) => SendServerQuery(stream, queryType, pointer, extra));
		int compressedBlockCount = 0;
		long compressedByteCount = 0;
		long decodedByteCount = 0;
		byte[] previousBlock = null;
		while (DateTime.UtcNow < endUtc)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!TryReadUInt32(stream, cancellationToken, out uint blockSize))
			{
				continue;
			}
			if (blockSize == 0 || blockSize > MaxLiveBlockSize)
			{
				throw new TracyFileFormatException("TracyLiveDecodeFailed", "Invalid Tracy live LZ4 block size.");
			}
			byte[] compressedBlock = ReadExactly(stream, checked((int)blockSize), cancellationToken, DateTime.UtcNow.AddSeconds(2));
			byte[] decodedBlock = TracyLz4BlockDecoder.Decode(compressedBlock, TargetFrameSize, previousBlock);
			previousBlock = decodedBlock;
			compressedBlockCount++;
			compressedByteCount += blockSize;
			decodedByteCount += decodedBlock.Length;
			decoder.ProcessBlock(decodedBlock);
		}
		cancellationToken.ThrowIfCancellationRequested();
		TrySendTerminate(stream);

		TracyEventStream eventStream = decoder.CreateEventStream(compressedBlockCount, compressedByteCount, decodedByteCount, decodedByteCount, diagnostics);
		return new TracyLiveCaptureResult(eventStream, diagnostics, connectStopwatch.Elapsed.TotalMilliseconds);
	}

	private static TracyFileFormatException CreateProtocolMismatch(string message, string status)
	{
		return new TracyFileFormatException(
			"TracyProtocolMismatch",
			message,
			"protocol:" + ProtocolVersion + ",status:" + status,
			TracyVersionRegistry.SupportedVersions,
			TracyVersionRegistry.LockedVersion);
	}

	private sealed class LiveWelcomeData
	{
		public LiveWelcomeData(TracyTraceMetadata metadata, long initBegin, long initEnd)
		{
			Metadata = metadata;
			InitBegin = initBegin;
			InitEnd = initEnd;
		}

		public TracyTraceMetadata Metadata { get; }

		public long InitBegin { get; }

		public long InitEnd { get; }
	}

	private static LiveWelcomeData ReadWelcomeData(byte[] bytes)
	{
		using MemoryStream stream = new MemoryStream(bytes);
		double timerMultiplier = ReadDouble(stream);
		long initBegin = ReadInt64(stream);
		long initEnd = ReadInt64(stream);
		ulong delay = ReadUInt64(stream);
		ulong resolution = ReadUInt64(stream);
		ulong epoch = ReadUInt64(stream);
		ulong executableTime = ReadUInt64(stream);
		ulong processId = ReadUInt64(stream);
		long samplingPeriod = ReadInt64(stream);
		byte flags = ReadByte(stream);
		byte cpuArchitecture = ReadByte(stream);
		string cpuManufacturer = ReadFixedAscii(stream, 12);
		uint cpuId = ReadUInt32(stream);
		string programName = ReadFixedAscii(stream, 64);
		string hostInfo = ReadFixedAscii(stream, 1024);

		long lastTime = ToTime(initEnd, initBegin, timerMultiplier);
		TracyTraceMetadata metadata = new TracyTraceMetadata(
			(long)(delay * timerMultiplier),
			(long)(resolution * timerMultiplier),
			timerMultiplier,
			lastTime,
			0,
			processId,
			samplingPeriod,
			cpuArchitecture,
			cpuId,
			cpuManufacturer,
			(flags & 1) != 0,
			programName,
			programName,
			(long)epoch,
			(long)executableTime,
			hostInfo,
			new List<TracyFrameSetSummary>(),
			0,
			0);
		return new LiveWelcomeData(metadata, initBegin, initEnd);
	}

	private static TracyTraceMetadata CopyMetadata(TracyTraceMetadata metadata, long lastTime, long frameOffset)
	{
		return new TracyTraceMetadata(
			metadata.Delay,
			metadata.Resolution,
			metadata.TimerMultiplier,
			Math.Max(metadata.LastTime, lastTime),
			frameOffset,
			metadata.ProcessId,
			metadata.SamplingPeriod,
			metadata.CpuArchitecture,
			metadata.CpuId,
			metadata.CpuManufacturer,
			metadata.OnDemand,
			metadata.CaptureName,
			metadata.CaptureProgram,
			metadata.CaptureTime,
			metadata.ExecutableTime,
			metadata.HostInfo,
			metadata.FrameSets,
			metadata.StringCount,
			metadata.ThreadNameCount);
	}

	private static void TrySendTerminate(Stream stream)
	{
		try
		{
			SendServerQuery(stream, 0, 0, 0);
		}
		catch (IOException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static void SendServerQuery(Stream stream, byte queryType, ulong pointer, uint extra)
	{
		stream.WriteByte(queryType);
		WriteUInt64(stream, pointer);
		WriteUInt32(stream, extra);
	}

	private static byte[] ReadExactly(Stream stream, int size)
	{
		return ReadExactly(stream, size, CancellationToken.None);
	}

	private static byte[] ReadExactly(Stream stream, int size, CancellationToken cancellationToken)
	{
		return ReadExactly(stream, size, cancellationToken, null);
	}

	private static byte[] ReadExactly(Stream stream, int size, CancellationToken cancellationToken, DateTime? deadlineUtc)
	{
		byte[] bytes = new byte[size];
		int offset = 0;
		while (offset < size)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (deadlineUtc.HasValue && DateTime.UtcNow > deadlineUtc.Value)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Timed out reading Tracy live stream.");
			}
			int read = Read(stream, bytes, offset, size - offset, cancellationToken);
			if (read < 0)
			{
				continue;
			}
			if (read == 0)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Unexpected end of Tracy live stream.");
			}
			offset += read;
		}
		return bytes;
	}

	private static int Read(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		try
		{
			return stream.Read(buffer, offset, count);
		}
		catch (IOException ex) when (IsReadTimeout(ex))
		{
			return -1;
		}
	}

	private static int ReadByteOrEnd(Stream stream, CancellationToken cancellationToken, DateTime deadlineUtc)
	{
		byte[] bytes = new byte[1];
		int read = Read(stream, bytes, 0, 1, cancellationToken);
		while (read < 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (DateTime.UtcNow > deadlineUtc)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Timed out waiting for Tracy handshake status.");
			}
			read = Read(stream, bytes, 0, 1, cancellationToken);
		}
		return read == 0 ? -1 : bytes[0];
	}

	private static bool TryReadUInt32(Stream stream, CancellationToken cancellationToken, out uint value)
	{
		byte[] bytes = new byte[4];
		int offset = 0;
		DateTime partialReadDeadlineUtc = DateTime.MinValue;
		while (offset < bytes.Length)
		{
			cancellationToken.ThrowIfCancellationRequested();
			int read = Read(stream, bytes, offset, bytes.Length - offset, cancellationToken);
			if (read < 0)
			{
				if (offset == 0)
				{
					value = 0;
					return false;
				}
				if (DateTime.UtcNow > partialReadDeadlineUtc)
				{
					throw new TracyFileFormatException("TracyConnectFailed", "Timed out reading Tracy live block header.");
				}
				continue;
			}
			if (read == 0)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Unexpected end of Tracy live stream.");
			}
			offset += read;
			if (offset > 0 && partialReadDeadlineUtc == DateTime.MinValue)
			{
				partialReadDeadlineUtc = DateTime.UtcNow.AddSeconds(2);
			}
		}
		value = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
		return true;
	}

	private static bool IsReadTimeout(IOException ex)
	{
		return ex.InnerException is SocketException socketException &&
			socketException.SocketErrorCode == SocketError.TimedOut;
	}

	private static void WriteUInt32(Stream stream, uint value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteUInt64(Stream stream, ulong value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static byte ReadByte(Stream stream)
	{
		int value = stream.ReadByte();
		if (value < 0)
		{
			throw new TracyFileFormatException("TracyConnectFailed", "Unexpected end of Tracy welcome message.");
		}
		return (byte)value;
	}

	private static uint ReadUInt32(Stream stream)
	{
		byte[] bytes = ReadExactly(stream, 4);
		return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
	}

	private static ulong ReadUInt64(Stream stream)
	{
		byte[] bytes = ReadExactly(stream, 8);
		return ReadUInt64(bytes, 0);
	}

	private static ulong ReadUInt64(byte[] bytes, int offset)
	{
		ulong value = 0;
		for (int i = 0; i < 8; i++)
		{
			value |= (ulong)bytes[offset + i] << (8 * i);
		}
		return value;
	}

	private static long ReadInt64(Stream stream)
	{
		return unchecked((long)ReadUInt64(stream));
	}

	private static long ReadInt64(byte[] bytes, int offset)
	{
		return unchecked((long)ReadUInt64(bytes, offset));
	}

	private static long ToTime(long tsc, long baseTime, double timerMultiplier)
	{
		return (long)((tsc - baseTime) * timerMultiplier);
	}

	private static double ReadDouble(Stream stream)
	{
		byte[] bytes = ReadExactly(stream, 8);
		return BitConverter.ToDouble(bytes, 0);
	}

	private static string ReadFixedAscii(Stream stream, int size)
	{
		byte[] bytes = ReadExactly(stream, size);
		int length = Array.IndexOf(bytes, (byte)0);
		if (length < 0)
		{
			length = bytes.Length;
		}
		return Encoding.ASCII.GetString(bytes, 0, length);
	}
}
