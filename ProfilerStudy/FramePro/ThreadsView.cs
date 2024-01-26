using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Editor;
using SCLCoreCLR;

namespace FramePro;

internal class ThreadsView : SessionView
{
	private Session m_Session;

	private Settings m_Settings;

	private Dictionary<string, ThreadTimeSpanGraph> m_ThreadTimeSpanGraphs = new Dictionary<string, ThreadTimeSpanGraph>();

	private List<int> m_Threads = new List<int>();

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private TimeRange m_SelectedTimeRange = new TimeRange();

	private const double m_DefaultScale = 10.0;

	private Set<long> m_HighlightedTimeSpans = new Set<long>();

	private string m_HighlightFilter;

	private const int m_ResizeThreadHeightDelay = 500;

	private int m_LastUpdateInfoPanelTime;

	private const int m_UpdateInfoPanelInterval = 1000;

	private const int m_ThreadInfoMouseWheelLargeJumpFraction = 3;

	private bool m_ThreadFilterTextBoxEmpty;

	private bool m_SessionIsReady;

	private long m_SelectedTimeSpan = -1L;

	private bool m_InSelectedTimeSpanChangedEventFunction;

	private const int m_UpdateCustomStatGraphsInterval = 100;

	private int m_LastUpdateCustomStatGraphsTime;

	private List<string> m_CustomStatGraphsVisible = new List<string>();

	private List<string> m_CustomStatGraphsVisibleTemp = new List<string>();

	private bool m_ShownTooManyThreadsWarning;

	private const int m_MinCoreGraphHeight = 175;

	private float m_DPIScale = 1f;

	private const int m_ThreadOrderTimerInterval = 200;

	private System.Windows.Forms.Timer m_ThreadOrderTimer = new System.Windows.Forms.Timer();

	private const int m_ThreadAddedTimerInterval = 200;

	private System.Windows.Forms.Timer m_ThreadAddedTimer = new System.Windows.Forms.Timer();

	private const int m_UpdateGraphHeightsInterval = 200;

	private System.Windows.Forms.Timer m_UpdateGraphHeightsTimer = new System.Windows.Forms.Timer();

	private ThreadIdMode m_ThreadIdMode;

	private IContainer components;

	private Panel m_TimelinePanel;

	private Timeline m_Timeline;

	private Panel m_CoreBackPanel;

	private Splitter m_CoreViewSplitter;

	private Panel m_TimeSpanGraphPanel;

	private Panel m_TimeSpanGraphPanelInner;

	private FrameGraphPanel m_FrameGraph;

	private VerticalLabelPanel verticalLabelPanel1;

	private VerticalLabelPanel verticalLabelPanel3;

	private VerticalLabelPanel verticalLabelPanel2;

	private Panel m_FrameGraphPanel;

	private VerticalLabelPanel verticalLabelPanel5;

	private InfoPanel m_InfoPanel;

	private Panel m_InfoPanelPanel;

	private VerticalLabelPanel verticalLabelPanel6;

	private Panel m_ThreadsPanel;

	private Panel panel4;

	private Splitter m_FrameGraphSplitter;

	private Splitter m_TimeSpanGraphSplitter;

	private LeftBackPanel timelineInfoPanel1;

	private LeftBackPanel leftBackPanel1;

	private VScrollBar m_VScrollBar;

	private System.Windows.Forms.Button button1;

	private System.Windows.Forms.Button button2;

	private TextBox m_ThreadFilterTextBox;

	private CoreGraphPanel m_CoreGraph;

	private FrameGraphYAxis m_FrameGraphYAxis;

	private SessionScrollBarPanel m_SessionScrollBarPanel;

	private Panel m_CustomGraphPanel;

	private LeftBackPanel leftBackPanel2;

	private VerticalLabelPanel verticalLabelPanel4;

	private Splitter m_CustomGraphPanelSplitter;

	private CustomStatsGraph m_CustomStatsGraph;

	private System.Windows.Forms.Button m_CustomStatSelectorButton;

	private TimeSpanGraphView m_TimespanGraphView;

	private Panel panel1;

	private ScopeDataGrid m_ScopeDataGrid;

	private Splitter m_DataGridSplitter;

	private Panel panel2;

	private FrameInfoPanel m_FrameInfoPanel;

	private Editor.Button m_ThreadIdModeButton;

	private ToolTip toolTip1;

	public override string ViewName => "Threads";

	public override Session Session => m_Session;

	public override int HighlightedTimeSpanCount
	{
		get
		{
			int num = 0;
			foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
			{
				num += value.TimeSpanGraph.HighlightedTimeSpanCount;
			}
			return num;
		}
	}

	public long SelectionStartTime => m_FrameGraph.VisibleStartTime;

	public long SelectionEndTime => m_FrameGraph.VisibleEndTime;

	private long SelectEndTime => m_SelectedTimeRange.m_StartTime + TimeSpanGraphWidth * m_SelectedTimeRange.m_TicksPerPixel;

	public bool InfoPanelVisible => m_Settings.ThreadsViewInfoPanelVisible;

	public bool FrameGraphVisible => m_Settings.ThreadsViewFrameGraphVisible;

	public bool TimeSpanGraphVisible => m_Settings.ThreadsViewTimeSpanGraphVisible;

	public bool CoreViewVisible => m_Settings.ThreadsViewCoreViewVisible;

	public bool CustomStatsVisible => m_Settings.ThreadsViewCustomStatsVisible;

	public bool DataGridVisible => m_Settings.ThreadsViewDataGridVisible;

	private int TimeSpanGraphWidth
	{
		get
		{
			if (m_ThreadTimeSpanGraphs.Count != 0)
			{
				using Dictionary<string, ThreadTimeSpanGraph>.ValueCollection.Enumerator enumerator = m_ThreadTimeSpanGraphs.Values.GetEnumerator();
				if (enumerator.MoveNext())
				{
					return enumerator.Current.TimeSpanGraph.ClientSize.Width;
				}
			}
			return m_TimeSpanGraphPanelInner.ClientSize.Width;
		}
	}

	private string ThreadFilterTextBoxText
	{
		get
		{
			if (!m_ThreadFilterTextBoxEmpty)
			{
				return m_ThreadFilterTextBox.Text;
			}
			return "";
		}
	}

	public TimeSpan DataGridTimeSpan
	{
		get
		{
			return m_ScopeDataGrid.TimeSpan;
		}
		set
		{
			m_ScopeDataGrid.TimeSpan = value;
		}
	}

	public event SectionVisibilityChangedHandler SectionVisibilityChanged;

	public event SelectetdTimeRangeChangedHandler SelectedTimeRangeChanged;

	public event SettingsChangedHandler SettingsChanged;

	public event TargetMSChangedHandler FrameTargetMSChanged;

	public event TargetMSChangedHandler TimeSpanTargetMSChanged;

	public event SelectedTimeSpanChangedHandler SelectedTimeSpanChanged;

	public event SelectedRangeChangedHandler SelectedRangeChanged;

	public event RangeChangedHandler FrameGraphRangeChanged;

	public ThreadsView()
	{
	}

	public ThreadsView(Session session, Settings settings)
	{
		InitializeComponent();
		m_Session = session;
		m_Settings = settings;
		m_ThreadIdMode = settings.ThreadIdMode;
		HookSessionEvents();
		m_ThreadOrderTimer.Interval = 200;
		m_ThreadOrderTimer.Tick += ThreadOrderChangedTimerTick;
		m_ThreadAddedTimer.Interval = 200;
		m_ThreadAddedTimer.Tick += ThreadAddedTimerTick;
		m_UpdateGraphHeightsTimer.Interval = 200;
		m_UpdateGraphHeightsTimer.Tick += UpdateGraphHeightsTimerTick;
		m_CoreGraph.ContextSwitchesVisible = m_Settings.ThreadsViewCoreGraphContextSwitchesVisible;
		m_CoreGraph.WaitEventsVisible = m_Settings.ThreadsViewWaitEVentsVisible;
		ShowInfoPanel(m_Settings.ThreadsViewInfoPanelVisible);
		ShowFrameGraph(m_Settings.ThreadsViewFrameGraphVisible);
		ShowTimeSpanGraph(m_Settings.ThreadsViewTimeSpanGraphVisible);
		ShowCoreView(m_Settings.ThreadsViewCoreViewVisible);
		ShowCustomStatsGraph(m_Settings.ThreadsViewCustomStatsVisible);
		m_SessionScrollBarPanel.TargetFrameMS = m_Settings.TargetFrameMS;
		m_SessionScrollBarPanel.SetSelectedRange(0L, m_FrameGraph.FrameXWidth);
		m_DPIScale = MainForm.DPIScale;
		m_VScrollBar.Size = new Size(ScaleDPI(m_VScrollBar.Width), m_VScrollBar.Height);
		m_FrameGraph.SetSettings(m_Settings);
		m_FrameGraph.SetSession(m_Session);
		m_FrameGraph.YScale = m_Settings.ThreadsViewFrameGraphYScale * (double)m_DPIScale;
		m_FrameGraph.TargetFrameMS = m_Settings.TargetFrameMS;
		m_FrameGraphYAxis.SetSettings(m_Settings);
		m_FrameGraphYAxis.YScale = m_Settings.ThreadsViewFrameGraphYScale * (double)m_DPIScale;
		m_FrameGraphYAxis.TargetFrameMS = m_Settings.TargetFrameMS;
		m_CoreGraph.SetSettings(settings);
		m_InfoPanel.SetSession(session);
		m_SessionScrollBarPanel.SetSession(session);
		m_SessionScrollBarPanel.SetSettings(settings);
		m_Timeline.SetSession(session);
		m_CoreGraph.SetSession(session);
		m_CustomStatsGraph.Initialise(session, settings);
		m_ScopeDataGrid.Initialise(session, settings);
		m_TimespanGraphView.SetSettings(m_Settings);
		m_TimespanGraphView.SetSession(session);
		m_TimespanGraphView.YScale = m_Settings.ThreadsViewFrameGraphYScale * (double)m_DPIScale;
		m_FrameGraphPanel.Height = ScaleDPI(m_Settings.FrameGraphHeight);
		m_TimespanGraphView.Height = ScaleDPI(m_Settings.TimeSpanGraphHeight);
		Text = "Threads";
		UpdateThreadIdModeButtonText();
		if (m_Settings.ThreadsViewCorePanelHeight != -1)
		{
			m_CoreBackPanel.Size = new Size(m_CoreBackPanel.Width, ScaleDPI(m_Settings.ThreadsViewCorePanelHeight));
		}
		SetSelectedTimeRangeScale(10.0);
		OnSelectedTimeRangeChanged();
		OnThreadAddedMainThread();
		UpdateMouseHoverEnabled();
		ClearThreadFilterTextBox();
		ShowDataGrid(m_Settings.ThreadsViewDataGridVisible);
		m_CustomGraphPanel.Size = new Size(m_CustomGraphPanel.Width, m_Settings.ThreadsViewCustomStatsGraphHeight);
		m_CustomStatsGraph.SetRange(0L, 10.0);
		m_CustomStatsGraph.YScale = m_FrameGraph.YScale;
		m_InfoPanelPanel.Size = new Size(m_InfoPanelPanel.Width, m_InfoPanel.PreferredHeight);
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	public override void OnActiveChanged()
	{
		m_FrameGraph.Active = base.Active;
		m_TimespanGraphView.Active = base.Active;
		m_CoreGraph.Active = base.Active;
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.Active = base.Active;
		}
		m_SessionScrollBarPanel.UpdateSessionScrollBarMode();
		base.OnActiveChanged();
	}

	private void ETLTraceFinished()
	{
		m_ControlTaskDispatcher.QueueTask(ETLTraceFinished_MainThread);
	}

	private void ETLTraceFinished_MainThread()
	{
		m_CoreGraph.OnETLTraceFinished();
	}

	private void HookSessionEvents()
	{
		m_Session.Disconnected += OnDisconnected;
		m_Session.ThreadAdded += OnThreadAdded;
		m_Session.ThreadNameChanged += OnThreadNameChanged;
		m_Session.ThreadOrderChanged += OnThreadOrderChanged;
		m_Session.SessionIsReady += OnSessionIsReady;
		m_Session.NonInteractiveModeFinished += OnNonInteractiveModeFinished;
		m_Session.ETLTraceFinished += ETLTraceFinished;
		m_Session.FinishedProcessingPackets += SessionFinishedProcessingPackets;
		m_Session.CoreCountChanged += OnMaxCoreCountChanged;
	}

	private void OnMaxCoreCountChanged()
	{
		m_ControlTaskDispatcher.QueueTask(OnMaxCoreCountChanged_Main);
	}

	private void OnMaxCoreCountChanged_Main()
	{
		int max = base.Height / 2;
		int num = Misc.Clamp((m_Settings.ThreadsViewCorePanelHeight != -1) ? ScaleDPI(m_Settings.ThreadsViewCorePanelHeight) : m_CoreGraph.GetDesiredHeight(), 175, max);
		m_CoreBackPanel.Height = num;
		Refresh();
	}

