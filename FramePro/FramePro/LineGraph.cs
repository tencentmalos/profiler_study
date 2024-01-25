using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SCLCoreCLR;

namespace FramePro;

internal class LineGraph : UserControl
{
	private class DrawInputs
	{
		private List<Graph> m_Graphs = new List<Graph>();

		private XAxisMode m_XAxisMode;

		private Size m_ControlSize = new Size(0, 0);

		private long m_Start;

		private double m_XScale;

		private double m_YScale;

		private List<string> m_UnitsToScaleToHeight;

		private Dictionary<string, double> m_UnitYScales = new Dictionary<string, double>();

		private double m_YOffset;

		private long m_LastFrameEndTime;

		private int m_ForceId;

		public ICollection<Graph> Graphs => m_Graphs;

		public XAxisMode XAxisMode => m_XAxisMode;

		public Size ControlSize => m_ControlSize;

		public long Start => m_Start;

		public double XScale => m_XScale;

		public double YOffset => m_YOffset;

		public DrawInputs()
		{
		}

		public DrawInputs(List<Graph> graphs, XAxisMode x_axis_mode, Size control_size, long start, double x_axis_scale, double y_scale, List<string> units_to_scale_to_height, double y_offset, long last_frame_end_time, int force_id)
		{
			m_Graphs = new List<Graph>(graphs);
			m_XAxisMode = x_axis_mode;
			m_ControlSize = control_size;
			m_Start = start;
			m_XScale = x_axis_scale;
			m_YScale = y_scale;
			m_UnitsToScaleToHeight = new List<string>(units_to_scale_to_height);
			m_YOffset = y_offset;
			m_LastFrameEndTime = last_frame_end_time;
			m_ForceId = force_id;
		}

		public override bool Equals(object obj)
		{
			if (obj is DrawInputs drawInputs && Utils.ListsEquals(m_Graphs, drawInputs.m_Graphs) && m_XAxisMode == drawInputs.m_XAxisMode && m_ControlSize == drawInputs.m_ControlSize && m_Start == drawInputs.m_Start && m_XScale == drawInputs.m_XScale && m_YScale == drawInputs.m_YScale && Utils.ListsEquals(m_UnitsToScaleToHeight, drawInputs.m_UnitsToScaleToHeight) && m_YOffset == drawInputs.m_YOffset && m_LastFrameEndTime == drawInputs.m_LastFrameEndTime)
			{
				return m_ForceId == drawInputs.m_ForceId;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(m_Graphs) ^ m_XAxisMode.GetHashCode() ^ m_ControlSize.GetHashCode() ^ m_Start.GetHashCode() ^ m_XScale.GetHashCode() ^ m_YScale.GetHashCode() ^ m_YOffset.GetHashCode() ^ m_LastFrameEndTime.GetHashCode() ^ m_ForceId.GetHashCode();
		}

		public double GetYScale(string units)
		{
			double result = m_YScale;
			if (units != null && m_UnitYScales.TryGetValue(units, out var value))
			{
				result = value;
			}
			return result;
		}

		public bool ShouldScaleUnit(string unit)
		{
			return m_UnitsToScaleToHeight.Contains(unit);
		}

		public void SetUnitYScale(string unit, double y_scale)
		{
			m_UnitYScales[unit] = y_scale;
		}
	}

	private class GraphDrawingData
	{
		public long m_LastFrameIndex;

		public List<GraphDrawData> m_GraphDrawData = new List<GraphDrawData>();
	}

	private class GraphDrawData
	{
		public List<FrameValue> m_FrameValues = new List<FrameValue>();

		public List<PerSecValue> m_TimeValues = new List<PerSecValue>();

		public List<FrameStruct> m_Frames = new List<FrameStruct>();

		public Point[] m_Points = new Point[0];

		public double[] m_Values = new double[0];

		public Pen m_Pen;

		public string m_GraphName;

		public string m_Unit;

		public long m_GraphNameId;

		public XAxisMode m_XAxisMode;
	}

	private struct ValuePerSec
	{
		public double m_Value;

		public double m_Count;

		public ValuePerSec(double value, double count)
		{
			m_Value = value;
			m_Count = count;
		}
	}

	private Session m_Session;

	private float m_DPIScale;

	private Settings m_Settings;

	private const int m_DefaultHorzMargin = 55;

	private const int m_DefaultBottomMargin = 25;

	private int m_HorzMargin = 55;

	private int m_BottomMargin = 25;

	private const int m_YMarginWidth = 30;

	private const int m_AxisLineLength = 6;

	private int m_MainThreadId;

	private XAxisMode m_XAxisMode;

	private long m_StartFrameX;

	private long m_StartTime;

	private const double m_MinXScale = 0.0005;

	private const double m_MaxXScale = 50.0;

	private double m_FrameXScale = 1.0;

	private double m_AlignedFrameXScale = 1.0;

	private double m_TimeXScale = 0.03;

	private double m_YScale = 1.0;

	private double m_YOffset;

	private const double m_MinGraphYScale = 3E-09;

	private const double m_MaxGraphYScale = 10000000.0;

	private List<Graph> m_Graphs = new List<Graph>();

	private Thread m_CalculateViewThread;

	private AutoResetEvent m_CalculateEvent = new AutoResetEvent(initialState: false);

	private int m_ForceRecalculateId;

	private volatile bool m_Disposing;

	private object m_DrawInputsLock = new object();

	private DrawInputs m_DrawInputs = new DrawInputs();

	private DrawInputs m_CalculateThreadDrawInputs = new DrawInputs();

	private object m_DrawDataSwapLock = new object();

	private GraphDrawingData m_MainThreadDrawData = new GraphDrawingData();

	private GraphDrawingData m_CalculateThreadDrawData = new GraphDrawingData();

	private Font m_AxisFont = new Font("Ariel", 7f);

	private Pen m_AxisPen = new Pen(Colours.FrameValueGraphAxis);

	private Brush m_GraphFillBrush = new SolidBrush(Colours.FrameValueGraphFillColour);

	private Brush m_AxisBrush = new SolidBrush(Colours.FrameValueGraphAxis);

	private Brush m_HighlightAxisBrush = new SolidBrush(Colours.FrameValueGraphYAxisHighlight);

	private Brush m_SelectAxisBrush = new SolidBrush(Colours.FrameValueGraphYAxisSelect);

	private Pen m_YAxisMeasureLinePen = new Pen(Colours.FrameValueGraphYAxisMeasureLine);

	private bool m_DraggingHeight;

	private int m_DragHeightLastY;

	private int m_DraggingHeightOriginY;

	private int m_DraggingHeightMult;

	private bool m_HighlightYAxis;

	private int m_HighlightYAxisMouseY;

	private bool m_DraggingYOrigin;

	private int m_DraggingOriginLastY;

	private bool m_ScrollingX;

	private int m_ScrollingXLastX;

	private const int m_MinXAxisIntervalGap = 50;

	private const int m_MinYAxisIntervalGap = 20;

	private const double m_GraphYDragSpeed = 0.015;

	private const double m_YAxisMouseWheelZoomSpeed = 0.2;

	private const int m_HoverGraphLineDist = 10;

	private const double m_MouseWheelZoomSpeed = 1.1;

	private GetGraphValuesPerFrameFunction m_GetGraphValuesPerFrameFunction;

	private GetGraphValuesPerSecFunction m_GetGraphValuesPerSecFunction;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private bool m_SessionIsReady;

