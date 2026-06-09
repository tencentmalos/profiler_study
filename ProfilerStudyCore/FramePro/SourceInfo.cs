using System.IO;

namespace ProfilerStudy;

public class SourceInfo : IProfilerStudySerialisable
{
	private string m_Filename = "";

	private string m_Function = "";

	private int m_Line;

	private TimeSpanType m_TimeSpanType;

	public string Filename => m_Filename;

	public string Function => m_Function;

	public int Line => m_Line;

	public TimeSpanType TimeSpanType => m_TimeSpanType;

	public SourceInfo()
	{
	}

	public SourceInfo(string filename, string function, int line, TimeSpanType time_span_type)
	{
		m_Filename = filename;
		m_Function = function;
		m_Line = line;
		m_TimeSpanType = time_span_type;
	}

	public SourceInfo(SourceInfo source_info)
	{
		m_Filename = source_info.m_Filename;
		m_Function = source_info.m_Function;
		m_Line = source_info.m_Line;
		m_TimeSpanType = source_info.m_TimeSpanType;
	}

	public void Read(BinaryReader reader, int version)
	{
		m_Filename = reader.ReadString();
		m_Function = reader.ReadString();
		m_Line = reader.ReadInt32();
		if (version > 3)
		{
			m_TimeSpanType = (TimeSpanType)reader.ReadInt32();
		}
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_Filename);
		writer.Write(m_Function);
		writer.Write(m_Line);
		writer.Write((int)m_TimeSpanType);
	}
}
