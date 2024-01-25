using System.Collections.Generic;

namespace SCLCoreCLR;

public class LinearDictionary<TKey, TValue>
{
	private struct Pair
	{
		public TKey m_Key;

		public TValue m_Value;
	}

	private List<Pair> m_Pairs = new List<Pair>();

	public TValue this[TKey key]
	{
		get
		{
			int num = 0;
			foreach (Pair pair in m_Pairs)
			{
				TKey key2 = pair.m_Key;
				if (key2.Equals(key))
				{
					if (num != 0)
					{
						Pair value = m_Pairs[0];
						m_Pairs[0] = pair;
						m_Pairs[num] = value;
					}
					return pair.m_Value;
				}
				num++;
			}
			return default(TValue);
		}
		set
		{
			Pair item = default(Pair);
			item.m_Key = key;
			item.m_Value = value;
			m_Pairs.Add(item);
		}
	}

	public void Clear()
	{
		m_Pairs.Clear();
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		foreach (Pair pair in m_Pairs)
		{
			TKey key2 = pair.m_Key;
			if (key2.Equals(key))
			{
				value = pair.m_Value;
				return true;
			}
		}
		value = default(TValue);
		return false;
	}
}
