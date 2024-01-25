using System;
using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class RowTitlePanel : Control
{
	private Row m_RootRow;

	private Brush m_FillBrush = SystemBrushes.ButtonFace;

	private Pen m_HiPen = SystemPens.ButtonHighlight;

	private Pen m_LowPen = SystemPens.ButtonShadow;

	private bool m_MouseEnabled = true;

	private DragRowInfo m_DragRowInfo;

	private int m_ScrollY;

	private bool m_PadEmptyRows;

	private bool m_CanResizeRows;

	private bool m_CanAddRemoveRows;

	private ContextMenu m_ContextMenu = new ContextMenu();

	private MenuItem m_InsertMenuItem = new MenuItem("Insert Row");

	private MenuItem m_DeleteMenuItem = new MenuItem("Delete Row");

	private Row m_FirstSelRow;

	private HDataGrid HDataGrid => (HDataGrid)base.Parent;

	private int DefaultRowHeight => HDataGrid.DefaultRowHeight;

	public bool MouseEnabled
	{
		get
		{
			return m_MouseEnabled;
		}
		set
		{
			m_MouseEnabled = value;
		}
	}

	public int ScrollY
	{
		get
		{
			return m_ScrollY;
		}
		set
		{
			m_ScrollY = value;
		}
	}

	public bool PadEmptyRows
	{
		get
		{
			return m_PadEmptyRows;
		}
		set
		{
			m_PadEmptyRows = value;
		}
	}

	public bool CanResizeRows
	{
		get
		{
			return m_CanResizeRows;
		}
		set
		{
			m_CanResizeRows = value;
		}
	}

	public bool CanAddRemoveRows
	{
		get
		{
			return m_CanAddRemoveRows;
		}
		set
		{
			m_CanAddRemoveRows = value;
		}
	}

	public RowTitlePanel(Row root_row)
	{
		m_RootRow = root_row;
		DoubleBuffered = true;
		m_ContextMenu.MenuItems.Add(m_InsertMenuItem);
		m_ContextMenu.MenuItems.Add(m_DeleteMenuItem);
		m_InsertMenuItem.Click += InsertMenuItemClick;
		m_DeleteMenuItem.Click += DeleteMenuItemClick;
	}

	private void InsertMenuItemClick(object sender, EventArgs e)
	{
		Row firstRow = Utils.GetFirstRow(HDataGrid.SelectedRows);
		HDataGrid.InsertEmptyRowBefore(firstRow);
	}

	private void DeleteMenuItemClick(object sender, EventArgs e)
	{
		HDataGrid.DeleteRows(HDataGrid.SelectedRows);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(SystemBrushes.AppWorkspace, base.ClientRectangle);
		int i = -m_ScrollY;
		RowIterator rowIterator = new RowIterator(m_RootRow.ChildRows);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int num = ((current.Height == -1) ? DefaultRowHeight : current.Height);
			if (i + num >= 0)
			{
				Rectangle rect = new Rectangle(0, i, base.ClientSize.Width, num);
				bool invert = HDataGrid.RowSelected(current);
				Utils.DrawBevelRect(e.Graphics, rect, m_FillBrush, m_HiPen, m_LowPen, invert);
			}
			if (i > base.ClientSize.Height)
			{
				break;
			}
			i += num;
		}
		if (m_PadEmptyRows)
		{
			for (; i < base.ClientSize.Height; i += DefaultRowHeight)
			{
				Rectangle rect2 = new Rectangle(0, i, base.ClientSize.Width, DefaultRowHeight);
				Utils.DrawBevelRect(e.Graphics, rect2, m_FillBrush, m_HiPen, m_LowPen);
			}
		}
		base.OnPaint(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (m_MouseEnabled)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (m_CanResizeRows && m_DragRowInfo.m_Row != null)
				{
					base.Capture = true;
					m_DragRowInfo.m_Active = true;
					m_DragRowInfo.m_MouseStartY = e.Y;
					m_DragRowInfo.m_StartHeight = ((m_DragRowInfo.m_Row.Height == -1) ? DefaultRowHeight : m_DragRowInfo.m_Row.Height);
				}
				else
				{
					Row row = (m_FirstSelRow = GetRow(e.Y));
					base.Capture = true;
					if (row != null)
					{
						HDataGrid.SelectRow(row);
					}
				}
			}
			else if (e.Button == MouseButtons.Right && m_CanAddRemoveRows && !HDataGrid.ReadOnly)
			{
				Row row2 = GetRow(e.Y);
				if (row2 != null)
				{
					HDataGrid.SelectRow(row2);
				}
				m_ContextMenu.Show(this, e.Location);
			}
		}
		base.OnMouseDown(e);
	}

	private Row GetRow(int y)
	{
		int num = -m_ScrollY;
		RowIterator rowIterator = new RowIterator(m_RootRow.ChildRows);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int num2 = ((current.Height == -1) ? DefaultRowHeight : current.Height);
			num += num2;
			if (y < num)
			{
				return current;
			}
		}
		return null;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_MouseEnabled)
		{
			if (m_DragRowInfo.m_Active)
			{
				int num = e.Y - m_DragRowInfo.m_MouseStartY;
				if (num != 0)
				{
					m_DragRowInfo.m_Row.Height = Math.Max(m_DragRowInfo.m_StartHeight + num, 4);
					HDataGrid.Refresh();
				}
			}
			else if (m_FirstSelRow != null)
			{
				Row row = GetRow(e.Y);
				HDataGrid.SelectRowRange(m_FirstSelRow, row);
			}
			else if (m_CanResizeRows)
			{
				int num2 = 0;
				bool flag = false;
				RowIterator rowIterator = new RowIterator(m_RootRow.ChildRows);
				while (rowIterator.MoveNext())
				{
					Row current = rowIterator.Current;
					int num3 = ((current.Height == -1) ? DefaultRowHeight : current.Height);
					num2 += num3;
					if (Math.Abs(e.Y - num2) < 4)
					{
						m_DragRowInfo.m_Row = current;
						Cursor = Cursors.HSplit;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					Cursor = Cursors.Default;
					m_DragRowInfo.m_Row = null;
				}
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_FirstSelRow != null)
			{
				m_FirstSelRow = null;
				base.Capture = false;
			}
			if (m_DragRowInfo.m_Active)
			{
				m_DragRowInfo.m_Active = false;
				base.Capture = false;
			}
		}
		base.OnMouseUp(e);
	}

	protected override void OnResize(EventArgs e)
	{
		Refresh();
		base.OnResize(e);
	}
}
