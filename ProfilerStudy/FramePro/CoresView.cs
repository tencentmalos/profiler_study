using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SCLCoreCLR;

namespace FramePro;

internal class CoresView : SessionView
{
    private Session m_Session;

    private Settings m_Settings;

    private TimeRange m_SelectedTimeRange = new TimeRange();

    private bool m_TrackEnd;

    private Set<long> m_HighlightedTimeSpans = new Set<long>();

    private string m_HighlightFilter;

    private int m_LastUpdateInfoPanelTime;

    private const int m_UpdateInfoPanelInterval = 1000;

    private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

    private long m_SelectedTimeSpan = -1L;

    private bool m_SessionIsReady;

    private float m_DPIScale;

    private IContainer components;

    private Panel m_InfoPanelPanel;

    private InfoPanel m_InfoPanel;

    private VerticalLabelPanel verticalLabelPanelInfo;

    private Panel panel4;

    private Panel m_FrameGraphPanel;

    private FrameGraphPanel m_FrameGraph;

    private LeftBackPanel leftBackPanel1;

    private VerticalLabelPanel verticalLabelPanelFrame;

    private Splitter m_FrameGraphSplitter;

    private TimeSpanGraphView m_TimespanGraphView;

    private Splitter m_CoreViewSplitter;

    private Panel m_CoreBackPanel;

    private Panel m_CoreGraphPanel;

    private CoreGraph m_CoreGraph;

    private Panel m_CoreGraphLeftPanel;

    private Panel m_TimelinePanel;

    private Timeline m_Timeline;

    private LeftBackPanel timelineInfoPanel1;

    private Panel panel1;

    private LeftBackPanel leftBackPanel2;

    private VerticalLabelPanel verticalLabelPanelCores;

    private LeftBackPanel m_TimeLineLine;

    private CheckBox m_ContextSwitchesCheckBox;

    private CheckBox m_ScopeHeirachyCheckBox;

    private CorePanel m_CorePanel;

    private Splitter m_TimeSpanGraphSplitter;

    private FrameGraphYAxis m_FrameGraphYAxis;

    private SessionScrollBarPanel m_SessionScrollBarPanel;

    private CheckBox m_WaitEventsCheckBox;

    private FrameInfoPanel m_FrameInfoPanel;

    public override string ViewName => "Cores";

    public override Session Session => m_Session;

    public override int HighlightedTimeSpanCount => m_CoreGraph.HighlightedTimeSpansCount;

    private long SelectEndTime => m_SelectedTimeRange.m_StartTime + m_CoreGraph.Width * m_SelectedTimeRange.m_TicksPerPixel;

    public bool InfoPanelVisible => m_Settings.CoresViewInfoPanelVisible;

    public bool FrameGraphVisible => m_Settings.CoresViewFrameGraphVisible;

    public bool TimeSpanGraphVisible => m_Settings.CoresViewTimeSpanGraphVisible;

    public event SelectetdTimeRangeChangedHandler SelectedTimeRangeChanged;

    public event ShowThreadHandler ShowThread;

    public event SectionVisibilityChangedHandler ViewVisibilityChanged;

    public event TargetMSChangedHandler FrameTargetMSChanged;

    public event TargetMSChangedHandler TimeSpanTargetMSChanged;

    public event SelectedTimeSpanChangedHandler SelectedTimeSpanChanged;

    public event SelectedRangeChangedHandler SelectedRangeChanged;

    public event RangeChangedHandler FrameGraphRangeChanged;

    public CoresView()
    {
    }

    public CoresView(Session session, Settings settings)
    {
        InitializeComponent();
        m_Session = session;
        m_Settings = settings;
        m_DPIScale = MainForm.DPIScale;
        HookSessionEvents();
        ShowInfoPanel(m_Settings.CoresViewInfoPanelVisible);
        ShowFrameGraph(m_Settings.CoresViewFrameGraphVisible);
        ShowTimeSpanGraph(m_Settings.CoresViewTimeSpanGraphVisible);
        m_CorePanel.SetSession(session);
        m_CorePanel.SetCoreGraph(m_CoreGraph);
        m_SessionScrollBarPanel.SetSession(session);
        m_SessionScrollBarPanel.SetSettings(settings);
        m_SessionScrollBarPanel.TargetFrameMS = m_Settings.TargetFrameMS;
        m_FrameGraph.SetSettings(m_Settings);
        m_FrameGraph.SetSession(m_Session);
        m_FrameGraph.YScale = m_Settings.CoresViewFrameGraphYScale;
        m_FrameGraph.TargetFrameMS = m_Settings.TargetFrameMS;
        m_FrameGraphPanel.Height = ScaleDPI(m_Settings.FrameGraphHeight);
        m_FrameGraphYAxis.SetSettings(m_Settings);
        m_FrameGraphYAxis.YScale = m_Settings.CoresViewFrameGraphYScale;
        m_FrameGraphYAxis.TargetFrameMS = m_Settings.TargetFrameMS;
        m_CoreGraph.ContextSwitchesVisible = m_Settings.CoresViewContextSwitchesVisible;
        m_CoreGraph.ShowWaitEvents = m_Settings.CoresViewWaitEventsVisible;
        m_CoreGraph.SetSettings(settings);
        m_CoreGraph.SetSession(session);
        m_InfoPanel.SetSession(session);
        m_Timeline.SetSession(session);
        m_Timeline.FrameLineColour = Colours.CoresViewTimelineFrameLine;
        m_TimespanGraphView.SetSettings(m_Settings);
        m_TimespanGraphView.SetSession(session);
        m_TimespanGraphView.YScale = m_Settings.CoresViewFrameGraphYScale;
        m_TimespanGraphView.Height = ScaleDPI(m_Settings.TimeSpanGraphHeight);
        m_CoreGraph.ShowHeirachy = m_Settings.CoresViewShowHeirachy;
        m_ScopeHeirachyCheckBox.Checked = m_Settings.CoresViewShowHeirachy;
        m_ContextSwitchesCheckBox.Checked = m_Settings.CoresViewContextSwitchesVisible;
        m_WaitEventsCheckBox.Checked = m_Settings.CoresViewContextSwitchesVisible;
        UpdateMouseHoverEnabled();
    }

