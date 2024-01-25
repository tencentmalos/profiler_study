using System;
using System.IO;
using SCLCoreCLR;

namespace FramePro;

public class TimeSpanList
{
	private int m_ThreadId;

	private TimeSpan m_RootTimeSpan = new TimeSpan(int.MaxValue, long.MinValue, long.MaxValue);

	private TimeSpan m_CurrentTimeSpan;

	private TimeSpanArray m_TimeSpanRootArray;

	private long m_TimerFrequency;

	private long m_TotalTimeSpanCount;

	private ReadWriteLock m_Lock = new ReadWriteLock();

	public TimeSpan RootTimeSpan => m_RootTimeSpan;

	public long TotalTimeSpanCount => m_TotalTimeSpanCount;

	public ReadWriteLock Lock => m_Lock;

	public TimeSpanList(long timer_frequency, int thraed_id)
	{
		m_ThreadId = thraed_id;
		m_CurrentTimeSpan = m_RootTimeSpan;
		m_TimerFrequency = timer_frequency;
		m_TimeSpanRootArray = new TimeSpanArray(timer_frequency);
	}

	public void Read(BinaryReader reader, int version, ThreadJobContext context, TimeSpanInfoSet time_span_info_set, long file_size)
	{
		m_RootTimeSpan.Read(reader, version, context, time_span_info_set, file_size);
		for (TimeSpan timeSpan = m_RootTimeSpan.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			AddTimeSpanToRootArray(timeSpan);
		}
		if (version >= 13)
		{
			m_TotalTimeSpanCount = reader.ReadInt64();
		}
		else
		{
			m_TotalTimeSpanCount = CountTimeSpans(m_RootTimeSpan);
		}
	}

	private static long CountTimeSpans(TimeSpan time_span)
	{
		long num = 0L;
		TimeSpanIterator timeSpanIterator = new TimeSpanIterator(time_span);
		while (timeSpanIterator.MoveNext())
		{
			num++;
		}
		return num;
	}

	public void Write(BinaryWriter writer, ThreadJobContext context)
	{
		RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(m_RootTimeSpan);
		int num = 0;
		while (rootFirstTimeSpanIterator.MoveNext())
		{
			num++;
		}
		int count = 0;
		m_RootTimeSpan.Write(writer, context, ref count, num);
		writer.Write(m_TotalTimeSpanCount);
	}

