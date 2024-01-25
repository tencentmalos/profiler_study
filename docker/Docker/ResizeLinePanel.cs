using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class ResizeLinePanel : Form
{
	private Size m_FormSize;

	public Size FormSize
	{
		get
		{
			return m_FormSize;
		}
		set
		{
			base.Size = value;
			m_FormSize = value;
		}
	}

	public ResizeLinePanel()
	{
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.Opacity = 0.5;
	}

	protected override void OnShown(EventArgs e)
	{
		base.Size = m_FormSize;
		base.OnShown(e);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.FillRectangle(Brushes.Blue, 1, 1, base.ClientSize.Width - 2, base.ClientSize.Height - 2);
		e.Graphics.DrawRectangle(Pens.Black, 0, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
		base.OnPaint(e);
	}
}
