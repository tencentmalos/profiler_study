namespace SCLCoreCLR;

public class Wildcard
{
	private string m_Pattern;

	private string m_PatternLowercase;

	private static char[] m_LowercaseTable;

	public string Pattern => m_Pattern;

	static Wildcard()
	{
		m_LowercaseTable = new char[255];
		for (int i = 0; i < 255; i++)
		{
			m_LowercaseTable[i] = (char)((i >= 65 && i <= 90) ? (i + 97 - 65) : i);
		}
	}

	private static char MakeLowercase(char c)
	{
		if ((long)c >= (long)m_LowercaseTable.Length)
		{
			return c;
		}
		return m_LowercaseTable[(uint)c];
	}

	public static bool Match(string value, string pattern)
	{
		return new Wildcard(pattern).IsMatch(value);
	}

	public Wildcard(string pattern)
	{
		m_Pattern = pattern;
		m_PatternLowercase = pattern.ToLower();
	}

	public bool IsMatch(string str)
	{
		if (m_Pattern.Length == 0)
		{
			return false;
		}
		bool flag = false;
		int num = 0;
		int num2 = 0;
		int length = m_Pattern.Length;
		int length2 = str.Length;
		while (true)
		{
			flag = false;
			if (m_Pattern[num] == '*')
			{
				flag = true;
				do
				{
					num++;
				}
				while (num < length && m_Pattern[num] == '*');
			}
			int i;
			while (true)
			{
				int num3;
				for (i = 0; num + i < length && m_Pattern[num + i] != '*'; i++)
				{
					num3 = num2 + i;
					if (num3 == length2)
					{
						return false;
					}
					if (MakeLowercase(str[num3]) == m_PatternLowercase[num + i])
					{
						continue;
					}
					if (num3 == length2)
					{
						return false;
					}
					if (m_Pattern[num + i] == '?' && str[num3] != '.')
					{
						continue;
					}
					goto IL_00ab;
				}
				if (num + i < length && m_Pattern[num + i] == '*')
				{
					break;
				}
				if (num2 + i == length2)
				{
					return true;
				}
				if (i != 0 && m_Pattern[num + i - 1] == '*')
				{
					return true;
				}
				if (!flag)
				{
					return false;
				}
				num2++;
				continue;
				IL_00ab:
				if (!flag)
				{
					return false;
				}
				num2++;
				if (num3 == length2)
				{
					return false;
				}
			}
			num2 += i;
			num += i;
		}
	}
}
