using System.Collections.Generic;

namespace SCL;

internal class RowIterator
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

	public RowIterator(RowCollection rows)
		: this((rows.Count != 0) ? rows.DisplayRows[0] : null)
	{
	}

	public RowIterator(Row row)
	{
		if (row != null)
		{
			Row row2 = row;
			while (row2.Parent != null)
			{
				int num = row2.Parent.ChildRows.DisplayRows.IndexOf(row2);
				m_ChildIndexStack.Add(num + 1);
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
			m_ChildIndexStack.Add(0);
			return true;
		}
		while (m_CurrentRow != null)
		{
			if (CurrentChildIndex < m_CurrentRow.ChildRows.Count)
			{
				m_CurrentRow = m_CurrentRow.ChildRows.DisplayRows[CurrentChildIndex];
				int currentChildIndex = CurrentChildIndex + 1;
				CurrentChildIndex = currentChildIndex;
				m_ChildIndexStack.Add(0);
				if (!m_CurrentRow.Hidden && (m_IncludeExpanded || m_CurrentRow.Parent.Expanded))
				{
					return true;
				}
			}
			m_CurrentRow = m_CurrentRow.Parent;
			m_ChildIndexStack.RemoveAt(m_ChildIndexStack.Count - 1);
			if (m_ChildIndexStack.Count == 0)
			{
				m_CurrentRow = null;
			}
		}
		return m_CurrentRow != null;
	}
}