	public TimeSpanList Clone(TimeSpanInfoSet time_span_info_set, long start_time, long end_time)
	{
		TimeSpanList timeSpanList = new TimeSpanList(m_TimerFrequency, m_ThreadId);
		TimeSpan timeSpan = GetTimeSpan_Legacy(start_time);
		if (timeSpan == null)
		{
			return timeSpanList;
		}
		while (timeSpan.Parent != m_RootTimeSpan)
		{
			timeSpan = timeSpan.Parent;
		}
		TimeSpan timeSpan2 = GetTimeSpan_Legacy(end_time);
		if (timeSpan2 != null)
		{
			while (timeSpan2.Parent != m_RootTimeSpan)
			{
				timeSpan2 = timeSpan2.Parent;
			}
			timeSpan2 = timeSpan2.Next;
		}
		RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan);
		while (rootFirstTimeSpanIterator.MoveNext() && rootFirstTimeSpanIterator.Current != timeSpan2)
		{
			if (rootFirstTimeSpanIterator.Current.IsTimeSpanEx)
			{
				timeSpanList.Add(((TimeSpanEx)rootFirstTimeSpanIterator.Current).Clone());
			}
			else
			{
				timeSpanList.Add(rootFirstTimeSpanIterator.Current.Clone());
			}
		}
		return timeSpanList;
	}

	private static bool TimeSpanContains(TimeSpan parent, TimeSpan child)
	{
		if (parent.StartTime != long.MinValue)
		{
			if (child.StartTime >= parent.StartTime)
			{
				return child.EndTime <= parent.EndTime;
			}
			return false;
		}
		return true;
	}

	private static bool TimeSpanContainsButNotExact(TimeSpan parent, TimeSpan child)
	{
		if (TimeSpanContains(parent, child))
		{
			return !ExactMatch(parent, child);
		}
		return false;
	}

	private static bool ExactMatch(TimeSpan parent, TimeSpan child)
	{
		if (child.StartTime == parent.StartTime)
		{
			return child.EndTime == parent.EndTime;
		}
		return false;
	}

	public void Add(TimeSpan time_span)
	{
		Add(time_span, null);
	}

	public void Add(TimeSpan time_span, GetTimerNameDelegate get_timer_name)
	{
		AddTimeSpanToHeirachy(time_span, get_timer_name);
		if (time_span.Parent == m_RootTimeSpan)
		{
			AddTimeSpanToRootArray(time_span);
		}
		m_TotalTimeSpanCount++;
	}

	private void CheckTimeSpanPosition(TimeSpan time_span)
	{
		_ = time_span.Prev;
		_ = time_span.Next;
		for (TimeSpan timeSpan = time_span.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			_ = timeSpan.StartTime;
		}
	}

	private void AddTimeSpanToHeirachy(TimeSpan time_span, GetTimerNameDelegate get_timer_name)
	{
		TimeSpan timeSpan = m_CurrentTimeSpan;
		TimeSpan timeSpan2 = m_CurrentTimeSpan.Children;
		while (!TimeSpanContains(timeSpan, time_span) || ExactMatch(timeSpan, time_span))
		{
			timeSpan2 = timeSpan;
			timeSpan = timeSpan.Parent;
		}
		if (timeSpan.Children == null)
		{
			timeSpan.Children = time_span;
			time_span.Parent = timeSpan;
			m_CurrentTimeSpan = time_span;
			return;
		}
		if (timeSpan == m_RootTimeSpan && Math.Abs(timeSpan2.StartTime - time_span.StartTime) > m_TimerFrequency)
		{
			TimeSpan timeSpan3 = m_TimeSpanRootArray.Get_Legacy(time_span.StartTime);
			if (timeSpan3 != null)
			{
				timeSpan2 = timeSpan3;
			}
		}
		while (timeSpan2 != null && time_span.StartTime <= timeSpan2.StartTime && !TimeSpanContainsButNotExact(timeSpan2, time_span))
		{
			timeSpan2 = timeSpan2.Prev;
		}
		while (timeSpan2 != null)
		{
			if (TimeSpanContainsButNotExact(timeSpan2, time_span))
			{
				timeSpan = timeSpan2;
				TimeSpan children = timeSpan2.Children;
				timeSpan2 = ((children == null || (time_span.StartTime <= children.StartTime && !TimeSpanContainsButNotExact(children, time_span))) ? null : children);
				continue;
			}
			TimeSpan next = timeSpan2.Next;
			if (next == null || next.StartTime > time_span.StartTime || TimeSpanContains(time_span, next))
			{
				break;
			}
			timeSpan2 = next;
		}
		time_span.Parent = timeSpan;
		if (timeSpan2 != null)
		{
			time_span.Next = timeSpan2.Next;
			if (timeSpan2.Next != null)
			{
				timeSpan2.Next.Prev = time_span;
			}
			timeSpan2.Next = time_span;
			time_span.Prev = timeSpan2;
		}
		else
		{
			time_span.Next = timeSpan.Children;
			if (timeSpan.Children != null)
			{
				timeSpan.Children.Prev = time_span;
			}
			timeSpan.Children = time_span;
		}
		TimeSpan next2 = time_span.Next;
		if (next2 != null && TimeSpanContains(time_span, next2))
		{
			TimeSpan time_span2 = next2;
			int num = 1;
			next2.Parent = time_span;
			time_span.Children = next2;
			next2.Prev = null;
			TimeSpan next3 = next2.Next;
			while (next3 != null && TimeSpanContains(time_span, next3))
			{
				next3.Parent = time_span;
				next3 = next3.Next;
				num++;
			}
			time_span.Next = next3;
			if (next3 != null)
			{
				next3.Prev.Next = null;
				next3.Prev = time_span;
			}
			if (time_span.Parent == m_RootTimeSpan)
			{
				m_TimeSpanRootArray.Remove(time_span2, num);
			}
		}
		m_CurrentTimeSpan = time_span;
	}

	private void AddTimeSpanToRootArray(TimeSpan time_span)
	{
		m_TimeSpanRootArray.Add(time_span);
	}

	private bool ChildContainsTimeSpan(TimeSpan parent, TimeSpan time_span)
	{
		if (time_span.StartTime == time_span.EndTime)
		{
			return false;
		}
		for (TimeSpan timeSpan = parent.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			if (TimeSpanContains(timeSpan, time_span) && !ExactMatch(timeSpan, time_span))
			{
				return true;
			}
		}
		return false;
	}

	private int SortTimeSpans(TimeSpan time_span1, TimeSpan time_span2)
	{
		if (time_span1.StartTime < time_span2.StartTime)
		{
			return -1;
		}
		if (time_span1.StartTime == time_span2.StartTime && time_span1.Duration > time_span2.Duration)
		{
			return -1;
		}
		return 1;
	}

	public TimeSpan GetTimeSpanAtTime(long time)
	{
		return m_TimeSpanRootArray.GetTimeSpanAtTime(time);
	}

	public TimeSpan GetTimeSpan_Legacy(long time)
	{
		return m_TimeSpanRootArray.Get_Legacy(time);
	}
}
