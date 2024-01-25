using System;

namespace SCLCoreCLR;

public class MurmurHash2
{
	public static uint Hash(string str)
	{
		return Hash(GetBytes(str), 3314489979u);
	}

	private static byte[] GetBytes(string str)
	{
		byte[] array = new byte[str.Length * 2];
		Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
		return array;
	}

	private static uint Hash(byte[] data, uint seed)
	{
		int num = data.Length;
		if (num == 0)
		{
			return 0u;
		}
		uint num2 = seed ^ (uint)num;
		int num3 = 0;
		while (num >= 4)
		{
			uint num4 = BitConverter.ToUInt32(data, num3);
			num4 *= 1540483477;
			num4 ^= num4 >> 24;
			num4 *= 1540483477;
			num2 *= 1540483477;
			num2 ^= num4;
			num3 += 4;
			num -= 4;
		}
		switch (num)
		{
		case 3:
			num2 ^= BitConverter.ToUInt16(data, num3);
			num2 ^= (uint)(data[num3 + 2] << 16);
			num2 *= 1540483477;
			break;
		case 2:
			num2 ^= BitConverter.ToUInt16(data, num3);
			num2 *= 1540483477;
			break;
		case 1:
			num2 ^= data[num3];
			num2 *= 1540483477;
			break;
		}
		num2 ^= num2 >> 13;
		num2 *= 1540483477;
		return num2 ^ (num2 >> 15);
	}
}
