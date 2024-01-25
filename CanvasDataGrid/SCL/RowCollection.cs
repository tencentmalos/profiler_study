using System;
using System.Collections;
using System.Collections.Generic;

namespace SCL;

public class RowCollection : ICollection<Row>, IEnumerable<Row>, IEnumerable
{
	private List<Row> m_Rows = new List<Row>();

	private List<Row> m_DisplayRows = new List<Row>();

	private Row m_ParentRow;

	public int Count => m_Rows.Count;

	public bool IsReadOnly => false;

	public List<Row> DisplayRows => m_DisplayRows;

	public Row this[int index]
	{
		get
		{
			return m_Rows[index];
		}
		set
		{
			int index2 = m_Rows.IndexOf(m_Rows[index]);
			m_Rows[index2] = value;
			m_Rows[index] = value;
			m_ParentRow.OnRowAdded(value);
		}
	}

	public RowCollection()
	{
		m_ParentRow = new Row();
	}

	internal RowCollection(Row parent_row)
	{
		m_ParentRow = parent_row;
	}

	public RowCollection(int capacity)
	{
		m_Rows = new List<Row>(capacity);
		m_DisplayRows = new List<Row>(capacity);
	}

	public RowCollection(RowCollection other)
	{
		m_Rows = new List<Row>(other.m_Rows);
		m_DisplayRows = new List<Row>(other.m_DisplayRows);
	}

	internal void SetParent(Row value)
	{
		m_ParentRow = value;
	}

	private void AddToRowsList(Row row)
	{
		row.StableSortIndex = m_Rows.Count;
		m_Rows.Add(row);
	}

	public void Add(Row row)
	{
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		if (m_ParentRow != null)
		{
			m_ParentRow.OnRowAdded(row);
		}
	}

	public Row Add(object cell)
	{
		Row row = new Row(cell);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2)
	{
		Row row = new Row(cell1, cell2);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3)
	{
		Row row = new Row(cell1, cell2, cell3);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4)
	{
		Row row = new Row(cell1, cell2, cell3, cell4);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7, object cell8)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7, cell8);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7, object cell8, object cell9)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7, cell8, cell9);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7, object cell8, object cell9, object cell10)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7, cell8, cell9, cell10);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7, object cell8, object cell9, object cell10, object cell11)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7, cell8, cell9, cell10, cell11);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public Row Add(object cell1, object cell2, object cell3, object cell4, object cell5, object cell6, object cell7, object cell8, object cell9, object cell10, object cell11, object cell12)
	{
		Row row = new Row(cell1, cell2, cell3, cell4, cell5, cell6, cell7, cell8, cell9, cell10, cell11, cell12);
		AddToRowsList(row);
		m_DisplayRows.Add(row);
		m_ParentRow.OnRowAdded(row);
		return row;
	}

	public void Clear()
	{
		if (m_ParentRow != null)
		{
			foreach (Row row in m_Rows)
			{
				m_ParentRow.OnRowRemoved(row);
			}
		}
		m_Rows.Clear();
		m_DisplayRows.Clear();
	}

	public bool Contains(Row item)
	{
		return m_Rows.Contains(item);
	}

	public void CopyTo(Row[] array, int arrayIndex)
	{
		for (int i = 0; i < m_Rows.Count; i++)
		{
			array[i + arrayIndex] = m_Rows[i];
		}
	}

	public bool Remove(Row item)
	{
		if (m_Rows.Remove(item))
		{
			m_Rows.Remove(item);
			m_DisplayRows.Remove(item);
			m_ParentRow.OnRowRemoved(item);
			return true;
		}
		return false;
	}

	public IEnumerator<Row> GetEnumerator()
	{
		return m_Rows.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return m_Rows.GetEnumerator();
	}

	public int IndexOf(Row row)
	{
		return m_Rows.IndexOf(row);
	}

	internal void InsertDisplayRow(int index, Row row)
	{
		int index2 = ((index == m_DisplayRows.Count) ? m_DisplayRows.Count : m_Rows.IndexOf(m_DisplayRows[index]));
		m_Rows.Insert(index2, row);
		m_DisplayRows.Insert(index, row);
		m_ParentRow.OnRowAdded(row);
	}

	public void Insert(int index, Row row)
	{
		int index2 = ((index == m_Rows.Count) ? m_Rows.Count : m_DisplayRows.IndexOf(m_Rows[index]));
		m_DisplayRows.Insert(index2, row);
		m_Rows.Insert(index, row);
		m_ParentRow.OnRowAdded(row);
	}

	public void InsertRange(int index, List<Row> rows)
	{
		int index2 = ((index == m_Rows.Count) ? m_Rows.Count : m_DisplayRows.IndexOf(m_Rows[index]));
		m_DisplayRows.InsertRange(index2, rows);
		m_Rows.InsertRange(index, rows);
		foreach (Row row in rows)
		{
			m_ParentRow.OnRowAdded(row);
		}
	}

	private static int DefaultColumnComparer(object item_a, object item_b)
	{
		int result = 0;
		if (item_a != null || (item_b != null && item_a.GetType() == item_b.GetType() && !(item_a is bool)))
		{
			if (item_a is double num && item_b is double value)
			{
				result = num.CompareTo(value);
			}
			else if (item_a is int num2 && item_b is int value2)
			{
				result = num2.CompareTo(value2);
			}
			else if (item_a is long num3 && item_b is long value3)
			{
				result = num3.CompareTo(value3);
			}
			else if (item_a is ulong num4 && item_b is ulong value4)
			{
				result = num4.CompareTo(value4);
			}
			else if (item_a is uint num5 && item_b is uint value5)
			{
				result = num5.CompareTo(value5);
			}
			else if (item_a is float num6 && item_b is float value6)
			{
				result = num6.CompareTo(value6);
			}
			else if (item_a is short num7 && item_b is short value7)
			{
				result = num7.CompareTo(value7);
			}
			else if (item_a is IComparable comparable)
			{
				result = comparable.CompareTo(item_b);
			}
		}
		return result;
	}

	public void Sort(int column_index, bool reverse, ColumnComparer comparer)
	{
		if (comparer == null)
		{
			comparer = DefaultColumnComparer;
		}
		RowComparer comparer2 = new RowComparer(column_index, reverse, comparer);
		m_DisplayRows.Sort(comparer2);
		foreach (Row row in m_Rows)
		{
			row.ChildRows.Sort(column_index, reverse);
		}
	}

	public void Sort(int column_index, bool reverse)
	{
		Sort(column_index, reverse, null);
	}

	public void Reverse()
	{
		m_Rows.Reverse();
	}

	public void RemoveAt(int index)
	{
		int index2 = m_DisplayRows.IndexOf(m_Rows[index]);
		m_DisplayRows.RemoveAt(index2);
		m_Rows.RemoveAt(index);
	}
}
