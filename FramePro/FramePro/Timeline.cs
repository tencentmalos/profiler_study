using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace FramePro;

internal class Timeline : UserControl
{
	private Session m_Session;

	private TimeRange m_VisibleTimeRange = new TimeRange();

	private const int m_TimelineHeight = 30;

	private const int m_FrameHeight = 20;

	private const int m_TextXGap = 10;

	private Color m_FrameLineColour = Color.Black;

	private Pen m_LinePen = Pens.Black;

	private Brush m_FrameBrush = new SolidBrush(Colours.TimeLineFrameFillColour);

	private Pen m_FrameLinePen = new Pen(Colours.FrameLine);

	private Brush m_FrameTextBrush = new SolidBrush(Colours.TimeLineFrameTextColour);

	private Brush m_StallFrameBrush = Brushes.Red;

	private Pen m_StallFramePen = Pens.DarkRed;

	private Brush m_StallFrameTextBrush = Brushes.Black;

	private Brush m_TimeSpanSpikeFrameBrush = Brushes.Orange;

	private Pen m_TimeSpanSpikeFramePen = Pens.DarkOrange;

	private Brush m_TimeSpanSpikeFrameTextBrush = Brushes.Black;

	private List<Frame> m_VisibleFrames = new List<Frame>();

	private const int m_IntervalIdealWidthS = 100;

	private const int m_IntervalIdealWidthMS = 100;

	private const int m_IntervalIdealWidthUS = 200;

	private const int m_IntervalIdealWidthNS = 200;

	private const int m_MaxSubStepCount = 1000;

	private int[] m_TimeSteps = new int[8] { 100000, 10000, 1000, 100, 50, 10, 5, 1 };

	private MeasureLine m_MeasureLine = new MeasureLine();

	private Pen m_MeasureLinePen = new Pen(new SolidBrush(Colours.TimelineMeasureLineColour));

	private Brush m_MeasureLineFillBrush = new SolidBrush(Colours.TimelineMeasureLineFillColour);

	private bool m_MouseHoverEnabled = true;

	private const int m_MinFrameLineWidth = 5;

	private const int m_IntervalLineHeight = 10;

	private bool m_SessionIsReady;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private float m_DPIScale;

	private IContainer components;

	public TimeRange VisibleTimeRange
	{
		get
		{
			TimeRange timeRange = new TimeRange();
			timeRange.Copy(m_VisibleTimeRange);
			return timeRange;
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

	public static int MinFrameLineWidth => 5;

	public Color FrameLineColour
	{
		get
		{
			return m_FrameLineColour;
		}
		set
		{
			m_FrameLineColour = value;
			m_FrameLinePen = new Pen(value);
		}
	}

	public event MeasureLineChangedHandler MeasureLineChanged;

	public Timeline()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: true);
		m_MeasureLine.Changed += MeasureLineChangedEvent;
		m_DPIScale = MainForm.DPIScale;
		base.Size = new Size(base.Width, ScaleDPI(30) + ScaleDPI(20));
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
		m_FrameBrush.Dispose();
		m_FrameLinePen.Dispose();
		m_FrameTextBrush.Dispose();
		m_MeasureLinePen.Dispose();
		m_MeasureLineFillBrush.Dispose();
		base.Dispose(disposing);
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
		m_SessionIsReady = true;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		try
		{
			e.Graphics.Clear(Colours.TimelieBackground);
			if (m_Session == null || !m_SessionIsReady || m_Session.TimerFrequency == 0L)
			{
				return;
			}
			if (m_VisibleTimeRange.m_TicksPerPixel != 0L)
			{
				long num = XToTime(base.Width) - XToTime(0);
				if (num < m_Session.TimerFrequency / 1000000)
				{
					DrawTimeIntervals(e.Graphics, 1000000000L, "ns", ScaleDPI(200), zero_on_first_frame: true);
				}
				else if (num < m_Session.TimerFrequency / 1000)
				{
					DrawTimeIntervals(e.Graphics, 1000000L, "μs", ScaleDPI(200), zero_on_first_frame: true);
				}
				else if (num < m_Session.TimerFrequency)
				{
					DrawTimeIntervals(e.Graphics, 1000L, "ms", ScaleDPI(100), zero_on_first_frame: true);
				}
				else
				{
					DrawTimeIntervals(e.Graphics, 1L, "s", ScaleDPI(100), zero_on_first_frame: false);
				}
				RenderFrames(e.Graphics);
			}
			m_MeasureLine.Draw(e.Graphics, base.Height, m_MeasureLinePen, m_MeasureLineFillBrush);
		}
		catch (Exception)
		{
		}
	}

