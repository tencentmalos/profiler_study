using SCLCoreCLR;

namespace ProfilerStudy;

internal class ThreadFilterRow
{
	public string m_Name;

	public bool m_Visible = true;

	public bool m_Collapsed;

	public int m_CustomHeight;

	public ThreadFilterRow()
	{
	}

	public ThreadFilterRow(string name)
	{
		m_Name = name;
	}

	public ThreadFilterRow(ThreadFilterRow other)
	{
		m_Name = other.m_Name;
		m_Visible = other.m_Visible;
		m_Collapsed = other.m_Collapsed;
		m_CustomHeight = other.m_CustomHeight;
	}

	public void Read(XmlReadStream stream)
	{
		stream.Read("Name", ref m_Name);
		stream.Read("Visible", ref m_Visible);
		stream.Read("Collapsed", ref m_Collapsed);
		stream.Read("CustomHeight", ref m_CustomHeight);
	}

	public void Write(XmlWriteStream stream)
	{
		stream.Write("Name", m_Name);
		stream.Write("Visible", m_Visible);
		stream.Write("Collapsed", m_Collapsed);
		stream.Write("CustomHeight", m_CustomHeight);
	}
}
