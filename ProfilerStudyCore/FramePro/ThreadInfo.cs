using System.IO;

namespace ProfilerStudy;

internal class ThreadInfo : IProfilerStudySerialisable
{
	public string m_Name;

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

	public ThreadInfo()
	{
	}

	public ThreadInfo(string name)
	{
		m_Name = name;
	}

	public void Read(BinaryReader reader, int version)
	{
		m_Name = reader.ReadString();
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_Name);
	}
}
