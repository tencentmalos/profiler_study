using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

internal class PacketBufferTimeCell : UserControl
{
	private int m_Time;

	private bool m_UsingPercent;

	private int m_PercentComplete;

	private SolidBrush m_TextBrush;

	public int Time
	{
		get
		{
			return m_Time;
		}
		set
		{
			if (m_Time != value)
			{
				m_Time = value;
				Refresh();
			}
		}
	}

	public int PercentComplete
	{
		get
		{
			return m_PercentComplete;
		}
		set
		{
			if (m_PercentComplete != value)
			{
				m_PercentComplete = value;
				m_UsingPercent = true;
				Refresh();
			}
		}
	}

	public PacketBufferTimeCell()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (m_UsingPercent)
		{
			e.Graphics.Clear(BackColor);
			if (m_TextBrush == null)
			{
				m_TextBrush = new SolidBrush(ForeColor);
			}
			int num = 4;
			int num2 = (base.ClientSize.Height - Font.Height) / 2;
			int num3 = 100 - m_PercentComplete;
			e.Graphics.DrawString(num3 + "%", Font, m_TextBrush, num, num2);
			return;
		}
		Color color = ForeColor;
		int num4 = m_Time / 1000;
		if (num4 >= 15)
		{
			e.Graphics.Clear(Color.Red);
			color = Color.White;
		}
		else if (num4 != 0)
		{
			e.Graphics.Clear(Color.Orange);
		}
		else
		{
			e.Graphics.Clear(BackColor);
		}
		if (m_TextBrush == null || m_TextBrush.Color != color)
		{
			m_TextBrush = new SolidBrush(color);
		}
		string s = num4 + " sec";
		int num5 = 4;
		int num6 = (base.ClientSize.Height - Font.Height) / 2;
		e.Graphics.DrawString(s, Font, m_TextBrush, num5, num6);
	}
}
