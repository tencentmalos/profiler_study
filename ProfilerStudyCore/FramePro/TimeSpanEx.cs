using System.Collections.Generic;
using System.IO;
using SCLCoreCLR;

namespace ProfilerStudy;

public class TimeSpanEx : TimeSpan
{
	private List<HiResTimer> m_HiResTimers;

	private Array<TimeSpanCustomStat> m_CustomStats;

	public static SetCustomStatUnitDelegate_Depreciated m_SetCustomStatUnitDelegate_Depreciated;

	public ICollection<HiResTimer> HiResTimers => m_HiResTimers;

	public Array<TimeSpanCustomStat> CustomStats => m_CustomStats;

	public TimeSpanEx()
	{
	}

	public TimeSpanEx(long start_time, long end_time, List<HiResTimer> hires_timers)
		: base(TimeSpanInfo.InvalidInfoId, start_time, end_time, ex_time_span: true)
	{
		m_HiResTimers = hires_timers;
	}

	public TimeSpanEx(int time_span_info_id, long start_time, long end_time, Array<TimeSpanCustomStat> custom_stats)
		: base(time_span_info_id, start_time, end_time, ex_time_span: true)
	{
		m_CustomStats = new Array<TimeSpanCustomStat>(custom_stats);
	}

	public override void Read(BinaryReader binary_reader, int version, ThreadJobContext context, TimeSpanInfoSet time_span_info_set, long file_size)
	{
		base.Read(binary_reader, version, context, time_span_info_set, file_size);
		int num = binary_reader.ReadInt32();
		if (num != -1)
		{
			m_HiResTimers = new List<HiResTimer>();
			for (int i = 0; i < num; i++)
			{
				HiResTimer item = default(HiResTimer);
				item.m_Name = binary_reader.ReadInt64();
				item.m_Duration = binary_reader.ReadInt64();
				item.m_Count = binary_reader.ReadInt64();
				m_HiResTimers.Add(item);
			}
		}
		else
		{
			m_HiResTimers = null;
		}
		int num2 = binary_reader.ReadInt32();
		if (num2 != -1)
		{
			m_CustomStats = new Array<TimeSpanCustomStat>();
			for (int j = 0; j < num2; j++)
			{
				TimeSpanCustomStat timeSpanCustomStat = new TimeSpanCustomStat();
				timeSpanCustomStat.m_Name = binary_reader.ReadInt64();
				timeSpanCustomStat.m_ValueInt64 = binary_reader.ReadInt64();
				timeSpanCustomStat.m_ValueDouble = binary_reader.ReadDouble();
				if (version < 40)
				{
					long unit = binary_reader.ReadInt64();
					m_SetCustomStatUnitDelegate_Depreciated(base.TimeSpanInfoId, unit);
				}
				m_CustomStats.Add(timeSpanCustomStat);
			}
		}
		else
		{
			m_CustomStats = null;
		}
	}

	public override void Write(BinaryWriter binary_writer, ThreadJobContext context, ref int count, int max)
	{
		base.Write(binary_writer, context, ref count, max);
		int num = ((m_HiResTimers != null) ? m_HiResTimers.Count : (-1));
		binary_writer.Write(num);
		if (num != -1)
		{
			foreach (HiResTimer hiResTimer in m_HiResTimers)
			{
				binary_writer.Write(hiResTimer.m_Name);
				binary_writer.Write(hiResTimer.m_Duration);
				binary_writer.Write(hiResTimer.m_Count);
			}
		}
		int num2 = ((m_CustomStats != null) ? m_CustomStats.Count : (-1));
		binary_writer.Write(num2);
		if (num2 == -1)
		{
			return;
		}
		foreach (TimeSpanCustomStat customStat in m_CustomStats)
		{
			binary_writer.Write(customStat.m_Name);
			binary_writer.Write(customStat.m_ValueInt64);
			binary_writer.Write(customStat.m_ValueDouble);
		}
	}

	public new TimeSpanEx Clone()
	{
		TimeSpanEx timeSpanEx = new TimeSpanEx();
		timeSpanEx.Copy(this);
		if (m_HiResTimers != null && m_HiResTimers.Count != 0)
		{
			timeSpanEx.m_HiResTimers = new List<HiResTimer>();
			foreach (HiResTimer hiResTimer in m_HiResTimers)
			{
				timeSpanEx.m_HiResTimers.Add(hiResTimer);
			}
		}
		if (m_CustomStats != null && m_CustomStats.Count != 0)
		{
			timeSpanEx.m_CustomStats = new Array<TimeSpanCustomStat>();
			foreach (TimeSpanCustomStat customStat in m_CustomStats)
			{
				timeSpanEx.m_CustomStats.Add(customStat.Clone());
			}
		}
		return timeSpanEx;
	}
}
