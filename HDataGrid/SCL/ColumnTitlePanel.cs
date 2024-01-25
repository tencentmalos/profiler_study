using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class ColumnTitlePanel : Control
{
	private List<Column> m_Columns;

	private int m_RowTitleWidth;

	private Brush m_FillBrush = SystemBrushes.ButtonFace;

	private Pen m_HiPen = SystemPens.ButtonHighlight;

	private Pen m_LowPen = SystemPens.ButtonShadow;

	private bool m_MouseEnabled = true;

	private bool m_DraggingColumn;

	private int m_LastMouseX;

	private Column m_ColumnBeingResized;

	private int m_ScrollX;

	private bool m_CanSortByColumn;

	private bool m_CanShowHideColumns;

	private ContextMenuStrip m_ContextMenu = new ContextMenuStrip();

	private float m_DPIScale;

	private bool HasFillColumn
	{
		get
		{
			foreach (Column column in m_Columns)
			{
				if (column.Visible && column.WidthMode == Column.EWidthMode.Fill)
				{
					return true;
				}
			}
			return false;
		}
	}

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

	public int RowTitleWidth
	{
		get
		{
			return m_RowTitleWidth;
		}
		set
		{
			m_RowTitleWidth = value;
		}
	}

	public int ScrollX
	{
		get
		{
			return m_ScrollX;
		}
		set
		{
			m_ScrollX = value;
		}
	}

	public bool DraggingColumn => m_DraggingColumn;

	public bool CanSortByColumn
	{
		get
		{
			return m_CanSortByColumn;
		}
		set
		{
			m_CanSortByColumn = value;
		}
	}

	public bool CanShowHideColumns
	{
		get
		{
			return m_CanShowHideColumns;
		}
		set
		{
			m_CanShowHideColumns = value;
		}
	}

	private int VisibleColumnCount
	{
		get
		{
			int num = 0;
			foreach (Column column in m_Columns)
			{
				if (column.Visible)
				{
					num++;
				}
			}
			return num;
		}
	}

	private bool ContextMenuNeedsUpdating
	{
		get
		{
			if (m_ContextMenu.Items.Count != m_Columns.Count)
			{
				return true;
			}
			for (int i = 0; i < m_Columns.Count; i++)
			{
				if (m_ContextMenu.Items[i].Text != m_Columns[i].Name)
				{
					return true;
				}
			}
			return false;
		}
	}

	public event ColumnVisibilityChangedHandler ColumnVisibilityChanged;

	public ColumnTitlePanel(List<Column> columns, float dpi_scale)
	{
		m_Columns = columns;
		DoubleBuffered = true;
		m_DPIScale = dpi_scale;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (m_RowTitleWidth != 0)
		{
			Rectangle rect = new Rectangle(0, 0, m_RowTitleWidth, base.ClientSize.Height);
			Utils.DrawBevelRect(e.Graphics, rect, m_FillBrush, m_HiPen, m_LowPen);
		}
		int num = m_RowTitleWidth - m_ScrollX;
		foreach (Column column in m_Columns)
		{
			if (!column.Visible)
			{
				continue;
			}
			int num2 = column.Width;
			Rectangle rect2 = new Rectangle(num, 0, num2, base.ClientSize.Height);
			if (rect2.Right > m_RowTitleWidth)
			{
				Rectangle clip = new Rectangle(Math.Max(rect2.Left, m_RowTitleWidth), rect2.Top, rect2.Width, rect2.Height);
				e.Graphics.SetClip(clip);
				Utils.DrawBevelRect(e.Graphics, rect2, m_FillBrush, m_HiPen, m_LowPen);
				SizeF sizeF = e.Graphics.MeasureString(column.Name, SystemFonts.DefaultFont);
				Point point = new Point(rect2.X + rect2.Height / 2, rect2.Y + (rect2.Height - (int)sizeF.Height) / 2);
				e.Graphics.DrawString(column.Name, Font, SystemBrushes.ControlText, point);
				if (m_CanSortByColumn && column.SortMode != 0)
				{
					DrawSortArrow(e.Graphics, rect2, column);
				}
			}
			num += num2;
		}
		e.Graphics.SetClip(base.ClientRectangle);
		if (num < base.Width)
		{
			Utils.DrawBevelRect(e.Graphics, new Rectangle(num, 0, base.Width - num, base.ClientSize.Height), m_FillBrush, m_HiPen, m_LowPen);
		}
		base.OnPaint(e);
	}

	private void DrawSortArrow(Graphics graphics, Rectangle rect, Column column)
	{
		Point point = new Point(rect.X + rect.Width - rect.Height, rect.Y + rect.Height / 2);
		int num = 12;
		int num2 = 7;
		int num3 = ((column.SortMode != Column.ESortMode.Decreasing) ? 1 : (-1));
		Point[] points = new Point[3]
		{
			new Point(point.X, point.Y - num3 * num2 / 2),
			new Point(point.X + num / 2, point.Y + num3 * num2 / 2),
			new Point(point.X - num / 2, point.Y + num3 * num2 / 2)
		};
		graphics.FillPolygon(SystemBrushes.ControlDark, points);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_MouseEnabled)
		{
			if (m_DraggingColumn)
			{
				int num = e.X - m_LastMouseX;
				m_LastMouseX = e.X;
				((HDataGrid)base.Parent).ResizeColumn(m_ColumnBeingResized, m_ColumnBeingResized.Width + num);
			}
			else
			{
				int num2 = m_RowTitleWidth;
				bool flag = false;
				int num3 = m_Columns.Count;
				if (HasFillColumn)
				{
					num3--;
				}
				for (int i = 0; i < num3; i++)
				{
					if (m_Columns[i].Visible)
					{
						num2 += m_Columns[i].Width;
						if (Math.Abs(e.X - num2) < ScaleDPI(4))
						{
							m_ColumnBeingResized = m_Columns[i];
							Cursor = Cursors.VSplit;
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					Cursor = Cursors.Default;
					m_ColumnBeingResized = null;
				}
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (m_MouseEnabled)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (m_ColumnBeingResized != null)
				{
					base.Capture = true;
					m_DraggingColumn = true;
					m_LastMouseX = e.X;
				}
				else
				{
					Column column = GetColumn(e.X);
					if (column != null && m_CanSortByColumn && column.Sortable)
					{
						if (column.SortMode == Column.ESortMode.Increasing)
						{
							column.SortMode = Column.ESortMode.Decreasing;
						}
						else
						{
							column.SortMode = Column.ESortMode.Increasing;
						}
						((HDataGrid)base.Parent).Sort(column);
						base.Parent.Refresh();
					}
				}
			}
			else if (e.Button == MouseButtons.Right && m_CanShowHideColumns)
			{
				UpdateContextMenu();
				m_ContextMenu.Show(Cursor.Position);
			}
		}
		base.OnMouseDown(e);
	}

	private Column GetColumn(int x)
	{
		if (x < m_RowTitleWidth)
		{
			return null;
		}
		x -= m_RowTitleWidth;
		int num = 0;
		foreach (Column column in m_Columns)
		{
			if (column.Visible)
			{
				num += column.Width;
				if (num > x)
				{
					return column;
				}
			}
		}
		if (m_Columns.Count != 0)
		{
			int num2 = m_Columns.Count - 1;
			while (num2 > 0 && !m_Columns[num2].Visible)
			{
				num2--;
			}
			if (num2 < 0)
			{
				return null;
			}
			return m_Columns[num2];
		}
		return null;
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && m_DraggingColumn)
		{
			m_DraggingColumn = false;
			base.Capture = false;
		}
		base.OnMouseUp(e);
	}

	protected override void OnResize(EventArgs e)
	{
		Refresh();
		base.OnResize(e);
	}

	private void InitialiseContextMenu()
	{
		foreach (ToolStripMenuItem item in m_ContextMenu.Items)
		{
			item.CheckedChanged -= ContextMenuItemCheckChanged;
		}
		m_ContextMenu.Items.Clear();
		foreach (Column column in m_Columns)
		{
			ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(column.Name);
			toolStripMenuItem.CheckOnClick = true;
			toolStripMenuItem.Checked = column.Visible;
			toolStripMenuItem.CheckedChanged += ContextMenuItemCheckChanged;
			m_ContextMenu.Items.Add(toolStripMenuItem);
		}
	}

	private void ContextMenuItemCheckChanged(object sender, EventArgs e)
	{
		ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
		int index = m_ContextMenu.Items.IndexOf(toolStripMenuItem);
		m_Columns[index].Visible = toolStripMenuItem.Checked;
		if (this.ColumnVisibilityChanged != null)
		{
			this.ColumnVisibilityChanged();
		}
	}

	private void UpdateContextMenu()
	{
		if (ContextMenuNeedsUpdating)
		{
			InitialiseContextMenu();
		}
		for (int i = 0; i < m_Columns.Count; i++)
		{
			ToolStripMenuItem obj = (ToolStripMenuItem)m_ContextMenu.Items[i];
			obj.Checked = m_Columns[i].Visible;
			obj.CheckOnClick = !m_Columns[i].Visible || VisibleColumnCount != 1;
		}
	}
}