	private void RenderFrames(Graphics graphics)
	{
		long num = base.ClientSize.Width * m_VisibleTimeRange.m_TicksPerPixel;
		m_VisibleFrames.Clear();
		m_Session.GetFrames(m_VisibleTimeRange.m_StartTime, m_VisibleTimeRange.m_StartTime + num, m_VisibleFrames);
		int count = m_VisibleFrames.Count;
		for (int i = 0; i < count; i++)
		{
			Frame frame = m_VisibleFrames[i];
			Frame frame2 = ((i < count - 1) ? m_VisibleFrames[i + 1] : null);
			int num2 = Math.Max(-2, TimeToX(frame.StartTime));
			int num3 = Math.Min(TimeToX(frame.EndTime), base.Width + 2);
			Rectangle rectangle = new Rectangle(num2, ScaleDPI(30), num3 - num2, ScaleDPI(20));
			if (rectangle.Width < ScaleDPI(5))
			{
				continue;
			}
			if (frame2 != null && m_Session.FrameProStall(frame, frame2))
			{
				graphics.FillRectangle(m_StallFrameBrush, rectangle);
				graphics.DrawLine(m_StallFramePen, rectangle.X, rectangle.Y, rectangle.X, rectangle.Bottom);
				graphics.DrawLine(m_StallFramePen, rectangle.Right, rectangle.Y, rectangle.Right, rectangle.Bottom);
				graphics.SetClip(rectangle);
				string timeString = Utils.GetTimeString(frame.WaitForSendCompleteTime, m_Session.TimerFrequency);
				string s = "Warning: FramePro stall of " + timeString + " due to too many scopes (check previous frames)";
				int num4 = (int)graphics.MeasureString(s, Font).Width;
				int num5 = Math.Max(rectangle.X, rectangle.X + rectangle.Width / 2 - num4 / 2);
				int num6 = rectangle.Y + rectangle.Height / 2 - Font.Height / 2;
				graphics.DrawString(s, Font, m_StallFrameTextBrush, num5, num6);
				graphics.ResetClip();
				continue;
			}
			if (frame2 != null && m_Session.FrameProTimeSpanSpike(frame, frame2))
			{
				graphics.FillRectangle(m_TimeSpanSpikeFrameBrush, rectangle);
				graphics.DrawLine(m_TimeSpanSpikeFramePen, rectangle.X, rectangle.Y, rectangle.X, rectangle.Bottom);
				graphics.DrawLine(m_TimeSpanSpikeFramePen, rectangle.Right, rectangle.Y, rectangle.Right, rectangle.Bottom);
				graphics.SetClip(rectangle);
				string s2 = "Warning: " + frame.TimeSpanCount + " scopes in frame. Please reduce the number of scopes.";
				int num7 = (int)graphics.MeasureString(s2, Font).Width;
				int num8 = Math.Max(rectangle.X, rectangle.X + rectangle.Width / 2 - num7 / 2);
				int num9 = rectangle.Y + rectangle.Height / 2 - Font.Height / 2;
				graphics.DrawString(s2, Font, m_TimeSpanSpikeFrameTextBrush, num8, num9);
				graphics.ResetClip();
				continue;
			}
			graphics.FillRectangle(m_FrameBrush, rectangle);
			graphics.DrawLine(m_FrameLinePen, rectangle.X, rectangle.Y, rectangle.X, rectangle.Bottom);
			graphics.DrawLine(m_FrameLinePen, rectangle.Right, rectangle.Y, rectangle.Right, rectangle.Bottom);
			string text = frame.Index.ToString();
			string s3 = "Frame: " + text;
			int num10 = (int)graphics.MeasureString(text, Font).Width;
			int num11 = (int)graphics.MeasureString(s3, Font).Width;
			int num12 = rectangle.Width - 2 * ScaleDPI(10);
			int num13 = rectangle.X + rectangle.Width / 2;
			int num14 = rectangle.Y + rectangle.Height / 2 - Font.Height / 2;
			if (num11 <= num12)
			{
				graphics.DrawString(s3, Font, m_FrameTextBrush, num13 - num11 / 2, num14);
			}
			else if (num10 <= num12)
			{
				graphics.DrawString(text, Font, m_FrameTextBrush, num13 - num10 / 2, num14);
			}
		}
	}

