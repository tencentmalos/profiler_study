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
	private static readonly byte[] HandshakeShibboleth = Encoding.ASCII.GetBytes("TracyPrf");

	public static TracyLiveCaptureResult Capture(string host, int port, int durationSeconds)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			throw new ArgumentException("Tracy host is required.", nameof(host));
		}
		if (port <= 0 || port > 65535)
		{
			throw new ArgumentOutOfRangeException(nameof(port), "Tracy port is outside the TCP port range.");
		}

		Stopwatch connectStopwatch = Stopwatch.StartNew();
		using TcpClient client = new TcpClient();
		try
		{
			IAsyncResult connect = client.BeginConnect(host, port, null, null);
			if (!connect.AsyncWaitHandle.WaitOne(System.TimeSpan.FromSeconds(5)))
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Timed out connecting to Tracy target.");
			}
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
		client.ReceiveTimeout = 5000;
		client.SendTimeout = 5000;

		using NetworkStream stream = client.GetStream();
		stream.Write(HandshakeShibboleth, 0, HandshakeShibboleth.Length);
		WriteUInt32(stream, ProtocolVersion);
		int status = stream.ReadByte();
		if (status < 0)
		{
			throw CreateProtocolMismatch("Tracy target closed the connection before handshake status.", "closed");
		}
		if (status != 1)
		{
			throw CreateProtocolMismatch("Tracy target rejected protocol version " + ProtocolVersion + " with handshake status " + status + ".", status.ToString());
		}

		byte[] welcomeBytes = ReadExactly(stream, WelcomeMessageSize);
		TracyTraceMetadata metadata = ReadWelcomeMetadata(welcomeBytes);
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
			},
			new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyLiveEventDecodingPending",
				["message"] = "Live LZ4 event stream decoding is not enabled in this implementation slice."
			}
		};

		int sleepMilliseconds = Math.Max(0, Math.Min(durationSeconds, 300)) * 1000;
		if (sleepMilliseconds > 0)
		{
			Thread.Sleep(sleepMilliseconds);
		}
		TrySendTerminate(stream);

		TracyEventStream eventStream = new TracyEventStream(
			new TracyFileHeader(TracyVersionRegistry.LockedVersion, "live"),
			0,
			0,
			0,
			0,
			metadata,
			new List<TracyCpuZoneSummary>(),
			new List<TracyPlotSummary>(),
			new List<TracyThreadSummary>(),
			0,
			diagnostics);
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

	private static TracyTraceMetadata ReadWelcomeMetadata(byte[] bytes)
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

		long lastTime = (long)((initEnd - initBegin) * timerMultiplier);
		return new TracyTraceMetadata(
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
	}

	private static void TrySendTerminate(Stream stream)
	{
		try
		{
			stream.WriteByte(0);
			WriteUInt64(stream, 0);
			WriteUInt32(stream, 0);
		}
		catch (IOException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private static byte[] ReadExactly(Stream stream, int size)
	{
		byte[] bytes = new byte[size];
		int offset = 0;
		while (offset < size)
		{
			int read = stream.Read(bytes, offset, size - offset);
			if (read == 0)
			{
				throw new TracyFileFormatException("TracyConnectFailed", "Unexpected end of Tracy live stream.");
			}
			offset += read;
		}
		return bytes;
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
		ulong value = 0;
		for (int i = 0; i < bytes.Length; i++)
		{
			value |= (ulong)bytes[i] << (8 * i);
		}
		return value;
	}

	private static long ReadInt64(Stream stream)
	{
		return unchecked((long)ReadUInt64(stream));
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
