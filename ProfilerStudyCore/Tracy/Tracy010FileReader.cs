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
				["payloadByteCount"] = payloadByteCount
			}
		};
		return new TracyEventStream(header, decodedBlocks.Count, compressedByteCount, decodedByteCount, payloadByteCount, diagnostics);
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
}
