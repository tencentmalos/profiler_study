using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class WindowButton : Control
{
	public delegate void ClickedHandler();

	private Bitmap m_CloseBitmap;

	private Bitmap m_CloseHiBitmap;

	private bool m_Highlight;

	public event ClickedHandler Clicked;

	public WindowButton(Bitmap bitmap, Bitmap bitmap_hi)
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		m_CloseBitmap = bitmap;
		m_CloseHiBitmap = bitmap_hi;
		base.Size = m_CloseBitmap.Size;
	}

	protected override void OnPaintBackground(PaintEventArgs pevent)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Bitmap image = (m_Highlight ? m_CloseHiBitmap : m_CloseBitmap);
		e.Graphics.DrawImage(image, new Point(0, 0));
		base.OnPaint(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (!m_Highlight)
		{
			m_Highlight = true;
			Refresh();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (m_Highlight)
		{
			m_Highlight = false;
			Refresh();
		}
		base.OnMouseLeave(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		if (m_Highlight)
		{
			m_Highlight = false;
			Refresh();
		}
		base.OnLostFocus(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (this.Clicked != null)
		{
			this.Clicked();
		}
		base.OnMouseDown(e);
	}
}
