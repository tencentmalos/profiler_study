using System;
using System.Drawing;
using System.Windows.Forms;
using SCL;
using SCLCoreCLR;

namespace Editor;

internal class ColourFEditControl : Control, IDataGridEditControl
{
	private ColourF m_Colour;

	private bool m_OldColourSet;

	private ColourF m_OldColour;

	private PaletteControlForm m_PaletteControlForm;

	public object Value
	{
		get
		{
			return m_Colour;
		}
		set
		{
			if (!m_OldColourSet)
			{
				m_OldColourSet = true;
				m_OldColour = (ColourF)value;
			}
			m_Colour = (ColourF)value;
			UpdateBackColour();
			Refresh();
			if (m_PaletteControlForm != null)
			{
				m_PaletteControlForm.Colour = m_Colour.ToColor();
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
			m_PaletteControlForm.Colour = m_Colour.ToColor();
			m_PaletteControlForm.Location = Utils.PickGoodScreenPositionForForm(m_PaletteControlForm.Size);
			m_PaletteControlForm.Show(this);
			m_PaletteControlForm.Activate();
			m_PaletteControlForm.FormClosing += FormClosing;
		}
		base.OnGotFocus(e);
	}

	private void PaletteControlFormColourChanged(Color colour)
	{
		m_Colour = new ColourF(colour);
		UpdateBackColour();
		Refresh();
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close: false);
		}
	}

	private void UpdateBackColour()
	{
		BackColor = m_Colour.ToColor();
	}

	private void FormClosing(object sender, FormClosingEventArgs e)
	{
		m_PaletteControlForm.FormClosing -= FormClosing;
		m_PaletteControlForm.ColourChanged -= PaletteControlFormColourChanged;
		ColourF colour = m_Colour;
		m_Colour = m_OldColour;
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close: false);
		}
		m_Colour = colour;
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close: true);
		}
		m_PaletteControlForm = null;
	}
}
