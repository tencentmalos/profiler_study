using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class FloatingForm : Form
{
	private DockManager m_DockManager;

	private DockPanel m_RootPanel;

	private bool m_Dragging;

	private Size m_StartDraggingSize;

	private bool m_MouseMoveDrag;

	private Point m_LastMousePt;

	public DockPanel DockPanel => m_RootPanel;

	private FloatingForm()
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		base.ShowInTaskbar = false;
	}

	public FloatingForm(DockManager dock_manager)
		: this()
	{
		m_DockManager = dock_manager;
		UpdateIcon();
	}

	public FloatingForm(DockPanel dock_panel, DockManager dock_manager)
		: this()
	{
		m_DockManager = dock_manager;
		m_RootPanel = dock_panel;
		dock_panel.Dock = DockStyle.Fill;
		base.Controls.Add(m_RootPanel);
		Text = m_RootPanel.ActivePanelName;
		UpdateIcon();
	}

	private void UpdateIcon()
	{
		if (m_DockManager != null && m_DockManager.Icon != null)
		{
			base.Icon = m_DockManager.Icon;
		}
	}

	protected override void WndProc(ref Message msg)
	{
		switch (msg.Msg)
		{
		case 561:
			m_Dragging = true;
			m_StartDraggingSize = base.Size;
			break;
		case 562:
			if (m_Dragging)
			{
				m_Dragging = false;
				OnDraggingStopped();
			}
			else
			{
				m_RootPanel.UpdateFloatingBounds();
			}
			break;
		case 163:
			m_DockManager.DockFloating(this);
			return;
		}
		base.WndProc(ref msg);
	}

	private void OnDraggingStopped()
	{
		if (!m_DockManager.DockToDockTarget(this))
		{
			m_RootPanel.UpdateFloatingBounds();
		}
	}

	protected override void OnMove(EventArgs e)
	{
		bool flag = base.Size != m_StartDraggingSize;
		if (m_Dragging && !flag)
		{
			m_DockManager.HandleDragWindowMouseMove(m_RootPanel, Cursor.Position);
		}
		base.OnMove(e);
	}

	public void Read(XmlReadStream read_stream, ICollection<Control> controls)
	{
		SuspendLayout();
		base.Location = Utils.ReadPoint(read_stream, "Location", base.Location);
		base.Size = Utils.ReadSize(read_stream, "Size", base.Size);
		m_RootPanel = new DockPanel(m_DockManager);
		m_RootPanel.Dock = DockStyle.Fill;
		base.Controls.Add(m_RootPanel);
		if (read_stream.StartElement("RootPanel"))
		{
			m_RootPanel.Read(read_stream, controls);
			read_stream.EndElement();
		}
		Text = m_RootPanel.ActivePanelName;
		ResumeLayout();
	}

	public void Write(XmlWriteStream write_stream)
	{
		Utils.Write(write_stream, "Location", base.Location);
		Utils.Write(write_stream, "Size", base.Size);
		write_stream.StartElement("RootPanel");
		m_RootPanel.Write(write_stream);
		write_stream.EndElement();
	}

	public bool SelectPanel(Point screen_pt)
	{
		return m_RootPanel.SelectPanel(Cursor.Position);
	}

	public void DeselectPanel()
	{
		m_RootPanel.DeselectPanel();
	}

	public void SetDragging()
	{
		m_Dragging = true;
		m_MouseMoveDrag = true;
		m_LastMousePt = Cursor.Position;
		base.Capture = true;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_MouseMoveDrag)
		{
			Point lastMousePt = PointToScreen(e.Location);
			base.Location = new Point(base.Location.X + lastMousePt.X - m_LastMousePt.X, base.Location.Y + lastMousePt.Y - m_LastMousePt.Y);
			m_LastMousePt = lastMousePt;
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		StopDragging();
		OnDraggingStopped();
		base.OnMouseUp(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		StopDragging();
		base.OnLostFocus(e);
	}

	private void StopDragging()
	{
		if (m_MouseMoveDrag)
		{
			m_MouseMoveDrag = false;
			base.Capture = false;
		}
	}

	public void ForceClose()
	{
		int tabCount = m_RootPanel.TabCount;
		for (int i = 0; i < tabCount; i++)
		{
			Close();
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (m_RootPanel != null && m_RootPanel.TabCount > 1)
		{
			m_RootPanel.CloseCurrentTab();
			e.Cancel = true;
		}
		else
		{
			base.OnFormClosing(e);
		}
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		return m_RootPanel.GetControlAtScreenPoint(pt);
	}

	public Control GetActiveControl()
	{
		return m_RootPanel.GetActiveControl();
	}
}
