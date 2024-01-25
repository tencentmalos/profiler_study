using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class CheckButton : UserControl
{
	private string m_ButtonText = "Button";

	private bool m_Checked;

	private Color m_CheckedBackgroundColour = Color.LightBlue;

	private Pen m_BorderPen = new Pen(Color.LightGray);

	private Brush m_TextBrush = new SolidBrush(Control.DefaultForeColor);

	private IContainer components;

	public bool Checked
	{
		get
		{
			return m_Checked;
		}
		set
		{
			m_Checked = value;
			Refresh();
		}
	}

	public Color CheckedBackgroundColour
	{
		get
		{
			return m_CheckedBackgroundColour;
		}
		set
		{
			m_CheckedBackgroundColour = value;
			Refresh();
		}
	}

	public Color BorderColour
	{
		get
		{
			return m_BorderPen.Color;
		}
		set
		{
			m_BorderPen.Color = value;
			Refresh();
		}
	}

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

	public event CheckButtonCheckChangedHandler CheckChange;

	public CheckButton()
	{
		InitializeComponent();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (m_Checked)
		{
			e.Graphics.Clear(m_CheckedBackgroundColour);
		}
		e.Graphics.DrawRectangle(m_BorderPen, 0, 0, base.Width - 1, base.Height - 1);
		SizeF sizeF = e.Graphics.MeasureString(m_ButtonText, Font);
		int num = (base.Width - (int)sizeF.Width) / 2;
		int num2 = (base.Height - (int)sizeF.Height) / 2;
		e.Graphics.DrawString(m_ButtonText, Font, m_TextBrush, num, num2);
		base.OnPaint(e);
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		m_TextBrush = new SolidBrush(ForeColor);
		Refresh();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (!m_Checked)
		{
			m_Checked = true;
			Refresh();
			if (this.CheckChange != null)
			{
				this.CheckChange();
			}
		}
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
