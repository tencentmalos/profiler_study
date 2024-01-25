using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class SelectBox
{
	private int m_SelPanelThickness = 4;

	private Panel m_SelLeftPanel = new DBPanel();

	private Panel m_SelRightPanel = new DBPanel();

	private Panel m_SelTopPanel = new DBPanel();

	private Panel m_SelBottomPanel = new DBPanel();

	private Color m_Colour = SystemColors.ControlDarkDark;

	public bool Visible
	{
		get
		{
			return m_SelLeftPanel.Visible;
		}
		set
		{
			if (Visible != value)
			{
				if (value)
				{
					m_SelLeftPanel.Show();
					m_SelRightPanel.Show();
					m_SelTopPanel.Show();
					m_SelBottomPanel.Show();
				}
				else
				{
					m_SelLeftPanel.Hide();
					m_SelRightPanel.Hide();
					m_SelTopPanel.Hide();
					m_SelBottomPanel.Hide();
				}
				if (this.SelectBoxVisibilityChanged != null)
				{
					this.SelectBoxVisibilityChanged(value);
				}
			}
		}
	}

	public Panel SelLeftPanel => m_SelLeftPanel;

	public Panel SelRightPanel => m_SelRightPanel;

	public Panel SelTopPanel => m_SelTopPanel;

	public Panel SelBottomPanel => m_SelBottomPanel;

	public int SelPanelThickness
	{
		get
		{
			return m_SelPanelThickness;
		}
		set
		{
			m_SelPanelThickness = value;
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
			m_Colour = value;
			UpdateColour();
		}
	}

	public event SelectBoxVisibilityChangedHandler SelectBoxVisibilityChanged;

	public SelectBox()
	{
		UpdateColour();
	}

	public void AddTo(Control control)
	{
		control.Controls.Add(m_SelLeftPanel);
		control.Controls.Add(m_SelRightPanel);
		control.Controls.Add(m_SelTopPanel);
		control.Controls.Add(m_SelBottomPanel);
	}

	public void BringToFront()
	{
		m_SelLeftPanel.BringToFront();
		m_SelRightPanel.BringToFront();
		m_SelTopPanel.BringToFront();
		m_SelBottomPanel.BringToFront();
	}

	public void SetRect(Rectangle rect)
	{
		m_SelLeftPanel.Location = new Point(rect.Left - m_SelPanelThickness / 2, rect.Top - m_SelPanelThickness / 2);
		m_SelLeftPanel.Size = new Size(m_SelPanelThickness, rect.Height + m_SelPanelThickness);
		m_SelRightPanel.Location = new Point(rect.Right - m_SelPanelThickness / 2, rect.Top - m_SelPanelThickness / 2);
		m_SelRightPanel.Size = new Size(m_SelPanelThickness, rect.Height + m_SelPanelThickness);
		m_SelTopPanel.Location = new Point(rect.Left - m_SelPanelThickness / 2, rect.Top - m_SelPanelThickness / 2);
		m_SelTopPanel.Size = new Size(rect.Width + m_SelPanelThickness, m_SelPanelThickness);
		m_SelBottomPanel.Location = new Point(rect.Left - m_SelPanelThickness / 2, rect.Bottom - m_SelPanelThickness / 2);
		m_SelBottomPanel.Size = new Size(rect.Width + m_SelPanelThickness, m_SelPanelThickness);
		m_SelLeftPanel.Left = rect.Left - 2;
		m_SelLeftPanel.BringToFront();
		m_SelRightPanel.BringToFront();
		m_SelTopPanel.BringToFront();
		m_SelBottomPanel.BringToFront();
	}

	private void UpdateColour()
	{
		m_SelLeftPanel.BackColor = m_Colour;
		m_SelRightPanel.BackColor = m_Colour;
		m_SelTopPanel.BackColor = m_Colour;
		m_SelBottomPanel.BackColor = m_Colour;
	}
}
