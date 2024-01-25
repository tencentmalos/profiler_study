using System;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace Editor;

internal class ColourEditControl : Control, IDataGridEditControl
{
	private Color m_Colour;

	private PaletteControlForm m_PaletteControlForm;

	public object Value
	{
		get
		{
			return m_Colour;
		}
		set
		{
			m_Colour = (Color)value;
			UpdateBackColour();
			Refresh();
			if (m_PaletteControlForm != null)
			{
				m_PaletteControlForm.Colour = m_Colour;
			}
		}
	}

	public event EditControlValueChangedHandler EditControlValueChanged;

	protected override void OnGotFocus(EventArgs e)
	{
		if (m_PaletteControlForm == null)
		{
			m_PaletteControlForm = new PaletteControlForm();
			m_PaletteControlForm.ColourChanged += PaletteControlFormColourChanged;
			m_PaletteControlForm.Colour = m_Colour;
			m_PaletteControlForm.Location = Utils.PickGoodScreenPositionForForm(m_PaletteControlForm.Size);
			m_PaletteControlForm.Show(this);
			m_PaletteControlForm.Activate();
			m_PaletteControlForm.FormClosing += FormClosing;
		}
		base.OnGotFocus(e);
	}

	private void PaletteControlFormColourChanged(Color colour)
	{
		m_Colour = colour;
		UpdateBackColour();
		Refresh();
	}

	private void UpdateBackColour()
	{
		BackColor = Color.FromArgb(m_Colour.R, m_Colour.G, m_Colour.B);
	}

	private void FormClosing(object sender, FormClosingEventArgs e)
	{
		m_PaletteControlForm.FormClosing -= FormClosing;
		m_PaletteControlForm.ColourChanged -= PaletteControlFormColourChanged;
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close: true);
		}
		m_PaletteControlForm = null;
	}
}
