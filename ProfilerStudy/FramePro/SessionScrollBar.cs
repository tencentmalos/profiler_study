using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using SCLCoreCLR;

namespace FramePro;

internal class SessionScrollBar : UserControl
{
	public enum EMode
	{
		Time,
		Frame
	}

	private const int m_TopLineHeight = 2;

	private const int m_MinIntervalWidth = 50;

	private const int m_IntervalLineHeight = 6;

	private const int m_IntervalTextY = 8;

	private const int m_ScrollBarHeight = 25;

	private const int m_ResizeDist = 4;

	private const int m_EventDiamondSize = 4;

	private const int m_EventY = 30;

	private EMode m_Mode = EMode.Frame;

	private long m_SelectedRangeStartFrameX;

	private long m_SelectedRangeEndFrameX;

	private Session m_Session;

	private Settings m_Settings;

	private bool m_SessionIsReady;

	private Pen m_LineLen = new Pen(Colours.SessionScrollLine);

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private Font m_AxisFont = new Font("Ariel", 7f);

	private Brush m_AxisBrush = new SolidBrush(Colours.SessionScrollLine);

	private long m_TotalSessionTime;

	private int m_TotalSessionFrameCount;

	private Interval m_Interval;

	private Brush m_ScrollBarBrush = new SolidBrush(Colours.SessionScrollBarColour);

	private bool m_Dragging;

	private int m_LastDragX;

	private bool m_HoverResize;

	private bool m_DraggingStart;

	private bool m_DraggingEnd;

	private bool m_LockedForDragging;

	private List<Event> m_Events = new List<Event>();

	private BrushSet m_BrushSet = new BrushSet();

	private long[] m_FrameTimes;

	private double m_TargetFrameMS;

	private Pen m_GraphPen = new Pen(Colours.FrameGraphFrameBar);

	private Pen m_GraphPenWarning = new Pen(Colours.FrameGraphFrameBarWarning);

	private Pen m_GraphPenAlert = new Pen(Colours.FrameGraphFrameBarAlert);

	private const int m_InBudgetFrameLineHeight = 2;

	private const int m_WarningFrameLineHeight = 2;

	private const int m_AlertFrameLineHeight = 4;

	private float m_DPIScale = 1f;

	private IContainer components;

