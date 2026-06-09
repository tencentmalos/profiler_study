using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ProfilerStudy.Properties;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class FrameGraphPanel : UserControl
{
	private struct FrameRenderData
	{
		public Frame m_Frame;

		public Rectangle m_Rect;

		public long m_Duration;
	}

	private struct EventDrawData
	{
		public Event m_Event;

		public int m_X;
	}

	private class RenderData
	{
		public int m_LastFrameIndex;

		public List<FrameStruct> m_Frames = new List<FrameStruct>();

		public List<FrameRenderData> m_FrameRenderData = new List<FrameRenderData>();

		public List<FrameTimeSpanStruct> m_TimeSpanFrames = new List<FrameTimeSpanStruct>();

		public List<FrameRenderData> m_TimeSpanRenderData = new List<FrameRenderData>();

		public List<EventDrawData> m_Events = new List<EventDrawData>();
	}

	private class Range
	{
		private long m_StartFrameX;

		private double m_Scale;

		private double m_AlignedScale;

		private Size m_ClientSize;

		private double m_YScale;

		private int m_ForceUpdateId;

		public long StartFrameX
		{
			get
			{
				return m_StartFrameX;
			}
			set
			{
				m_StartFrameX = value;
			}
		}

		public double Scale
		{
			get
			{
				return m_Scale;
			}
			set
			{
				if (m_Scale != value)
				{
					m_Scale = Misc.Clamp(value, 1.0, 1000.0);
					m_AlignedScale = ((m_Scale > 1.0) ? Math.Round(m_Scale) : m_Scale);
					m_AlignedScale = Math.Max(1.0, m_AlignedScale);
				}
			}
		}

		public double AlignedScale => m_AlignedScale;

		public Size ClientSize
		{
			get
			{
				return m_ClientSize;
			}
			set
			{
				m_ClientSize = value;
			}
		}

		public double YScale
		{
			get
			{
				return m_YScale;
			}
			set
			{
				m_YScale = value;
			}
		}

		public int ForceUpdateId
		{
			get
			{
				return m_ForceUpdateId;
			}
			set
			{
				m_ForceUpdateId = value;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is Range range && m_StartFrameX == range.m_StartFrameX && m_Scale == range.m_Scale && m_AlignedScale == range.m_AlignedScale && m_ClientSize == range.m_ClientSize && m_YScale == range.m_YScale)
			{
				return ForceUpdateId == range.ForceUpdateId;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public void Copy(Range other)
		{
			m_StartFrameX = other.m_StartFrameX;
			m_Scale = other.m_Scale;
			m_AlignedScale = other.m_AlignedScale;
			m_ClientSize = other.m_ClientSize;
			m_YScale = other.m_YScale;
			ForceUpdateId = other.ForceUpdateId;
		}
	}

	private enum DragMode
	{
		None,
		Move,
		VisibleRange,
		SelectedRange,
		MoveSelection
	}

	private Session m_Session;

	private Brush m_GraphBrush = new SolidBrush(Colours.FrameGraphFrameBar);

	private Brush m_GraphBrushWarning = new SolidBrush(Colours.FrameGraphFrameBarWarning);

	private Brush m_GraphBrushAlert = new SolidBrush(Colours.FrameGraphFrameBarAlert);

	public const double DefaultScale = 10.0;

	private int m_DragMoveX;

	private long m_StartDragFrameX;

	private long m_VisibleStartTime;

	private long m_VisibleEndTime;

	private Brush m_VisibleRangeFillBrush = new SolidBrush(Colours.FrameGraphVisibleRangeFill);

	private Pen m_VisibleRangeLineBrush = new Pen(Colours.FrameGraphVisibleRangeLine);

	private Pen m_TargetMSPen = new Pen(Colours.FrameGraphTargetLine);

	private Brush m_SelectionFillBrush = new SolidBrush(Colours.FrameGraphSelectionFill);

	private Pen m_SelectionLineBrush = new Pen(Colours.FrameGraphSelectionLine);

	private DragMode m_DragMode;

	private double m_TargetFrameMS;

	private RenderData m_RenderData = new RenderData();

	private RenderData m_RendererRenderData = new RenderData();

	private long m_DragSelectionStartTime;

	private long m_DragSelectionEndTime;

	private bool m_ShowingTimeSpans;

	private long m_TimeSpanName = -1L;

	private Brush m_TimeSpanFrameBrush = new SolidBrush(Colours.FrameGraphTimeSpanBar);

	private Brush m_TimeSpanBrush = Brushes.White;

	private Brush m_TimeSpanOverTargetBrush = Brushes.Red;

	private Settings m_Settings;

	private Thread m_CalculateViewThread;

	private volatile bool m_ExitCalculateViewThread;

	private AutoResetEvent m_CalculateViewThreadWakeEvent = new AutoResetEvent(initialState: false);

	private AutoResetEvent m_CalculateViewOnCompleteFinished = new AutoResetEvent(initialState: false);

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private Range m_VisibleRange = new Range();

	private Range m_RendererVisibleRange = new Range();

	private int m_MainThreadId;

	private bool m_ShiftHeld;

	private bool m_IsSessionReady;

	private bool m_ShowProcessingDataMessage;

	private Font m_ProcessingDataFont = new Font("Monaco", 16f, FontStyle.Regular, GraphicsUnit.Point, 0);

	private Brush m_ProcessingDataBrush = new SolidBrush(Color.White);

	private System.Windows.Forms.Timer m_DrawProcessingDataTimer = new System.Windows.Forms.Timer();

	private int m_FirstProcessedPacketTime;

	private int m_LastProcessedPacketTime;

	private BrushSet m_BrushSet = new BrushSet();

	private const int m_EventY = 10;

	private const int m_EventDiamondSize = 10;

	private const int m_EventTextGap = 5;

	private bool m_ShowEvents;

	private List<Event> m_Events = new List<Event>();

	private Font m_EventFont = new Font("Monaco", 8f, FontStyle.Regular, GraphicsUnit.Point, 0);

	private Brush m_EventLableBrush = new SolidBrush(Colours.FrameGraphEventLabelBackColour);

	private Brush m_EventTextBrush = new SolidBrush(Colours.FrameGraphEventLabelForeColour);

	private Pen m_EventLabelBorderPen = new Pen(Colours.FrameGraphEventLabelBorderColour);

	private const int m_EventLabelInflate = 2;

	private bool m_Active;

	private int m_SelectedRangeStartFrame = -1;

	private int m_SelectedRangeEndFrame = -1;

	private float m_ScaleDPI;

	private IContainer components;

	private ContextMenuStrip m_VisibleRangeContextMenu;

	private ToolStripMenuItem zoomToolStripMenuItem;

	private ContextMenuStrip m_SelectedRangeContextMenu;

	private ToolStripMenuItem clearSelectionToolStripMenuItem;

	private ToolStripMenuItem createSessionFromSelectionToolStripMenuItem1;

	private bool NeedsToRecalculate => !m_RendererVisibleRange.Equals(m_VisibleRange);

	public bool NeedsUpdate => m_RenderData.m_LastFrameIndex < XToFrameX(base.Width, m_VisibleRange) / Session.FrameXPerFrame;

	public long FrameXWidth => XToFrameX(base.Width, m_VisibleRange);

	public bool ShowingTimeSpans
	{
		get
		{
			return m_ShowingTimeSpans;
		}
		set
		{
			m_ShowingTimeSpans = value;
		}
	}

	public long TimeSpanName => m_TimeSpanName;

	public long VisibleStartTime => m_VisibleStartTime;

	public long VisibleEndTime => m_VisibleEndTime;

	public double YScale
	{
		get
		{
			return m_VisibleRange.YScale;
		}
		set
		{
			m_VisibleRange.YScale = value;
			if (m_IsSessionReady)
			{
				OnRangeChanged();
				RecalculateView();
			}
		}
	}

	public bool ShowEvents
	{
		get
		{
			return m_ShowEvents;
		}
		set
		{
			m_ShowEvents = value;
		}
	}

	public double TargetFrameMS
	{
		get
		{
			return m_TargetFrameMS;
		}
		set
		{
			if (m_TargetFrameMS != value)
			{
				m_TargetFrameMS = value;
				Refresh();
			}
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

	public event VisibleRangeChangedHandler VisibleRangeChanged;

	public event RangeChangedHandler RangeChanged;

	public event SelectedRangeChangedHandler SelectedRangeChanged;

	public FrameGraphPanel()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
		m_ScaleDPI = MainForm.DPIScale;
		m_VisibleRange.Scale = 10.0 * (double)m_ScaleDPI;
		m_VisibleRange.ClientSize = base.ClientSize;
		m_CalculateViewThread = new Thread(CalculateViewThread);
		m_CalculateViewThread.Name = "FrameGraphPanel";
		m_CalculateViewThread.Start();
		RecalculateView();
		m_DrawProcessingDataTimer.Interval = 1000;
		m_DrawProcessingDataTimer.Tick += DrawProcessingDataTimerTick;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if ((e.Modifiers & Keys.Shift) == Keys.Shift && !m_ShiftHeld)
		{
			m_ShiftHeld = true;
			UpdateFrameInfoBox(Cursor.Position);
		}
		base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		if ((e.Modifiers & Keys.Shift) == 0 && m_ShiftHeld)
		{
			m_ShiftHeld = false;
			UpdateFrameInfoBox(Cursor.Position);
		}
		base.OnKeyUp(e);
	}

	private void CalculateViewThread()
	{
		while (!m_ExitCalculateViewThread)
		{
			if (!NeedsToRecalculate)
			{
				m_CalculateViewThreadWakeEvent.WaitOne();
			}
			if (NeedsToRecalculate)
			{
				lock (m_VisibleRange)
				{
					m_RendererVisibleRange.Copy(m_VisibleRange);
				}
				if (m_Session != null && m_Session.TimerFrequency != 0L)
				{
					CalculateView();
				}
				m_ControlTaskDispatcher.QueueTask(OnCalculateViewComplete);
				m_CalculateViewOnCompleteFinished.WaitOne();
			}
		}
	}

	private void CheckIsMainThread()
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_BrushSet.Dispose();
		m_ExitCalculateViewThread = true;
		m_CalculateViewThreadWakeEvent.Set();
		m_CalculateViewOnCompleteFinished.Set();
		m_CalculateViewThread.Join();
		m_TargetMSPen.Dispose();
		m_TimeSpanFrameBrush.Dispose();
		m_VisibleRangeLineBrush.Dispose();
		m_SelectionFillBrush.Dispose();
		m_SelectionLineBrush.Dispose();
		m_GraphBrushAlert.Dispose();
		m_VisibleRangeFillBrush.Dispose();
		m_GraphBrushWarning.Dispose();
		m_GraphBrush.Dispose();
		m_CalculateViewThreadWakeEvent.Dispose();
		m_CalculateViewOnCompleteFinished.Dispose();
		m_EventLableBrush.Dispose();
		m_EventTextBrush.Dispose();
		m_EventLabelBorderPen.Dispose();
		base.Dispose(disposing);
	}

	private void OnCalculateViewComplete()
	{
		CheckIsMainThread();
		Misc.Swap(ref m_RenderData, ref m_RendererRenderData);
		Refresh();
		m_CalculateViewOnCompleteFinished.Set();
		if (NeedsToRecalculate)
		{
			m_CalculateViewThreadWakeEvent.Set();
		}
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
	}

	protected override void OnResize(EventArgs e)
	{
		lock (m_VisibleRange)
		{
			m_VisibleRange.ClientSize = base.ClientSize;
		}
		if (MainForm.Inst != null && MainForm.Inst.WindowState != FormWindowState.Minimized)
		{
			RecalculateView();
		}
		base.OnResize(e);
	}

	public void SetSession(Session session)
	{
		m_Session = session;
		HookSession();
	}

	private void HookSession()
	{
		m_Session.SessionIsReady += SessionIsReady;
	}

	private void SessionIsReady()
	{
		m_ControlTaskDispatcher.QueueTask(SessionIsReady_Main);
	}

	private void SessionIsReady_Main()
	{
		m_IsSessionReady = true;
		RecalculateView();
	}

	public void SetTimeRange(long start_time, long end_time)
	{
		long start_frame_x = TimeToFrameX(start_time);
		long end_frame_x = TimeToFrameX(end_time);
		double scale = GetScale(start_frame_x, end_frame_x);
		SetRange(start_frame_x, scale);
	}

	public void SetRange(long start_frame_x, double scale)
	{
		if (m_VisibleRange.StartFrameX != start_frame_x || m_VisibleRange.Scale != scale)
		{
			lock (m_VisibleRange)
			{
				m_VisibleRange.StartFrameX = start_frame_x;
				m_VisibleRange.Scale = scale;
			}
			RecalculateView();
		}
	}

	public void GotoStart()
	{
		m_VisibleRange.StartFrameX = 0L;
		RecalculateView();
		OnRangeChanged();
	}

	private void OnRangeChanged()
	{
		long end_frame_x = XToFrameX(base.Width, m_VisibleRange);
		if (this.RangeChanged != null)
		{
			this.RangeChanged(this, m_VisibleRange.StartFrameX, end_frame_x, m_VisibleRange.Scale);
		}
	}

	public void GotoEnd()
	{
		CheckIsMainThread();
		long num = XToFrameX(base.ClientSize.Width, m_VisibleRange) - m_VisibleRange.StartFrameX;
		long num2 = m_Session.FrameCount * Session.FrameXPerFrame - num;
		if (num2 > m_VisibleRange.StartFrameX)
		{
			m_VisibleRange.StartFrameX = num2;
			OnRangeChanged();
			RecalculateView();
		}
	}

	private void RecalculateView()
	{
		RecalculateView(force: false);
	}

	public void RecalculateView(bool force)
	{
		if (force)
		{
			lock (m_VisibleRange)
			{
				Range visibleRange = m_VisibleRange;
				int forceUpdateId = visibleRange.ForceUpdateId + 1;
				visibleRange.ForceUpdateId = forceUpdateId;
			}
		}
		if (m_Active)
		{
			m_CalculateViewThreadWakeEvent.Set();
		}
	}

	private void CalculateView()
	{
		m_RendererRenderData.m_Frames.Clear();
		m_RendererRenderData.m_FrameRenderData.Clear();
		m_RendererRenderData.m_TimeSpanRenderData.Clear();
		m_RendererRenderData.m_Events.Clear();
		int num = (int)((XToFrameX(m_RendererVisibleRange.ClientSize.Width, m_RendererVisibleRange) - m_RendererVisibleRange.StartFrameX + Session.FrameXPerFrame - 1) / Session.FrameXPerFrame);
		int num2 = (int)(m_RendererVisibleRange.StartFrameX / Session.FrameXPerFrame);
		int num3 = num2 + num;
		m_Session.GetFrames(num2, num3, m_RendererRenderData.m_Frames);
		m_RendererRenderData.m_LastFrameIndex = Math.Min(m_Session.FrameCount, num3);
		m_RendererRenderData.m_TimeSpanFrames.Clear();
		if (m_TimeSpanName != -1 && m_RendererRenderData.m_Frames.Count != 0)
		{
			m_Session.GetTimeSpanFrameTimes(num2, num3, m_TimeSpanName, m_RendererRenderData.m_TimeSpanFrames);
		}
		int num4 = (int)m_RendererVisibleRange.AlignedScale;
		int num5 = -(int)(m_RendererVisibleRange.StartFrameX % Session.FrameXPerFrame * num4 / Session.FrameXPerFrame);
		int num6 = ((num4 > 2) ? (num4 - 1) : num4);
		int num7 = 0;
		foreach (FrameStruct frame in m_RendererRenderData.m_Frames)
		{
			if (frame.m_Frame != null)
			{
				int num8 = TimeToY(frame.m_EndTime - frame.m_StartTime, m_RendererVisibleRange);
				FrameRenderData item = default(FrameRenderData);
				item.m_Frame = frame.m_Frame;
				item.m_Rect = new Rectangle(num5, num8, num6, m_RendererVisibleRange.ClientSize.Height - num8);
				item.m_Duration = frame.m_Frame.Duration;
				m_RendererRenderData.m_FrameRenderData.Add(item);
				if (m_ShowingTimeSpans && m_TimeSpanName != -1)
				{
					FrameTimeSpanStruct frameTimeSpanStruct = m_RendererRenderData.m_TimeSpanFrames[num7];
					FrameRenderData item2 = default(FrameRenderData);
					int num9 = TimeToY(frameTimeSpanStruct.m_Duration, m_RendererVisibleRange);
					item2.m_Frame = frame.m_Frame;
					item2.m_Rect = new Rectangle(num5, num9, num6, m_RendererVisibleRange.ClientSize.Height - num9);
					item2.m_Duration = frameTimeSpanStruct.m_Duration;
					m_RendererRenderData.m_TimeSpanRenderData.Add(item2);
				}
				num5 += num4;
				num7++;
			}
		}
		m_Events.Clear();
		long start_time = XToTime(-10, m_RendererVisibleRange);
		long end_time = XToTime(base.Width + 10, m_RendererVisibleRange);
		m_Session.GetEvents(start_time, end_time, m_Events);
		foreach (Event @event in m_Events)
		{
			EventDrawData item3 = default(EventDrawData);
			item3.m_Event = @event;
			item3.m_X = TimeToX(@event.Time, m_RendererVisibleRange);
			m_RendererRenderData.m_Events.Add(item3);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		try
		{
			e.Graphics.Clear(Colours.FrameGraphBackground);
			if (base.DesignMode || m_Session == null || !m_IsSessionReady)
			{
				return;
			}
			foreach (FrameRenderData frameRenderDatum in m_RenderData.m_FrameRenderData)
			{
				e.Graphics.FillRectangle(GetFrameBrush(frameRenderDatum.m_Duration), frameRenderDatum.m_Rect);
			}
			if (m_TimeSpanName != -1)
			{
				foreach (FrameRenderData timeSpanRenderDatum in m_RenderData.m_TimeSpanRenderData)
				{
					e.Graphics.FillRectangle(GetTimeSpanBrush(timeSpanRenderDatum.m_Duration), timeSpanRenderDatum.m_Rect);
				}
			}
			DrawVisibleRange(e.Graphics);
			DrawSelectedRange(e.Graphics);
			if (m_Session != null && m_Session.TimerFrequency != 0L)
			{
				long time = (long)(Misc.Clamp(m_TargetFrameMS, -10000.0, 10000.0) * (double)m_Session.TimerFrequency) / 1000;
				int num = TimeToY(time, m_VisibleRange);
				e.Graphics.DrawLine(m_TargetMSPen, 0, num, base.ClientSize.Width, num);
			}
			if (m_ShowEvents)
			{
				DrawEvents(e.Graphics);
			}
			if (m_ShowProcessingDataMessage)
			{
				DrawProcessingMessage(e.Graphics);
			}
		}
		catch (Exception)
		{
		}
	}

	private void DrawSelectedRange(Graphics graphics)
	{
		if (m_SelectedRangeStartFrame != -1 && m_SelectedRangeEndFrame != -1)
		{
			int num = Misc.Clamp(FrameIndexToX(m_SelectedRangeStartFrame, m_VisibleRange), 0, base.ClientSize.Width);
			int num2 = Misc.Clamp(FrameIndexToX(m_SelectedRangeEndFrame + 1, m_VisibleRange), 0, base.ClientSize.Width);
			Rectangle rect = new Rectangle(num, 0, num2 - num, m_VisibleRange.ClientSize.Height);
			graphics.DrawLine(m_SelectionLineBrush, rect.Left, rect.Top, rect.Left, rect.Bottom);
			if (rect.Width != 0)
			{
				graphics.FillRectangle(m_SelectionFillBrush, rect);
				graphics.DrawLine(m_SelectionLineBrush, rect.Right, rect.Top, rect.Right, rect.Bottom);
			}
		}
	}

	private Brush GetFrameBrush(long duration)
	{
		if (m_ShowingTimeSpans)
		{
			return m_TimeSpanFrameBrush;
		}
		return CoreUtils.GetFrameTimeCategory(duration, m_Session.TimerFrequency, m_TargetFrameMS) switch
		{
			CoreUtils.FrameTimeCategory.InBudget => m_GraphBrush, 
			CoreUtils.FrameTimeCategory.Warning => m_GraphBrushWarning, 
			CoreUtils.FrameTimeCategory.Alert => m_GraphBrushAlert, 
			_ => m_GraphBrushWarning, 
		};
	}

	private Brush GetTimeSpanBrush(long duration)
	{
		if (!((double)(duration * 1000) / (double)m_Session.TimerFrequency <= m_TargetFrameMS))
		{
			return m_TimeSpanOverTargetBrush;
		}
		return m_TimeSpanBrush;
	}

	private Rectangle GetSelectedRect()
	{
		int num = Misc.Clamp(TimeToX(m_VisibleStartTime), 0, base.ClientSize.Width);
		int num2 = Misc.Clamp(TimeToX(m_VisibleEndTime), 0, base.ClientSize.Width);
		return new Rectangle(num, 0, num2 - num, m_VisibleRange.ClientSize.Height);
	}

	private void DrawVisibleRange(Graphics graphics)
	{
		Rectangle selectedRect = GetSelectedRect();
		graphics.DrawLine(m_VisibleRangeLineBrush, selectedRect.Left, selectedRect.Top, selectedRect.Left, selectedRect.Bottom);
		if (selectedRect.Width != 0)
		{
			graphics.FillRectangle(m_VisibleRangeFillBrush, selectedRect);
			graphics.DrawLine(m_VisibleRangeLineBrush, selectedRect.Right, selectedRect.Top, selectedRect.Right, selectedRect.Bottom);
		}
	}

	private long TimeToFrameX(long time)
	{
		return m_Session.TimeToFrameX(time);
	}

	private int TimeToX(long time, Range range)
	{
		long frame_x = TimeToFrameX(time);
		return FrameXToX(frame_x, range);
	}

	private int TimeToX(long time)
	{
		CheckIsMainThread();
		return TimeToX(time, m_VisibleRange);
	}

	private int TimeToY(long time, Range range)
	{
		return range.ClientSize.Height - (int)((double)(time * 1000) * range.YScale / (double)m_Session.TimerFrequency);
	}

	private int FrameXToX(long frame_x, Range range)
	{
		int num = (int)range.AlignedScale;
		return (int)((frame_x - range.StartFrameX) * num / Session.FrameXPerFrame);
	}

	private long XToFrameX(int x, Range range)
	{
		int num = (int)range.AlignedScale;
		return x * Session.FrameXPerFrame / num + range.StartFrameX;
	}

	public void OnMouseWheel(int delta, Point mouse_pt)
	{
		long num = XToFrameX(mouse_pt.X, m_VisibleRange);
		double num2 = ((delta > 0) ? MainSessionView.ZoomMultiplier : (1.0 / MainSessionView.ZoomMultiplier));
		lock (m_VisibleRange)
		{
			m_VisibleRange.Scale *= num2;
			m_VisibleRange.StartFrameX += num - XToFrameX(mouse_pt.X, m_VisibleRange);
			m_VisibleRange.StartFrameX = Math.Max(0L, m_VisibleRange.StartFrameX);
		}
		RecalculateView();
		OnRangeChanged();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (m_IsSessionReady)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (Control.ModifierKeys == Keys.Shift)
				{
					SetSelectedRangeAndNotify(-1, -1);
					m_DragMode = DragMode.SelectedRange;
					StartDragging(e.X);
				}
				else if (Control.ModifierKeys == Keys.Control)
				{
					SelectFrame(e.X);
					m_DragMode = DragMode.VisibleRange;
					StartDragging(e.X);
				}
				else if (GetSelectedRect().Contains(e.Location))
				{
					m_DragSelectionStartTime = m_VisibleStartTime;
					m_DragSelectionEndTime = m_VisibleEndTime;
					m_DragMode = DragMode.MoveSelection;
					StartDragging(e.X);
				}
				else
				{
					m_DragMode = DragMode.Move;
					m_StartDragFrameX = m_VisibleRange.StartFrameX;
					StartDragging(e.X);
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				long num = XToTime(e.X, m_VisibleRange);
				if (num >= m_VisibleStartTime && num < m_VisibleEndTime)
				{
					m_VisibleRangeContextMenu.Show(PointToScreen(e.Location));
				}
				else if (m_SelectedRangeStartFrame != -1 && m_SelectedRangeEndFrame != -1)
				{
					m_SelectedRangeContextMenu.Show(PointToScreen(e.Location));
				}
			}
		}
		base.OnMouseDown(e);
	}

	private void SetSelectedRangeAndNotify(int start_frame_index, int end_frame_index)
	{
		if (m_SelectedRangeStartFrame != start_frame_index || m_SelectedRangeEndFrame != end_frame_index)
		{
			SetSelectedRange(start_frame_index, end_frame_index);
			if (this.SelectedRangeChanged != null)
			{
				this.SelectedRangeChanged(start_frame_index, end_frame_index);
			}
		}
	}

	public void SetSelectedRange(int start_frame_index, int end_frame_index)
	{
		if (m_SelectedRangeStartFrame != start_frame_index || m_SelectedRangeEndFrame != end_frame_index)
		{
			m_SelectedRangeStartFrame = start_frame_index;
			m_SelectedRangeEndFrame = end_frame_index;
			Refresh();
		}
	}

	private void StartDragging(int x)
	{
		base.Capture = true;
		m_DragMoveX = x;
	}

	private void UpdateFrameInfoBox(Point mouse_pos)
	{
		if (MainForm.Inst == null)
		{
			return;
		}
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		if (ShowEventHoverBox(mouse_pos))
		{
			return;
		}
		long num = m_VisibleRange.StartFrameX / Session.FrameXPerFrame * Session.FrameXPerFrame;
		int num2 = (int)((XToFrameX(mouse_pos.X, m_VisibleRange) - num) / Session.FrameXPerFrame);
		Frame frame = null;
		if (num2 >= 0 && num2 < m_RenderData.m_Frames.Count)
		{
			frame = m_RenderData.m_Frames[num2].m_Frame;
		}
		if (frame != null)
		{
			if (m_TimeSpanName != -1)
			{
				if (!(hoverBox.Target is int) || (int)hoverBox.Target != num2)
				{
					hoverBox.Target = num2;
					_ = m_VisibleRange.StartFrameX / Session.FrameXPerFrame;
					hoverBox.Clear();
					hoverBox.Title = "Frame";
					hoverBox.AddLine("Frame", num2.ToString());
					hoverBox.AddLine("Time Span", m_Session.GetTimerName(m_TimeSpanName));
					FrameTimeSpanStruct frameTimeSpanStruct = m_RenderData.m_TimeSpanFrames[num2];
					string timeString = Utils.GetTimeString(frameTimeSpanStruct.m_Duration, m_Session.TimerFrequency);
					hoverBox.AddLine("Duration", timeString);
					hoverBox.AddLine("Count", frameTimeSpanStruct.m_Count.ToString());
					hoverBox.SubmitLines();
				}
			}
			else if (Utils.IsShiftHeld)
			{
				int num3 = m_SelectedRangeEndFrame - m_SelectedRangeStartFrame;
				long num4 = m_Session.FrameXToTime(FrameIndexToFrameX(m_SelectedRangeStartFrame));
				long num5 = m_Session.FrameXToTime(FrameIndexToFrameX(m_SelectedRangeEndFrame)) - num4;
				long num6 = num5 * 1000 / m_Session.TimerFrequency;
				string text = ((num6 <= 1000) ? Utils.GetTimeString(num5, m_Session.TimerFrequency) : (((double)num6 / 1000.0).ToString("#.#") + " sec"));
				string text2 = num3 + " " + text;
				if (!(hoverBox.Target is string) || (string)hoverBox.Target != text2)
				{
					hoverBox.Clear();
					hoverBox.Title = "Time Range";
					hoverBox.AddLine("Selected Frame Count", num3.ToString());
					hoverBox.AddLine("Selection Duration", text);
					hoverBox.SubmitLines();
					hoverBox.Target = text2;
				}
			}
			else if (hoverBox.Target != frame && hoverBox.Target != frame)
			{
				hoverBox.Target = frame;
				int num7 = num2 + (int)(m_VisibleRange.StartFrameX / Session.FrameXPerFrame);
				hoverBox.Clear();
				hoverBox.Title = "Frame";
				hoverBox.AddLine("Frame Index", num7.ToString());
				string timeString2 = Utils.GetTimeString(frame.Duration, m_Session.TimerFrequency);
				hoverBox.AddLine("Frame Duration", timeString2);
				hoverBox.SubmitLines();
			}
			Point location = PointToScreen(mouse_pos);
			hoverBox.SetLocation(location);
			hoverBox.Visible = true;
		}
		else
		{
			hoverBox.Visible = false;
		}
	}

	private bool ShowEventHoverBox(Point mouse_pos)
	{
		if (mouse_pos.Y >= 0 && mouse_pos.Y < 20)
		{
			foreach (EventDrawData event2 in m_RenderData.m_Events)
			{
				if (mouse_pos.X >= event2.m_X - 10 && mouse_pos.X < event2.m_X + 10)
				{
					HoverBox hoverBox = MainForm.Inst.HoverBox;
					Event @event = event2.m_Event;
					string text = "Event: " + @event.Name;
					if (!(hoverBox.Target is string) || (string)hoverBox.Target != text)
					{
						hoverBox.Target = text;
						hoverBox.Clear();
						hoverBox.Title = "Event";
						hoverBox.AddLine("Name", m_Session.GetString(@event.Name));
						hoverBox.SubmitLines();
					}
					hoverBox.Visible = true;
					Point location = PointToScreen(mouse_pos);
					hoverBox.SetLocation(location);
					return true;
				}
			}
		}
		return false;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		UpdateFrameInfoBox(e.Location);
		switch (m_DragMode)
		{
		case DragMode.Move:
		{
			long num4 = XToFrameX(e.X, m_VisibleRange) - XToFrameX(m_DragMoveX, m_VisibleRange);
			m_VisibleRange.StartFrameX = Math.Max(0L, m_StartDragFrameX - num4);
			RecalculateView();
			OnRangeChanged();
			break;
		}
		case DragMode.SelectedRange:
		{
			int a2 = XToFrameIndex(m_DragMoveX);
			int b2 = XToFrameIndex(e.X);
			if (a2 > b2)
			{
				Utils.Swap(ref a2, ref b2);
			}
			SetSelectedRangeAndNotify(a2, b2);
			break;
		}
		case DragMode.VisibleRange:
		{
			long frame_x = XToFrameX(m_DragMoveX, m_VisibleRange);
			long frame_x2 = XToFrameX(e.X, m_VisibleRange);
			long a = FrameXToTime(frame_x);
			long b = FrameXToTime(frame_x2);
			if (a == b)
			{
				SelectFrame(e.X);
				break;
			}
			if (a > b)
			{
				Utils.Swap(ref a, ref b);
			}
			if (SetVisibleRange(a, b, scroll_into_view: false))
			{
				OnVisibleRangeChanged();
			}
			break;
		}
		case DragMode.MoveSelection:
		{
			long num = XToTime(e.X, m_VisibleRange) - XToTime(m_DragMoveX, m_VisibleRange);
			long num2 = m_DragSelectionEndTime - m_DragSelectionStartTime;
			long num3 = Math.Max(m_Session.FirstFrameTime, m_DragSelectionStartTime + num);
			SetVisibleRangeAndUpdate(num3, num3 + num2);
			Refresh();
			break;
		}
		}
		base.OnMouseMove(e);
	}

	private void SetVisibleRangeAndUpdate(long start_time, long end_time)
	{
		if (start_time != m_VisibleStartTime || end_time != m_VisibleEndTime)
		{
			m_VisibleStartTime = start_time;
			m_VisibleEndTime = end_time;
			OnVisibleRangeChanged();
		}
	}

	private long XToTime(int x, Range range)
	{
		long frame_x = XToFrameX(x, range);
		return FrameXToTime(frame_x);
	}

	private long FrameXToTime(long frame_x)
	{
		return m_Session.FrameXToTime(frame_x);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_DragMoveX == e.X && (m_DragMode == DragMode.Move || m_DragMode == DragMode.MoveSelection))
			{
				SelectFrame(e.X);
			}
			if (m_DragMode != 0)
			{
				base.Capture = false;
				m_DragMode = DragMode.None;
			}
		}
		base.OnMouseUp(e);
	}

	private static long FrameIndexToFrameX(int frame_index)
	{
		return frame_index * Session.FrameXPerFrame;
	}

	private static int FrameXToFrameIndex(long frame_x)
	{
		return (int)(frame_x / Session.FrameXPerFrame);
	}

	private int XToFrameIndex(int x)
	{
		return FrameXToFrameIndex(XToFrameX(x, m_VisibleRange));
	}

	private int FrameIndexToX(int frame_index, Range range)
	{
		return FrameXToX(FrameIndexToFrameX(frame_index), range);
	}

	private void SelectFrame(int x)
	{
		CheckIsMainThread();
		int index = XToFrameIndex(x);
		Frame frame = m_Session.GetFrame(index);
		if (frame != null && SetVisibleRange(frame.StartTime, frame.EndTime, scroll_into_view: false))
		{
			OnVisibleRangeChanged();
		}
	}

	private void OnVisibleRangeChanged()
	{
		if (this.VisibleRangeChanged != null)
		{
			this.VisibleRangeChanged(this, m_VisibleStartTime, m_VisibleEndTime);
		}
	}

	public bool SetVisibleRange(long start_time, long end_time, bool scroll_into_view)
	{
		CheckIsMainThread();
		if (m_VisibleStartTime != start_time || m_VisibleEndTime != end_time)
		{
			m_VisibleStartTime = start_time;
			m_VisibleEndTime = end_time;
			if (m_IsSessionReady)
			{
				if (scroll_into_view)
				{
					if (!ScrollSelectionIntoView())
					{
						Refresh();
					}
				}
				else
				{
					Refresh();
				}
			}
			return true;
		}
		return false;
	}

	public void UpdateShowingProcessingDataMessage()
	{
		if (!ShowingTimeSpans && m_Session.ProcessingPackets && !m_Session.Connected)
		{
			m_DrawProcessingDataTimer.Start();
			m_FirstProcessedPacketTime = Environment.TickCount;
		}
	}

	private void DrawProcessingDataTimerTick(object sender, EventArgs e)
	{
		if (m_Session.ProcessingPackets)
		{
			if (Environment.TickCount - m_FirstProcessedPacketTime > 2000)
			{
				m_ShowProcessingDataMessage = true;
			}
			m_LastProcessedPacketTime = Environment.TickCount;
		}
		else if (Environment.TickCount - m_LastProcessedPacketTime > 1000)
		{
			m_ShowProcessingDataMessage = false;
			m_DrawProcessingDataTimer.Stop();
		}
		Refresh();
	}

	private void DrawEvents(Graphics graphics)
	{
		int count = m_RenderData.m_Events.Count;
		for (int i = 0; i < count; i++)
		{
			EventDrawData eventDrawData = m_RenderData.m_Events[i];
			string @string = m_Session.GetString(eventDrawData.m_Event.Name);
			int num = (int)graphics.MeasureString(@string, m_EventFont).Width;
			int num2 = eventDrawData.m_X + 5 + num + 5 + 5 + 10;
			bool draw_text = i == count - 1 || m_RenderData.m_Events[i + 1].m_X > num2;
			DrawEvent(eventDrawData.m_Event, eventDrawData.m_X, @string, draw_text, graphics);
		}
	}

	private void DrawEvent(Event ev, int x, string event_name, bool draw_text, Graphics graphics)
	{
		Point[] points = new Point[4]
		{
			new Point(x - 10, 10),
			new Point(x, 0),
			new Point(x + 10, 10),
			new Point(x, 20)
		};
		Brush brush = m_BrushSet.GetBrush(ev.Colour);
		graphics.FillPolygon(brush, points);
		if (draw_text)
		{
			SizeF sizeF = graphics.MeasureString(event_name, m_EventFont);
			Rectangle rect = new Rectangle(x + 10 + 5, 10 - (int)sizeF.Height / 2, (int)sizeF.Width, (int)sizeF.Height);
			rect.Inflate(2, 2);
			graphics.FillRectangle(m_EventLableBrush, rect);
			graphics.DrawRectangle(m_EventLabelBorderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
			graphics.DrawString(event_name, m_EventFont, m_EventTextBrush, rect.X + 2, rect.Y + 2);
		}
	}

	private void DrawProcessingMessage(Graphics graphics)
	{
		int num = Resources.ProcessingBackground.Width;
		int num2 = Resources.ProcessingBackground.Height;
		int num3 = (base.Width - num) / 2;
		int num4 = (base.Height - num2) / 2;
		graphics.DrawImageUnscaled(Resources.ProcessingBackground, num3, num4);
		string text = "Processing Data (" + m_Session.ProcessingCompletePercent + "%)...";
		SizeF sizeF = graphics.MeasureString(text, m_ProcessingDataFont);
		int num5 = num3 + (num - (int)sizeF.Width) / 2;
		int num6 = num4 + (num2 - (int)sizeF.Height) / 2;
		int num7 = Environment.TickCount / 1000 % 4;
		text = text.Substring(0, text.Length - (3 - num7));
		graphics.DrawString(text, m_ProcessingDataFont, m_ProcessingDataBrush, num5, num6);
	}

	private bool ScrollSelectionIntoView()
	{
		long num = TimeToFrameX(m_VisibleStartTime);
		long num2 = TimeToFrameX(m_VisibleEndTime);
		long num3 = XToFrameX(base.Width, m_VisibleRange) - XToFrameX(0, m_VisibleRange);
		if (num < m_VisibleRange.StartFrameX)
		{
			m_VisibleRange.StartFrameX = num;
			OnRangeChanged();
			RecalculateView();
			return true;
		}
		if (m_VisibleRange.StartFrameX + num3 < num2)
		{
			m_VisibleRange.StartFrameX = num2 - num3;
			OnRangeChanged();
			RecalculateView();
			return true;
		}
		return false;
	}

	public void CentreSelection()
	{
		long num = TimeToFrameX(m_VisibleStartTime);
		long num2 = TimeToFrameX(m_VisibleEndTime);
		long num3 = num + (num2 - num) / 2;
		long num4 = XToFrameX(base.Width, m_VisibleRange) - XToFrameX(0, m_VisibleRange);
		m_VisibleRange.StartFrameX = Math.Max(0L, num3 - num4 / 2);
		OnRangeChanged();
		RecalculateView();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (!base.DesignMode && MainForm.Inst != null)
		{
			MainForm.Inst.HoverBox.Visible = false;
		}
		base.OnMouseLeave(e);
	}

	public void SetTimeSpan(long time_sapn_name)
	{
		if (m_TimeSpanName != time_sapn_name)
		{
			m_TimeSpanName = time_sapn_name;
			RecalculateView(force: true);
		}
	}

	private void VisibleRangeContextMenuItem(object sender, EventArgs e)
	{
		long start_frame_x = TimeToFrameX(m_VisibleStartTime);
		long end_frame_x = TimeToFrameX(m_VisibleEndTime);
		double scale = GetScale(start_frame_x, end_frame_x);
		SetRange(start_frame_x, scale);
		OnRangeChanged();
	}

	private double GetScale(long start_frame_x, long end_frame_x)
	{
		long num = Math.Abs(start_frame_x - end_frame_x);
		return (double)(base.ClientSize.Width * Session.FrameXPerFrame) / (double)num;
	}

	public void CentreFrame(int frame_index)
	{
		Frame frame = m_Session.GetFrame(frame_index);
		long num = TimeToFrameX(frame.StartTime);
		long num2 = TimeToFrameX(frame.EndTime);
		long num3 = XToFrameX(base.Width, m_VisibleRange) - XToFrameX(0, m_VisibleRange);
		long val = num - (num3 - (num2 - num)) / 2;
		m_VisibleRange.StartFrameX = Math.Max(0L, val);
		OnRangeChanged();
		RecalculateView();
	}

	private void CreateSessionFromSelectionMenuItemClick(object sender, EventArgs e)
	{
		MainForm.Inst.CloneSession(m_VisibleStartTime, m_VisibleEndTime);
	}

	public void OnTargetMSChanged()
	{
		RecalculateView(force: true);
	}

	private void ClearSelectionMenuItemClicked(object sender, EventArgs e)
	{
		SetSelectedRangeAndNotify(-1, -1);
	}

	private void CreateSessionFromSelectionMenuItemClicked(object sender, EventArgs e)
	{
		long frame_x = FrameIndexToFrameX(m_SelectedRangeStartFrame);
		long frame_x2 = FrameIndexToFrameX(m_SelectedRangeEndFrame + 1);
		long start_time = FrameXToTime(frame_x);
		long end_time = FrameXToTime(frame_x2);
		MainForm.Inst.CloneSession(start_time, end_time);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		this.m_VisibleRangeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.zoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_SelectedRangeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.clearSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.createSessionFromSelectionToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.m_VisibleRangeContextMenu.SuspendLayout();
		this.m_SelectedRangeContextMenu.SuspendLayout();
		base.SuspendLayout();
		this.m_VisibleRangeContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.m_VisibleRangeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.zoomToolStripMenuItem });
		this.m_VisibleRangeContextMenu.Name = "m_SelectedRangeContextMenu";
		this.m_VisibleRangeContextMenu.Size = new System.Drawing.Size(107, 26);
		this.zoomToolStripMenuItem.Name = "zoomToolStripMenuItem";
		this.zoomToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
		this.zoomToolStripMenuItem.Text = "Zoom";
		this.zoomToolStripMenuItem.Click += new System.EventHandler(VisibleRangeContextMenuItem);
		this.m_SelectedRangeContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
		this.m_SelectedRangeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.clearSelectionToolStripMenuItem, this.createSessionFromSelectionToolStripMenuItem1 });
		this.m_SelectedRangeContextMenu.Name = "m_SelectedRange1ContextMenu";
		this.m_SelectedRangeContextMenu.Size = new System.Drawing.Size(229, 48);
		this.clearSelectionToolStripMenuItem.Name = "clearSelectionToolStripMenuItem";
		this.clearSelectionToolStripMenuItem.Size = new System.Drawing.Size(228, 22);
		this.clearSelectionToolStripMenuItem.Text = "Clear Selection";
		this.clearSelectionToolStripMenuItem.Click += new System.EventHandler(ClearSelectionMenuItemClicked);
		this.createSessionFromSelectionToolStripMenuItem1.Name = "createSessionFromSelectionToolStripMenuItem1";
		this.createSessionFromSelectionToolStripMenuItem1.Size = new System.Drawing.Size(228, 22);
		this.createSessionFromSelectionToolStripMenuItem1.Text = "Create session from selection";
		this.createSessionFromSelectionToolStripMenuItem1.Click += new System.EventHandler(CreateSessionFromSelectionMenuItemClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Name = "FrameGraphPanel";
		base.Size = new System.Drawing.Size(917, 128);
		this.m_VisibleRangeContextMenu.ResumeLayout(false);
		this.m_SelectedRangeContextMenu.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
