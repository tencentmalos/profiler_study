namespace Editor;

public class PropertyName
{
	private string m_Name;

	private string m_NiceName;

	public string Name
	{
		get
		{
			return m_Name;
		}
		set
		{
			m_Name = value;
		}
	}

	public string NiceName
	{
		get
		{
			return m_NiceName;
		}
		set
		{
			m_NiceName = value;
		}
	}

	public PropertyName(string name)
	{
		m_Name = name;
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (c >= 'A' && c <= 'Z')
			{
				m_NiceName += " ";
			}
			m_NiceName += c;
		}
		m_NiceName = m_NiceName.Trim();
	}

	public override string ToString()
	{
		return m_NiceName;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PropertyName propertyName))
		{
			return false;
		}
		return propertyName.m_Name == m_Name;
	}

	public override int GetHashCode()
	{
		return m_Name.GetHashCode();
	}
}
