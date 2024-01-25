using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Editor;

public class Button : UserControl
{
	private Brush m_TextBrush;

	private string m_ButtonText = "Button";

	private Image m_Image;

	private bool m_Pressed;

	private bool m_Hover;

	private Color m_HoverColour = Color.FromArgb(245, 245, 245);

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
			Refresh();
		}
	}

	public Image Image
	{
		get
		{
			return m_Image;
		}
		set
		{
			m_Image = value;
			Refresh();
		}
	}

	public Color HoverColour
	{
		get
		{
			return m_HoverColour;
		}
		set
		{
			m_HoverColour = value;
			Refresh();
		}
	}

	public event ButtonClickedHandler Clicked;

	public Button()
	{
		InitializeComponent();
		m_TextBrush = new SolidBrush(ForeColor);
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		m_TextBrush = new SolidBrush(ForeColor);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (m_Hover)
		{
			e.Graphics.Clear(m_HoverColour);
		}
		if (!string.IsNullOrEmpty(m_ButtonText))
		{
			SizeF sizeF = e.Graphics.MeasureString(m_ButtonText, Font);
			int num = (base.ClientSize.Width - (int)sizeF.Width) / 2;
			int num2 = (base.ClientSize.Height - (int)sizeF.Height) / 2;
			e.Graphics.DrawString(m_ButtonText, Font, m_TextBrush, num, num2);
		}
		if (m_Image != null)
		{
			int num3 = (base.ClientSize.Width - m_Image.Width) / 2;
			int num4 = (base.ClientSize.Height - m_Image.Height) / 2;
			e.Graphics.DrawImage(m_Image, num3, num4);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			m_Pressed = true;
			base.Capture = true;
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && m_Pressed)
		{
			if (base.ClientRectangle.Contains(e.Location) && this.Clicked != null)
			{
				this.Clicked(this);
			}
			base.Capture = false;
		}
		base.OnMouseUp(e);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		m_Hover = true;
		Refresh();
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		m_Hover = false;
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
		this.components = new System.ComponentModel.Container();
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
	}
}
