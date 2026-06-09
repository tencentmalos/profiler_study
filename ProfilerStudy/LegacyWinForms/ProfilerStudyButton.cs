using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

internal class ProfilerStudyButton : UserControl
{
	private string m_Text = "Button";

	private Brush m_TextBrush = new SolidBrush(Colours.ButtonText);

	private Brush m_HighlightBrush = new SolidBrush(Colours.ButtonHighlight);

	private Image m_Image;

	private Image m_DisabledImage;

	private bool m_MouseInside;

	private bool m_UseImageAsText;

	private bool m_HighlightEnabled = true;

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
			m_DisabledImage = Utils.To96Dpi(value);
			Refresh();
		}
	}

	public bool UseImageAsText
	{
		get
		{
			return m_UseImageAsText;
		}
		set
		{
			m_UseImageAsText = value;
			Refresh();
		}
	}

	public bool HighlightEnabled
	{
		get
		{
			return m_HighlightEnabled;
		}
		set
		{
			m_HighlightEnabled = value;
		}
	}

	public ProfilerStudyButton()
	{
		base.Size = new Size(65, 65);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (m_MouseInside && m_HighlightEnabled)
		{
			e.Graphics.FillRectangle(m_HighlightBrush, 0, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
		}
		int num = (int)e.Graphics.MeasureString(m_Text, Font).Width;
		int num2 = (base.ClientSize.Width - num) / 2;
		Image image = ((base.Enabled || m_DisabledImage == null) ? m_Image : m_DisabledImage);
		int num3 = ((image != null) ? (base.ClientSize.Height - 3 * Font.Height / 2) : ((base.ClientSize.Height - Font.Height) / 2));
		if (image != null)
		{
			int num4 = Utils.ScaleDPI(image.Width, e.Graphics);
			int num5 = Utils.ScaleDPI(image.Height, e.Graphics);
			int num6 = (base.ClientSize.Width - num4) / 2;
			int num7 = (m_UseImageAsText ? (num3 + 2) : ((num3 - num5) / 2 + 3));
			e.Graphics.DrawImage(image, num6, num7);
		}
		if (!m_UseImageAsText)
		{
			e.Graphics.DrawString(m_Text, Font, m_TextBrush, num2, num3);
		}
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Refresh();
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

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.Name = "ProfilerStudyButton";
		base.ResumeLayout(false);
	}
}
