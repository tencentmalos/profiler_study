using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class CoreGraph : UserControl
{
	private enum MouseDragMode
	{
		Horizontal,
		Vertical
	}

	private class TimeSpanRenderData
	{
		public long m_StartTime;

		public long m_EndTime;

		public TimeSpan m_TimeSpan;

		public Rectangle m_Rect;

		public Brush m_Brush;

		public int m_StableSortIndex;

		public int m_ThreadId;

		public int m_Core;

		public bool m_IsIdleTimeSpan;

		public bool m_Highlight;

		public bool m_Selected;
	}

	private class ContextRenderData
	{
		public long m_StartTime;

		public long m_EndTime;

		public int m_ProcessId;

		public int m_ThreadId;

		public Rectangle m_Rect;

		public Brush m_Brush;
	}

	private class CoreRenderData
	{
		public List<TimeSpanRenderData> m_TimeSpans = new List<TimeSpanRenderData>();
	}

	private class ScopeRenderBrush
	{
		public Color m_Colour;

		public Brush m_TimeSpanBrush;

		public Brush m_IdleTimeSpanBrush;
	}

	private struct ContextSwitchRenderData
	{
		public ContextSwitch m_SessionContextSwitch;

		public Rectangle m_Rect;

		public Pen m_Pen;
	}

	private class RendererInput
	{
		private TimeRange m_VisibleTimeRange = new TimeRange();

		private List<int> m_CoreGraphHeights = new List<int>();

		private int m_ForceRecalculateCounter;

		private bool m_ShowHeirachy;

		private ScopeColourMode m_ColourMode;

		private bool m_ShowWaitEvents;

		private float m_DPIScale;

		public TimeRange VisibleTimeRange => m_VisibleTimeRange;

		public List<int> CoreGraphHeights => m_CoreGraphHeights;

		public bool ShowHeirachy => m_ShowHeirachy;

		public ScopeColourMode ColourMode => m_ColourMode;

		public bool ShowWaitEvents => m_ShowWaitEvents;

		public float DPIScale => m_DPIScale;

		public void Set(TimeRange time_range, List<int> core_graph_heights, int force_recalculate_counter, bool showing_heirachy, ScopeColourMode colour_mode, bool show_wait_events, float dpi_scale)
		{
			m_VisibleTimeRange.Copy(time_range);
			m_CoreGraphHeights.Clear();
			m_CoreGraphHeights.AddRange(core_graph_heights);
			m_ForceRecalculateCounter = force_recalculate_counter;
			m_ShowHeirachy = showing_heirachy;
			m_ColourMode = colour_mode;
			m_ShowWaitEvents = show_wait_events;
			m_DPIScale = dpi_scale;
		}

		public bool Copy(RendererInput other)
		{
			if (!m_VisibleTimeRange.Equals(other.m_VisibleTimeRange) || !Utils.ListsEquals(m_CoreGraphHeights, other.m_CoreGraphHeights) || m_ForceRecalculateCounter != other.m_ForceRecalculateCounter || m_ShowHeirachy != other.m_ShowHeirachy || m_ColourMode != other.m_ColourMode || m_ShowWaitEvents != other.m_ShowWaitEvents || m_DPIScale != other.m_DPIScale)
			{
				m_VisibleTimeRange.Copy(other.m_VisibleTimeRange);
				m_CoreGraphHeights.Clear();
				m_CoreGraphHeights.AddRange(other.m_CoreGraphHeights);
				m_ForceRecalculateCounter = other.m_ForceRecalculateCounter;
				m_ShowHeirachy = other.m_ShowHeirachy;
				m_ColourMode = other.m_ColourMode;
				m_ShowWaitEvents = other.m_ShowWaitEvents;
				m_DPIScale = other.m_DPIScale;
				return true;
			}
			return false;
		}
	}

	private class RenderData
	{
		public long m_RenderEndTime;

		public RendererInput m_RendererInput = new RendererInput();

		public List<FrameRenderData> m_FrameRenderData = new List<FrameRenderData>();

		public List<Frame> m_VisibleFrames = new List<Frame>();

		public List<CoreRenderData> m_CoreRenderData = new List<CoreRenderData>();

		public List<ContextSwitchRenderData> m_ContextSwitches = new List<ContextSwitchRenderData>();

		public List<List<ContextRenderData>> m_Contexts = new List<List<ContextRenderData>>();

		public List<WaitEventRenderData> m_WaitEvents = new List<WaitEventRenderData>();

		public int m_HighlightedTimeSpansCount;

		public bool m_ClearBrushes;
	}

	private struct WaitEventRenderData
	{
		public Point m_Point;

		public WaitEvent m_WaitEvent;
	}

	private struct FrameRenderData
	{
		public Frame m_Frame;

		public int m_X;
	}

	private class ThreadTimeSpans
	{
		public List<TimeSpanRenderData> m_TimeSpans = new List<TimeSpanRenderData>();
	}

	private Session m_Session;

	private Settings m_Settings;

	private int m_MainThreadId;

	private Pen m_FramePen = new Pen(Colours.FrameLine);

	private bool m_StartedDragMove;

	private Point m_DragMovePos;

	private long m_StartDragTime;

	private int m_StartDragScrolllY;

	private Thread m_CalculateViewThread;

	private bool m_ExitCalculateViewThread;

	private AutoResetEvent m_CalculateViewThreadWakeEvent = new AutoResetEvent(initialState: false);

	private AutoResetEvent m_RenderDataSwappedEvent = new AutoResetEvent(initialState: false);

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private TimeRange m_VisibleTimeRange = new TimeRange();

	private RendererInput m_RendererInput = new RendererInput();

	private RenderData m_RenderData = new RenderData();

	private RenderData m_RendererRenderData = new RenderData();

	private int m_UpdateColours;

	private object m_Lock = new object();

	private Brush m_CoreBrush = new SolidBrush(Colours.CoreBar);

	private List<int> m_Threads = new List<int>();

	private Set<long> m_HighlightedTimeSpans = new Set<long>();

	private Brush m_HighlightBrush = new SolidBrush(Colours.TimeSpanHighlight);

	private Brush m_SelectedTimeSpanBrush = new SolidBrush(Colours.SelectedTimeSpan);

	private TimeSpanRenderData m_ContextMenuTimeSpanRenderData;

	private ClassAllocator<TimeSpanRenderData> m_TimeSpanRenderDataAllocator = new ClassAllocator<TimeSpanRenderData>();

	private MeasureLine m_MeasureLine = new MeasureLine();

	private Pen m_MeasureLinePen = new Pen(new SolidBrush(Colours.MeasureLineColour));

	private Brush m_MeasureLineFillBrush = new SolidBrush(Colours.MeasureLineFillColour);

	private List<TimeSpanRenderData> m_AllTimeSpanRenderData = new List<TimeSpanRenderData>();

	private List<TimeSpanRenderData> m_RenderDataStack = new List<TimeSpanRenderData>();

	private bool m_MouseHoverEnabled = true;

	private Pen m_ContextSwitchPen = new Pen(new SolidBrush(Colours.CoreContextSwitch));

	private Pen m_ContextSwitchOtherProcessPen = new Pen(new SolidBrush(Colours.CoreContextSwitchOtherProcess));

	private ClassAllocator<ContextRenderData> m_ContextRenderDataAllocator = new ClassAllocator<ContextRenderData>();

	private Dictionary<int, ThreadTimeSpans> m_ThreadTimeSpanRenderData = new Dictionary<int, ThreadTimeSpans>();

	private HatchBrushManager m_HatchBrushManager = new HatchBrushManager();

	private bool m_ContextSwitchesVisible;

	private int m_CoreBarHeight = 6;

	private bool m_ShowHeirachy;

	private int m_NotShowingHeirachyCoreHeight = 20;

	private const int m_DefaultHeirachyCoreHeight = 140;

	private int m_ForceRecalculateCounter;

	private const int m_MinVertResizeHoverDist = 2;

	private int m_CoreVResizeHover = -1;

	private bool m_StartedCoreVResize;

	private int m_CoreVResizeLastY;

	private const int m_MinCoreHeight = 10;

	private bool m_Active;

	private int m_HighlightedTimeSpansCount;

	private long m_SelectedTimeSpanName = -1L;

	private List<WaitEvent> m_TempWaitEvents = new List<WaitEvent>();

	private WaitEvent m_SelectedWaitEvent;

	private bool m_WaitEventSelected;

	private Dictionary<int, int> m_SelectedStartWaitEventIndex = new Dictionary<int, int>();

	private int m_SelectedTriggerWaitEventIndex = -1;

	private Dictionary<int, int> m_SelectedStopWaitEventIndex = new Dictionary<int, int>();

	private Brush m_SelectedWaitEventFadeBrush = new SolidBrush(Colours.WaitEventHighlightFadeColour);

	private Pen m_WaitEventConnectorPen = new Pen(Colours.WaitEventConnectorColour, 2f);

	private Pen m_WaitEventPreTriggerConnectorPen = new Pen(Colours.WaitEventPreTriggerConnectorColour, 2f);

	private Pen m_WaitEventPostTriggerConnectorPen = new Pen(Colours.WaitEventPostTriggerConnectorColour, 2f);

	private Brush m_WaitEventBrush = new SolidBrush(Colours.WaitEventColour);

	private Pen m_WaitEventBorderPen = new Pen(Colours.WaitEventBorderColour);

	private Brush m_SelectedWaitEventBrush = new SolidBrush(Colours.SelectedWaitEventColour);

	private Pen m_SelectedWaitEventBorderPen = new Pen(Colours.SelectedWaitEventBorderColour);

	private const int m_SelectedWaitEventCircleRadius = 5;

	private Brush m_CoreColourABrush = new SolidBrush(Colours.CoreColourA);

	private Brush m_CoreColourBBrush = new SolidBrush(Colours.CoreColourB);

	private bool m_MouseClicked;

	private Point m_LastMovePoint;

	private Point m_MouseClickPoint;

	private bool m_ShowWaitEvents;

	private const int m_WaitEventSize = 4;

	private const int m_MouseWheelLargeJumpFraction = 3;

	private Dictionary<int, ScopeRenderBrush> m_ThreadRenderBrushes = new Dictionary<int, ScopeRenderBrush>();

	private Dictionary<long, ScopeRenderBrush> m_ScopeRenderBrushes = new Dictionary<long, ScopeRenderBrush>();

	private const int m_HighlightInflate = 2;

	private const int m_FirstCoreYOffset = 20;

	private MouseDragMode m_MouseDragMode;

	private int m_MaxDragOffset;

	private const int m_MouseDragModeSnapDist = 20;

	private float m_DPIScale = 1f;

	private IContainer components;

	private VScrollBar m_VScrollBar;

	private ContextMenuStrip m_ContextMenuStrip;

	private ToolStripMenuItem jumpToSourceToolStripMenuItem;

	private ToolStripMenuItem showInThreadViewToolStripMenuItem;

	private ToolStripMenuItem setColourToolStripMenuItem;

	public bool NeedsUpdate => m_RenderData.m_RenderEndTime < XToTime(base.ClientSize.Width, m_VisibleTimeRange);

	private Color BackgroundColour
	{
		get
		{
			if (!m_ShowHeirachy)
			{
				return Colours.CoreBackgroundNoHeirachy;
			}
			return Colours.CoreBackgroundHeirachy;
		}
	}

	public bool MouseHoverEnabled
	{
		get
		{
			return m_MouseHoverEnabled;
		}
		set
		{
			m_MouseHoverEnabled = value;
		}
	}

	public bool ContextSwitchesVisible
	{
		get
		{
			return m_ContextSwitchesVisible;
		}
		set
		{
			m_ContextSwitchesVisible = value;
		}
	}

	public int NotShowingHeirachyCoreHeight
	{
		get
		{
			return m_NotShowingHeirachyCoreHeight;
		}
		set
		{
			m_NotShowingHeirachyCoreHeight = value;
		}
	}

	public int CoreRectHeight
	{
		get
		{
			return m_CoreBarHeight;
		}
		set
		{
			m_CoreBarHeight = value;
		}
	}

	public bool ShowHeirachy
	{
		get
		{
			return m_ShowHeirachy;
		}
		set
		{
			m_ShowHeirachy = value;
			RecalculateView();
			UpdateVScrollBar();
			OnCoreHeightsChanged();
		}
	}

	public bool Active
	{
		get
		{
			return m_Active;
		}
		set
		{
			m_Active = value;
			if (value)
			{
				RecalculateView(force: true);
			}
		}
	}

	public int HighlightedTimeSpansCount => m_HighlightedTimeSpansCount;

	public bool ShowWaitEvents
	{
		get
		{
			return m_ShowWaitEvents;
		}
		set
		{
			m_ShowWaitEvents = value;
			RecalculateView();
		}
	}

	public event CoreGraphTimeRangeChangedHandler TimeRangeChanged;

	public event CoreGraphShowThreadHandler CoreGraphShowThread;

	public event MeasureLineChangedHandler MeasureLineChanged;

	public event CoreHeightsChangedHandler CoreHeightsChanged;

	public event CoreScrollChangedHandler CoreScrollChanged;

	public event SelectedTimeSpanChangedHandler SelectedTimeSpanChanged;

	public event StopTrackingEndHandler StopTrackingEnd;

	public CoreGraph()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		m_DPIScale = MainForm.DPIScale;
		m_FramePen = new Pen(Colours.FrameLine, ScaleDPI(1));
		m_MeasureLine.Changed += MeasureLineChangedEvent;
		m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
		m_CalculateViewThread = new Thread(CalculateViewThread);
		m_CalculateViewThread.Name = "CoreGraph";
		m_CalculateViewThread.Start();
	}

	private int ScaleDPI(int value)
	{
		return (int)(((float)value + 0.5f) * m_DPIScale);
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
	}

	private void MeasureLineChangedEvent(int start_x, int end_x)
	{
		if (this.MeasureLineChanged != null)
		{
			this.MeasureLineChanged(start_x, end_x);
		}
		Refresh();
	}

	public void SetSession(Session session)
	{
		m_Session = session;
	}

	public void OnDisconnected()
	{
		RecalculateView(force: true);
	}

	public void OnETLTraceFinished()
	{
		RecalculateView(force: true);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_ExitCalculateViewThread = true;
		m_CalculateViewThreadWakeEvent.Set();
		m_RenderDataSwappedEvent.Set();
		m_CalculateViewThread.Join(30000);
		m_CoreBrush.Dispose();
		m_HighlightBrush.Dispose();
		m_SelectedTimeSpanBrush.Dispose();
		m_MeasureLinePen.Dispose();
		m_MeasureLineFillBrush.Dispose();
		m_FramePen.Dispose();
		m_WaitEventConnectorPen.Dispose();
		m_WaitEventPreTriggerConnectorPen.Dispose();
		m_WaitEventPostTriggerConnectorPen.Dispose();
		m_SelectedWaitEventFadeBrush.Dispose();
		m_WaitEventBrush.Dispose();
		m_WaitEventBorderPen.Dispose();
		m_SelectedWaitEventBrush.Dispose();
		m_SelectedWaitEventBorderPen.Dispose();
		m_CoreColourABrush.Dispose();
		m_CoreColourBBrush.Dispose();
		m_CalculateViewThreadWakeEvent.Dispose();
		m_RenderDataSwappedEvent.Dispose();
		base.Dispose(disposing);
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		if (base.Visible)
		{
			RecalculateView();
		}
		base.OnVisibleChanged(e);
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void CalculateViewThread()
	{
		while (!m_ExitCalculateViewThread)
		{
			bool flag = false;
			lock (m_RendererInput)
			{
				flag = m_RendererRenderData.m_RendererInput.Copy(m_RendererInput);
			}
			if (m_RendererRenderData.m_ClearBrushes)
			{
				m_ThreadRenderBrushes.Clear();
				m_ScopeRenderBrushes.Clear();
				m_RendererRenderData.m_ClearBrushes = false;
			}
			if (flag)
			{
				CalculateView();
				m_ControlTaskDispatcher.QueueTask(OnCalculateViewComplete);
				m_RenderDataSwappedEvent.WaitOne();
			}
			else
			{
				m_CalculateViewThreadWakeEvent.WaitOne();
			}
		}
	}

	private void OnCalculateViewComplete()
	{
		CheckIsMainThread();
		Misc.Swap(ref m_RenderData, ref m_RendererRenderData);
		m_RenderDataSwappedEvent.Set();
		lock (m_Lock)
		{
			if (m_UpdateColours != 0)
			{
				m_UpdateColours--;
				RecalculateView(force: true);
			}
		}
		UpdateVScrollBar();
		UpdateHighlightedWaitEventIndices();
		Refresh();
	}

	private void UpdateVScrollBar()
	{
		if (m_Session == null)
		{
			return;
		}
		int num = GetFirstCoreYOffset(m_ShowHeirachy);
		foreach (int coreGraphHeight in GetCoreGraphHeights())
		{
			num += ScaleDPI(coreGraphHeight);
		}
		m_VScrollBar.Maximum = num;
		m_VScrollBar.LargeChange = Math.Max(0, base.ClientSize.Height);
		m_VScrollBar.Visible = num > base.ClientSize.Height;
		OnScrollChanged();
	}

	private void OnScrollChanged()
	{
		if (this.CoreScrollChanged != null)
		{
			this.CoreScrollChanged(m_VScrollBar.Value);
		}
	}

	public void UpdateGraph(bool force)
	{
		RecalculateView(force);
	}

	private void CalculateView()
	{
		if (m_Session != null && m_Session.IsReady)
		{
			TimeRange visibleTimeRange = m_RendererRenderData.m_RendererInput.VisibleTimeRange;
			long num = base.ClientSize.Width * visibleTimeRange.m_TicksPerPixel;
			long startTime = visibleTimeRange.m_StartTime;
			long num2 = startTime + num;
			m_RendererRenderData.m_RenderEndTime = Math.Min(m_Session.LastFrameEndTime, num2);
			CalculateCoreFrame(startTime, num2);
			CalculateWaitEvents(startTime, num2);
			if (m_Session.RecordingContextSwitches)
			{
				CalculateContextSwitchRenderData(startTime, num2);
			}
			else
			{
				CalculateCoreTimeSpanRenderData(startTime, num2);
			}
		}
	}

	private void CalculateWaitEvents(long start_time, long end_time)
	{
		m_RendererRenderData.m_WaitEvents.Clear();
		RendererInput rendererInput = m_RendererRenderData.m_RendererInput;
		if (!rendererInput.ShowWaitEvents)
		{
			return;
		}
		TimeRange visibleTimeRange = rendererInput.VisibleTimeRange;
		m_TempWaitEvents.Clear();
		m_Session.GetWaitEvents(start_time, end_time, m_TempWaitEvents);
		foreach (WaitEvent tempWaitEvent in m_TempWaitEvents)
		{
			Rectangle coreRect = GetCoreRect(tempWaitEvent.Core, rendererInput);
			int num = TimeToX_NoClamp(tempWaitEvent.Time, visibleTimeRange);
			int num2 = coreRect.Y + coreRect.Height / 2;
			WaitEventRenderData item = default(WaitEventRenderData);
			item.m_Point = new Point(num, num2);
			item.m_WaitEvent = tempWaitEvent;
			m_RendererRenderData.m_WaitEvents.Add(item);
		}
	}

	private void CalculateCoreFrame(long start_time, long end_time)
	{
		m_RendererRenderData.m_VisibleFrames.Clear();
		TimeRange visibleTimeRange = m_RendererRenderData.m_RendererInput.VisibleTimeRange;
		if (m_Session == null || visibleTimeRange.m_TicksPerPixel == 0L)
		{
			return;
		}
		m_Session.GetFrames(start_time, end_time, m_RendererRenderData.m_VisibleFrames);
		m_RendererRenderData.m_FrameRenderData.Clear();
		foreach (Frame visibleFrame in m_RendererRenderData.m_VisibleFrames)
		{
			FrameRenderData item = default(FrameRenderData);
			item.m_Frame = visibleFrame;
			item.m_X = TimeToX(visibleFrame.EndTime, visibleTimeRange);
			item.m_X = Misc.Clamp(item.m_X, -1, base.ClientSize.Width + 1);
			m_RendererRenderData.m_FrameRenderData.Add(item);
		}
	}

	private void CalculateContextSwitchRenderData(long start_time, long end_time)
	{
		m_RendererRenderData.m_ContextSwitches.Clear();
		foreach (List<ContextRenderData> context in m_RendererRenderData.m_Contexts)
		{
			foreach (ContextRenderData item3 in context)
			{
				m_ContextRenderDataAllocator.Free(item3);
			}
			context.Clear();
		}
		foreach (CoreRenderData coreRenderDatum in m_RendererRenderData.m_CoreRenderData)
		{
			FreeTimeSpanRenderDataList(coreRenderDatum.m_TimeSpans);
		}
		if (m_Session == null)
		{
			return;
		}
		m_Session.GetContextSwitches(start_time, end_time, out var context_switches);
		List<List<ContextRenderData>> contexts = m_RendererRenderData.m_Contexts;
		RendererInput rendererInput = m_RendererRenderData.m_RendererInput;
		TimeRange visibleTimeRange = rendererInput.VisibleTimeRange;
		int num = 0;
		Rectangle rectangle = default(Rectangle);
		foreach (List<ContextSwitch> item4 in context_switches)
		{
			foreach (ContextSwitch item5 in item4)
			{
				bool flag = item5.m_ProcessId == m_Session.ProcessId || m_Session.IsSessionThread(item5.m_NewThreadId);
				ContextSwitchRenderData item = default(ContextSwitchRenderData);
				item.m_SessionContextSwitch = item5;
				item.m_Pen = (flag ? m_ContextSwitchPen : m_ContextSwitchOtherProcessPen);
				int num2 = TimeToX(item5.m_Timestamp, visibleTimeRange);
				Rectangle coreRect = GetCoreRect(item5.m_CPUId, rendererInput);
				int num3 = coreRect.Height / 4;
				item.m_Rect = new Rectangle(num2, coreRect.Y - num3, 1, coreRect.Height + 2 * num3);
				if (item.m_Rect != rectangle)
				{
					m_RendererRenderData.m_ContextSwitches.Add(item);
					rectangle = item.m_Rect;
				}
				while (item5.m_CPUId >= contexts.Count)
				{
					contexts.Add(new List<ContextRenderData>());
				}
				List<ContextRenderData> list = contexts[item5.m_CPUId];
				ContextRenderData contextRenderData;
				int num4;
				int num5;
				int num6;
				Color color;
				if (item5.m_OldThreadId != 0 && list.Count != 0)
				{
					contextRenderData = list[list.Count - 1];
					contextRenderData.m_EndTime = item5.m_Timestamp;
					contextRenderData.m_Rect = GetTimeSpanRect(contextRenderData.m_StartTime, contextRenderData.m_EndTime, 0, item5.m_CPUId, highlight: false, rendererInput);
					num4 = TimeToX_NoClamp(contextRenderData.m_StartTime, visibleTimeRange);
					num5 = contextRenderData.m_Rect.Y;
					if (contextRenderData.m_ProcessId != m_Session.ProcessId)
					{
						num6 = (m_Session.IsSessionThread(contextRenderData.m_ThreadId) ? 1 : 0);
						if (num6 == 0)
						{
							color = Colours.CoreOtherProcessThreadBack;
							goto IL_02f0;
						}
					}
					else
					{
						num6 = 1;
					}
					color = GetThreadRenderBrush(contextRenderData.m_ThreadId).m_Colour;
					goto IL_02f0;
				}
				goto IL_0321;
				IL_0321:
				int newThreadId = item5.m_NewThreadId;
				if (newThreadId != 0)
				{
					ContextRenderData contextRenderData2 = m_ContextRenderDataAllocator.Alloc();
					contextRenderData2.m_StartTime = item5.m_Timestamp;
					contextRenderData2.m_EndTime = 0L;
					contextRenderData2.m_ProcessId = item5.m_ProcessId;
					contextRenderData2.m_ThreadId = newThreadId;
					contextRenderData2.m_Rect = default(Rectangle);
					contextRenderData2.m_Brush = (flag ? GetThreadRenderBrush(newThreadId).m_TimeSpanBrush : Brushes.Pink);
					list.Add(contextRenderData2);
				}
				continue;
				IL_02f0:
				Color back_colour = color;
				Color fore_colour = ((num6 != 0) ? Colours.UntrackedThreadHatchForeColour : Colours.CoreOtherProcessThreadFore);
				contextRenderData.m_Brush = m_HatchBrushManager.GetBrush(back_colour, fore_colour, new Point(num4, num5));
				goto IL_0321;
			}
		}
		foreach (ThreadTimeSpans value3 in m_ThreadTimeSpanRenderData.Values)
		{
			foreach (TimeSpanRenderData timeSpan4 in value3.m_TimeSpans)
			{
				m_TimeSpanRenderDataAllocator.Free(timeSpan4);
			}
			value3.m_TimeSpans.Clear();
		}
		m_Threads.Clear();
		m_Session.GetThreads(m_Threads);
		Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
		foreach (int thread in m_Threads)
		{
			if (!m_ThreadTimeSpanRenderData.TryGetValue(thread, out var value))
			{
				value = new ThreadTimeSpans();
				m_ThreadTimeSpanRenderData[thread] = value;
			}
			ScopeRenderBrush threadRenderBrush = GetThreadRenderBrush(thread);
			TimeSpan timeSpan = m_Session.GetTimeSpan(thread, start_time);
			TimeSpan timeSpan2 = timeSpan;
			while (timeSpan2 != null && (timeSpan2.Parent.Parent != null || timeSpan2.StartTime <= end_time))
			{
				TimeSpan timeSpan3 = timeSpan2;
				TimeSpanRenderData timeSpanRenderData = null;
				if (!IsHiresTimeSpan(timeSpan3))
				{
					TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(timeSpan3.TimeSpanInfoId);
					SourceInfoStruct sourceInfo = m_Session.GetSourceInfo(timeSpanInfo.SourceInfo);
					timeSpanRenderData = m_TimeSpanRenderDataAllocator.Alloc();
					ScopeRenderBrush scopeRenderBrush = rendererInput.ColourMode switch
					{
						ScopeColourMode.Thread => threadRenderBrush, 
						ScopeColourMode.Scope => GetScopeRenderBrush(timeSpan3), 
						_ => threadRenderBrush, 
					};
					bool flag2 = timeSpanInfo.Name == m_SelectedTimeSpanName;
					bool flag3 = IsHighlighted(timeSpanInfo.Name);
					Brush brush;
					if (!flag3)
					{
						brush = (flag2 ? m_SelectedTimeSpanBrush : ((!sourceInfo.IsValid || sourceInfo.TimeSpanType != TimeSpanType.Idle) ? scopeRenderBrush.m_TimeSpanBrush : scopeRenderBrush.m_IdleTimeSpanBrush));
					}
					else
					{
						brush = m_HighlightBrush;
						num++;
					}
					timeSpanRenderData.m_StartTime = timeSpan3.StartTime;
					timeSpanRenderData.m_EndTime = timeSpan3.EndTime;
					timeSpanRenderData.m_TimeSpan = timeSpan3;
					timeSpanRenderData.m_Brush = brush;
					timeSpanRenderData.m_IsIdleTimeSpan = sourceInfo.IsValid && sourceInfo.TimeSpanType == TimeSpanType.Idle;
					timeSpanRenderData.m_Highlight = flag3;
					timeSpanRenderData.m_Selected = flag2;
					timeSpanRenderData.m_ThreadId = thread;
					if (GetTimeSpanRect(timeSpan3, timeSpanInfo.Core, highlight: false, rendererInput).IntersectsWith(rect))
					{
						value.m_TimeSpans.Add(timeSpanRenderData);
					}
				}
				if (timeSpan2.Children != null)
				{
					timeSpan2 = timeSpan2.Children;
					continue;
				}
				while (timeSpan2.Next == null)
				{
					timeSpan2 = timeSpan2.Parent;
					if (timeSpan2 == timeSpan.Parent)
					{
						timeSpan2 = null;
						break;
					}
				}
				if (timeSpan2 != null && timeSpan2.Next != null)
				{
					timeSpan2 = timeSpan2.Next;
				}
			}
		}
		m_RendererRenderData.m_HighlightedTimeSpansCount = num;
		int num7 = 0;
		foreach (List<ContextRenderData> item6 in contexts)
		{
			while (m_RendererRenderData.m_CoreRenderData.Count <= num7)
			{
				m_RendererRenderData.m_CoreRenderData.Add(new CoreRenderData());
			}
			CoreRenderData coreRenderData = m_RendererRenderData.m_CoreRenderData[num7];
			Rectangle rectangle2 = default(Rectangle);
			foreach (ContextRenderData item7 in item6)
			{
				long startTime = item7.m_StartTime;
				long endTime = item7.m_EndTime;
				if (!m_ThreadTimeSpanRenderData.TryGetValue(item7.m_ThreadId, out var value2))
				{
					continue;
				}
				int count = value2.m_TimeSpans.Count;
				int i;
				for (i = 0; i < count && value2.m_TimeSpans[i].m_EndTime <= startTime; i++)
				{
				}
				for (; i < count && value2.m_TimeSpans[i].m_StartTime < endTime; i++)
				{
					TimeSpanRenderData timeSpanRenderData2 = value2.m_TimeSpans[i];
					if (timeSpanRenderData2.m_StartTime < endTime && timeSpanRenderData2.m_EndTime > startTime)
					{
						TimeSpanRenderData timeSpanRenderData3 = m_TimeSpanRenderDataAllocator.Alloc();
						timeSpanRenderData3.m_StartTime = Math.Max(startTime, timeSpanRenderData2.m_StartTime);
						timeSpanRenderData3.m_EndTime = Math.Min(timeSpanRenderData2.m_EndTime, endTime);
						timeSpanRenderData3.m_TimeSpan = timeSpanRenderData2.m_TimeSpan;
						timeSpanRenderData3.m_Brush = timeSpanRenderData2.m_Brush;
						timeSpanRenderData3.m_ThreadId = timeSpanRenderData2.m_ThreadId;
						timeSpanRenderData3.m_Core = num7;
						timeSpanRenderData3.m_IsIdleTimeSpan = timeSpanRenderData2.m_IsIdleTimeSpan;
						timeSpanRenderData3.m_Highlight = timeSpanRenderData2.m_Highlight;
						timeSpanRenderData3.m_Selected = timeSpanRenderData2.m_Selected;
						int heirachyDepth = GetHeirachyDepth(timeSpanRenderData2.m_TimeSpan);
						timeSpanRenderData3.m_Rect = GetTimeSpanRect(timeSpanRenderData3.m_StartTime, timeSpanRenderData3.m_EndTime, heirachyDepth, timeSpanRenderData3.m_Core, timeSpanRenderData3.m_Highlight || timeSpanRenderData3.m_Selected, rendererInput);
						if (timeSpanRenderData3.m_Rect != rectangle2)
						{
							coreRenderData.m_TimeSpans.Add(timeSpanRenderData3);
							rectangle2 = timeSpanRenderData3.m_Rect;
							continue;
						}
						int index = coreRenderData.m_TimeSpans.Count - 1;
						TimeSpanRenderData item2 = coreRenderData.m_TimeSpans[index];
						m_TimeSpanRenderDataAllocator.Free(item2);
						coreRenderData.m_TimeSpans[index] = timeSpanRenderData3;
					}
				}
			}
			num7++;
		}
	}

	private bool IsHighlighted(long name)
	{
		return m_HighlightedTimeSpans.Contains(name);
	}

	private void FreeTimeSpanRenderDataList(List<TimeSpanRenderData> time_span_render_datas)
	{
		foreach (TimeSpanRenderData time_span_render_data in time_span_render_datas)
		{
			m_TimeSpanRenderDataAllocator.Free(time_span_render_data);
		}
		time_span_render_datas.Clear();
	}

	public void UpdateThreadColours()
	{
		lock (m_Lock)
		{
			m_UpdateColours = 2;
		}
	}

	private ScopeRenderBrush GetThreadRenderBrush(int thread_id)
	{
		if (!m_ThreadRenderBrushes.TryGetValue(thread_id, out var value))
		{
			value = new ScopeRenderBrush();
			string threadName = m_Session.GetThreadName(thread_id);
			Color color = (value.m_Colour = m_Session.GetThreadColour(threadName));
			value.m_TimeSpanBrush = new SolidBrush(color);
			value.m_IdleTimeSpanBrush = new SolidBrush(Utils.Lerp(color, BackgroundColour, Colours.CoreGraphIdleTransparency));
			m_ThreadRenderBrushes[thread_id] = value;
		}
		return value;
	}

	private bool TimeSpanIsLargePercentageOfParent(TimeSpan time_span)
	{
		if (time_span.Parent.Parent == null)
		{
			return true;
		}
		return time_span.Duration * 100 / time_span.Parent.Duration > 30;
	}

	private static bool TimeSpanHasBigChild(TimeSpan time_span, long min_duration)
	{
		if (time_span.Parent == null)
		{
			return true;
		}
		for (TimeSpan timeSpan = time_span.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			if (timeSpan.Duration > min_duration)
			{
				return true;
			}
		}
		return false;
	}

	private ScopeRenderBrush GetScopeRenderBrush(TimeSpan time_span)
	{
		long name = m_Session.GetTimeSpanInfo(time_span.TimeSpanInfoId).Name;
		if (!m_ScopeRenderBrushes.TryGetValue(name, out var value))
		{
			value = new ScopeRenderBrush();
			Color color = (value.m_Colour = m_Session.GetScopeColour(name));
			value.m_TimeSpanBrush = new SolidBrush(color);
			value.m_IdleTimeSpanBrush = new SolidBrush(Utils.Lerp(color, BackgroundColour, Colours.CoreGraphIdleTransparency));
			m_ScopeRenderBrushes[name] = value;
		}
		return value;
	}

	private bool IsHiresTimeSpan(TimeSpan time_span)
	{
		if (time_span.IsTimeSpanEx)
		{
			return ((TimeSpanEx)time_span).HiResTimers != null;
		}
		return false;
	}

	private void CalculateCoreTimeSpanRenderData(long start_time, long end_time)
	{
		m_AllTimeSpanRenderData.Clear();
		foreach (CoreRenderData coreRenderDatum in m_RendererRenderData.m_CoreRenderData)
		{
			FreeTimeSpanRenderDataList(coreRenderDatum.m_TimeSpans);
		}
		RendererInput rendererInput = m_RendererRenderData.m_RendererInput;
		TimeRange visibleTimeRange = rendererInput.VisibleTimeRange;
		if (m_Session == null || visibleTimeRange.m_TicksPerPixel == 0L)
		{
			return;
		}
		m_Threads.Clear();
		m_Session.GetThreads(m_Threads);
		foreach (int thread in m_Threads)
		{
			ScopeRenderBrush threadRenderBrush = GetThreadRenderBrush(thread);
			TimeSpan timeSpan = m_Session.GetTimeSpan(thread, start_time);
			TimeSpan timeSpan2 = timeSpan;
			m_RenderDataStack.Clear();
			while (timeSpan2 != null && (timeSpan2.Parent.Parent != null || timeSpan2.StartTime <= end_time))
			{
				TimeSpan timeSpan3 = timeSpan2;
				TimeSpanRenderData timeSpanRenderData = null;
				if (!IsHiresTimeSpan(timeSpan3))
				{
					TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(timeSpan3.TimeSpanInfoId);
					SourceInfoStruct sourceInfo = m_Session.GetSourceInfo(timeSpanInfo.SourceInfo);
					timeSpanRenderData = m_TimeSpanRenderDataAllocator.Alloc();
					ScopeRenderBrush scopeRenderBrush = rendererInput.ColourMode switch
					{
						ScopeColourMode.Thread => threadRenderBrush, 
						ScopeColourMode.Scope => GetScopeRenderBrush(timeSpan3), 
						_ => threadRenderBrush, 
					};
					bool flag = timeSpanInfo.Name == m_SelectedTimeSpanName;
					bool flag2 = IsHighlighted(timeSpanInfo.Name);
					Brush brush = (flag2 ? m_HighlightBrush : (flag ? m_SelectedTimeSpanBrush : ((!sourceInfo.IsValid || sourceInfo.TimeSpanType != TimeSpanType.Idle) ? scopeRenderBrush.m_TimeSpanBrush : scopeRenderBrush.m_IdleTimeSpanBrush)));
					timeSpanRenderData.m_StartTime = timeSpan3.StartTime;
					timeSpanRenderData.m_EndTime = timeSpan3.EndTime;
					timeSpanRenderData.m_TimeSpan = timeSpan3;
					timeSpanRenderData.m_Brush = brush;
					timeSpanRenderData.m_Rect = GetTimeSpanRect(timeSpan3, timeSpanInfo.Core, flag2, rendererInput);
					timeSpanRenderData.m_IsIdleTimeSpan = sourceInfo.IsValid && sourceInfo.TimeSpanType == TimeSpanType.Idle;
					timeSpanRenderData.m_Highlight = flag2;
					timeSpanRenderData.m_ThreadId = thread;
					timeSpanRenderData.m_Core = timeSpanInfo.Core;
					if (flag)
					{
						Rectangle rect = timeSpanRenderData.m_Rect;
						timeSpanRenderData.m_Rect = new Rectangle(rect.X, rect.Y - 16, rect.Width, rect.Height + 16);
					}
					m_AllTimeSpanRenderData.Add(timeSpanRenderData);
					if (!m_ShowHeirachy && m_RenderDataStack.Count != 0)
					{
						TimeSpanRenderData timeSpanRenderData2 = SplitTimeSpanRenderData(m_RenderDataStack[m_RenderDataStack.Count - 1], timeSpanRenderData);
						m_AllTimeSpanRenderData.Add(timeSpanRenderData2);
						m_RenderDataStack[m_RenderDataStack.Count - 1] = timeSpanRenderData2;
					}
				}
				if (timeSpan2.Children != null)
				{
					if (!IsHiresTimeSpan(timeSpan3))
					{
						m_RenderDataStack.Add(timeSpanRenderData);
					}
					timeSpan2 = timeSpan2.Children;
					continue;
				}
				while (timeSpan2.Next == null)
				{
					timeSpan2 = timeSpan2.Parent;
					if (timeSpan2 == timeSpan.Parent || m_RenderDataStack.Count == 0)
					{
						timeSpan2 = null;
						break;
					}
					if (!IsHiresTimeSpan(timeSpan2))
					{
						m_RenderDataStack.RemoveAt(m_RenderDataStack.Count - 1);
					}
				}
				if (timeSpan2 != null && timeSpan2.Next != null)
				{
					timeSpan2 = timeSpan2.Next;
				}
			}
		}
		foreach (TimeSpanRenderData allTimeSpanRenderDatum in m_AllTimeSpanRenderData)
		{
			int core = allTimeSpanRenderDatum.m_Core;
			while (m_RendererRenderData.m_CoreRenderData.Count <= core)
			{
				m_RendererRenderData.m_CoreRenderData.Add(new CoreRenderData());
			}
			CoreRenderData coreRenderData = m_RendererRenderData.m_CoreRenderData[core];
			allTimeSpanRenderDatum.m_StableSortIndex = coreRenderData.m_TimeSpans.Count;
			coreRenderData.m_TimeSpans.Add(allTimeSpanRenderDatum);
		}
		foreach (CoreRenderData coreRenderDatum2 in m_RendererRenderData.m_CoreRenderData)
		{
			coreRenderDatum2.m_TimeSpans.Sort(TimeSpanRenderDataComparer);
		}
	}

	private int GetHeirachyDepth(TimeSpan time_span)
	{
		int num = 0;
		if (m_ShowHeirachy)
		{
			TimeSpan timeSpan = time_span.Parent;
			while (timeSpan != null && timeSpan.Parent != null)
			{
				if (!IsHiresTimeSpan(timeSpan))
				{
					num++;
				}
				timeSpan = timeSpan.Parent;
			}
			if (m_Session.RecordingContextSwitches)
			{
				num++;
			}
		}
		return num;
	}

	private TimeSpanRenderData SplitTimeSpanRenderData(TimeSpanRenderData parent, TimeSpanRenderData child)
	{
		RendererInput rendererInput = m_RendererRenderData.m_RendererInput;
		TimeSpanRenderData timeSpanRenderData = m_TimeSpanRenderDataAllocator.Alloc();
		timeSpanRenderData.m_StartTime = child.m_TimeSpan.EndTime;
		timeSpanRenderData.m_EndTime = parent.m_TimeSpan.EndTime;
		timeSpanRenderData.m_TimeSpan = parent.m_TimeSpan;
		timeSpanRenderData.m_Brush = parent.m_Brush;
		timeSpanRenderData.m_StableSortIndex = parent.m_StableSortIndex;
		timeSpanRenderData.m_ThreadId = parent.m_ThreadId;
		timeSpanRenderData.m_Core = parent.m_Core;
		timeSpanRenderData.m_IsIdleTimeSpan = parent.m_IsIdleTimeSpan;
		timeSpanRenderData.m_Highlight = parent.m_Highlight;
		int heirachyDepth = GetHeirachyDepth(parent.m_TimeSpan);
		timeSpanRenderData.m_Rect = GetTimeSpanRect(timeSpanRenderData.m_StartTime, timeSpanRenderData.m_EndTime, heirachyDepth, timeSpanRenderData.m_Core, parent.m_Highlight, rendererInput);
		parent.m_EndTime = child.m_TimeSpan.StartTime;
		parent.m_Core = child.m_Core;
		int heirachyDepth2 = GetHeirachyDepth(child.m_TimeSpan);
		parent.m_Rect = GetTimeSpanRect(parent.m_StartTime, parent.m_EndTime, heirachyDepth2, parent.m_Core, parent.m_Highlight, rendererInput);
		return timeSpanRenderData;
	}

	private int TimeSpanRenderDataComparer(TimeSpanRenderData time_span_1, TimeSpanRenderData time_span_2)
	{
		bool isIdleTimeSpan = time_span_1.m_IsIdleTimeSpan;
		bool isIdleTimeSpan2 = time_span_2.m_IsIdleTimeSpan;
		if (isIdleTimeSpan && !isIdleTimeSpan2)
		{
			return -1;
		}
		if (isIdleTimeSpan2 && !isIdleTimeSpan)
		{
			return 1;
		}
		bool highlight = time_span_1.m_Highlight;
		bool highlight2 = time_span_2.m_Highlight;
		if (highlight && !highlight2)
		{
			return 1;
		}
		if (highlight2 && !highlight)
		{
			return -1;
		}
		long startTime = time_span_1.m_StartTime;
		long startTime2 = time_span_2.m_StartTime;
		int num = startTime.CompareTo(startTime2);
		if (num == 0)
		{
			num = time_span_1.m_StableSortIndex.CompareTo(time_span_2.m_StableSortIndex);
		}
		return num;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private int GetFirstCoreYOffset(bool show_heirachy)
	{
		int value = ((!show_heirachy) ? 20 : 0);
		return ScaleDPI(value);
	}

	private int GetCoreY(int index, RendererInput renderer_input)
	{
		int num = GetFirstCoreYOffset(renderer_input.ShowHeirachy);
		for (int i = 0; i < index; i++)
		{
			num += GetCoreHeight(i, renderer_input);
		}
		return num;
	}

	public int GetCoreHeight(int index)
	{
		CheckIsMainThread();
		UpdateRendererInputs();
		return GetCoreHeight(index, m_RendererInput);
	}

	private static int GetCoreHeight(int index, RendererInput renderer_input)
	{
		return (int)((float)((index < renderer_input.CoreGraphHeights.Count) ? renderer_input.CoreGraphHeights[index] : 140) * renderer_input.DPIScale);
	}

	private int GetCoreYFromSettings(int index)
	{
		CheckIsMainThread();
		int num = 0;
		for (int i = 0; i < index; i++)
		{
			num += GetCoreHeightFromSettings(i);
		}
		return num;
	}

	private int GetCoreHeightFromSettings(int index)
	{
		CheckIsMainThread();
		if (index >= m_Settings.CoreGraphCoreHeights.Count)
		{
			return ScaleDPI(140);
		}
		return m_Settings.CoreGraphCoreHeights[index];
	}

	private void SetCoreHeightInSettings(int index, int height)
	{
		ScaleDPI(140);
		while (m_Settings.CoreGraphCoreHeights.Count <= index)
		{
			m_Settings.CoreGraphCoreHeights.Add(height);
		}
		m_Settings.CoreGraphCoreHeights[index] = height;
		RecalculateView();
		OnCoreHeightsChanged();
	}

	public Rectangle GetCoreRect(int index)
	{
		CheckIsMainThread();
		UpdateRendererInputs();
		return GetCoreRect(index, m_RendererInput);
	}

	private Rectangle GetCoreRect(int index, RendererInput renderer_input)
	{
		int num = GetCoreY(index, renderer_input) - renderer_input.VisibleTimeRange.m_ScrollY;
		int num2 = ScaleDPI(m_CoreBarHeight);
		return new Rectangle(0, num, base.ClientSize.Width, num2);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		try
		{
			e.Graphics.Clear(BackgroundColour);
			e.Graphics.DrawLine(Pens.Black, 0, 0, base.ClientSize.Width, 0);
			if (m_Session == null || !m_Session.IsReady)
			{
				return;
			}
			m_HighlightedTimeSpansCount = m_RenderData.m_HighlightedTimeSpansCount;
			RendererInput rendererInput = m_RenderData.m_RendererInput;
			int coreCount = m_Session.CoreCount;
			if (m_ShowHeirachy)
			{
				for (int i = 0; i < coreCount; i++)
				{
					Rectangle coreRect = GetCoreRect(i, rendererInput);
					coreRect = new Rectangle(coreRect.X, coreRect.Y, coreRect.Width, GetCoreHeight(i, rendererInput));
					Brush brush = ((((uint)i & (true ? 1u : 0u)) != 0) ? m_CoreColourABrush : m_CoreColourBBrush);
					e.Graphics.FillRectangle(brush, coreRect);
				}
			}
			for (int j = 0; j < coreCount; j++)
			{
				Rectangle coreRect2 = GetCoreRect(j, rendererInput);
				e.Graphics.FillRectangle(m_CoreBrush, coreRect2);
			}
			int num = 0;
			Rectangle rectangle = default(Rectangle);
			foreach (List<ContextRenderData> context in m_RenderData.m_Contexts)
			{
				foreach (ContextRenderData item in context)
				{
					if (item.m_Rect != rectangle)
					{
						e.Graphics.FillRectangle(item.m_Brush, item.m_Rect);
						rectangle = item.m_Rect;
					}
				}
				num++;
			}
			int num2 = 0;
			foreach (CoreRenderData coreRenderDatum in m_RenderData.m_CoreRenderData)
			{
				if (m_ShowHeirachy)
				{
					Rectangle coreRect3 = GetCoreRect(num2, rendererInput);
					Rectangle clip = new Rectangle(coreRect3.X, coreRect3.Y, coreRect3.Width, GetCoreHeight(num2, rendererInput));
					e.Graphics.SetClip(clip);
				}
				foreach (TimeSpanRenderData timeSpan in coreRenderDatum.m_TimeSpans)
				{
					e.Graphics.FillRectangle(timeSpan.m_Brush, timeSpan.m_Rect);
					if (timeSpan.m_Highlight ? (timeSpan.m_Rect.Width > ScaleDPI(2)) : (timeSpan.m_Rect.Width > 2))
					{
						e.Graphics.DrawLine(Pens.Black, timeSpan.m_Rect.X, timeSpan.m_Rect.Y, timeSpan.m_Rect.X, timeSpan.m_Rect.Bottom - 1);
						e.Graphics.DrawLine(Pens.Black, timeSpan.m_Rect.Right, timeSpan.m_Rect.Y, timeSpan.m_Rect.Right, timeSpan.m_Rect.Bottom - 1);
					}
				}
				num2++;
			}
			e.Graphics.ResetClip();
			if (m_ContextSwitchesVisible)
			{
				foreach (ContextSwitchRenderData contextSwitch in m_RenderData.m_ContextSwitches)
				{
					Graphics graphics = e.Graphics;
					Pen pen = contextSwitch.m_Pen;
					Rectangle rect = contextSwitch.m_Rect;
					int x = rect.X;
					rect = contextSwitch.m_Rect;
					int y = rect.Y;
					rect = contextSwitch.m_Rect;
					int x2 = rect.X;
					rect = contextSwitch.m_Rect;
					graphics.DrawLine(pen, x, y, x2, rect.Bottom);
				}
			}
			if (m_ShowWaitEvents)
			{
				DrawWaitEvents(e.Graphics);
			}
			int num3 = -2;
			int y2 = (m_ShowHeirachy ? (GetCoreRect(coreCount - 1).Y + GetCoreHeight(coreCount - 1, rendererInput)) : GetCoreRect(coreCount - 1).Bottom);
			foreach (FrameRenderData frameRenderDatum in m_RenderData.m_FrameRenderData)
			{
				int num4 = frameRenderDatum.m_X;
				if (num4 > num3 + ScaleDPI(Timeline.MinFrameLineWidth))
				{
					e.Graphics.DrawLine(m_FramePen, num4, 0, num4, y2);
				}
				num3 = num4;
			}
			m_MeasureLine.Draw(e.Graphics, base.Height, m_MeasureLinePen, m_MeasureLineFillBrush);
		}
		catch (Exception)
		{
		}
	}

	private void DrawWaitEvents(Graphics graphics)
	{
		foreach (WaitEventRenderData waitEvent in m_RenderData.m_WaitEvents)
		{
			DrawWaitEvent(waitEvent, graphics);
		}
		if (m_SelectedWaitEvent == null || m_SelectedStartWaitEventIndex.Count == 0 || m_SelectedTriggerWaitEventIndex == -1 || m_SelectedStopWaitEventIndex.Count == 0)
		{
			return;
		}
		WaitEventRenderData waitEventRenderData = default(WaitEventRenderData);
		foreach (int value in m_SelectedStartWaitEventIndex.Values)
		{
			WaitEventRenderData waitEventRenderData2 = m_RenderData.m_WaitEvents[value];
			if (waitEventRenderData.m_WaitEvent == null || waitEventRenderData2.m_WaitEvent.Time < waitEventRenderData.m_WaitEvent.Time)
			{
				waitEventRenderData = waitEventRenderData2;
			}
		}
		WaitEventRenderData waitEventRenderData3 = default(WaitEventRenderData);
		foreach (int value2 in m_SelectedStopWaitEventIndex.Values)
		{
			WaitEventRenderData waitEventRenderData4 = m_RenderData.m_WaitEvents[value2];
			if (waitEventRenderData3.m_WaitEvent == null || waitEventRenderData4.m_WaitEvent.Time > waitEventRenderData3.m_WaitEvent.Time)
			{
				waitEventRenderData3 = waitEventRenderData4;
			}
		}
		WaitEventRenderData waitEventRenderData5 = m_RenderData.m_WaitEvents[m_SelectedTriggerWaitEventIndex];
		graphics.FillRectangle(m_SelectedWaitEventFadeBrush, waitEventRenderData.m_Point.X, 0, waitEventRenderData3.m_Point.X - waitEventRenderData.m_Point.X, base.Height);
		foreach (int key in m_SelectedStartWaitEventIndex.Keys)
		{
			WaitEventRenderData start_event = m_RenderData.m_WaitEvents[m_SelectedStartWaitEventIndex[key]];
			if (m_SelectedStopWaitEventIndex.ContainsKey(key))
			{
				WaitEventRenderData stop_event = m_RenderData.m_WaitEvents[m_SelectedStopWaitEventIndex[key]];
				DrawWaitEventConnector(start_event, stop_event, graphics, m_WaitEventConnectorPen);
				DrawWaitEventConnector(start_event, waitEventRenderData5, graphics, m_WaitEventPreTriggerConnectorPen);
				DrawWaitEventConnector(waitEventRenderData5, stop_event, graphics, m_WaitEventPostTriggerConnectorPen);
			}
		}
		foreach (int value3 in m_SelectedStartWaitEventIndex.Values)
		{
			DrawSelectedWaitEvent(m_RenderData.m_WaitEvents[value3], graphics);
		}
		DrawSelectedWaitEvent(waitEventRenderData5, graphics);
		foreach (int value4 in m_SelectedStopWaitEventIndex.Values)
		{
			DrawSelectedWaitEvent(m_RenderData.m_WaitEvents[value4], graphics);
		}
	}

	private void DrawWaitEvent(WaitEventRenderData wait_event, Graphics graphics)
	{
		Rectangle waitEventRect = GetWaitEventRect(wait_event.m_Point);
		if (waitEventRect.IntersectsWith(base.ClientRectangle))
		{
			graphics.FillRectangle(m_WaitEventBrush, waitEventRect);
			graphics.DrawRectangle(m_WaitEventBorderPen, waitEventRect);
		}
	}

	private void DrawSelectedWaitEvent(WaitEventRenderData wait_event, Graphics graphics)
	{
		Rectangle waitEventRect = GetWaitEventRect(wait_event.m_Point);
		graphics.FillRectangle(m_SelectedWaitEventBrush, waitEventRect);
		graphics.DrawRectangle(m_SelectedWaitEventBorderPen, waitEventRect);
		graphics.DrawEllipse(Pens.Red, Rectangle.Inflate(waitEventRect, 5, 5));
	}

	private void DrawWaitEventConnector(WaitEventRenderData start_event, WaitEventRenderData stop_event, Graphics graphics, Pen pen)
	{
		Point point = start_event.m_Point;
		Point point2 = stop_event.m_Point;
		Point pt = new Point(point.X + (point2.X - point.X) / 2, point.Y);
		Point pt2 = new Point(point.X + (point2.X - point.X) / 2, point2.Y);
		SmoothingMode smoothingMode = graphics.SmoothingMode;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.DrawBezier(pen, point, pt, pt2, point2);
		graphics.SmoothingMode = smoothingMode;
	}

	private Rectangle GetWaitEventRect(Point p)
	{
		return new Rectangle(p.X - 2, p.Y - 2, 4, 4);
	}

	private Rectangle GetTimeSpanRect(TimeSpan time_span, int core, bool highlight, RendererInput renderer_input)
	{
		int heirachyDepth = GetHeirachyDepth(time_span);
		return GetTimeSpanRect(time_span.StartTime, time_span.EndTime, heirachyDepth, core, highlight, renderer_input);
	}

	private bool IsOnRecalculateThread()
	{
		return Thread.CurrentThread.ManagedThreadId == m_CalculateViewThread.ManagedThreadId;
	}

	private Rectangle GetTimeSpanRect(long start_time, long end_time, int heirachy_depth, int core, bool highlight, RendererInput renderer_input)
	{
		int num = TimeToX(start_time, renderer_input.VisibleTimeRange);
		int num2 = TimeToX(end_time, renderer_input.VisibleTimeRange);
		int num3 = Math.Max(1, num2 - num);
		Rectangle coreRect = GetCoreRect(core, renderer_input);
		int num4 = heirachy_depth * (coreRect.Height + 1);
		Rectangle result = new Rectangle(num, coreRect.Y + num4, num3, coreRect.Height);
		if (highlight)
		{
			int num5 = ScaleDPI(2);
			result = new Rectangle(result.X, result.Y - num5, result.Width, result.Height + 2 * num5);
			if (result.Width < num5)
			{
				result = new Rectangle(result.X - num5 / 2, result.Y, num5, result.Height);
			}
		}
		return result;
	}

	private long XToTime(int x, TimeRange visible_time_range)
	{
		return x * visible_time_range.m_TicksPerPixel + visible_time_range.m_StartTime;
	}

	private int TimeToX(long time, TimeRange visible_time_range)
	{
		return (int)Misc.Clamp((time - visible_time_range.m_StartTime) / visible_time_range.m_TicksPerPixel, -1L, base.ClientSize.Width + 2);
	}

	private int TimeToX_NoClamp(long time, TimeRange visible_time_range)
	{
		return (int)Misc.Clamp((time - visible_time_range.m_StartTime) / visible_time_range.m_TicksPerPixel, -2147483648L, 2147483647L);
	}

	protected override void OnResize(EventArgs e)
	{
		if (MainForm.Inst != null && MainForm.Inst.WindowState != FormWindowState.Minimized && m_Session != null)
		{
			UpdateVScrollBar();
		}
		base.OnResize(e);
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(e.Location);
			if (timeSpanRenderData != null)
			{
				ShowInThreadView(timeSpanRenderData.m_ThreadId);
			}
		}
		base.OnMouseDoubleClick(e);
	}

	private TimeSpan GetTimeSpan(Point location)
	{
		return GetTimeSpanRenderData(location)?.m_TimeSpan;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			m_MouseClicked = true;
			m_LastMovePoint = e.Location;
			m_MouseClickPoint = e.Location;
			WaitEvent waitEvent = GetWaitEvent(e.Location);
			if (waitEvent != null)
			{
				SelectWaitEvent(waitEvent);
				m_WaitEventSelected = true;
				Refresh();
			}
			else if (m_CoreVResizeHover != -1)
			{
				m_StartedCoreVResize = true;
				m_CoreVResizeLastY = e.Y;
				base.Capture = true;
			}
			else if (Utils.IsShiftHeld)
			{
				m_MeasureLine.Dragging = true;
				base.Capture = true;
			}
			else
			{
				m_StartedDragMove = true;
				base.Capture = true;
				m_DragMovePos = e.Location;
				m_MaxDragOffset = 0;
				m_StartDragTime = m_VisibleTimeRange.m_StartTime;
				m_StartDragScrolllY = m_VScrollBar.Value;
				m_MouseDragMode = MouseDragMode.Horizontal;
			}
		}
		else if (e.Button == MouseButtons.Right)
		{
			m_ContextMenuTimeSpanRenderData = GetTimeSpanRenderData(e.Location);
			m_ContextMenuStrip.Show(PointToScreen(e.Location));
			m_ContextMenuStrip.Enabled = m_ContextMenuTimeSpanRenderData != null;
		}
		if (this.StopTrackingEnd != null)
		{
			this.StopTrackingEnd();
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (e.Location != m_LastMovePoint)
		{
			m_MouseClicked = false;
		}
		m_LastMovePoint = e.Location;
		if (m_MouseHoverEnabled && !m_StartedDragMove)
		{
			UpdateFrameInfoBox(e.Location);
			m_MeasureLine.HandleMouseMove(e.X);
		}
		if (m_StartedDragMove)
		{
			MainForm.Inst.HoverBox.Visible = false;
			int value = e.X - m_DragMovePos.X;
			int num = e.Y - m_DragMovePos.Y;
			if (m_MouseDragMode == MouseDragMode.Horizontal && m_MaxDragOffset < 20 && Math.Abs(num) > 20)
			{
				m_MouseDragMode = MouseDragMode.Vertical;
			}
			switch (m_MouseDragMode)
			{
			case MouseDragMode.Horizontal:
			{
				m_MaxDragOffset = Math.Max(m_MaxDragOffset, Math.Abs(value));
				long num2 = XToTime(value, m_VisibleTimeRange) - m_VisibleTimeRange.m_StartTime;
				long startTime = Math.Max(m_Session.FirstFrameTime, m_StartDragTime - num2);
				lock (m_VisibleTimeRange)
				{
					m_VisibleTimeRange.m_StartTime = startTime;
				}
				OnTimeRangeChanged();
				RecalculateView();
				break;
			}
			case MouseDragMode.Vertical:
			{
				int scrollY = Misc.Clamp(m_StartDragScrolllY - num, 0, m_VScrollBar.Maximum);
				SetScrollY(scrollY);
				break;
			}
			}
		}
		else if (m_StartedCoreVResize)
		{
			int num3 = e.Y - m_CoreVResizeLastY;
			m_CoreVResizeLastY = e.Y;
			int coreHeightFromSettings = GetCoreHeightFromSettings(m_CoreVResizeHover);
			int num4 = Math.Max(10, coreHeightFromSettings + num3);
			SetCoreHeightInSettings(m_CoreVResizeHover, num4);
		}
		else if (m_ShowHeirachy)
		{
			int dist;
			int closestCoreLine = GetClosestCoreLine(e.Y, out dist);
			if (dist < 2)
			{
				m_CoreVResizeHover = closestCoreLine;
				Cursor = Cursors.SizeNS;
			}
			else
			{
				m_CoreVResizeHover = -1;
				Cursor = Cursors.Default;
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_MouseClicked && e.Location == m_MouseClickPoint)
			{
				m_MouseClicked = false;
				m_LastMovePoint = e.Location;
				if (GetWaitEvent(e.Location) == null && m_SelectedWaitEvent != null)
				{
					ClearSelectedWaitEvent();
					Refresh();
				}
				else
				{
					TimeSpan timeSpan = GetTimeSpan(e.Location);
					long time_span_name = ((timeSpan != null) ? m_Session.GetTimeSpanInfo(timeSpan.TimeSpanInfoId).Name : (-1));
					SelectTimeSpanInternal(time_span_name);
				}
			}
			if (m_MeasureLine.Dragging)
			{
				m_MeasureLine.Dragging = false;
				base.Capture = false;
			}
			else if (m_StartedDragMove)
			{
				base.Capture = false;
				m_StartedDragMove = false;
			}
			else if (m_StartedCoreVResize)
			{
				base.Capture = false;
				m_StartedCoreVResize = false;
				m_Settings.Write();
			}
		}
		base.OnMouseUp(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (MainForm.Inst != null)
		{
			MainForm.Inst.HoverBox.Visible = false;
		}
		base.OnMouseLeave(e);
	}

	private int GetClosestCoreLine(int y, out int dist)
	{
		CheckIsMainThread();
		dist = int.MaxValue;
		if (m_Session == null)
		{
			return -1;
		}
		int coreCount = m_Session.CoreCount;
		int result = -1;
		for (int i = 0; i < coreCount; i++)
		{
			int num = Math.Abs(GetCoreYFromSettings(i + 1) - m_VisibleTimeRange.m_ScrollY - y);
			if (num < dist)
			{
				result = i;
				dist = num;
			}
		}
		return result;
	}

	private bool FindContextSwitch(Point location, out ContextSwitch out_context_switch)
	{
		CheckIsMainThread();
		foreach (ContextSwitchRenderData contextSwitch in m_RenderData.m_ContextSwitches)
		{
			Rectangle rect = contextSwitch.m_Rect;
			rect.Inflate(4, 4);
			if (rect.Contains(location))
			{
				out_context_switch = contextSwitch.m_SessionContextSwitch;
				return true;
			}
		}
		out_context_switch = default(ContextSwitch);
		return false;
	}

	private string GetProcessName(ContextSwitch context_switch)
	{
		return GetProcessName(context_switch.m_ProcessId, context_switch.m_NewThreadId);
	}

	private string GetProcessName(int process_id, int thread_id)
	{
		if (process_id == -1 && m_Session.IsSessionThread(thread_id))
		{
			return m_Session.SessionDetails.m_Name;
		}
		return m_Session.GetProcessName(process_id);
	}

	private static string GetContextSwitchIdentifier(ContextSwitch context_switch)
	{
		return context_switch.m_Timestamp.ToString() + context_switch.m_ProcessId + context_switch.m_CPUId + context_switch.m_NewThreadId;
	}

	private WaitEvent GetWaitEvent(Point p)
	{
		CheckIsMainThread();
		long num = long.MaxValue;
		long num2 = XToTime(p.X, m_VisibleTimeRange);
		WaitEvent result = null;
		foreach (WaitEventRenderData waitEvent in m_RenderData.m_WaitEvents)
		{
			Rectangle waitEventRect = GetWaitEventRect(waitEvent.m_Point);
			waitEventRect.Inflate(2, 2);
			if (waitEventRect.Contains(p))
			{
				long num3 = Math.Abs(num2 - waitEvent.m_WaitEvent.Time);
				if (num3 < num)
				{
					result = waitEvent.m_WaitEvent;
					num = num3;
				}
			}
		}
		return result;
	}

	private int GetWaitEventIndex(WaitEvent wait_event)
	{
		CheckIsMainThread();
		int num = 0;
		foreach (WaitEventRenderData waitEvent in m_RenderData.m_WaitEvents)
		{
			if (waitEvent.m_WaitEvent == wait_event)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	private void UpdateHighlightedWaitEventIndices()
	{
		CheckIsMainThread();
		m_SelectedStartWaitEventIndex.Clear();
		m_SelectedTriggerWaitEventIndex = -1;
		m_SelectedStopWaitEventIndex.Clear();
		if (m_SelectedWaitEvent != null)
		{
			GetWaitEventStartTriggerAndStop(m_SelectedWaitEvent, m_SelectedStartWaitEventIndex, ref m_SelectedTriggerWaitEventIndex, m_SelectedStopWaitEventIndex);
		}
	}

	private void FindPrevWaitEvents(WaitEvent wait_event, WaitEvent.WaitEventMode mode, Dictionary<int, int> wait_events)
	{
		for (int num = GetWaitEventIndex(wait_event) - 1; num >= 0; num--)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[num].m_WaitEvent;
			if (waitEvent.EventId == wait_event.EventId)
			{
				if (waitEvent.Mode == mode)
				{
					wait_events[waitEvent.ThreadId] = num;
				}
				else if (num == 0 || m_RenderData.m_WaitEvents[num - 1].m_WaitEvent.Time != waitEvent.Time)
				{
					break;
				}
			}
		}
	}

	private int FindPrevWaitEventIndex(WaitEvent wait_event, WaitEvent.WaitEventMode mode, WaitEvent.WaitEventMode terminate_mode, int thread_id)
	{
		for (int num = GetWaitEventIndex(wait_event) - 1; num >= 0; num--)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[num].m_WaitEvent;
			if (waitEvent.EventId == wait_event.EventId && (thread_id == -1 || waitEvent.ThreadId == thread_id))
			{
				if (waitEvent.Mode == mode)
				{
					return num;
				}
				if (waitEvent.Mode == terminate_mode)
				{
					return -1;
				}
			}
		}
		return -1;
	}

	private void FindNextWaitEvents(WaitEvent wait_event, WaitEvent.WaitEventMode mode, Dictionary<int, int> wait_events)
	{
		for (int i = GetWaitEventIndex(wait_event) + 1; i < m_RenderData.m_WaitEvents.Count; i++)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[i].m_WaitEvent;
			if (waitEvent.EventId == wait_event.EventId)
			{
				if (waitEvent.Mode != mode)
				{
					break;
				}
				wait_events[waitEvent.ThreadId] = i;
			}
		}
	}

	private int FindNextWaitEventIndex(WaitEvent wait_event, WaitEvent.WaitEventMode mode, WaitEvent.WaitEventMode terminate_mode, int thread_id)
	{
		for (int i = GetWaitEventIndex(wait_event) + 1; i < m_RenderData.m_WaitEvents.Count; i++)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[i].m_WaitEvent;
			if (waitEvent.EventId == wait_event.EventId && (thread_id == -1 || waitEvent.ThreadId == thread_id))
			{
				if (waitEvent.Mode == mode)
				{
					return i;
				}
				if (waitEvent.Mode == terminate_mode)
				{
					return -1;
				}
			}
		}
		return -1;
	}

	private void GetWaitEventStartTriggerAndStop(WaitEvent wait_event, Dictionary<int, int> start_events, ref int trigger_event, Dictionary<int, int> stop_events)
	{
		start_events.Clear();
		trigger_event = -1;
		stop_events.Clear();
		switch (wait_event.Mode)
		{
		case WaitEvent.WaitEventMode.Start:
		{
			int waitEventIndex2 = GetWaitEventIndex(wait_event);
			if (waitEventIndex2 != -1)
			{
				start_events[wait_event.ThreadId] = waitEventIndex2;
			}
			trigger_event = FindNextWaitEventIndex(wait_event, WaitEvent.WaitEventMode.Trigger, WaitEvent.WaitEventMode.Stop, -1);
			if (trigger_event != -1)
			{
				int num2 = FindNextWaitEventIndex(m_RenderData.m_WaitEvents[trigger_event].m_WaitEvent, WaitEvent.WaitEventMode.Stop, WaitEvent.WaitEventMode.Start, wait_event.ThreadId);
				if (num2 != -1)
				{
					stop_events[wait_event.ThreadId] = num2;
				}
			}
			break;
		}
		case WaitEvent.WaitEventMode.Trigger:
			trigger_event = GetWaitEventIndex(wait_event);
			FindPrevWaitEvents(wait_event, WaitEvent.WaitEventMode.Start, start_events);
			FindNextWaitEvents(wait_event, WaitEvent.WaitEventMode.Stop, stop_events);
			break;
		case WaitEvent.WaitEventMode.Stop:
		{
			int waitEventIndex = GetWaitEventIndex(wait_event);
			if (waitEventIndex != -1)
			{
				stop_events[wait_event.ThreadId] = waitEventIndex;
			}
			trigger_event = FindPrevWaitEventIndex(wait_event, WaitEvent.WaitEventMode.Trigger, WaitEvent.WaitEventMode.Start, -1);
			if (trigger_event != -1)
			{
				int num = FindPrevWaitEventIndex(m_RenderData.m_WaitEvents[trigger_event].m_WaitEvent, WaitEvent.WaitEventMode.Start, WaitEvent.WaitEventMode.Stop, wait_event.ThreadId);
				if (num != -1)
				{
					start_events[wait_event.ThreadId] = num;
				}
			}
			break;
		}
		}
	}

	private void SelectWaitEvent(WaitEvent wait_event)
	{
		CheckIsMainThread();
		m_SelectedWaitEvent = wait_event;
		UpdateHighlightedWaitEventIndices();
	}

	private void ClearSelectedWaitEvent()
	{
		CheckIsMainThread();
		m_SelectedWaitEvent = null;
		m_SelectedStartWaitEventIndex.Clear();
		m_SelectedTriggerWaitEventIndex = -1;
		m_SelectedStopWaitEventIndex.Clear();
		m_WaitEventSelected = false;
	}

	private void UpdateFrameInfoBox(Point mouse_pos)
	{
		if (MainForm.Inst == null)
		{
			return;
		}
		if (m_MeasureLine.Dragging)
		{
			long start_time = XToTime(m_MeasureLine.StartX, m_VisibleTimeRange);
			long end_time = XToTime(m_MeasureLine.EndX, m_VisibleTimeRange);
			MeasureLine.UpdateTimerInfoBox(start_time, end_time, m_Session.TimerFrequency, PointToScreen(mouse_pos));
			return;
		}
		WaitEvent waitEvent = GetWaitEvent(mouse_pos);
		if (!m_WaitEventSelected)
		{
			SelectWaitEvent(waitEvent);
		}
		if (waitEvent != null)
		{
			HoverBox hoverBox = MainForm.Inst.HoverBox;
			hoverBox.Visible = true;
			string text = "WaitEvent" + waitEvent.Time + waitEvent.EventId + waitEvent.ThreadId + waitEvent.Core + waitEvent.Mode;
			if (!(hoverBox.Target is string) || (string)hoverBox.Target != text)
			{
				hoverBox.Target = text;
				hoverBox.Clear();
				switch (waitEvent.Mode)
				{
				case WaitEvent.WaitEventMode.Start:
					hoverBox.Title = "Wait Event Start";
					break;
				case WaitEvent.WaitEventMode.Stop:
					hoverBox.Title = "Wait Event Stop";
					break;
				case WaitEvent.WaitEventMode.Trigger:
					hoverBox.Title = "Wait Event Trigger";
					break;
				}
				hoverBox.AddLine("Id", "0x" + waitEvent.EventId.ToString("x"));
				hoverBox.AddLine("Thread", m_Session.GetThreadName(waitEvent.ThreadId));
				hoverBox.AddLine("Core", waitEvent.Core.ToString());
				Dictionary<int, int> dictionary = new Dictionary<int, int>();
				int trigger_event = -1;
				Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
				GetWaitEventStartTriggerAndStop(waitEvent, dictionary, ref trigger_event, dictionary2);
				int threadId = waitEvent.ThreadId;
				if (dictionary.ContainsKey(threadId) && dictionary2.ContainsKey(threadId))
				{
					CheckIsMainThread();
					WaitEvent waitEvent2 = m_RenderData.m_WaitEvents[dictionary[threadId]].m_WaitEvent;
					long time = m_RenderData.m_WaitEvents[dictionary2[threadId]].m_WaitEvent.Time - waitEvent2.Time;
					hoverBox.AddLine("Wait Time", Utils.GetTimeString(time, m_Session.TimerFrequency));
				}
				else
				{
					hoverBox.AddLine("Wait Time", "unknown");
				}
				hoverBox.SubmitLines();
				Refresh();
			}
			Point location = PointToScreen(mouse_pos);
			hoverBox.SetLocation(location);
			return;
		}
		if (m_SelectedWaitEvent != null && !m_WaitEventSelected)
		{
			ClearSelectedWaitEvent();
			Refresh();
		}
		if (FindContextSwitch(mouse_pos, out var out_context_switch))
		{
			HoverBox hoverBox2 = MainForm.Inst.HoverBox;
			hoverBox2.Visible = true;
			string contextSwitchIdentifier = GetContextSwitchIdentifier(out_context_switch);
			if (!(hoverBox2.Target is string) || (string)hoverBox2.Target != contextSwitchIdentifier)
			{
				hoverBox2.Target = contextSwitchIdentifier;
				hoverBox2.Clear();
				string processName = GetProcessName(out_context_switch);
				string value = ((out_context_switch.m_OldThreadId != 0) ? m_Session.GetThreadName(out_context_switch.m_OldThreadId) : "None");
				string value2 = ((out_context_switch.m_NewThreadId != 0) ? m_Session.GetThreadName(out_context_switch.m_NewThreadId) : "None");
				hoverBox2.Title = "Context Switch";
				hoverBox2.AddLine("Process", processName);
				hoverBox2.AddLine("Old Thread", value);
				hoverBox2.AddLine("New Thread", value2);
				if (m_Session.Platform == Platform.PS4)
				{
					hoverBox2.AddLine("Incident", out_context_switch.m_OldThreadState.ToString());
				}
				else
				{
					hoverBox2.AddLine("Old Thread State", out_context_switch.m_OldThreadState.ToString());
					hoverBox2.AddLine("Old Thread Wait Reason", out_context_switch.m_OldThreadWaitReason.ToString());
				}
				hoverBox2.SubmitLines();
			}
			Point location2 = PointToScreen(mouse_pos);
			hoverBox2.SetLocation(location2);
			return;
		}
		HoverBox hoverBox3 = MainForm.Inst.HoverBox;
		TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(mouse_pos);
		if (timeSpanRenderData != null)
		{
			hoverBox3.Visible = true;
			if (hoverBox3.Target != timeSpanRenderData)
			{
				hoverBox3.Target = timeSpanRenderData;
				hoverBox3.Clear();
				hoverBox3.AddLine("Thread", m_Session.GetThreadName(timeSpanRenderData.m_ThreadId));
				TimeSpan timeSpan = timeSpanRenderData.m_TimeSpan;
				int frameIndex = m_Session.GetFrameIndex(XToTime(mouse_pos.X, m_VisibleTimeRange));
				Utils.AddTimerInfoBoxLines(hoverBox3, timeSpan, frameIndex, m_Session, "Scope");
				hoverBox3.SubmitLines();
			}
			Point location3 = PointToScreen(mouse_pos);
			hoverBox3.SetLocation(location3);
			return;
		}
		ContextRenderData contextRenderData = GetContextRenderData(mouse_pos);
		if (contextRenderData != null)
		{
			hoverBox3.Visible = true;
			if (hoverBox3.Target != contextRenderData)
			{
				hoverBox3.Target = contextRenderData;
				hoverBox3.Clear();
				string processName2 = GetProcessName(contextRenderData.m_ProcessId, contextRenderData.m_ThreadId);
				hoverBox3.Title = "Thread";
				hoverBox3.AddLine("Name", m_Session.GetThreadName(contextRenderData.m_ThreadId));
				hoverBox3.AddLine("Process", processName2);
				string timeString = Utils.GetTimeString(contextRenderData.m_EndTime - contextRenderData.m_StartTime, m_Session.TimerFrequency);
				hoverBox3.AddLine("Duration:", timeString);
				hoverBox3.SubmitLines();
			}
			Point location4 = PointToScreen(mouse_pos);
			hoverBox3.SetLocation(location4);
		}
		else
		{
			hoverBox3.Visible = false;
		}
	}

	private int FindPrevStartWaitEventIndex(WaitEvent wait_event, int wait_event_index, ref int trigger_event_index)
	{
		CheckIsMainThread();
		for (int num = wait_event_index - 1; num >= 0; num--)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[num].m_WaitEvent;
			if (waitEvent.EventId == wait_event.EventId)
			{
				if (waitEvent.Mode != WaitEvent.WaitEventMode.Trigger)
				{
					if (waitEvent.Mode != 0)
					{
						break;
					}
					return num;
				}
				trigger_event_index = num;
			}
		}
		return -1;
	}

	private int FindNextStopWaitEventIndex(WaitEvent wait_event, int wait_event_index, ref int trigger_event_index)
	{
		CheckIsMainThread();
		for (int i = wait_event_index + 1; i < m_RenderData.m_WaitEvents.Count; i++)
		{
			WaitEvent waitEvent = m_RenderData.m_WaitEvents[i].m_WaitEvent;
			if (waitEvent.EventId != wait_event.EventId)
			{
				continue;
			}
			if (waitEvent.Mode == WaitEvent.WaitEventMode.Trigger)
			{
				trigger_event_index = i;
				continue;
			}
			if (waitEvent.Mode != WaitEvent.WaitEventMode.Stop)
			{
				break;
			}
			return i;
		}
		return -1;
	}

	private void OnTimeRangeChanged()
	{
		if (this.TimeRangeChanged != null)
		{
			this.TimeRangeChanged(m_VisibleTimeRange);
		}
	}

	private TimeSpanRenderData GetTimeSpanRenderData(Point location)
	{
		CheckIsMainThread();
		TimeSpanRenderData timeSpanRenderData = null;
		foreach (CoreRenderData coreRenderDatum in m_RenderData.m_CoreRenderData)
		{
			foreach (TimeSpanRenderData timeSpan in coreRenderDatum.m_TimeSpans)
			{
				if (timeSpan.m_Rect.Contains(location))
				{
					timeSpanRenderData = timeSpan;
				}
			}
			if (timeSpanRenderData != null)
			{
				return timeSpanRenderData;
			}
		}
		return timeSpanRenderData;
	}

	private ContextRenderData GetContextRenderData(Point location)
	{
		CheckIsMainThread();
		foreach (List<ContextRenderData> context in m_RenderData.m_Contexts)
		{
			foreach (ContextRenderData item in context)
			{
				if (item.m_Rect.Contains(location))
				{
					return item;
				}
			}
		}
		return null;
	}

	private void RecalculateView(bool force)
	{
		if (force)
		{
			m_ForceRecalculateCounter++;
		}
		RecalculateView();
	}

	private void RecalculateView()
	{
		if (base.Visible && m_Session != null && m_Session.IsReady && Active)
		{
			UpdateRendererInputs();
			m_CalculateViewThreadWakeEvent.Set();
		}
	}

	private void UpdateRendererInputs()
	{
		lock (m_RendererInput)
		{
			m_RendererInput.Set(m_VisibleTimeRange, GetCoreGraphHeights(), m_ForceRecalculateCounter, m_ShowHeirachy, m_Settings.ScopeColourMode, m_ShowWaitEvents, m_DPIScale);
		}
	}

	public void SetTimeRange(TimeRange time_range)
	{
		if (m_VisibleTimeRange.Equals(time_range))
		{
			return;
		}
		lock (m_VisibleTimeRange)
		{
			long startTime = time_range.m_StartTime;
			if (time_range.m_Scale == m_VisibleTimeRange.m_Scale && m_VisibleTimeRange.m_TicksPerPixel != 0L)
			{
				int num = TimeToX_NoClamp(time_range.m_StartTime, m_VisibleTimeRange);
				startTime = XToTime(num, m_VisibleTimeRange);
			}
			m_VisibleTimeRange.CopyAllExceptScrollY(time_range);
			m_VisibleTimeRange.m_StartTime = startTime;
		}
		RecalculateView();
	}

	private void VScrollBarScroll(object sender, ScrollEventArgs e)
	{
		SetScrollY(m_VScrollBar.Value);
	}

	private void SetScrollY(int value)
	{
		int max = Math.Max(0, m_VScrollBar.Maximum - m_VScrollBar.LargeChange);
		value = Misc.Clamp(value, 0, max);
		m_VScrollBar.Value = value;
		m_VisibleTimeRange.m_ScrollY = value;
		RecalculateView();
		OnScrollChanged();
	}

	private void CheckIsMainThread()
	{
	}

	public void SetHighlightedTimeSpans(Set<long> time_span_names)
	{
		m_HighlightedTimeSpans = new Set<long>(time_span_names);
		RecalculateView(force: true);
	}

	private void ShowInThreadView(int thread_id)
	{
		if (this.CoreGraphShowThread != null)
		{
			this.CoreGraphShowThread(thread_id);
		}
	}

	private void ShowInThreadViewMenuItem(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpanRenderData != null)
		{
			ShowInThreadView(m_ContextMenuTimeSpanRenderData.m_ThreadId);
		}
	}

	private void JumpToSourceCodeMenuItem(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpanRenderData != null)
		{
			Utils.JumpToSourceCode(m_ContextMenuTimeSpanRenderData.m_TimeSpan, m_Session, m_Settings);
		}
	}

	public void SetMeasureLine(int start_x, int end_x)
	{
		m_MeasureLine.Set(start_x, end_x);
	}

	public void OnScopeColourModeChanged()
	{
		RecalculateView();
	}

	private List<int> GetCoreGraphHeights()
	{
		CheckIsMainThread();
		List<int> list = new List<int>();
		int num = ((m_Session != null) ? m_Session.CoreCount : 0);
		if (m_ShowHeirachy)
		{
			if (m_Settings != null)
			{
				list.AddRange(m_Settings.CoreGraphCoreHeights);
			}
		}
		else
		{
			for (int i = 0; i < num; i++)
			{
				list.Add(m_NotShowingHeirachyCoreHeight);
			}
		}
		int item = (m_ShowHeirachy ? 140 : m_NotShowingHeirachyCoreHeight);
		for (int j = list.Count; j < num; j++)
		{
			list.Add(item);
		}
		return list;
	}

	private void OnCoreHeightsChanged()
	{
		if (this.CoreHeightsChanged != null)
		{
			this.CoreHeightsChanged();
		}
	}

	public void SelectTimeSpan(long time_span_name)
	{
		if (m_SelectedTimeSpanName != time_span_name)
		{
			m_SelectedTimeSpanName = time_span_name;
			RecalculateView(force: true);
		}
	}

	private void SelectTimeSpanInternal(long time_span_name)
	{
		if (m_SelectedTimeSpanName != time_span_name)
		{
			SelectTimeSpan(time_span_name);
			OnSelectedTimeSpanChanged();
		}
	}

	private void OnSelectedTimeSpanChanged()
	{
		if (this.SelectedTimeSpanChanged != null)
		{
			this.SelectedTimeSpanChanged(m_SelectedTimeSpanName, null);
		}
	}

	public void OnScopeColourChanged()
	{
		m_RenderData.m_ClearBrushes = true;
		RecalculateView(force: true);
	}

	private void SetScopeColourMenuItemClick(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpanRenderData != null)
		{
			ColorDialog colorDialog = new ColorDialog();
			if (colorDialog.ShowDialog(this) == DialogResult.OK)
			{
				TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(m_ContextMenuTimeSpanRenderData.m_TimeSpan.TimeSpanInfoId);
				string timerName = m_Session.GetTimerName(timeSpanInfo.Name);
				m_Session.SetScopeColour(timerName, colorDialog.Color);
			}
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (Utils.IsShiftHeld)
		{
			int num = base.Height / 3;
			int num2 = ((e.Delta > 0) ? (-num) : num);
			SetScrollY(m_VScrollBar.Value + num2);
		}
	}

	public int GetDesiredHeight()
	{
		int num = ScaleDPI(m_NotShowingHeirachyCoreHeight);
		return GetFirstCoreYOffset(m_ShowHeirachy) + num * m_Session.CoreCount;
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.m_VScrollBar = new System.Windows.Forms.VScrollBar();
		this.m_ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.showInThreadViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.jumpToSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.setColourToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ContextMenuStrip.SuspendLayout();
		base.SuspendLayout();
		this.m_VScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_VScrollBar.Location = new System.Drawing.Point(1040, 0);
		this.m_VScrollBar.Name = "m_VScrollBar";
		this.m_VScrollBar.Size = new System.Drawing.Size(17, 178);
		this.m_VScrollBar.TabIndex = 0;
		this.m_VScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(VScrollBarScroll);
		this.m_ContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.showInThreadViewToolStripMenuItem, this.jumpToSourceToolStripMenuItem, this.setColourToolStripMenuItem });
		this.m_ContextMenuStrip.Name = "m_ContextMenuStrip";
		this.m_ContextMenuStrip.Size = new System.Drawing.Size(188, 92);
		this.showInThreadViewToolStripMenuItem.Name = "showInThreadViewToolStripMenuItem";
		this.showInThreadViewToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
		this.showInThreadViewToolStripMenuItem.Text = "Show In Thread View";
		this.showInThreadViewToolStripMenuItem.Click += new System.EventHandler(ShowInThreadViewMenuItem);
		this.jumpToSourceToolStripMenuItem.Name = "jumpToSourceToolStripMenuItem";
		this.jumpToSourceToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
		this.jumpToSourceToolStripMenuItem.Text = "Go to Source";
		this.jumpToSourceToolStripMenuItem.Click += new System.EventHandler(JumpToSourceCodeMenuItem);
		this.setColourToolStripMenuItem.Name = "setColourToolStripMenuItem";
		this.setColourToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
		this.setColourToolStripMenuItem.Text = "Set Colour";
		this.setColourToolStripMenuItem.Click += new System.EventHandler(SetScopeColourMenuItemClick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_VScrollBar);
		base.Name = "CoreGraph";
		base.Size = new System.Drawing.Size(1057, 178);
		this.m_ContextMenuStrip.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
