using System;
using System.Drawing;

namespace ProfilerStudy;

internal class MeasureLine
{
	private int m_StartX;

	private int m_EndX;

	private bool m_Dragging;

	public bool Dragging
	{
		get
		{
			return m_Dragging;
		}
		set
		{
			m_Dragging = value;
		}
	}

	public int StartX => m_StartX;

	public int EndX => m_EndX;

	public event MeasureLineChangedHandler Changed;

	public void Draw(Graphics graphics, int height, Pen line_pen, Brush fill_brush)
	{
		int a = m_StartX;
		int b = m_EndX;
		if (a > b)
		{
			Utils.Swap(ref a, ref b);
		}
		graphics.DrawLine(line_pen, a, 0, a, height);
		int num = b - a;
		if (num > 0)
		{
			if (num > 1)
			{
				graphics.FillRectangle(fill_brush, a + 1, 0, num - 1, height);
			}
			graphics.DrawLine(line_pen, b, 0, b, height);
		}
	}

	public void HandleMouseMove(int x)
	{
		if (x != m_EndX)
		{
			Set(m_Dragging ? m_StartX : x, x);
		}
	}

	public void Set(int start_x, int end_x)
	{
		if (m_StartX != start_x || m_EndX != end_x)
		{
			m_StartX = start_x;
			m_EndX = end_x;
			if (this.Changed != null)
			{
				this.Changed(m_StartX, m_EndX);
			}
		}
	}

	public static void UpdateTimerInfoBox(long start_time, long end_time, long timer_frequency, Point screen_location)
	{
		if (MainForm.Inst == null)
		{
			return;
		}
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		long num = Math.Abs(end_time - start_time);
		if (num != 0L)
		{
			string timeString = Utils.GetTimeString(num, timer_frequency);
			if (hoverBox.Target == null || hoverBox.Target.ToString() != timeString)
			{
				hoverBox.Target = timeString;
				hoverBox.Clear();
				hoverBox.AddLine("Duration:", timeString);
				hoverBox.SubmitLines();
				hoverBox.Visible = true;
				hoverBox.SetLocation(screen_location);
			}
		}
		else
		{
			hoverBox.Visible = false;
		}
	}
}
