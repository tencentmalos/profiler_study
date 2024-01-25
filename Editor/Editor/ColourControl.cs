using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace Editor;

internal class ColourControl : Control, IDataGridControl, IHDataGridCellControl
{
	private Color m_Colour;

	private Panel m_Panel = new Panel();

	public object Value
	{
		get
		{
			return Colour;
		}
		set
		{
			Colour = (Color)value;
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
			UpdateControlColour();
			Refresh();
			if (this.CellChanged != null)
			{
				this.CellChanged(this);
			}
		}
	}

	public event ControlCellChangedHandler CellChanged;

	public ColourControl(Color colour)
	{
		m_Panel.Dock = DockStyle.Fill;
		base.Controls.Add(m_Panel);
		m_Colour = colour;
		UpdateControlColour();
	}

	private void UpdateControlColour()
	{
		m_Panel.BackColor = Color.FromArgb(m_Colour.R, m_Colour.G, m_Colour.B);
	}
}
