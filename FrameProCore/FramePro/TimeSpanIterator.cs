namespace FramePro;

public class TimeSpanIterator
{
	private TimeSpan m_Root;

	private TimeSpan m_Current;

	private int m_Depth;

	public TimeSpan Current => m_Current;

	public int Depth => m_Depth;

	public TimeSpanIterator(TimeSpan time_span)
	{
		m_Root = time_span;
	}

	private void MoveToFirstChild()
	{
		while (m_Current.Children != null)
		{
			m_Current = m_Current.Children;
			m_Depth++;
		}
	}

	public bool MoveNext()
	{
		if (m_Current == null)
		{
			if (m_Root == null)
			{
				return false;
			}
			m_Current = m_Root;
			MoveToFirstChild();
			return true;
		}
		if (m_Current.Next != null)
		{
			m_Current = m_Current.Next;
			MoveToFirstChild();
			return true;
		}
		if (m_Current.Parent != m_Root.Parent)
		{
			m_Current = m_Current.Parent;
			m_Depth--;
			return true;
		}
		return false;
	}
}