	private long GetVisibleDuration(Frame frame)
	{
		long num = base.ClientSize.Width * m_VisibleTimeRange.m_TicksPerPixel;
		long num2 = Math.Max(frame.StartTime, m_VisibleTimeRange.m_StartTime);
		return Math.Min(frame.EndTime, m_VisibleTimeRange.m_StartTime + num) - num2;
	}

	private void DrawTimeIntervals(Graphics graphics, long interval, string unit_text, int ideal_width, bool zero_on_first_frame)
	{
		long num = base.ClientSize.Width * m_VisibleTimeRange.m_TicksPerPixel;
		m_VisibleFrames.Clear();
		m_Session.GetFrames(m_VisibleTimeRange.m_StartTime, m_VisibleTimeRange.m_StartTime + num, m_VisibleFrames);
		long startTime = m_VisibleTimeRange.m_StartTime;
		if (m_VisibleFrames.Count != 0)
		{
			Frame frame = m_VisibleFrames[0];
			startTime = frame.StartTime;
			if (m_VisibleFrames.Count >= 2)
			{
				Frame frame2 = m_VisibleFrames[1];
				if (GetVisibleDuration(frame2) > GetVisibleDuration(frame))
				{
					startTime = frame2.StartTime;
				}
			}
		}
		int num2 = CalculateTimeStep(interval, ideal_width);
		long num3 = num2 * m_Session.TimerFrequency / interval;
		long num4 = startTime;
		int num5 = 0;
		while (num4 > m_VisibleTimeRange.m_StartTime - num3)
		{
			num4 -= num3;
			num5--;
		}
		while (num4 + num3 < m_VisibleTimeRange.m_StartTime)
		{
			num4 += num3;
			num5++;
		}
		long num6 = m_VisibleTimeRange.m_StartTime + num + num3;
		int num7 = GetIdealSubStepCount(num2);
		long num8 = num3 / num7;
		int num9 = TimeToPixels((float)num2 / (float)num7, interval);
		while (num9 > ideal_width && num7 < ScaleDPI(1000))
		{
			num7 *= 10;
			num9 = TimeToPixels((float)num2 / (float)num7, interval);
			num8 = num3 / num7;
		}
		bool flag = num9 >= 5;
		for (long num10 = num4; num10 < num6; num10 += num3)
		{
			int num11 = TimeToX(num10);
			if (num11 > -100 && num11 < base.Width + 100)
			{
				graphics.DrawLine(m_LinePen, num11, 0, num11, ScaleDPI(10));
				string text = (zero_on_first_frame ? (num5 * num2).ToString() : ((num10 - m_Session.FirstFrameTime) * interval / m_Session.TimerFrequency).ToString());
				text += unit_text;
				int num12 = (int)graphics.MeasureString(text, Font).Width;
				graphics.DrawString(text, Font, Brushes.Black, num11 - num12 / 2, ScaleDPI(10));
			}
			if (flag)
			{
				for (int i = 1; i < num7; i++)
				{
					int num13 = TimeToX(num10 + i * num8);
					if (num13 > -100 && num13 < base.Width + 100)
					{
						graphics.DrawLine(m_LinePen, num13, 0, num13, 5);
					}
				}
			}
			num5++;
		}
	}

	private int GetIdealSubStepCount(int step)
	{
		if (step != 5 && step != 50)
		{
			return 10;
		}
		return 5;
	}

