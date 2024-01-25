using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class DragNodeTargetMarker
{
	private const int m_Thickness = 2;

	private Panel m_HorzPanel = new Panel();

	private Panel m_VertPanel = new Panel();

	public DragNodeTargetMarker()
	{
		m_HorzPanel.BackColor = SystemColors.ControlDarkDark;
		m_VertPanel.BackColor = SystemColors.ControlDarkDark;
	}

	public void AddTo(Control control)
	{
		Hide();
		control.Controls.Add(m_HorzPanel);
		control.Controls.Add(m_VertPanel);
	}

	public void Show()
	{
		m_HorzPanel.Show();
		m_VertPanel.Show();
	}

	public void Hide()
	{
		m_HorzPanel.Hide();
		m_VertPanel.Hide();
	}

	public void SetPosition(int x, int y, int width, int height)
	{
		m_HorzPanel.Location = new Point(x, y - 1);
		m_HorzPanel.Size = new Size(width, 2);
		m_VertPanel.Location = new Point(x, y - height / 2);
		m_VertPanel.Size = new Size(2, height);
		m_HorzPanel.BringToFront();
		m_VertPanel.BringToFront();
	}
}
