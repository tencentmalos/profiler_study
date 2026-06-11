using System;

namespace ProfilerStudy.Tracy;

internal static class TracyLz4BlockDecoder
{
	public static byte[] Decode(byte[] input, int maxOutputSize)
	{
		return Decode(input, maxOutputSize, null);
	}

	public static byte[] Decode(byte[] input, int maxOutputSize, byte[] dictionary)
	{
		if (input == null)
		{
			throw new ArgumentNullException(nameof(input));
		}
		if (maxOutputSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(maxOutputSize));
		}

		byte[] output = new byte[maxOutputSize];
		int inputOffset = 0;
		int outputOffset = 0;
		while (inputOffset < input.Length)
		{
			byte token = input[inputOffset++];
			int literalLength = token >> 4;
			if (literalLength == 15)
			{
				literalLength += ReadExtendedLength(input, ref inputOffset);
			}
			if (literalLength < 0 || inputOffset + literalLength > input.Length || outputOffset + literalLength > output.Length)
			{
				throw new TracyFileFormatException("TracyLz4DecodeFailed", "Invalid Tracy LZ4 literal block.");
			}
			Buffer.BlockCopy(input, inputOffset, output, outputOffset, literalLength);
			inputOffset += literalLength;
			outputOffset += literalLength;
			if (inputOffset == input.Length)
			{
				break;
			}
			if (inputOffset + 2 > input.Length)
			{
				throw new TracyFileFormatException("TracyLz4DecodeFailed", "Invalid Tracy LZ4 match offset.");
			}
			int matchOffset = input[inputOffset] | (input[inputOffset + 1] << 8);
			inputOffset += 2;
			int dictionaryLength = dictionary == null ? 0 : dictionary.Length;
			if (matchOffset <= 0 || matchOffset > outputOffset + dictionaryLength)
			{
				throw new TracyFileFormatException("TracyLz4DecodeFailed", "Invalid Tracy LZ4 match distance.");
			}
			int matchLength = token & 0x0F;
			if (matchLength == 15)
			{
				matchLength += ReadExtendedLength(input, ref inputOffset);
			}
			matchLength += 4;
			if (outputOffset + matchLength > output.Length)
			{
				throw new TracyFileFormatException("TracyLz4DecodeFailed", "Invalid Tracy LZ4 match length.");
			}
			int matchSource = outputOffset - matchOffset;
			for (int i = 0; i < matchLength; i++)
			{
				int sourceIndex = matchSource + i;
				if (sourceIndex < 0)
				{
					output[outputOffset++] = dictionary[dictionaryLength + sourceIndex];
				}
				else
				{
					output[outputOffset++] = output[sourceIndex];
				}
			}
		}

		byte[] result = new byte[outputOffset];
		Buffer.BlockCopy(output, 0, result, 0, outputOffset);
		return result;
	}

	private static int ReadExtendedLength(byte[] input, ref int inputOffset)
	{
		int length = 0;
		byte value;
		do
		{
			if (inputOffset >= input.Length)
			{
				throw new TracyFileFormatException("TracyLz4DecodeFailed", "Invalid Tracy LZ4 extended length.");
			}
			value = input[inputOffset++];
			length += value;
		}
		while (value == 255);
		return length;
	}
}
