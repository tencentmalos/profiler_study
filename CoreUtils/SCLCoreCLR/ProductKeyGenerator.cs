using System;
using System.Collections.Generic;

namespace SCLCoreCLR;

public class ProductKeyGenerator
{
	public enum Mode
	{
		Invalid,
		Demo,
		Purchased
	}

	private string m_ProductID;

	private int m_Version;

	private const ulong m_Comp = 5166777255949604290uL;

	private const ulong m_RegComp = 7435984585426691294uL;

	private const int m_PurchaseKeyCount = 10000;

	private List<uint> m_PurchaseCodes = new List<uint>(10000);

	private const int m_ShuffleOrderSeed = 1234;

	private int[] m_ShuffleOrder;

	private const int m_RegShuffleOrderKey = 5678;

	private int[] m_RegShuffleOrder;

	public ProductKeyGenerator(string product_id, int version)
	{
		m_ProductID = product_id;
		m_Version = version;
		Initialise();
	}

	private void Initialise()
	{
		Random rand = new Random(GenerateSeed(m_ProductID, m_Version));
		InitialisePurchaseCodes(rand);
		m_ShuffleOrder = GenerateShuffleOrder(rand);
		m_RegShuffleOrder = GenerateShuffleOrder(rand);
	}

	private static int[] GenerateShuffleOrder(Random rand)
	{
		List<int> list = new List<int>(64);
		for (int i = 0; i < 64; i++)
		{
			list.Add(i);
		}
		int[] array = new int[64];
		for (int j = 0; j < 64; j++)
		{
			int index = rand.GeneratePositiveInt() % list.Count;
			array[j] = list[index];
			list.RemoveAt(index);
		}
		return array;
	}

	private static ulong Shuffle(ulong value, int[] order)
	{
		ulong num = 0uL;
		for (int i = 0; i < 64; i++)
		{
			int num2 = order[i];
			num |= ((value >> i) & 1) << num2;
		}
		return num;
	}

	private static ulong Unshuffle(ulong value, int[] order)
	{
		ulong num = 0uL;
		for (int i = 0; i < 64; i++)
		{
			int num2 = order[i];
			num |= ((value >> num2) & 1) << i;
		}
		return num;
	}

	private ulong Obfuscate(ulong value)
	{
		return Shuffle(value ^ 0x47B4148A1E7195C2uL, m_ShuffleOrder);
	}

	private ulong Unobfuscate(ulong value)
	{
		return Unshuffle(value, m_ShuffleOrder) ^ 0x47B4148A1E7195C2uL;
	}

	private static uint GetDay()
	{
		return (uint)(DateTime.Now.ToFileTimeUtc() / 10000 / 1000 / 60 / 60 / 24);
	}

	private static uint GenerateSeed(string product_id, int version)
	{
		char num = product_id[0];
		uint num2 = product_id[1];
		return ((uint)num << 16) | (num2 << 8) | (uint)version;
	}

	private void InitialisePurchaseCodes(Random rand)
	{
		if (m_PurchaseCodes.Count == 0)
		{
			for (int i = 0; i < 10000; i++)
			{
				m_PurchaseCodes.Add(rand.Generate());
			}
		}
	}

	public List<ulong> GetAllPurchaseKeys()
	{
		List<ulong> list = new List<ulong>();
		foreach (uint purchaseCode in m_PurchaseCodes)
		{
			ulong item = GenerateKey(Mode.Purchased, purchaseCode);
			list.Add(item);
		}
		return list;
	}

	public ulong GenerateDemoKey(uint demo_length)
	{
		uint code = GetDay() + demo_length;
		return GenerateKey(Mode.Demo, code);
	}

	private ulong GenerateKey(Mode mode, uint code)
	{
		long num = (byte)m_ProductID[0];
		ulong num2 = (byte)m_ProductID[1];
		long num3 = (num << 8) | (long)num2;
		ulong num4 = (ulong)m_Version;
		ulong num5 = (ulong)mode;
		ulong num6 = code;
		ulong value = (ulong)(num3 << 48) | (num4 << 40) | (num5 << 32) | num6;
		return Obfuscate(value);
	}

	public bool UnpackKey(ulong key, ref int version, ref Mode mode, ref int days_left)
	{
		ulong num = Unobfuscate(key);
		ulong num2 = (num >> 48) & 0xFFFF;
		ulong num3 = (num >> 40) & 0xFF;
		ulong num4 = (num >> 32) & 0xFF;
		uint num5 = (uint)(num & 0xFFFFFFFFu);
		char c = (char)((num2 >> 8) & 0xFF);
		char c2 = (char)(num2 & 0xFF);
		if (m_ProductID[0] != c || m_ProductID[1] != c2)
		{
			return false;
		}
		version = (int)num3;
		mode = (Mode)num4;
		switch (mode)
		{
		case Mode.Demo:
		{
			uint num6 = num5;
			uint day = GetDay();
			days_left = (int)(num6 - day);
			return true;
		}
		case Mode.Purchased:
			return m_PurchaseCodes.Contains(num5);
		default:
			return false;
		}
	}

	public ulong ObfuscateForRegistry(ulong value)
	{
		return Shuffle(value ^ 0x6731EC9539A364DEuL, m_RegShuffleOrder);
	}

	public ulong UnobfuscateForRegistry(ulong value)
	{
		return Unshuffle(value, m_RegShuffleOrder) ^ 0x6731EC9539A364DEuL;
	}

	private void Check()
	{
		ulong value = GenerateDemoKey(30u);
		ulong value2 = ObfuscateForRegistry(value);
		UnobfuscateForRegistry(value2);
	}
}