	private void SessionFinishedProcessingPackets()
	{
		m_ControlTaskDispatcher.QueueTask(SessionFinishedProcessingPackets_Main);
	}

	private void SessionFinishedProcessingPackets_Main()
	{
		if (m_SessionIsReady)
		{
			RefreshAllViews();
		}
	}

	private void OnThreadOrderChanged()
	{
		m_ControlTaskDispatcher.QueueTask(OnThreadOrderChanged_Main);
	}

	private void OnThreadOrderChanged_Main()
	{
		if (!m_ThreadOrderTimer.Enabled)
		{
			m_ThreadOrderTimer.Start();
		}
	}

	private void ThreadOrderChangedTimerTick(object sender, EventArgs e)
	{
		SortThreadList();
		UpdateTimeSpanGraphs();
		m_CoreGraph.UpdateThreadColours();
		m_ThreadOrderTimer.Stop();
	}

	private void OnThreadNameChanged(string old_name, string new_name)
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			OnThreadNameChanged_MainThread(old_name, new_name);
		});
	}

	private void OnThreadNameChanged_MainThread(string old_name, string new_name)
	{
		SortThreadList();
		UpdateThreadFilter();
		ThreadTimeSpanGraph value = null;
		if (m_ThreadTimeSpanGraphs.TryGetValue(old_name, out value))
		{
			value.ThreadName = new_name;
			value.TimeSpanColour = m_Session.GetThreadColour(new_name);
			m_ThreadTimeSpanGraphs.Remove(old_name);
			m_ThreadTimeSpanGraphs[new_name] = value;
			UpdateTimeSpanGraphs();
		}
		SortThreadList();
	}

	private void OnThreadAdded()
	{
		m_ControlTaskDispatcher.QueueTask(OnThreadAddedMainThread);
	}

	private void OnThreadAddedMainThread()
	{
		if (!m_ThreadAddedTimer.Enabled)
		{
			m_ThreadAddedTimer.Start();
		}
	}

	private void ThreadAddedTimerTick(object sender, EventArgs e)
	{
		UpdateThreadFilter();
		SortThreadList();
		UpdateTimeSpanGraphs();
		m_ThreadAddedTimer.Stop();
	}

	private List<string> GetThreadOrder()
	{
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name);
		if (threadFilter != null)
		{
			List<string> list = new List<string>();
			{
				foreach (ThreadFilterRow thread in threadFilter.m_Threads)
				{
					list.Add(thread.m_Name);
				}
				return list;
			}
		}
		return m_Session.GetThreadOrder();
	}

	private bool IsFilteredInByFilterTextBox(string thread_name)
	{
		string threadFilterTextBoxText = ThreadFilterTextBoxText;
		if (!string.IsNullOrEmpty(threadFilterTextBoxText))
		{
			return thread_name.IndexOf(threadFilterTextBoxText, StringComparison.OrdinalIgnoreCase) != -1;
		}
		return true;
	}

	private int ThreadIdSorter(int thread_id_a, int thread_id_b)
	{
		string threadName = GetThreadName(thread_id_a);
		string threadName2 = GetThreadName(thread_id_b);
		return string.Compare(threadName, threadName2);
	}

	private void SortThreadList()
	{
		m_Threads = m_Session.GetThreadIds();
		m_Threads.Sort(ThreadIdSorter);
		List<string> threadOrder = GetThreadOrder();
		if (threadOrder.Count != 0)
		{
			List<int> list = new List<int>();
			foreach (string item in threadOrder)
			{
				foreach (int item2 in new List<int>(m_Threads))
				{
					if (m_Session.GetThreadName(item2) == item)
					{
						m_Threads.Remove(item2);
						list.Add(item2);
					}
				}
			}
			list.AddRange(m_Threads);
			m_Threads = list;
			return;
		}
		string text = ((m_Session.MainThreadId != -1) ? m_Session.GetThreadName(m_Session.MainThreadId) : "");
		for (int i = 0; i < m_Threads.Count; i++)
		{
			int num = m_Threads[i];
			if (m_Session.GetThreadName(num) == text)
			{
				m_Threads.RemoveAt(i);
				m_Threads.Insert(0, num);
			}
		}
	}

	private bool IsThreadVisible(string thread_name, int thread_id)
	{
		if (!string.IsNullOrEmpty(ThreadFilterTextBoxText))
		{
			if (!IsFilteredInByFilterTextBox(thread_name))
			{
				return IsFilteredInByFilterTextBox(thread_id.ToString());
			}
			return true;
		}
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name);
		if (threadFilter != null)
		{
			foreach (ThreadFilterRow thread in threadFilter.m_Threads)
			{
				if (thread.m_Name == thread_name)
				{
					return thread.m_Visible;
				}
			}
		}
		return true;
	}

	private void RemoveThreadTimeSpanGraph(ThreadTimeSpanGraph thread_time_span_graph)
	{
		m_TimeSpanGraphPanelInner.Controls.Remove(thread_time_span_graph);
		m_ThreadTimeSpanGraphs.Remove(thread_time_span_graph.ThreadName);
	}

	private void ShowTooManyThreadsWarning()
	{
		if (!m_ShownTooManyThreadsWarning)
		{
			m_ShownTooManyThreadsWarning = true;
			string message = "WARNING: Too many threads! Only showing first " + m_Settings.CoreSettings.MaxVisibleThreads + " threads";
			MainForm.Inst.LogLine(message);
			MessageBox.Show(message, "ProfilerStudy WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private string GetThreadName(int thread_id)
	{
		string threadName = m_Session.GetThreadName(thread_id);
		return m_ThreadIdMode switch
		{
			ThreadIdMode.Name => threadName, 
			ThreadIdMode.Id => threadName + " (" + thread_id + ")", 
			_ => "error", 
		};
	}

	private void UpdateTimeSpanGraphs()
	{
		m_TimeSpanGraphPanelInner.SuspendLayout();
		bool flag = false;
		int num = 0;
		List<int> list = new List<int>(m_Threads);
		list.Reverse();
		List<string> list2 = new List<string>();
		foreach (int item in list)
		{
			list2.Add(GetThreadName(item));
		}
		if (list.Count > m_Settings.CoreSettings.MaxVisibleThreads)
		{
			list.RemoveRange(0, list.Count - m_Settings.CoreSettings.MaxVisibleThreads);
			ShowTooManyThreadsWarning();
		}
		Set<string> set = new Set<string>();
		foreach (string item2 in new List<string>(m_ThreadTimeSpanGraphs.Keys))
		{
			bool flag2 = set.Contains(item2);
			if (!list2.Contains(item2) || !IsThreadVisible(m_ThreadTimeSpanGraphs[item2].RealThreadName, m_ThreadTimeSpanGraphs[item2].ThreadId) || flag2)
			{
				RemoveThreadTimeSpanGraph(m_ThreadTimeSpanGraphs[item2]);
				flag = true;
			}
			else
			{
				set.Add(item2);
			}
		}
		foreach (int item3 in list)
		{
			string threadName = GetThreadName(item3);
			int thread_id = ((m_ThreadIdMode == ThreadIdMode.Id) ? item3 : 0);
			if (IsThreadVisible(m_Session.GetThreadName(item3), item3))
			{
				ThreadTimeSpanGraph threadTimeSpanGraph = GetThreadTimeSpanGraph(threadName);
				if (threadTimeSpanGraph == null)
				{
					threadTimeSpanGraph = CreateThreadTimeSpanGraph(threadName, thread_id, m_Session.FirstFrameTime);
					m_ThreadTimeSpanGraphs[threadName] = threadTimeSpanGraph;
				}
				threadTimeSpanGraph.TimeSpanColour = m_Session.GetThreadColour(threadTimeSpanGraph.ThreadName);
				if (!m_TimeSpanGraphPanelInner.Controls.Contains(threadTimeSpanGraph))
				{
					threadTimeSpanGraph.Dock = DockStyle.Top;
					m_TimeSpanGraphPanelInner.Controls.Add(threadTimeSpanGraph);
					flag = true;
				}
				if (m_TimeSpanGraphPanelInner.Controls.IndexOf(threadTimeSpanGraph) != num)
				{
					m_TimeSpanGraphPanelInner.Controls.SetChildIndex(threadTimeSpanGraph, num);
				}
				int expandedThreadHeight = GetExpandedThreadHeight(threadTimeSpanGraph.ThreadName);
				if (threadTimeSpanGraph.Height != expandedThreadHeight)
				{
					threadTimeSpanGraph.Size = new Size(threadTimeSpanGraph.Width, threadTimeSpanGraph.TimeSpanGraph.IdealHeight);
				}
				num++;
			}
		}
		if (flag)
		{
			UpdateTimeSpanGraphPanelSize();
		}
		m_TimeSpanGraphPanelInner.ResumeLayout();
	}

	private void OnDisconnected()
	{
		m_ControlTaskDispatcher.QueueTask(OnDisconnected_MainThread);
		UpdateMouseHoverEnabled();
	}

	private void OnDisconnected_MainThread()
	{
		m_SessionScrollBarPanel.OnSessionChanged();
		m_CoreGraph.OnDisconnected();
		m_FrameGraph.UpdateShowingProcessingDataMessage();
	}

	private void RefreshAllViews()
	{
		OnThreadAddedMainThread();
		UpdateFramesDataGrid();
		m_TimespanGraphView.UpdateTimeSpanDataGrid();
		CalculateTimeSpanGraphHeights();
		m_TimespanGraphView.RecalculateView(force: true);
		m_FrameGraph.RecalculateView(force: true);
		m_CustomStatsGraph.RecalculateView();
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.TimeSpanGraph.RecalculateView(force: true);
		}
		UpdateVScrollBar();
		UpdateInfoPanel();
		m_CoreGraph.Refresh();
	}

	private void OnSessionIsReady()
	{
		m_ControlTaskDispatcher.QueueTask(OnSessionIsReady_Main);
	}

	private void OnSessionIsReady_Main()
	{
		m_SessionIsReady = true;
		m_SessionScrollBarPanel.OnSessionChanged();
		m_CoreGraph.Initialise();
		m_CoreGraph.UpdateThreadColours();
		m_FrameGraph.UpdateShowingProcessingDataMessage();
		UpdateCustomStatGraphStatVisibility();
		m_CustomStatsGraph.SetSessionIsReady();
		OnMaxCoreCountChanged_Main();
		RefreshAllViews();
	}

	public override void UpdateView()
	{
		if (!m_Session.IsReady || (!m_Session.Connected && !m_Session.ProcessingPackets))
		{
			return;
		}
		m_SessionScrollBarPanel.OnSessionChanged();
		if (m_TimespanGraphView.NeedsUpdate)
		{
			m_TimespanGraphView.RecalculateView(force: true);
		}
		if (m_FrameGraph.NeedsUpdate)
		{
			m_FrameGraph.RecalculateView(force: true);
		}
		if (m_CustomStatsGraph.Visible)
		{
			if (m_CustomStatsGraph.NeedsUpdate)
			{
				m_CustomStatsGraph.RecalculateView();
			}
			int tickCount = Environment.TickCount;
			if (tickCount - m_LastUpdateCustomStatGraphsTime > 100)
			{
				UpdateCustomStatGraphStatVisibility();
				m_LastUpdateCustomStatGraphsTime = tickCount;
			}
		}
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			if (value.TimeSpanGraph.NeedsUpdate)
			{
				value.TimeSpanGraph.UpdateGraph(force: true);
			}
		}
		if (m_CoreGraph.NeedsUpdate)
		{
			m_CoreGraph.UpdateGraph(force: true);
		}
		if (Environment.TickCount - m_LastUpdateInfoPanelTime >= 1000)
		{
			m_LastUpdateInfoPanelTime = Environment.TickCount;
			if (m_InfoPanelPanel.Visible && m_Session.ReceivedConnectPacket)
			{
				UpdateInfoPanel();
			}
			UpdateFramesDataGrid();
			m_TimespanGraphView.UpdateTimeSpanDataGrid();
		}
	}

	private void OnSettingsChanged()
	{
		if (this.SettingsChanged != null)
		{
			this.SettingsChanged();
		}
	}

	private void UpdateInfoPanel()
	{
		m_InfoPanel.UpdateInfo();
	}

	private void UpdateFramesDataGrid()
	{
		m_FrameInfoPanel.UpdateStats(m_Session);
	}

	private int GetExpandedThreadHeight(string thread_name)
	{
		if (IsThreadCollapsed(thread_name))
		{
			return m_Settings.ThreadScopeHeight;
		}
		int customHeight = GetCustomHeight(thread_name);
		if (customHeight != 0)
		{
			return customHeight;
		}
		return GetIdealHeight(thread_name);
	}

	private void CalculateTimeSpanGraphHeights()
	{
		if (!m_UpdateGraphHeightsTimer.Enabled)
		{
			m_UpdateGraphHeightsTimer.Start();
		}
	}

	private void UpdateGraphHeightsTimerTick(object sender, EventArgs e)
	{
		m_TimeSpanGraphPanelInner.SuspendLayout();
		List<ThreadTimeSpanGraph> list = new List<ThreadTimeSpanGraph>();
		foreach (Control control in m_TimeSpanGraphPanelInner.Controls)
		{
			if (control is ThreadTimeSpanGraph item)
			{
				list.Add(item);
			}
		}
		list.Reverse();
		foreach (ThreadTimeSpanGraph item2 in list)
		{
			int expandedThreadHeight = GetExpandedThreadHeight(item2.ThreadName);
			if (item2.Height != expandedThreadHeight)
			{
				item2.Size = new Size(item2.Width, expandedThreadHeight);
			}
		}
		m_TimeSpanGraphPanelInner.ResumeLayout();
		m_UpdateGraphHeightsTimer.Stop();
	}

	private int GetIdealHeight(string thread_name)
	{
		if (m_ThreadTimeSpanGraphs.TryGetValue(thread_name, out var value) && value.TimeSpanGraph.IdealHeightValid)
		{
			int num = value.TimeSpanGraph.IdealHeight;
			if (value.RealThreadName == m_Session.GetThreadName(m_Session.MainThreadId))
			{
				num += m_Settings.ThreadScopeHeight / 2;
			}
			return num;
		}
		return m_Settings.ThreadScopeHeight;
	}

	private ThreadTimeSpanGraph CreateThreadTimeSpanGraph(string thread_name, int thread_id, long first_frame_time)
	{
		Color threadColour = m_Session.GetThreadColour(thread_name);
		ThreadTimeSpanGraph threadTimeSpanGraph = new ThreadTimeSpanGraph(m_Session, thread_name, thread_id, first_frame_time, threadColour, m_Settings);
		threadTimeSpanGraph.Size = new Size(m_TimeSpanGraphPanelInner.Width, m_Settings.ThreadScopeHeight);
		HookThreadTimeSpanGraph(threadTimeSpanGraph);
		threadTimeSpanGraph.TimeSpanGraph.SetTimeRange(m_SelectedTimeRange);
		threadTimeSpanGraph.TimeSpanGraph.MouseHoverEnabled = !base.TrackEnd;
		threadTimeSpanGraph.Active = base.Active;
		threadTimeSpanGraph.TimeSpanHeightChanged += TimeSpanHeightChangedByUser;
		return threadTimeSpanGraph;
	}

	private void TimeSpanHeightChangedByUser()
	{
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.OnThreadScopeHeightChanged();
		}
	}

	private void HookThreadTimeSpanGraph(ThreadTimeSpanGraph thread_time_span_graph)
	{
		thread_time_span_graph.Disposed += ThreadTimeSpanGraphDisposed;
		thread_time_span_graph.TimeSpanGraph.TimeRangeChanged += TimeSpanGraphRangeChanged;
		thread_time_span_graph.TimeSpanGraph.SelectedTimeSpanChanged += SelectedTimeSpanChangedEvent;
		thread_time_span_graph.TimeSpanGraph.HighlightScope += TimeSpanGraphHighlightScope;
		thread_time_span_graph.TimeSpanGraph.HighlightSingleScope += TimeSpanGraphHighlightSingleScope;
		thread_time_span_graph.TimeSpanGraph.MouseLeave += TimeSpanGraphMouseLeave;
		thread_time_span_graph.TimeSpanGraph.MeasureLineChanged += MeasureLineChanged;
		thread_time_span_graph.TimeSpanGraph.MouseDown += TimeSpanGraph_MouseDown;
		thread_time_span_graph.TimeSpanGraph.IdealHeightChanged += TimeSpanGraphIdealHeightChanged;
		thread_time_span_graph.TimeSpanGraph.ScrollY += OnTimeSpanGraphScrollY;
		thread_time_span_graph.LocationChanged += ThreadTimeSpanGraphLocationChanged;
		thread_time_span_graph.SizeChanged += ThreadTimeSpanGraphSizeChanged;
		thread_time_span_graph.UserResizedHeight += TimeSpanGraphUserResizedHeight;
		thread_time_span_graph.CollapseThread += ThreadTimeSpanGraphCollapseThread;
		thread_time_span_graph.ExpandThread += ThreadTimeSpanGraphExpandThread;
		thread_time_span_graph.CollapseExpandThreadToggle += ThreadTimeSpanGraphCollapseExpandThreadToggle;
		thread_time_span_graph.CollapseAllThreads += ThreadTimeSpanGraphCollapseAllThreads;
		thread_time_span_graph.ExpandAllThreads += ThreadTimeSpanGraphExpandAllThreads;
		thread_time_span_graph.HideThread += ThreadTimeSpanGraphHideThread;
		thread_time_span_graph.MoveThreadUp += TimeSpanMoveThreadUp;
		thread_time_span_graph.MoveThreadDown += TimeSpanMoveThreadDown;
		thread_time_span_graph.FilterThreadsClicked += ThreadTimeSpanGraphFilterThreadsClicked;
	}

	private void OnTimeSpanGraphScrollY(int offset_y)
	{
		ScrollVertical(offset_y);
	}

	private void ThreadTimeSpanGraphCollapseThread(ThreadTimeSpanGraph sender)
	{
		if (!IsThreadCollapsed(sender.ThreadName))
		{
			CollapseThreadTimeSpanGraph(sender.ThreadName);
		}
	}

	private void ThreadTimeSpanGraphExpandThread(ThreadTimeSpanGraph sender)
	{
		ExpandThreadTimeSpanGraph(sender.ThreadName);
	}

	private void ThreadTimeSpanGraphCollapseAllThreads()
	{
		SuspendLayout();
		List<int> list = new List<int>(m_Threads);
		list.Reverse();
		foreach (int item in list)
		{
			CollapseThreadTimeSpanGraph(GetThreadName(item));
		}
		UpdateTimeSpanGraphPanelSize();
		ResumeLayout();
	}

	private void ThreadTimeSpanGraphExpandAllThreads()
	{
		SuspendLayout();
		List<int> list = new List<int>(m_Threads);
		list.Reverse();
		foreach (int item in list)
		{
			ExpandThreadTimeSpanGraph(GetThreadName(item));
		}
		UpdateTimeSpanGraphPanelSize();
		ResumeLayout();
	}

	private void TimeSpanMoveThreadUp(ThreadTimeSpanGraph sender)
	{
		ThreadFilter orCreateThreadFilter = GetOrCreateThreadFilter();
		string threadName = sender.ThreadName;
		int num = m_TimeSpanGraphPanelInner.Controls.IndexOf(sender);
		string threadName2 = ((ThreadTimeSpanGraph)m_TimeSpanGraphPanelInner.Controls[num + 1]).ThreadName;
		int threadFilterRowIndex = orCreateThreadFilter.GetThreadFilterRowIndex(threadName);
		int threadFilterRowIndex2 = orCreateThreadFilter.GetThreadFilterRowIndex(threadName2);
		if (threadFilterRowIndex != -1 && threadFilterRowIndex2 != -1)
		{
			ThreadFilterRow item = orCreateThreadFilter.m_Threads[threadFilterRowIndex];
			orCreateThreadFilter.m_Threads.RemoveAt(threadFilterRowIndex);
			orCreateThreadFilter.m_Threads.Insert(threadFilterRowIndex2, item);
			m_Settings.Write();
			SortThreadList();
			UpdateTimeSpanGraphs();
		}
	}

	private void TimeSpanMoveThreadDown(ThreadTimeSpanGraph sender)
	{
		ThreadFilter orCreateThreadFilter = GetOrCreateThreadFilter();
		string threadName = sender.ThreadName;
		int num = m_TimeSpanGraphPanelInner.Controls.IndexOf(sender);
		string threadName2 = ((ThreadTimeSpanGraph)m_TimeSpanGraphPanelInner.Controls[num - 1]).ThreadName;
		int threadFilterRowIndex = orCreateThreadFilter.GetThreadFilterRowIndex(threadName);
		int threadFilterRowIndex2 = orCreateThreadFilter.GetThreadFilterRowIndex(threadName2);
		if (threadFilterRowIndex != -1 && threadFilterRowIndex2 != -1)
		{
			ThreadFilterRow item = orCreateThreadFilter.m_Threads[threadFilterRowIndex];
			orCreateThreadFilter.m_Threads.RemoveAt(threadFilterRowIndex);
			orCreateThreadFilter.m_Threads.Insert(threadFilterRowIndex2, item);
			m_Settings.Write();
			SortThreadList();
			UpdateTimeSpanGraphs();
		}
	}

	private void ThreadTimeSpanGraphHideThread(ThreadTimeSpanGraph sender)
	{
		GetOrCreateThreadFilter().GetThreadFilterRow(sender.ThreadName).m_Visible = false;
		m_Settings.Write();
		UpdateTimeSpanGraphs();
	}

	private void TimeSpanGraphIdealHeightChanged(TimeSpanGraph sender)
	{
		m_ControlTaskDispatcher.QueueTask(CalculateTimeSpanGraphHeights);
	}

	private void ThreadTimeSpanGraphFilterThreadsClicked()
	{
		FilterThreads();
	}

	private void ThreadTimeSpanGraphDisposed(object sender, EventArgs e)
	{
		UnhookThreadTimeSpanGraph((ThreadTimeSpanGraph)sender);
	}

	private void TimeSpanGraphHighlightScope(long time_span_name)
	{
		string timerName = m_Session.GetTimerName(time_span_name);
		HighlightTimeSpans(timerName);
	}

	private void ThreadTimeSpanGraphCollapseExpandThreadToggle(ThreadTimeSpanGraph sender)
	{
		if (IsThreadCollapsed(sender.ThreadName))
		{
			ExpandThreadTimeSpanGraph(sender.ThreadName);
		}
		else
		{
			CollapseThreadTimeSpanGraph(sender.ThreadName);
		}
	}

	private void CollapseThreadTimeSpanGraph(string thread_name)
	{
		GetOrCreateThreadFilter().SetCollapsed(thread_name, collapsed: true);
		SetTimeSpanGraphHeight(thread_name, m_Settings.ThreadScopeHeight);
		m_Settings.Write();
	}

	private void ExpandThreadTimeSpanGraph(string thread_name)
	{
		ThreadFilter orCreateThreadFilter = GetOrCreateThreadFilter();
		if (IsThreadCollapsed(thread_name))
		{
			orCreateThreadFilter.SetCollapsed(thread_name, collapsed: false);
		}
		else
		{
			orCreateThreadFilter.ClearCustomHeight(thread_name);
		}
		int expandedThreadHeight = GetExpandedThreadHeight(thread_name);
		SetTimeSpanGraphHeight(thread_name, expandedThreadHeight);
		m_Settings.Write();
	}

	private int GetCustomHeight(string thread_name)
	{
		return m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name)?.GetCustomHeight(thread_name) ?? 0;
	}

	private bool IsThreadCollapsed(string thread_name)
	{
		return m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name)?.IsCollapsed(thread_name) ?? false;
	}

	private void SetTimeSpanGraphHeight(string thread_name, int height)
	{
		if (m_ThreadTimeSpanGraphs.TryGetValue(thread_name, out var value) && value.Height != height)
		{
			value.Size = new Size(value.Width, height);
		}
	}

	private void UnhookThreadTimeSpanGraph(ThreadTimeSpanGraph thread_time_span_graph)
	{
		thread_time_span_graph.Disposed -= ThreadTimeSpanGraphDisposed;
		thread_time_span_graph.TimeSpanGraph.TimeRangeChanged -= TimeSpanGraphRangeChanged;
		thread_time_span_graph.TimeSpanGraph.SelectedTimeSpanChanged -= SelectedTimeSpanChangedEvent;
		thread_time_span_graph.TimeSpanGraph.HighlightScope -= TimeSpanGraphHighlightScope;
		thread_time_span_graph.TimeSpanGraph.MouseLeave -= TimeSpanGraphMouseLeave;
		thread_time_span_graph.TimeSpanGraph.MeasureLineChanged -= MeasureLineChanged;
		thread_time_span_graph.TimeSpanGraph.MouseDown -= TimeSpanGraph_MouseDown;
		thread_time_span_graph.TimeSpanGraph.IdealHeightChanged -= TimeSpanGraphIdealHeightChanged;
		thread_time_span_graph.TimeSpanGraph.ScrollY -= OnTimeSpanGraphScrollY;
		thread_time_span_graph.LocationChanged -= ThreadTimeSpanGraphLocationChanged;
		thread_time_span_graph.SizeChanged -= ThreadTimeSpanGraphSizeChanged;
		thread_time_span_graph.UserResizedHeight -= TimeSpanGraphUserResizedHeight;
		thread_time_span_graph.CollapseThread -= ThreadTimeSpanGraphCollapseThread;
		thread_time_span_graph.ExpandThread -= ThreadTimeSpanGraphExpandThread;
		thread_time_span_graph.CollapseExpandThreadToggle -= ThreadTimeSpanGraphCollapseExpandThreadToggle;
		thread_time_span_graph.CollapseAllThreads -= ThreadTimeSpanGraphCollapseAllThreads;
		thread_time_span_graph.ExpandAllThreads -= ThreadTimeSpanGraphExpandAllThreads;
		thread_time_span_graph.HideThread -= ThreadTimeSpanGraphHideThread;
		thread_time_span_graph.MoveThreadUp -= TimeSpanMoveThreadUp;
		thread_time_span_graph.MoveThreadDown -= TimeSpanMoveThreadDown;
		thread_time_span_graph.FilterThreadsClicked -= ThreadTimeSpanGraphFilterThreadsClicked;
	}

	private void TimeSpanGraph_MouseDown(object sender, MouseEventArgs e)
	{
		StopTrackingEnd();
	}

	private void ThreadTimeSpanGraphLocationChanged(object sender, EventArgs e)
	{
		UpdateTimeSpanGraphPanelSize();
	}

	private void ThreadTimeSpanGraphSizeChanged(object sender, EventArgs e)
	{
		UpdateTimeSpanGraphPanelSize();
	}

	private void TimeSpanGraphUserResizedHeight(object sender)
	{
		ThreadTimeSpanGraph threadTimeSpanGraph = (ThreadTimeSpanGraph)sender;
		GetOrCreateThreadFilter().SetCustomHeight(threadTimeSpanGraph.ThreadName, threadTimeSpanGraph.Height);
		OnSettingsChanged();
	}

	private void TimeSpanGraphMouseLeave(object sender, EventArgs e)
	{
		if (MainForm.Inst != null)
		{
			MainForm.Inst.HoverBox.Visible = false;
		}
	}

	private void TimeSpanGraphRangeChanged(TimeSpanGraph time_span_graph, TimeRange time_range)
	{
		m_SelectedTimeRange.CopyAllExceptScrollY(time_range);
		OnSelectedTimeRangeChanged();
	}

	private void CoreGraphTimeRangeChanged(TimeRange time_range)
	{
		m_SelectedTimeRange.CopyAllExceptScrollY(time_range);
		OnSelectedTimeRangeChanged();
	}

	private static long XToTime(int x, TimeRange visible_time_range)
	{
		return x * visible_time_range.m_TicksPerPixel + visible_time_range.m_StartTime;
	}

	private void ThreadTimeSpanGraphMouseWheel(int delta, Point mouse_pt)
	{
		long num = XToTime(mouse_pt.X, m_SelectedTimeRange);
		double num2 = ((delta > 0) ? MainSessionView.ZoomMultiplier : (1.0 / MainSessionView.ZoomMultiplier));
		SetSelectedTimeRangeScale(m_SelectedTimeRange.m_Scale * num2);
		long val = m_SelectedTimeRange.m_StartTime + num - XToTime(mouse_pt.X, m_SelectedTimeRange);
		val = Math.Max(m_Session.FirstFrameTime, val);
		m_SelectedTimeRange.m_StartTime = val;
		OnSelectedTimeRangeChanged();
	}

	private void SetSelectedTimeRangeScale(double scale)
	{
		double num = scale;
		if (m_Session.TimerFrequency != 0L)
		{
			double max = (double)m_Session.TimerFrequency / 1000.0;
			num = Misc.Clamp(num, MainSessionView.MinZoomScale, max);
		}
		long num2 = Math.Max(1L, (long)((double)m_Session.TimerFrequency / (1000.0 * num)));
		m_SelectedTimeRange.m_Scale = num;
		if (m_SelectedTimeRange.m_TicksPerPixel != num2)
		{
			m_SelectedTimeRange.m_TicksPerPixel = num2;
		}
	}

	public override void OnMouseWheel(int delta, Point location)
	{
		Point point = PointToScreen(location);
		bool flag = false;
		if (!flag && m_TimespanGraphView.RectangleToScreen(m_TimespanGraphView.ClientRectangle).Contains(point))
		{
			flag = true;
			m_TimespanGraphView.OnMouseWheel(delta, m_TimespanGraphView.PointToClient(point));
		}
		if (!flag && m_FrameGraph.RectangleToScreen(m_FrameGraph.ClientRectangle).Contains(point))
		{
			flag = true;
			m_FrameGraph.OnMouseWheel(delta, m_FrameGraph.PointToClient(point));
		}
		if (!flag)
		{
			Rectangle rectangle = m_TimeSpanGraphPanel.RectangleToScreen(m_TimeSpanGraphPanel.ClientRectangle);
			int num = 0;
			using (Dictionary<string, ThreadTimeSpanGraph>.ValueCollection.Enumerator enumerator = m_ThreadTimeSpanGraphs.Values.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					num = enumerator.Current.ThreadLabelWidth;
				}
			}
			if (rectangle.Contains(point))
			{
				if (point.X - rectangle.X < num || Utils.IsShiftHeld)
				{
					int num2 = m_TimeSpanGraphPanel.Height / 3;
					int offset = ((delta > 0) ? (-num2) : num2);
					ScrollVertical(offset);
				}
				else
				{
					Point mouse_pt = m_TimeSpanGraphPanel.PointToClient(point);
					mouse_pt.X -= num;
					ThreadTimeSpanGraphMouseWheel(delta, mouse_pt);
				}
			}
		}
		if (!flag && m_CoreGraph.RectangleToScreen(m_CoreGraph.CoreGraphClientRectangle).Contains(point) && !Utils.IsShiftHeld)
		{
			Point mouse_pt2 = m_CoreGraph.PointToCoreGraphClient(point);
			ThreadTimeSpanGraphMouseWheel(delta, mouse_pt2);
			flag = true;
		}
		if (!flag && m_CustomStatsGraph.RectangleToScreen(m_CustomStatsGraph.ClientRectangle).Contains(point))
		{
			Point location2 = m_CustomStatsGraph.PointToClient(point);
			m_CustomStatsGraph.OnMouseWheel(delta, location2);
			flag = true;
		}
	}

	private void ScrollVertical(int offset)
	{
		SetVScrollValue(m_VScrollBar.Value + offset);
	}

	private void FrameGraphSelectionChanged(Control sender, long start_time, long end_time)
	{
		SetSelectedTimeRange(sender, start_time, end_time);
		StopTrackingEnd();
	}

	public void SetVisibleTimeRange(long start_time, long end_time, bool refresh_thread_and_core_views)
	{
		SetVisibleTimeRange(null, start_time, end_time, scroll_into_view: true, refresh_thread_and_core_views);
	}

	private void SetSelectedTimeRange(Control sender, long start_time, long end_time)
	{
		SetVisibleTimeRange(sender, start_time, end_time, scroll_into_view: false, refresh_thread_and_core_views: true);
	}

	private void SetVisibleTimeRange(Control sender, long start_time, long end_time, bool scroll_into_view, bool refresh_thread_and_core_views)
	{
		if (m_TimespanGraphView != sender)
		{
			m_TimespanGraphView.SetSelectedRange(start_time, end_time, scroll_into_view);
		}
		if (m_FrameGraph != sender)
		{
			m_FrameGraph.SetVisibleRange(start_time, end_time, scroll_into_view);
		}
		Utils.SetTimeRange(start_time, end_time, TimeSpanGraphWidth, m_Session.TimerFrequency, m_SelectedTimeRange);
		if (refresh_thread_and_core_views)
		{
			if (m_Timeline != sender)
			{
				m_Timeline.SetTimeRange(m_SelectedTimeRange);
			}
			foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
			{
				if (value.TimeSpanGraph != sender)
				{
					value.TimeSpanGraph.SetTimeRange(m_SelectedTimeRange);
				}
			}
			if (m_CoreGraph != sender)
			{
				m_CoreGraph.SetTimeRange(m_SelectedTimeRange);
			}
		}
		if (this.SelectedTimeRangeChanged != null)
		{
			this.SelectedTimeRangeChanged(this, start_time, end_time);
		}
	}

	private void OnSelectedTimeRangeChanged()
	{
		OnSelectedTimeRangeChanged(scroll_into_view: false);
	}

	private void OnSelectedTimeRangeChanged(bool scroll_into_view)
	{
		OnSelectedTimeRangeChanged(scroll_into_view, refresh_thread_and_core_views: true);
	}

	private void OnSelectedTimeRangeChanged(bool scroll_into_view, bool refresh_thread_and_core_views)
	{
		m_TimespanGraphView.SetSelectedRange(m_SelectedTimeRange.m_StartTime, SelectEndTime, scroll_into_view);
		m_FrameGraph.SetVisibleRange(m_SelectedTimeRange.m_StartTime, SelectEndTime, scroll_into_view);
		if (refresh_thread_and_core_views)
		{
			m_Timeline.SetTimeRange(m_SelectedTimeRange);
			foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
			{
				value.TimeSpanGraph.SetTimeRange(m_SelectedTimeRange);
			}
			m_CoreGraph.SetTimeRange(m_SelectedTimeRange);
		}
		if (this.SelectedTimeRangeChanged != null)
		{
			this.SelectedTimeRangeChanged(this, m_SelectedTimeRange.m_StartTime, SelectEndTime);
		}
	}

	private ThreadTimeSpanGraph GetThreadTimeSpanGraph(string thread_name)
	{
		ThreadTimeSpanGraph value = null;
		m_ThreadTimeSpanGraphs.TryGetValue(thread_name, out value);
		return value;
	}

	public void FillWithRandomData()
	{
		m_Session.FillWithRandomData();
	}

	private void TimeSpanGraphPanelResize(object sender, EventArgs e)
	{
		UpdateTimeSpanGraphPanelSize();
	}

	private void UpdateTimeSpanGraphPanelSize()
	{
		if (MainForm.Inst == null || MainForm.Inst.WindowState == FormWindowState.Minimized || !m_SessionIsReady)
		{
			return;
		}
		int num = 0;
		foreach (Control control in m_TimeSpanGraphPanelInner.Controls)
		{
			int num2 = control.Location.Y + control.Height;
			if (num2 > num)
			{
				num = num2;
			}
		}
		m_TimeSpanGraphPanelInner.Size = new Size(m_TimeSpanGraphPanel.Width, num);
		UpdateVScrollBar();
	}

	private void UpdateVScrollBar()
	{
		if (m_VScrollBar.Maximum != m_TimeSpanGraphPanelInner.Height || m_VScrollBar.LargeChange != m_TimeSpanGraphPanel.Height)
		{
			m_VScrollBar.Maximum = m_TimeSpanGraphPanelInner.Height;
			m_VScrollBar.LargeChange = Math.Max(0, m_TimeSpanGraphPanel.Height);
			m_VScrollBar.Visible = m_VScrollBar.LargeChange < m_VScrollBar.Maximum;
			SetVScrollValue(m_VScrollBar.Value);
			UpdateScrolledIntoViewFlags();
		}
	}

	private void TimeSpanVScrollBarScroll(object sender, ScrollEventArgs e)
	{
		SetVScrollValue(m_VScrollBar.Value + e.NewValue - e.OldValue);
	}

	private void SetVScrollValue(int new_value)
	{
		int max = Math.Max(0, m_TimeSpanGraphPanelInner.Height - m_TimeSpanGraphPanel.Height);
		new_value = Misc.Clamp(new_value, 0, max);
		if (m_VScrollBar.Value != new_value)
		{
			m_VScrollBar.Value = new_value;
			m_TimeSpanGraphPanelInner.Location = new Point(m_TimeSpanGraphPanelInner.Location.X, -new_value);
			UpdateScrolledIntoViewFlags();
			m_TimeSpanGraphPanelInner.Refresh();
		}
	}

	private void UpdateScrolledIntoViewFlags()
	{
		Rectangle parent_screen_rect = m_TimeSpanGraphPanel.RectangleToScreen(m_TimeSpanGraphPanel.ClientRectangle);
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.UpdateScrolledIntoViewFlag(parent_screen_rect);
		}
	}

	private void GotoStart()
	{
		m_TimespanGraphView.GotoStart();
		m_FrameGraph.GotoStart();
		m_SelectedTimeRange.m_StartTime = m_Session.FirstFrameTime;
		OnSelectedTimeRangeChanged();
	}

	private void GotoEnd()
	{
		GotoEnd(refresh_thread_and_core_views: true);
	}

	public void GotoEnd(bool refresh_thread_and_core_views)
	{
		long lastFrameEndTime = m_Session.LastFrameEndTime;
		long num = SelectEndTime - m_SelectedTimeRange.m_StartTime;
		m_SelectedTimeRange.m_StartTime = lastFrameEndTime - num;
		OnSelectedTimeRangeChanged(scroll_into_view: true, refresh_thread_and_core_views);
	}

	public override void OnTrackEndChanged()
	{
		base.OnTrackEndChanged();
		UpdateMouseHoverEnabled();
	}

	private void UpdateMouseHoverEnabled()
	{
		bool mouseHoverEnabled = !base.TrackEnd;
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.TimeSpanGraph.MouseHoverEnabled = mouseHoverEnabled;
		}
		m_Timeline.MouseHoverEnabled = mouseHoverEnabled;
		m_CoreGraph.MouseHoverEnabled = mouseHoverEnabled;
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void TimeSpanPanelResize(object sender, EventArgs e)
	{
		OnSelectedTimeRangeChanged();
	}

	private void OnFrameGraphRangeChanged(Control sender, long start_frame_x, long end_frame_x, double view_scale)
	{
		m_SessionScrollBarPanel.SetSelectedRange(start_frame_x, end_frame_x);
		if (m_TimespanGraphView != sender)
		{
			m_TimespanGraphView.SetRange(start_frame_x, view_scale);
		}
		if (m_FrameGraph != sender)
		{
			m_FrameGraph.SetRange(start_frame_x, view_scale);
		}
		if (m_CustomStatsGraph != sender)
		{
			m_CustomStatsGraph.SetRange(start_frame_x, view_scale);
		}
		this.FrameGraphRangeChanged(sender, start_frame_x, end_frame_x, view_scale);
	}

	public void SetFrameGraphRange(long start_frame_x, double view_scale)
	{
		m_FrameGraph.SetRange(start_frame_x, view_scale);
	}

	private void SelectedTimeSpanChangedEvent(long time_span_name, TimeSpan time_span)
	{
		m_ScopeDataGrid.TimeSpan = time_span;
		m_InSelectedTimeSpanChangedEventFunction = true;
		if (m_SelectedTimeSpan == time_span_name)
		{
			m_InSelectedTimeSpanChangedEventFunction = false;
			return;
		}
		m_SelectedTimeSpan = time_span_name;
		m_TimespanGraphView.SetTimeSpan(time_span_name);
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.SetSelectedTimeSpan(time_span_name);
		}
		m_CoreGraph.SelectTimeSpan(time_span_name);
		m_TimespanGraphView.UpdateTimeSpanDataGrid();
		if (!TimeSpanGraphVisible)
		{
			ShowTimeSpanGraph(visible: true);
			OnViewVisibilityChanged();
		}
		if (this.SelectedTimeSpanChanged != null)
		{
			this.SelectedTimeSpanChanged(time_span_name, time_span);
		}
		m_InSelectedTimeSpanChangedEventFunction = false;
	}

	private void OnViewVisibilityChanged()
	{
		if (this.SectionVisibilityChanged != null)
		{
			this.SectionVisibilityChanged();
		}
	}

	public void ShowInfoPanel(bool visible)
	{
		if (m_Settings.ThreadsViewInfoPanelVisible != visible)
		{
			m_Settings.ThreadsViewInfoPanelVisible = visible;
			m_Settings.Write();
		}
		m_InfoPanelPanel.Visible = visible;
	}

	public void ShowFrameGraph(bool visible)
	{
		if (m_Settings.ThreadsViewFrameGraphVisible != visible)
		{
			m_Settings.ThreadsViewFrameGraphVisible = visible;
			m_Settings.Write();
		}
		m_FrameGraphPanel.Visible = visible;
		m_FrameGraphSplitter.Visible = visible;
	}

	public void ShowTimeSpanGraph(bool visible)
	{
		if (m_Settings.ThreadsViewTimeSpanGraphVisible != visible)
		{
			m_Settings.ThreadsViewTimeSpanGraphVisible = visible;
			m_Settings.Write();
		}
		m_TimespanGraphView.Visible = visible;
		m_TimeSpanGraphSplitter.Visible = visible;
	}

	public void ShowCoreView(bool visible)
	{
		if (m_Settings.ThreadsViewCoreViewVisible != visible)
		{
			m_Settings.ThreadsViewCoreViewVisible = visible;
			m_Settings.Write();
		}
		m_CoreViewSplitter.Visible = visible;
		m_CoreBackPanel.Visible = visible;
	}

	public void ShowCustomStatsGraph(bool visible)
	{
		if (m_Settings.ThreadsViewCustomStatsVisible != visible)
		{
			m_Settings.ThreadsViewCustomStatsVisible = visible;
			m_Settings.Write();
		}
		m_CustomGraphPanel.Visible = visible;
		m_CustomGraphPanelSplitter.Visible = visible;
	}

	public void ShowDataGrid(bool visible)
	{
		if (m_Settings.ThreadsViewDataGridVisible != visible)
		{
			m_Settings.ThreadsViewDataGridVisible = visible;
			m_Settings.Write();
		}
		m_ScopeDataGrid.Visible = visible;
		m_DataGridSplitter.Visible = visible;
	}

	public void HighlightTimeSpans(string filter)
	{
		m_HighlightFilter = filter.Trim();
		UpdateHighlightFilter();
	}

	private void UpdateHighlightFilter()
	{
		Set<long> set = ((m_HighlightFilter.Length != 0) ? m_Session.GetTimerNames(m_HighlightFilter) : new Set<long>());
		if (m_HighlightedTimeSpans.SequenceEqual(set))
		{
			return;
		}
		m_HighlightedTimeSpans = set;
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.TimeSpanGraph.SetHighlightedTimeSpans(set);
		}
		m_CoreGraph.SetHighlightedTimeSpans(set);
	}

	private void TimeSpanGraphHighlightSingleScope(TimeSpan time_span)
	{
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.TimeSpanGraph.SetSingleHighlightedTimeSpan(time_span);
		}
	}

	public override void GotoPrev(string filter)
	{
		StopTrackingEnd();
		filter = filter.Trim();
		Set<long> time_spans = ((filter.Length != 0) ? m_Session.GetTimerNames(filter) : new Set<long>());
		int thread_id = 0;
		TimeSpan timeSpan = m_Session.FindPrevTimeSpan(time_spans, m_SelectedTimeRange.m_StartTime, ref thread_id);
		if (timeSpan == null)
		{
			timeSpan = m_Session.FindPrevTimeSpan(time_spans, m_Session.LastFrameEndTime, ref thread_id);
		}
		if (timeSpan == null)
		{
			MessageBox.Show("Unable to find Scope: " + filter);
			return;
		}
		SetSelectedTimeRange(null, timeSpan.StartTime, timeSpan.EndTime);
		string threadName = GetThreadName(thread_id);
		ScrollThreadIntoView(threadName);
	}

	public override void GotoNext(string filter)
	{
		StopTrackingEnd();
		filter = filter.Trim();
		Set<long> time_spans = ((filter.Length != 0) ? m_Session.GetTimerNames(filter) : new Set<long>());
		int thread_id = 0;
		TimeSpan timeSpan = m_Session.FindNextTimeSpan(time_spans, m_SelectedTimeRange.m_StartTime, ref thread_id);
		if (timeSpan == null)
		{
			timeSpan = m_Session.FindNextTimeSpan(time_spans, m_Session.FirstFrameTime, ref thread_id);
		}
		if (timeSpan == null)
		{
			MessageBox.Show("Unable to find Scope: " + filter);
			return;
		}
		SetSelectedTimeRange(null, timeSpan.StartTime, timeSpan.EndTime);
		string threadName = GetThreadName(thread_id);
		ScrollThreadIntoView(threadName);
	}

	public void ScrollThreadIntoView(string thread_name)
	{
		SetVScrollValue(m_ThreadTimeSpanGraphs[thread_name].Location.Y);
	}

	private void CoreGraphShowThread(int thread_id)
	{
		string threadName = GetThreadName(thread_id);
		ScrollThreadIntoView(threadName);
	}

	private void MeasureLineChanged(int start_x, int end_x)
	{
		m_Timeline.SetMeasureLine(start_x, end_x);
		m_CoreGraph.SetMeasureLine(start_x, end_x);
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.TimeSpanGraph.SetMeasureLine(start_x, end_x);
		}
	}

	public void MoveToPrevFrame(bool zoom_to_frame)
	{
		StopTrackingEnd();
		long startTime = m_SelectedTimeRange.m_StartTime;
		int frameIndex = m_Session.GetFrameIndex(startTime);
		if (frameIndex <= 0)
		{
			return;
		}
		Frame frame = m_Session.GetFrame(frameIndex - 1);
		if (frame != null)
		{
			if (zoom_to_frame)
			{
				Utils.SetTimeRange(frame.StartTime, frame.EndTime, TimeSpanGraphWidth, m_Session.TimerFrequency, m_SelectedTimeRange);
			}
			else
			{
				m_SelectedTimeRange.m_StartTime = frame.StartTime;
			}
			OnSelectedTimeRangeChanged();
		}
	}

	public void MoveToNextFrame(bool zoom_to_frame)
	{
		StopTrackingEnd();
		long startTime = m_SelectedTimeRange.m_StartTime;
		int frameIndex = m_Session.GetFrameIndex(startTime);
		if (frameIndex >= m_Session.FrameCount - 1)
		{
			return;
		}
		Frame frame = m_Session.GetFrame(frameIndex + 1);
		if (frame != null)
		{
			if (zoom_to_frame)
			{
				Utils.SetTimeRange(frame.StartTime, frame.EndTime, TimeSpanGraphWidth, m_Session.TimerFrequency, m_SelectedTimeRange);
			}
			else
			{
				m_SelectedTimeRange.m_StartTime = frame.StartTime;
			}
			OnSelectedTimeRangeChanged();
		}
	}

	private void OnNonInteractiveModeFinished()
	{
		m_ControlTaskDispatcher.QueueTask(OnNonInteractiveModeFinished_MainThread);
	}

	private void OnNonInteractiveModeFinished_MainThread()
	{
		RefreshAllViews();
		if (m_SelectedTimeRange.m_StartTime < 0)
		{
			GotoStart();
		}
	}

	public void GotoMaxFrame()
	{
		int maxFrameIndex = m_Session.MaxFrameIndex;
		if (maxFrameIndex >= 0 && maxFrameIndex < m_Session.FrameCount)
		{
			SelectAndCentreFrame(maxFrameIndex);
		}
	}

	public void GotoPrevSpike()
	{
		long startTime = m_SelectedTimeRange.m_StartTime;
		int num = m_Session.FindPrevSpike(startTime);
		if (num != -1)
		{
			SelectAndCentreFrame(num);
		}
	}

	public void GotoNextSpike()
	{
		long startTime = m_SelectedTimeRange.m_StartTime;
		int num = m_Session.FindNextSpike(startTime);
		if (num != -1)
		{
			SelectAndCentreFrame(num);
		}
	}

	private void SelectAndCentreFrame(int frame_index)
	{
		Frame frame = m_Session.GetFrame(frame_index);
		SetSelectedTimeRange(null, frame.StartTime, frame.EndTime);
		m_FrameGraph.CentreFrame(frame_index);
		m_TimespanGraphView.CentreFrame(frame_index);
		StopTrackingEnd();
	}

	private void FilterThreadsButtonClicked(object sender, EventArgs e)
	{
		FilterThreads();
	}

	private ThreadFilter GetOrCreateThreadFilter()
	{
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name);
		if (threadFilter == null)
		{
			threadFilter = Utils.CreateNewThreadFilter(m_Session);
			m_Settings.SetThreadFilter(m_Session.SessionDetails.m_Name, threadFilter);
		}
		return threadFilter;
	}

	private void UpdateThreadFilter()
	{
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name);
		if (threadFilter == null)
		{
			return;
		}
		List<int> list = new List<int>();
		m_Session.GetThreads(list);
		m_Session.GetThreadOrder();
		bool flag = false;
		foreach (int item in list)
		{
			string threadName = m_Session.GetThreadName(item);
			if (Utils.GetThreadIndex(threadFilter, threadName) == -1)
			{
				Utils.AddThreadToFilter(threadName, threadFilter, m_Session);
				flag = true;
			}
		}
		if (flag)
		{
			m_Settings.Write();
		}
	}

	private void FilterThreads()
	{
		ThreadFilterForm threadFilterForm = new ThreadFilterForm(m_Settings, m_Session);
		if (threadFilterForm.ShowDialog(this) == DialogResult.OK)
		{
			GetOrCreateThreadFilter().m_Threads = threadFilterForm.GetThreadFilters();
			m_Settings.Write();
			OnThreadOrderChanged_Main();
		}
	}

	private void FilterThreadsTextBoxKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			SubmitFilterThreadsTextBox();
		}
	}

	private void FilterThreadsTextBoxClearButtonPressed(object sender, EventArgs e)
	{
		ClearThreadFilterTextBox();
		SubmitFilterThreadsTextBox();
	}

	private void SubmitFilterThreadsTextBox()
	{
		if (m_Settings.FilterThreadsTextBoxText != ThreadFilterTextBoxText)
		{
			m_Settings.FilterThreadsTextBoxText = ThreadFilterTextBoxText;
			m_Settings.Write();
		}
		UpdateTimeSpanGraphs();
		SetVScrollValue(0);
		if (ThreadFilterTextBoxText == "")
		{
			ClearThreadFilterTextBox();
		}
	}

	private void ClearThreadFilterTextBox()
	{
		m_ThreadFilterTextBox.Text = "Filter threads...";
		m_ThreadFilterTextBox.ForeColor = Color.Gray;
		m_ThreadFilterTextBoxEmpty = true;
	}

	private void ThreadFilterTextChanged(object sender, EventArgs e)
	{
		if (m_ThreadFilterTextBoxEmpty)
		{
			m_ThreadFilterTextBox.ForeColor = ForeColor;
			m_ThreadFilterTextBoxEmpty = false;
		}
	}

	private void ThreadFilterTextBoxEntered(object sender, EventArgs e)
	{
		if (m_ThreadFilterTextBoxEmpty)
		{
			m_ThreadFilterTextBox.ForeColor = ForeColor;
			m_ThreadFilterTextBox.Text = "";
			m_ThreadFilterTextBoxEmpty = false;
		}
	}

	private void CoreContextSwitchesVisibleChanged()
	{
		if (m_Settings.ThreadsViewCoreGraphContextSwitchesVisible != m_CoreGraph.ContextSwitchesVisible)
		{
			m_Settings.ThreadsViewCoreGraphContextSwitchesVisible = m_CoreGraph.ContextSwitchesVisible;
			m_Settings.Write();
		}
	}

	private void FrameGraphSplitterMoved(object sender, SplitterEventArgs e)
	{
		int num = (int)((float)m_FrameGraph.Height / m_DPIScale);
		if (m_Settings.FrameGraphHeight != num)
		{
			m_Settings.FrameGraphHeight = num;
			m_Settings.Write();
		}
	}

	private void TimeSpanGraphSplitterMoved(object sender, SplitterEventArgs e)
	{
		int num = (int)((float)m_TimespanGraphView.Height / m_DPIScale);
		if (m_Settings.TimeSpanGraphHeight != num)
		{
			m_Settings.TimeSpanGraphHeight = num;
			m_Settings.Write();
		}
	}

	public override void OnTargetFrameTimeChanged()
	{
		m_FrameGraph.Refresh();
		m_TimeSpanGraphPanel.Refresh();
	}

	private void FrameGraphYAxisScaleChanged(double y_scale)
	{
		m_Settings.ThreadsViewFrameGraphYScale = y_scale / (double)m_DPIScale;
		m_FrameGraph.YScale = y_scale;
		m_TimespanGraphView.YScale = y_scale;
		m_CustomStatsGraph.YScale = y_scale;
	}

	private void TimeSpanGraphViewYAxisScaleChanged(double y_scale)
	{
		m_FrameGraph.YScale = y_scale;
		m_FrameGraphYAxis.YScale = y_scale;
		m_CustomStatsGraph.YScale = y_scale;
	}

	private void FrameGraphYAxisTargetMsChanged(double target_ms)
	{
		m_Settings.TargetFrameMS = target_ms;
		m_SessionScrollBarPanel.TargetFrameMS = m_Settings.TargetFrameMS;
		m_FrameGraph.TargetFrameMS = target_ms;
		if (this.FrameTargetMSChanged != null)
		{
			this.FrameTargetMSChanged(this);
		}
	}

	public void OnTargetMSChanged()
	{
		m_FrameGraph.TargetFrameMS = m_Settings.TargetFrameMS;
		m_FrameGraphYAxis.TargetFrameMS = m_Settings.TargetFrameMS;
	}

	public void OnTimeSpanTargetMSChanged()
	{
		m_TimespanGraphView.OnTimeSpanTargetMSChanged();
	}

	private void TimeSpanGraphViewTargetMSChanged(double target_ms)
	{
		if (this.TimeSpanTargetMSChanged != null)
		{
			this.TimeSpanTargetMSChanged(this);
		}
	}

	public void SetSelectedTimeSpan(long time_span_name)
	{
		SelectedTimeSpanChangedEvent(time_span_name, null);
	}

	private void CoreGraphStopTrackingEnd()
	{
		StopTrackingEnd();
	}

	private void CoreGraphSplitterMoved(object sender, SplitterEventArgs e)
	{
		int num = (int)((float)m_CoreBackPanel.Height / m_DPIScale);
		if (m_Settings.ThreadsViewCorePanelHeight != num)
		{
			m_Settings.ThreadsViewCorePanelHeight = num;
			m_Settings.Write();
		}
	}

	private void SessionScrollBarChanged(long start_frame_x, long end_frame_x)
	{
		StopTrackingEnd();
		long start_time = m_Session.FrameXToTime(start_frame_x);
		long end_time = m_Session.FrameXToTime(end_frame_x);
		m_TimespanGraphView.SetTimeRange(start_time, end_time);
		m_FrameGraph.SetTimeRange(start_time, end_time);
		m_CustomStatsGraph.SetTimeRange(start_time, end_time);
	}

	private void CoreGraphWaitEventsVisibleChanged()
	{
		if (m_Settings.ThreadsViewWaitEVentsVisible != m_CoreGraph.WaitEventsVisible)
		{
			m_Settings.ThreadsViewWaitEVentsVisible = m_CoreGraph.WaitEventsVisible;
			m_Settings.Write();
		}
	}

	public void OnScopeColourModeChanged()
	{
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.OnScopeColourModeChanged();
		}
		m_CoreGraph.OnScopeColourModeChanged();
	}

	public override void OnThreadScopeHeightChanged()
	{
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.OnThreadScopeHeightChanged();
		}
	}

	private void ShowCustomStatButtonClicked(object sender, EventArgs e)
	{
		CustomStatSelector customStatSelector = new CustomStatSelector();
		Point location = m_CustomStatSelectorButton.Parent.Parent.PointToScreen(new Point(m_CustomStatSelectorButton.Parent.Location.X + m_CustomStatSelectorButton.Parent.Width, m_CustomStatSelectorButton.Parent.Location.Y + m_CustomStatSelectorButton.Parent.Height));
		customStatSelector.Location = location;
		List<CustomStatSelector.CustomStat> list = new List<CustomStatSelector.CustomStat>();
		foreach (CustomStatSessionData customStat in m_Session.GetCustomStats())
		{
			CustomStatSelector.CustomStat item = default(CustomStatSelector.CustomStat);
			item.m_Name = m_Session.GetString(customStat.Name);
			item.m_Visible = m_Settings.GetCustomStatVisibleInThreadView(item.m_Name);
			list.Add(item);
		}
		customStatSelector.Setup(list);
		customStatSelector.CustomStatVisibilityChanged += CustomStatVisibilityChanged;
		customStatSelector.ShowDialog();
	}

	private void CustomStatVisibilityChanged(string name, bool visible)
	{
		m_Settings.SetCustomStatVisibleInThreadView(name, visible);
		m_Settings.Write();
		UpdateCustomStatGraphStatVisibility();
	}

	private void UpdateCustomStatGraphStatVisibility()
	{
		List<long> list = new List<long>();
		foreach (CustomStatSessionData customStat in m_Session.GetCustomStats())
		{
			string @string = m_Session.GetString(customStat.Name);
			if (m_Settings.GetCustomStatVisibleInThreadView(@string))
			{
				list.Add(customStat.Name);
				m_CustomStatGraphsVisibleTemp.Add(@string);
			}
		}
		if (!Utils.ListsEquals(m_CustomStatGraphsVisibleTemp, m_CustomStatGraphsVisible))
		{
			m_CustomStatsGraph.SetCustomStats(list);
			m_CustomStatGraphsVisible = new List<string>(m_CustomStatGraphsVisibleTemp);
			m_CustomStatGraphsVisibleTemp.Clear();
		}
	}

	private void CustomStatGraphTimeRangeChanged(Control sender, long start_frame_x, long end_frame_x, double frame_x_scale)
	{
		OnFrameGraphRangeChanged(sender, start_frame_x, end_frame_x, frame_x_scale);
	}

	private void CustomStatsGraphSplitterMoved(object sender, SplitterEventArgs e)
	{
		if (m_Settings.ThreadsViewCustomStatsGraphHeight != m_CustomGraphPanel.Height)
		{
			m_Settings.ThreadsViewCustomStatsGraphHeight = m_CustomGraphPanel.Height;
			m_Settings.Write();
		}
	}

	public override void OnScopeColourChanged()
	{
		foreach (ThreadTimeSpanGraph value in m_ThreadTimeSpanGraphs.Values)
		{
			value.OnScopeColourChanged();
		}
		m_CoreGraph.OnScopeColourChanged();
		RefreshAllViews();
	}

	public override void OnCustomStatColourChanged()
	{
		RefreshAllViews();
	}

	private void FrameGraphSelectedRangeChanged(int start_frame_index, int end_frame_index)
	{
		SetSelectedRange(start_frame_index, end_frame_index);
		if (this.SelectedRangeChanged != null)
		{
			this.SelectedRangeChanged(start_frame_index, end_frame_index);
		}
	}

	public void SetSelectedRange(int start_frame_index, int end_frame_index)
	{
		m_FrameGraph.SetSelectedRange(start_frame_index, end_frame_index);
		m_TimespanGraphView.SetSelectedRange(start_frame_index, end_frame_index);
		m_FrameInfoPanel.SetSelectedRange(start_frame_index, end_frame_index, m_Session);
	}

	private void ThreadIdModeButtonClick(object sender, EventArgs e)
	{
		m_ThreadIdMode = (ThreadIdMode)((int)(m_ThreadIdMode + 1) % Enum.GetNames(typeof(ThreadIdMode)).Length);
		m_Settings.ThreadIdMode = m_ThreadIdMode;
		m_Settings.Write();
		OnThreadIdModeChanged();
	}

	private void OnThreadIdModeChanged()
	{
		UpdateThreadIdModeButtonText();
		UpdateTimeSpanGraphs();
	}

	private void UpdateThreadIdModeButtonText()
	{
		m_ThreadIdModeButton.ButtonText = m_ThreadIdMode.ToString();
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.m_ThreadsPanel = new System.Windows.Forms.Panel();
		this.m_TimeSpanGraphPanel = new System.Windows.Forms.Panel();
		this.m_TimeSpanGraphPanelInner = new System.Windows.Forms.Panel();
		this.verticalLabelPanel1 = new FramePro.VerticalLabelPanel();
		this.m_VScrollBar = new System.Windows.Forms.VScrollBar();
		this.m_TimelinePanel = new System.Windows.Forms.Panel();
		this.m_Timeline = new FramePro.Timeline();
		this.timelineInfoPanel1 = new FramePro.LeftBackPanel();
		this.button2 = new System.Windows.Forms.Button();
		this.m_ThreadFilterTextBox = new System.Windows.Forms.TextBox();
		this.button1 = new System.Windows.Forms.Button();
		this.m_ThreadIdModeButton = new Editor.Button();
		this.verticalLabelPanel3 = new FramePro.VerticalLabelPanel();
		this.m_CoreViewSplitter = new System.Windows.Forms.Splitter();
		this.m_CoreBackPanel = new System.Windows.Forms.Panel();
		this.m_CoreGraph = new FramePro.CoreGraphPanel();
		this.verticalLabelPanel2 = new FramePro.VerticalLabelPanel();
		this.m_TimeSpanGraphSplitter = new System.Windows.Forms.Splitter();
		this.m_FrameGraphSplitter = new System.Windows.Forms.Splitter();
		this.m_FrameGraphPanel = new System.Windows.Forms.Panel();
		this.m_FrameGraph = new FramePro.FrameGraphPanel();
		this.leftBackPanel1 = new FramePro.LeftBackPanel();
		this.panel2 = new System.Windows.Forms.Panel();
		this.m_FrameInfoPanel = new FramePro.FrameInfoPanel();
		this.m_FrameGraphYAxis = new FramePro.FrameGraphYAxis();
		this.verticalLabelPanel5 = new FramePro.VerticalLabelPanel();
		this.panel4 = new System.Windows.Forms.Panel();
		this.m_InfoPanelPanel = new System.Windows.Forms.Panel();
		this.m_InfoPanel = new FramePro.InfoPanel();
		this.verticalLabelPanel6 = new FramePro.VerticalLabelPanel();
		this.m_SessionScrollBarPanel = new FramePro.SessionScrollBarPanel();
		this.m_CustomGraphPanel = new System.Windows.Forms.Panel();
		this.m_CustomStatsGraph = new FramePro.CustomStatsGraph();
		this.leftBackPanel2 = new FramePro.LeftBackPanel();
		this.panel1 = new System.Windows.Forms.Panel();
		this.m_CustomStatSelectorButton = new System.Windows.Forms.Button();
		this.verticalLabelPanel4 = new FramePro.VerticalLabelPanel();
		this.m_CustomGraphPanelSplitter = new System.Windows.Forms.Splitter();
		this.m_TimespanGraphView = new FramePro.TimeSpanGraphView();
		this.m_ScopeDataGrid = new FramePro.ScopeDataGrid();
		this.m_DataGridSplitter = new System.Windows.Forms.Splitter();
		this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
		this.m_ThreadsPanel.SuspendLayout();
		this.m_TimeSpanGraphPanel.SuspendLayout();
		this.m_TimelinePanel.SuspendLayout();
		this.timelineInfoPanel1.SuspendLayout();
		this.m_CoreBackPanel.SuspendLayout();
		this.m_FrameGraphPanel.SuspendLayout();
		this.leftBackPanel1.SuspendLayout();
		this.panel2.SuspendLayout();
		this.m_InfoPanelPanel.SuspendLayout();
		this.m_CustomGraphPanel.SuspendLayout();
		this.leftBackPanel2.SuspendLayout();
		base.SuspendLayout();
		this.m_ThreadsPanel.Controls.Add(this.m_TimeSpanGraphPanel);
		this.m_ThreadsPanel.Controls.Add(this.verticalLabelPanel1);
		this.m_ThreadsPanel.Controls.Add(this.m_VScrollBar);
		this.m_ThreadsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_ThreadsPanel.Location = new System.Drawing.Point(0, 725);
		this.m_ThreadsPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_ThreadsPanel.Name = "m_ThreadsPanel";
		this.m_ThreadsPanel.Size = new System.Drawing.Size(1648, 295);
		this.m_ThreadsPanel.TabIndex = 14;
		this.m_TimeSpanGraphPanel.BackColor = System.Drawing.Color.FromArgb(165, 165, 165);
		this.m_TimeSpanGraphPanel.Controls.Add(this.m_TimeSpanGraphPanelInner);
		this.m_TimeSpanGraphPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_TimeSpanGraphPanel.Location = new System.Drawing.Point(34, 0);
		this.m_TimeSpanGraphPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TimeSpanGraphPanel.Name = "m_TimeSpanGraphPanel";
		this.m_TimeSpanGraphPanel.Size = new System.Drawing.Size(1597, 295);
		this.m_TimeSpanGraphPanel.TabIndex = 4;
		this.m_TimeSpanGraphPanel.Resize += new System.EventHandler(TimeSpanGraphPanelResize);
		this.m_TimeSpanGraphPanelInner.Location = new System.Drawing.Point(0, 0);
		this.m_TimeSpanGraphPanelInner.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TimeSpanGraphPanelInner.Name = "m_TimeSpanGraphPanelInner";
		this.m_TimeSpanGraphPanelInner.Size = new System.Drawing.Size(1648, 262);
		this.m_TimeSpanGraphPanelInner.TabIndex = 0;
		this.m_TimeSpanGraphPanelInner.Resize += new System.EventHandler(TimeSpanPanelResize);
		this.verticalLabelPanel1.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel1.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel1.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel1.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel1.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel1.Name = "verticalLabelPanel1";
		this.verticalLabelPanel1.PanelText = "Threads";
		this.verticalLabelPanel1.Size = new System.Drawing.Size(34, 295);
		this.verticalLabelPanel1.TabIndex = 7;
		this.m_VScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_VScrollBar.Location = new System.Drawing.Point(1631, 0);
		this.m_VScrollBar.Name = "m_VScrollBar";
		this.m_VScrollBar.Size = new System.Drawing.Size(17, 295);
		this.m_VScrollBar.TabIndex = 8;
		this.m_VScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(TimeSpanVScrollBarScroll);
		this.m_TimelinePanel.Controls.Add(this.m_Timeline);
		this.m_TimelinePanel.Controls.Add(this.timelineInfoPanel1);
		this.m_TimelinePanel.Controls.Add(this.verticalLabelPanel3);
		this.m_TimelinePanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TimelinePanel.Location = new System.Drawing.Point(0, 648);
		this.m_TimelinePanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TimelinePanel.Name = "m_TimelinePanel";
		this.m_TimelinePanel.Size = new System.Drawing.Size(1648, 77);
		this.m_TimelinePanel.TabIndex = 0;
		this.m_Timeline.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_Timeline.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_Timeline.FrameLineColour = System.Drawing.Color.White;
		this.m_Timeline.Location = new System.Drawing.Point(259, 0);
		this.m_Timeline.Margin = new System.Windows.Forms.Padding(6);
		this.m_Timeline.MouseHoverEnabled = true;
		this.m_Timeline.Name = "m_Timeline";
		this.m_Timeline.Size = new System.Drawing.Size(1389, 77);
		this.m_Timeline.TabIndex = 1;
		this.m_Timeline.MeasureLineChanged += new FramePro.MeasureLineChangedHandler(MeasureLineChanged);
		this.timelineInfoPanel1.BackColor = System.Drawing.Color.LightGray;
		this.timelineInfoPanel1.Controls.Add(this.button2);
		this.timelineInfoPanel1.Controls.Add(this.m_ThreadFilterTextBox);
		this.timelineInfoPanel1.Controls.Add(this.button1);
		this.timelineInfoPanel1.Controls.Add(this.m_ThreadIdModeButton);
		this.timelineInfoPanel1.Dock = System.Windows.Forms.DockStyle.Left;
		this.timelineInfoPanel1.Location = new System.Drawing.Point(34, 0);
		this.timelineInfoPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.timelineInfoPanel1.Name = "timelineInfoPanel1";
		this.timelineInfoPanel1.Size = new System.Drawing.Size(225, 77);
		this.timelineInfoPanel1.TabIndex = 9;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.button2.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(122, 122, 122);
		this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button2.Location = new System.Drawing.Point(189, 43);
		this.button2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(34, 34);
		this.button2.TabIndex = 10;
		this.button2.Text = "X";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(FilterThreadsTextBoxClearButtonPressed);
		this.m_ThreadFilterTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ThreadFilterTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ThreadFilterTextBox.Location = new System.Drawing.Point(0, 43);
		this.m_ThreadFilterTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_ThreadFilterTextBox.Name = "m_ThreadFilterTextBox";
		this.m_ThreadFilterTextBox.Size = new System.Drawing.Size(188, 29);
		this.m_ThreadFilterTextBox.TabIndex = 1;
		this.m_ThreadFilterTextBox.TextChanged += new System.EventHandler(ThreadFilterTextChanged);
		this.m_ThreadFilterTextBox.Enter += new System.EventHandler(ThreadFilterTextBoxEntered);
		this.m_ThreadFilterTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FilterThreadsTextBoxKeyDown);
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.button1.BackColor = System.Drawing.Color.FromArgb(225, 225, 225);
		this.button1.FlatAppearance.BorderSize = 0;
		this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(0, 0);
		this.button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(162, 48);
		this.button1.TabIndex = 0;
		this.button1.Text = "Filter Threads...";
		this.button1.UseVisualStyleBackColor = false;
		this.button1.Click += new System.EventHandler(FilterThreadsButtonClicked);
		this.m_ThreadIdModeButton.BackColor = System.Drawing.Color.FromArgb(225, 225, 225);
		this.m_ThreadIdModeButton.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.m_ThreadIdModeButton.ButtonText = "Name";
		this.m_ThreadIdModeButton.HoverColour = System.Drawing.Color.FromArgb(245, 245, 245);
		this.m_ThreadIdModeButton.Image = null;
		this.m_ThreadIdModeButton.Location = new System.Drawing.Point(162, -1);
		this.m_ThreadIdModeButton.Name = "m_ThreadIdModeButton";
		this.m_ThreadIdModeButton.Size = new System.Drawing.Size(62, 50);
		this.m_ThreadIdModeButton.TabIndex = 11;
		this.toolTip1.SetToolTip(this.m_ThreadIdModeButton, "Show threads by name or by id");
		this.m_ThreadIdModeButton.Click += new System.EventHandler(ThreadIdModeButtonClick);
		this.verticalLabelPanel3.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel3.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel3.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel3.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel3.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel3.Name = "verticalLabelPanel3";
		this.verticalLabelPanel3.PanelText = "";
		this.verticalLabelPanel3.Size = new System.Drawing.Size(34, 77);
		this.verticalLabelPanel3.TabIndex = 8;
		this.m_CoreViewSplitter.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.m_CoreViewSplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_CoreViewSplitter.Location = new System.Drawing.Point(0, 1020);
		this.m_CoreViewSplitter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_CoreViewSplitter.Name = "m_CoreViewSplitter";
		this.m_CoreViewSplitter.Size = new System.Drawing.Size(1648, 5);
		this.m_CoreViewSplitter.TabIndex = 2;
		this.m_CoreViewSplitter.TabStop = false;
		this.m_CoreViewSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(CoreGraphSplitterMoved);
		this.m_CoreBackPanel.Controls.Add(this.m_CoreGraph);
		this.m_CoreBackPanel.Controls.Add(this.verticalLabelPanel2);
		this.m_CoreBackPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_CoreBackPanel.Location = new System.Drawing.Point(0, 1025);
		this.m_CoreBackPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_CoreBackPanel.Name = "m_CoreBackPanel";
		this.m_CoreBackPanel.Size = new System.Drawing.Size(1648, 269);
		this.m_CoreBackPanel.TabIndex = 1;
		this.m_CoreGraph.Active = false;
		this.m_CoreGraph.ContextSwitchesVisible = false;
		this.m_CoreGraph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_CoreGraph.Location = new System.Drawing.Point(34, 0);
		this.m_CoreGraph.Margin = new System.Windows.Forms.Padding(6);
		this.m_CoreGraph.MouseHoverEnabled = true;
		this.m_CoreGraph.Name = "m_CoreGraph";
		this.m_CoreGraph.Size = new System.Drawing.Size(1614, 269);
		this.m_CoreGraph.TabIndex = 9;
		this.m_CoreGraph.WaitEventsVisible = false;
		this.m_CoreGraph.ContextSwitchesVisibleChanged += new FramePro.ContextSwitchesVisibleChangedHandler(CoreContextSwitchesVisibleChanged);
		this.m_CoreGraph.WaitEventsVisibleChanged += new FramePro.WaitEventsVisibleChangedHandler(CoreGraphWaitEventsVisibleChanged);
		this.m_CoreGraph.TimeRangeChanged += new FramePro.CoreGraphTimeRangeChangedHandler(CoreGraphTimeRangeChanged);
		this.m_CoreGraph.CoreGraphShowThread += new FramePro.CoreGraphShowThreadHandler(CoreGraphShowThread);
		this.m_CoreGraph.MeasureLineChanged += new FramePro.MeasureLineChangedHandler(MeasureLineChanged);
		this.m_CoreGraph.SelectedTimeSpanChanged += new FramePro.SelectedTimeSpanChangedHandler(SelectedTimeSpanChangedEvent);
		this.m_CoreGraph.StopTrackingEnd += new FramePro.StopTrackingEndHandler(CoreGraphStopTrackingEnd);
		this.verticalLabelPanel2.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel2.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel2.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel2.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel2.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel2.Name = "verticalLabelPanel2";
		this.verticalLabelPanel2.PanelText = "Cores";
		this.verticalLabelPanel2.Size = new System.Drawing.Size(34, 269);
		this.verticalLabelPanel2.TabIndex = 8;
		this.m_TimeSpanGraphSplitter.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.m_TimeSpanGraphSplitter.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TimeSpanGraphSplitter.Location = new System.Drawing.Point(0, 603);
		this.m_TimeSpanGraphSplitter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TimeSpanGraphSplitter.Name = "m_TimeSpanGraphSplitter";
		this.m_TimeSpanGraphSplitter.Size = new System.Drawing.Size(1648, 5);
		this.m_TimeSpanGraphSplitter.TabIndex = 17;
		this.m_TimeSpanGraphSplitter.TabStop = false;
		this.m_TimeSpanGraphSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(TimeSpanGraphSplitterMoved);
		this.m_FrameGraphSplitter.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.m_FrameGraphSplitter.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_FrameGraphSplitter.Location = new System.Drawing.Point(0, 381);
		this.m_FrameGraphSplitter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_FrameGraphSplitter.Name = "m_FrameGraphSplitter";
		this.m_FrameGraphSplitter.Size = new System.Drawing.Size(1648, 5);
		this.m_FrameGraphSplitter.TabIndex = 16;
		this.m_FrameGraphSplitter.TabStop = false;
		this.m_FrameGraphSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(FrameGraphSplitterMoved);
		this.m_FrameGraphPanel.Controls.Add(this.m_FrameGraph);
		this.m_FrameGraphPanel.Controls.Add(this.leftBackPanel1);
		this.m_FrameGraphPanel.Controls.Add(this.verticalLabelPanel5);
		this.m_FrameGraphPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_FrameGraphPanel.Location = new System.Drawing.Point(0, 227);
		this.m_FrameGraphPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_FrameGraphPanel.Name = "m_FrameGraphPanel";
		this.m_FrameGraphPanel.Size = new System.Drawing.Size(1648, 154);
		this.m_FrameGraphPanel.TabIndex = 9;
		this.m_FrameGraph.Active = false;
		this.m_FrameGraph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_FrameGraph.Location = new System.Drawing.Point(259, 0);
		this.m_FrameGraph.Margin = new System.Windows.Forms.Padding(6);
		this.m_FrameGraph.Name = "m_FrameGraph";
		this.m_FrameGraph.ShowEvents = true;
		this.m_FrameGraph.ShowingTimeSpans = false;
		this.m_FrameGraph.Size = new System.Drawing.Size(1389, 154);
		this.m_FrameGraph.TabIndex = 5;
		this.m_FrameGraph.TargetFrameMS = 0.0;
		this.m_FrameGraph.YScale = 1.0;
		this.m_FrameGraph.VisibleRangeChanged += new FramePro.VisibleRangeChangedHandler(FrameGraphSelectionChanged);
		this.m_FrameGraph.RangeChanged += new FramePro.RangeChangedHandler(OnFrameGraphRangeChanged);
		this.m_FrameGraph.SelectedRangeChanged += new FramePro.SelectedRangeChangedHandler(FrameGraphSelectedRangeChanged);
		this.leftBackPanel1.Controls.Add(this.panel2);
		this.leftBackPanel1.Controls.Add(this.m_FrameGraphYAxis);
		this.leftBackPanel1.Dock = System.Windows.Forms.DockStyle.Left;
		this.leftBackPanel1.Location = new System.Drawing.Point(34, 0);
		this.leftBackPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.leftBackPanel1.Name = "leftBackPanel1";
		this.leftBackPanel1.Size = new System.Drawing.Size(225, 154);
		this.leftBackPanel1.TabIndex = 10;
		this.panel2.Controls.Add(this.m_FrameInfoPanel);
		this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel2.Location = new System.Drawing.Point(0, 0);
		this.panel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(193, 154);
		this.panel2.TabIndex = 2;
		this.m_FrameInfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_FrameInfoPanel.Location = new System.Drawing.Point(0, 0);
		this.m_FrameInfoPanel.Margin = new System.Windows.Forms.Padding(6);
		this.m_FrameInfoPanel.Name = "m_FrameInfoPanel";
		this.m_FrameInfoPanel.Size = new System.Drawing.Size(193, 154);
		this.m_FrameInfoPanel.TabIndex = 0;
		this.m_FrameGraphYAxis.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_FrameGraphYAxis.Location = new System.Drawing.Point(193, 0);
		this.m_FrameGraphYAxis.Margin = new System.Windows.Forms.Padding(6);
		this.m_FrameGraphYAxis.Name = "m_FrameGraphYAxis";
		this.m_FrameGraphYAxis.Size = new System.Drawing.Size(32, 154);
		this.m_FrameGraphYAxis.TabIndex = 1;
		this.m_FrameGraphYAxis.TargetFrameMS = 0.0;
		this.m_FrameGraphYAxis.YScale = 0.0;
		this.m_FrameGraphYAxis.FrameGraphYAxisScaleChanged += new FramePro.FrameGraphYAxisScaleChangedHandler(FrameGraphYAxisScaleChanged);
		this.m_FrameGraphYAxis.FrameGraphYAxisTargetMSChanged += new FramePro.FrameGraphYAxisTargetMSChangedHandler(FrameGraphYAxisTargetMsChanged);
		this.verticalLabelPanel5.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel5.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel5.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel5.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel5.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel5.Name = "verticalLabelPanel5";
		this.verticalLabelPanel5.PanelText = "Frame";
		this.verticalLabelPanel5.Size = new System.Drawing.Size(34, 154);
		this.verticalLabelPanel5.TabIndex = 9;
		this.panel4.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel4.Location = new System.Drawing.Point(0, 222);
		this.panel4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel4.Name = "panel4";
		this.panel4.Size = new System.Drawing.Size(1648, 5);
		this.panel4.TabIndex = 15;
		this.m_InfoPanelPanel.Controls.Add(this.m_InfoPanel);
		this.m_InfoPanelPanel.Controls.Add(this.verticalLabelPanel6);
		this.m_InfoPanelPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_InfoPanelPanel.Location = new System.Drawing.Point(0, 0);
		this.m_InfoPanelPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_InfoPanelPanel.Name = "m_InfoPanelPanel";
		this.m_InfoPanelPanel.Size = new System.Drawing.Size(1648, 160);
		this.m_InfoPanelPanel.TabIndex = 13;
		this.m_InfoPanel.BackColor = System.Drawing.Color.WhiteSmoke;
		this.m_InfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_InfoPanel.Location = new System.Drawing.Point(34, 0);
		this.m_InfoPanel.Margin = new System.Windows.Forms.Padding(6, 8, 6, 8);
		this.m_InfoPanel.Name = "m_InfoPanel";
		this.m_InfoPanel.Size = new System.Drawing.Size(1614, 160);
		this.m_InfoPanel.TabIndex = 12;
		this.verticalLabelPanel6.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel6.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel6.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel6.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel6.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel6.Name = "verticalLabelPanel6";
		this.verticalLabelPanel6.PanelText = "INFO";
		this.verticalLabelPanel6.Size = new System.Drawing.Size(34, 160);
		this.verticalLabelPanel6.TabIndex = 13;
		this.m_SessionScrollBarPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_SessionScrollBarPanel.Location = new System.Drawing.Point(0, 160);
		this.m_SessionScrollBarPanel.Margin = new System.Windows.Forms.Padding(6);
		this.m_SessionScrollBarPanel.Name = "m_SessionScrollBarPanel";
		this.m_SessionScrollBarPanel.Size = new System.Drawing.Size(1648, 62);
		this.m_SessionScrollBarPanel.TabIndex = 20;
		this.m_SessionScrollBarPanel.TargetFrameMS = 0.0;
		this.m_SessionScrollBarPanel.ScrollBarChanged += new FramePro.SessionScrollBarChangedHandler(SessionScrollBarChanged);
		this.m_CustomGraphPanel.Controls.Add(this.m_CustomStatsGraph);
		this.m_CustomGraphPanel.Controls.Add(this.leftBackPanel2);
		this.m_CustomGraphPanel.Controls.Add(this.verticalLabelPanel4);
		this.m_CustomGraphPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_CustomGraphPanel.Location = new System.Drawing.Point(0, 608);
		this.m_CustomGraphPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_CustomGraphPanel.Name = "m_CustomGraphPanel";
		this.m_CustomGraphPanel.Size = new System.Drawing.Size(1648, 35);
		this.m_CustomGraphPanel.TabIndex = 21;
		this.m_CustomStatsGraph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_CustomStatsGraph.Location = new System.Drawing.Point(259, 0);
		this.m_CustomStatsGraph.Margin = new System.Windows.Forms.Padding(6);
		this.m_CustomStatsGraph.Name = "m_CustomStatsGraph";
		this.m_CustomStatsGraph.Size = new System.Drawing.Size(1389, 35);
		this.m_CustomStatsGraph.TabIndex = 12;
		this.m_CustomStatsGraph.YScale = 1.0;
		this.m_CustomStatsGraph.TimeRangeChanged += new FramePro.ValueGraphTimeRangeChangedHandler(CustomStatGraphTimeRangeChanged);
		this.leftBackPanel2.BackColor = System.Drawing.Color.FromArgb(196, 196, 196);
		this.leftBackPanel2.Controls.Add(this.panel1);
		this.leftBackPanel2.Controls.Add(this.m_CustomStatSelectorButton);
		this.leftBackPanel2.Dock = System.Windows.Forms.DockStyle.Left;
		this.leftBackPanel2.Location = new System.Drawing.Point(34, 0);
		this.leftBackPanel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.leftBackPanel2.Name = "leftBackPanel2";
		this.leftBackPanel2.Size = new System.Drawing.Size(225, 35);
		this.leftBackPanel2.TabIndex = 11;
		this.panel1.BackColor = System.Drawing.Color.FromArgb(122, 122, 122);
		this.panel1.Location = new System.Drawing.Point(0, 35);
		this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(224, 2);
		this.panel1.TabIndex = 2;
		this.m_CustomStatSelectorButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_CustomStatSelectorButton.BackColor = System.Drawing.Color.FromArgb(225, 225, 225);
		this.m_CustomStatSelectorButton.FlatAppearance.BorderSize = 0;
		this.m_CustomStatSelectorButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.m_CustomStatSelectorButton.Location = new System.Drawing.Point(0, 0);
		this.m_CustomStatSelectorButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_CustomStatSelectorButton.Name = "m_CustomStatSelectorButton";
		this.m_CustomStatSelectorButton.Size = new System.Drawing.Size(224, 35);
		this.m_CustomStatSelectorButton.TabIndex = 1;
		this.m_CustomStatSelectorButton.Text = "Custom Stat...";
		this.m_CustomStatSelectorButton.UseVisualStyleBackColor = false;
		this.m_CustomStatSelectorButton.Click += new System.EventHandler(ShowCustomStatButtonClicked);
		this.verticalLabelPanel4.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel4.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel4.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel4.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel4.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
		this.verticalLabelPanel4.Name = "verticalLabelPanel4";
		this.verticalLabelPanel4.PanelText = "";
		this.verticalLabelPanel4.Size = new System.Drawing.Size(34, 35);
		this.verticalLabelPanel4.TabIndex = 9;
		this.m_CustomGraphPanelSplitter.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.m_CustomGraphPanelSplitter.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_CustomGraphPanelSplitter.Location = new System.Drawing.Point(0, 643);
		this.m_CustomGraphPanelSplitter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_CustomGraphPanelSplitter.Name = "m_CustomGraphPanelSplitter";
		this.m_CustomGraphPanelSplitter.Size = new System.Drawing.Size(1648, 5);
		this.m_CustomGraphPanelSplitter.TabIndex = 22;
		this.m_CustomGraphPanelSplitter.TabStop = false;
		this.m_CustomGraphPanelSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(CustomStatsGraphSplitterMoved);
		this.m_TimespanGraphView.Active = false;
		this.m_TimespanGraphView.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TimespanGraphView.Location = new System.Drawing.Point(0, 386);
		this.m_TimespanGraphView.Margin = new System.Windows.Forms.Padding(6);
		this.m_TimespanGraphView.Name = "m_TimespanGraphView";
		this.m_TimespanGraphView.Size = new System.Drawing.Size(1648, 217);
		this.m_TimespanGraphView.TabIndex = 19;
		this.m_TimespanGraphView.YScale = 0.0;
		this.m_TimespanGraphView.VisibleRangeChanged += new FramePro.VisibleRangeChangedHandler(FrameGraphSelectionChanged);
		this.m_TimespanGraphView.RangeChanged += new FramePro.RangeChangedHandler(OnFrameGraphRangeChanged);
		this.m_TimespanGraphView.SelectionChanged += new FramePro.TimeSpanGraphViewSelectionChangedHandler(SelectedTimeSpanChangedEvent);
		this.m_TimespanGraphView.FrameGraphYAxisScaleChanged += new FramePro.FrameGraphYAxisScaleChangedHandler(TimeSpanGraphViewYAxisScaleChanged);
		this.m_TimespanGraphView.TimeSpanGraphViewTargetMSChanged += new FramePro.TimeSpanGraphViewTargetMSChangedHandler(TimeSpanGraphViewTargetMSChanged);
		this.m_TimespanGraphView.SelectedRangeChanged += new FramePro.SelectedRangeChangedHandler(FrameGraphSelectedRangeChanged);
		this.m_ScopeDataGrid.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_ScopeDataGrid.Location = new System.Drawing.Point(1652, 0);
		this.m_ScopeDataGrid.Margin = new System.Windows.Forms.Padding(6);
		this.m_ScopeDataGrid.Name = "m_ScopeDataGrid";
		this.m_ScopeDataGrid.Size = new System.Drawing.Size(422, 1294);
		this.m_ScopeDataGrid.TabIndex = 23;
		this.m_ScopeDataGrid.TimeSpan = null;
		this.m_DataGridSplitter.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.m_DataGridSplitter.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_DataGridSplitter.Location = new System.Drawing.Point(1648, 0);
		this.m_DataGridSplitter.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_DataGridSplitter.Name = "m_DataGridSplitter";
		this.m_DataGridSplitter.Size = new System.Drawing.Size(4, 1294);
		this.m_DataGridSplitter.TabIndex = 24;
		this.m_DataGridSplitter.TabStop = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_ThreadsPanel);
		base.Controls.Add(this.m_TimelinePanel);
		base.Controls.Add(this.m_CustomGraphPanelSplitter);
		base.Controls.Add(this.m_CustomGraphPanel);
		base.Controls.Add(this.m_CoreViewSplitter);
		base.Controls.Add(this.m_CoreBackPanel);
		base.Controls.Add(this.m_TimeSpanGraphSplitter);
		base.Controls.Add(this.m_TimespanGraphView);
		base.Controls.Add(this.m_FrameGraphSplitter);
		base.Controls.Add(this.m_FrameGraphPanel);
		base.Controls.Add(this.panel4);
		base.Controls.Add(this.m_SessionScrollBarPanel);
		base.Controls.Add(this.m_InfoPanelPanel);
		base.Controls.Add(this.m_DataGridSplitter);
		base.Controls.Add(this.m_ScopeDataGrid);
		base.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		base.Name = "ThreadsView";
		base.Size = new System.Drawing.Size(2074, 1294);
		this.m_ThreadsPanel.ResumeLayout(false);
		this.m_TimeSpanGraphPanel.ResumeLayout(false);
		this.m_TimelinePanel.ResumeLayout(false);
		this.timelineInfoPanel1.ResumeLayout(false);
		this.timelineInfoPanel1.PerformLayout();
		this.m_CoreBackPanel.ResumeLayout(false);
		this.m_FrameGraphPanel.ResumeLayout(false);
		this.leftBackPanel1.ResumeLayout(false);
		this.panel2.ResumeLayout(false);
		this.m_InfoPanelPanel.ResumeLayout(false);
		this.m_CustomGraphPanel.ResumeLayout(false);
		this.leftBackPanel2.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
