using System.IO;

namespace ProfilerStudy;

public struct CustomStat
{
	private long m_Name;

	private long m_ValueInt64;

	private double m_ValueDouble;

	private long m_Count;

	private long m_AccValueInt64;

	private double m_AccValueDouble;

	public static LegacySetGraphUnitDelegate m_LegacySetGraphUnitDelegate;

	public long Name => m_Name;

	public long ValueInt64 => m_ValueInt64;

	public double ValueDouble => m_ValueDouble;

	public long AccValueInt64 => m_AccValueInt64;

	public double AccValueDouble => m_AccValueDouble;

	public long Count => m_Count;

	public CustomStat(long name, long value_int64, double value_double, long count)
		: this(name, value_int64, value_double, value_int64, value_double, count)
	{
	}

	public CustomStat(long name, long value_int64, double value_double, long acc_value_int64, double acc_value_double, long count)
	{
		m_Name = name;
		m_ValueInt64 = value_int64;
		m_ValueDouble = value_double;
		m_AccValueInt64 = acc_value_int64;
		m_AccValueDouble = acc_value_double;
		m_Count = count;
	}

	public void OnStringRemapped(long old_string_id, long new_string_id)
	{
		m_Name = new_string_id;
	}

	public void Read(BinaryReader binary_reader, int version)
	{
		m_Name = binary_reader.ReadInt64();
		m_ValueInt64 = binary_reader.ReadInt64();
		m_ValueDouble = binary_reader.ReadDouble();
		m_Count = binary_reader.ReadInt64();
		if (version >= 41)
		{
			m_AccValueInt64 = binary_reader.ReadInt64();
			m_AccValueDouble = binary_reader.ReadDouble();
		}
		if (version < 40)
		{
			long graph = binary_reader.ReadInt64();
			long unit = binary_reader.ReadInt64();
			m_LegacySetGraphUnitDelegate(m_Name, graph, unit);
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Name);
		binary_writer.Write(m_ValueInt64);
		binary_writer.Write(m_ValueDouble);
		binary_writer.Write(m_Count);
		binary_writer.Write(m_AccValueInt64);
		binary_writer.Write(m_AccValueDouble);
	}
}
