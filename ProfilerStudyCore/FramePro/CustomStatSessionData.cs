using System.IO;

namespace ProfilerStudy;

public class CustomStatSessionData : IProfilerStudySerialisable
{
	private long m_Name;

	private CustomStatValueType m_ValueType;

	public long m_TotalValueInt64;

	public double m_TotalValueDouble;

	public long m_TotalCount;

	public long m_MinValuePerFrameInt64;

	public long m_MaxValuePerFrameInt64;

	public double m_MinValuePerFrameDouble;

	public double m_MaxValuePerFrameDouble;

	public long m_MinCountPerFrame;

	public long m_MaxCountPerFrame;

	public long m_AccTotalValueInt64;

	public double m_AccTotalValueDouble;

	public long m_AccMinValuePerFrameInt64;

	public long m_AccMaxValuePerFrameInt64;

	public double m_AccMinValuePerFrameDouble;

	public double m_AccMaxValuePerFrameDouble;

	public int m_FirstFrameSeen;

	public static LegacySetGraphUnitDelegate m_LegacySetGraphUnitDelegate;

	public long Name => m_Name;

	public CustomStatValueType ValueType => m_ValueType;

	public CustomStatSessionData()
	{
	}

	public CustomStatSessionData(long name, CustomStatValueType value_type, int first_frame_seen)
	{
		m_Name = name;
		m_ValueType = value_type;
		m_FirstFrameSeen = first_frame_seen;
	}

	public CustomStatSessionData(CustomStatSessionData other)
	{
		m_Name = other.m_Name;
		m_ValueType = other.m_ValueType;
		m_TotalValueInt64 = other.m_TotalValueInt64;
		m_TotalValueDouble = other.m_TotalValueDouble;
		m_TotalCount = other.m_TotalCount;
		m_MinValuePerFrameInt64 = other.m_MinValuePerFrameInt64;
		m_MaxValuePerFrameInt64 = other.m_MaxValuePerFrameInt64;
		m_MinValuePerFrameDouble = other.m_MinValuePerFrameDouble;
		m_MaxValuePerFrameDouble = other.m_MaxValuePerFrameDouble;
		m_MinCountPerFrame = other.m_MinCountPerFrame;
		m_MaxCountPerFrame = other.m_MaxCountPerFrame;
		m_AccTotalValueInt64 = other.m_AccTotalValueInt64;
		m_AccTotalValueDouble = other.m_AccTotalValueDouble;
		m_AccMinValuePerFrameInt64 = other.m_AccMinValuePerFrameInt64;
		m_AccMaxValuePerFrameInt64 = other.m_AccMaxValuePerFrameInt64;
		m_AccMinValuePerFrameDouble = other.m_AccMinValuePerFrameDouble;
		m_AccMaxValuePerFrameDouble = other.m_AccMaxValuePerFrameDouble;
		m_FirstFrameSeen = other.m_FirstFrameSeen;
	}

	public void OnNameRemapped(long old_string_id, long new_string_id)
	{
		if (m_Name == old_string_id)
		{
			m_Name = new_string_id;
		}
	}

	public void Read(BinaryReader binary_reader, int version)
	{
		m_Name = binary_reader.ReadInt64();
		m_ValueType = (CustomStatValueType)binary_reader.ReadInt32();
		if (version < 40)
		{
			long graph = binary_reader.ReadInt64();
			long unit = binary_reader.ReadInt64();
			m_LegacySetGraphUnitDelegate(m_Name, graph, unit);
		}
		m_TotalValueInt64 = binary_reader.ReadInt64();
		m_TotalValueDouble = binary_reader.ReadDouble();
		m_TotalCount = binary_reader.ReadInt64();
		m_MinValuePerFrameInt64 = ((version >= 32) ? binary_reader.ReadInt64() : 0);
		m_MaxValuePerFrameInt64 = binary_reader.ReadInt64();
		m_MinValuePerFrameDouble = ((version >= 32) ? binary_reader.ReadDouble() : 0.0);
		m_MaxValuePerFrameDouble = binary_reader.ReadDouble();
		m_MinCountPerFrame = ((version >= 32) ? binary_reader.ReadInt64() : 0);
		m_MaxCountPerFrame = binary_reader.ReadInt64();
		if (version >= 41)
		{
			m_AccTotalValueInt64 = binary_reader.ReadInt64();
			m_AccTotalValueDouble = binary_reader.ReadDouble();
			m_AccMinValuePerFrameInt64 = ((version >= 32) ? binary_reader.ReadInt64() : 0);
			m_AccMaxValuePerFrameInt64 = binary_reader.ReadInt64();
			m_AccMinValuePerFrameDouble = ((version >= 32) ? binary_reader.ReadDouble() : 0.0);
			m_AccMaxValuePerFrameDouble = binary_reader.ReadDouble();
		}
		if (version >= 40)
		{
			m_FirstFrameSeen = binary_reader.ReadInt32();
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Name);
		binary_writer.Write((int)m_ValueType);
		binary_writer.Write(m_TotalValueInt64);
		binary_writer.Write(m_TotalValueDouble);
		binary_writer.Write(m_TotalCount);
		binary_writer.Write(m_MinValuePerFrameInt64);
		binary_writer.Write(m_MaxValuePerFrameInt64);
		binary_writer.Write(m_MinValuePerFrameDouble);
		binary_writer.Write(m_MaxValuePerFrameDouble);
		binary_writer.Write(m_MinCountPerFrame);
		binary_writer.Write(m_MaxCountPerFrame);
		binary_writer.Write(m_AccTotalValueInt64);
		binary_writer.Write(m_AccTotalValueDouble);
		binary_writer.Write(m_AccMinValuePerFrameInt64);
		binary_writer.Write(m_AccMaxValuePerFrameInt64);
		binary_writer.Write(m_AccMinValuePerFrameDouble);
		binary_writer.Write(m_AccMaxValuePerFrameDouble);
		binary_writer.Write(m_FirstFrameSeen);
	}
}
