using System.Collections.Generic;

namespace ProfilerStudy;

internal class WaitEventIterator
{
	public enum JumpMode
	{
		LessThanOrEqual,
		GreaterThanOrEqual
	}

	private class ThreadIterator
	{
		public TimeArray<WaitEvent> m_WaitEvents;

		public TimeArray<WaitEvent>.Index m_Index;
	}

	private Dictionary<int, ThreadIterator> m_Iterators = new Dictionary<int, ThreadIterator>();

	private WaitEvent m_CurrentWaitEvent;

	public WaitEvent Current => m_CurrentWaitEvent;

	public bool Done => m_CurrentWaitEvent == null;

	public void Initialise(Dictionary<int, TimeArray<WaitEvent>> wait_events)
	{
		foreach (int key in wait_events.Keys)
		{
			if (!m_Iterators.ContainsKey(key))
			{
				ThreadIterator threadIterator = new ThreadIterator();
				threadIterator.m_WaitEvents = wait_events[key];
				m_Iterators[key] = threadIterator;
			}
			m_Iterators[key].m_Index = TimeArray<WaitEvent>.InvalidIndex;
		}
	}

	public void JumpTo(long time, JumpMode mode)
	{
		m_CurrentWaitEvent = null;
		foreach (int key in m_Iterators.Keys)
		{
			ThreadIterator threadIterator = m_Iterators[key];
			TimeArray<WaitEvent> waitEvents = threadIterator.m_WaitEvents;
			TimeArray<WaitEvent>.Index index = waitEvents.GetIndex(time);
			if (mode == JumpMode.GreaterThanOrEqual && waitEvents[index].Time < time)
			{
				index = waitEvents.MoveNext(index);
			}
			threadIterator.m_Index = index;
		}
		switch (mode)
		{
		case JumpMode.LessThanOrEqual:
			MovePrev();
			break;
		case JumpMode.GreaterThanOrEqual:
			MoveNext();
			break;
		}
	}

	public void MovePrev()
	{
		WaitEvent waitEvent = null;
		foreach (int key in m_Iterators.Keys)
		{
			ThreadIterator threadIterator = m_Iterators[key];
			if (threadIterator.m_Index.IsValid)
			{
				WaitEvent waitEvent2 = threadIterator.m_WaitEvents[threadIterator.m_Index];
				if (waitEvent == null || waitEvent2.Time > waitEvent.Time)
				{
					waitEvent = waitEvent2;
				}
			}
		}
		m_CurrentWaitEvent = waitEvent;
		if (m_CurrentWaitEvent != null)
		{
			int threadId = m_CurrentWaitEvent.ThreadId;
			ThreadIterator threadIterator2 = m_Iterators[threadId];
			threadIterator2.m_Index = threadIterator2.m_WaitEvents.MovePrev(threadIterator2.m_Index);
		}
	}

	public void MoveNext()
	{
		WaitEvent waitEvent = null;
		foreach (int key in m_Iterators.Keys)
		{
			ThreadIterator threadIterator = m_Iterators[key];
			if (threadIterator.m_Index.IsValid)
			{
				WaitEvent waitEvent2 = threadIterator.m_WaitEvents[threadIterator.m_Index];
				if (waitEvent == null || waitEvent2.Time < waitEvent.Time)
				{
					waitEvent = waitEvent2;
				}
			}
		}
		m_CurrentWaitEvent = waitEvent;
		if (m_CurrentWaitEvent != null)
		{
			int threadId = m_CurrentWaitEvent.ThreadId;
			ThreadIterator threadIterator2 = m_Iterators[threadId];
			threadIterator2.m_Index = threadIterator2.m_WaitEvents.MoveNext(threadIterator2.m_Index);
		}
	}
}
