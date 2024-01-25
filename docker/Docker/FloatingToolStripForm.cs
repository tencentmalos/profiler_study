using System;
using System.Windows.Forms;

namespace Docker;

internal class FloatingToolStripForm : Form
{
	private DockManager m_DockManager;

	private ToolStrip m_ToolStrip;

	public ToolStrip ToolStrip => m_ToolStrip;

	public FloatingToolStripForm(DockManager dock_manager, ToolStrip tool_strip)
	{
		m_DockManager = dock_manager;
		m_ToolStrip = tool_strip;
		base.FormBorderStyle = FormBorderStyle.FixedToolWindow;
		tool_strip.GripStyle = ToolStripGripStyle.Hidden;
		base.Controls.Add(tool_strip);
		base.ClientSize = tool_strip.PreferredSize;
	}

	protected override void WndProc(ref Message msg)
	{
		switch (msg.Msg)
		{
		case 163:
			return;
		case 562:
			m_DockManager.OnFloatingToolStripDragged(this);
			break;
		}
		base.WndProc(ref msg);
	}

	protected override void OnMove(EventArgs e)
	{
		m_DockManager.OnFloatingToolStripFormMoved(this);
		base.OnMove(e);
	}
}
