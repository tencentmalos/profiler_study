using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class ThreadTimeSpanGraph : UserControl
{
	private TimeSpanGraph m_TimeSpanGraph;

	private Session m_Session;

	private Settings m_Settings;

	private int m_ContextMenuClosedtime;

	private const int m_MaxHoverBoxThreadIdCount = 5;

	private int m_ThreadId;

	private string m_RealThreadName;

	private IContainer components;

	private ThreadRowPanel m_ThreadInfoPanel;

	private Panel m_TimeSpanGraphParentPanel;

	private ContextMenuStrip m_ThreadContextMenuStrip;

	private ToolStripMenuItem filterToolStripMenuItem;

	private ToolStripMenuItem hideToolStripMenuItem;

	private ToolStripMenuItem moveUpToolStripMenuItem;

	private ToolStripMenuItem moveDownToolStripMenuItem;

	private ToolStripMenuItem collapseAllToolStripMenuItem;

	private ToolStripMenuItem expandAllToolStripMenuItem;

	private ToolStripSeparator toolStripSeparator1;

	private ToolStripMenuItem m_CollapseMenuItem;

	private ToolStripMenuItem m_ExpandMenuItem;

	private ToolStripSeparator toolStripSeparator4;

	private ToolStripSeparator toolStripSeparator3;

	private ToolStripSeparator toolStripSeparator2;

	public string ThreadName
	{
		get
		{
			return m_ThreadInfoPanel.ThreadName;
		}
		set
		{
			m_TimeSpanGraph.ThreadName = value;
			m_ThreadInfoPanel.ThreadName = value;
			m_ThreadInfoPanel.Refresh();
		}
	}

	public string RealThreadName => m_RealThreadName;

	public Color TimeSpanColour
	{
		get
		{
			return m_TimeSpanGraph.ThreadColour;
		}
		set
		{
			if (m_TimeSpanGraph.ThreadColour != value)
			{
				m_TimeSpanGraph.ThreadColour = value;
				m_ThreadInfoPanel.BackColor = Utils.MultiplyColour(value, 0.9f);
			}
		}
	}

	public TimeSpanGraph TimeSpanGraph => m_TimeSpanGraph;

	public int ThreadLabelWidth => m_ThreadInfoPanel.Width;

	public bool Active
	{
		get
		{
			return m_TimeSpanGraph.Active;
		}
		set
		{
			m_TimeSpanGraph.Active = value;
		}
	}

	public int ThreadId => m_ThreadId;

	public event CollapseExpandThreadToggleHandler CollapseExpandThreadToggle;

	public event CollapseThreadHandler CollapseThread;

	public event ExpandThreadHandler ExpandThread;

	public event CollapseAllThreadsHandler CollapseAllThreads;

	public event ExpandAllThreadsHandler ExpandAllThreads;

	public event HideThreadHandler HideThread;

	public event MoveThreadUpHandler MoveThreadUp;

	public event MoveThreadDownHandler MoveThreadDown;

	public event FilterThreadsClickedHandler FilterThreadsClicked;

	public event UserResizedHeightHandler UserResizedHeight;

	public event TimeSpanHeightChangedHandler TimeSpanHeightChanged;

	public ThreadTimeSpanGraph(Session session, string thread_name, int thread_id, long min_time, Color colour, Settings settings)
	{
		InitializeComponent();
		m_Settings = settings;
		m_Session = session;
		m_TimeSpanGraph = new TimeSpanGraph(session, thread_name, thread_id, min_time, settings.ScopeColourMode, settings);
		m_TimeSpanGraph.UserResizedHeight += TimeSpanGraphUserResizedHeight;
		m_TimeSpanGraph.TimeSpanHeightChanged += OnThreadScopeHeightChangedByUser;
		m_TimeSpanGraph.Dock = DockStyle.Fill;
		ThreadName = thread_name;
		m_ThreadId = thread_id;
		m_RealThreadName = ((thread_id != 0) ? m_Session.GetThreadName(thread_id) : thread_name);
		m_TimeSpanGraphParentPanel.Controls.Add(m_TimeSpanGraph);
		TimeSpanColour = colour;
	}

	private void OnThreadScopeHeightChangedByUser()
	{
		if (this.TimeSpanHeightChanged != null)
		{
			this.TimeSpanHeightChanged();
		}
	}

	private void TimeSpanGraphUserResizedHeight(object sender)
	{
		if (this.UserResizedHeight != null)
		{
			this.UserResizedHeight(this);
		}
	}

	private string GetThreadIdsString(string thread_name)
	{
		if (m_ThreadId != 0)
		{
			return m_ThreadId.ToString();
		}
		string text = "";
		List<int> threadIds = m_Session.GetThreadIds(ThreadName);
		bool flag = threadIds.Count > 5;
		if (flag)
		{
			threadIds.RemoveRange(0, threadIds.Count - 5);
		}
		foreach (int item in threadIds)
		{
			text = text + item + ", ";
		}
		if (text.EndsWith(", "))
		{
			text = text.Substring(0, text.Length - ", ".Length);
		}
		if (flag)
		{
			text = "... " + text;
		}
		return text;
	}

	private void ThreadInfoPanelMouseMove(Point mouse_pos)
	{
		if (MainForm.Inst != null)
		{
			HoverBox hoverBox = MainForm.Inst.HoverBox;
			string text = "Thread" + ThreadName;
			if (!(hoverBox.Target is string) || (string)hoverBox.Target != text)
			{
				hoverBox.Target = text;
				hoverBox.Clear();
				hoverBox.Title = "Thread";
				hoverBox.AddLine("Name", ThreadName);
				hoverBox.AddLine("Id", GetThreadIdsString(ThreadName));
				hoverBox.AddLine("Av Scopes/Frame", m_Session.AverageScopesPerFrame(ThreadName).ToString("0.0"));
				hoverBox.SubmitLines();
			}
			hoverBox.Visible = true;
			Point location = PointToScreen(mouse_pos);
			hoverBox.SetLocation(location);
		}
	}

	private void ThreadInfoPanelMouseLeave(object sender, EventArgs e)
	{
		if (MainForm.Inst != null)
		{
			HoverBox hoverBox = MainForm.Inst.HoverBox;
			string text = "Thread" + ThreadName;
			if (hoverBox.Target is string && (string)hoverBox.Target == text)
			{
				hoverBox.Visible = false;
			}
		}
	}

	private void ThreadInfoPanelMouseClick(object sender, MouseEventArgs e)
	{
		OnMouseClicked(e);
	}

	private void ThreadNamePanelCollapseExpandToggle()
	{
		if (this.CollapseExpandThreadToggle != null)
		{
			this.CollapseExpandThreadToggle(this);
		}
	}

	private void OnMouseClicked(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Right)
		{
			m_ThreadContextMenuStrip.Show(Cursor.Position);
		}
	}

	private void LabelMouseClicked(object sender, MouseEventArgs e)
	{
		OnMouseClicked(e);
	}

	private void FilterThreadMenuItemClicked(object sender, EventArgs e)
	{
		if (this.FilterThreadsClicked != null)
		{
			this.FilterThreadsClicked();
		}
	}

	private void HideThreadMenuItemClick(object sender, EventArgs e)
	{
		if (this.HideThread != null)
		{
			this.HideThread(this);
		}
	}

	private void ContextMenuClosed(object sender, ToolStripDropDownClosedEventArgs e)
	{
		m_ContextMenuClosedtime = Environment.TickCount;
	}

	private void MoveThreadUpMenuItemClicked(object sender, EventArgs e)
	{
		if (this.MoveThreadUp != null)
		{
			this.MoveThreadUp(this);
		}
	}

	private void MoveDownMenuItemClicked(object sender, EventArgs e)
	{
		if (this.MoveThreadDown != null)
		{
			this.MoveThreadDown(this);
		}
	}

	private void CollapseAllMenuItemClicked(object sender, EventArgs e)
	{
		if (this.CollapseAllThreads != null)
		{
			this.CollapseAllThreads();
		}
	}

	private void ExpandAllThreadsMenuItem(object sender, EventArgs e)
	{
		if (this.ExpandAllThreads != null)
		{
			this.ExpandAllThreads();
		}
	}

	private void CollapseMenuItemClicked(object sender, EventArgs e)
	{
		if (this.CollapseThread != null)
		{
			this.CollapseThread(this);
		}
	}

	private void ExpandMenuItemClicked(object sender, EventArgs e)
	{
		if (this.ExpandThread != null)
		{
			this.ExpandThread(this);
		}
	}

	public void UpdateScrolledIntoViewFlag(Rectangle parent_screen_rect)
	{
		Rectangle rectangle = RectangleToScreen(new Rectangle(0, 0, base.Width, base.Height));
		m_TimeSpanGraph.ScrolledIntoView = rectangle.IntersectsWith(parent_screen_rect);
	}

	public void SetSelectedTimeSpan(long time_span_name)
	{
		m_TimeSpanGraph.SelectTimeSpan(time_span_name, null);
		m_ThreadInfoPanel.SetSelectedTimeSpan(time_span_name, m_TimeSpanGraph.ThreadName, m_Session);
	}

	public void OnScopeColourModeChanged()
	{
		m_TimeSpanGraph.SetScopeColourMode(m_Settings.ScopeColourMode);
	}

	public void OnThreadScopeHeightChanged()
	{
		m_TimeSpanGraph.TimeSpanHeight = m_Settings.ThreadScopeHeight;
	}

	public void OnScopeColourChanged()
	{
		m_TimeSpanGraph.OnScopeColourChanged();
	}

	private void OnThreadRowPanelMouseMove(Point location)
	{
		ThreadInfoPanelMouseMove(location);
	}

	private void OnThreadRowPanelMouseDown(MouseButtons button)
	{
		if (button == MouseButtons.Right)
		{
			m_ThreadContextMenuStrip.Show(Cursor.Position);
		}
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
		this.components = new System.ComponentModel.Container();
		this.m_TimeSpanGraphParentPanel = new System.Windows.Forms.Panel();
		this.m_ThreadContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.hideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
		this.m_CollapseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ExpandMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
		this.moveUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.moveDownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
		this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
		this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ThreadInfoPanel = new FramePro.ThreadRowPanel();
		this.m_ThreadContextMenuStrip.SuspendLayout();
		base.SuspendLayout();
		this.m_TimeSpanGraphParentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_TimeSpanGraphParentPanel.Location = new System.Drawing.Point(225, 0);
		this.m_TimeSpanGraphParentPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TimeSpanGraphParentPanel.Name = "m_TimeSpanGraphParentPanel";
		this.m_TimeSpanGraphParentPanel.Size = new System.Drawing.Size(1289, 206);
		this.m_TimeSpanGraphParentPanel.TabIndex = 1;
		this.m_ThreadContextMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
		this.m_ThreadContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[12]
		{
			this.hideToolStripMenuItem, this.toolStripSeparator1, this.m_CollapseMenuItem, this.m_ExpandMenuItem, this.toolStripSeparator4, this.moveUpToolStripMenuItem, this.moveDownToolStripMenuItem, this.toolStripSeparator3, this.collapseAllToolStripMenuItem, this.expandAllToolStripMenuItem,
			this.toolStripSeparator2, this.filterToolStripMenuItem
		});
		this.m_ThreadContextMenuStrip.Name = "m_ThreadContextMenuStrip";
		this.m_ThreadContextMenuStrip.Size = new System.Drawing.Size(182, 284);
		this.m_ThreadContextMenuStrip.Closed += new System.Windows.Forms.ToolStripDropDownClosedEventHandler(ContextMenuClosed);
		this.hideToolStripMenuItem.Name = "hideToolStripMenuItem";
		this.hideToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.hideToolStripMenuItem.Text = "Hide";
		this.hideToolStripMenuItem.Click += new System.EventHandler(HideThreadMenuItemClick);
		this.toolStripSeparator1.Name = "toolStripSeparator1";
		this.toolStripSeparator1.Size = new System.Drawing.Size(178, 6);
		this.m_CollapseMenuItem.Name = "m_CollapseMenuItem";
		this.m_CollapseMenuItem.Size = new System.Drawing.Size(181, 32);
		this.m_CollapseMenuItem.Text = "Collapse";
		this.m_CollapseMenuItem.Click += new System.EventHandler(CollapseMenuItemClicked);
		this.m_ExpandMenuItem.Name = "m_ExpandMenuItem";
		this.m_ExpandMenuItem.Size = new System.Drawing.Size(181, 32);
		this.m_ExpandMenuItem.Text = "Expand";
		this.m_ExpandMenuItem.Click += new System.EventHandler(ExpandMenuItemClicked);
		this.toolStripSeparator4.Name = "toolStripSeparator4";
		this.toolStripSeparator4.Size = new System.Drawing.Size(178, 6);
		this.moveUpToolStripMenuItem.Name = "moveUpToolStripMenuItem";
		this.moveUpToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.moveUpToolStripMenuItem.Text = "Move Up";
		this.moveUpToolStripMenuItem.Click += new System.EventHandler(MoveThreadUpMenuItemClicked);
		this.moveDownToolStripMenuItem.Name = "moveDownToolStripMenuItem";
		this.moveDownToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.moveDownToolStripMenuItem.Text = "Move Down";
		this.moveDownToolStripMenuItem.Click += new System.EventHandler(MoveDownMenuItemClicked);
		this.toolStripSeparator3.Name = "toolStripSeparator3";
		this.toolStripSeparator3.Size = new System.Drawing.Size(178, 6);
		this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
		this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.collapseAllToolStripMenuItem.Text = "Collapse All";
		this.collapseAllToolStripMenuItem.Click += new System.EventHandler(CollapseAllMenuItemClicked);
		this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
		this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.expandAllToolStripMenuItem.Text = "Expand All";
		this.expandAllToolStripMenuItem.Click += new System.EventHandler(ExpandAllThreadsMenuItem);
		this.toolStripSeparator2.Name = "toolStripSeparator2";
		this.toolStripSeparator2.Size = new System.Drawing.Size(178, 6);
		this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
		this.filterToolStripMenuItem.Size = new System.Drawing.Size(181, 32);
		this.filterToolStripMenuItem.Text = "Settings...";
		this.filterToolStripMenuItem.Click += new System.EventHandler(FilterThreadMenuItemClicked);
		this.m_ThreadInfoPanel.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_ThreadInfoPanel.Location = new System.Drawing.Point(0, 0);
		this.m_ThreadInfoPanel.Name = "m_ThreadInfoPanel";
		this.m_ThreadInfoPanel.Size = new System.Drawing.Size(225, 206);
		this.m_ThreadInfoPanel.TabIndex = 0;
		this.m_ThreadInfoPanel.ThreadName = "";
		this.m_ThreadInfoPanel.CollapseExpandToggle += new FramePro.ThreadRowPanelCollapseExpandToggleHandler(ThreadNamePanelCollapseExpandToggle);
		this.m_ThreadInfoPanel.ThreadRowPanelMouseMove += new FramePro.ThreadRowPanelMouseMoveHandler(OnThreadRowPanelMouseMove);
		this.m_ThreadInfoPanel.ThreadRowPanelMouseDown += new FramePro.ThreadRowPanelMouseDownHandler(OnThreadRowPanelMouseDown);
		this.m_ThreadInfoPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(ThreadInfoPanelMouseClick);
		this.m_ThreadInfoPanel.MouseLeave += new System.EventHandler(ThreadInfoPanelMouseLeave);
		base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_TimeSpanGraphParentPanel);
		base.Controls.Add(this.m_ThreadInfoPanel);
		base.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		base.Name = "ThreadTimeSpanGraph";
		base.Size = new System.Drawing.Size(1514, 206);
		this.m_ThreadContextMenuStrip.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
