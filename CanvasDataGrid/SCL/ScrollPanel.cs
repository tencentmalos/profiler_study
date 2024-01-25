using System;
using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class ScrollPanel : UserControl
{
	private CellsPanel m_CellsPanel;

	private VScrollBar m_VScrollBar = new VScrollBar();

	private HScrollBar m_HScrollBar = new HScrollBar();

	private bool m_VScrollBarVisible;

	private bool m_HScrollBarVisible;

	public Point ScrollPosition
	{
		get
		{
			return new Point(-m_HScrollBar.Value, -m_VScrollBar.Value);
		}
		set
		{
			if (m_HScrollBar.Visible)
			{
				HScrollBar hScrollBar = m_HScrollBar;
				int value2 = -value.X;
				int max = (m_HScrollBar.Maximum = m_HScrollBar.LargeChange);
				hScrollBar.Value = Utils.Clamp(value2, 0, max);
			}
			else
			{
				m_HScrollBar.Value = 0;
			}
			if (m_VScrollBar.Visible)
			{
				m_VScrollBar.Value = Utils.Clamp(-value.Y, 0, m_VScrollBar.Maximum - m_VScrollBar.LargeChange);
			}
			else
			{
				m_VScrollBar.Value = 0;
			}
		}
	}

	public ScrollPanel(CellsPanel cells_panel)
	{
		m_CellsPanel = cells_panel;
		base.Controls.Add(cells_panel);
		m_VScrollBar.ValueChanged += VScrollBarValueChanged;
		m_VScrollBar.Visible = false;
		m_VScrollBar.Dock = DockStyle.Right;
		base.Controls.Add(m_VScrollBar);
		m_HScrollBar.ValueChanged += HScrollBarValueChanged;
		m_HScrollBar.Visible = false;
		m_HScrollBar.Dock = DockStyle.Bottom;
		base.Controls.Add(m_HScrollBar);
	}

	protected override void OnResize(EventArgs e)
	{
		UpdateScrollBars();
		base.OnResize(e);
	}

	private void VScrollBarValueChanged(object sender, EventArgs e)
	{
		OnScrollPositionChanged(ScrollOrientation.VerticalScroll, -m_VScrollBar.Value);
	}

	private void HScrollBarValueChanged(object sender, EventArgs e)
	{
		OnScrollPositionChanged(ScrollOrientation.HorizontalScroll, -m_HScrollBar.Value);
	}

	private void OnScrollPositionChanged(ScrollOrientation scroll_orientation, int new_value)
	{
		((HDataGrid)base.Parent).ScrollPanelScrollEvent(scroll_orientation, -new_value);
		UpdateCellsPanelScrollPosition();
	}

	private void UpdateCellsPanelScrollPosition()
	{
		if (m_CellsPanel.ScrollPosition != ScrollPosition)
		{
			m_CellsPanel.ScrollPosition = ScrollPosition;
			m_CellsPanel.OnScrollPositionChanged();
		}
	}

	public void UpdateScrollBars()
	{
		Size scrollSize = m_CellsPanel.GetScrollSize();
		bool flag = false;
		bool flag2 = scrollSize.Height > base.Height;
		if (m_VScrollBarVisible != flag2)
		{
			m_VScrollBarVisible = flag2;
			flag = true;
			m_VScrollBar.Visible = flag2;
		}
		m_VScrollBar.Maximum = scrollSize.Height;
		m_VScrollBar.LargeChange = ((base.Height <= 0) ? 1 : base.Height);
		bool flag3 = scrollSize.Width > base.Width;
		if (m_HScrollBarVisible != flag3)
		{
			m_HScrollBarVisible = flag3;
			flag = true;
			m_HScrollBar.Visible = flag3;
			if (flag3)
			{
				m_HScrollBar.Maximum = scrollSize.Width;
				m_HScrollBar.LargeChange = ((base.Width <= 0) ? 1 : base.Width);
			}
		}
		m_VScrollBar.LargeChange = ((base.Height <= 0) ? 1 : base.Height);
		if (flag)
		{
			((HDataGrid)base.Parent).UpdateColumnWidths();
		}
	}

	public void Reset()
	{
		m_HScrollBar.Value = 0;
		m_VScrollBar.Value = 0;
		UpdateCellsPanelScrollPosition();
	}
}