    private int ScaleDPI(int value)
    {
        return (int)((float)value * m_DPIScale);
    }

    protected override void WndProc(ref Message m)
    {
        m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
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
        m_Session.ThreadOrderChanged += OnThreadOrderChanged;
        m_Session.SessionIsReady += OnSessionIsReady;
        m_Session.NonInteractiveModeFinished += OnNonInteractiveModeFinished;
        m_Session.ETLTraceFinished += ETLTraceFinished;
        m_Session.FinishedProcessingPackets += SessionFinishedProcessingPackets;
    }

    private void SessionFinishedProcessingPackets()
    {
        m_ControlTaskDispatcher.QueueTask(SessionFinishedProcessingPackets_Main);
    }

    private void SessionFinishedProcessingPackets_Main()
    {
        RefreshAllViews();
    }

    private void OnThreadOrderChanged()
    {
        m_ControlTaskDispatcher.QueueTask(OnThreadOrderChanged_Main);
    }

    private void OnThreadOrderChanged_Main()
    {
        m_CoreGraph.UpdateThreadColours();
    }

    private void OnDisconnected()
    {
        m_ControlTaskDispatcher.QueueTask(OnDisconnected_MainThread);
    }

    private void OnDisconnected_MainThread()
    {
        UpdateMouseHoverEnabled();
        m_SessionScrollBarPanel.OnSessionChanged();
        m_FrameGraph.UpdateShowingProcessingDataMessage();
        m_CoreGraph.OnDisconnected();
    }

    private void RefreshAllViews()
    {
        if (m_SessionIsReady)
        {
            UpdateFramesDataGrid();
            m_TimespanGraphView.UpdateTimeSpanDataGrid();
            m_TimespanGraphView.RecalculateView(force: true);
            m_FrameGraph.RecalculateView(force: true);
            UpdateInfoPanel();
            m_CoreGraph.Refresh();
        }
    }

    private void OnSessionIsReady()
    {
        m_ControlTaskDispatcher.QueueTask(OnSessionIsReady_Main);
    }

    private void OnSessionIsReady_Main()
    {
        m_SessionIsReady = true;
        m_SessionScrollBarPanel.OnSessionChanged();
        m_CoreGraph.UpdateThreadColours();
        m_FrameGraph.UpdateShowingProcessingDataMessage();
        RefreshAllViews();
    }

