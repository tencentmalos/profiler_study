using System.Collections.Generic;
using System.IO;

namespace ProfilerStudy;

public class Frame : IProfilerStudySerialisable
{
	private int m_Index;

	private long m_StartTime;

	private long m_EndTime;

	private int m_TimeSpanCount;

	private int m_BytesSent;

	private long m_WaitForSendCompleteTime;

	private long m_PrevFrameSendTime;

	private long m_SelTimeSpanNameId;

	private int m_SelTimeSpanThreadId;

	private List<CustomStat> m_CustomStats = new List<CustomStat>();

	public bool Valid => m_EndTime != long.MaxValue;

	public long StartTime => m_StartTime;

	public long EndTime => m_EndTime;

	public long Duration => m_EndTime - m_StartTime;

	public int Index => m_Index;

	public int TimeSpanCount => m_TimeSpanCount;

	public int BytesSent => m_BytesSent;

	public long WaitForSendCompleteTime => m_WaitForSendCompleteTime;

	public long PrevFrameSendTime => m_PrevFrameSendTime;

	public long SelTimeSpanNameId
	{
		get
		{
			return m_SelTimeSpanNameId;
		}
		set
		{
			m_SelTimeSpanNameId = value;
		}
	}

	public int SelTimeSpanThreadId
	{
		get
		{
			return m_SelTimeSpanThreadId;
		}
		set
		{
			m_SelTimeSpanThreadId = value;
		}
	}

	public Frame(int index)
	{
		m_Index = index;
	}

	public Frame(int index, long start_time)
	{
		m_Index = index;
		m_StartTime = start_time;
		m_EndTime = long.MaxValue;
	}

	public Frame(int index, long start_time, long end_time)
	{
		m_Index = index;
		m_StartTime = start_time;
		m_EndTime = end_time;
	}

	public void OnCustomStatStringRemapped(long old_string_id, long new_string_id)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == old_string_id)
				{
					CustomStat value = m_CustomStats[i];
					value.OnStringRemapped(old_string_id, new_string_id);
					m_CustomStats[i] = value;
				}
			}
		}
	}

	public void Finalise(long end_time, int time_span_count, int bytes_sent, long wait_for_send_compete_time, long prev_frame_send_time)
	{
		m_EndTime = end_time;
		m_TimeSpanCount = time_span_count;
		m_BytesSent = bytes_sent;
		m_WaitForSendCompleteTime = wait_for_send_compete_time;
		m_PrevFrameSendTime = prev_frame_send_time;
	}

	public void Read(BinaryReader binary_reader, int version)
	{
		m_StartTime = binary_reader.ReadInt64();
		m_EndTime = binary_reader.ReadInt64();
		m_TimeSpanCount = binary_reader.ReadInt32();
		m_BytesSent = binary_reader.ReadInt32();
		m_WaitForSendCompleteTime = binary_reader.ReadInt64();
		m_PrevFrameSendTime = binary_reader.ReadInt64();
		if (version < 23)
		{
			return;
		}
		int num = binary_reader.ReadInt32();
		if (num != 0)
		{
			m_CustomStats = new List<CustomStat>(num);
			for (int i = 0; i < num; i++)
			{
				CustomStat item = default(CustomStat);
				item.Read(binary_reader, version);
				m_CustomStats.Add(item);
			}
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_StartTime);
		binary_writer.Write(m_EndTime);
		binary_writer.Write(m_TimeSpanCount);
		binary_writer.Write(m_BytesSent);
		binary_writer.Write(m_WaitForSendCompleteTime);
		binary_writer.Write(m_PrevFrameSendTime);
		lock (m_CustomStats)
		{
			binary_writer.Write(m_CustomStats.Count);
			if (m_CustomStats == null)
			{
				return;
			}
			foreach (CustomStat customStat in m_CustomStats)
			{
				customStat.Write(binary_writer);
			}
		}
	}

	public void AddCustomStat(long name, long value_int64, double value_double, int count)
	{
		lock (m_CustomStats)
		{
			int count2 = m_CustomStats.Count;
			for (int i = 0; i < count2; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					m_CustomStats[i] = new CustomStat(name, m_CustomStats[i].ValueInt64 + value_int64, m_CustomStats[i].ValueDouble + value_double, m_CustomStats[i].AccValueInt64 + value_int64, m_CustomStats[i].AccValueDouble + value_double, m_CustomStats[i].Count + 1);
					return;
				}
			}
			m_CustomStats.Add(new CustomStat(name, value_int64, value_double, count));
		}
	}

	public long GetCustomStatValueInt64(long name)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					return m_CustomStats[i].ValueInt64;
				}
			}
		}
		return 0L;
	}

	public double GetCustomStatValueDouble(long name)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					return m_CustomStats[i].ValueDouble;
				}
			}
		}
		return 0.0;
	}

	public long GetCustomStatAccValueInt64(long name)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					return m_CustomStats[i].AccValueInt64;
				}
			}
		}
		return 0L;
	}

	public double GetCustomStatAccValueDouble(long name)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					return m_CustomStats[i].AccValueDouble;
				}
			}
		}
		return 0.0;
	}

	public long GetCustomStatCount(long name)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					return m_CustomStats[i].Count;
				}
			}
		}
		return 0L;
	}

	public void AddAccumulatedCustomStats(long name, long value_int64, double value_double)
	{
		lock (m_CustomStats)
		{
			int count = m_CustomStats.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_CustomStats[i].Name == name)
				{
					m_CustomStats[i] = new CustomStat(name, m_CustomStats[i].ValueInt64, m_CustomStats[i].ValueDouble, m_CustomStats[i].AccValueInt64 + value_int64, m_CustomStats[i].AccValueDouble + value_double, m_CustomStats[i].Count + 1);
					return;
				}
			}
		}
		m_CustomStats.Add(new CustomStat(name, 0L, 0.0, value_int64, value_double, 0L));
	}

	public void AddAccumulatedCustomStats(Frame frame)
	{
		lock (m_CustomStats)
		{
			foreach (CustomStat customStat in frame.m_CustomStats)
			{
				CustomStat item = new CustomStat(customStat.Name, 0L, 0.0, customStat.AccValueInt64, customStat.AccValueDouble, 0L);
				m_CustomStats.Add(item);
			}
		}
	}
}
