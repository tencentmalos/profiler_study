using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class DockTargetPanel : Form
{
	private static Pen m_BorderPen = new Pen(Color.FromArgb(64, 255, 255, 255));

	private static Brush m_FillBrush = new SolidBrush(Color.FromArgb(64, 64, 255));

	private Timer m_FadeInTimer;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ExStyle |= 128;
			return obj;
		}
	}

	public DockTargetPanel()
	{
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.Enabled = false;
		base.Opacity = 0.0;
		m_FadeInTimer = new Timer();
		m_FadeInTimer.Tick += FadeInTimer;
		m_FadeInTimer.Enabled = true;
		m_FadeInTimer.Interval = 10;
	}

	protected override void Dispose(bool disposing)
	{
		m_FadeInTimer.Enabled = false;
		base.Dispose(disposing);
	}

	private void FadeInTimer(object sender, EventArgs e)
	{
		if (base.Opacity < 0.5)
		{
			base.Opacity = Math.Min(base.Opacity + 0.01, 0.5);
		}
		if (base.Opacity >= 0.5)
		{
			m_FadeInTimer.Enabled = false;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Rectangle rect = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		e.Graphics.DrawRectangle(m_BorderPen, rect);
		rect.Inflate(-1, -1);
		e.Graphics.FillRectangle(m_FillBrush, rect);
	}
}