    public override void UpdateView()
    {
        if (!m_Session.Connected && !m_Session.ProcessingPackets)
        {
            return;
        }
        m_SessionScrollBarPanel.OnSessionChanged();
        if (m_FrameGraph.NeedsUpdate)
        {
            m_FrameGraph.RecalculateView(force: true);
        }
        if (m_TimespanGraphView.NeedsUpdate)
        {
            m_TimespanGraphView.RecalculateView(force: true);
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

    private void UpdateInfoPanel()
    {
        m_InfoPanel.UpdateInfo();
    }

    private void UpdateFramesDataGrid()
    {
        m_FrameInfoPanel.UpdateStats(m_Session);
    }

    private void CoreGraphTimeRangeChanged(TimeRange time_range)
    {
        m_SelectedTimeRange.CopyAllExceptScrollY(time_range);
        OnVisibleTimeRangeChanged(m_CoreGraph, scroll_into_view: true, refresh_core_graph: true);
    }

    private static long XToTime(int x, TimeRange visible_time_range)
    {
        return visible_time_range.m_StartTime + x * visible_time_range.m_TicksPerPixel;
    }

    private void CoreGraphMouseWheel(int delta, Point mouse_pt)
    {
        if (!Utils.IsShiftHeld)
        {
            long num = XToTime(mouse_pt.X, m_SelectedTimeRange);
            double num2 = ((delta > 0) ? MainSessionView.ZoomMultiplier : (1.0 / MainSessionView.ZoomMultiplier));
            SetSelectedTimeRangeScale(m_SelectedTimeRange.m_Scale * num2);
            m_SelectedTimeRange.m_StartTime += num - XToTime(mouse_pt.X, m_SelectedTimeRange);
            OnVisibleTimeRangeChanged(null, scroll_into_view: false, refresh_core_graph: true);
        }
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
        if (m_SelectedTimeRange.m_TicksPerPixel != num2)
        {
            m_SelectedTimeRange.m_Scale = num;
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
        if (!flag && m_CoreGraph.RectangleToScreen(m_CoreGraph.ClientRectangle).Contains(point))
        {
            Point mouse_pt = m_CoreGraph.PointToClient(point);
            CoreGraphMouseWheel(delta, mouse_pt);
        }
    }

    private void FrameGraphSelectionChanged(Control sender, long start_time, long end_time)
    {
        SetVisibleTimeRange(sender, start_time, end_time, scroll_into_view: true, refresh_core_graph: true);
        StopTrackingEnd();
    }

    private void SetSelectedTimeRange(long start_time, long end_time)
    {
        SetVisibleTimeRange(null, start_time, end_time, scroll_into_view: true, refresh_core_graph: true);
    }

    public void SetVisibleTimeRange(long start_time, long end_time, bool refresh_core_graph)
    {
        SetVisibleTimeRange(null, start_time, end_time, scroll_into_view: true, refresh_core_graph);
    }

    private void SetVisibleTimeRange(Control sender, long start_time, long end_time, bool scroll_into_view, bool refresh_core_graph)
    {
        Utils.SetTimeRange(start_time, end_time, m_CoreGraph.Width, m_Session.TimerFrequency, m_SelectedTimeRange);
        OnVisibleTimeRangeChanged(sender, scroll_into_view, refresh_core_graph);
    }

    private void OnVisibleTimeRangeChanged(Control sender, bool scroll_into_view, bool refresh_core_graph)
    {
        if (m_TimespanGraphView != sender)
        {
            m_TimespanGraphView.SetSelectedRange(m_SelectedTimeRange.m_StartTime, SelectEndTime, scroll_into_view);
        }
        if (m_FrameGraph != sender)
        {
            m_FrameGraph.SetVisibleRange(m_SelectedTimeRange.m_StartTime, SelectEndTime, scroll_into_view);
        }
        if (refresh_core_graph)
        {
            if (m_Timeline != sender)
            {
                m_Timeline.SetTimeRange(m_SelectedTimeRange);
            }
            if (m_CoreGraph != sender)
            {
                m_CoreGraph.SetTimeRange(m_SelectedTimeRange);
            }
        }
        if (this.SelectedTimeRangeChanged != null)
        {
            this.SelectedTimeRangeChanged(this, m_SelectedTimeRange.m_StartTime, SelectEndTime);
        }
    }

    public override void OnTrackEndChanged()
    {
        UpdateMouseHoverEnabled();
        base.OnTrackEndChanged();
    }

    private void UpdateMouseHoverEnabled()
    {
        bool mouseHoverEnabled = !m_TrackEnd;
        m_Timeline.MouseHoverEnabled = mouseHoverEnabled;
        m_CoreGraph.MouseHoverEnabled = mouseHoverEnabled;
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
        if (this.FrameGraphRangeChanged != null)
        {
            this.FrameGraphRangeChanged(sender, start_frame_x, end_frame_x, view_scale);
        }
    }

    public void SetFrameGraphRange(long start_frame_x, double view_scale)
    {
        m_FrameGraph.SetRange(start_frame_x, view_scale);
    }

    private void GraphTimeSpan(long time_span_name)
    {
        m_TimespanGraphView.SetTimeSpan(time_span_name);
        if (!TimeSpanGraphVisible)
        {
            ShowTimeSpanGraph(visible: true);
            OnViewVisibilityChanged();
        }
        if (this.SelectedTimeSpanChanged != null)
        {
            this.SelectedTimeSpanChanged(time_span_name, null);
        }
    }

    private void OnViewVisibilityChanged()
    {
        if (this.ViewVisibilityChanged != null)
        {
            this.ViewVisibilityChanged();
        }
    }

    public void ShowInfoPanel(bool visible)
    {
        if (m_Settings.CoresViewInfoPanelVisible != visible)
        {
            m_Settings.CoresViewInfoPanelVisible = visible;
            m_Settings.Write();
        }
        m_InfoPanelPanel.Visible = visible;
    }

    public void ShowFrameGraph(bool visible)
    {
        if (m_Settings.CoresViewFrameGraphVisible != visible)
        {
            m_Settings.CoresViewFrameGraphVisible = visible;
            m_Settings.Write();
        }
        m_FrameGraphPanel.Visible = visible;
        m_FrameGraphSplitter.Visible = visible;
    }

    public void ShowTimeSpanGraph(bool visible)
    {
        if (m_Settings.CoresViewTimeSpanGraphVisible != visible)
        {
            m_Settings.CoresViewTimeSpanGraphVisible = visible;
            m_Settings.Write();
        }
        m_TimespanGraphView.Visible = visible;
        m_TimeSpanGraphSplitter.Visible = visible;
    }

    public void HighlightTimeSpans(string filter)
    {
        m_HighlightFilter = filter.Trim();
        UpdateHighlightFilter();
    }

    private void UpdateHighlightFilter()
    {
        Set<long> set = ((m_HighlightFilter.Length != 0) ? m_Session.GetTimerNames(m_HighlightFilter) : new Set<long>());
        if (!m_HighlightedTimeSpans.SequenceEqual(set))
        {
            m_HighlightedTimeSpans = set;
            m_CoreGraph.SetHighlightedTimeSpans(set);
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
        }
        else
        {
            SetSelectedTimeRange(timeSpan.StartTime, timeSpan.EndTime);
        }
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
        }
        else
        {
            SetSelectedTimeRange(timeSpan.StartTime, timeSpan.EndTime);
        }
    }

    private void MeasureLineChanged(int start_x, int end_x)
    {
        m_Timeline.SetMeasureLine(start_x, end_x);
        m_CoreGraph.SetMeasureLine(start_x, end_x);
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
                SetSelectedTimeRange(frame.StartTime, frame.EndTime);
                return;
            }
            m_SelectedTimeRange.m_StartTime = frame.StartTime;
            OnVisibleTimeRangeChanged(null, scroll_into_view: true, refresh_core_graph: true);
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
                SetSelectedTimeRange(frame.StartTime, frame.EndTime);
                return;
            }
            m_SelectedTimeRange.m_StartTime = frame.StartTime;
            OnVisibleTimeRangeChanged(null, scroll_into_view: true, refresh_core_graph: true);
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
            m_SelectedTimeRange.m_StartTime = m_Session.FirstFrameTime;
            OnVisibleTimeRangeChanged(null, scroll_into_view: true, refresh_core_graph: true);
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
        int num = m_Session.FindPrevSpike(m_SelectedTimeRange.m_StartTime);
        if (num != -1)
        {
            SelectAndCentreFrame(num);
        }
    }

    public void GotoNextSpike()
    {
        int num = m_Session.FindNextSpike(m_SelectedTimeRange.m_StartTime);
        if (num != -1)
        {
            SelectAndCentreFrame(num);
        }
    }

    private void SelectAndCentreFrame(int frame_index)
    {
        Frame frame = m_Session.GetFrame(frame_index);
        SetSelectedTimeRange(frame.StartTime, frame.EndTime);
        m_FrameGraph.CentreFrame(frame_index);
        m_TimespanGraphView.CentreFrame(frame_index);
        StopTrackingEnd();
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
        m_TimespanGraphView.Refresh();
    }

    private void CoreGraphShowThread(int thread_id)
    {
        if (this.ShowThread != null)
        {
            this.ShowThread(thread_id);
        }
    }

    private void ContextSwitchesCheckBoxCheckChanged(object sender, EventArgs e)
    {
        m_CoreGraph.ContextSwitchesVisible = m_ContextSwitchesCheckBox.Checked;
        m_CoreGraph.Refresh();
        if (m_Settings.CoresViewContextSwitchesVisible != m_ContextSwitchesCheckBox.Checked)
        {
            m_Settings.CoresViewContextSwitchesVisible = m_ContextSwitchesCheckBox.Checked;
            m_Settings.Write();
        }
    }

