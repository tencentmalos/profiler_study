using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class SessionViewTabButton : UserControl
{
	private const int m_Padding = 10;

	private string m_ButtonText = "test";

	private const int m_ActiveTriangleWidth = 18;

	private const int m_ActiveTriangleHeight = 8;

	private Brush m_InactiveTextBrush = new SolidBrush(Color.FromArgb(148, 148, 148));

	private Brush m_HoverTextBrush = new SolidBrush(Color.FromArgb(120, 150, 207));

	private Brush m_ActiveTextBrush = new SolidBrush(Color.FromArgb(99, 130, 207));

	private Brush m_ActiveTriangleBrush = new SolidBrush(Colours.SessionViewTabActiveTriangle);

	private bool m_MouseHover;

	private bool m_Active;

	private IContainer components;

	public string ButtonText
	{
		get
		{
			return m_ButtonText;
		}
		set
		{
			m_ButtonText = value;
			OnButtonTextChanged();
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
			Refresh();
		}
	}

	public SessionViewTabButton()
	{
		Font = new Font("Monaco", 18f);
		InitializeComponent();
		OnButtonTextChanged();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (m_Active)
		{
			int num = base.Width / 2;
			Point[] points = new Point[3]
			{
				new Point(num - 9, 0),
				new Point(num + 9, 0),
				new Point(num, 8)
			};
			e.Graphics.FillPolygon(m_ActiveTriangleBrush, points);
		}
		SizeF sizeF = e.Graphics.MeasureString(m_ButtonText, Font);
		int num2 = (base.Width - (int)sizeF.Width) / 2;
		int num3 = (base.Height - (int)sizeF.Height) / 2;
		Brush brush = (m_Active ? m_ActiveTextBrush : ((!m_MouseHover) ? m_InactiveTextBrush : m_HoverTextBrush));
		e.Graphics.DrawString(m_ButtonText, Font, brush, num2, num3);
	}

	private void OnButtonTextChanged()
	{
		Graphics graphics = CreateGraphics();
		SizeF sizeF = graphics.MeasureString(m_ButtonText, Font);
		graphics.Dispose();
		base.Size = new Size((int)sizeF.Width + 20, base.Height);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		m_MouseHover = true;
		Refresh();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		m_MouseHover = false;
		Refresh();
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
		this.ForeColor = System.Drawing.Color.FromArgb(121, 121, 121);
		base.Name = "SessionViewTabButton";
		base.Size = new System.Drawing.Size(134, 25);
		base.ResumeLayout(false);
	}
}