	public EMode Mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			if (m_Mode != value)
			{
				m_Mode = value;
				if (m_SessionIsReady)
				{
					UpdateInterval();
					RecalculateFrameTimes();
					Refresh();
				}
				m_Settings.SessionScrollBarMode = value;
				m_Settings.Write();
			}
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
			m_TargetFrameMS = value;
			if (m_SessionIsReady)
			{
				RecalculateFrameTimes();
				Refresh();
			}
		}
	}

	public event SessionScrollBarChangedHandler SessionScrollBarChanged;

	public SessionScrollBar()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		m_DPIScale = MainForm.DPIScale;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void Dispose(bool disposing)
	{
		m_BrushSet.Dispose();
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	public void SetSession(Session session)
	{
		m_Session = session;
		HookSession();
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
		m_Mode = m_Settings.SessionScrollBarMode;
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
		m_SessionIsReady = true;
		Refresh();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(Colours.SessionScrollBarBackground);
		for (int i = 0; i < ScaleDPI(2); i++)
		{
			e.Graphics.DrawLine(m_LineLen, 0, i, base.Width, i);
		}
		if (m_SessionIsReady)
		{
			DrawFrameTimes(e.Graphics);
			DrawSelectedRange(e.Graphics);
			switch (m_Mode)
			{
			case EMode.Frame:
				DrawFrameIntervals(e.Graphics);
				break;
			case EMode.Time:
				DrawTimeIntervals(e.Graphics);
				break;
			}
			DrawEvents(e.Graphics);
		}
		base.OnPaint(e);
	}

	private long ToNanoSeconds(long time)
	{
		BigInteger bigInteger = new BigInteger(time);
		BigInteger bigInteger2 = new BigInteger(1000000000);
		BigInteger bigInteger3 = new BigInteger(m_Session.TimerFrequency);
		return (long)(bigInteger * bigInteger2 / bigInteger3);
	}

	private void DrawFrameIntervals(Graphics graphics)
	{
		int num = (int)((double)m_TotalSessionFrameCount / m_Interval.StepSize) + 1;
		for (int i = 0; i < num; i++)
		{
			int num2 = (int)((double)i * m_Interval.StepSize);
			int num3 = FrameXToX(num2 * Session.FrameXPerFrame);
			graphics.DrawLine(m_LineLen, num3, 0, num3, ScaleDPI(6));
			string s = ((double)i * m_Interval.Step).ToString();
			int num4 = (int)graphics.MeasureString(s, m_AxisFont).Width;
			graphics.DrawString(s, m_AxisFont, m_AxisBrush, num3 - num4 / 2, ScaleDPI(8));
		}
	}

	private void DrawTimeIntervals(Graphics graphics)
	{
		int num = (int)((double)ToNanoSeconds(m_TotalSessionTime) / m_Interval.StepSize) + 1;
		long timerFrequency = m_Session.TimerFrequency;
		for (int i = 0; i < num; i++)
		{
			long time = (long)((double)i * m_Interval.StepSize * (double)timerFrequency / 1000000000.0) + m_Session.FirstFrameTime;
			int num2 = TimeToX(time);
			graphics.DrawLine(m_LineLen, num2, 0, num2, ScaleDPI(6));
			string text = ((double)i * m_Interval.Step).ToString();
			if (i != 0)
			{
				text = text + " " + m_Interval.UnitName;
			}
			int num3 = (int)graphics.MeasureString(text, m_AxisFont).Width;
			graphics.DrawString(text, m_AxisFont, m_AxisBrush, num2 - num3 / 2, ScaleDPI(8));
		}
	}

	private void DrawEvents(Graphics graphics)
	{
		m_Events.Clear();
		m_Session.GetEvents(m_Session.FirstFrameTime, m_Session.FirstFrameTime + m_TotalSessionTime, m_Events);
		foreach (Event @event in m_Events)
		{
			DrawEvent(@event, graphics);
		}
	}

	private void DrawEvent(Event ev, Graphics graphics)
	{
		int num = TimeToX(ev.Time);
		Point[] points = new Point[4]
		{
			new Point(num - ScaleDPI(4), ScaleDPI(30)),
			new Point(num, ScaleDPI(30) - ScaleDPI(4)),
			new Point(num + ScaleDPI(4), ScaleDPI(30)),
			new Point(num, ScaleDPI(30) + ScaleDPI(4))
		};
		Brush brush = m_BrushSet.GetBrush(ev.Colour);
		graphics.FillPolygon(brush, points);
	}

	private int TimeToX(long time)
	{
		switch (m_Mode)
		{
		case EMode.Frame:
		{
			long frame_x = TimeToFrameX(time);
			return FrameXToX(frame_x);
		}
		case EMode.Time:
			if (m_TotalSessionTime == 0L)
			{
				return 0;
			}
			return (int)((time - m_Session.FirstFrameTime) * base.Width / m_TotalSessionTime);
		default:
			return -1;
		}
	}

	private long XToTime(int x)
	{
		switch (m_Mode)
		{
		case EMode.Frame:
		{
			long frame_x = XToFrameX(x);
			return FrameXToTime(frame_x);
		}
		case EMode.Time:
			return m_Session.FirstFrameTime + x * m_TotalSessionTime / base.Width;
		default:
			return -1L;
		}
	}

	private int FrameXToX(long frame_x)
	{
		switch (m_Mode)
		{
		case EMode.Frame:
			if (m_TotalSessionFrameCount == 0)
			{
				return 0;
			}
			return (int)(frame_x * base.Width / (m_TotalSessionFrameCount * Session.FrameXPerFrame));
		case EMode.Time:
		{
			long time = FrameXToTime(frame_x);
			return TimeToX(time);
		}
		default:
			return -1;
		}
	}

	private long XToFrameX(int x)
	{
		switch (m_Mode)
		{
		case EMode.Frame:
			return x * m_TotalSessionFrameCount * Session.FrameXPerFrame / base.Width;
		case EMode.Time:
		{
			long time = XToTime(x);
			return TimeToFrameX(time);
		}
		default:
			return -1L;
		}
	}

	private Pen GetFramePen(long frame_duration)
	{
		return CoreUtils.GetFrameTimeCategory(frame_duration, m_Session.TimerFrequency, m_TargetFrameMS) switch
		{
			CoreUtils.FrameTimeCategory.InBudget => m_GraphPen, 
			CoreUtils.FrameTimeCategory.Warning => m_GraphPenWarning, 
			CoreUtils.FrameTimeCategory.Alert => m_GraphPenAlert, 
			_ => m_GraphPenWarning, 
		};
	}

	private int GetFrameLineHeight(long frame_duration)
	{
		return CoreUtils.GetFrameTimeCategory(frame_duration, m_Session.TimerFrequency, m_TargetFrameMS) switch
		{
			CoreUtils.FrameTimeCategory.InBudget => ScaleDPI(2), 
			CoreUtils.FrameTimeCategory.Warning => ScaleDPI(2), 
			CoreUtils.FrameTimeCategory.Alert => ScaleDPI(4), 
			_ => 0, 
		};
	}

	private void RecalculateFrameTimes()
	{
		if (m_FrameTimes == null || m_FrameTimes.Length != base.Width)
		{
			m_FrameTimes = new long[base.Width];
		}
		switch (m_Mode)
		{
		case EMode.Frame:
			m_Session.SampleFrameTimesForSession(m_FrameTimes);
			break;
		case EMode.Time:
			m_Session.SampleFrameTimesForSessionByTime(m_FrameTimes, XToTime);
			break;
		}
	}

	private void DrawFrameTimes(Graphics graphics)
	{
		graphics.DrawLine(Pens.White, 0, base.Height - 1, base.Width, base.Height - 1);
		if (m_FrameTimes != null)
		{
			for (int i = 0; i < m_FrameTimes.Length; i++)
			{
				long frame_duration = m_FrameTimes[i];
				Pen framePen = GetFramePen(frame_duration);
				int frameLineHeight = GetFrameLineHeight(frame_duration);
				graphics.DrawLine(framePen, i, base.Height - 2, i, base.Height - 2 - frameLineHeight);
			}
		}
	}

	protected override void OnResize(EventArgs e)
	{
		if (m_SessionIsReady)
		{
			UpdateInterval();
			RecalculateFrameTimes();
		}
		Refresh();
		base.OnResize(e);
	}

	private void DrawSelectedRange(Graphics graphics)
	{
		int value = FrameXToX(m_SelectedRangeStartFrameX);
		int value2 = FrameXToX(m_SelectedRangeEndFrameX);
		value = Misc.Clamp(value, 0, base.Width);
		value2 = Misc.Clamp(value2, 0, base.Width);
		graphics.FillRectangle(rect: new Rectangle(value, ScaleDPI(2), value2 - value, ScaleDPI(25)), brush: m_ScrollBarBrush);
	}

	public void SetSelectedRange(long start_frame_x, long end_frame_x)
	{
		m_SelectedRangeStartFrameX = start_frame_x;
		m_SelectedRangeEndFrameX = end_frame_x;
		Refresh();
	}

	public void OnSessionChanged()
	{
		if (m_Session.IsReady)
		{
			if (!m_LockedForDragging)
			{
				m_TotalSessionTime = m_Session.LastFrameEndTime - m_Session.FirstFrameTime;
				m_TotalSessionFrameCount = m_Session.FrameCount;
				UpdateInterval();
			}
			RecalculateFrameTimes();
			Refresh();
		}
	}

	private void UpdateInterval()
	{
		switch (m_Mode)
		{
		case EMode.Frame:
			m_Interval = MiscUnits.GetBestInterval(m_TotalSessionFrameCount, base.Width, ScaleDPI(50), "Frame");
			break;
		case EMode.Time:
		{
			long num = ToNanoSeconds(m_TotalSessionTime);
			m_Interval = TimeUnits.GetBestInterval(num, base.Width, ScaleDPI(50));
			break;
		}
		}
	}

	private void CentreOnFrameX(long frame_x)
	{
		long num = m_SelectedRangeEndFrameX - m_SelectedRangeStartFrameX;
		m_SelectedRangeStartFrameX = Math.Max(0L, frame_x - num / 2);
		m_SelectedRangeEndFrameX = m_SelectedRangeStartFrameX + num;
		OnSelectedRangeChanged();
		Refresh();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_HoverResize)
			{
				int num = FrameXToX(m_SelectedRangeStartFrameX);
				if (Math.Abs(e.X - num) < ScaleDPI(4))
				{
					m_DraggingStart = true;
				}
				else
				{
					m_DraggingEnd = true;
				}
			}
			else
			{
				long num2 = XToFrameX(e.X);
				if (num2 < m_SelectedRangeStartFrameX || num2 >= m_SelectedRangeEndFrameX)
				{
					CentreOnFrameX(num2);
				}
				m_Dragging = true;
			}
			m_LastDragX = e.X;
			base.Capture = true;
			MainForm.Inst.HoverBox.Visible = false;
			LockSessionTime();
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_Dragging)
		{
			long num = XToFrameX(e.X) - XToFrameX(m_LastDragX);
			m_LastDragX = e.X;
			long num2 = m_SelectedRangeEndFrameX - m_SelectedRangeStartFrameX;
			m_SelectedRangeStartFrameX = Math.Max(0L, m_SelectedRangeStartFrameX + num);
			m_SelectedRangeEndFrameX = m_SelectedRangeStartFrameX + num2;
			OnSelectedRangeChanged();
			Refresh();
		}
		else if (m_DraggingStart)
		{
			long num3 = XToFrameX(e.X) - XToFrameX(m_LastDragX);
			m_LastDragX = e.X;
			m_SelectedRangeStartFrameX += num3;
			m_SelectedRangeStartFrameX = Misc.Clamp(m_SelectedRangeStartFrameX, 0L, m_SelectedRangeEndFrameX - Session.FrameXPerFrame);
			OnSelectedRangeChanged();
			Refresh();
		}
		else if (m_DraggingEnd)
		{
			long num4 = XToFrameX(e.X) - XToFrameX(m_LastDragX);
			m_LastDragX = e.X;
			m_SelectedRangeEndFrameX += num4;
			m_SelectedRangeEndFrameX = Misc.Clamp(m_SelectedRangeEndFrameX, m_SelectedRangeStartFrameX + Session.FrameXPerFrame, m_TotalSessionFrameCount * Session.FrameXPerFrame);
			OnSelectedRangeChanged();
			Refresh();
		}
		else
		{
			UpdateHoverBox(e.Location);
			int num5 = FrameXToX(m_SelectedRangeStartFrameX);
			int num6 = FrameXToX(m_SelectedRangeEndFrameX);
			if (num6 - num5 > ScaleDPI(4))
			{
				if (Math.Abs(e.X - num5) < ScaleDPI(4) || Math.Abs(e.X - num6) < ScaleDPI(4))
				{
					Cursor.Current = Cursors.SizeWE;
					m_HoverResize = true;
				}
				else if (m_HoverResize)
				{
					Cursor.Current = Cursors.Default;
					m_HoverResize = false;
				}
			}
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_Dragging)
			{
				m_Dragging = false;
				base.Capture = false;
				UnlockSessionTime();
			}
			else if (m_DraggingStart || m_DraggingEnd)
			{
				m_DraggingStart = false;
				m_DraggingEnd = false;
				base.Capture = false;
				Cursor.Current = Cursors.Default;
				m_HoverResize = false;
				UnlockSessionTime();
			}
		}
		base.OnMouseUp(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (m_HoverResize)
		{
			m_HoverResize = false;
			Cursor.Current = Cursors.Default;
		}
		MainForm.Inst.HoverBox.Visible = false;
		base.OnMouseLeave(e);
	}

	private void OnSelectedRangeChanged()
	{
		if (this.SessionScrollBarChanged != null)
		{
			this.SessionScrollBarChanged(m_SelectedRangeStartFrameX, m_SelectedRangeEndFrameX);
		}
	}

	private void LockSessionTime()
	{
		m_LockedForDragging = true;
	}

	private void UnlockSessionTime()
	{
		m_LockedForDragging = false;
		Refresh();
	}

	private Event FindEvent(int x)
	{
		long num = XToTime(x);
		long num2 = XToTime(x - ScaleDPI(4));
		long num3 = XToTime(x + ScaleDPI(4));
		Event result = null;
		long num4 = long.MaxValue;
		foreach (Event @event in m_Events)
		{
			long num5 = Math.Abs(@event.Time - num);
			if (@event.Time >= num2 && @event.Time < num3 && num5 < num4)
			{
				result = @event;
				num4 = num5;
			}
		}
		return result;
	}

	private void UpdateHoverBox(Point mouse_pt)
	{
		Event @event = null;
		if (mouse_pt.Y >= ScaleDPI(30) - ScaleDPI(4) && mouse_pt.Y < ScaleDPI(30) + ScaleDPI(4))
		{
			@event = FindEvent(mouse_pt.X);
		}
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		if (@event != null)
		{
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
			Point location = PointToScreen(mouse_pt);
			hoverBox.SetLocation(location);
		}
		else if (mouse_pt.Y < ScaleDPI(2) + ScaleDPI(6))
		{
			int num = (int)(XToFrameX(mouse_pt.X) / Session.FrameXPerFrame);
			long time = XToTime(mouse_pt.X) - m_Session.FirstFrameTime;
			string text2 = "Frame: " + num + time;
			if (!(hoverBox.Target is string) || (string)hoverBox.Target != text2)
			{
				hoverBox.Target = text2;
				hoverBox.Clear();
				hoverBox.Title = "Frame";
				hoverBox.AddLine("Frame", num.ToString());
				string timeString = Utils.GetTimeString(time, m_Session.TimerFrequency);
				hoverBox.AddLine("Time", timeString);
				hoverBox.SubmitLines();
			}
			hoverBox.Visible = true;
			Point location2 = PointToScreen(mouse_pt);
			hoverBox.SetLocation(location2);
		}
		else
		{
			hoverBox.Visible = false;
		}
	}

	private long TimeToFrameX(long time)
	{
		return m_Session.TimeToFrameX(time);
	}

	private long FrameXToTime(long frame_x)
	{
		return m_Session.FrameXToTime(frame_x);
	}

	public void UpdateSessionScrollBarMode()
	{
		Mode = m_Settings.SessionScrollBarMode;
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Name = "SessionScrollBar";
		base.Size = new System.Drawing.Size(1079, 82);
		base.ResumeLayout(false);
	}
}
