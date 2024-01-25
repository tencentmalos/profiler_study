using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class TabbedPanelTitleBar : Control
{
	public delegate void CloseButtonClickedHandler();

	public delegate void DoubleClickHandler();

	public delegate void CaptionDraggedHandler();

	private const int m_TextOffsetX = 8;

	private bool m_HighlightCloseRect;

	private bool m_MousePresssed;

	private float m_DPIScale;

	public event CloseButtonClickedHandler CloseButtonClicked;

	public event DoubleClickHandler TitleDoubleClick;

	public event CaptionDraggedHandler CaptionDragged;

	public TabbedPanelTitleBar()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		m_DPIScale = (float)base.DeviceDpi / 96f;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		Refresh();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.DrawString(Text, Font, SystemBrushes.ActiveCaptionText, ScaleDPI(8), (base.Height - Font.Height) / 2);
		Utils.DrawCloseButton(GetCloseButtonRect(), e.Graphics, m_HighlightCloseRect);
		base.OnPaint(e);
	}

	private Rectangle GetCloseButtonRect()
	{
		int num = 2 * base.Height / 3;
		return new Rectangle(base.Width - 3 * num / 2, (base.Height - num) / 2, num, num);
	}

	protected override void OnResize(EventArgs e)
	{
		Refresh();
		base.OnResize(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (GetCloseButtonRect().Contains(e.Location))
		{
			if (this.CloseButtonClicked != null)
			{
				this.CloseButtonClicked();
			}
		}
		else
		{
			m_MousePresssed = true;
			base.OnMouseDown(e);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_MousePresssed)
		{
			if (this.CaptionDragged != null)
			{
				this.CaptionDragged();
			}
			m_MousePresssed = false;
			return;
		}
		Rectangle closeButtonRect = GetCloseButtonRect();
		bool flag = closeButtonRect.Contains(e.Location);
		if (m_HighlightCloseRect != flag)
		{
			m_HighlightCloseRect = flag;
			Invalidate(closeButtonRect);
			Update();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		m_MousePresssed = false;
		base.OnMouseUp(e);
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		if (this.TitleDoubleClick != null)
		{
			this.TitleDoubleClick();
		}
		base.OnMouseDoubleClick(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		m_MousePresssed = false;
		base.OnLostFocus(e);
	}
}