	private bool m_ChangedHeight;

	private string m_Unit = "";

	private string m_UnitToDisplay = "";

	private bool m_ConvertCyclesToMS;

	private const int m_FrameXMult = 1000;

	private bool m_ShowMouseHoverPoint;

	private Point m_HoverPointLocation;

	private Brush m_GraphHoverPointBrush = new SolidBrush(Colours.FrameValueGraphHoverPointColour);

	private const int m_HoverPointSize = 2;

	private const int m_MinYAxisHoverBoxWidth = 60;

	private const int m_MinXAxisHoverBoxWidth = 60;

	private Brush m_SessionStartEndRectBrush = new SolidBrush(Colours.SessionStartEndRectBrush);

	private bool m_DrawAxis = true;

	private bool m_AlignScale;

	private List<string> m_UnitsToScaleToHeight = new List<string>();

	private IContainer components;

	public bool NeedsUpdate => m_MainThreadDrawData.m_LastFrameIndex < XToFrameX(base.Width) / Session.FrameXPerFrame;

	public XAxisMode XAxisMode
	{
		get
		{
			return m_XAxisMode;
		}
		set
		{
			if (m_XAxisMode != value)
			{
				m_XAxisMode = value;
				RecalculateView();
			}
		}
	}

	public long XStart => m_XAxisMode switch
	{
		XAxisMode.Frame => m_StartFrameX, 
		XAxisMode.Time => m_StartTime, 
		_ => 0L, 
	};

	public double XScale => m_XAxisMode switch
	{
		XAxisMode.Frame => m_FrameXScale, 
		XAxisMode.Time => m_TimeXScale, 
		_ => 0.0, 
	};

	public string Unit
	{
		get
		{
			return m_Unit;
		}
		set
		{
			m_Unit = value;
			m_ConvertCyclesToMS = value != null && value.ToLower() == "cycles";
			SetDisplayUnitName(m_ConvertCyclesToMS ? "ms" : value);
		}
	}

	public string UnitToDisplay => m_UnitToDisplay;

	public double YScale
	{
		get
		{
			return m_YScale;
		}
		set
		{
			m_YScale = value;
			RecalculateView();
		}
	}

	private int XAxisY => base.Height - ScaleDPI(m_BottomMargin);

	public int GraphHeight => GetGraphRect().Height;

	public ICollection<Graph> Graphs => m_Graphs;

	public GetGraphValuesPerFrameFunction GetGraphValuesPerFrameFunction
	{
		get
		{
			return m_GetGraphValuesPerFrameFunction;
		}
		set
		{
			m_GetGraphValuesPerFrameFunction = value;
		}
	}

	public GetGraphValuesPerSecFunction GetGraphValuesPerSecFunction
	{
		get
		{
			return m_GetGraphValuesPerSecFunction;
		}
		set
		{
			m_GetGraphValuesPerSecFunction = value;
		}
	}

	public bool ChangedHeight => m_ChangedHeight;

	public double YOffset
	{
		get
		{
			return m_YOffset;
		}
		set
		{
			m_YOffset = value;
			RecalculateView();
		}
	}

	public bool DrawAxis
	{
		get
		{
			return m_DrawAxis;
		}
		set
		{
			m_DrawAxis = value;
			m_HorzMargin = (value ? 55 : 0);
			m_BottomMargin = (value ? 25 : 0);
		}
	}

	public bool AlignScale
	{
		get
		{
			return m_AlignScale;
		}
		set
		{
			m_AlignScale = value;
		}
	}

	public event StopTrackingEndHandler StopTrackingEnd;

	public event ValueGraphTimeRangeChangedHandler TimeRangeChanged;

	public event DisplayUnitChangedHandler DisplayUnitChanged;

	public LineGraph()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		m_MainThreadId = Thread.CurrentThread.ManagedThreadId;
		m_CalculateViewThread = new Thread(CalculateViewThread);
		m_CalculateViewThread.Name = "LineGraph";
		m_DPIScale = MainForm.DPIScale;
		m_FrameXScale *= m_DPIScale;
		m_AlignedFrameXScale *= m_DPIScale;
		m_TimeXScale *= m_DPIScale;
		m_YScale *= m_DPIScale;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	public void SetSessionIsReady()
	{
		m_SessionIsReady = true;
		m_StartTime = m_Session.FirstFrameTime;
		RecalculateView();
	}

	private void OnTimeRangeChanged()
	{
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
			if (this.TimeRangeChanged != null)
			{
				this.TimeRangeChanged(this, m_StartFrameX, XToFrameX(base.Width), m_FrameXScale);
			}
			break;
		case XAxisMode.Time:
			if (this.TimeRangeChanged != null)
			{
				this.TimeRangeChanged(this, m_StartTime, XToTime(base.Width, m_Session.TimerFrequency), m_TimeXScale);
			}
			break;
		}
	}

