using System;
using System.Text;

namespace FramePro;

internal class StringReader
{
	private const int g_MaxInlineStringLength = 256;

	public static int MaxInlineStringLength => 256;

	private static bool ReadByteArray(ReceiveStream reader, byte[] array)
	{
		int num;
		for (int i = 0; i != array.Length; i += num)
		{
			num = reader.Read(array, i, array.Length - i);
			if (num == 0)
			{
				return false;
			}
		}
		return true;
	}

	public static string Read(ReceiveStream reader, int length)
	{
		byte[] array = new byte[(length + 3) & -4];
		if (ReadByteArray(reader, array))
		{
			return Encoding.ASCII.GetString(array, 0, length);
		}
		return "";
	}

	public static string ReadW(ReceiveStream reader, int length)
	{
		int num = 2 * length;
		byte[] array = new byte[(num + 3) & -4];
		if (ReadByteArray(reader, array))
		{
			char[] array2 = new char[length];
			Buffer.BlockCopy(array, 0, array2, 0, num);
			return new string(array2);
		}
		return "";
	}

	public static string ReadInlineString(ReceiveStream reader)
	{
		byte[] array = new byte[256];
		if (ReadByteArray(reader, array))
		{
			int num = 0;
			for (int i = 0; i < 256 && array[i] != 0; i++)
			{
				num++;
			}
			return Encoding.ASCII.GetString(array, 0, num);
		}
		return "";
	}
}
