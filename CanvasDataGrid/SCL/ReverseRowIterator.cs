using System.Collections.Generic;

namespace SCL;

internal class ReverseRowIterator
{
	private Row m_StartRow;

	private bool m_Started;

	private Row m_CurrentRow;

	private List<int> m_ChildIndexStack = new List<int>();

	private bool m_IncludeExpanded;

	private int CurrentChildIndex
	{
		get
		{
			return m_ChildIndexStack[m_ChildIndexStack.Count - 1];
		}
		set
		{
			m_ChildIndexStack[m_ChildIndexStack.Count - 1] = value;
		}
	}

	public Row Current => m_CurrentRow;

	public bool IncludeExpanded
	{
		get
		{
			return m_IncludeExpanded;
		}
		set
		{
			m_IncludeExpanded = value;
		}
	}

	public ReverseRowIterator(RowCollection rows)
		: this((rows.Count != 0) ? rows.DisplayRows[0] : null)
	{
	}

	public ReverseRowIterator(Row row)
	{
		if (row != null)
		{
			Row row2 = row;
			while (row2.Parent != null)
			{
				int item = row2.Parent.ChildRows.DisplayRows.IndexOf(row2);
				m_ChildIndexStack.Add(item);
				row2 = row2.Parent;
			}
			m_ChildIndexStack.Reverse();
		}
		m_StartRow = row;
	}

	public bool MoveNext()
	{
		if (!m_Started)
		{
			m_Started = true;
			if (m_StartRow == null)
			{
				return false;
			}
			m_CurrentRow = m_StartRow;
			return true;
		}
		while (m_CurrentRow != null)
		{
			if (CurrentChildIndex > 0)
			{
				int currentChildIndex = CurrentChildIndex - 1;
				CurrentChildIndex = currentChildIndex;
				m_CurrentRow = m_CurrentRow.Parent.ChildRows.DisplayRows[CurrentChildIndex];
				if (!m_CurrentRow.Hidden)
				{
					GotoLastChild();
					return true;
				}
			}
			if (m_CurrentRow.Parent.Parent == null)
			{
				m_CurrentRow = null;
				return false;
			}
			m_CurrentRow = m_CurrentRow.Parent;
			m_ChildIndexStack.RemoveAt(m_ChildIndexStack.Count - 1);
			if (m_CurrentRow != null)
			{
				return true;
			}
		}
		return m_CurrentRow != null;
	}

	private void GotoLastChild()
	{
		if (m_CurrentRow.Expanded)
		{
			int count = m_CurrentRow.ChildRows.Count;
			if (count != 0)
			{
				int num = count - 1;
				m_CurrentRow = m_CurrentRow.ChildRows.DisplayRows[num];
				m_ChildIndexStack.Add(num);
				GotoLastChild();
			}
		}
	}
}
