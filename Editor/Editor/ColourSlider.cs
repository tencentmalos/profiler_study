using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Editor;

internal class ColourSlider : UserControl
{
	public delegate void ColourChangedHandler(Color colour);

	public enum EMode
	{
		R,
		G,
		B
	}

	private EMode m_Mode;

	private bool m_MouseButtonHeld;

	private Bitmap m_Bitmap;

	private const int m_Gap = 8;

	private Color m_Colour = Color.Blue;

	private IContainer components;

	public EMode Mode
	{
		get
		{
			return m_Mode;
		}
		set
		{
			m_Mode = value;
		}
	}

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

	public ColourSlider()
	{
		InitializeComponent();
		SetStyle(ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint, value: true);
		UpdateStyles();
		UpdateBitmap();
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateBitmap();
		Refresh();
	}

	private void UpdateBitmap()
	{
		int num = base.Width;
		int num2 = base.Height - 16;
		if (m_Bitmap == null || m_Bitmap.Width < num || m_Bitmap.Height < num2)
		{
			m_Bitmap = new Bitmap(num, num2);
		}
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int r = m_Colour.R;
				int g = m_Colour.G;
				int b = m_Colour.B;
				int num3 = j * 255 / num;
				Color color = Color.Black;
				switch (m_Mode)
				{
				case EMode.R:
					color = Color.FromArgb(num3, g, b);
					break;
				case EMode.G:
					color = Color.FromArgb(r, num3, b);
					break;
				case EMode.B:
					color = Color.FromArgb(r, g, num3);
					break;
				}
				m_Bitmap.SetPixel(j, i, color);
			}
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private static int Lerp(int a, int b, float v)
	{
		return (int)((float)a * (1f - v) + (float)b * v);
	}

	private static Color Lerp(Color c1, Color c2, float v)
	{
		return Color.FromArgb(Lerp(c1.R, c2.R, v), Lerp(c1.G, c2.G, v), Lerp(c1.B, c2.B, v));
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		m_MouseButtonHeld = true;
		base.Capture = true;
		UpdateValue(e.X);
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		m_MouseButtonHeld = false;
		base.Capture = false;
		base.OnMouseUp(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (m_MouseButtonHeld)
		{
			UpdateValue(e.X);
		}
		base.OnMouseMove(e);
	}

	private void UpdateValue(int mouse_x)
	{
		int value = (mouse_x - 1) * 255 / (base.Size.Width - 2);
		value = Misc.Clamp(value, 0, 255);
		SetValue(value);
		UpdateBitmap();
		Refresh();
	}

	private void SetValue(int v)
	{
		int red = m_Colour.R;
		int green = m_Colour.G;
		int blue = m_Colour.B;
		switch (m_Mode)
		{
		case EMode.R:
			red = v;
			break;
		case EMode.G:
			green = v;
			break;
		case EMode.B:
			blue = v;
			break;
		}
		Color color = Color.FromArgb(red, green, blue);
		if (color != m_Colour)
		{
			m_Colour = color;
			if (this.ColourChanged != null)
			{
				this.ColourChanged(m_Colour);
			}
		}
	}

	private int GetValue()
	{
		return m_Mode switch
		{
			EMode.R => m_Colour.R, 
			EMode.G => m_Colour.G, 
			EMode.B => m_Colour.B, 
			_ => 0, 
		};
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		int num = 8;
		int num2 = base.Size.Height - 8;
		Brush brush = new SolidBrush(BackColor);
		e.Graphics.FillRectangle(brush, 0, 0, base.Size.Width, num);
		e.Graphics.FillRectangle(brush, 0, num2, base.Size.Width, base.Size.Height - num2);
		Rectangle rect = new Rectangle(0, num, base.Width, num2 - num);
		e.Graphics.DrawImageUnscaledAndClipped(m_Bitmap, rect);
		int num3 = GetValue() * base.Size.Width / 255;
		int num4 = num2 + 1;
		Point[] points = new Point[3]
		{
			new Point(num3, num4),
			new Point(num3 - 4, num4 + 8),
			new Point(num3 + 4, num4 + 8)
		};
		e.Graphics.FillPolygon(SystemBrushes.ControlDarkDark, points);
		base.OnPaint(e);
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
		base.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
		base.Name = "ColourSlider";
		base.Size = new System.Drawing.Size(337, 24);
		base.ResumeLayout(false);
	}
}