	public void ResetXAxisZoom()
	{
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
		{
			double x_scale2 = (double)(base.Width - ScaleDPI(m_HorzMargin)) / (double)m_Session.FrameCount;
			SetTimeRange(0L, x_scale2);
			break;
		}
		case XAxisMode.Time:
		{
			double x_scale = (double)(base.Width - ScaleDPI(m_HorzMargin)) / (double)(m_Session.TotalConnectTime * 1000 / m_Session.TimerFrequency);
			SetTimeRange(m_Session.FirstFrameTime, x_scale);
			break;
		}
		}
	}

	public void SetTimeRange(long start, long end)
	{
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
		{
			double x_scale2 = (double)((base.Width - ScaleDPI(m_HorzMargin)) * 1000) / (double)(end - start);
			SetTimeRange(start, x_scale2);
			break;
		}
		case XAxisMode.Time:
		{
			double x_scale = (base.Width - ScaleDPI(m_HorzMargin)) * m_Session.TimerFrequency / (1000 * (end - start));
			SetTimeRange(start, x_scale);
			break;
		}
		}
	}

	public void SetTimeRange(long start, double x_scale)
	{
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
			if (m_StartFrameX != start || m_FrameXScale != x_scale)
			{
				m_StartFrameX = start;
				m_FrameXScale = Misc.Clamp(x_scale, 1.0, 1000.0);
				m_AlignedFrameXScale = ((m_FrameXScale > 1.0) ? Math.Round(m_FrameXScale) : m_FrameXScale);
				m_AlignedFrameXScale = Math.Max(1.0, m_AlignedFrameXScale);
				RecalculateView();
			}
			break;
		case XAxisMode.Time:
			if (m_StartTime != start || m_TimeXScale != x_scale)
			{
				m_StartTime = start;
				m_TimeXScale = x_scale;
				RecalculateView();
			}
			break;
		}
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	public void SetSession(Session session)
	{
		m_Session = session;
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
	}

	protected override void OnCreateControl()
	{
		base.OnCreateControl();
		if (!base.DesignMode)
		{
			m_CalculateViewThread.Start();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_Disposing = true;
		m_CalculateEvent.Set();
		base.Dispose(disposing);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private Rectangle GetGraphRect()
	{
		return new Rectangle(ScaleDPI(m_HorzMargin), 0, base.Width - ScaleDPI(m_HorzMargin), XAxisY);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.Clear(Colours.FrameValueGraphBackgroundColour);
		Rectangle graphRect = GetGraphRect();
		e.Graphics.FillRectangle(m_GraphFillBrush, graphRect);
		if (base.DesignMode || !m_SessionIsReady)
		{
			return;
		}
		lock (m_DrawDataSwapLock)
		{
			e.Graphics.SetClip(graphRect);
			foreach (GraphDrawData graphDrawDatum in m_MainThreadDrawData.m_GraphDrawData)
			{
				if (graphDrawDatum.m_Points.Length > 1)
				{
					e.Graphics.DrawLines(graphDrawDatum.m_Pen, graphDrawDatum.m_Points);
				}
			}
			e.Graphics.ResetClip();
			if (m_DrawAxis)
			{
				DrawXAxis(e.Graphics);
				DrawYAxis(e.Graphics);
			}
		}
		if (m_DrawAxis && m_HighlightYAxis && !m_DraggingHeight && !m_DraggingYOrigin)
		{
			e.Graphics.DrawLine(m_YAxisMeasureLinePen, ScaleDPI(m_HorzMargin), m_HighlightYAxisMouseY, base.Width, m_HighlightYAxisMouseY);
		}
		if (m_ShowMouseHoverPoint)
		{
			Rectangle rect = new Rectangle(m_HoverPointLocation.X - 1, m_HoverPointLocation.Y - 1, 2, 2);
			e.Graphics.FillRectangle(m_GraphHoverPointBrush, rect);
		}
		DrawSessionStartEndRects(e.Graphics);
	}

	private void DrawSessionStartEndRects(Graphics graphics)
	{
		int num = 0;
		int num2 = 0;
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
			num = FrameXToX(0L, m_DrawInputs);
			num2 = FrameXToX(m_Session.FrameCount * 1000, m_DrawInputs);
			break;
		case XAxisMode.Time:
			num = TimeToX(m_Session.FirstFrameTime, m_Session.TimerFrequency, m_DrawInputs);
			num2 = TimeToX(m_Session.LastFrameEndTime, m_Session.TimerFrequency, m_DrawInputs);
			break;
		}
		if (num > 0)
		{
			graphics.FillRectangle(rect: new Rectangle(ScaleDPI(m_HorzMargin), 0, num - ScaleDPI(m_HorzMargin), XAxisY), brush: m_SessionStartEndRectBrush);
		}
		if (num2 < base.Width)
		{
			graphics.FillRectangle(rect: new Rectangle(num2, 0, base.Width - num2, XAxisY), brush: m_SessionStartEndRectBrush);
		}
	}

	private int FrameXToX(long frame_x, DrawInputs draw_inputs)
	{
		return ScaleDPI(m_HorzMargin) + (int)((double)(frame_x - m_StartFrameX) * draw_inputs.XScale / 1000.0);
	}

	private long XToFrameX(int x, DrawInputs draw_inputs)
	{
		return draw_inputs.Start + (long)((double)((x - ScaleDPI(m_HorzMargin)) * 1000) / draw_inputs.XScale);
	}

	private long XToFrameX(int x)
	{
		double num = (m_AlignScale ? m_AlignedFrameXScale : m_FrameXScale);
		return m_StartFrameX + (long)((double)((x - ScaleDPI(m_HorzMargin)) * 1000) / num);
	}

	private int TimeToX(long time, long timer_frequency, DrawInputs draw_inputs)
	{
		return ScaleDPI(m_HorzMargin) + (int)((double)(time - draw_inputs.Start) * 1000.0 / (double)timer_frequency * draw_inputs.XScale);
	}

	private long XToTime(int x, long timer_frequency, DrawInputs draw_inputs)
	{
		return draw_inputs.Start + (long)((double)(x - ScaleDPI(m_HorzMargin)) / draw_inputs.XScale * (double)timer_frequency / 1000.0);
	}

	private long XToTime(int x, long timer_frequency)
	{
		return m_StartTime + (long)((double)(x - ScaleDPI(m_HorzMargin)) / m_TimeXScale * (double)timer_frequency / 1000.0);
	}

	private void DrawXAxis(Graphics graphics)
	{
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
			DrawFrameXAxis(graphics);
			break;
		case XAxisMode.Time:
			DrawTimeXAxis(graphics);
			break;
		}
	}

	private void DrawFrameXAxis(Graphics graphics)
	{
		lock (m_DrawInputsLock)
		{
			graphics.DrawLine(m_AxisPen, ScaleDPI(m_HorzMargin) - 1, XAxisY, base.Width, XAxisY);
			int num = base.Width - ScaleDPI(m_HorzMargin);
			double num2 = (long)((double)num / m_DrawInputs.XScale);
			Interval bestInterval = MiscUnits.GetBestInterval(num2, num, ScaleDPI(50), "Frames");
			double num3 = bestInterval.StepSize * 1000.0;
			long num4 = (long)(((double)m_DrawInputs.Start + num3 - 1.0) / num3);
			long num5 = (long)(((double)m_DrawInputs.Start + num2 * 1000.0) / num3);
			for (long num6 = num4; num6 <= num5; num6++)
			{
				long frame_x = (long)((double)num6 * num3);
				int num7 = FrameXToX(frame_x, m_DrawInputs);
				graphics.DrawLine(m_AxisPen, num7, XAxisY, num7, base.Height - (ScaleDPI(m_BottomMargin) - ScaleDPI(6)));
				string s = ((double)num6 * bestInterval.Step).ToString();
				int num8 = (int)graphics.MeasureString(s, m_AxisFont).Width;
				graphics.DrawString(s, m_AxisFont, m_AxisBrush, num7 - num8 / 2, base.Height - (ScaleDPI(m_BottomMargin) - ScaleDPI(6) - 2));
			}
		}
	}

	private void DrawTimeXAxis(Graphics graphics)
	{
		graphics.DrawLine(m_AxisPen, ScaleDPI(m_HorzMargin) - 1, XAxisY, base.Width, XAxisY);
		int num = base.Width - ScaleDPI(m_HorzMargin);
		long timerFrequency = m_Session.TimerFrequency;
		long num2 = Utils.ToNanoSec((long)((double)(num * timerFrequency) / (m_TimeXScale * 1000.0)), timerFrequency);
		Interval bestInterval = TimeUnits.GetBestInterval(num2, num, ScaleDPI(50));
		long num3 = Utils.ToNanoSec(m_DrawInputs.Start - m_Session.FirstFrameTime, timerFrequency);
		long num4 = (long)(((double)num3 + bestInterval.StepSize - 1.0) / bestInterval.StepSize);
		long num5 = (long)((double)(num3 + num2) / bestInterval.StepSize);
		for (long num6 = num4; num6 <= num5; num6++)
		{
			long nano_sec = (long)((double)num6 * bestInterval.StepSize);
			long time = m_Session.FirstFrameTime + Utils.FromNanoSec(nano_sec, timerFrequency);
			int num7 = TimeToX(time, timerFrequency, m_DrawInputs);
			graphics.DrawLine(m_AxisPen, num7, XAxisY, num7, base.Height - (ScaleDPI(m_BottomMargin) - ScaleDPI(6)));
			string s = (double)num6 * bestInterval.Step + " " + bestInterval.UnitName;
			int num8 = (int)graphics.MeasureString(s, m_AxisFont).Width;
			graphics.DrawString(s, m_AxisFont, m_AxisBrush, num7 - num8 / 2, base.Height - (ScaleDPI(m_BottomMargin) - ScaleDPI(6) - 2));
		}
	}

	private void DrawYAxis(Graphics graphics)
	{
		DrawInputs drawInputs = m_DrawInputs;
		if (m_HighlightYAxis || m_DraggingHeight || m_DraggingYOrigin)
		{
			Brush brush = ((m_DraggingHeight || m_DraggingYOrigin) ? m_SelectAxisBrush : m_HighlightAxisBrush);
			Rectangle rect = new Rectangle(ScaleDPI(m_HorzMargin) - ScaleDPI(30), 0, ScaleDPI(30), base.Height);
			graphics.FillRectangle(brush, rect);
		}
		graphics.DrawLine(m_AxisPen, ScaleDPI(m_HorzMargin) - 1, XAxisY + 1, ScaleDPI(m_HorzMargin) - 1, 0);
		Interval appropiateYInterval = GetAppropiateYInterval();
		int num = (int)((double)XAxisY / m_YScale / appropiateYInterval.StepSize) + 1;
		int num2 = (int)(drawInputs.YOffset / appropiateYInterval.StepSize);
		for (int i = 0; i < num; i++)
		{
			double num3 = (double)(i + num2) * appropiateYInterval.Step;
			double value = num3 * (double)appropiateYInterval.Unit;
			int num4 = ValueToY(value, drawInputs, null);
			if (num4 >= -m_AxisFont.Height && num4 <= base.Height + m_AxisFont.Height)
			{
				graphics.DrawLine(m_AxisPen, ScaleDPI(m_HorzMargin) - ScaleDPI(6), num4, ScaleDPI(m_HorzMargin), num4);
				int num5 = (int)graphics.MeasureString(num3.ToString(), m_AxisFont).Width;
				graphics.DrawString(num3.ToString(), m_AxisFont, m_AxisBrush, ScaleDPI(m_HorzMargin) - ScaleDPI(6) - 2 - num5, num4 - m_AxisFont.Height / 2);
			}
		}
		string s = appropiateYInterval.UnitName + GetAXisTitleString();
		SetDisplayUnitName(appropiateYInterval.UnitName);
		StringFormat stringFormat = new StringFormat(StringFormatFlags.DirectionVertical);
		SizeF sizeF = graphics.MeasureString(s, m_AxisFont, new PointF(0f, 0f), stringFormat);
		int num6 = 4;
		int num7 = (base.ClientSize.Height - (int)sizeF.Height) / 2;
		graphics.DrawString(s, m_AxisFont, m_AxisBrush, num6, num7, stringFormat);
	}

	private string GetAXisTitleString()
	{
		return m_XAxisMode switch
		{
			XAxisMode.Frame => " / Frame", 
			XAxisMode.Time => " / Sec", 
			_ => "error", 
		};
	}

	private void SetDisplayUnitName(string unit_name)
	{
		if (m_UnitToDisplay != unit_name)
		{
			m_UnitToDisplay = unit_name;
			if (this.DisplayUnitChanged != null)
			{
				this.DisplayUnitChanged();
			}
		}
	}

	private Interval GetAppropiateYInterval()
	{
		int num = base.ClientSize.Height - ScaleDPI(m_BottomMargin);
		double num2 = (double)num / m_YScale;
		if (m_Unit.ToLower() == "bytes")
		{
			return MemoryUnits.GetBestInterval(num2, num, ScaleDPI(20));
		}
		return MiscUnits.GetBestInterval(num2, num, ScaleDPI(20), m_UnitToDisplay);
	}

	private void CalculateViewThread()
	{
		while (!m_Disposing)
		{
			m_CalculateEvent.WaitOne();
			bool flag = false;
			lock (m_DrawInputsLock)
			{
				if (!m_CalculateThreadDrawInputs.Equals(m_DrawInputs))
				{
					m_CalculateThreadDrawInputs = m_DrawInputs;
					flag = true;
				}
			}
			if (flag)
			{
				CalculateView();
			}
		}
	}

	private static bool Equal(List<Graph> graphs_a, List<Graph> graphs_b)
	{
		int count = graphs_a.Count;
		if (graphs_b.Count != count)
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			if (!graphs_a[i].Equals(graphs_b[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool SetGraphs(List<Graph> graphs)
	{
		if (!Equal(m_Graphs, graphs))
		{
			m_Graphs.Clear();
			m_Graphs.AddRange(graphs);
			RecalculateView();
			return true;
		}
		return false;
	}

	public void RecalculateView()
	{
		RecalculateView(force: false);
	}

	public void RecalculateView(bool force)
	{
		if (m_Settings != null && m_SessionIsReady && MainForm.Inst.WindowState != FormWindowState.Minimized)
		{
			long start;
			double x_axis_scale;
			switch (m_XAxisMode)
			{
			case XAxisMode.Frame:
				start = m_StartFrameX;
				x_axis_scale = (m_AlignScale ? m_AlignedFrameXScale : m_FrameXScale);
				break;
			case XAxisMode.Time:
				start = m_StartTime;
				x_axis_scale = m_TimeXScale;
				break;
			default:
				start = 0L;
				x_axis_scale = 0.0;
				break;
			}
			if (force)
			{
				m_ForceRecalculateId++;
			}
			lock (m_DrawInputsLock)
			{
				m_DrawInputs = new DrawInputs(m_Graphs, m_XAxisMode, base.Size, start, x_axis_scale, m_YScale, m_UnitsToScaleToHeight, m_YOffset, m_Session.LastFrameEndTime, m_ForceRecalculateId);
			}
			m_CalculateEvent.Set();
		}
	}

	private void CalculateView()
	{
		switch (m_CalculateThreadDrawInputs.XAxisMode)
		{
		case XAxisMode.Frame:
			CalculatePerFrameView();
			break;
		case XAxisMode.Time:
			CalculatePerSecView();
			break;
		}
		lock (m_DrawDataSwapLock)
		{
			Misc.Swap(ref m_MainThreadDrawData, ref m_CalculateThreadDrawData);
		}
		m_ControlTaskDispatcher.QueueTask(Refresh);
	}

	private void CalculatePerFrameView()
	{
		List<GraphDrawData> list = new List<GraphDrawData>(m_CalculateThreadDrawData.m_GraphDrawData);
		m_CalculateThreadDrawData.m_GraphDrawData.Clear();
		if (m_Session == null)
		{
			return;
		}
		DrawInputs calculateThreadDrawInputs = m_CalculateThreadDrawInputs;
		foreach (Graph graph in calculateThreadDrawInputs.Graphs)
		{
			GraphDrawData graphDrawData = ((list.Count != 0) ? Utils.RemoveFirst(list) : new GraphDrawData());
			graphDrawData.m_GraphName = graph.m_StatName;
			graphDrawData.m_Unit = graph.m_Unit;
			graphDrawData.m_GraphNameId = graph.m_NameId;
			graphDrawData.m_XAxisMode = graph.m_XAxisMode;
			graphDrawData.m_Pen = new Pen(graph.m_Colour);
			m_CalculateThreadDrawData.m_GraphDrawData.Add(graphDrawData);
		}
		int frameCount = m_Session.FrameCount;
		if (frameCount == 0)
		{
			return;
		}
		long timerFrequency = m_Session.TimerFrequency;
		_ = calculateThreadDrawInputs.ControlSize.Width;
		ScaleDPI(m_HorzMargin);
		int value = (int)(calculateThreadDrawInputs.Start / 1000);
		int value2 = (int)((XToFrameX(base.Width, calculateThreadDrawInputs) + 1000 - 1) / 1000);
		value = Misc.Clamp(value, 0, frameCount - 1);
		value2 = Misc.Clamp(value2, 0, frameCount - 1);
		m_CalculateThreadDrawData.m_LastFrameIndex = Math.Min(m_Session.FrameCount, value2);
		foreach (GraphDrawData graphDrawDatum in m_CalculateThreadDrawData.m_GraphDrawData)
		{
			switch (graphDrawDatum.m_XAxisMode)
			{
			case XAxisMode.Frame:
				GetGraphStatsOverFrames(graphDrawDatum, value, value2);
				break;
			case XAxisMode.Time:
				GetGraphTimeStatOverFrames(graphDrawDatum, value, value2, timerFrequency);
				break;
			}
		}
		bool convertCyclesToMS = m_ConvertCyclesToMS;
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		foreach (GraphDrawData graphDrawDatum2 in m_CalculateThreadDrawData.m_GraphDrawData)
		{
			string unit = graphDrawDatum2.m_Unit;
			if (!calculateThreadDrawInputs.ShouldScaleUnit(unit))
			{
				continue;
			}
			double num = 0.0;
			int num2 = 0;
			for (int i = value; i <= value2; i++)
			{
				FrameValue frameValue = graphDrawDatum2.m_FrameValues[num2];
				num2++;
				long frame_x = i * 1000;
				FrameXToX(frame_x, calculateThreadDrawInputs);
				double num3 = frameValue.m_Value;
				if (convertCyclesToMS)
				{
					num3 = num3 * 1000.0 / (double)timerFrequency;
				}
				if (num3 > num)
				{
					num = num3;
				}
			}
			if (dictionary.TryGetValue(unit, out var value3))
			{
				if (num > value3)
				{
					dictionary[unit] = num;
				}
			}
			else
			{
				dictionary[unit] = num;
			}
		}
		foreach (string key in dictionary.Keys)
		{
			double num4 = dictionary[key];
			double y_scale = ((num4 != 0.0) ? ((double)calculateThreadDrawInputs.ControlSize.Height / num4) : 1.0);
			calculateThreadDrawInputs.SetUnitYScale(key, y_scale);
		}
		foreach (GraphDrawData graphDrawDatum3 in m_CalculateThreadDrawData.m_GraphDrawData)
		{
			switch (graphDrawDatum3.m_XAxisMode)
			{
			case XAxisMode.Frame:
				GraphStatsOverFrames(graphDrawDatum3, calculateThreadDrawInputs, value, value2, timerFrequency);
				break;
			case XAxisMode.Time:
				GraphTimeStatOverFrames(graphDrawDatum3, calculateThreadDrawInputs, value, value2, timerFrequency);
				break;
			}
		}
	}

	private void GetGraphStatsOverFrames(GraphDrawData graph_draw_data, int first_frame_index, int last_frame_index)
	{
		graph_draw_data.m_FrameValues.Clear();
		m_GetGraphValuesPerFrameFunction(first_frame_index, last_frame_index, graph_draw_data.m_GraphNameId, graph_draw_data.m_FrameValues);
	}

	private void GraphStatsOverFrames(GraphDrawData graph_draw_data, DrawInputs draw_inputs, int first_frame_index, int last_frame_index, long timer_frequency)
	{
		bool convertCyclesToMS = m_ConvertCyclesToMS;
		int num = last_frame_index + 1 - first_frame_index;
		int num2 = graph_draw_data.m_Points.Length - num;
		if (num2 != 0)
		{
			int num3 = ((graph_draw_data.m_Points.Length != 0) ? (num2 * 100 / graph_draw_data.m_Points.Length) : 100);
			if (num2 < 0 || num3 > 10)
			{
				graph_draw_data.m_Points = new Point[num];
				graph_draw_data.m_Values = new double[num];
			}
		}
		int num4 = 0;
		for (int i = first_frame_index; i <= last_frame_index; i++)
		{
			FrameValue frameValue = graph_draw_data.m_FrameValues[num4];
			num4++;
			long frame_x = i * 1000;
			int value = FrameXToX(frame_x, draw_inputs);
			double num5 = frameValue.m_Value;
			if (convertCyclesToMS)
			{
				num5 = num5 * 1000.0 / (double)timer_frequency;
			}
			int value2 = ValueToY(num5, draw_inputs, graph_draw_data.m_Unit);
			value = Misc.Clamp(value, -1000000, 1000000);
			value2 = Misc.Clamp(value2, -1000000, 1000000);
			int num6 = i - first_frame_index;
			graph_draw_data.m_Points[num6] = new Point(value, value2);
			graph_draw_data.m_Values[num6] = num5;
		}
		int num7 = graph_draw_data.m_Points.Length - num;
		for (int j = 0; j < num7; j++)
		{
			long frame_x2 = (last_frame_index + j) * 1000;
			int value3 = FrameXToX(frame_x2, draw_inputs);
			int value4 = ValueToY(0.0, draw_inputs, graph_draw_data.m_Unit);
			value3 = Misc.Clamp(value3, -1000000, 1000000);
			value4 = Misc.Clamp(value4, -1000000, 1000000);
			int num8 = num + j;
			graph_draw_data.m_Points[num8] = new Point(value3, value4);
			graph_draw_data.m_Values[num8] = 0.0;
		}
	}

	private void GetGraphTimeStatOverFrames(GraphDrawData graph_draw_data, int first_frame_index, int last_frame_index, long timer_frequency)
	{
		Frame frame = m_Session.GetFrame(first_frame_index);
		Frame frame2 = m_Session.GetFrame(last_frame_index);
		long start_time = frame?.StartTime ?? m_Session.FirstFrameTime;
		long end_time = frame2?.EndTime ?? m_Session.LastFrameEndTime;
		graph_draw_data.m_TimeValues.Clear();
		m_GetGraphValuesPerSecFunction(graph_draw_data.m_GraphNameId, start_time, end_time, graph_draw_data.m_TimeValues, out var first_interval_time);
		graph_draw_data.m_Frames.Clear();
		m_Session.GetFrames(first_frame_index, last_frame_index, graph_draw_data.m_Frames);
		graph_draw_data.m_FrameValues.Clear();
		foreach (FrameStruct frame3 in graph_draw_data.m_Frames)
		{
			int num = (int)((frame3.m_EndTime - first_interval_time) * 1000 / timer_frequency / ValuePerSecArray.IntervalInMs);
			double value = 0.0;
			double count = 0.0;
			if (num > 0 && num < graph_draw_data.m_TimeValues.Count)
			{
				value = graph_draw_data.m_TimeValues[num].m_Value;
				count = graph_draw_data.m_TimeValues[num].m_Count;
			}
			FrameValue item = new FrameValue(frame3.m_EndTime, value, count);
			graph_draw_data.m_FrameValues.Add(item);
		}
	}

	private void GraphTimeStatOverFrames(GraphDrawData graph_draw_data, DrawInputs draw_inputs, int first_frame_index, int last_frame_index, long timer_frequency)
	{
		bool convertCyclesToMS = m_ConvertCyclesToMS;
		int count = graph_draw_data.m_Frames.Count;
		int num = graph_draw_data.m_Points.Length - count;
		if (num != 0)
		{
			int num2 = ((graph_draw_data.m_Points.Length != 0) ? (num * 100 / graph_draw_data.m_Points.Length) : 100);
			if (num < 0 || num2 > 10)
			{
				graph_draw_data.m_Points = new Point[count];
				graph_draw_data.m_Values = new double[count];
			}
		}
		long num3 = draw_inputs.Start / 1000 * 1000;
		int i = 0;
		foreach (FrameValue frameValue in graph_draw_data.m_FrameValues)
		{
			long frame_x = num3 + i * 1000;
			int num4 = FrameXToX(frame_x, draw_inputs);
			double num5 = frameValue.m_Value;
			if (convertCyclesToMS)
			{
				num5 = num5 * 1000.0 / (double)timer_frequency;
			}
			int num6 = ValueToY(num5, draw_inputs, graph_draw_data.m_Unit);
			graph_draw_data.m_Points[i] = new Point(num4, num6);
			graph_draw_data.m_Values[i] = num5;
			i++;
		}
		if (i > 0 && i < graph_draw_data.m_Points.Length)
		{
			Point point = graph_draw_data.m_Points[i - 1];
			double num7 = graph_draw_data.m_Values[i - 1];
			for (; i < graph_draw_data.m_Points.Length; i++)
			{
				graph_draw_data.m_Points[i] = point;
				graph_draw_data.m_Values[i] = num7;
			}
		}
	}

	private void CalculatePerSecView()
	{
		List<GraphDrawData> list = new List<GraphDrawData>(m_CalculateThreadDrawData.m_GraphDrawData);
		m_CalculateThreadDrawData.m_GraphDrawData.Clear();
		if (m_Session == null)
		{
			return;
		}
		DrawInputs calculateThreadDrawInputs = m_CalculateThreadDrawInputs;
		foreach (Graph graph in calculateThreadDrawInputs.Graphs)
		{
			GraphDrawData graphDrawData = ((list.Count != 0) ? Utils.RemoveFirst(list) : new GraphDrawData());
			graphDrawData.m_GraphName = graph.m_StatName;
			graphDrawData.m_GraphNameId = graph.m_NameId;
			graphDrawData.m_Pen = new Pen(graph.m_Colour);
			m_CalculateThreadDrawData.m_GraphDrawData.Add(graphDrawData);
		}
		long timerFrequency = m_Session.TimerFrequency;
		int num = calculateThreadDrawInputs.ControlSize.Width - ScaleDPI(m_HorzMargin);
		foreach (GraphDrawData graphDrawDatum in m_CalculateThreadDrawData.m_GraphDrawData)
		{
			long start = calculateThreadDrawInputs.Start;
			long end_time = XToTime(ScaleDPI(m_HorzMargin) + num, timerFrequency, calculateThreadDrawInputs);
			switch (graphDrawDatum.m_XAxisMode)
			{
			case XAxisMode.Frame:
				GraphFrameStatsOverTime(graphDrawDatum, calculateThreadDrawInputs, start, end_time, timerFrequency);
				break;
			case XAxisMode.Time:
				GraphTimeStatOverTime(graphDrawDatum, calculateThreadDrawInputs, start, end_time, timerFrequency);
				break;
			}
		}
	}

	private void GraphFrameStatsOverTime(GraphDrawData graph_draw_data, DrawInputs draw_inputs, long start_time, long end_time, long timer_frequency)
	{
		bool convertCyclesToMS = m_ConvertCyclesToMS;
		int start_frame_index = Math.Max(0, m_Session.GetFrameIndex(start_time));
		int end_frame_index = Math.Min(m_Session.GetFrameIndex(end_time), m_Session.FrameCount - 1);
		graph_draw_data.m_FrameValues.Clear();
		m_GetGraphValuesPerFrameFunction(start_frame_index, end_frame_index, graph_draw_data.m_GraphNameId, graph_draw_data.m_FrameValues);
		int count = graph_draw_data.m_FrameValues.Count;
		int num = graph_draw_data.m_Points.Length - count;
		if (num != 0)
		{
			int num2 = ((graph_draw_data.m_Points.Length != 0) ? (num * 100 / graph_draw_data.m_Points.Length) : 100);
			if (num < 0 || num2 > 10)
			{
				graph_draw_data.m_Points = new Point[count];
				graph_draw_data.m_Values = new double[count];
			}
		}
		_ = draw_inputs.Start / 1000;
		int i = 0;
		foreach (FrameValue frameValue in graph_draw_data.m_FrameValues)
		{
			long frameEndTime = frameValue.m_FrameEndTime;
			int num3 = TimeToX(frameEndTime, timer_frequency, draw_inputs);
			double num4 = frameValue.m_Value;
			if (convertCyclesToMS)
			{
				num4 = num4 * 1000.0 / (double)timer_frequency;
			}
			int num5 = ValueToY(num4, draw_inputs, graph_draw_data.m_Unit);
			graph_draw_data.m_Points[i] = new Point(num3, num5);
			graph_draw_data.m_Values[i] = num4;
			i++;
		}
		if (i > 0 && i < graph_draw_data.m_Points.Length)
		{
			Point point = graph_draw_data.m_Points[i - 1];
			double num6 = graph_draw_data.m_Values[i - 1];
			for (; i < graph_draw_data.m_Points.Length; i++)
			{
				graph_draw_data.m_Points[i] = point;
				graph_draw_data.m_Values[i] = num6;
			}
		}
	}

	private void GraphTimeStatOverTime(GraphDrawData graph_draw_data, DrawInputs draw_inputs, long start_time, long end_time, long timer_frequency)
	{
		bool convertCyclesToMS = m_ConvertCyclesToMS;
		graph_draw_data.m_TimeValues.Clear();
		m_GetGraphValuesPerSecFunction(graph_draw_data.m_GraphNameId, start_time, end_time, graph_draw_data.m_TimeValues, out var first_interval_time);
		int count = graph_draw_data.m_TimeValues.Count;
		int num = graph_draw_data.m_Points.Length - count;
		if (num < 0 || num * 100 / graph_draw_data.m_Points.Length > 10)
		{
			graph_draw_data.m_Points = new Point[count];
			graph_draw_data.m_Values = new double[count];
		}
		int num2 = 0;
		int i = 0;
		foreach (PerSecValue timeValue in graph_draw_data.m_TimeValues)
		{
			long time = first_interval_time + num2 * timer_frequency / 1000;
			int num3 = TimeToX(time, timer_frequency, draw_inputs);
			double num4 = timeValue.m_Value;
			if (convertCyclesToMS)
			{
				num4 = num4 * 1000.0 / (double)timer_frequency;
			}
			int num5 = ValueToY(num4, draw_inputs, graph_draw_data.m_Unit);
			graph_draw_data.m_Points[i] = new Point(num3, num5);
			graph_draw_data.m_Values[i] = num4;
			i++;
			num2 += ValuePerSecArray.IntervalInMs;
		}
		if (i > 0 && i < graph_draw_data.m_Points.Length)
		{
			Point point = graph_draw_data.m_Points[i - 1];
			double num6 = graph_draw_data.m_Values[i - 1];
			for (; i < graph_draw_data.m_Points.Length; i++)
			{
				graph_draw_data.m_Points[i] = point;
				graph_draw_data.m_Values[i] = num6;
			}
		}
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		if (MainForm.Inst != null && MainForm.Inst.WindowState != FormWindowState.Minimized)
		{
			RecalculateView();
		}
	}

	private int ValueToY(double value, DrawInputs draw_inputs, string unit)
	{
		return draw_inputs.ControlSize.Height - ScaleDPI(m_BottomMargin) - (int)(draw_inputs.GetYScale(unit) * (value - draw_inputs.YOffset));
	}

	private double YToValueDouble(int y, DrawInputs draw_inputs, string unit)
	{
		return (double)(draw_inputs.ControlSize.Height - ScaleDPI(m_BottomMargin) - y) / draw_inputs.GetYScale(unit) + draw_inputs.YOffset;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		MainForm.Inst.HoverBox.Hide();
		bool flag = false;
		if (m_ShowMouseHoverPoint)
		{
			m_ShowMouseHoverPoint = false;
			flag = true;
		}
		if (m_HighlightYAxis)
		{
			m_HighlightYAxis = false;
			flag = true;
		}
		if (flag)
		{
			Refresh();
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (e.X <= ScaleDPI(m_HorzMargin))
			{
				m_DraggingYOrigin = true;
				m_DraggingOriginLastY = e.Y;
				base.Capture = true;
			}
			else
			{
				m_ScrollingX = true;
				m_ScrollingXLastX = e.X;
				base.Capture = true;
				FireStopTrackingEnd();
			}
		}
		else if (e.Button == MouseButtons.Right)
		{
			m_DraggingHeight = true;
			m_DragHeightLastY = e.Y;
			m_DraggingHeightOriginY = ValueToY(0.0, m_DrawInputs, null);
			int num = ValueToY(0.0, m_DrawInputs, null);
			m_DraggingHeightMult = ((e.Y <= num) ? 1 : (-1));
			base.Capture = true;
		}
		base.OnMouseDoubleClick(e);
	}

	private void FireStopTrackingEnd()
	{
		if (this.StopTrackingEnd != null)
		{
			this.StopTrackingEnd();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		bool flag = e.X > ScaleDPI(m_HorzMargin) - ScaleDPI(30) && e.X <= ScaleDPI(m_HorzMargin);
		if (flag != m_HighlightYAxis)
		{
			m_HighlightYAxis = flag;
			Refresh();
		}
		if (m_HighlightYAxis && e.Y != m_HighlightYAxisMouseY)
		{
			m_HighlightYAxisMouseY = e.Y;
			Refresh();
		}
		if (m_DraggingYOrigin)
		{
			int num = m_DraggingOriginLastY - e.Y;
			m_DraggingOriginLastY = e.Y;
			m_YOffset -= (double)num / m_YScale;
			RecalculateView();
			MainForm.Inst.HoverBox.Hide();
		}
		else if (m_DraggingHeight)
		{
			m_ChangedHeight = true;
			int num2 = m_DraggingHeightMult * (m_DragHeightLastY - e.Y);
			m_DragHeightLastY = e.Y;
			m_YScale += 0.015 * m_YScale * (double)num2;
			m_YScale = Misc.Clamp(m_YScale, 3E-09, 10000000.0);
			m_YOffset = (double)(-(base.Height - ScaleDPI(m_BottomMargin) - m_DraggingHeightOriginY)) / m_YScale;
			RecalculateView();
			MainForm.Inst.HoverBox.Hide();
		}
		else if (m_ScrollingX)
		{
			switch (m_XAxisMode)
			{
			case XAxisMode.Frame:
			{
				long num5 = XToFrameX(m_ScrollingXLastX);
				long num6 = XToFrameX(e.X) - num5;
				m_StartFrameX -= num6;
				break;
			}
			case XAxisMode.Time:
			{
				long timerFrequency = m_Session.TimerFrequency;
				long num3 = XToTime(m_ScrollingXLastX, timerFrequency);
				long num4 = XToTime(e.X, timerFrequency) - num3;
				m_StartTime -= num4;
				break;
			}
			}
			m_ScrollingXLastX = e.X;
			OnTimeRangeChanged();
			RecalculateView();
			MainForm.Inst.HoverBox.Hide();
			m_ShowMouseHoverPoint = false;
		}
		else if (e.X < ScaleDPI(m_HorzMargin))
		{
			if (e.X > ScaleDPI(m_HorzMargin) - ScaleDPI(30))
			{
				HoverBox hoverBox = MainForm.Inst.HoverBox;
				double num7 = YToValueDouble(e.Y, m_DrawInputs, null);
				if (m_Unit.ToLower() == "bytes")
				{
					num7 /= 1024.0;
				}
				string key = (string)(hoverBox.Target = Utils.TruncateDouble(num7) + " " + m_UnitToDisplay);
				hoverBox.Clear();
				hoverBox.MinWidth = ScaleDPI(60);
				hoverBox.AddLine(key, "");
				hoverBox.SubmitLines();
				hoverBox.Visible = true;
				Point location = PointToScreen(e.Location);
				hoverBox.SetLocation(location);
			}
			else
			{
				MainForm.Inst.HoverBox.Hide();
			}
			if (m_ShowMouseHoverPoint)
			{
				m_ShowMouseHoverPoint = false;
				Refresh();
			}
		}
		else if (e.Y > base.Height - ScaleDPI(m_BottomMargin))
		{
			if (e.Y < base.Height - ScaleDPI(m_BottomMargin) + ScaleDPI(30))
			{
				HoverBox hoverBox2 = MainForm.Inst.HoverBox;
				hoverBox2.Target = e.X;
				hoverBox2.Clear();
				hoverBox2.MinWidth = ScaleDPI(60);
				switch (m_XAxisMode)
				{
				case XAxisMode.Frame:
					hoverBox2.AddLine("Frame", (XToFrameX(e.X) / 1000).ToString());
					break;
				case XAxisMode.Time:
				{
					string text = Utils.TruncateDouble((double)(XToTime(e.X, m_Session.TimerFrequency) - m_Session.FirstFrameTime) / (double)m_Session.TimerFrequency).ToString();
					hoverBox2.AddLine("Time", text + " Sec");
					break;
				}
				}
				hoverBox2.SubmitLines();
				hoverBox2.Visible = true;
				Point location2 = PointToScreen(e.Location);
				hoverBox2.SetLocation(location2);
			}
			else
			{
				MainForm.Inst.HoverBox.Hide();
			}
			if (m_ShowMouseHoverPoint)
			{
				m_ShowMouseHoverPoint = false;
				Refresh();
			}
		}
		else
		{
			int dist = ScaleDPI(10);
			Point hit_point = default(Point);
			double hit_value = 0.0;
			string closestGraph = GetClosestGraph(e.Location, ref dist, ref hit_point, ref hit_value);
			if (closestGraph != null)
			{
				XToFrameX(hit_point.X);
				m_ShowMouseHoverPoint = true;
				m_HoverPointLocation = hit_point;
				Refresh();
				HoverBox hoverBox3 = MainForm.Inst.HoverBox;
				string text2 = closestGraph + hit_value;
				if (!(hoverBox3.Target is string) || (string)hoverBox3.Target != text2)
				{
					string value = Utils.TruncateDouble(hit_value).ToString();
					hoverBox3.Target = text2;
					hoverBox3.Clear();
					hoverBox3.Title = closestGraph;
					switch (m_XAxisMode)
					{
					case XAxisMode.Frame:
						hoverBox3.AddLine("Frame", (XToFrameX(hit_point.X) / 1000).ToString());
						break;
					case XAxisMode.Time:
						hoverBox3.AddLine("Time", 0.ToString());
						break;
					}
					hoverBox3.AddLine("Value", value);
					hoverBox3.SubmitLines();
				}
				hoverBox3.Visible = true;
				Point location3 = PointToScreen(e.Location);
				hoverBox3.SetLocation(location3);
			}
			else
			{
				if (m_ShowMouseHoverPoint)
				{
					m_ShowMouseHoverPoint = false;
					Refresh();
				}
				MainForm.Inst.HoverBox.Hide();
			}
		}
		base.OnMouseMove(e);
	}

	private static int GetPointIndex(int x, Point[] points)
	{
		int num = 0;
		foreach (Point point in points)
		{
			if (point.X > x)
			{
				break;
			}
			num++;
		}
		if (num > 0)
		{
			num--;
		}
		return num;
	}

	private GraphDrawData GetGraphDrawData(string name)
	{
		foreach (GraphDrawData graphDrawDatum in m_MainThreadDrawData.m_GraphDrawData)
		{
			if (graphDrawDatum.m_GraphName == name)
			{
				return graphDrawDatum;
			}
		}
		return null;
	}

	private string GetClosestGraph(Point p, ref int dist, ref Point hit_point, ref double hit_value)
	{
		string result = null;
		_ = m_Session.TimerFrequency;
		lock (m_DrawDataSwapLock)
		{
			int num = dist;
			foreach (GraphDrawData graphDrawDatum in m_MainThreadDrawData.m_GraphDrawData)
			{
				if (graphDrawDatum.m_Points.Length < 2)
				{
					continue;
				}
				int num2 = 0;
				int num3 = 0;
				switch (m_XAxisMode)
				{
				case XAxisMode.Time:
					num2 = GetPointIndex(p.X - ScaleDPI(10) / 2, graphDrawDatum.m_Points);
					num3 = GetPointIndex(p.X + ScaleDPI(10) / 2, graphDrawDatum.m_Points);
					break;
				case XAxisMode.Frame:
					num2 = (int)((XToFrameX(p.X - ScaleDPI(10) / 2) - m_StartFrameX) / 1000);
					num3 = (int)((XToFrameX(p.X + ScaleDPI(10) / 2) - m_StartFrameX) / 1000);
					break;
				}
				num2--;
				num3++;
				num2 = Misc.Clamp(num2, 0, graphDrawDatum.m_Points.Length - 1);
				num3 = Misc.Clamp(num3, 0, graphDrawDatum.m_Points.Length - 1);
				for (int i = num2; i <= num3; i++)
				{
					int num4 = Math.Min(i + 1, graphDrawDatum.m_Points.Length - 1);
					Point p2 = graphDrawDatum.m_Points[i];
					Point p3 = graphDrawDatum.m_Points[num4];
					Point closest_point;
					int distanceFromLineSegment = Utils.GetDistanceFromLineSegment(p, p2, p3, out closest_point);
					if (distanceFromLineSegment < num)
					{
						num = distanceFromLineSegment;
						dist = distanceFromLineSegment;
						result = graphDrawDatum.m_GraphName;
						hit_point = closest_point;
						if (i == num4)
						{
							hit_value = graphDrawDatum.m_Values[i];
							continue;
						}
						double a = graphDrawDatum.m_Values[i];
						double b = graphDrawDatum.m_Values[num4];
						int num5 = p3.X - p2.X;
						double p4 = ((num5 != 0) ? ((double)((closest_point.X - p2.X) / num5)) : 0.0);
						hit_value = Utils.Lerp(a, b, p4);
					}
				}
			}
			return result;
		}
	}

	public void GotoEnd()
	{
		int num = base.Width - ScaleDPI(m_HorzMargin);
		switch (m_XAxisMode)
		{
		case XAxisMode.Frame:
		{
			long num3 = num * 1000;
			m_StartFrameX = Math.Max(0L, m_Session.FrameCount - num3) * 1000;
			break;
		}
		case XAxisMode.Time:
		{
			long num2 = (long)((double)(num * m_Session.TimerFrequency) / (m_TimeXScale * 1000.0));
			m_StartTime = Math.Max(m_Session.FirstFrameTime, m_Session.LastFrameEndTime - num2);
			break;
		}
		}
		OnTimeRangeChanged();
	}

	public void RefreshGraph()
	{
		RecalculateView();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (m_DraggingYOrigin)
		{
			base.Capture = false;
			m_DraggingYOrigin = false;
			m_Settings.Write();
			Refresh();
		}
		if (m_DraggingHeight)
		{
			base.Capture = false;
			m_DraggingHeight = false;
			m_Settings.Write();
			Refresh();
		}
		else if (m_ScrollingX)
		{
			base.Capture = false;
			m_ScrollingX = false;
		}
		base.OnMouseUp(e);
	}

	public void OnMouseWheel(int delta, Point location)
	{
		if (location.X < ScaleDPI(m_HorzMargin))
		{
			m_ChangedHeight = true;
			int num = ((delta > 0) ? 1 : (-1));
			int num2 = ValueToY(0.0, m_DrawInputs, null);
			m_YScale += 0.2 * m_YScale * (double)num;
			m_YScale = Misc.Clamp(m_YScale, 3E-09, 10000000.0);
			m_YOffset = (double)(-(base.Height - ScaleDPI(m_BottomMargin) - num2)) / m_YScale;
			RecalculateView();
			MainForm.Inst.HoverBox.Hide();
		}
		else
		{
			switch (m_XAxisMode)
			{
			case XAxisMode.Frame:
				OnMouseWheelFrameMode(delta, location);
				break;
			case XAxisMode.Time:
				OnMouseWheelTimeMode(delta, location);
				break;
			}
		}
		OnTimeRangeChanged();
		if (this.StopTrackingEnd != null)
		{
			this.StopTrackingEnd();
		}
		RecalculateView();
	}

	public void OnMouseWheelFrameMode(int delta, Point location)
	{
		long num = XToFrameX(location.X);
		double frameXScale = m_FrameXScale;
		SetTimeRange(x_scale: (delta <= 0) ? (frameXScale / 1.1) : (frameXScale * 1.1), start: m_StartFrameX);
		long num2 = XToFrameX(location.X) - num;
		SetTimeRange(m_StartFrameX - num2, m_FrameXScale);
	}

	public void OnMouseWheelTimeMode(int delta, Point location)
	{
		long timerFrequency = m_Session.TimerFrequency;
		long num = XToTime(location.X, timerFrequency);
		if (delta > 0)
		{
			m_TimeXScale *= 1.1;
		}
		else
		{
			m_TimeXScale /= 1.1;
		}
		m_TimeXScale = Misc.Clamp(m_TimeXScale, 0.0005, 50.0);
		long num2 = XToTime(location.X, timerFrequency);
		m_StartTime -= num2 - num;
	}

	public void SetFillBackgroundColour(Color colour)
	{
		m_GraphFillBrush = new SolidBrush(colour);
	}

	public void SetUnitScaleToHeight(string units, bool value)
	{
		if (value)
		{
			if (!m_UnitsToScaleToHeight.Contains(units))
			{
				m_UnitsToScaleToHeight.Add(units);
			}
		}
		else if (m_UnitsToScaleToHeight.Contains(units))
		{
			m_UnitsToScaleToHeight.Remove(units);
		}
		RecalculateView();
	}

	public void OnScopeColourChanged(GetLineColourDelegate get_line_colour_delegate)
	{
		foreach (Graph graph in m_Graphs)
		{
			graph.m_Colour = get_line_colour_delegate(m_Session.GetStringId(graph.m_StatName));
		}
		RecalculateView(force: true);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
	}
}
