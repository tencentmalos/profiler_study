using System;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class ViewButton : UserControl
{
	private bool m_Checked;

	private string m_Text = "View";

	private Brush m_TextBrush = new SolidBrush(Colours.ButtonText);

	private Brush m_HighlightBrush = new SolidBrush(Colours.ButtonHighlight);

	private Brush m_CheckedBackgroundBrush = new SolidBrush(Colours.ButtonChecked);

	private Brush m_CheckedHighlightBrush = new SolidBrush(Colours.ButtonCheckedHighlight);

	private Image m_Image;

	private Image m_DisabledImage;

	private bool m_MouseInside;

	public string ButtonText
	{
		get
		{
			return m_Text;
		}
		set
		{
			m_Text = value;
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
			m_Image = Utils.To96Dpi(value);
			Refresh();
		}
	}

	public Image DisabledImage
	{
		get
		{
			return m_DisabledImage;
		}
		set
		{
			m_DisabledImage = value;
			Refresh();
		}
	}

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

	public event ViewButtonCheckedChangedHandler CheckedChanged;

	public ViewButton()
	{
		base.Size = new Size(65, 65);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (m_MouseInside)
		{
			Brush brush = (m_Checked ? m_CheckedHighlightBrush : m_HighlightBrush);
			e.Graphics.FillRectangle(brush, 0, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
		}
		else if (m_Checked && base.Enabled)
		{
			e.Graphics.FillRectangle(m_CheckedBackgroundBrush, base.ClientRectangle);
		}
		int num = (int)e.Graphics.MeasureString(m_Text, Font).Width;
		int num2 = (base.ClientSize.Width - num) / 2;
		int num3 = base.ClientSize.Height - 3 * Font.Height / 2;
		Image image = ((base.Enabled || m_DisabledImage == null) ? m_Image : m_DisabledImage);
		if (image != null)
		{
			int num4 = Utils.ScaleDPI(image.Width, e.Graphics);
			int num5 = Utils.ScaleDPI(image.Height, e.Graphics);
			int num6 = (base.ClientSize.Width - num4) / 2;
			int num7 = (num3 - num5) / 2 + 3;
			e.Graphics.DrawImage(image, num6, num7);
		}
		e.Graphics.DrawString(m_Text, Font, m_TextBrush, num2, num3);
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Refresh();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		m_Checked = !m_Checked;
		Refresh();
		if (this.CheckedChanged != null)
		{
			this.CheckedChanged(this);
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		m_MouseInside = true;
		Refresh();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		m_MouseInside = false;
		Refresh();
		base.OnMouseLeave(e);
	}
}
