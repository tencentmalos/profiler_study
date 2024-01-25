using System;
using System.Collections.Generic;
using SCL.HDataGridArray;

namespace SCL;

public class Row
{
	internal delegate void RowRemovedHandler();

	private Row m_Parent;

	private RowCollection m_ChildRows;

	private Array<Cell> m_Cells = new Array<Cell>();

	private HDataGrid m_HDataGrid;

	private int m_Height = -1;

	private bool m_Expanded = true;

	private bool m_Hidden;

	private int m_StableSortIndex;

	public int Height
	{
		get
		{
			return m_Height;
		}
		set
		{
			m_Height = value;
		}
	}

	public RowCollection ChildRows
	{
		get
		{
			return m_ChildRows;
		}
		set
		{
			m_ChildRows = value;
			value.SetParent(this);
			foreach (Row item in value)
			{
				item.m_Parent = this;
			}
			if (this.RowRemoved != null)
			{
				this.RowRemoved();
			}
		}
	}

	public Row FirstChild
	{
		get
		{
			if (m_ChildRows.Count <= 0)
			{
				return null;
			}
			return m_ChildRows[0];
		}
	}

	public Row FirstDisplayedChild
	{
		get
		{
			if (m_ChildRows.Count <= 0)
			{
				return null;
			}
			return m_ChildRows.DisplayRows[0];
		}
	}

	public Row LastDisplayedChild
	{
		get
		{
			if (m_ChildRows.Count <= 0)
			{
				return null;
			}
			return m_ChildRows.DisplayRows[m_ChildRows.Count - 1];
		}
	}

	public Row Parent => m_Parent;

	public Array<Cell> Cells => m_Cells;

	public bool Expanded
	{
		get
		{
			return m_Expanded;
		}
		set
		{
			if (m_Expanded != value)
			{
				m_Expanded = value;
				if (HDataGrid != null)
				{
					HDataGrid.UpdateScrollSize();
				}
			}
		}
	}

	public int Depth
	{
		get
		{
			Row parent = m_Parent;
			int num = 0;
			while (parent != null)
			{
				num++;
				parent = parent.Parent;
			}
			return num;
		}
	}

	public bool Hidden
	{
		get
		{
			return m_Hidden;
		}
		set
		{
			m_Hidden = value;
		}
	}

	internal Row Root
	{
		get
		{
			Row row = this;
			while (row.Parent != null)
			{
				row = row.Parent;
			}
			return row;
		}
	}

	private HDataGrid HDataGrid
	{
		get
		{
			if (m_HDataGrid != null)
			{
				return m_HDataGrid;
			}
			if (Parent != null)
			{
				return Parent.HDataGrid;
			}
			return null;
		}
	}

	public int Index
	{
		get
		{
			if (m_Parent == null)
			{
				return -1;
			}
			return m_Parent.m_ChildRows.IndexOf(this);
		}
	}

	public int StableSortIndex
	{
		get
		{
			return m_StableSortIndex;
		}
		set
		{
			m_StableSortIndex = value;
		}
	}

	internal event RowRemovedHandler RowRemoved;

	internal Row(HDataGrid hdatagrid)
		: this()
	{
		m_HDataGrid = hdatagrid;
	}

	public Row()
	{
		m_ChildRows = new RowCollection(this);
	}

	private void AddCell(object obj)
	{
		if (obj is Cell value)
		{
			m_Cells.Add(value);
		}
		else
		{
			m_Cells.Add(new Cell(obj));
		}
	}

	public Row(object obj)
		: this()
	{
		AddCell(obj);
	}

	public Row(object obj1, object obj2)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
	}

	public Row(object obj1, object obj2, object obj3)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
	}

	public Row(object obj1, object obj2, object obj3, object obj4)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
		AddCell(obj8);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
		AddCell(obj8);
		AddCell(obj9);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9, object obj10)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
		AddCell(obj8);
		AddCell(obj9);
		AddCell(obj10);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9, object obj10, object obj11)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
		AddCell(obj8);
		AddCell(obj9);
		AddCell(obj10);
		AddCell(obj11);
	}

	public Row(object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9, object obj10, object obj11, object obj12)
		: this()
	{
		AddCell(obj1);
		AddCell(obj2);
		AddCell(obj3);
		AddCell(obj4);
		AddCell(obj5);
		AddCell(obj6);
		AddCell(obj7);
		AddCell(obj8);
		AddCell(obj9);
		AddCell(obj10);
		AddCell(obj11);
		AddCell(obj12);
	}

	public override string ToString()
	{
		if (m_Cells.Count == 0)
		{
			return "EmptyRow";
		}
		return m_Cells[0].ToString();
	}

	public Row Clone()
	{
		Row row = new Row();
		row.m_Height = m_Height;
		row.m_Expanded = m_Expanded;
		row.m_Hidden = m_Hidden;
		foreach (Cell cell in m_Cells)
		{
			ICloneable cloneable = cell;
			Cell value = ((cloneable == null) ? ((cell == null) ? null : new Cell("")) : ((Cell)cloneable.Clone()));
			row.m_Cells.Add(value);
		}
		row.ChildRows = Utils.Clone(m_ChildRows);
		return row;
	}

	public void InsertBefore(Row child_row, Row insert_row)
	{
		int index = m_ChildRows.IndexOf(child_row);
		m_ChildRows.Insert(index, insert_row);
		OnRowAdded(insert_row);
	}

	public void InsertAfter(Row child_row, Row insert_row)
	{
		int index = m_ChildRows.IndexOf(child_row) + 1;
		m_ChildRows.Insert(index, insert_row);
		OnRowAdded(insert_row);
	}

	public void InsertChildrenAtHead(List<Row> rows)
	{
		m_ChildRows.InsertRange(0, rows);
	}

	internal bool IsParentOf(Row row)
	{
		for (Row parent = row.Parent; parent != null; parent = parent.Parent)
		{
			if (parent == this)
			{
				return true;
			}
		}
		return false;
	}

	internal bool IsFirstDisplayedChild()
	{
		return m_Parent.m_ChildRows.DisplayRows[0] == this;
	}

	public void Sort(int col_index, bool reverse)
	{
		m_ChildRows.Sort(col_index, reverse);
		foreach (Row childRow in m_ChildRows)
		{
			childRow.Sort(col_index, reverse);
		}
	}

	internal void OnRowAdded(Row row)
	{
		row.m_Parent = this;
	}

	internal void OnRowRemoved(Row row)
	{
		row.m_Parent = null;
		if (this.RowRemoved != null)
		{
			this.RowRemoved();
		}
	}

	public object GetCellValue(int col)
	{
		if (col >= m_Cells.Count)
		{
			return null;
		}
		return m_Cells[col].Value;
	}

	public void ClearChildRows()
	{
		m_ChildRows.Clear();
	}
}
