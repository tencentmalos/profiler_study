using System;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Editor;

internal class HVColourPanel : Panel
{
	public delegate void ColourChangedHandler(Color colour, ColourSelectMode mode);

	private Bitmap m_Bitmap;

	private Color m_Colour = Color.Blue;

	private ColourSelectMode m_SelectMode;

	private bool m_DraggingMouse;

	public Color Colour
	{
		get
		{
			return m_Colour;
		}
		set
		{
			if (m_Colour != value)
			{
				m_Colour = value;
				UpdateBitmap();
				Refresh();
			}
		}
	}

	public event ColourChangedHandler ColourChanged;

	public HVColourPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint, value: true);
		UpdateStyles();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (m_Bitmap == null)
		{
			UpdateBitmap();
		}
		if (m_Bitmap != null)
		{
			e.Graphics.DrawImageUnscaledAndClipped(m_Bitmap, base.ClientRectangle);
		}
		base.OnPaint(e);
	}

	protected override void OnResize(EventArgs eventargs)
	{
		base.OnResize(eventargs);
		UpdateBitmap();
		Refresh();
	}

	private void UpdateBitmap()
	{
		if (base.Width == 0 || base.Height == 0)
		{
			return;
		}
		if (m_Bitmap == null || m_Bitmap.Width < base.Width || m_Bitmap.Height < base.Height)
		{
			m_Bitmap = new Bitmap(base.Width, base.Height);
		}
		for (int i = 0; i < base.Height; i++)
		{
			for (int j = 0; j < base.Width; j++)
			{
				Color colour = GetColour(j, i);
				m_Bitmap.SetPixel(j, i, colour);
			}
		}
		Point locationFromColour = GetLocationFromColour(m_Colour);
		Color colour2 = GetColour(locationFromColour.X, locationFromColour.Y);
		ColourMarker.DrawMarker(m_Bitmap, locationFromColour, colour2);
	}

	private Color GetColour(int x, int y)
	{
		ColourUtils.ColorToHSV(m_Colour, out var _, out var saturation, out var _);
		if (saturation < 0.001)
		{
			saturation = 1.0;
		}
		GetHueValue(x, y, out var h, out var v);
		return ColourUtils.ColorFromHSV(h, saturation, v);
	}

	private Point GetLocationFromColour(Color colour)
	{
		ColourUtils.ColorToHSV(colour, out var hue, out var _, out var value);
		int num = (int)(hue * (double)base.Width / 360.0);
		int num2 = (int)((double)base.Height - value * (double)base.Height);
		return new Point(num, num2);
	}

	private void GetHueValue(int x, int y, out double h, out double v)
	{
		h = ((base.Width != 0) ? ((double)x * 360.0 / (double)base.Width) : 0.0);
		v = ((base.Height != 0) ? ((double)(base.Height - y) / (double)base.Height) : 0.0);
	}

	private void SetColour(Point p)
	{
		int num = Misc.Clamp(p.X, 0, base.Width);
		int num2 = Misc.Clamp(p.Y, 0, base.Height);
		m_Colour = GetColour(num, num2);
		UpdateBitmap();
		Refresh();
		if (this.ColourChanged != null)
		{
			this.ColourChanged(m_Colour, m_SelectMode);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
		{
			m_SelectMode = ((e.Button != MouseButtons.Left) ? ColourSelectMode.Back : ColourSelectMode.Fore);
			SetColour(e.Location);
			m_DraggingMouse = true;
			base.Capture = true;
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		m_DraggingMouse = false;
		base.Capture = false;
		base.OnMouseUp(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_DraggingMouse)
		{
			SetColour(e.Location);
		}
		base.OnMouseMove(e);
	}
}
