using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class MDIFormTitleText : Control
{
	private string m_TitleText;

	private const int m_TitleTextOffset = 8;

	private bool m_Selected;

	private SolidBrush m_ActiveBrush = new SolidBrush(Color.Black);

	private SolidBrush m_InactiveBrush = new SolidBrush(Color.Black);

	private float m_DPIScale;

	public string TitleText
	{
		get
		{
			return m_TitleText;
		}
		set
		{
			m_TitleText = value;
		}
	}

	public bool Selected
	{
		get
		{
			return m_Selected;
		}
		set
		{
			m_Selected = value;
		}
	}

	public int TextWidth => (int)CreateGraphics().MeasureString(m_TitleText, Font).Width;

	public Color ActiveColour
	{
		get
		{
			return m_ActiveBrush.Color;
		}
		set
		{
			m_ActiveBrush = new SolidBrush(value);
		}
	}

	public Color InactiveColour
	{
		get
		{
			return m_InactiveBrush.Color;
		}
		set
		{
			m_InactiveBrush = new SolidBrush(value);
		}
	}

	public MDIFormTitleText()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		m_DPIScale = (float)base.DeviceDpi / 96f;
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(BackColor);
		Brush brush = (Selected ? m_ActiveBrush : m_InactiveBrush);
		int num = (base.Height - Font.Height) / 2;
		e.Graphics.DrawString(m_TitleText, Font, brush, ScaleDPI(8), num);
		base.OnPaint(e);
	}
}
