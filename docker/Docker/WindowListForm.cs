using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class WindowListForm : Form
{
	private MDITabs m_TabsForm;

	private List<MDITab> m_Tabs;

	private const int m_TextOffsetX = 4;

	private const int m_GapY = 4;

	private int m_HilightTabIndex = -1;

	private const int m_ScrollButtonHeight = 12;

	private int m_ScrollIndex;

	private int m_ScrollSpeed;

	private System.Windows.Forms.Timer m_Timer = new System.Windows.Forms.Timer();

	private const int m_ScrollTimerInterval = 100;

	private int ItemHeight => Font.Height + 4;

	private int MaxScroll
	{
		get
		{
			int num = base.Height / ItemHeight;
			return m_Tabs.Count - num;
		}
	}

	private bool TopScrollButtonVisible => m_ScrollIndex > 0;

	private bool BottomScrollButtonVisible => (m_Tabs.Count - m_ScrollIndex) * ItemHeight > base.Height;

	public WindowListForm(MDITabs tabs_form, Point location, int width)
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		m_TabsForm = tabs_form;
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		m_Tabs = new List<MDITab>(tabs_form.Tabs);
		m_Tabs.Sort(MDITabCompare);
		int val = 4 + tabs_form.Tabs.Count * ItemHeight;
		val = Math.Min(Screen.PrimaryScreen.WorkingArea.Bottom - location.Y, val);
		base.Size = new Size(width, val);
		m_Timer.Interval = 100;
		m_Timer.Tick += ScrollTimerTick;
		m_Timer.Start();
	}

	protected override void OnClosed(EventArgs e)
	{
		m_Timer.Stop();
		m_Timer.Tick -= ScrollTimerTick;
		base.OnClosed(e);
	}

	private static int MDITabCompare(MDITab tab1, MDITab tab2)
	{
		return tab1.Name.CompareTo(tab2.Name);
	}

	private void ScrollTimerTick(object sender, EventArgs e)
	{
		if (m_ScrollSpeed != 0)
		{
			m_ScrollIndex = Misc.Clamp(m_ScrollIndex + m_ScrollSpeed, 0, MaxScroll);
			Refresh();
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private Rectangle GetTopScrollRect()
	{
		return new Rectangle(0, 0, base.Width, 12);
	}

	private Rectangle GetBottomScrollRect()
	{
		return new Rectangle(0, base.Height - 12, base.Width, 12);
	}

	private void DrawScrollButton(Graphics graphics, bool top)
	{
		Rectangle rect = (top ? GetTopScrollRect() : GetBottomScrollRect());
		graphics.FillRectangle(SystemBrushes.ControlLight, rect);
		graphics.DrawRectangle(SystemPens.ControlDarkDark, rect);
		int num = rect.Left + rect.Width / 2;
		int num2 = rect.Top + rect.Height / 2;
		int num3 = rect.Height / 2;
		int num4 = (top ? 1 : (-1));
		Point[] points = new Point[3]
		{
			new Point(num, num2 - num4 * num3 / 2),
			new Point(num - num3 / 2, num2 + num4 * num3 / 2),
			new Point(num + num3 / 2, num2 + num4 * num3 / 2)
		};
		graphics.FillPolygon(SystemBrushes.ControlDarkDark, points);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(SystemColors.ControlDark);
		int num = 4;
		for (int i = m_ScrollIndex; i < m_Tabs.Count; i++)
		{
			if (num >= base.Height)
			{
				break;
			}
			MDITab mDITab = m_Tabs[i];
			bool num2 = i == m_HilightTabIndex;
			if (num2)
			{
				e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, 0, num, base.Width, ItemHeight);
			}
			Brush brush = (num2 ? SystemBrushes.ControlDarkDark : SystemBrushes.ControlLightLight);
			e.Graphics.DrawString(mDITab.Name, Font, brush, 4f, num);
			num += ItemHeight;
		}
		if (TopScrollButtonVisible)
		{
			DrawScrollButton(e.Graphics, top: true);
		}
		if (BottomScrollButtonVisible)
		{
			DrawScrollButton(e.Graphics, top: false);
		}
		e.Graphics.DrawRectangle(SystemPens.ControlDarkDark, 0, 0, base.Width - 1, base.Height - 1);
		base.OnPaint(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		Close();
	}

	private int GetTabIndex(int y_pt)
	{
		int num = 4 - m_ScrollIndex * ItemHeight;
		for (int i = 0; i < m_Tabs.Count; i++)
		{
			if (num + ItemHeight > y_pt)
			{
				return i;
			}
			num += ItemHeight;
		}
		return -1;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		bool flag = false;
		int num = GetTabIndex(e.Y);
		if (m_HilightTabIndex != num)
		{
			m_HilightTabIndex = num;
			flag = true;
		}
		if (flag)
		{
			Refresh();
		}
		m_ScrollSpeed = 0;
		if (TopScrollButtonVisible && GetTopScrollRect().Contains(e.Location))
		{
			m_ScrollSpeed = -1;
		}
		if (BottomScrollButtonVisible && GetBottomScrollRect().Contains(e.Location))
		{
			m_ScrollSpeed = 1;
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (m_HilightTabIndex != -1)
		{
			m_TabsForm.ActivateMDIForm(m_Tabs[m_HilightTabIndex].Form);
			Close();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		m_ScrollSpeed = 0;
		base.OnMouseLeave(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if (e.Delta < 0)
		{
			if (m_ScrollIndex < MaxScroll)
			{
				m_ScrollIndex = Math.Min(m_ScrollIndex + 2, MaxScroll);
				Refresh();
			}
		}
		else if (e.Delta > 0 && m_ScrollIndex > 0)
		{
			m_ScrollIndex = Math.Max(0, m_ScrollIndex - 2);
			Refresh();
		}
		base.OnMouseWheel(e);
	}
}
