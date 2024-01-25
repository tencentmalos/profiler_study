using System.Drawing;
using System.Windows.Forms;
using SCL;
using SCLCoreCLR;

namespace Editor;

internal class ColourFControl : Control, IDataGridControl, IHDataGridCellControl
{
	private ColourF m_Colour;

	public object Value
	{
		get
		{
			return m_Colour;
		}
		set
		{
			m_Colour = (ColourF)value;
		}
	}

	public ColourF Colour
	{
		get
		{
			return m_Colour;
		}
		set
		{
			m_Colour = value;
			UpdateBackColour();
			Refresh();
			if (this.CellChanged != null)
			{
				this.CellChanged(this);
			}
		}
	}

	public event ControlCellChangedHandler CellChanged;

	public ColourFControl(ColourF colour)
	{
		m_Colour = colour;
		UpdateBackColour();
	}

	private void UpdateBackColour()
	{
		Color color = m_Colour.ToColor();
		BackColor = Color.FromArgb(color.R, color.G, color.B);
	}
}
