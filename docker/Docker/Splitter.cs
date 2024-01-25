using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class Splitter : Control
{
	private const int m_SplitterSize = 4;

	private bool m_Dragging;

	private Point m_StartDragPos;

	private SplitMode m_SplitMode;

	private Cursor m_OldCursor;

	private static ResizeLinePanel m_ResizeLinePanel = new ResizeLinePanel();

	public Splitter(SplitMode split_mode, Color colour)
	{
		m_SplitMode = split_mode;
		BackColor = colour;
		base.Size = new Size(4, 4);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		m_Dragging = true;
		base.Capture = true;
		m_StartDragPos = e.Location;
		Point pos = PointToScreen(new Point(0, 0));
		ShowResizeLinePanel(pos);
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_Dragging)
		{
			Point location = m_ResizeLinePanel.Location;
			Point point = PointToScreen(new Point(0, 0));
			if (m_SplitMode == SplitMode.Horz)
			{
				int num = e.Y - m_StartDragPos.Y;
				location.Y = point.Y + num;
			}
			else
			{
				int num2 = e.X - m_StartDragPos.X;
				location.X = point.X + num2;
			}
			m_ResizeLinePanel.Location = location;
		}
		else
		{
			m_OldCursor = Cursor;
			if (m_SplitMode == SplitMode.Horz)
			{
				Cursor = Cursors.SizeNS;
			}
			else
			{
				Cursor = Cursors.SizeWE;
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		RestoreCursor();
		base.OnMouseLeave(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		RestoreCursor();
		base.OnLostFocus(e);
	}

	private void RestoreCursor()
	{
		if (m_OldCursor != null)
		{
			Cursor = m_OldCursor;
			m_OldCursor = null;
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (m_Dragging)
		{
			m_Dragging = false;
			base.Capture = false;
			m_ResizeLinePanel.Hide();
			Point pos = PointToScreen(new Point(e.X, e.Y));
			((DockPanel)base.Parent).SplitterMoved(pos);
		}
		base.OnMouseUp(e);
	}

	private void ShowResizeLinePanel(Point pos)
	{
		m_ResizeLinePanel.Location = pos;
		m_ResizeLinePanel.FormSize = base.Size;
		m_ResizeLinePanel.Show();
		m_ResizeLinePanel.BringToFront();
	}
}