    private void WaitEventsCheckBoxChanged(object sender, EventArgs e)
    {
        m_CoreGraph.ShowWaitEvents = m_WaitEventsCheckBox.Checked;
        if (m_Settings.CoresViewWaitEventsVisible != m_WaitEventsCheckBox.Checked)
        {
            m_Settings.CoresViewWaitEventsVisible = m_WaitEventsCheckBox.Checked;
            m_Settings.Write();
        }
    }

    private void ScopeHeirachyCheckBoxCheckChanged(object sender, EventArgs e)
    {
        m_CoreGraph.ShowHeirachy = m_ScopeHeirachyCheckBox.Checked;
        if (m_Settings.CoresViewShowHeirachy != m_ScopeHeirachyCheckBox.Checked)
        {
            m_Settings.CoresViewShowHeirachy = m_ScopeHeirachyCheckBox.Checked;
            m_Settings.Write();
        }
    }

    public void OnScopeColourModeChanged()
    {
        m_CoreGraph.OnScopeColourModeChanged();
    }

    private void CoreGraphScrollChanged(int scroll_y)
    {
        m_CorePanel.SetScrollY(scroll_y);
    }

    public override void OnActiveChanged()
    {
        m_FrameGraph.Active = base.Active;
        m_TimespanGraphView.Active = base.Active;
        m_CoreGraph.Active = base.Active;
        m_SessionScrollBarPanel.UpdateSessionScrollBarMode();
        base.OnActiveChanged();
    }

    private void SelectedTimeSpanChangedEvent(long time_span_name, TimeSpan time_span)
    {
        if (m_SelectedTimeSpan != time_span_name)
        {
            SetSelectedTimeSpan(time_span_name);
            if (this.SelectedTimeSpanChanged != null)
            {
                this.SelectedTimeSpanChanged(time_span_name, time_span);
            }
        }
    }

