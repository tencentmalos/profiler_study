using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class SessionViewTabsPanel : UserControl
{
	private Pen m_TopLinePen = new Pen(Colours.SessionViewTabActiveTriangle);

	private List<SessionViewTab> m_Tabs = new List<SessionViewTab>();

	private IContainer components;

	public SessionView ActiveTab
	{
		get
		{
			foreach (SessionViewTab tab in m_Tabs)
			{
				if (tab.Button.Active)
				{
					return tab.SessionView;
				}
			}
			return null;
		}
	}

	public event SessionViewActiveTabChangedHandler SessionViewActiveTabChanged;

	public SessionViewTabsPanel()
	{
		InitializeComponent();
	}

	public void AddTab(SessionView session_view)
	{
		SessionViewTab sessionViewTab = new SessionViewTab(session_view);
		sessionViewTab.Button.Location = new Point(GetTotalButtonsWidth(), 1);
		sessionViewTab.Button.Size = new Size(sessionViewTab.Button.Width, base.Height - 1);
		sessionViewTab.Button.Click += TabButtonClicked;
		base.Controls.Add(sessionViewTab.Button);
		m_Tabs.Add(sessionViewTab);
	}

	private int GetTotalButtonsWidth()
	{
		if (m_Tabs.Count == 0)
		{
			return 0;
		}
		return m_Tabs[m_Tabs.Count - 1].Button.Right;
	}

	public void ActivateTab(string name)
	{
		foreach (SessionViewTab tab in m_Tabs)
		{
			if (tab.Button.ButtonText == name)
			{
				ActivateTab(tab);
				break;
			}
		}
	}

	private void TabButtonClicked(object sender, EventArgs e)
	{
		SessionViewTabButton sessionViewTabButton = (SessionViewTabButton)sender;
		ActivateTab(sessionViewTabButton.ButtonText);
	}

	private void ActivateTab(SessionViewTab tab)
	{
		if (ActiveTab != null)
		{
			ActiveTab.Active = false;
		}
		foreach (SessionViewTab tab2 in m_Tabs)
		{
			tab2.Button.Active = tab2 == tab;
		}
		tab.SessionView.Active = true;
		tab.SessionView.BringToFront();
		if (this.SessionViewActiveTabChanged != null)
		{
			this.SessionViewActiveTabChanged(tab.SessionView);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.DrawLine(m_TopLinePen, 0, 0, base.Width, 0);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(237, 237, 237);
		base.Name = "SessionViewTabsPanel";
		base.Size = new System.Drawing.Size(1064, 77);
		base.ResumeLayout(false);
	}
}
