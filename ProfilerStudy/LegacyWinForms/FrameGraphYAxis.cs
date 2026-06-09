using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class FrameGraphYAxis : UserControl
{
	private const int m_MinIntervalHeight = 15;

	private const int m_IntervalLineWidth = 4;

	private const int m_TextGapX = 1;

	private const int m_TargetSpotSize = 6;

	private const int m_TargetRectInflate = 4;

	private Settings m_Settings;

	private double m_YScale;

	private double m_TargetFrameMS;

	private Font m_AxisFont = new Font("Ariel", 7f);

	private bool m_StartedDrag;

	private bool m_StartedDragTarget;

	private int m_LastMouseDragY;

	private const double m_GraphYDragSpeed = 0.015;

	private const double m_MinGraphYScale = 3E-06;

	private Pen m_TopLinePen = new Pen(Color.FromArgb(160, 160, 160));

	private float m_DPIScale;

	private IContainer components;

	public double YScale
	{
		get
		{
			return m_YScale;
		}
		set
		{
			m_YScale = value;
			Refresh();
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

	public event FrameGraphYAxisScaleChangedHandler FrameGraphYAxisScaleChanged;

	public event FrameGraphYAxisTargetMSChangedHandler FrameGraphYAxisTargetMSChanged;

	public FrameGraphYAxis()
	{
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(Color.LightGray);
		e.Graphics.DrawLine(m_TopLinePen, 0, 0, base.ClientSize.Width - 1, 0);
		e.Graphics.DrawLine(Pens.Black, base.ClientSize.Width - 1, 0, base.ClientSize.Width - 1, base.ClientSize.Height);
		if (!base.DesignMode)
		{
			double num = (double)base.ClientSize.Height / m_YScale;
			Interval bestInterval = MiscUnits.GetBestInterval(num, base.ClientSize.Height, ScaleDPI(15), "ms");
			int num2 = (int)(num / bestInterval.StepSize) + 1;
			for (int i = 0; i < num2; i++)
			{
				double num3 = (double)i * bestInterval.Step;
				double ms = num3 * (double)bestInterval.Unit;
				int num4 = MsToY(ms);
				e.Graphics.DrawLine(Pens.Black, base.ClientSize.Width - ScaleDPI(4), num4, base.ClientSize.Width, num4);
				string s = num3.ToString("0.###");
				SizeF sizeF = e.Graphics.MeasureString(s, m_AxisFont);
				int num5 = base.ClientSize.Width - ScaleDPI(4) - ScaleDPI(1) - (int)sizeF.Width;
				int num6 = num4 - m_AxisFont.Height / 2;
				e.Graphics.DrawString(s, m_AxisFont, Brushes.Black, num5, num6);
				e.Graphics.FillEllipse(Brushes.Red, GetTargetSpotRect());
			}
		}
		base.OnPaint(e);
	}

	private Rectangle GetTargetSpotRect()
	{
		double ms = Misc.Clamp(m_TargetFrameMS, -10000.0, 10000.0);
		int num = MsToY(ms);
		int num2 = ScaleDPI(6);
		return new Rectangle(base.ClientSize.Width - num2, num - num2 / 2, num2, num2);
	}

	private Rectangle GetTargetSpotRectForHitTest()
	{
		Rectangle targetSpotRect = GetTargetSpotRect();
		int num = ScaleDPI(4);
		targetSpotRect.Inflate(num, num);
		return targetSpotRect;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (GetTargetSpotRectForHitTest().Contains(e.Location))
			{
				m_StartedDragTarget = true;
				base.Capture = true;
				m_LastMouseDragY = e.Y;
			}
			else
			{
				m_StartedDrag = true;
				base.Capture = true;
				m_LastMouseDragY = e.Y;
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		HoverBox hoverBox = MainForm.Inst.HoverBox;
		if (m_StartedDrag)
		{
			int num = m_LastMouseDragY - e.Y;
			m_LastMouseDragY = e.Y;
			m_YScale += 0.015 * m_YScale * (double)num;
			m_YScale = Math.Max(3E-06, m_YScale);
			Refresh();
			if (this.FrameGraphYAxisScaleChanged != null)
			{
				this.FrameGraphYAxisScaleChanged(m_YScale);
			}
			hoverBox.Visible = false;
		}
		else if (m_StartedDragTarget)
		{
			int num2 = m_LastMouseDragY - e.Y;
			m_LastMouseDragY = e.Y;
			double num3 = (double)num2 / m_YScale;
			m_TargetFrameMS += num3;
			Refresh();
			if (this.FrameGraphYAxisTargetMSChanged != null)
			{
				this.FrameGraphYAxisTargetMSChanged(m_TargetFrameMS);
			}
			hoverBox.Visible = true;
			hoverBox.Clear();
			hoverBox.Title = "";
			hoverBox.AddLine(m_TargetFrameMS.ToString("0.###"), "");
			hoverBox.SubmitLines();
			Point location = PointToScreen(e.Location);
			hoverBox.SetLocation(location);
		}
		else
		{
			if (GetTargetSpotRectForHitTest().Contains(e.Location))
			{
				Cursor = Cursors.SizeNS;
			}
			else
			{
				Cursor = Cursors.Default;
			}
			hoverBox.Visible = false;
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_StartedDrag)
			{
				m_StartedDrag = false;
				base.Capture = false;
				m_Settings.Write();
			}
			else if (m_StartedDragTarget)
			{
				m_StartedDragTarget = false;
				base.Capture = false;
				m_Settings.Write();
			}
		}
		base.OnMouseUp(e);
	}

	private int MsToY(double ms)
	{
		return base.ClientSize.Height - (int)(ms * m_YScale);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Name = "FrameGraphYAxis";
		base.Size = new System.Drawing.Size(46, 157);
		base.ResumeLayout(false);
	}
}
