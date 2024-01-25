using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace FramePro;

internal class CoreGraphPanel : UserControl
{
	private Session m_Session;

	private Settings m_Settings;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private bool m_ShowContextSwitches;

	private IContainer components;

	private Panel m_TopPanel;

	private CheckBox m_DisplayContextSwitchesCheckBox;

	private Panel m_MainPanel;

	private CoreGraph m_CoreGraph;

	private BackgroundWorker backgroundWorker1;

	private NotShowingContextSwitchesMessageBox m_NotShowingContextSwitchesMessageBox;

	private CorePanel m_CorePanel;

	private CheckBox m_DisplayWaitEventsCheckBox;

	public bool Active
	{
		get
		{
			return m_CoreGraph.Active;
		}
		set
		{
			m_CoreGraph.Active = value;
		}
	}

	public bool ContextSwitchesVisible
	{
		get
		{
			return m_ShowContextSwitches;
		}
		set
		{
			if (m_ShowContextSwitches != value)
			{
				m_ShowContextSwitches = value;
				UpdateShowContextSwitchesCheckBox();
			}
		}
	}

	public bool WaitEventsVisible
	{
		get
		{
			return m_CoreGraph.ShowWaitEvents;
		}
		set
		{
			m_CoreGraph.ShowWaitEvents = value;
			m_DisplayWaitEventsCheckBox.Checked = value;
		}
	}

	public bool MouseHoverEnabled
	{
		get
		{
			return m_CoreGraph.MouseHoverEnabled;
		}
		set
		{
			m_CoreGraph.MouseHoverEnabled = value;
		}
	}

	public Rectangle CoreGraphClientRectangle => new Rectangle(m_CoreGraph.Location, m_CoreGraph.Size);

	public bool NeedsUpdate => m_CoreGraph.NeedsUpdate;

	public event ContextSwitchesVisibleChangedHandler ContextSwitchesVisibleChanged;

	public event WaitEventsVisibleChangedHandler WaitEventsVisibleChanged;

	public event CoreGraphTimeRangeChangedHandler TimeRangeChanged;

	public event CoreGraphShowThreadHandler CoreGraphShowThread;

	public event MeasureLineChangedHandler MeasureLineChanged;

	public event SelectedTimeSpanChangedHandler SelectedTimeSpanChanged;

	public event StopTrackingEndHandler StopTrackingEnd;

	public CoreGraphPanel()
	{
		InitializeComponent();
		m_CorePanel.SetCoreGraph(m_CoreGraph);
	}

	public void SetSession(Session session)
	{
		m_Session = session;
		HookSession();
		m_CoreGraph.SetSession(session);
		m_CorePanel.SetSession(session);
		UpdateNotShowingContextSwitchesMessageBox();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
		m_CoreGraph.SetSettings(settings);
	}

	private void UpdateNotShowingContextSwitchesMessageBox()
	{
		UpdateShowContextSwitchesCheckBox();
		m_NotShowingContextSwitchesMessageBox.Visible = !m_Session.RecordingContextSwitches && !m_Settings.NotTrackingContextSwitchesDismissed;
	}

	private void NotShowingContextSwitchesWantsToClose()
	{
		m_TopPanel.Controls.Remove(m_NotShowingContextSwitchesMessageBox);
		m_Settings.NotTrackingContextSwitchesDismissed = true;
		m_Settings.Write();
	}

	private void NotShowingContextSwitchesWantsToLoadFile()
	{
		if (MainForm.Inst.LoadContextSwitchFile())
		{
			m_TopPanel.Controls.Remove(m_NotShowingContextSwitchesMessageBox);
			UpdateGraph(force: true);
		}
	}

	private void HookSession()
	{
		m_Session.RecordingContextSwitchesChanged += RecordingContextSwitchesChanged;
	}

	private void RecordingContextSwitchesChanged()
	{
		m_ControlTaskDispatcher.QueueTask(RecordingContextSwitchesChanged_MainThread);
	}

