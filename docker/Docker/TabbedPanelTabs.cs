using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class TabbedPanelTabs : Control
{
	public delegate void TabSelectedHandler(Control control);

	public delegate void FloatTabHandler(Control control);

	public delegate void CloseTabHandler(Control control);

	private class Tab
	{
		public Control m_Control;

		public Rectangle m_Rect;

		public Tab(Control control)
		{
			m_Control = control;
		}
	}

	private List<Tab> m_Tabs = new List<Tab>();

	private Tab m_ActiveTab;

	private const int m_TextGapX = 4;

	private const int m_CornerSize = 2;

	private const int m_FontGapY = 4;

	private const int m_TabGapBottom = 2;

	private int m_MaxDragDist;

	private bool m_DraggingTab;

	private ContextMenuStrip m_ContextMenu = new ContextMenuStrip();

	private ToolStripMenuItem m_CloseTabMenuItem = new ToolStripMenuItem("Close");

	private Tab m_ContextMenuTab;

	private float m_DPIScale;

	public event TabSelectedHandler TabSelected;

	public event FloatTabHandler FloatTab;

	public event CloseTabHandler CloseTab;

	public TabbedPanelTabs()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		IniitaliseContextMenu();
		m_DPIScale = (float)base.DeviceDpi / 96f;
		base.Size = new Size(base.Width, Font.Height + 2 * ScaleDPI(4) + ScaleDPI(2));
		m_MaxDragDist = 3 * Font.Height / 2;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void IniitaliseContextMenu()
	{
		m_ContextMenu.Items.Add(m_CloseTabMenuItem);
		m_CloseTabMenuItem.Click += CloseMenuItemClick;
	}

	private void CloseMenuItemClick(object sender, EventArgs e)
	{
		if (this.CloseTab != null)
		{
			this.CloseTab(m_ContextMenuTab.m_Control);
		}
	}

	private void LayoutTabs(Graphics graphics)
	{
		int num = ScaleDPI(4);
		int num2 = base.Height - ScaleDPI(2);
		foreach (Tab tab in m_Tabs)
		{
			string text = tab.m_Control.Text;
			int num3 = (int)graphics.MeasureString(text, Font).Width + 2 * ScaleDPI(4);
			tab.m_Rect = new Rectangle(num, 0, num3, num2);
			num += num3;
		}
		if (num <= base.Width)
		{
			return;
		}
		num = 0;
		int num4 = base.Width / m_Tabs.Count;
		foreach (Tab tab2 in m_Tabs)
		{
			tab2.m_Rect = new Rectangle(num, 0, num4, num2);
			num += num4;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		LayoutTabs(e.Graphics);
		e.Graphics.Clear(SystemColors.ControlDarkDark);
		e.Graphics.DrawLine(SystemPens.ControlDark, 0, 0, base.Width, 0);
		foreach (Tab tab in m_Tabs)
		{
			string s = tab.m_Control.Text;
			_ = e.Graphics.MeasureString(s, Font).Width;
			if (tab == m_ActiveTab)
			{
				int num = ScaleDPI(2);
				Rectangle rect = tab.m_Rect;
				Point[] points = new Point[6]
				{
					new Point(rect.Left, rect.Top),
					new Point(rect.Left, rect.Bottom - num),
					new Point(rect.Left + num, rect.Bottom),
					new Point(rect.Right - num, rect.Bottom),
					new Point(rect.Right, rect.Bottom - num),
					new Point(rect.Right, rect.Top)
				};
				e.Graphics.FillPolygon(SystemBrushes.ControlLightLight, points);
			}
			Brush brush = ((tab == m_ActiveTab) ? SystemBrushes.ControlDarkDark : SystemBrushes.ControlLightLight);
			int num2 = tab.m_Rect.X + ScaleDPI(4);
			int num3 = tab.m_Rect.Top + (tab.m_Rect.Height - Font.Height) / 2;
			Rectangle clip = new Rectangle(tab.m_Rect.X + ScaleDPI(4), tab.m_Rect.Y, tab.m_Rect.Width - 2 * ScaleDPI(4), tab.m_Rect.Height);
			e.Graphics.SetClip(clip);
			e.Graphics.DrawString(s, Font, brush, num2, num3);
			e.Graphics.SetClip(base.ClientRectangle);
		}
		base.OnPaint(e);
	}

	private Tab GetTab(Point pos)
	{
		foreach (Tab tab in m_Tabs)
		{
			if (tab.m_Rect.Contains(pos))
			{
				return tab;
			}
		}
		return null;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		Tab tab = GetTab(e.Location);
		if (e.Button == MouseButtons.Left)
		{
			if (tab != null)
			{
				m_ActiveTab = tab;
				if (this.TabSelected != null)
				{
					this.TabSelected(tab.m_Control);
				}
				Refresh();
				m_DraggingTab = true;
				base.Capture = true;
			}
		}
		else if (e.Button == MouseButtons.Right && tab != null)
		{
			m_ContextMenuTab = GetTab(e.Location);
			m_ContextMenu.Show(this, PointToClient(Cursor.Position));
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_DraggingTab)
		{
			bool flag = false;
			int num = 0;
			foreach (Tab tab in m_Tabs)
			{
				if (tab.m_Rect.Contains(e.Location) && tab != m_ActiveTab)
				{
					int index = m_Tabs.IndexOf(m_ActiveTab);
					Tab value = m_Tabs[num];
					m_Tabs[num] = m_ActiveTab;
					m_Tabs[index] = value;
					Refresh();
					flag = true;
					break;
				}
				num++;
			}
			Rectangle rect = m_ActiveTab.m_Rect;
			if (!flag && !rect.Contains(e.Location))
			{
				int val = 0;
				int val2 = 0;
				if (e.X < rect.Left)
				{
					val = rect.Left - e.X;
				}
				if (e.X > rect.Right)
				{
					val = e.X - rect.Right;
				}
				if (e.Y < rect.Top)
				{
					val2 = rect.Top - e.Y;
				}
				if (e.Y > rect.Bottom)
				{
					val2 = e.Y - rect.Bottom;
				}
				if (Math.Max(val, val2) > m_MaxDragDist)
				{
					StopDragging();
					if (this.FloatTab != null)
					{
						this.FloatTab(m_ActiveTab.m_Control);
					}
				}
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			StopDragging();
		}
		base.OnMouseUp(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		StopDragging();
		base.OnLostFocus(e);
	}

	private void StopDragging()
	{
		if (m_DraggingTab)
		{
			m_DraggingTab = false;
			base.Capture = false;
		}
	}

	protected override void OnResize(EventArgs e)
	{
		Refresh();
		base.OnResize(e);
	}

	public void AddControl(Control control)
	{
		Tab tab = new Tab(control);
		m_Tabs.Add(tab);
		m_ActiveTab = tab;
		Refresh();
	}

	public void RemoveControl(Control control)
	{
		Tab tab = null;
		foreach (Tab tab2 in m_Tabs)
		{
			if (tab2.m_Control == control)
			{
				m_Tabs.Remove(tab2);
				break;
			}
			tab = tab2;
		}
		if (tab != null)
		{
			m_ActiveTab = tab;
		}
		else if (m_Tabs.Count != 0)
		{
			m_ActiveTab = m_Tabs[0];
		}
		else
		{
			m_ActiveTab = null;
		}
		Refresh();
	}

	public void Read(XmlReadStream read_stream)
	{
		List<string> list = new List<string>();
		if (read_stream.StartElement("Tabs"))
		{
			for (int i = 0; i < read_stream.Count; i++)
			{
				read_stream.StartElement(i);
				list.Add(read_stream.CurrentValue);
				read_stream.EndElement();
			}
			read_stream.EndElement();
		}
		List<Tab> list2 = new List<Tab>(m_Tabs.Count);
		foreach (string item in list)
		{
			Tab tab = FindTab(item);
			if (tab != null)
			{
				list2.Add(tab);
				m_Tabs.Remove(tab);
			}
		}
		list2.AddRange(m_Tabs);
		m_Tabs = list2;
		string value = null;
		if (read_stream.Read("ActiveTab", ref value))
		{
			Tab tab2 = FindTab(value);
			if (tab2 != null)
			{
				m_ActiveTab = tab2;
			}
		}
	}

	private Tab FindTab(string name)
	{
		foreach (Tab tab in m_Tabs)
		{
			if (tab.m_Control.Text == name)
			{
				return tab;
			}
		}
		return null;
	}

	public void Write(XmlWriteStream write_stream)
	{
		write_stream.StartElement("Tabs");
		foreach (Tab tab in m_Tabs)
		{
			write_stream.Write("Tab", tab.m_Control.Text);
		}
		write_stream.EndElement();
		write_stream.Write("ActiveTab", m_ActiveTab.m_Control.Text);
	}

	public void SetActiveTab(Control control)
	{
		foreach (Tab tab in m_Tabs)
		{
			if (tab.m_Control == control)
			{
				if (m_ActiveTab != tab)
				{
					m_ActiveTab = tab;
					Refresh();
				}
				break;
			}
		}
	}
}
