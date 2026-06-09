namespace ProfilerStudy;

public class RootFirstTimeSpanIterator
{
	private TimeSpan m_RootTimeSpan;

	private TimeSpan m_FirstTimeSpan;

	private TimeSpan m_Current;

	public TimeSpan Current => m_Current;

	public RootFirstTimeSpanIterator(TimeSpan time_span)
		: this(time_span, is_root: false)
	{
	}

	public RootFirstTimeSpanIterator(TimeSpan time_span, bool is_root)
	{
		m_RootTimeSpan = (is_root ? time_span : null);
		m_FirstTimeSpan = time_span;
	}

	public bool MoveNext()
	{
		if (m_Current == null)
		{
			m_Current = m_FirstTimeSpan;
			return m_Current != null;
		}
		if (m_Current.Children != null)
		{
			m_Current = m_Current.Children;
			return true;
		}
		while (m_Current.Next == null)
		{
			if (m_Current == m_RootTimeSpan)
			{
				return false;
			}
			m_Current = m_Current.Parent;
			if (m_Current == null || m_Current.Parent == null)
			{
				return false;
			}
			if (m_Current == m_RootTimeSpan)
			{
				return false;
			}
		}
		if (m_Current.Next != null)
		{
			if (m_Current == m_RootTimeSpan)
			{
				return false;
			}
			m_Current = m_Current.Next;
			return true;
		}
		return false;
	}

	public bool MovePrev()
	{
		if (m_Current == null)
		{
			m_Current = m_FirstTimeSpan;
			return m_Current != null;
		}
		if (m_Current == null)
		{
			return false;
		}
		if (m_Current.Prev != null)
		{
			m_Current = m_Current.Prev;
			MoveToLastChild();
			return true;
		}
		if (m_Current.Parent != null)
		{
			m_Current = m_Current.Parent;
			return m_Current.Parent != null;
		}
		return false;
	}

	private void MoveToLastChild()
	{
		while (m_Current.ChildCount != 0)
		{
			m_Current = m_Current.Children;
			while (m_Current.Next != null)
			{
				m_Current = m_Current.Next;
			}
		}
	}
}