	private void RecordingContextSwitchesChanged_MainThread()
	{
		UpdateNotShowingContextSwitchesMessageBox();
	}

	public void Initialise()
	{
		UpdateNotShowingContextSwitchesMessageBox();
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	public void OnDisconnected()
	{
		m_ControlTaskDispatcher.QueueTask(OnDisconnected_MainThread);
	}

	private void OnDisconnected_MainThread()
	{
		UpdateNotShowingContextSwitchesMessageBox();
		m_CoreGraph.OnDisconnected();
	}

	public void OnETLTraceFinished()
	{
		UpdateNotShowingContextSwitchesMessageBox();
		m_CoreGraph.OnETLTraceFinished();
	}

	private void UpdateShowContextSwitchesCheckBox()
	{
		bool flag = m_ShowContextSwitches && m_Session != null && m_Session.RecordingContextSwitches;
		m_CoreGraph.ContextSwitchesVisible = flag;
		m_CoreGraph.Refresh();
		m_DisplayContextSwitchesCheckBox.Checked = flag;
	}

	private void ContextSwitchesCheckBoxCheckChanged(object sender, EventArgs e)
	{
		if (m_Session == null)
		{
			return;
		}
		if (m_Session.RecordingContextSwitches)
		{
			ContextSwitchesVisible = m_DisplayContextSwitchesCheckBox.Checked;
			if (this.ContextSwitchesVisibleChanged != null)
			{
				this.ContextSwitchesVisibleChanged();
			}
		}
		else if (m_DisplayContextSwitchesCheckBox.Checked)
		{
			new NoContextSwitchesWarningDialog().ShowDialog();
			m_DisplayContextSwitchesCheckBox.Checked = false;
		}
	}

	private void DisplayWaitEventsCheckedChanged(object sender, EventArgs e)
	{
		m_CoreGraph.ShowWaitEvents = m_DisplayWaitEventsCheckBox.Checked;
		if (this.WaitEventsVisibleChanged != null)
		{
			this.WaitEventsVisibleChanged();
		}
	}

	public void UpdateThreadColours()
	{
		m_CoreGraph.UpdateThreadColours();
	}

	public void SetTimeRange(TimeRange time_range)
	{
		m_CoreGraph.SetTimeRange(time_range);
	}

	public void SetHighlightedTimeSpans(Set<long> highlighted_time_spans)
	{
		m_CoreGraph.SetHighlightedTimeSpans(highlighted_time_spans);
	}

	public void SetMeasureLine(int start_x, int end_x)
	{
		m_CoreGraph.SetMeasureLine(start_x, end_x);
	}

	private void CoreGraphTimeRangeChanged(TimeRange time_range)
	{
		if (this.TimeRangeChanged != null)
		{
			this.TimeRangeChanged(time_range);
		}
	}

	private void CoreGraphShowThreadEvent(int thread_id)
	{
		if (this.CoreGraphShowThread != null)
		{
			this.CoreGraphShowThread(thread_id);
		}
	}

	private void CoreGraphScrollChanged(int scroll_y)
	{
		m_CorePanel.SetScrollY(scroll_y);
	}

	private void MeasureLineChangedEvent(int start_x, int end_x)
	{
		if (this.MeasureLineChanged != null)
		{
			this.MeasureLineChanged(start_x, end_x);
		}
	}

	public void SelectTimeSpan(long time_span_name)
	{
		m_CoreGraph.SelectTimeSpan(time_span_name);
	}

	private void OnSelectedTimeSpanChanged(long time_span_name, TimeSpan time_span)
	{
		if (this.SelectedTimeSpanChanged != null)
		{
			this.SelectedTimeSpanChanged(time_span_name, null);
		}
	}

	private void CoreGraphStopTrackingEnd()
	{
		if (this.StopTrackingEnd != null)
		{
			this.StopTrackingEnd();
		}
	}

	public Point PointToCoreGraphClient(Point screen_point)
	{
		return m_CoreGraph.PointToClient(screen_point);
	}

	public void UpdateGraph(bool force)
	{
		m_CoreGraph.UpdateGraph(force);
	}

	public void OnScopeColourModeChanged()
	{
		m_CoreGraph.OnScopeColourModeChanged();
	}

	public void OnScopeColourChanged()
	{
		m_CoreGraph.OnScopeColourChanged();
	}

	public int GetDesiredHeight()
	{
		return m_CoreGraph.GetDesiredHeight() + m_MainPanel.Top;
	}

	private void InitializeComponent()
	{
		this.m_TopPanel = new System.Windows.Forms.Panel();
		this.m_DisplayWaitEventsCheckBox = new System.Windows.Forms.CheckBox();
		this.m_NotShowingContextSwitchesMessageBox = new FramePro.NotShowingContextSwitchesMessageBox();
		this.m_DisplayContextSwitchesCheckBox = new System.Windows.Forms.CheckBox();
		this.m_MainPanel = new System.Windows.Forms.Panel();
		this.m_CoreGraph = new FramePro.CoreGraph();
		this.m_CorePanel = new FramePro.CorePanel();
		this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
		this.m_TopPanel.SuspendLayout();
		this.m_MainPanel.SuspendLayout();
		base.SuspendLayout();
		this.m_TopPanel.BackColor = System.Drawing.Color.FromArgb(81, 81, 81);
		this.m_TopPanel.Controls.Add(this.m_DisplayWaitEventsCheckBox);
		this.m_TopPanel.Controls.Add(this.m_NotShowingContextSwitchesMessageBox);
		this.m_TopPanel.Controls.Add(this.m_DisplayContextSwitchesCheckBox);
		this.m_TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TopPanel.Location = new System.Drawing.Point(0, 0);
		this.m_TopPanel.Name = "m_TopPanel";
		this.m_TopPanel.Size = new System.Drawing.Size(947, 21);
		this.m_TopPanel.TabIndex = 0;
		this.m_DisplayWaitEventsCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.m_DisplayWaitEventsCheckBox.AutoSize = true;
		this.m_DisplayWaitEventsCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DisplayWaitEventsCheckBox.ForeColor = System.Drawing.Color.White;
		this.m_DisplayWaitEventsCheckBox.Location = new System.Drawing.Point(631, 2);
		this.m_DisplayWaitEventsCheckBox.Name = "m_DisplayWaitEventsCheckBox";
		this.m_DisplayWaitEventsCheckBox.Size = new System.Drawing.Size(126, 17);
		this.m_DisplayWaitEventsCheckBox.TabIndex = 3;
		this.m_DisplayWaitEventsCheckBox.Text = "Display Wait Events";
		this.m_DisplayWaitEventsCheckBox.UseVisualStyleBackColor = true;
		this.m_DisplayWaitEventsCheckBox.CheckedChanged += new System.EventHandler(DisplayWaitEventsCheckedChanged);
		this.m_NotShowingContextSwitchesMessageBox.BackColor = System.Drawing.Color.FromArgb(35, 41, 86);
		this.m_NotShowingContextSwitchesMessageBox.Location = new System.Drawing.Point(0, 0);
		this.m_NotShowingContextSwitchesMessageBox.Name = "m_NotShowingContextSwitchesMessageBox";
		this.m_NotShowingContextSwitchesMessageBox.Size = new System.Drawing.Size(553, 20);
		this.m_NotShowingContextSwitchesMessageBox.TabIndex = 2;
		this.m_NotShowingContextSwitchesMessageBox.Visible = false;
		this.m_NotShowingContextSwitchesMessageBox.WantsToClose += new FramePro.ContextSwitchMessageBoxWantsToCloseHandler(NotShowingContextSwitchesWantsToClose);
		this.m_NotShowingContextSwitchesMessageBox.WantsToLoadFile += new FramePro.ContextSwitchMessageBoxWantsToLoadFile(NotShowingContextSwitchesWantsToLoadFile);
		this.m_DisplayContextSwitchesCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.m_DisplayContextSwitchesCheckBox.AutoSize = true;
		this.m_DisplayContextSwitchesCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DisplayContextSwitchesCheckBox.ForeColor = System.Drawing.Color.White;
		this.m_DisplayContextSwitchesCheckBox.Location = new System.Drawing.Point(790, 2);
		this.m_DisplayContextSwitchesCheckBox.Name = "m_DisplayContextSwitchesCheckBox";
		this.m_DisplayContextSwitchesCheckBox.Size = new System.Drawing.Size(154, 17);
		this.m_DisplayContextSwitchesCheckBox.TabIndex = 0;
		this.m_DisplayContextSwitchesCheckBox.Text = "Display Context Switches";
		this.m_DisplayContextSwitchesCheckBox.UseVisualStyleBackColor = true;
		this.m_DisplayContextSwitchesCheckBox.CheckedChanged += new System.EventHandler(ContextSwitchesCheckBoxCheckChanged);
		this.m_MainPanel.Controls.Add(this.m_CoreGraph);
		this.m_MainPanel.Controls.Add(this.m_CorePanel);
		this.m_MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_MainPanel.Location = new System.Drawing.Point(0, 21);
		this.m_MainPanel.Name = "m_MainPanel";
		this.m_MainPanel.Size = new System.Drawing.Size(947, 155);
		this.m_MainPanel.TabIndex = 1;
		this.m_CoreGraph.Active = false;
		this.m_CoreGraph.ContextSwitchesVisible = false;
		this.m_CoreGraph.CoreRectHeight = 6;
		this.m_CoreGraph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_CoreGraph.Location = new System.Drawing.Point(150, 0);
		this.m_CoreGraph.MouseHoverEnabled = true;
		this.m_CoreGraph.Name = "m_CoreGraph";
		this.m_CoreGraph.NotShowingHeirachyCoreHeight = 20;
		this.m_CoreGraph.ShowHeirachy = false;
		this.m_CoreGraph.ShowWaitEvents = false;
		this.m_CoreGraph.Size = new System.Drawing.Size(797, 155);
		this.m_CoreGraph.TabIndex = 0;
		this.m_CoreGraph.TimeRangeChanged += new FramePro.CoreGraphTimeRangeChangedHandler(CoreGraphTimeRangeChanged);
		this.m_CoreGraph.CoreGraphShowThread += new FramePro.CoreGraphShowThreadHandler(CoreGraphShowThreadEvent);
		this.m_CoreGraph.MeasureLineChanged += new FramePro.MeasureLineChangedHandler(MeasureLineChangedEvent);
		this.m_CoreGraph.CoreScrollChanged += new FramePro.CoreScrollChangedHandler(CoreGraphScrollChanged);
		this.m_CoreGraph.SelectedTimeSpanChanged += new FramePro.SelectedTimeSpanChangedHandler(OnSelectedTimeSpanChanged);
		this.m_CoreGraph.StopTrackingEnd += new FramePro.StopTrackingEndHandler(CoreGraphStopTrackingEnd);
		this.m_CorePanel.BackColor = System.Drawing.Color.FromArgb(81, 81, 81);
		this.m_CorePanel.CoreYGap = 20;
		this.m_CorePanel.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_CorePanel.ForeColor = System.Drawing.Color.White;
		this.m_CorePanel.Location = new System.Drawing.Point(0, 0);
		this.m_CorePanel.Name = "m_CorePanel";
		this.m_CorePanel.Size = new System.Drawing.Size(150, 155);
		this.m_CorePanel.TabIndex = 1;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_MainPanel);
		base.Controls.Add(this.m_TopPanel);
		base.Name = "CoreGraphPanel";
		base.Size = new System.Drawing.Size(947, 176);
		this.m_TopPanel.ResumeLayout(false);
		this.m_TopPanel.PerformLayout();
		this.m_MainPanel.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
