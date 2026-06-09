using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

public class VerticalLabelPanel : UserControl
{
	private string m_PanelText = "Text";

	private Pen m_LinePen;

	private IContainer components;

	public string PanelText
	{
		get
		{
			return m_PanelText;
		}
		set
		{
			m_PanelText = value;
		}
	}

	public VerticalLabelPanel()
	{
		InitializeComponent();
		m_LinePen = new Pen(ForeColor);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_LinePen.Dispose();
		base.Dispose(disposing);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		Refresh();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		StringFormat stringFormat = new StringFormat(StringFormatFlags.DirectionVertical);
		SizeF sizeF = e.Graphics.MeasureString(m_PanelText, Font, new PointF(0f, 0f), stringFormat);
		int num = (base.ClientSize.Width - (int)sizeF.Width) / 2 - 2;
		int num2 = (base.ClientSize.Height - (int)sizeF.Height) / 2;
		e.Graphics.DrawString(m_PanelText, Font, SystemBrushes.ControlText, num, num2, stringFormat);
		e.Graphics.DrawLine(m_LinePen, base.ClientSize.Width - 1, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
		base.OnPaint(e);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		base.Name = "VerticalLabelPanel";
		base.ResumeLayout(false);
	}
}
