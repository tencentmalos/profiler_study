using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

public class LeftBackPanel : Panel
{
	private Pen m_LinePen;

	public LeftBackPanel()
	{
		m_LinePen = new Pen(ForeColor);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.DrawLine(m_LinePen, base.ClientSize.Width - 1, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
	}
}
