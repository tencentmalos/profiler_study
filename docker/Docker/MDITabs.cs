using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class MDITabs : Control
{
	private const int m_TopGap = 8;

	private const int m_BottomGap = 4;

	private const int m_TextGapX = 8;

	private const int m_CloseGapX = 4;

	private const int m_TextGapY1 = 6;

	private const int m_TextGapY2 = 4;

	private const int m_TabStartX = 2;

	private const int m_CloseButtonSize = 15;

	private const int m_CornerSize = 2;

	private const int m_ListButtonSize = 15;

	private const int m_ListButtonGapX = 8;

	private const int m_ListButtonTriSize = 4;

	private const int m_ListFormWidth = 200;

	private List<MDITab> m_Tabs = new List<MDITab>();

	private MDITab m_ActiveTab;

	private MDITab m_HighlightTab;

	private bool m_HighlightCloseButton;

	private bool m_HighlightListButton;

	private bool m_HighlightRestoreButton;

	private bool m_DraggingTab;

	private float m_DPIScale;

	private const int m_TabWidth = 200;

	private MDIPanel MDIPanel => (MDIPanel)base.Parent;

	private int MaxTabX
	{
		get
		{
			Rectangle restoreButtonRect = GetRestoreButtonRect();
			return restoreButtonRect.Left - restoreButtonRect.Width;
		}
	}

	public DockManager DockManager
	{
		get
		{
			Control control = base.Parent;
			while (control != null && !(control is DockManager))
			{
				control = control.Parent;
			}
			return (DockManager)control;
		}
	}

	public List<MDITab> Tabs => m_Tabs;

	public MDITabs()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		m_DPIScale = (float)base.DeviceDpi / 96f;
		int num = DPIScale(8) + DPIScale(6) + Font.Height + DPIScale(4) + DPIScale(4);
		base.Size = new Size(DPIScale(200), num);
	}

	private int DPIScale(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
	}

	protected override void OnResize(EventArgs eventargs)
	{
		base.OnResize(eventargs);
		Refresh();
	}

	private MDITab FindTab(MDIForm form)
	{
		foreach (MDITab tab in m_Tabs)
		{
			if (tab.Form == form)
			{
				return tab;
			}
		}
		return null;
	}

	private void AddTab(MDIForm form)
	{
		MDITab mDITab = new MDITab(form);
		mDITab.MDITabNameChanged += MDITabNameChanged;
		m_Tabs.Insert(0, mDITab);
		m_ActiveTab = mDITab;
	}

	private void MDITabNameChanged()
	{
		Refresh();
	}

	public void UpdateTabs()
	{
		Set<MDIForm> set = new Set<MDIForm>();
		foreach (MDIForm mDIForm in MDIPanel.MDIForms)
		{
			if (FindTab(mDIForm) == null)
			{
				AddTab(mDIForm);
			}
			set.Add(mDIForm);
		}
		List<MDITab> list = new List<MDITab>();
		foreach (MDITab tab in m_Tabs)
		{
			if (!set.Contains(tab.Form))
			{
				list.Add(tab);
			}
		}
		foreach (MDITab item in list)
		{
			RemoveTab(item);
		}
		if (m_ActiveTab != null && list.Contains(m_ActiveTab))
		{
			m_ActiveTab = ((m_Tabs.Count != 0) ? m_Tabs[0] : null);
		}
		m_HighlightTab = null;
		Refresh();
	}

	protected override void Dispose(bool disposing)
	{
		foreach (MDITab item in new List<MDITab>(m_Tabs))
		{
			RemoveTab(item);
		}
		base.Dispose(disposing);
	}

	private void RemoveTab(MDITab tab)
	{
		tab.MDITabNameChanged -= MDITabNameChanged;
		tab.Dispose();
		m_Tabs.Remove(tab);
	}

	private void LayoutTabs(Graphics graphics)
	{
		int num = DPIScale(2);
		foreach (MDITab tab in m_Tabs)
		{
			tab.X = num;
			tab.Width = CalcTabWidth(tab, graphics);
			num += tab.Width;
		}
	}

	private void MakeActiveTabVisible(Graphics graphics)
	{
		if (MDIPanel.MDIForms.Count == 0)
		{
			return;
		}
		MDIForm activeForm = MDIPanel.ActiveForm;
		if (!IsTabVisible(activeForm))
		{
			MDITab mDITab = FindTab(activeForm);
			if (mDITab != null)
			{
				m_Tabs.Remove(mDITab);
				m_Tabs.Insert(0, mDITab);
				LayoutTabs(graphics);
			}
		}
	}

	private bool IsTabVisible(MDIForm form)
	{
		GetRestoreButtonRect();
		foreach (MDITab tab in m_Tabs)
		{
			if (tab.X + tab.Width > MaxTabX)
			{
				return false;
			}
			if (tab.Form == form)
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(SystemColors.ControlDarkDark);
		LayoutTabs(e.Graphics);
		MakeActiveTabVisible(e.Graphics);
		GetRestoreButtonRect();
		foreach (MDITab tab in m_Tabs)
		{
			if (tab.X + tab.Width > MaxTabX)
			{
				break;
			}
			DrawTab(tab, e.Graphics);
		}
		e.Graphics.FillRectangle(SystemBrushes.ControlLightLight, 0, base.Height - DPIScale(4), base.Width, DPIScale(4));
		DrawListButton(e.Graphics);
		DrawRestoreButton(e.Graphics);
		base.OnPaint(e);
	}

	private void DrawListButton(Graphics graphics)
	{
		Rectangle listButtonRect = GetListButtonRect();
		int num = listButtonRect.X + listButtonRect.Width / 2;
		int num2 = listButtonRect.Y + listButtonRect.Height / 2;
		if (m_HighlightListButton)
		{
			graphics.FillRectangle(SystemBrushes.ControlLight, listButtonRect);
			graphics.DrawRectangle(SystemPens.ControlDarkDark, listButtonRect);
		}
		int num3 = DPIScale(4);
		Point[] points = new Point[3]
		{
			new Point(listButtonRect.Left + num3, num2),
			new Point(listButtonRect.Right - num3, num2),
			new Point(num, num2 + num3)
		};
		Brush brush = (m_HighlightListButton ? SystemBrushes.ControlDarkDark : SystemBrushes.ControlLight);
		graphics.FillPolygon(brush, points);
	}

	private void DrawRestoreButton(Graphics graphics)
	{
		Rectangle restoreButtonRect = GetRestoreButtonRect();
		int num = restoreButtonRect.Width / 2;
		int num2 = restoreButtonRect.Height / 2;
		if (m_HighlightRestoreButton)
		{
			graphics.FillRectangle(SystemBrushes.ControlLightLight, restoreButtonRect);
		}
		Pen pen = (m_HighlightRestoreButton ? SystemPens.ControlDarkDark : SystemPens.ControlLight);
		Rectangle rect = new Rectangle(restoreButtonRect.Left + 2, restoreButtonRect.Top + 2, num, num2);
		graphics.DrawRectangle(pen, rect);
		Rectangle rect2 = new Rectangle(restoreButtonRect.Left + restoreButtonRect.Width / 2 - 2, restoreButtonRect.Top + restoreButtonRect.Height / 2 - 2, num, num2);
		graphics.DrawRectangle(pen, rect2);
	}

	private void DrawTab(MDITab tab, Graphics graphics)
	{
		bool flag = tab.Form == MDIPanel.ActiveForm;
		if (flag || tab == m_HighlightTab)
		{
			Rectangle tabRect = GetTabRect(tab);
			int num = DPIScale(2);
			Point[] points = new Point[6]
			{
				new Point(tabRect.Left, tabRect.Bottom),
				new Point(tabRect.Left, tabRect.Top + num),
				new Point(tabRect.Left + num, tabRect.Top),
				new Point(tabRect.Right - num, tabRect.Top),
				new Point(tabRect.Right, tabRect.Top + num),
				new Point(tabRect.Right, tabRect.Bottom)
			};
			Brush brush = (flag ? SystemBrushes.ControlLightLight : SystemBrushes.ControlDark);
			graphics.FillPolygon(brush, points);
			Rectangle closeRect = GetCloseRect(tab);
			bool highlight = tab == m_HighlightTab && m_HighlightCloseButton;
			Utils.DrawCloseButton(closeRect, graphics, highlight);
		}
		Brush brush2 = (flag ? SystemBrushes.ControlDarkDark : SystemBrushes.ControlLightLight);
		int num2 = tab.X + DPIScale(8);
		int num3 = DPIScale(8) + DPIScale(6);
		graphics.DrawString(tab.Name, Font, brush2, num2, num3);
	}

	private Rectangle GetTabRect(MDITab tab)
	{
		return new Rectangle(tab.X, DPIScale(8), tab.Width, base.Height - DPIScale(4) - DPIScale(8));
	}

	private Rectangle GetCloseRect(MDITab tab)
	{
		Rectangle tabRect = GetTabRect(tab);
		int num = DPIScale(15);
		return new Rectangle(tabRect.Right - DPIScale(4) - num, tabRect.Top + DPIScale(8) + Font.Height - num - 2, num, num);
	}

	private MDITab FindTab(int x)
	{
		foreach (MDITab tab in m_Tabs)
		{
			if (tab.X + tab.Width > x)
			{
				return tab;
			}
		}
		return null;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_HighlightListButton)
			{
				Rectangle listButtonRect = GetListButtonRect();
				int num = listButtonRect.Right - DPIScale(200);
				int num2 = listButtonRect.Bottom + 1;
				Point location = PointToScreen(new Point(num, num2));
				WindowListForm windowListForm = new WindowListForm(this, location, DPIScale(200));
				windowListForm.Show();
				windowListForm.Location = location;
			}
			else if (m_HighlightRestoreButton)
			{
				DockManager.RestoreWindows();
			}
			else
			{
				MDITab mDITab = FindTab(e.X);
				if (mDITab != null)
				{
					if (mDITab == m_HighlightTab && m_HighlightCloseButton)
					{
						bool flag = mDITab == m_ActiveTab;
						int num3 = m_Tabs.IndexOf(mDITab);
						if (DockManager.TryCloseMDIForm(mDITab.Form))
						{
							UpdateTabs();
							if (flag && m_Tabs.Count != 0)
							{
								if (num3 == m_Tabs.Count)
								{
									num3--;
								}
								ActivateMDIForm(m_Tabs[num3].Form);
								m_ActiveTab = m_Tabs[num3];
							}
							Refresh();
						}
					}
					else
					{
						if (mDITab != m_ActiveTab)
						{
							ActivateMDIForm(mDITab.Form);
							m_ActiveTab = mDITab;
							Refresh();
						}
						m_DraggingTab = true;
						base.Capture = true;
					}
				}
			}
		}
		base.OnMouseDown(e);
	}

	public void ActivateMDIForm(MDIForm form)
	{
		DockManager.BringMaximisedMDIFormToFront(form);
		Refresh();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		bool flag = false;
		if (m_DraggingTab)
		{
			MDITab mDITab = FindTab(e.X);
			if (mDITab != null && mDITab != m_ActiveTab)
			{
				int index = m_Tabs.IndexOf(m_ActiveTab);
				int index2 = m_Tabs.IndexOf(mDITab);
				MDITab value = m_Tabs[index2];
				m_Tabs[index2] = m_HighlightTab;
				m_Tabs[index] = value;
				Refresh();
			}
			else if (e.Y < -base.Height || e.Y > 2 * base.Height)
			{
				if (m_Tabs.Count == 1)
				{
					base.Visible = false;
				}
				MDIPanel.FloatMaximisedForm(m_ActiveTab.Form, m_ActiveTab.X);
				StopDraggingTab();
			}
		}
		else
		{
			MDITab mDITab2 = FindTab(e.X);
			if (m_HighlightTab != mDITab2)
			{
				m_HighlightTab = mDITab2;
				flag = true;
			}
			bool flag2 = false;
			if (m_HighlightTab != null)
			{
				flag2 = GetCloseRect(m_HighlightTab).Contains(e.Location);
			}
			if (m_HighlightCloseButton != flag2)
			{
				m_HighlightCloseButton = flag2;
				flag = true;
			}
			bool flag3 = GetListButtonRect().Contains(e.Location);
			if (m_HighlightListButton != flag3)
			{
				m_HighlightListButton = flag3;
				flag = true;
			}
			bool flag4 = GetRestoreButtonRect().Contains(e.Location);
			if (m_HighlightRestoreButton != flag4)
			{
				m_HighlightRestoreButton = flag4;
				flag = true;
			}
			if (flag)
			{
				Refresh();
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			StopDraggingTab();
		}
		base.OnMouseUp(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		bool flag = false;
		if (m_HighlightTab != null)
		{
			m_HighlightTab = null;
			flag = true;
		}
		if (m_HighlightListButton)
		{
			m_HighlightListButton = false;
			flag = true;
		}
		if (m_HighlightRestoreButton)
		{
			m_HighlightRestoreButton = false;
			flag = true;
		}
		StopDraggingTab();
		if (flag)
		{
			Refresh();
		}
		base.OnMouseLeave(e);
	}

	private void StopDraggingTab()
	{
		if (m_DraggingTab)
		{
			m_DraggingTab = false;
			base.Capture = false;
		}
	}

	private int CalcTabWidth(MDITab tab, Graphics graphics)
	{
		int num = (int)graphics.MeasureString(tab.Name, Font).Width;
		return DPIScale(8) + num + DPIScale(4) + DPIScale(15) + DPIScale(4);
	}

	public Rectangle GetRestoreButtonRect()
	{
		Rectangle listButtonRect = GetListButtonRect();
		return new Rectangle(listButtonRect.Left - 2 * listButtonRect.Width, listButtonRect.Top, listButtonRect.Width, listButtonRect.Height);
	}

	public Rectangle GetListButtonRect()
	{
		int num = DPIScale(15);
		return new Rectangle(base.Width - DPIScale(8) - num, (base.Height - DPIScale(4) - num) / 2, num, num);
	}

	public void Read(XmlReadStream read_stream)
	{
		List<int> list = new List<int>();
		if (read_stream.StartElement("Tabs"))
		{
			for (int i = 0; i < read_stream.Count; i++)
			{
				read_stream.StartElement(i);
				int item = Misc.Convert(read_stream.CurrentValue);
				list.Add(item);
				read_stream.EndElement();
			}
			read_stream.EndElement();
		}
		List<MDITab> list2 = new List<MDITab>(m_Tabs.Count);
		foreach (int item2 in list)
		{
			MDITab mDITab = FindTabFromDockPanelID(item2);
			if (mDITab != null)
			{
				m_Tabs.Remove(mDITab);
				list2.Add(mDITab);
			}
		}
		list2.AddRange(m_Tabs);
		m_Tabs = list2;
	}

	private MDITab FindTabFromDockPanelID(int dock_panel_id)
	{
		foreach (MDITab tab in m_Tabs)
		{
			if (tab.Form.DockPanel.UniqueID == dock_panel_id)
			{
				return tab;
			}
		}
		return null;
	}

	public void Write(XmlWriteStream write_stream)
	{
		write_stream.StartElement("Tabs");
		foreach (MDITab tab in m_Tabs)
		{
			write_stream.Write("Tab", tab.Form.DockPanel.UniqueID);
		}
		write_stream.EndElement();
	}
}
