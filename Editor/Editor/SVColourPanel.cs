using System;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Editor;

internal class SVColourPanel : Control
{
	public delegate void ColourChangedHandler(Color colour, ColourSelectMode mode);

	private Bitmap m_Bitmap;

	private Color m_Colour = Color.Blue;

	private ColourSelectMode m_SelectMode;

	private bool m_DraggingMouse;

	private const int m_Gap = 10;

	private const int m_MarkerSize = 4;

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

	public SVColourPanel()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint, value: true);
		UpdateStyles();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
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
		ColourUtils.ColorToHSV(m_Colour, out var _, out var _, out var _);
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
		double saturation = ((base.Height != 0) ? ((double)(base.Height - y) / (double)base.Height) : 0.0);
		double value = ((base.Width != 0) ? ((double)x / (double)base.Width) : 0.0);
		ColourUtils.ColorToHSV(m_Colour, out var hue, out var _, out var _);
		return ColourUtils.ColorFromHSV(hue, saturation, value);
	}

	private Point GetLocationFromColour(Color colour)
	{
		ColourUtils.ColorToHSV(colour, out var _, out var saturation, out var value);
		int num = (int)(value * (double)base.Width);
		int num2 = (int)((double)base.Height - saturation * (double)base.Height);
		return new Point(num, num2);
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

	private void SetColour(int x, int y)
	{
		x = Misc.Clamp(x, 0, base.Width);
		y = Misc.Clamp(y, 0, base.Height);
		ColourUtils.ColorToHSV(GetColour(x, y), out var _, out var saturation, out var value);
		ColourUtils.ColorToHSV(m_Colour, out var hue2, out var _, out var _);
		Color color = ColourUtils.ColorFromHSV(hue2, saturation, value);
		if (m_Colour != color)
		{
			m_Colour = color;
			UpdateBitmap();
			Refresh();
			if (this.ColourChanged != null)
			{
				this.ColourChanged(m_Colour, m_SelectMode);
			}
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
		{
			m_DraggingMouse = true;
			base.Capture = true;
			m_SelectMode = ((e.Button != MouseButtons.Left) ? ColourSelectMode.Back : ColourSelectMode.Fore);
			SetColour(e.X, e.Y);
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
			SetColour(e.X, e.Y);
		}
		base.OnMouseMove(e);
	}
}
