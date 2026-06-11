using System;
using System.IO;
using System.Text;

namespace ProfilerStudy.Tracy;

public static class Tracy010FileReader
{
	private const int MaxBlockSize = 1024 * 1024;
	private const int MaxDecodedBlockSize = 64 * 1024;

	public static TracyFileHeader ReadHeader(string path)
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

		uint blockSize = ReadUInt32(stream);
		if (blockSize == 0 || blockSize > MaxBlockSize)
		{
			throw new TracyFileFormatException("TracyFileFormatInvalid", "Invalid Tracy first block size.");
		}
		byte[] compressedBlock = ReadExactly(stream, checked((int)blockSize));
		byte[] firstBlock = TracyLz4BlockDecoder.Decode(compressedBlock, MaxDecodedBlockSize);
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
		return new TracyFileHeader(version, "lz4");
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

	private static uint ReadUInt32(Stream stream)
	{
		byte[] bytes = ReadExactly(stream, 4);
		return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
	}
}
