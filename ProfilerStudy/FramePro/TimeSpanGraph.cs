using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Editor;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class TimeSpanGraph : UserControl
{
	private struct TimeSpanRenderData
	{
		public TimeSpan m_TimeSpan;

		public Rectangle m_Rect;

		public string m_Text;

		public Point m_TextPos;

		public bool m_IsIdleTimeSpan;

		public bool m_ClipText;

		public bool m_Highlight;

		public bool m_Select;

		public bool m_IsHiResTimer;

		public HiResTimer m_HiResTimer;

		public List<Rectangle> m_IdleRects;
	}

	private struct ColouredRect
	{
		public Rectangle m_Rect;

		public Color m_Colour;
	}

	private class RenderData : IDisposable
	{
		public class ScopeRectArray
		{
			public RectArray m_RectArray = new RectArray();

			public int m_Count;
		}

		public long m_RenderEndTime;

		public List<FrameRenderData> m_FrameRenderData = new List<FrameRenderData>();

		public List<Frame> m_VisibleFrames = new List<Frame>();

		public RectArray m_IdleTimeSpanRects = new RectArray();

		public List<ColouredRect> m_HiResTimerRects = new List<ColouredRect>();

		public RectArray m_HighlightRects = new RectArray();

		public RectArray m_SelectedRects = new RectArray();

		public Dictionary<Color, ScopeRectArray> m_ScopeColourTimeSpanRects = new Dictionary<Color, ScopeRectArray>();

		public Dictionary<Color, ScopeRectArray> m_IdleScopeColourTimeSpanRects = new Dictionary<Color, ScopeRectArray>();

		public List<TimeSpanRenderData> m_TimeSpanRenderData = new List<TimeSpanRenderData>();

		public bool m_ClearScopeColoursMap;

		public void Dispose()
		{
			m_IdleTimeSpanRects.Dispose();
			m_HighlightRects.Dispose();
			m_SelectedRects.Dispose();
		}
	}

	private struct FrameRenderData
	{
		public Frame m_Frame;

		public int m_X;
	}

	private enum MouseDragMode
	{
		Horizontal,
		Vertical
	}

	private class State
	{
		public TimeRange m_TimeRange = new TimeRange();

		public ScopeColourMode m_ScopeColourMode;

		public int m_ForceUpdateId;

		public Color m_ThreadColour;

		public void Copy(State other)
		{
			m_TimeRange.Copy(other.m_TimeRange);
			m_ScopeColourMode = other.m_ScopeColourMode;
			m_ForceUpdateId = other.m_ForceUpdateId;
			m_ThreadColour = other.m_ThreadColour;
		}

		public bool Equals(State other)
		{
			if (m_TimeRange.Equals(other.m_TimeRange) && m_ScopeColourMode == other.m_ScopeColourMode && m_ForceUpdateId == other.m_ForceUpdateId)
			{
				return m_ThreadColour == other.m_ThreadColour;
			}
			return false;
		}
	}

	private Session m_Session;

	private int m_MainThreadId;

	private string m_ThreadName;

	private int m_ThreadId;

	private int m_TimeSpanHeight;

	private Pen m_FramePen = new Pen(Colours.FrameLine);

	private Brush m_TimeSpanBrush;

	private Brush m_IdleTimeSpanBrush;

	private Brush m_HiResTimerBrush;

	private Pen m_TimeSpanBorderPen = new Pen(Color.Black);

	private Pen m_IdleTimeSpanBorderPen = new Pen(Color.Black);

	private Pen m_HighlightPen = new Pen(Colours.TimeSpanHighlight, 2f);

	private Brush m_TimeSpanTextBrush = new SolidBrush(Colours.TimeSpanText);

	private const int m_TextXGap = 2;

	private const int m_MinTextRectWidth = 15;

	private MouseDragMode m_MouseDragMode;

	private bool m_StartedDragMove;

	private Point m_StartDragMousePos;

	private long m_StartDragTime;

	private Point m_LastDragMouseScreenPos;

	private Color m_Colour;

	private int m_IdealHeight;

	private int m_ScrollMaxHeight;

	private Thread m_CalculateViewThread;

	private AutoResetEvent m_CalculateViewThreadWakeEvent = new AutoResetEvent(initialState: false);

	private AutoResetEvent m_CalculateViewOnCompleteFinished = new AutoResetEvent(initialState: false);

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private State m_State = new State();

	private State m_NewDrawState = new State();

	private State m_DrawState = new State();

	private State m_NewCalcThreadState = new State();

	private State m_CalcState = new State();

	private RenderData m_RenderData = new RenderData();

	private RenderData m_RendererRenderData = new RenderData();

	private Rectangle m_LastRect;

	private TimeSpan m_ContextMenuTimeSpan;

	private int m_ContextMenuFrameIndex;

	private Set<long> m_HighlightedTimeSpans = new Set<long>();

	private ReadWriteLock m_HighlightedTimeSpansLock = new ReadWriteLock();

	private TimeSpan m_SingleHighlightedTimeSpan;

	private long m_SelectedTimeSpanName = -1L;

	private TimeSpan m_SelectedTimeSpan;

	private Pen m_HighlightBorderPen = new Pen(Colours.TimeSpanHighlight, 2f);

	private Pen m_SelectedTimespanBorderPen = new Pen(Colours.SelectedTimeSpan, 6f);

	private float m_SelectedTimespanBorderPenWidth;

	private Pen m_SelectedTimespanBorderPenOutline = new Pen(Colours.SelectedTimeSpanOutline, 1f);

	private MeasureLine m_MeasureLine = new MeasureLine();

	private Pen m_MeasureLinePen = new Pen(new SolidBrush(Colours.MeasureLineColour));

	private Brush m_MeasureLineFillBrush = new SolidBrush(Colours.MeasureLineFillColour);

	private Pen m_TimeLinePen;

	private bool m_MouseHoverEnabled = true;

	private long m_CurrentMouseTime;

	private volatile bool m_Disposing;

	private bool m_Active;

	private bool m_ScrolledIntoView;

	private Point[] m_TimeLines = new Point[0];

	private const int m_MinTimeLinesSpacing = 10;

	private Dictionary<int, Color> m_TimeSpanInfoToColourMap = new Dictionary<int, Color>();

	private Dictionary<Color, Brush> m_ScopeColourBrushes = new Dictionary<Color, Brush>();

	private HatchBrushManager m_HatchBrushManager = new HatchBrushManager();

	private Settings m_Settings;

	private Brush m_ResizeLineBrush = new SolidBrush(Colours.TimeSpanGraphResizeLine);

	private const int m_ResizeLineHeight = 3;

	private const int m_ResizeHoverMinTime = 120;

	private bool m_ResizeHoverTimerStarted;

	private System.Windows.Forms.Timer m_ResizeHoverTimer = new System.Windows.Forms.Timer();

	private bool m_ResizeHoverStarted;

	private bool m_Resizing;

	private Point m_StartResizeScreenPos;

	private int m_StartResizeHeight;

	private int m_MaxHorzDragOffset;

	private const int m_DragModeSnapDist = 20;

	private float m_DPIScale = 1f;

	private Font m_TimeSpanFont;

	private const float m_TimeSpanFontScale = 0.5f;

	private const int m_MintimeSpanHeight = 8;

	private const int m_MaxtimeSpanHeight = 60;

	private IContainer components;

	private VScrollBar m_VScrollBar;

	private ContextMenuStrip m_ScopeContextMenu;

	private ToolStripMenuItem jumpToSourceCodeToolStripMenuItem;

	private ToolStripMenuItem selectToolStripMenuItem1;

	private ToolStripMenuItem copyToolStripMenuItem;

	private ToolStripMenuItem zoomToScopeToolStripMenuItem;

	private ContextMenuStrip m_MeasureContextMenu;

	private ToolStripMenuItem m_DurationContextMenuItem;

	private ToolStripMenuItem toolStripMenuItem2;

	private ToolStripMenuItem highlightToolStripMenuItem;

	private ContextMenuStrip m_ContextMenu;

	private ToolStripMenuItem jumpToNextScopeToolStripMenuItem;

	private ToolStripMenuItem jumpToNextScopeToolStripMenuItem1;

	private ToolStripMenuItem findPrevScopeToolStripMenuItem;

	private ToolStripMenuItem findPrevScopeToolStripMenuItem1;

	private ToolStripMenuItem m_SetScopeColourMenuItem;

	private ToolStripMenuItem goToMaxToolStripMenuItem;

	private bool RenderStateNeedsUpdating
	{
		get
		{
			lock (m_NewCalcThreadState)
			{
				return !m_CalcState.Equals(m_NewCalcThreadState);
			}
		}
	}

	public bool NeedsUpdate
	{
		get
		{
			CheckIsMainThread();
			if (m_RenderData.m_RenderEndTime >= XToTime(base.ClientSize.Width, m_State.m_TimeRange))
			{
				return m_RenderData.m_ClearScopeColoursMap;
			}
			return true;
		}
	}

	public Color ThreadColour
	{
		get
		{
			return m_Colour;
		}
		set
		{
			m_Colour = value;
			m_TimeSpanBrush = new SolidBrush(value);
			m_IdleTimeSpanBrush = new SolidBrush(GetThreadIdleColour(value));
			m_HiResTimerBrush = new SolidBrush(Utils.Lerp(value, Color.Black, 0.04f));
			m_TimeSpanBorderPen = new Pen(Utils.Lerp(Color.Black, value, Colours.TimeSpanBorderValue));
			m_IdleTimeSpanBorderPen = new Pen(Utils.Lerp(m_TimeSpanBorderPen.Color, Colours.TimeSpanBackground, Colours.TimeSpanIdleTransparency));
			m_State.m_ThreadColour = value;
			Refresh();
		}
	}

	public int IdealHeight => m_IdealHeight;

	public int TimeSpanHeight
	{
		get
		{
			return m_TimeSpanHeight;
		}
		set
		{
			if (m_TimeSpanHeight != value)
			{
				m_TimeSpanHeight = value;
				UpdateTimeSpanFont();
				RecalculateView(force: true);
			}
		}
	}

	public bool IdealHeightValid => m_IdealHeight != 0;

	public int HighlightedTimeSpanCount
	{
		get
		{
			if (m_RenderData.m_HighlightRects == null)
			{
				return 0;
			}
			return m_RenderData.m_HighlightRects.Length;
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

	public bool Active
	{
		get
		{
			return m_Active;
		}
		set
		{
			if (m_Active != value)
			{
				m_Active = value;
				if (value)
				{
					RecalculateView(force: true);
				}
			}
		}
	}

	public bool ScrolledIntoView
	{
		get
		{
			return m_ScrolledIntoView;
		}
		set
		{
			if (m_ScrolledIntoView != value)
			{
				m_ScrolledIntoView = value;
				RecalculateView(force: true);
			}
		}
	}

	public string ThreadName
	{
		get
		{
			return m_ThreadName;
		}
		set
		{
			m_ThreadName = value;
			RecalculateView(force: true);
		}
	}

	public event TimeRangeChangedHandler TimeRangeChanged;

	public event SelectedTimeSpanChangedHandler SelectedTimeSpanChanged;

	public event MeasureLineChangedHandler MeasureLineChanged;

	public event HighlightScopeHandler HighlightScope;

	public event HighlightSingleScopeHandler HighlightSingleScope;

	public event IdealHeightChangedHandler IdealHeightChanged;

	public event ScrollYHandler ScrollY;

	public event UserResizedHeightHandler UserResizedHeight;

	public event TimeSpanHeightChangedHandler TimeSpanHeightChanged;

	public TimeSpanGraph(Session session, string thread_name, int thread_id, long min_time, ScopeColourMode scope_colour_mode, Settings settings)
	{
		InitializeComponent();
		m_Session = session;
		m_Settings = settings;
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		m_MeasureLine.Changed += MeasureLineChangedEvent;
		m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
		m_TimeSpanHeight = m_Settings.ThreadScopeHeight;
		m_ThreadName = thread_name;
		m_ThreadId = thread_id;
		m_State.m_TimeRange.m_StartTime = min_time;
		m_State.m_ScopeColourMode = scope_colour_mode;
		m_CalculateViewThread = new Thread(CalculateViewThread);
		m_CalculateViewThread.Name = "TimeSpanGraph";
		m_CalculateViewThread.Start();
		m_ResizeHoverTimer.Interval = 120;
		m_ResizeHoverTimer.Tick += ResizeHoverTimerTick;
		RecalculateView();
		m_DPIScale = MainForm.DPIScale;
		m_HighlightBorderPen = new Pen(m_HighlightBorderPen.Color, m_HighlightBorderPen.Width * m_DPIScale);
		m_SelectedTimespanBorderPen = new Pen(m_SelectedTimespanBorderPen.Color, m_SelectedTimespanBorderPen.Width * m_DPIScale);
		m_SelectedTimespanBorderPenOutline = new Pen(m_SelectedTimespanBorderPenOutline.Color, m_SelectedTimespanBorderPenOutline.Width * m_DPIScale);
		m_SelectedTimespanBorderPenWidth = m_SelectedTimespanBorderPen.Width;
		m_FramePen = new Pen(Colours.FrameLine, ScaleDPI(1));
		m_VScrollBar.Size = new Size(ScaleDPI(m_VScrollBar.Width), m_VScrollBar.Height);
		UpdateTimeSpanFont();
	}

	private void MeasureLineChangedEvent(int start_x, int end_x)
	{
		Refresh();
		if (this.MeasureLineChanged != null)
		{
			this.MeasureLineChanged(start_x, end_x);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_Disposing = true;
		m_CalculateViewThreadWakeEvent.Set();
		m_CalculateViewOnCompleteFinished.Set();
		m_CalculateViewThread.Join();
		m_MeasureLineFillBrush.Dispose();
		m_SelectedTimespanBorderPen.Dispose();
		m_SelectedTimespanBorderPenOutline.Dispose();
		m_TimeSpanBrush.Dispose();
		m_HiResTimerBrush.Dispose();
		m_TimeSpanTextBrush.Dispose();
		m_TimeSpanBorderPen.Dispose();
		m_MeasureLinePen.Dispose();
		m_IdleTimeSpanBrush.Dispose();
		m_HighlightBorderPen.Dispose();
		m_HighlightPen.Dispose();
		m_IdleTimeSpanBorderPen.Dispose();
		m_FramePen.Dispose();
		foreach (Brush value in m_ScopeColourBrushes.Values)
		{
			value.Dispose();
		}
		m_ScopeColourBrushes.Clear();
		m_CalculateViewThreadWakeEvent.Dispose();
		m_CalculateViewOnCompleteFinished.Dispose();
		m_RenderData.Dispose();
		m_RendererRenderData.Dispose();
		base.Dispose(disposing);
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void CalculateViewThread()
	{
		while (!m_Disposing)
		{
			if (!RenderStateNeedsUpdating)
			{
				m_CalculateViewThreadWakeEvent.WaitOne();
			}
			if (RenderStateNeedsUpdating)
			{
				lock (m_NewCalcThreadState)
				{
					m_CalcState.Copy(m_NewCalcThreadState);
				}
				if (m_Session != null && m_CalcState.m_TimeRange.m_TicksPerPixel != 0L)
				{
					CalculateView();
				}
				lock (m_NewDrawState)
				{
					m_NewDrawState.Copy(m_CalcState);
				}
				m_ControlTaskDispatcher.QueueTask(delegate
				{
					OnCalculateViewComplete(this);
				});
				m_CalculateViewOnCompleteFinished.WaitOne();
			}
		}
	}

	private void OnCalculateViewComplete(TimeSpanGraph time_span_graph)
	{
		CheckIsMainThread();
		if (time_span_graph == this)
		{
			Misc.Swap(ref m_RenderData, ref m_RendererRenderData);
			lock (m_NewDrawState)
			{
				m_DrawState.Copy(m_NewDrawState);
			}
			if (m_RendererRenderData.m_ClearScopeColoursMap)
			{
				m_TimeSpanInfoToColourMap.Clear();
			}
			if (m_IdealHeight != m_ScrollMaxHeight)
			{
				m_ScrollMaxHeight = m_IdealHeight;
				UpdateVScrollBar();
			}
			CalculateTimeSpanStateText();
			Refresh();
			m_CalculateViewOnCompleteFinished.Set();
			if (RenderStateNeedsUpdating)
			{
				RecalculateView();
			}
		}
	}

	private void UpdateVScrollBar()
	{
		m_VScrollBar.Maximum = m_ScrollMaxHeight;
		m_VScrollBar.LargeChange = Math.Max(0, base.ClientSize.Height);
		m_VScrollBar.Visible = m_ScrollMaxHeight > base.ClientSize.Height;
	}

	public void UpdateGraph(bool force)
	{
		RecalculateView(force);
	}

	private void CalculateView()
	{
		TimeRange timeRange = m_CalcState.m_TimeRange;
		long num = base.ClientSize.Width * timeRange.m_TicksPerPixel;
		long startTime = timeRange.m_StartTime;
		long num2 = startTime + num;
		m_RendererRenderData.m_RenderEndTime = Math.Min(m_Session.LastFrameEndTime, num2);
		CalculateTimeSpanFrame(startTime, num2);
		CalcaulteTimeSpanRenderData(startTime, num2);
	}

	private void CalculateTimeSpanFrame(long start_time, long end_time)
	{
		m_RendererRenderData.m_VisibleFrames.Clear();
		if (m_Session == null || m_CalcState.m_TimeRange.m_TicksPerPixel == 0L)
		{
			return;
		}
		m_Session.GetFrames(start_time, end_time, m_RendererRenderData.m_VisibleFrames);
		m_RendererRenderData.m_FrameRenderData.Clear();
		foreach (Frame visibleFrame in m_RendererRenderData.m_VisibleFrames)
		{
			FrameRenderData item = default(FrameRenderData);
			item.m_Frame = visibleFrame;
			item.m_X = TimeToX(visibleFrame.EndTime, m_CalcState.m_TimeRange);
			item.m_X = Misc.Clamp(item.m_X, -1, base.ClientSize.Width + 1);
			m_RendererRenderData.m_FrameRenderData.Add(item);
		}
	}

	private void CalculateTimeSpanStateText()
	{
		Graphics graphics = CreateGraphics();
		int count = m_RenderData.m_TimeSpanRenderData.Count;
		for (int i = 0; i < count; i++)
		{
			TimeSpanRenderData timeSpanRenderData = m_RenderData.m_TimeSpanRenderData[i];
			Point text_pos = default(Point);
			timeSpanRenderData.m_Text = GetTimeSpanRenderText(timeSpanRenderData, graphics, out var _, ref text_pos, out var clip_text);
			timeSpanRenderData.m_TextPos = text_pos;
			timeSpanRenderData.m_ClipText = clip_text;
			m_RenderData.m_TimeSpanRenderData[i] = timeSpanRenderData;
		}
		graphics.Dispose();
	}

	private RenderData.ScopeRectArray GetScopeRectArray(Color colour)
	{
		if (!m_RendererRenderData.m_ScopeColourTimeSpanRects.TryGetValue(colour, out var value))
		{
			value = new RenderData.ScopeRectArray();
			m_RendererRenderData.m_ScopeColourTimeSpanRects[colour] = value;
		}
		return value;
	}

	private RenderData.ScopeRectArray GetIdleScopeRectArray(Color colour)
	{
		if (!m_RendererRenderData.m_IdleScopeColourTimeSpanRects.TryGetValue(colour, out var value))
		{
			value = new RenderData.ScopeRectArray();
			m_RendererRenderData.m_IdleScopeColourTimeSpanRects[colour] = value;
		}
		return value;
	}

	private List<int> GetThreadIds()
	{
		if (m_ThreadId != 0)
		{
			return new List<int> { m_ThreadId };
		}
		return m_Session.GetThreadIds(m_ThreadName);
	}

	private void CalcaulteTimeSpanRenderData(long start_time, long end_time)
	{
		m_RendererRenderData.m_TimeSpanRenderData.Clear();
		int num = GetDPIScaledTimeSpanHeight();
		List<int> threadIds = GetThreadIds();
		TimeRange timeRange = m_CalcState.m_TimeRange;
		bool flag = m_CalcState.m_ScopeColourMode == ScopeColourMode.Scope;
		List<List<ContextSwitch>> context_switches = null;
		List<Utils.IdleRange> list = null;
		if (m_Session.RecordingContextSwitches)
		{
			m_Session.GetContextSwitches(start_time, end_time, out context_switches);
			list = Utils.GetCPUIdleTimes(threadIds, start_time, context_switches);
		}
		foreach (int item4 in threadIds)
		{
			TimeSpan timeSpan = m_Session.GetTimeSpan(item4, start_time);
			m_LastRect = default(Rectangle);
			TimeSpanIterator timeSpanIterator = new TimeSpanIterator(timeSpan);
			while (timeSpanIterator.MoveNext() && (timeSpanIterator.Current.Parent.Parent != null || timeSpanIterator.Current.StartTime <= end_time))
			{
				TimeSpan current2 = timeSpanIterator.Current;
				TimeSpanRenderData item = default(TimeSpanRenderData);
				item.m_TimeSpan = current2;
				Rectangle timeSpanRect = GetTimeSpanRect(current2, timeSpanIterator.Depth, timeRange);
				if (current2.IsTimeSpanEx && ((TimeSpanEx)current2).HiResTimers != null)
				{
					TimeSpanEx obj = current2 as TimeSpanEx;
					long num2 = current2.StartTime;
					_ = current2.Duration;
					item.m_IsHiResTimer = true;
					foreach (HiResTimer hiResTimer2 in obj.HiResTimers)
					{
						HiResTimer hiResTimer = (item.m_HiResTimer = hiResTimer2);
						item.m_HiResTimer.m_Name = m_Session.RemapStringId(hiResTimer.m_Name);
						int num3 = Math.Max(-1, TimeToX(num2, timeRange));
						int num4 = TimeToX(num2 + hiResTimer.m_Duration, timeRange);
						num2 += hiResTimer.m_Duration;
						item.m_Rect = new Rectangle(num3, timeSpanRect.Y, num4 - num3, timeSpanRect.Height);
						long name = item.m_HiResTimer.m_Name;
						item.m_Highlight = IsHighlighted(name);
						item.m_Select = name == m_SelectedTimeSpanName && name != -1;
						if (item.m_Rect.Right > 0 && item.m_Rect.X < base.Width && (item.m_Rect != m_LastRect || item.m_Rect.Width > 0 || item.m_Highlight || item.m_Select))
						{
							m_LastRect = item.m_Rect;
							m_RendererRenderData.m_TimeSpanRenderData.Add(item);
							int bottom = item.m_Rect.Bottom;
							if (bottom > num)
							{
								num = bottom;
							}
						}
					}
					long num5 = current2.EndTime - num2;
					if (num5 != 0L)
					{
						item.m_HiResTimer.m_Name = -1L;
						item.m_HiResTimer.m_Duration = num5;
						item.m_HiResTimer.m_Count = 1L;
						int num6 = Math.Max(-1, TimeToX(num2, timeRange));
						int num7 = TimeToX(num2 + num5, timeRange);
						item.m_Rect = new Rectangle(num6, timeSpanRect.Y, num7 - num6, timeSpanRect.Height);
						item.m_Select = false;
						m_RendererRenderData.m_TimeSpanRenderData.Add(item);
					}
					continue;
				}
				item.m_Rect = timeSpanRect;
				if (list != null && timeSpanRect.Width != 0)
				{
					int num8 = -1;
					List<Utils.IdleRange> list2 = new List<Utils.IdleRange>();
					Utils.GetTimeSpanIdleRanges(list, current2, list2);
					foreach (Utils.IdleRange item5 in list2)
					{
						int num9 = TimeToX(item5.m_StartTime, timeRange);
						int num10 = TimeToX(item5.m_EndTime, timeRange);
						if (num9 != num10 && num10 != num8)
						{
							Rectangle item2 = new Rectangle(num9, timeSpanRect.Y + 1, num10 - num9, timeSpanRect.Height - 1);
							if (item.m_IdleRects == null)
							{
								item.m_IdleRects = new List<Rectangle>();
							}
							item.m_IdleRects.Add(item2);
							num8 = num10;
						}
					}
				}
				TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(current2.TimeSpanInfoId);
				item.m_Highlight = IsHighlighted(timeSpanInfo.Name) || current2 == m_SingleHighlightedTimeSpan;
				item.m_Select = timeSpanInfo.Name == m_SelectedTimeSpanName;
				if (item.m_Rect.Right > 0 && item.m_Rect.X < base.Width && (item.m_Rect != m_LastRect || item.m_Rect.Width > 0 || item.m_Highlight || item.m_Select))
				{
					m_LastRect = item.m_Rect;
					SourceInfoStruct sourceInfo = m_Session.GetSourceInfo(timeSpanInfo.SourceInfo);
					item.m_IsIdleTimeSpan = sourceInfo.IsValid && sourceInfo.TimeSpanType == TimeSpanType.Idle;
					m_RendererRenderData.m_TimeSpanRenderData.Add(item);
					int bottom2 = item.m_Rect.Bottom;
					if (bottom2 > num)
					{
						num = bottom2;
					}
				}
			}
		}
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		foreach (TimeSpanRenderData timeSpanRenderDatum in m_RendererRenderData.m_TimeSpanRenderData)
		{
			if (timeSpanRenderDatum.m_IsIdleTimeSpan)
			{
				num12++;
			}
			else if (timeSpanRenderDatum.m_IsHiResTimer)
			{
				num13++;
			}
			else
			{
				num11++;
			}
			if (timeSpanRenderDatum.m_Highlight)
			{
				num14++;
			}
			if (timeSpanRenderDatum.m_Select)
			{
				num15++;
			}
		}
		foreach (RenderData.ScopeRectArray value2 in m_RendererRenderData.m_ScopeColourTimeSpanRects.Values)
		{
			value2.m_RectArray.Resize(0, can_reduce: false);
			value2.m_Count = 0;
		}
		foreach (RenderData.ScopeRectArray value3 in m_RendererRenderData.m_IdleScopeColourTimeSpanRects.Values)
		{
			value3.m_RectArray.Resize(0, can_reduce: false);
			value3.m_Count = 0;
		}
		if (flag)
		{
			foreach (TimeSpanRenderData timeSpanRenderDatum2 in m_RendererRenderData.m_TimeSpanRenderData)
			{
				if (timeSpanRenderDatum2.m_IsIdleTimeSpan)
				{
					continue;
				}
				int timeSpanInfoId = timeSpanRenderDatum2.m_TimeSpan.TimeSpanInfoId;
				if (!m_TimeSpanInfoToColourMap.TryGetValue(timeSpanInfoId, out var value))
				{
					if (timeSpanInfoId != TimeSpanInfo.InvalidInfoId)
					{
						TimeSpanInfo timeSpanInfo2 = m_Session.GetTimeSpanInfo(timeSpanInfoId);
						value = m_Session.GetScopeColour(timeSpanInfo2.Name);
					}
					m_TimeSpanInfoToColourMap[timeSpanInfoId] = value;
				}
				GetScopeRectArray(value).m_Count++;
				if (timeSpanRenderDatum2.m_IdleRects != null)
				{
					Color idleColour = GetIdleColour(value);
					GetIdleScopeRectArray(idleColour).m_Count += timeSpanRenderDatum2.m_IdleRects.Count;
				}
			}
		}
		foreach (RenderData.ScopeRectArray value4 in m_RendererRenderData.m_ScopeColourTimeSpanRects.Values)
		{
			value4.m_RectArray.Resize(value4.m_Count);
			value4.m_Count = 0;
		}
		foreach (RenderData.ScopeRectArray value5 in m_RendererRenderData.m_IdleScopeColourTimeSpanRects.Values)
		{
			value5.m_RectArray.Resize(value5.m_Count);
			value5.m_Count = 0;
		}
		m_RendererRenderData.m_HiResTimerRects.Clear();
		m_RendererRenderData.m_HiResTimerRects.Capacity = num13;
		m_RendererRenderData.m_IdleTimeSpanRects.Resize(num12);
		m_RendererRenderData.m_HighlightRects.Resize(num14);
		m_RendererRenderData.m_SelectedRects.Resize(num15);
		Color threadColour = m_CalcState.m_ThreadColour;
		Color threadIdleColour = GetThreadIdleColour(threadColour);
		int num16 = 0;
		int num17 = 0;
		int num18 = 0;
		foreach (TimeSpanRenderData timeSpanRenderDatum3 in m_RendererRenderData.m_TimeSpanRenderData)
		{
			Color colour;
			if (timeSpanRenderDatum3.m_IsIdleTimeSpan)
			{
				m_RendererRenderData.m_IdleTimeSpanRects.m_Array[num16++] = timeSpanRenderDatum3.m_Rect;
				colour = threadIdleColour;
			}
			else if (timeSpanRenderDatum3.m_IsHiResTimer)
			{
				colour = (flag ? GetTimeSpanColour(timeSpanRenderDatum3.m_TimeSpan.Parent) : threadColour);
				ColouredRect item3 = default(ColouredRect);
				item3.m_Rect = timeSpanRenderDatum3.m_Rect;
				item3.m_Colour = colour;
				m_RendererRenderData.m_HiResTimerRects.Add(item3);
			}
			else
			{
				colour = (flag ? GetTimeSpanColour(timeSpanRenderDatum3.m_TimeSpan) : threadColour);
				RenderData.ScopeRectArray scopeRectArray = GetScopeRectArray(colour);
				if (scopeRectArray.m_RectArray.Length == scopeRectArray.m_Count)
				{
					scopeRectArray.m_RectArray.Resize(scopeRectArray.m_Count + 32);
				}
				scopeRectArray.m_RectArray.m_Array[scopeRectArray.m_Count++] = timeSpanRenderDatum3.m_Rect;
			}
			if (timeSpanRenderDatum3.m_IdleRects != null)
			{
				Color idleColour2 = GetIdleColour(colour);
				RenderData.ScopeRectArray idleScopeRectArray = GetIdleScopeRectArray(idleColour2);
				foreach (Rectangle idleRect in timeSpanRenderDatum3.m_IdleRects)
				{
					if (idleScopeRectArray.m_RectArray.Length == idleScopeRectArray.m_Count)
					{
						idleScopeRectArray.m_RectArray.Resize(idleScopeRectArray.m_Count + 32);
					}
					idleScopeRectArray.m_RectArray.m_Array[idleScopeRectArray.m_Count++] = idleRect;
				}
			}
			if (timeSpanRenderDatum3.m_Highlight)
			{
				Rectangle rect = timeSpanRenderDatum3.m_Rect;
				int num19 = rect.X;
				rect = timeSpanRenderDatum3.m_Rect;
				int num20 = rect.Y;
				rect = timeSpanRenderDatum3.m_Rect;
				int num21 = rect.Width + 1;
				rect = timeSpanRenderDatum3.m_Rect;
				Rectangle rectangle = new Rectangle(num19, num20, num21, rect.Height + 1);
				if (rectangle.Width <= 0)
				{
					rectangle = new Rectangle(rectangle.X - 1, rectangle.Y, 2, rectangle.Height + 1);
				}
				m_RendererRenderData.m_HighlightRects.m_Array[num17++] = rectangle;
			}
			if (timeSpanRenderDatum3.m_Select)
			{
				Rectangle rect2 = timeSpanRenderDatum3.m_Rect;
				int num22 = (int)(m_SelectedTimespanBorderPenWidth / 2f);
				rect2 = ((rect2.Width <= 2 * num22) ? new Rectangle(rect2.X, rect2.Y + num22, 1, rect2.Height - 2 * num22) : new Rectangle(rect2.X + num22, rect2.Y + num22, rect2.Width - 2 * num22, rect2.Height - 2 * num22));
				m_RendererRenderData.m_SelectedRects.m_Array[num18++] = rect2;
			}
		}
		if (num > m_IdealHeight)
		{
			m_IdealHeight = num;
			if (this.IdealHeightChanged != null)
			{
				this.IdealHeightChanged(this);
			}
		}
	}

	private Color GetTimeSpanColour(TimeSpan time_span)
	{
		int timeSpanInfoId = time_span.TimeSpanInfoId;
		Color value = Color.Orange;
		m_TimeSpanInfoToColourMap.TryGetValue(timeSpanInfoId, out value);
		return value;
	}

	private bool IsHighlighted(long time_span_name)
	{
		using (new ReadLockScope(m_HighlightedTimeSpansLock))
		{
			return m_HighlightedTimeSpans.Contains(time_span_name);
		}
	}

	private Rectangle GetTextRect(Rectangle time_span_rect)
	{
		Rectangle result = time_span_rect;
		result.Inflate(-(2 * ScaleDPI(2)), -1);
		return result;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private string GetTimeSpanRenderText(TimeSpanRenderData render_data, Graphics graphics, out Rectangle text_rect, ref Point text_pos, out bool clip_text)
	{
		Rectangle rect = render_data.m_Rect;
		text_rect = GetTextRect(rect);
		int num = Math.Max(0, text_rect.Left);
		int num2 = Math.Min(text_rect.Right, base.ClientSize.Width);
		text_rect = new Rectangle(num, text_rect.Y, num2 - num, text_rect.Height);
		clip_text = false;
		if (text_rect.Width <= ScaleDPI(15))
		{
			return null;
		}
		string result;
		int num6;
		if (render_data.m_IsHiResTimer)
		{
			string text = ((render_data.m_HiResTimer.m_Name != -1) ? m_Session.GetString(render_data.m_HiResTimer.m_Name) : "HiRes Misc");
			int num3 = (int)graphics.MeasureString(text, m_TimeSpanFont).Width;
			long duration = render_data.m_HiResTimer.m_Duration;
			string timeString = Utils.GetTimeString(duration, m_Session.TimerFrequency);
			int num4 = (int)(duration * 100 / render_data.m_TimeSpan.Duration);
			string text2 = text + "  " + timeString + "  (" + render_data.m_HiResTimer.m_Count + ") " + num4 + "%";
			int num5 = (int)graphics.MeasureString(text2, m_TimeSpanFont).Width;
			if (num5 < text_rect.Width)
			{
				result = text2;
				num6 = num5;
			}
			else
			{
				result = text;
				num6 = num3;
			}
		}
		else
		{
			TimeSpan timeSpan = render_data.m_TimeSpan;
			TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(timeSpan.TimeSpanInfoId);
			string timerName = m_Session.GetTimerName(timeSpanInfo.Name);
			int num7 = (int)graphics.MeasureString(timerName, m_TimeSpanFont).Width;
			string timeString2 = Utils.GetTimeString(timeSpan.Duration, m_Session.TimerFrequency);
			string text3 = timerName + "    " + timeString2;
			int num8 = (int)graphics.MeasureString(text3, m_TimeSpanFont).Width;
			if (num8 < text_rect.Width)
			{
				result = text3;
				num6 = num8;
			}
			else
			{
				result = timerName;
				num6 = num7;
			}
		}
		int num9 = Math.Max(text_rect.X, text_rect.X + (text_rect.Width - num6) / 2);
		int num10 = text_rect.Y + (text_rect.Height - m_TimeSpanFont.Height) / 2;
		text_pos = new Point(num9, num10);
		clip_text = num6 > text_rect.Width;
		return result;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private void FillScopeRects(Dictionary<Color, RenderData.ScopeRectArray> rect_arrays, Graphics graphics)
	{
		foreach (Color key in rect_arrays.Keys)
		{
			RenderData.ScopeRectArray scopeRectArray = rect_arrays[key];
			if (scopeRectArray.m_RectArray.m_Array.Length != 0)
			{
				if (!m_ScopeColourBrushes.TryGetValue(key, out var value))
				{
					value = new SolidBrush(key);
					m_ScopeColourBrushes[key] = value;
				}
				graphics.FillRectangles(value, scopeRectArray.m_RectArray.m_Array);
			}
		}
	}

	private void DrawScopeRects(Dictionary<Color, RenderData.ScopeRectArray> rect_arrays, Graphics graphics)
	{
		foreach (Color key in rect_arrays.Keys)
		{
			RenderData.ScopeRectArray scopeRectArray = rect_arrays[key];
			if (scopeRectArray.m_RectArray.m_Array.Length != 0)
			{
				graphics.DrawRectangles(m_TimeSpanBorderPen, scopeRectArray.m_RectArray.m_Array);
			}
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		try
		{
			e.Graphics.Clear(Colours.TimeSpanBackground);
			if (m_Session == null || !m_Session.IsReady || !m_ScrolledIntoView)
			{
				return;
			}
			DrawTimeLines(e.Graphics, m_Session.TimerFrequency / 1000000, ref m_TimeLinePen);
			DrawTimeLines(e.Graphics, m_Session.TimerFrequency / 10000, ref m_TimeLinePen);
			DrawTimeLines(e.Graphics, m_Session.TimerFrequency / 1000, ref m_TimeLinePen);
			if (m_RenderData.m_IdleTimeSpanRects != null && m_RenderData.m_IdleTimeSpanRects.Length != 0)
			{
				e.Graphics.FillRectangles(m_IdleTimeSpanBrush, m_RenderData.m_IdleTimeSpanRects.m_Array);
				e.Graphics.DrawRectangles(m_IdleTimeSpanBorderPen, m_RenderData.m_IdleTimeSpanRects.m_Array);
			}
			FillScopeRects(m_RenderData.m_ScopeColourTimeSpanRects, e.Graphics);
			FillScopeRects(m_RenderData.m_IdleScopeColourTimeSpanRects, e.Graphics);
			DrawScopeRects(m_RenderData.m_ScopeColourTimeSpanRects, e.Graphics);
			if (m_RenderData.m_HiResTimerRects != null)
			{
				foreach (ColouredRect hiResTimerRect in m_RenderData.m_HiResTimerRects)
				{
					ColourUtils.ColorToHSV(hiResTimerRect.m_Colour, out var hue, out var _, out var _);
					Color colour = ((hue > 245.0) ? Color.White : Color.Black);
					Color fore_colour = Utils.Lerp(hiResTimerRect.m_Colour, colour, 0.6f);
					HatchBrushManager hatchBrushManager = m_HatchBrushManager;
					Color colour2 = hiResTimerRect.m_Colour;
					Rectangle rect = hiResTimerRect.m_Rect;
					Brush brush = hatchBrushManager.GetBrush(colour2, fore_colour, rect.Location);
					e.Graphics.FillRectangle(brush, hiResTimerRect.m_Rect);
					e.Graphics.DrawRectangle(m_TimeSpanBorderPen, hiResTimerRect.m_Rect);
				}
			}
			if (m_RenderData.m_SelectedRects != null && m_RenderData.m_SelectedRects.Length != 0)
			{
				e.Graphics.DrawRectangles(m_SelectedTimespanBorderPen, m_RenderData.m_SelectedRects.m_Array);
				e.Graphics.DrawRectangles(m_SelectedTimespanBorderPenOutline, m_RenderData.m_SelectedRects.m_Array);
			}
			if (m_RenderData.m_HighlightRects != null && m_RenderData.m_HighlightRects.Length != 0)
			{
				e.Graphics.DrawRectangles(m_HighlightBorderPen, m_RenderData.m_HighlightRects.m_Array);
			}
			foreach (TimeSpanRenderData timeSpanRenderDatum in m_RenderData.m_TimeSpanRenderData)
			{
				if (timeSpanRenderDatum.m_Text != null)
				{
					if (timeSpanRenderDatum.m_ClipText)
					{
						e.Graphics.SetClip(GetTextRect(timeSpanRenderDatum.m_Rect));
					}
					e.Graphics.DrawString(timeSpanRenderDatum.m_Text, m_TimeSpanFont, m_TimeSpanTextBrush, timeSpanRenderDatum.m_TextPos);
					if (timeSpanRenderDatum.m_ClipText)
					{
						e.Graphics.ResetClip();
					}
				}
			}
			int num = -2;
			foreach (FrameRenderData frameRenderDatum in m_RenderData.m_FrameRenderData)
			{
				int num2 = frameRenderDatum.m_X;
				if (num2 > num + ScaleDPI(Timeline.MinFrameLineWidth))
				{
					e.Graphics.DrawLine(m_FramePen, num2, 0, num2, base.ClientSize.Height);
				}
				num = num2;
			}
			if (m_MouseHoverEnabled)
			{
				m_MeasureLine.Draw(e.Graphics, base.Height, m_MeasureLinePen, m_MeasureLineFillBrush);
			}
			DrawResizeLine(e.Graphics);
		}
		catch (Exception)
		{
		}
	}

	private void DrawResizeLine(Graphics graphics)
	{
		int num = ScaleDPI(3);
		graphics.FillRectangle(m_ResizeLineBrush, 0, base.Height - num, base.Width, num);
	}

	private void DrawTimeLines(Graphics graphics, long step_time, ref Pen pen)
	{
		State drawState = m_DrawState;
		int frameIndex = m_Session.GetFrameIndex(drawState.m_TimeRange.m_StartTime);
		Frame frame = m_Session.GetFrame(frameIndex);
		if (frame == null)
		{
			return;
		}
		if (TimeToX(frame.EndTime, drawState.m_TimeRange) < base.ClientSize.Width / 2 && frameIndex < m_Session.FrameCount - 1)
		{
			frame = m_Session.GetFrame(frameIndex + 1);
		}
		long num = frame.StartTime;
		int num2 = 0;
		while (num > drawState.m_TimeRange.m_StartTime - step_time)
		{
			num -= step_time;
			num2--;
		}
		while (num < drawState.m_TimeRange.m_StartTime - step_time)
		{
			num += step_time;
			num2++;
		}
		int num3 = TimeToX(num + step_time, drawState.m_TimeRange);
		float num4 = Misc.Clamp((float)(TimeToX(num + 2 * step_time, drawState.m_TimeRange) - num3 - (ScaleDPI(10) - 1)) / 100f, 0f, 1f);
		if (num4 > 0.1f)
		{
			Color color = Utils.Lerp(Colours.TimeSpanBackground, Colours.ThreadTimeLine, num4);
			if (pen == null || pen.Color != color)
			{
				pen = new Pen(color);
			}
			long num5 = base.ClientSize.Width * drawState.m_TimeRange.m_TicksPerPixel;
			long num6 = drawState.m_TimeRange.m_StartTime + num5 + step_time;
			_ = (num - frame.StartTime) / step_time;
			int y = base.ClientSize.Height;
			for (long num7 = num; num7 < num6; num7 += step_time)
			{
				int num8 = TimeToX(num7, drawState.m_TimeRange);
				graphics.DrawLine(pen, num8, 0, num8, y);
				num2++;
			}
		}
	}

	private Rectangle GetTimeSpanRect(TimeSpan time_span, int depth, TimeRange visible_time_range)
	{
		long num = Math.Max(time_span.Duration, 1L);
		long startTime = time_span.StartTime;
		long time = startTime + num;
		int dPIScaledTimeSpanHeight = GetDPIScaledTimeSpanHeight();
		int num2 = Math.Max(-1, TimeToX(startTime, visible_time_range));
		int num3 = Math.Min(TimeToX(time, visible_time_range) - num2, base.ClientSize.Width + 2);
		int num4 = depth * dPIScaledTimeSpanHeight - visible_time_range.m_ScrollY;
		return new Rectangle(num2, num4, num3, dPIScaledTimeSpanHeight);
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
		if (MainForm.Inst != null && MainForm.Inst.WindowState != FormWindowState.Minimized)
		{
			UpdateVScrollBar();
			RecalculateView(force: true);
		}
		base.OnResize(e);
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(e.Location);
			if (timeSpanRenderData.m_TimeSpan != null)
			{
				MainForm.Inst.ShowThreadsViewDataGrid(timeSpanRenderData.m_TimeSpan);
			}
		}
		base.OnMouseDoubleClick(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_ResizeHoverStarted)
			{
				m_ResizeHoverStarted = false;
				m_StartResizeScreenPos = PointToScreen(e.Location);
				m_StartResizeHeight = base.Parent.Parent.Height;
				m_Resizing = true;
				base.Capture = true;
				Cursor.Current = Cursors.SizeNS;
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
				m_StartDragMousePos = e.Location;
				m_LastDragMouseScreenPos = PointToScreen(e.Location);
				m_StartDragTime = m_State.m_TimeRange.m_StartTime;
				m_MouseDragMode = MouseDragMode.Horizontal;
				m_MaxHorzDragOffset = 0;
				if (MainForm.Inst != null)
				{
					MainForm.Inst.HoverBox.Hide();
				}
			}
			this.HighlightSingleScope(null);
		}
		else if (e.Button == MouseButtons.Right)
		{
			TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(e.Location);
			if (timeSpanRenderData.m_TimeSpan != null)
			{
				m_ContextMenuTimeSpan = timeSpanRenderData.m_TimeSpan;
				m_ContextMenuFrameIndex = m_Session.GetFrameIndex(XToTime(e.X, m_DrawState.m_TimeRange));
				m_ScopeContextMenu.Show(PointToScreen(e.Location));
				this.HighlightSingleScope(timeSpanRenderData.m_TimeSpan);
			}
			else
			{
				m_ContextMenuTimeSpan = null;
				m_ContextMenu.Show(PointToScreen(e.Location));
				this.HighlightSingleScope(null);
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		StopResizeHover();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_Resizing)
		{
			Point point = PointToClient(m_StartResizeScreenPos);
			int num = e.Location.Y - point.Y;
			base.Parent.Parent.Size = new Size(base.Parent.Parent.Width, m_StartResizeHeight + num);
			if (this.UserResizedHeight != null)
			{
				this.UserResizedHeight(this);
			}
		}
		else if (m_StartedDragMove)
		{
			int num2 = ScaleDPI(20);
			if (m_MouseDragMode == MouseDragMode.Horizontal && m_MaxHorzDragOffset < num2 && Math.Abs(e.Y - m_StartDragMousePos.Y) > num2)
			{
				m_MouseDragMode = MouseDragMode.Vertical;
			}
			switch (m_MouseDragMode)
			{
			case MouseDragMode.Horizontal:
			{
				int value = e.X - m_StartDragMousePos.X;
				m_MaxHorzDragOffset = Math.Max(m_MaxHorzDragOffset, Math.Abs(value));
				long num4 = XToTime(value, m_State.m_TimeRange) - m_State.m_TimeRange.m_StartTime;
				long startTime = Math.Max(m_Session.FirstFrameTime, m_StartDragTime - num4);
				m_State.m_TimeRange.m_StartTime = startTime;
				OnTimeRangeChanged();
				RecalculateView();
				break;
			}
			case MouseDragMode.Vertical:
			{
				Point lastDragMouseScreenPos = PointToScreen(e.Location);
				Point point2 = PointToClient(m_LastDragMouseScreenPos);
				int num3 = e.Location.Y - point2.Y;
				if (this.ScrollY != null)
				{
					this.ScrollY(-num3);
				}
				m_LastDragMouseScreenPos = lastDragMouseScreenPos;
				break;
			}
			}
		}
		else if (e.Y >= base.Height - ScaleDPI(3))
		{
			StartResizeHover();
		}
		else
		{
			StopResizeHover();
			if (m_MouseHoverEnabled)
			{
				UpdateHoverBox(e.Location);
			}
		}
		m_MeasureLine.HandleMouseMove(e.X);
		if (m_ResizeHoverStarted || m_Resizing)
		{
			Cursor.Current = Cursors.SizeNS;
		}
		m_CurrentMouseTime = XToTime(e.X, m_State.m_TimeRange);
		base.OnMouseMove(e);
	}

	private void UpdateHoverBox(Point location)
	{
		if (MainForm.Inst == null)
		{
			return;
		}
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		if (Utils.IsShiftHeld)
		{
			long start_time = XToTime(m_MeasureLine.StartX, m_State.m_TimeRange);
			long end_time = XToTime(m_MeasureLine.EndX, m_State.m_TimeRange);
			MeasureLine.UpdateTimerInfoBox(start_time, end_time, m_Session.TimerFrequency, PointToScreen(location));
			return;
		}
		TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(location);
		if (timeSpanRenderData.m_TimeSpan != null && !m_StartedDragMove)
		{
			TimeSpan timeSpan = timeSpanRenderData.m_TimeSpan;
			string text = timeSpanRenderData.m_Rect.ToString();
			if (!(hoverBox.Target is string) || (string)hoverBox.Target != text)
			{
				hoverBox.Target = text;
				hoverBox.Clear();
				if (timeSpanRenderData.m_IsHiResTimer)
				{
					Utils.AddTimerInfoBoxLines(hoverBox, timeSpanRenderData.m_HiResTimer, timeSpanRenderData.m_TimeSpan.Duration, m_Session);
				}
				else
				{
					int frameIndex = m_Session.GetFrameIndex(XToTime(location.X, m_DrawState.m_TimeRange));
					Utils.AddTimerInfoBoxLines(hoverBox, timeSpan, frameIndex, m_Session);
					hoverBox.AddLine("End Core ", m_Session.GetTimeSpanInfo(timeSpan.TimeSpanInfoId).Core.ToString());
				}
				hoverBox.SubmitLines();
			}
			hoverBox.Visible = true;
			Point location2 = PointToScreen(location);
			hoverBox.SetLocation(location2);
		}
		else
		{
			hoverBox.Visible = false;
		}
	}

	public void SetMeasureLine(int start_x, int end_x)
	{
		m_MeasureLine.Set(start_x, end_x);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_Resizing)
			{
				m_Resizing = false;
				base.Capture = false;
				Cursor.Current = Cursors.Default;
			}
			else if (m_MeasureLine.Dragging)
			{
				m_MeasureLine.Dragging = false;
				base.Capture = false;
				long num = XToTime(m_MeasureLine.StartX, m_State.m_TimeRange);
				long time = XToTime(m_MeasureLine.EndX, m_State.m_TimeRange) - num;
				m_DurationContextMenuItem.Text = "Duration: " + Utils.GetTimeString(time, m_Session.TimerFrequency);
				m_MeasureContextMenu.Show(PointToScreen(e.Location));
			}
			else if (m_StartedDragMove)
			{
				base.Capture = false;
				m_StartedDragMove = false;
				if (e.Location == m_StartDragMousePos)
				{
					TimeSpanRenderData timeSpanRenderData = GetTimeSpanRenderData(e.Location);
					if (timeSpanRenderData.m_TimeSpan != null)
					{
						if (timeSpanRenderData.m_IsHiResTimer)
						{
							if (timeSpanRenderData.m_HiResTimer.m_Name != -1)
							{
								SelectTimeSpan(timeSpanRenderData.m_HiResTimer.m_Name, null);
							}
							else
							{
								SelectTimeSpan(-1L, null);
							}
							OnSelectedTimeSpanChanged();
						}
						else
						{
							SelectTimeSpan(m_Session.GetTimeSpanInfo(timeSpanRenderData.m_TimeSpan.TimeSpanInfoId).Name, timeSpanRenderData.m_TimeSpan);
							OnSelectedTimeSpanChanged();
						}
					}
					else
					{
						SelectTimeSpan(-1L, null);
						OnSelectedTimeSpanChanged();
					}
				}
			}
		}
		base.OnMouseUp(e);
	}

	public void SelectTimeSpan(long time_span_name, TimeSpan time_span)
	{
		if (m_SelectedTimeSpanName != time_span_name || m_SelectedTimeSpan != time_span)
		{
			m_SelectedTimeSpanName = time_span_name;
			m_SelectedTimeSpan = time_span;
			RecalculateView(force: true);
		}
	}

	private void OnSelectedTimeSpanChanged()
	{
		if (this.SelectedTimeSpanChanged != null)
		{
			this.SelectedTimeSpanChanged(m_SelectedTimeSpanName, m_SelectedTimeSpan);
		}
	}

	private static Color GetThreadIdleColour(Color thread_colour)
	{
		return Utils.Lerp(thread_colour, Colours.TimeSpanBackground, Colours.TimeSpanIdleTransparency);
	}

	private void OnTimeRangeChanged()
	{
		if (this.TimeRangeChanged != null)
		{
			this.TimeRangeChanged(this, m_State.m_TimeRange);
		}
	}

	private TimeSpanRenderData GetTimeSpanRenderData(Point location)
	{
		foreach (TimeSpanRenderData timeSpanRenderDatum in m_RenderData.m_TimeSpanRenderData)
		{
			Rectangle rect = timeSpanRenderDatum.m_Rect;
			if (rect.Contains(location))
			{
				return timeSpanRenderDatum;
			}
		}
		return default(TimeSpanRenderData);
	}

	public void RecalculateView(bool force)
	{
		if (force)
		{
			m_State.m_ForceUpdateId++;
		}
		RecalculateView();
	}

	private void RecalculateView()
	{
		if (m_Session != null && m_Session.IsReady && Active && m_ScrolledIntoView)
		{
			lock (m_NewCalcThreadState)
			{
				m_NewCalcThreadState.Copy(m_State);
			}
			m_CalculateViewThreadWakeEvent.Set();
		}
	}

	public void SetTimeRange(TimeRange time_range)
	{
		State state = m_State;
		if (state.m_TimeRange.Equals(time_range))
		{
			return;
		}
		lock (state.m_TimeRange)
		{
			long startTime = time_range.m_StartTime;
			if (time_range.m_Scale == state.m_TimeRange.m_Scale && state.m_TimeRange.m_TicksPerPixel != 0L)
			{
				int num = TimeToX_NoClamp(time_range.m_StartTime, state.m_TimeRange);
				startTime = XToTime(num, state.m_TimeRange);
			}
			state.m_TimeRange.CopyAllExceptScrollY(time_range);
			state.m_TimeRange.m_StartTime = startTime;
		}
		RecalculateView();
	}

	private void VScrollBarScroll(object sender, ScrollEventArgs e)
	{
		m_State.m_TimeRange.m_ScrollY = m_VScrollBar.Value;
		RecalculateView();
	}

	private void CheckIsMainThread()
	{
	}

	public void SetHighlightedTimeSpans(Set<long> time_span_names)
	{
		using (new WriteLockScope(m_HighlightedTimeSpansLock))
		{
			m_HighlightedTimeSpans = new Set<long>(time_span_names);
		}
		RecalculateView(force: true);
	}

	public void SetSingleHighlightedTimeSpan(TimeSpan time_span)
	{
		m_SingleHighlightedTimeSpan = time_span;
		RecalculateView(force: true);
	}

	private void SelectMenuItemClicked(object sender, EventArgs e)
	{
		SelectTimeSpan(m_Session.GetTimeSpanInfo(m_ContextMenuTimeSpan.TimeSpanInfoId).Name, null);
		OnSelectedTimeSpanChanged();
	}

	private void JumpToSourceCodeMenuItemClick(object sender, EventArgs e)
	{
		Utils.JumpToSourceCode(m_ContextMenuTimeSpan, m_Session, m_Settings);
	}

	private void CopyMenuItemClick(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpan != null)
		{
			TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(m_ContextMenuTimeSpan.TimeSpanInfoId);
			string timerName = m_Session.GetTimerName(timeSpanInfo.Name);
			string timeString = Utils.GetTimeString(m_ContextMenuTimeSpan.Duration, m_Session.TimerFrequency);
			string text = timeSpanInfo.Core.ToString();
			SourceInfoStruct sourceInfo = m_Session.GetSourceInfo(timeSpanInfo.SourceInfo);
			string text2 = "";
			text2 = text2 + "Name: " + timerName + "\r\n";
			text2 = text2 + "Duration: " + timeString + "\r\n";
			m_Session.GetTimeSpanFrameTime(timeSpanInfo.Name, m_ContextMenuFrameIndex, out var duration, out var count, out var max_duration, out var max_time_per_frame, out var max_count_per_frame);
			string timeString2 = Utils.GetTimeString(duration, m_Session.TimerFrequency);
			string timeString3 = Utils.GetTimeString(max_duration, m_Session.TimerFrequency);
			string timeString4 = Utils.GetTimeString(max_time_per_frame, m_Session.TimerFrequency);
			text2 = text2 + "Total time this frame: " + timeString2 + "\r\n";
			text2 = text2 + "Total count this frame: " + count + "\r\n";
			text2 = text2 + "Session Max: " + timeString3 + "\r\n";
			text2 = text2 + "Max time/frame: " + timeString4 + "\r\n";
			text2 = text2 + "Max count/frame: " + max_count_per_frame + "\r\n";
			text2 = text2 + "CPU: " + text + "\r\n";
			if (sourceInfo.Function != "")
			{
				text2 = text2 + "Function: " + sourceInfo.Function + "\r\n";
			}
			text2 = text2 + "Filename: " + sourceInfo.Filename + "\r\n";
			text2 = text2 + "Line: " + sourceInfo.Line + "\r\n";
			try
			{
				Clipboard.SetText(text2);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Copy Failed: " + ex.Message);
			}
		}
	}

	private void ZoomToTimeRange(long start_time, long end_time)
	{
		Utils.SetTimeRange(start_time, end_time, base.ClientSize.Width, m_Session.TimerFrequency, m_State.m_TimeRange);
		OnTimeRangeChanged();
		RecalculateView();
	}

	private void ZoomToScopeMenuItem(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpan != null)
		{
			ZoomToTimeRange(m_ContextMenuTimeSpan.StartTime, m_ContextMenuTimeSpan.EndTime);
		}
	}

	private void MeasureZoomMenuItem(object sender, EventArgs e)
	{
		long start_time = XToTime(m_MeasureLine.StartX, m_State.m_TimeRange);
		long end_time = XToTime(m_MeasureLine.EndX, m_State.m_TimeRange);
		ZoomToTimeRange(start_time, end_time);
	}

	private void HighlightMenuItemClick(object sender, EventArgs e)
	{
		HighlightTimeSpan(m_ContextMenuTimeSpan);
	}

	private void HighlightTimeSpan(TimeSpan time_span)
	{
		if (this.HighlightScope != null)
		{
			TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(time_span.TimeSpanInfoId);
			this.HighlightScope(timeSpanInfo.Name);
		}
	}

	private void HighlightSingleTimeSpan(TimeSpan time_span)
	{
		if (this.HighlightSingleScope != null)
		{
			this.HighlightSingleScope(time_span);
		}
	}

	private void FindPrevScopeMenuItemClicked(object sender, EventArgs e)
	{
		List<int> threadIds = GetThreadIds();
		TimeSpan timeSpan = null;
		long time = ((m_ContextMenuTimeSpan != null) ? (m_ContextMenuTimeSpan.StartTime - 1) : m_CurrentMouseTime);
		foreach (int item in threadIds)
		{
			TimeSpan timeSpan2 = m_Session.FindPrevTimeSpan(time, item, m_ContextMenuTimeSpan);
			if (timeSpan == null || timeSpan2.StartTime > timeSpan.StartTime)
			{
				timeSpan = timeSpan2;
			}
		}
		if (timeSpan != null)
		{
			_ = timeSpan.StartTime;
			Math.Max(100L, timeSpan.Duration);
			ScrollTimeSpanIntoView(timeSpan);
			HighlightSingleTimeSpan(timeSpan);
		}
		else
		{
			MessageBox.Show("Could not find any scopes", "ProfilerStudy", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private void FindNextScopeMenuItemClicked(object sender, EventArgs e)
	{
		List<int> threadIds = GetThreadIds();
		TimeSpan timeSpan = null;
		long time = ((m_ContextMenuTimeSpan != null) ? m_ContextMenuTimeSpan.EndTime : m_CurrentMouseTime);
		foreach (int item in threadIds)
		{
			TimeSpan timeSpan2 = m_Session.FindNextTimeSpan(time, item, m_ContextMenuTimeSpan);
			if (timeSpan == null || timeSpan2.StartTime < timeSpan.StartTime)
			{
				timeSpan = timeSpan2;
			}
		}
		if (timeSpan != null)
		{
			_ = timeSpan.StartTime;
			Math.Max(100L, timeSpan.Duration);
			ScrollTimeSpanIntoView(timeSpan);
			HighlightSingleTimeSpan(timeSpan);
		}
		else
		{
			MessageBox.Show("Could not find any scopes", "ProfilerStudy", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private void ScrollTimeSpanIntoView(TimeSpan time_span)
	{
		long num = XToTime(0, m_State.m_TimeRange);
		long num2 = XToTime(base.ClientSize.Width, m_State.m_TimeRange);
		long num3 = num2 - num;
		if (time_span.StartTime < num)
		{
			long startTime = time_span.StartTime;
			long end_time = startTime + num3;
			ZoomToTimeRange(startTime, end_time);
		}
		else if (time_span.EndTime > num2)
		{
			long endTime = time_span.EndTime;
			long start_time = endTime - num3;
			ZoomToTimeRange(start_time, endTime);
		}
	}

	public void SetScopeColourMode(ScopeColourMode scope_colour_mode)
	{
		m_State.m_ScopeColourMode = scope_colour_mode;
		RecalculateView();
	}

	public void OnScopeColourChanged()
	{
		m_RenderData.m_ClearScopeColoursMap = true;
		RecalculateView(force: true);
	}

	private void SetScopeColourMenuItemClick(object sender, EventArgs e)
	{
		ColorDialog colorDialog = new ColorDialog();
		if (colorDialog.ShowDialog(this) == DialogResult.OK)
		{
			TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(m_ContextMenuTimeSpan.TimeSpanInfoId);
			string timerName = m_Session.GetTimerName(timeSpanInfo.Name);
			m_Session.SetScopeColour(timerName, colorDialog.Color);
		}
	}

	private void ScopeGoToMaxClicked(object sender, EventArgs e)
	{
		if (m_ContextMenuTimeSpan == null || m_ContextMenuTimeSpan.TimeSpanInfoId == TimeSpanInfo.InvalidInfoId)
		{
			return;
		}
		TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(m_ContextMenuTimeSpan.TimeSpanInfoId);
		foreach (int threadId in GetThreadIds())
		{
			if (m_Session.GetTimeSpanMax(timeSpanInfo.Name, threadId, out var start_time) != 0L)
			{
				TimeSpan timeSpan = m_Session.GetTimeSpan(timeSpanInfo.Name, start_time);
				if (timeSpan != null)
				{
					ZoomToTimeRange(timeSpan.StartTime, timeSpan.EndTime);
				}
				break;
			}
		}
	}

	private void StartResizeHover()
	{
		if (!m_ResizeHoverTimerStarted && !m_ResizeHoverStarted)
		{
			m_ResizeHoverTimerStarted = true;
			m_ResizeHoverTimer.Start();
		}
	}

	private void StopResizeHover()
	{
		if (m_ResizeHoverTimerStarted)
		{
			m_ResizeHoverTimerStarted = false;
			m_ResizeHoverTimer.Stop();
		}
		if (m_ResizeHoverStarted)
		{
			m_ResizeHoverStarted = false;
			Cursor.Current = Cursors.Default;
		}
	}

	private void ResizeHoverTimerTick(object sender, EventArgs e)
	{
		m_ResizeHoverTimer.Stop();
		m_ResizeHoverTimerStarted = false;
		m_ResizeHoverStarted = true;
		Cursor.Current = Cursors.SizeNS;
	}

	private static Color GetIdleColour(Color colour)
	{
		return Color.FromArgb(colour.A, (int)((float)(int)colour.R * Colours.TimeSpanIdleBrightness), (int)((float)(int)colour.G * Colours.TimeSpanIdleBrightness), (int)((float)(int)colour.B * Colours.TimeSpanIdleBrightness));
	}

	private int GetDPIScaledTimeSpanHeight()
	{
		return (int)((float)m_TimeSpanHeight * m_DPIScale);
	}

	private void UpdateTimeSpanFont()
	{
		float emSize = (float)GetDPIScaledTimeSpanHeight() * 0.5f;
		m_TimeSpanFont = new Font("Monaco", emSize, GraphicsUnit.Pixel);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if ((Control.ModifierKeys & Keys.Control) != 0)
		{
			int num = ((e.Delta < 0) ? Math.Max(8, TimeSpanHeight - 1) : Math.Min(TimeSpanHeight + 1, 60));
			if (TimeSpanHeight != num)
			{
				TimeSpanHeight = num;
				m_Settings.ThreadScopeHeight = TimeSpanHeight;
				m_Settings.Write();
				if (this.TimeSpanHeightChanged != null)
				{
					this.TimeSpanHeightChanged();
				}
			}
		}
		base.OnMouseWheel(e);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.m_VScrollBar = new System.Windows.Forms.VScrollBar();
		this.m_ScopeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.selectToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.highlightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.jumpToSourceCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.goToMaxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.zoomToScopeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.findPrevScopeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.jumpToNextScopeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.m_SetScopeColourMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_MeasureContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.m_DurationContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.findPrevScopeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.jumpToNextScopeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ScopeContextMenu.SuspendLayout();
		this.m_MeasureContextMenu.SuspendLayout();
		this.m_ContextMenu.SuspendLayout();
		base.SuspendLayout();
		this.m_VScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_VScrollBar.Location = new System.Drawing.Point(1044, 0);
		this.m_VScrollBar.Name = "m_VScrollBar";
		this.m_VScrollBar.Size = new System.Drawing.Size(13, 178);
		this.m_VScrollBar.TabIndex = 0;
		this.m_VScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(VScrollBarScroll);
		this.m_ScopeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[9] { this.selectToolStripMenuItem1, this.highlightToolStripMenuItem, this.jumpToSourceCodeToolStripMenuItem, this.goToMaxToolStripMenuItem, this.copyToolStripMenuItem, this.zoomToScopeToolStripMenuItem, this.findPrevScopeToolStripMenuItem, this.jumpToNextScopeToolStripMenuItem1, this.m_SetScopeColourMenuItem });
		this.m_ScopeContextMenu.Name = "m_ContextMenu";
		this.m_ScopeContextMenu.Size = new System.Drawing.Size(156, 202);
		this.selectToolStripMenuItem1.Name = "selectToolStripMenuItem1";
		this.selectToolStripMenuItem1.Size = new System.Drawing.Size(155, 22);
		this.selectToolStripMenuItem1.Text = "Select";
		this.selectToolStripMenuItem1.Click += new System.EventHandler(SelectMenuItemClicked);
		this.highlightToolStripMenuItem.Name = "highlightToolStripMenuItem";
		this.highlightToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.highlightToolStripMenuItem.Text = "Highlight";
		this.highlightToolStripMenuItem.Click += new System.EventHandler(HighlightMenuItemClick);
		this.jumpToSourceCodeToolStripMenuItem.Name = "jumpToSourceCodeToolStripMenuItem";
		this.jumpToSourceCodeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.jumpToSourceCodeToolStripMenuItem.Text = "Go to Source";
		this.jumpToSourceCodeToolStripMenuItem.Click += new System.EventHandler(JumpToSourceCodeMenuItemClick);
		this.goToMaxToolStripMenuItem.Name = "goToMaxToolStripMenuItem";
		this.goToMaxToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.goToMaxToolStripMenuItem.Text = "Go to Max";
		this.goToMaxToolStripMenuItem.Click += new System.EventHandler(ScopeGoToMaxClicked);
		this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
		this.copyToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.copyToolStripMenuItem.Text = "Copy";
		this.copyToolStripMenuItem.Click += new System.EventHandler(CopyMenuItemClick);
		this.zoomToScopeToolStripMenuItem.Name = "zoomToScopeToolStripMenuItem";
		this.zoomToScopeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.zoomToScopeToolStripMenuItem.Text = "Zoom to Scope";
		this.zoomToScopeToolStripMenuItem.Click += new System.EventHandler(ZoomToScopeMenuItem);
		this.findPrevScopeToolStripMenuItem.Name = "findPrevScopeToolStripMenuItem";
		this.findPrevScopeToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
		this.findPrevScopeToolStripMenuItem.Text = "Find Prev";
		this.findPrevScopeToolStripMenuItem.Click += new System.EventHandler(FindPrevScopeMenuItemClicked);
		this.jumpToNextScopeToolStripMenuItem1.Name = "jumpToNextScopeToolStripMenuItem1";
		this.jumpToNextScopeToolStripMenuItem1.Size = new System.Drawing.Size(155, 22);
		this.jumpToNextScopeToolStripMenuItem1.Text = "Find Next";
		this.jumpToNextScopeToolStripMenuItem1.Click += new System.EventHandler(FindNextScopeMenuItemClicked);
		this.m_SetScopeColourMenuItem.Name = "m_SetScopeColourMenuItem";
		this.m_SetScopeColourMenuItem.Size = new System.Drawing.Size(155, 22);
		this.m_SetScopeColourMenuItem.Text = "Set Colour";
		this.m_SetScopeColourMenuItem.Click += new System.EventHandler(SetScopeColourMenuItemClick);
		this.m_MeasureContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.m_DurationContextMenuItem, this.toolStripMenuItem2 });
		this.m_MeasureContextMenu.Name = "m_ContextMenu";
		this.m_MeasureContextMenu.Size = new System.Drawing.Size(149, 48);
		this.m_DurationContextMenuItem.Name = "m_DurationContextMenuItem";
		this.m_DurationContextMenuItem.Size = new System.Drawing.Size(148, 22);
		this.m_DurationContextMenuItem.Text = "Duration: 0ms";
		this.toolStripMenuItem2.Name = "toolStripMenuItem2";
		this.toolStripMenuItem2.Size = new System.Drawing.Size(148, 22);
		this.toolStripMenuItem2.Text = "Zoom";
		this.toolStripMenuItem2.Click += new System.EventHandler(MeasureZoomMenuItem);
		this.m_ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.findPrevScopeToolStripMenuItem1, this.jumpToNextScopeToolStripMenuItem });
		this.m_ContextMenu.Name = "m_ContextMenu";
		this.m_ContextMenu.Size = new System.Drawing.Size(161, 48);
		this.findPrevScopeToolStripMenuItem1.Name = "findPrevScopeToolStripMenuItem1";
		this.findPrevScopeToolStripMenuItem1.Size = new System.Drawing.Size(160, 22);
		this.findPrevScopeToolStripMenuItem1.Text = "Find Prev Scope";
		this.findPrevScopeToolStripMenuItem1.Click += new System.EventHandler(FindPrevScopeMenuItemClicked);
		this.jumpToNextScopeToolStripMenuItem.Name = "jumpToNextScopeToolStripMenuItem";
		this.jumpToNextScopeToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
		this.jumpToNextScopeToolStripMenuItem.Text = "Find Next Scope";
		this.jumpToNextScopeToolStripMenuItem.Click += new System.EventHandler(FindNextScopeMenuItemClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_VScrollBar);
		this.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "TimeSpanGraph";
		base.Size = new System.Drawing.Size(1057, 178);
		this.m_ScopeContextMenu.ResumeLayout(false);
		this.m_MeasureContextMenu.ResumeLayout(false);
		this.m_ContextMenu.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
