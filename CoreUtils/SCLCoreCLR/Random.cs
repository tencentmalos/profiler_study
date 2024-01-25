using System;

namespace SCLCoreCLR;

public class Random
{
	private const uint m_ArraySize = 624u;

	private uint[] MT = new uint[624];

	private uint m_Index;

	public Random()
		: this(1571)
	{
	}

	public Random(int seed)
		: this((uint)seed)
	{
	}

	public Random(uint seed)
	{
		SetSeed(seed);
	}

	public void SetSeed(int seed)
	{
		SetSeed((uint)seed);
	}

	public void SetSeed(uint seed)
	{
		MT[0] = seed;
		for (uint num = 1u; num < 624; num++)
		{
			MT[num] = 1812433253 * (MT[num - 1] ^ (MT[num - 1] >> 30)) + num;
		}
	}

	public uint Generate()
	{
		if (m_Index == 0)
		{
			GenerateNumbers();
		}
		uint num = MT[m_Index];
		uint num2 = num ^ (num >> 11);
		int num3 = (int)num2 ^ ((int)(num2 << 7) & -1658038656);
		int num4 = num3 ^ ((num3 << 15) & -272236544);
		int result = num4 ^ (num4 >>> 18);
		m_Index = (m_Index + 1) % 624;
		return (uint)result;
	}

	public int GeneratePositiveInt()
	{
		return Math.Abs((int)Generate());
	}

	public float GenerateFloat()
	{
		return (float)((double)Generate() / 4294967295.0);
	}

	private void GenerateNumbers()
	{
		for (uint num = 0u; num < 624; num++)
		{
			uint num2 = (MT[num] & 0x80000000u) | (MT[(num + 1) % 624] & 0x7FFFFFFFu);
			MT[num] = MT[(num + 397) % 624] ^ (num2 >> 1);
			if ((num2 & 1) == 1)
			{
				MT[num] ^= 0x9908B0DFu;
			}
		}
	}
}
