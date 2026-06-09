using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class MainSessionView : UserControl
{
	private Session m_Session;

	private Settings m_Settings;

	private ThreadsView m_ThreadsView;

	private CoresView m_CoresView;

	private ScopesView m_ScopesView;

	private CustomStatsView m_CustomStatsView;

	private LogView m_LogView;

	private List<SessionView> m_Views = new List<SessionView>();

	private long m_SelectionStartTime;

	private long m_SelectionEndTime;

	private System.Windows.Forms.Timer m_Timer = new System.Windows.Forms.Timer();

	private const int m_TimerTickinterval = 30;

	private bool m_TrackEnd;

	private int m_ThreadAndCoreViewsUpdateInterval;

	private int m_LastWriteSettingsTime;

	private object m_SettingsDirtyLock = new object();

	private bool m_SettingsDirty;

	private int m_HighlightedTimeSpanCount;

	private int m_LastUpdateHighlightCountTime;

	private const int m_DefaultPixelsPerMs = 20;

	private bool m_InSelectedTimeRangeChanged;

	private const double m_ZoomMultiplier = 1.25;

	private const double m_MinZoomScale = 0.02;

	private long m_FrameGraphRangeStart;

	private double m_FrameGraphViewScale;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private IContainer components;

	private Panel m_MainPanel;

	private SessionViewTabsPanel m_TabsPanel;

	public ThreadsView ThreadsView => m_ThreadsView;

	public CoresView CoresView => m_CoresView;

	public ScopesView ScopesView => m_ScopesView;

	public CustomStatsView CustomStatsView => m_CustomStatsView;

	public LogView LogView => m_LogView;

	public Session Session => m_Session;

	public SessionView ActiveView => m_TabsPanel.ActiveTab;

	public bool TrackEnd
	{
		get
		{
			return m_TrackEnd;
		}
		set
		{
			if (m_TrackEnd == value)
			{
				return;
			}
			m_TrackEnd = value;
			foreach (SessionView view in m_Views)
			{
				view.TrackEnd = value;
			}
			if (this.TrackEndChanged != null)
			{
				this.TrackEndChanged();
			}
		}
	}

	public static double ZoomMultiplier => 1.25;

	public static double MinZoomScale => 0.02;

	public event ActiveViewChangedHandler ActiveViewChanged;

	public event TrackEndChangedHandler TrackEndChanged;

	public event HighlightedTimeSpanCountChangedHandler HighlightedTimeSpanCountChanged;

	public MainSessionView(Session session, Settings settings)
	{
		m_Session = session;
		m_Settings = settings;
		InitializeComponent();
		HookSessionEvents();
		m_Session.FilenameChanged += SessionFilenameChanged;
		m_ThreadsView = new ThreadsView(session, settings);
		m_CoresView = new CoresView(session, settings);
		m_ScopesView = new ScopesView(session, settings);
		m_CustomStatsView = new CustomStatsView(session, settings);
		m_LogView = new LogView(session);
		m_ThreadsView.SelectedTimeRangeChanged += VisibleTimeRangeChanged;
		m_ThreadsView.SettingsChanged += SettingsChanged;
		m_ThreadsView.FrameTargetMSChanged += FrameTargetMSChanged;
		m_ThreadsView.TimeSpanTargetMSChanged += TimeSpanTargetMSChanged;
		m_ThreadsView.SelectedTimeSpanChanged += ThreadsViewSelectedTimeSpanChanged;
		m_ThreadsView.SelectedRangeChanged += OnSelectedRangeChanged;
		m_ThreadsView.FrameGraphRangeChanged += OnFrameGraphRangeChanged;
		m_CoresView.SelectedTimeRangeChanged += VisibleTimeRangeChanged;
		m_CoresView.ShowThread += CoresViewShowThread;
		m_CoresView.FrameTargetMSChanged += FrameTargetMSChanged;
		m_CoresView.TimeSpanTargetMSChanged += TimeSpanTargetMSChanged;
		m_CoresView.SelectedTimeSpanChanged += CoresViewSelectedTimeSpanChanged;
		m_CoresView.SelectedRangeChanged += OnSelectedRangeChanged;
		m_CoresView.FrameGraphRangeChanged += OnFrameGraphRangeChanged;
		m_LogView.ShowFrame += LogViewShowFrame;
		SuspendLayout();
		AddSessionView(m_ThreadsView);
		AddSessionView(m_CoresView);
		AddSessionView(m_ScopesView);
		AddSessionView(m_CustomStatsView);
		AddSessionView(m_LogView);
		ActivateTab(m_ThreadsView);
		ResumeLayout();
		UpdateWindowTitle();
		TrackEnd = Environment.ProcessorCount > 2 && m_Session.Connected && (!m_Session.IsLocalConnnection || Environment.ProcessorCount >= 8);
		m_Timer.Interval = 30;
		m_Timer.Tick += TimerTick;
		m_Timer.Start();
	}

	private void OnFrameGraphRangeChanged(Control sender, long start_frame_x, long end_frame_x, double view_scale)
	{
		m_FrameGraphRangeStart = start_frame_x;
		m_FrameGraphViewScale = view_scale;
	}

	private void OnSelectedRangeChanged(int start_frame_index, int end_frame_index)
	{
		m_ThreadsView.SetSelectedRange(start_frame_index, end_frame_index);
		m_CoresView.SetSelectedRange(start_frame_index, end_frame_index);
	}

	public void OnScopeColourModeChanged()
	{
		m_ThreadsView.OnScopeColourModeChanged();
		m_CoresView.OnScopeColourModeChanged();
	}

	private void ThreadsViewSelectedTimeSpanChanged(long time_span_name, TimeSpan time_span)
	{
		m_CoresView.SetSelectedTimeSpan(time_span_name);
	}

	private void CoresViewSelectedTimeSpanChanged(long time_span_name, TimeSpan time_span)
	{
		m_ThreadsView.SetSelectedTimeSpan(time_span_name);
	}

	private void TimeSpanTargetMSChanged(SessionView sender)
	{
		if (sender != m_ThreadsView)
		{
			m_ThreadsView.OnTimeSpanTargetMSChanged();
		}
		if (sender != m_CoresView)
		{
			m_CoresView.OnTimeSpanTargetMSChanged();
		}
	}

	private void FrameTargetMSChanged(SessionView sender)
	{
		if (sender != m_ThreadsView)
		{
			m_ThreadsView.OnTargetMSChanged();
		}
		if (sender != m_CoresView)
		{
			m_CoresView.OnTargetMSChanged();
		}
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void CoresViewShowThread(int thread_id)
	{
		m_TabsPanel.ActivateTab("Threads");
		string threadName = m_Session.GetThreadName(thread_id);
		m_ThreadsView.ScrollThreadIntoView(threadName);
	}

	protected override void Dispose(bool disposing)
	{
		m_Session.Close();
		m_Timer.Stop();
		m_Timer.Tick -= TimerTick;
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void SettingsChanged()
	{
		lock (m_SettingsDirtyLock)
		{
			m_SettingsDirty = true;
		}
	}

	private void ViewRequestedStopTrackingEnd()
	{
		TrackEnd = false;
	}

	private void HookSessionEvents()
	{
		m_Session.Disconnected += OnDisconnected;
		m_Session.ReadStarted += SessionReadStarted;
		m_Session.SessionIsReady += SessionIsReady;
		m_Session.ShowContextSwitchWarning += ShowContextSwitchWarning;
	}

	private void ShowContextSwitchWarning(string error)
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			ShowContextSwitchWarning_Main(error);
		});
	}

	private void ShowContextSwitchWarning_Main(string error)
	{
		////if (m_Settings.ShowContextSwitchWarningBox)
		////{
		////	ContextSwitchErrorBox contextSwitchErrorBox = new ContextSwitchErrorBox(error);
		////	contextSwitchErrorBox.ShowDialog(MainForm.Inst);
		////	if (contextSwitchErrorBox.DontShowAgain)
		////	{
		////		m_Settings.ShowContextSwitchWarningBox = false;
		////		m_Settings.Write();
		////	}
		////}
		Log.WriteLine($"Context switch not support here:{error}");
	}

	private void SessionIsReady()
	{
		m_ControlTaskDispatcher.QueueTask(SessionIsReady_Main);
	}

	private void SessionIsReady_Main()
	{
		SetDefaultSelectDuration();
		GotoEnd();
	}

	private void SetDefaultSelectDuration()
	{
		long num = base.Width / 20 * m_Session.TimerFrequency / 1000;
		long firstFrameTime = m_Session.FirstFrameTime;
		long end_time = firstFrameTime + num;
		VisibleTimeRangeChanged(null, firstFrameTime, end_time);
	}

	private void OnDisconnected()
	{
		m_ControlTaskDispatcher.QueueTask(OnDisconnected_Main);
	}

	private void OnDisconnected_Main()
	{
		TrackEnd = false;
	}

	private void SessionReadStarted(bool is_dump)
	{
		if (is_dump)
		{
			m_ControlTaskDispatcher.QueueTask(delegate
			{
				SessionReadStarted_Main(is_dump);
			});
		}
	}

	private void SessionReadStarted_Main(bool is_dump)
	{
		if (is_dump)
		{
			m_Timer.Interval = 1000;
			TrackEnd = false;
		}
	}

	private void TimerTick(object sender, EventArgs e)
	{
		ActiveView.UpdateView();
		if ((m_Session.Connected || m_Session.ProcessingPackets) && TrackEnd && MainForm.Inst.WindowState != FormWindowState.Minimized)
		{
			m_ThreadAndCoreViewsUpdateInterval = (m_ThreadAndCoreViewsUpdateInterval + 1) % 8;
			bool refresh_thread_and_core_views = m_ThreadAndCoreViewsUpdateInterval == 0;
			GotoEnd(refresh_thread_and_core_views);
		}
		if (Environment.TickCount - m_LastUpdateHighlightCountTime > 1000)
		{
			UpdateHighlightedTimeSpanCount();
			m_LastUpdateHighlightCountTime = Environment.TickCount;
		}
		lock (m_SettingsDirtyLock)
		{
			if (m_SettingsDirty && Environment.TickCount - m_LastWriteSettingsTime > 1000)
			{
				m_Settings.Write();
				m_SettingsDirty = false;
				m_LastWriteSettingsTime = Environment.TickCount;
			}
		}
	}

	private void VisibleTimeRangeChanged(SessionView view, long start_time, long end_time)
	{
		VisibleTimeRangeChanged(view, start_time, end_time, refresh_thread_and_core_views: true);
	}

	private void VisibleTimeRangeChanged(SessionView view, long start_time, long end_time, bool refresh_thread_and_core_views)
	{
		if (m_InSelectedTimeRangeChanged)
		{
			return;
		}
		m_InSelectedTimeRangeChanged = true;
		if (start_time != m_SelectionStartTime || end_time != m_SelectionEndTime)
		{
			m_SelectionStartTime = start_time;
			m_SelectionEndTime = end_time;
			if (view != m_ThreadsView)
			{
				m_ThreadsView.SetVisibleTimeRange(start_time, end_time, refresh_thread_and_core_views);
			}
			if (view != m_CoresView)
			{
				m_CoresView.SetVisibleTimeRange(start_time, end_time, refresh_thread_and_core_views);
			}
		}
		m_InSelectedTimeRangeChanged = false;
	}

	private void SessionFilenameChanged()
	{
		UpdateWindowTitle();
	}

	private void UpdateWindowTitle()
	{
		Text = Path.GetFileNameWithoutExtension(m_Session.Filename);
	}

	private void AddSessionView(SessionView session_view)
	{
		session_view.Dock = DockStyle.Fill;
		m_MainPanel.Controls.Add(session_view);
		m_TabsPanel.AddTab(session_view);
		m_Views.Add(session_view);
		session_view.StopTrackingEndEvent += StopTrackingEnd;
	}

	public void OnScopeColourChanged()
	{
		foreach (SessionView view in m_Views)
		{
			view.OnScopeColourChanged();
		}
	}

	public void OnCustomStatInfoChanged()
	{
		foreach (SessionView view in m_Views)
		{
			view.OnCustomStatInfoChanged();
		}
	}

	public void OnCustomStatColourChanged()
	{
		foreach (SessionView view in m_Views)
		{
			view.OnCustomStatColourChanged();
		}
	}

	private void StopTrackingEnd()
	{
		TrackEnd = false;
	}

	private void ActivateTab(SessionView session_view)
	{
		ActivateTab(session_view.ViewName);
	}

	public void ActivateTab(string tab_name)
	{
		m_TabsPanel.ActivateTab(tab_name);
	}

	public ViewT GetView<ViewT>() where ViewT : SessionView
	{
		foreach (SessionView view in m_Views)
		{
			if (view is ViewT)
			{
				return (ViewT)view;
			}
		}
		return null;
	}

	private void ActiveTabChangedEvent(SessionView view)
	{
		UpdateHighlightedTimeSpanCount();
		if (this.ActiveViewChanged != null)
		{
			this.ActiveViewChanged(view);
		}
	}

	public void OnMouseWheel(int delta, Point location)
	{
		Point p = PointToScreen(location);
		if (ActiveView != null)
		{
			ActiveView.OnMouseWheel(delta, ActiveView.PointToClient(p));
		}
	}

	public void HighlightTimeSpans(string filter)
	{
		m_ThreadsView.HighlightTimeSpans(filter);
		m_CoresView.HighlightTimeSpans(filter);
	}

	private bool IsViewActive(SessionView view)
	{
		return view == m_TabsPanel.ActiveTab;
	}

	private void UpdateHighlightedTimeSpanCount()
	{
		int highlightedTimeSpanCount = ActiveView.HighlightedTimeSpanCount;
		if (highlightedTimeSpanCount != m_HighlightedTimeSpanCount)
		{
			m_HighlightedTimeSpanCount = highlightedTimeSpanCount;
			if (this.HighlightedTimeSpanCountChanged != null)
			{
				this.HighlightedTimeSpanCountChanged(highlightedTimeSpanCount);
			}
		}
	}

	public void GotoStart()
	{
		long num = m_SelectionEndTime - m_SelectionStartTime;
		VisibleTimeRangeChanged(null, m_Session.FirstFrameTime, m_Session.FirstFrameTime + num);
	}

	public void GotoEnd()
	{
		GotoEnd(refresh_thread_and_core_views: true);
	}

	public void GotoEnd(bool refresh_thread_and_core_views)
	{
		long num = m_SelectionEndTime - m_SelectionStartTime;
		VisibleTimeRangeChanged(null, m_Session.LastFrameEndTime - num, m_Session.LastFrameEndTime, refresh_thread_and_core_views);
	}

	public void ApplySessionViewSaveData(SessionViewSaveData session_view_save_data)
	{
		if (session_view_save_data.IsValid)
		{
			VisibleTimeRangeChanged(null, session_view_save_data.m_FrameGraphVisibleRangeStart, session_view_save_data.m_FrameGraphVisibleRangeEnd);
			if (session_view_save_data.m_FrameGraphStart != 0L)
			{
				m_ThreadsView.SetFrameGraphRange(session_view_save_data.m_FrameGraphStart, session_view_save_data.m_FrameGraphViewScale);
				m_CoresView.SetFrameGraphRange(session_view_save_data.m_FrameGraphStart, session_view_save_data.m_FrameGraphViewScale);
			}
		}
	}

	public void GetSessionViewSaveData(SessionViewSaveData session_view_save_data)
	{
		session_view_save_data.m_FrameGraphVisibleRangeStart = m_SelectionStartTime;
		session_view_save_data.m_FrameGraphVisibleRangeEnd = m_SelectionEndTime;
		session_view_save_data.m_FrameGraphStart = m_FrameGraphRangeStart;
		session_view_save_data.m_FrameGraphViewScale = m_FrameGraphViewScale;
	}

	private void LogViewShowFrame(long time)
	{
		m_TabsPanel.ActivateTab("Threads");
		int frameIndex = m_Session.GetFrameIndex(time);
		if (frameIndex != -1)
		{
			Frame frame = m_Session.GetFrame(frameIndex);
			if (frame != null)
			{
				m_ThreadsView.SetVisibleTimeRange(frame.StartTime, frame.EndTime, refresh_thread_and_core_views: true);
			}
		}
	}

	private void InitializeComponent()
	{
		this.m_MainPanel = new System.Windows.Forms.Panel();
		this.m_TabsPanel = new ProfilerStudy.SessionViewTabsPanel();
		base.SuspendLayout();
		this.m_MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_MainPanel.Location = new System.Drawing.Point(0, 0);
		this.m_MainPanel.Name = "m_MainPanel";
		this.m_MainPanel.Size = new System.Drawing.Size(1083, 625);
		this.m_MainPanel.TabIndex = 2;
		this.m_TabsPanel.BackColor = System.Drawing.Color.FromArgb(237, 237, 237);
		this.m_TabsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_TabsPanel.Location = new System.Drawing.Point(0, 625);
		this.m_TabsPanel.Name = "m_TabsPanel";
		this.m_TabsPanel.Size = new System.Drawing.Size(1083, 47);
		this.m_TabsPanel.TabIndex = 3;
		this.m_TabsPanel.SessionViewActiveTabChanged += new ProfilerStudy.SessionViewActiveTabChangedHandler(ActiveTabChangedEvent);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_MainPanel);
		base.Controls.Add(this.m_TabsPanel);
		base.Name = "MainSessionView";
		base.Size = new System.Drawing.Size(1083, 672);
		base.ResumeLayout(false);
	}
}