    private void FrameGraphYAxisScaleChanged(double y_scale)
    {
        m_Settings.CoresViewFrameGraphYScale = y_scale;
        m_FrameGraph.YScale = y_scale;
        m_TimespanGraphView.YScale = y_scale;
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

    private void ScopeGraphYScaleChanged(double y_scale)
    {
        m_Settings.CoresViewFrameGraphYScale = y_scale;
        m_FrameGraph.YScale = y_scale;
        m_FrameGraphYAxis.YScale = y_scale;
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
        if (m_SelectedTimeSpan != time_span_name)
        {
            m_SelectedTimeSpan = time_span_name;
            m_TimespanGraphView.SetTimeSpan(time_span_name);
            m_CoreGraph.SelectTimeSpan(time_span_name);
            m_TimespanGraphView.UpdateTimeSpanDataGrid();
            if (!TimeSpanGraphVisible)
            {
                ShowTimeSpanGraph(visible: true);
                OnViewVisibilityChanged();
            }
        }
    }

    private void CoreGraphStopTrackingEnd()
    {
        StopTrackingEnd();
    }

    private void SessionScrollBarChanged(long start_frame_x, long end_frame_x)
    {
        StopTrackingEnd();
        long start_time = m_Session.FrameXToTime(start_frame_x);
        long end_time = m_Session.FrameXToTime(end_frame_x);
        m_TimespanGraphView.SetTimeRange(start_time, end_time);
        m_FrameGraph.SetTimeRange(start_time, end_time);
    }

    public override void OnScopeColourChanged()
    {
        m_CoreGraph.OnScopeColourChanged();
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

    private void InitializeComponent()
    {
        this.m_InfoPanelPanel = new System.Windows.Forms.Panel();
        this.m_InfoPanel = new FramePro.InfoPanel();
        this.verticalLabelPanelInfo = new FramePro.VerticalLabelPanel();
        this.panel4 = new System.Windows.Forms.Panel();
        this.m_FrameGraphPanel = new System.Windows.Forms.Panel();
        this.m_FrameGraph = new FramePro.FrameGraphPanel();
        this.leftBackPanel1 = new FramePro.LeftBackPanel();
        this.m_FrameInfoPanel = new FramePro.FrameInfoPanel();
        this.m_FrameGraphYAxis = new FramePro.FrameGraphYAxis();
        this.verticalLabelPanelFrame = new FramePro.VerticalLabelPanel();
        this.m_FrameGraphSplitter = new System.Windows.Forms.Splitter();
        this.m_TimespanGraphView = new FramePro.TimeSpanGraphView();
        this.m_CoreViewSplitter = new System.Windows.Forms.Splitter();
        this.m_CoreBackPanel = new System.Windows.Forms.Panel();
        this.m_CoreGraphPanel = new System.Windows.Forms.Panel();
        this.m_CoreGraph = new FramePro.CoreGraph();
        this.m_CoreGraphLeftPanel = new System.Windows.Forms.Panel();
        this.m_CorePanel = new FramePro.CorePanel();
        this.m_TimelinePanel = new System.Windows.Forms.Panel();
        this.m_Timeline = new FramePro.Timeline();
        this.m_TimeLineLine = new FramePro.LeftBackPanel();
        this.timelineInfoPanel1 = new FramePro.LeftBackPanel();
        this.panel1 = new System.Windows.Forms.Panel();
        this.m_WaitEventsCheckBox = new System.Windows.Forms.CheckBox();
        this.m_ScopeHeirachyCheckBox = new System.Windows.Forms.CheckBox();
        this.m_ContextSwitchesCheckBox = new System.Windows.Forms.CheckBox();
        this.leftBackPanel2 = new FramePro.LeftBackPanel();
        this.verticalLabelPanelCores = new FramePro.VerticalLabelPanel();
        this.m_TimeSpanGraphSplitter = new System.Windows.Forms.Splitter();
        this.m_SessionScrollBarPanel = new FramePro.SessionScrollBarPanel();
        this.m_InfoPanelPanel.SuspendLayout();
        this.m_FrameGraphPanel.SuspendLayout();
        this.leftBackPanel1.SuspendLayout();
        this.m_CoreBackPanel.SuspendLayout();
        this.m_CoreGraphPanel.SuspendLayout();
        this.m_CoreGraphLeftPanel.SuspendLayout();
        this.m_TimelinePanel.SuspendLayout();
        this.panel1.SuspendLayout();
        this.SuspendLayout();
        // 
        // m_InfoPanelPanel
        // 
        this.m_InfoPanelPanel.Controls.Add(this.m_InfoPanel);
        this.m_InfoPanelPanel.Controls.Add(this.verticalLabelPanelInfo);
        this.m_InfoPanelPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_InfoPanelPanel.Location = new System.Drawing.Point(0, 0);
        this.m_InfoPanelPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_InfoPanelPanel.Name = "m_InfoPanelPanel";
        this.m_InfoPanelPanel.Size = new System.Drawing.Size(1748, 144);
        this.m_InfoPanelPanel.TabIndex = 14;
        // 
        // m_InfoPanel
        // 
        this.m_InfoPanel.BackColor = System.Drawing.Color.WhiteSmoke;
        this.m_InfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_InfoPanel.Location = new System.Drawing.Point(34, 0);
        this.m_InfoPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_InfoPanel.Name = "m_InfoPanel";
        this.m_InfoPanel.Size = new System.Drawing.Size(1714, 144);
        this.m_InfoPanel.TabIndex = 12;
        // 
        // verticalLabelPanelInfo
        // 
        this.verticalLabelPanelInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(222)))), ((int)(((byte)(222)))));
        this.verticalLabelPanelInfo.Dock = System.Windows.Forms.DockStyle.Left;
        this.verticalLabelPanelInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.verticalLabelPanelInfo.Location = new System.Drawing.Point(0, 0);
        this.verticalLabelPanelInfo.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
        this.verticalLabelPanelInfo.Name = "verticalLabelPanelInfo";
        this.verticalLabelPanelInfo.PanelText = "INFO";
        this.verticalLabelPanelInfo.Size = new System.Drawing.Size(34, 144);
        this.verticalLabelPanelInfo.TabIndex = 13;
        // 
        // panel4
        // 
        this.panel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(99)))), ((int)(((byte)(99)))));
        this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
        this.panel4.Location = new System.Drawing.Point(0, 199);
        this.panel4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.panel4.Name = "panel4";
        this.panel4.Size = new System.Drawing.Size(1748, 4);
        this.panel4.TabIndex = 16;
        // 
        // m_FrameGraphPanel
        // 
        this.m_FrameGraphPanel.Controls.Add(this.m_FrameGraph);
        this.m_FrameGraphPanel.Controls.Add(this.leftBackPanel1);
        this.m_FrameGraphPanel.Controls.Add(this.verticalLabelPanelFrame);
        this.m_FrameGraphPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_FrameGraphPanel.Location = new System.Drawing.Point(0, 203);
        this.m_FrameGraphPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_FrameGraphPanel.Name = "m_FrameGraphPanel";
        this.m_FrameGraphPanel.Size = new System.Drawing.Size(1748, 138);
        this.m_FrameGraphPanel.TabIndex = 17;
        // 
        // m_FrameGraph
        // 
        this.m_FrameGraph.Active = false;
        this.m_FrameGraph.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_FrameGraph.Location = new System.Drawing.Point(259, 0);
        this.m_FrameGraph.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_FrameGraph.Name = "m_FrameGraph";
        this.m_FrameGraph.ShowEvents = true;
        this.m_FrameGraph.ShowingTimeSpans = false;
        this.m_FrameGraph.Size = new System.Drawing.Size(1489, 138);
        this.m_FrameGraph.TabIndex = 5;
        this.m_FrameGraph.TargetFrameMS = 0D;
        this.m_FrameGraph.YScale = 0D;
        this.m_FrameGraph.VisibleRangeChanged += new FramePro.VisibleRangeChangedHandler(this.FrameGraphSelectionChanged);
        this.m_FrameGraph.RangeChanged += new FramePro.RangeChangedHandler(this.OnFrameGraphRangeChanged);
        this.m_FrameGraph.SelectedRangeChanged += new FramePro.SelectedRangeChangedHandler(this.FrameGraphSelectedRangeChanged);
        // 
        // leftBackPanel1
        // 
        this.leftBackPanel1.Controls.Add(this.m_FrameInfoPanel);
        this.leftBackPanel1.Controls.Add(this.m_FrameGraphYAxis);
        this.leftBackPanel1.Dock = System.Windows.Forms.DockStyle.Left;
        this.leftBackPanel1.Location = new System.Drawing.Point(34, 0);
        this.leftBackPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.leftBackPanel1.Name = "leftBackPanel1";
        this.leftBackPanel1.Size = new System.Drawing.Size(225, 138);
        this.leftBackPanel1.TabIndex = 10;
        // 
        // m_FrameInfoPanel
        // 
        this.m_FrameInfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_FrameInfoPanel.Location = new System.Drawing.Point(0, 0);
        this.m_FrameInfoPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_FrameInfoPanel.Name = "m_FrameInfoPanel";
        this.m_FrameInfoPanel.Size = new System.Drawing.Size(193, 138);
        this.m_FrameInfoPanel.TabIndex = 2;
        // 
        // m_FrameGraphYAxis
        // 
        this.m_FrameGraphYAxis.Dock = System.Windows.Forms.DockStyle.Right;
        this.m_FrameGraphYAxis.Location = new System.Drawing.Point(193, 0);
        this.m_FrameGraphYAxis.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_FrameGraphYAxis.Name = "m_FrameGraphYAxis";
        this.m_FrameGraphYAxis.Size = new System.Drawing.Size(32, 138);
        this.m_FrameGraphYAxis.TabIndex = 1;
        this.m_FrameGraphYAxis.TargetFrameMS = 0D;
        this.m_FrameGraphYAxis.YScale = 0D;
        this.m_FrameGraphYAxis.FrameGraphYAxisScaleChanged += new FramePro.FrameGraphYAxisScaleChangedHandler(this.FrameGraphYAxisScaleChanged);
        this.m_FrameGraphYAxis.FrameGraphYAxisTargetMSChanged += new FramePro.FrameGraphYAxisTargetMSChangedHandler(this.FrameGraphYAxisTargetMsChanged);
        // 
        // verticalLabelPanelFrame
        // 
        this.verticalLabelPanelFrame.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(222)))), ((int)(((byte)(222)))));
        this.verticalLabelPanelFrame.Dock = System.Windows.Forms.DockStyle.Left;
        this.verticalLabelPanelFrame.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.verticalLabelPanelFrame.Location = new System.Drawing.Point(0, 0);
        this.verticalLabelPanelFrame.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
        this.verticalLabelPanelFrame.Name = "verticalLabelPanelFrame";
        this.verticalLabelPanelFrame.PanelText = "Frame";
        this.verticalLabelPanelFrame.Size = new System.Drawing.Size(34, 138);
        this.verticalLabelPanelFrame.TabIndex = 9;
        // 
        // m_FrameGraphSplitter
        // 
        this.m_FrameGraphSplitter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(99)))), ((int)(((byte)(99)))));
        this.m_FrameGraphSplitter.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_FrameGraphSplitter.Location = new System.Drawing.Point(0, 341);
        this.m_FrameGraphSplitter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_FrameGraphSplitter.Name = "m_FrameGraphSplitter";
        this.m_FrameGraphSplitter.Size = new System.Drawing.Size(1748, 4);
        this.m_FrameGraphSplitter.TabIndex = 18;
        this.m_FrameGraphSplitter.TabStop = false;
        this.m_FrameGraphSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.FrameGraphSplitterMoved);
        // 
        // m_TimespanGraphView
        // 
        this.m_TimespanGraphView.Active = false;
        this.m_TimespanGraphView.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_TimespanGraphView.Location = new System.Drawing.Point(0, 345);
        this.m_TimespanGraphView.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_TimespanGraphView.Name = "m_TimespanGraphView";
        this.m_TimespanGraphView.Size = new System.Drawing.Size(1748, 195);
        this.m_TimespanGraphView.TabIndex = 20;
        this.m_TimespanGraphView.YScale = 0D;
        this.m_TimespanGraphView.VisibleRangeChanged += new FramePro.VisibleRangeChangedHandler(this.FrameGraphSelectionChanged);
        this.m_TimespanGraphView.RangeChanged += new FramePro.RangeChangedHandler(this.OnFrameGraphRangeChanged);
        this.m_TimespanGraphView.SelectionChanged += new FramePro.TimeSpanGraphViewSelectionChangedHandler(this.SelectedTimeSpanChangedEvent);
        this.m_TimespanGraphView.FrameGraphYAxisScaleChanged += new FramePro.FrameGraphYAxisScaleChangedHandler(this.ScopeGraphYScaleChanged);
        this.m_TimespanGraphView.TimeSpanGraphViewTargetMSChanged += new FramePro.TimeSpanGraphViewTargetMSChangedHandler(this.TimeSpanGraphViewTargetMSChanged);
        this.m_TimespanGraphView.SelectedRangeChanged += new FramePro.SelectedRangeChangedHandler(this.FrameGraphSelectedRangeChanged);
        // 
        // m_CoreViewSplitter
        // 
        this.m_CoreViewSplitter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(99)))), ((int)(((byte)(99)))));
        this.m_CoreViewSplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.m_CoreViewSplitter.Location = new System.Drawing.Point(0, 1303);
        this.m_CoreViewSplitter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_CoreViewSplitter.Name = "m_CoreViewSplitter";
        this.m_CoreViewSplitter.Size = new System.Drawing.Size(1748, 4);
        this.m_CoreViewSplitter.TabIndex = 25;
        this.m_CoreViewSplitter.TabStop = false;
        // 
        // m_CoreBackPanel
        // 
        this.m_CoreBackPanel.Controls.Add(this.m_CoreGraphPanel);
        this.m_CoreBackPanel.Controls.Add(this.m_TimelinePanel);
        this.m_CoreBackPanel.Controls.Add(this.panel1);
        this.m_CoreBackPanel.Controls.Add(this.verticalLabelPanelCores);
        this.m_CoreBackPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_CoreBackPanel.Location = new System.Drawing.Point(0, 544);
        this.m_CoreBackPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_CoreBackPanel.Name = "m_CoreBackPanel";
        this.m_CoreBackPanel.Size = new System.Drawing.Size(1748, 759);
        this.m_CoreBackPanel.TabIndex = 23;
        // 
        // m_CoreGraphPanel
        // 
        this.m_CoreGraphPanel.Controls.Add(this.m_CoreGraph);
        this.m_CoreGraphPanel.Controls.Add(this.m_CoreGraphLeftPanel);
        this.m_CoreGraphPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_CoreGraphPanel.Location = new System.Drawing.Point(34, 104);
        this.m_CoreGraphPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_CoreGraphPanel.Name = "m_CoreGraphPanel";
        this.m_CoreGraphPanel.Size = new System.Drawing.Size(1714, 655);
        this.m_CoreGraphPanel.TabIndex = 10;
        // 
        // m_CoreGraph
        // 
        this.m_CoreGraph.Active = false;
        this.m_CoreGraph.ContextSwitchesVisible = false;
        this.m_CoreGraph.CoreRectHeight = 12;
        this.m_CoreGraph.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_CoreGraph.Location = new System.Drawing.Point(202, 0);
        this.m_CoreGraph.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_CoreGraph.MouseHoverEnabled = true;
        this.m_CoreGraph.Name = "m_CoreGraph";
        this.m_CoreGraph.NotShowingHeirachyCoreHeight = 40;
        this.m_CoreGraph.ShowHeirachy = true;
        this.m_CoreGraph.ShowWaitEvents = false;
        this.m_CoreGraph.Size = new System.Drawing.Size(1512, 655);
        this.m_CoreGraph.TabIndex = 2;
        this.m_CoreGraph.TimeRangeChanged += new FramePro.CoreGraphTimeRangeChangedHandler(this.CoreGraphTimeRangeChanged);
        this.m_CoreGraph.CoreGraphShowThread += new FramePro.CoreGraphShowThreadHandler(this.CoreGraphShowThread);
        this.m_CoreGraph.MeasureLineChanged += new FramePro.MeasureLineChangedHandler(this.MeasureLineChanged);
        this.m_CoreGraph.CoreScrollChanged += new FramePro.CoreScrollChangedHandler(this.CoreGraphScrollChanged);
        this.m_CoreGraph.SelectedTimeSpanChanged += new FramePro.SelectedTimeSpanChangedHandler(this.SelectedTimeSpanChangedEvent);
        this.m_CoreGraph.StopTrackingEnd += new FramePro.StopTrackingEndHandler(this.CoreGraphStopTrackingEnd);
        // 
        // m_CoreGraphLeftPanel
        // 
        this.m_CoreGraphLeftPanel.Controls.Add(this.m_CorePanel);
        this.m_CoreGraphLeftPanel.Dock = System.Windows.Forms.DockStyle.Left;
        this.m_CoreGraphLeftPanel.Location = new System.Drawing.Point(0, 0);
        this.m_CoreGraphLeftPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_CoreGraphLeftPanel.Name = "m_CoreGraphLeftPanel";
        this.m_CoreGraphLeftPanel.Size = new System.Drawing.Size(202, 655);
        this.m_CoreGraphLeftPanel.TabIndex = 0;
        // 
        // m_CorePanel
        // 
        this.m_CorePanel.BackColor = System.Drawing.Color.DimGray;
        this.m_CorePanel.CoreYGap = 20;
        this.m_CorePanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_CorePanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.m_CorePanel.ForeColor = System.Drawing.Color.White;
        this.m_CorePanel.Location = new System.Drawing.Point(0, 0);
        this.m_CorePanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_CorePanel.Name = "m_CorePanel";
        this.m_CorePanel.Size = new System.Drawing.Size(202, 655);
        this.m_CorePanel.TabIndex = 0;
        // 
        // m_TimelinePanel
        // 
        this.m_TimelinePanel.Controls.Add(this.m_Timeline);
        this.m_TimelinePanel.Controls.Add(this.m_TimeLineLine);
        this.m_TimelinePanel.Controls.Add(this.timelineInfoPanel1);
        this.m_TimelinePanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_TimelinePanel.Location = new System.Drawing.Point(34, 35);
        this.m_TimelinePanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_TimelinePanel.Name = "m_TimelinePanel";
        this.m_TimelinePanel.Size = new System.Drawing.Size(1714, 69);
        this.m_TimelinePanel.TabIndex = 21;
        // 
        // m_Timeline
        // 
        this.m_Timeline.Dock = System.Windows.Forms.DockStyle.Fill;
        this.m_Timeline.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.m_Timeline.FrameLineColour = System.Drawing.Color.Black;
        this.m_Timeline.Location = new System.Drawing.Point(202, 4);
        this.m_Timeline.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_Timeline.MouseHoverEnabled = true;
        this.m_Timeline.Name = "m_Timeline";
        this.m_Timeline.Size = new System.Drawing.Size(1512, 65);
        this.m_Timeline.TabIndex = 1;
        this.m_Timeline.MeasureLineChanged += new FramePro.MeasureLineChangedHandler(this.MeasureLineChanged);
        // 
        // m_TimeLineLine
        // 
        this.m_TimeLineLine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(99)))), ((int)(((byte)(99)))));
        this.m_TimeLineLine.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_TimeLineLine.Location = new System.Drawing.Point(202, 0);
        this.m_TimeLineLine.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_TimeLineLine.Name = "m_TimeLineLine";
        this.m_TimeLineLine.Size = new System.Drawing.Size(1512, 4);
        this.m_TimeLineLine.TabIndex = 11;
        // 
        // timelineInfoPanel1
        // 
        this.timelineInfoPanel1.BackColor = System.Drawing.Color.LightGray;
        this.timelineInfoPanel1.Dock = System.Windows.Forms.DockStyle.Left;
        this.timelineInfoPanel1.Location = new System.Drawing.Point(0, 0);
        this.timelineInfoPanel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.timelineInfoPanel1.Name = "timelineInfoPanel1";
        this.timelineInfoPanel1.Size = new System.Drawing.Size(202, 69);
        this.timelineInfoPanel1.TabIndex = 9;
        // 
        // panel1
        // 
        this.panel1.BackColor = System.Drawing.Color.DimGray;
        this.panel1.Controls.Add(this.m_WaitEventsCheckBox);
        this.panel1.Controls.Add(this.m_ScopeHeirachyCheckBox);
        this.panel1.Controls.Add(this.m_ContextSwitchesCheckBox);
        this.panel1.Controls.Add(this.leftBackPanel2);
        this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
        this.panel1.Location = new System.Drawing.Point(34, 0);
        this.panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.panel1.Name = "panel1";
        this.panel1.Size = new System.Drawing.Size(1714, 35);
        this.panel1.TabIndex = 22;
        // 
        // m_WaitEventsCheckBox
        // 
        this.m_WaitEventsCheckBox.AutoSize = true;
        this.m_WaitEventsCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.m_WaitEventsCheckBox.ForeColor = System.Drawing.Color.White;
        this.m_WaitEventsCheckBox.Location = new System.Drawing.Point(422, 8);
        this.m_WaitEventsCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_WaitEventsCheckBox.Name = "m_WaitEventsCheckBox";
        this.m_WaitEventsCheckBox.Size = new System.Drawing.Size(125, 24);
        this.m_WaitEventsCheckBox.TabIndex = 15;
        this.m_WaitEventsCheckBox.Text = "Wait Events";
        this.m_WaitEventsCheckBox.UseVisualStyleBackColor = true;
        this.m_WaitEventsCheckBox.CheckedChanged += new System.EventHandler(this.WaitEventsCheckBoxChanged);
        // 
        // m_ScopeHeirachyCheckBox
        // 
        this.m_ScopeHeirachyCheckBox.AutoSize = true;
        this.m_ScopeHeirachyCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.m_ScopeHeirachyCheckBox.ForeColor = System.Drawing.Color.White;
        this.m_ScopeHeirachyCheckBox.Location = new System.Drawing.Point(597, 8);
        this.m_ScopeHeirachyCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_ScopeHeirachyCheckBox.Name = "m_ScopeHeirachyCheckBox";
        this.m_ScopeHeirachyCheckBox.Size = new System.Drawing.Size(154, 24);
        this.m_ScopeHeirachyCheckBox.TabIndex = 12;
        this.m_ScopeHeirachyCheckBox.Text = "Scope Heirachy";
        this.m_ScopeHeirachyCheckBox.UseVisualStyleBackColor = true;
        this.m_ScopeHeirachyCheckBox.CheckedChanged += new System.EventHandler(this.ScopeHeirachyCheckBoxCheckChanged);
        // 
        // m_ContextSwitchesCheckBox
        // 
        this.m_ContextSwitchesCheckBox.AutoSize = true;
        this.m_ContextSwitchesCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.m_ContextSwitchesCheckBox.ForeColor = System.Drawing.Color.White;
        this.m_ContextSwitchesCheckBox.Location = new System.Drawing.Point(212, 8);
        this.m_ContextSwitchesCheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_ContextSwitchesCheckBox.Name = "m_ContextSwitchesCheckBox";
        this.m_ContextSwitchesCheckBox.Size = new System.Drawing.Size(165, 24);
        this.m_ContextSwitchesCheckBox.TabIndex = 11;
        this.m_ContextSwitchesCheckBox.Text = "Context Switches";
        this.m_ContextSwitchesCheckBox.UseVisualStyleBackColor = true;
        this.m_ContextSwitchesCheckBox.CheckedChanged += new System.EventHandler(this.ContextSwitchesCheckBoxCheckChanged);
        // 
        // leftBackPanel2
        // 
        this.leftBackPanel2.BackColor = System.Drawing.Color.LightGray;
        this.leftBackPanel2.Dock = System.Windows.Forms.DockStyle.Left;
        this.leftBackPanel2.Location = new System.Drawing.Point(0, 0);
        this.leftBackPanel2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.leftBackPanel2.Name = "leftBackPanel2";
        this.leftBackPanel2.Size = new System.Drawing.Size(202, 35);
        this.leftBackPanel2.TabIndex = 10;
        // 
        // verticalLabelPanelCores
        // 
        this.verticalLabelPanelCores.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(222)))), ((int)(((byte)(222)))));
        this.verticalLabelPanelCores.Dock = System.Windows.Forms.DockStyle.Left;
        this.verticalLabelPanelCores.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.verticalLabelPanelCores.Location = new System.Drawing.Point(0, 0);
        this.verticalLabelPanelCores.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
        this.verticalLabelPanelCores.Name = "verticalLabelPanelCores";
        this.verticalLabelPanelCores.PanelText = "Cores";
        this.verticalLabelPanelCores.Size = new System.Drawing.Size(34, 759);
        this.verticalLabelPanelCores.TabIndex = 8;
        // 
        // m_TimeSpanGraphSplitter
        // 
        this.m_TimeSpanGraphSplitter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(99)))), ((int)(((byte)(99)))), ((int)(((byte)(99)))));
        this.m_TimeSpanGraphSplitter.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_TimeSpanGraphSplitter.Location = new System.Drawing.Point(0, 540);
        this.m_TimeSpanGraphSplitter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.m_TimeSpanGraphSplitter.Name = "m_TimeSpanGraphSplitter";
        this.m_TimeSpanGraphSplitter.Size = new System.Drawing.Size(1748, 4);
        this.m_TimeSpanGraphSplitter.TabIndex = 24;
        this.m_TimeSpanGraphSplitter.TabStop = false;
        this.m_TimeSpanGraphSplitter.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.TimeSpanGraphSplitterMoved);
        // 
        // m_SessionScrollBarPanel
        // 
        this.m_SessionScrollBarPanel.Dock = System.Windows.Forms.DockStyle.Top;
        this.m_SessionScrollBarPanel.Location = new System.Drawing.Point(0, 144);
        this.m_SessionScrollBarPanel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
        this.m_SessionScrollBarPanel.Name = "m_SessionScrollBarPanel";
        this.m_SessionScrollBarPanel.Size = new System.Drawing.Size(1748, 55);
        this.m_SessionScrollBarPanel.TabIndex = 26;
        this.m_SessionScrollBarPanel.TargetFrameMS = 0D;
        this.m_SessionScrollBarPanel.ScrollBarChanged += new FramePro.SessionScrollBarChangedHandler(this.SessionScrollBarChanged);
        // 
        // CoresView
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.m_CoreBackPanel);
        this.Controls.Add(this.m_CoreViewSplitter);
        this.Controls.Add(this.m_TimeSpanGraphSplitter);
        this.Controls.Add(this.m_TimespanGraphView);
        this.Controls.Add(this.m_FrameGraphSplitter);
        this.Controls.Add(this.m_FrameGraphPanel);
        this.Controls.Add(this.panel4);
        this.Controls.Add(this.m_SessionScrollBarPanel);
        this.Controls.Add(this.m_InfoPanelPanel);
        this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
        this.Name = "CoresView";
        this.Size = new System.Drawing.Size(1748, 1307);
        this.m_InfoPanelPanel.ResumeLayout(false);
        this.m_FrameGraphPanel.ResumeLayout(false);
        this.leftBackPanel1.ResumeLayout(false);
        this.m_CoreBackPanel.ResumeLayout(false);
        this.m_CoreGraphPanel.ResumeLayout(false);
        this.m_CoreGraphLeftPanel.ResumeLayout(false);
        this.m_TimelinePanel.ResumeLayout(false);
        this.panel1.ResumeLayout(false);
        this.panel1.PerformLayout();
        this.ResumeLayout(false);

        if (PlatformTool.IsRunOnWine())
        {
            //Do not support vertical layout in macos
            this.verticalLabelPanelCores.PanelText = "C";
            this.verticalLabelPanelInfo.PanelText = "I";
            this.verticalLabelPanelFrame.PanelText = "F";
        }
    }
}