	private int CalculateTimeStep(long interval, int ideal_width)
	{
		int result = 1;
		int num = int.MaxValue;
		for (int i = 0; i < m_TimeSteps.Length; i++)
		{
			int num2 = m_TimeSteps[i];
			int num3 = Math.Abs(TimeToPixels(num2, interval) - ideal_width);
			if (num3 > num)
			{
				break;
			}
			result = num2;
			num = num3;
		}
		return result;
	}

	private int TimeToPixels(int time, long interval)
	{
		return (int)(time * m_Session.TimerFrequency / (m_VisibleTimeRange.m_TicksPerPixel * interval));
	}

	private int TimeToPixels(float time, long interval)
	{
		return (int)(time * (float)m_Session.TimerFrequency / (float)(m_VisibleTimeRange.m_TicksPerPixel * interval));
	}

	private long XToTime(int x)
	{
		return x * m_VisibleTimeRange.m_TicksPerPixel + m_VisibleTimeRange.m_StartTime;
	}

	private int TimeToX(long time)
	{
		return (int)Misc.Clamp((time - m_VisibleTimeRange.m_StartTime) / m_VisibleTimeRange.m_TicksPerPixel, -2147483648L, 2147483647L);
	}

	public void SetTimeRange(TimeRange time_range)
	{
		if (!m_VisibleTimeRange.Equals(time_range))
		{
			m_VisibleTimeRange.CopyAllExceptScrollY(time_range);
			Refresh();
		}
	}

	public void SetMeasureLine(int start_x, int end_x)
	{
		m_MeasureLine.Set(start_x, end_x);
	}

	private Frame GetFrame(long time)
	{
		foreach (Frame visibleFrame in m_VisibleFrames)
		{
			if (time >= visibleFrame.StartTime && time < visibleFrame.EndTime)
			{
				return visibleFrame;
			}
		}
		return null;
	}

	private void UpdateFrameInfoBox(Point mouse_pos)
	{
		if (MainForm.Inst == null)
		{
			return;
		}
		if (m_MeasureLine.Dragging)
		{
			long start_time = XToTime(m_MeasureLine.StartX);
			long end_time = XToTime(m_MeasureLine.EndX);
			MeasureLine.UpdateTimerInfoBox(start_time, end_time, m_Session.TimerFrequency, PointToScreen(mouse_pos));
			return;
		}
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		bool visible = false;
		if (mouse_pos.Y > ScaleDPI(30))
		{
			long time = XToTime(mouse_pos.X);
			Frame frame = GetFrame(time);
			if (frame != null)
			{
				if (hoverBox.Target != frame)
				{
					hoverBox.Clear();
					hoverBox.Title = "Frame";
					hoverBox.AddLine("Frame", frame.Index.ToString());
					string timeString = Utils.GetTimeString(frame.Duration, m_Session.TimerFrequency);
					hoverBox.AddLine("Duration", timeString);
					hoverBox.AddLine("Scopes", frame.TimeSpanCount.ToString());
					hoverBox.AddLine("Bytes Sent", frame.BytesSent.ToString());
					hoverBox.SubmitLines();
				}
				Point location = PointToScreen(mouse_pos);
				hoverBox.SetLocation(location);
				visible = true;
			}
		}
		hoverBox.Visible = visible;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && Utils.IsShiftHeld)
		{
			m_MeasureLine.Dragging = true;
			base.Capture = true;
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		UpdateFrameInfoBox(e.Location);
		if (MouseHoverEnabled)
		{
			m_MeasureLine.HandleMouseMove(e.X);
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (m_MeasureLine.Dragging)
		{
			m_MeasureLine.Dragging = false;
			base.Capture = false;
		}
		base.OnMouseUp(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (!base.DesignMode && MainForm.Inst != null)
		{
			MainForm.Inst.HoverBox.Visible = false;
		}
		base.OnMouseLeave(e);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.Name = "Timeline";
		base.Size = new System.Drawing.Size(1085, 50);
		base.ResumeLayout(false);
	}
}
