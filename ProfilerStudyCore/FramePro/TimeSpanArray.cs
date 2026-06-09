using System;
using System.Collections.Generic;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class TimeSpanArray
{
	private class TimeBlock
	{
		public List<TimeSpan> m_TimeSpans = new List<TimeSpan>();
	}

	private long m_StartTime = long.MaxValue;

	private long m_BlockDuration;

	private List<TimeBlock> m_TimeBlocks = new List<TimeBlock>();

	public TimeSpanArray(long block_duration)
	{
		m_BlockDuration = block_duration;
	}

	private long AlignTimeToTimeBlock(long time)
	{
		return time / m_BlockDuration * m_BlockDuration;
	}

	public void Add(TimeSpan time_span)
	{
		long startTime = time_span.StartTime;
		if (m_StartTime == long.MaxValue)
		{
			m_StartTime = AlignTimeToTimeBlock(startTime);
		}
		int count = m_TimeBlocks.Count;
		int num = (int)((startTime >= m_StartTime) ? ((startTime - m_StartTime) / m_BlockDuration) : ((int)((startTime - m_StartTime + 1) / m_BlockDuration) - 1));
		if (num < 0)
		{
			int num2 = -num;
			List<TimeBlock> list = new List<TimeBlock>(num2);
			for (int i = 0; i < num2; i++)
			{
				list.Add(new TimeBlock());
			}
			m_TimeBlocks.InsertRange(0, list);
			m_StartTime = AlignTimeToTimeBlock(startTime);
			num = 0;
		}
		else if (num >= count)
		{
			int num3 = num + 1 - count;
			for (int j = 0; j < num3; j++)
			{
				m_TimeBlocks.Add(new TimeBlock());
			}
		}
		List<TimeSpan> timeSpans = m_TimeBlocks[num].m_TimeSpans;
		int count2 = timeSpans.Count;
		if (count2 == 0 || timeSpans[count2 - 1].EndTime <= startTime)
		{
			timeSpans.Add(time_span);
			return;
		}
		int num4 = 0;
		using (List<TimeSpan>.Enumerator enumerator = timeSpans.GetEnumerator())
		{
			while (enumerator.MoveNext() && enumerator.Current.StartTime <= startTime)
			{
				num4++;
			}
		}
		timeSpans.Insert(num4, time_span);
	}

	public void Remove(TimeSpan time_span, int count)
	{
		int num = (int)((time_span.StartTime - m_StartTime) / m_BlockDuration);
		if (num < 0 || num >= m_TimeBlocks.Count)
		{
			return;
		}
		List<TimeSpan> timeSpans = m_TimeBlocks[num].m_TimeSpans;
		int num2 = timeSpans.IndexOf(time_span);
		if (num2 == -1)
		{
			return;
		}
		int num3 = count;
		while (num3 != 0)
		{
			int num4 = Math.Min(num3, timeSpans.Count - num2);
			timeSpans.RemoveRange(num2, num4);
			num3 -= num4;
			num++;
			if (num != m_TimeBlocks.Count)
			{
				timeSpans = m_TimeBlocks[num].m_TimeSpans;
				num2 = 0;
				continue;
			}
			break;
		}
	}

	public TimeSpan Get_Legacy(long time)
	{
		int count = m_TimeBlocks.Count;
		if (count == 0)
		{
			return null;
		}
		int value = (int)((time - m_StartTime) / m_BlockDuration);
		value = Misc.Clamp(value, 0, count - 1);
		List<TimeSpan> timeSpans = m_TimeBlocks[value].m_TimeSpans;
		while ((timeSpans.Count == 0 || timeSpans[0].StartTime > time) && value > 0)
		{
			value--;
			timeSpans = m_TimeBlocks[value].m_TimeSpans;
		}
		foreach (TimeSpan item in timeSpans)
		{
			if (item.EndTime > time)
			{
				return item;
			}
		}
		TimeSpan timeSpan = timeSpans[timeSpans.Count - 1];
		if (time >= timeSpan.EndTime)
		{
			while (value < count - 1)
			{
				value++;
				List<TimeSpan> timeSpans2 = m_TimeBlocks[value].m_TimeSpans;
				if (timeSpans2.Count != 0)
				{
					timeSpan = timeSpans2[0];
					break;
				}
			}
		}
		return timeSpan;
	}

	public TimeSpan GetTimeSpanAtTime(long time)
	{
		int count = m_TimeBlocks.Count;
		if (count == 0)
		{
			return null;
		}
		int num = (int)((time - m_StartTime) / m_BlockDuration);
		if (num >= count)
		{
			return null;
		}
		if (num < 0)
		{
			num = 0;
		}
		TimeSpan timeSpan = null;
		List<TimeSpan> timeSpans = m_TimeBlocks[num].m_TimeSpans;
		if ((timeSpans.Count == 0 || timeSpans[0].StartTime > time) && num != 0)
		{
			List<TimeSpan> timeSpans2 = m_TimeBlocks[num - 1].m_TimeSpans;
			if (timeSpans2.Count != 0 && timeSpans2[timeSpans2.Count - 1].EndTime > time)
			{
				timeSpan = timeSpans2[timeSpans2.Count - 1];
			}
		}
		if (timeSpan == null)
		{
			foreach (TimeSpan item in timeSpans)
			{
				if (item.EndTime > time)
				{
					timeSpan = item;
					break;
				}
			}
		}
		if (timeSpan == null)
		{
			while (++num < m_TimeBlocks.Count)
			{
				List<TimeSpan> timeSpans3 = m_TimeBlocks[num].m_TimeSpans;
				if (timeSpans3.Count != 0)
				{
					timeSpan = timeSpans3[0];
					break;
				}
			}
		}
		if (timeSpan != null)
		{
			TimeSpan result = timeSpan;
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan);
			while (rootFirstTimeSpanIterator.MoveNext())
			{
				if (time >= rootFirstTimeSpanIterator.Current.StartTime && time < rootFirstTimeSpanIterator.Current.EndTime)
				{
					result = rootFirstTimeSpanIterator.Current;
				}
				if (rootFirstTimeSpanIterator.Current.StartTime > time)
				{
					break;
				}
			}
			return result;
		}
		return null;
	}
}
