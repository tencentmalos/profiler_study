using System;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace Editor;

internal class MultiLineTextBoxControl : Control, IDataGridControl, IHDataGridCellControl
{
	private string m_Text;

	private TextBox m_TextBox = new TextBox();

	public object Value
	{
		get
		{
			return TextValue;
		}
		set
		{
			TextValue = (string)value;
		}
	}

	public string TextValue
	{
		get
		{
			return m_Text;
		}
		set
		{
			m_Text = value;
			m_TextBox.Text = Utils.ExpandNewLineChars(value);
			if (this.CellChanged != null)
			{
				this.CellChanged(this);
			}
		}
	}

	public event ControlCellChangedHandler CellChanged;

	public MultiLineTextBoxControl(string text)
	{
		m_TextBox.ReadOnly = true;
		m_TextBox.BackColor = BackColor;
		m_TextBox.BorderStyle = BorderStyle.None;
		m_TextBox.Text = Utils.ExpandNewLineChars(text);
		m_Text = text;
		base.Controls.Add(m_TextBox);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		m_TextBox.Size = base.Size;
		m_TextBox.Location = new Point(0, (base.Height - m_TextBox.Height) / 2);
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		m_TextBox.BackColor = BackColor;
	}
}
