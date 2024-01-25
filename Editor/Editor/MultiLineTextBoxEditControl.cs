using System;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace Editor;

internal class MultiLineTextBoxEditControl : Control, IDataGridEditControl
{
	private string m_Text;

	private TextBox m_TextBox = new TextBox();

	private TextBoxControlForm m_TextBoxControlForm;

	public object Value
	{
		get
		{
			return m_Text;
		}
		set
		{
			m_Text = (string)value;
			m_TextBox.Text = Utils.ExpandNewLineChars(m_Text);
			Refresh();
			if (m_TextBoxControlForm != null)
			{
				m_TextBoxControlForm.TextValue = m_Text;
			}
		}
	}

	public event EditControlValueChangedHandler EditControlValueChanged;

	public MultiLineTextBoxEditControl()
	{
		m_TextBox.ReadOnly = true;
		m_TextBox.BackColor = BackColor;
		m_TextBox.BorderStyle = BorderStyle.None;
		base.Controls.Add(m_TextBox);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		m_TextBox.Size = base.Size;
		m_TextBox.Location = new Point(0, (base.Height - m_TextBox.Height) / 2);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		if (m_TextBoxControlForm == null)
		{
			m_TextBoxControlForm = new TextBoxControlForm();
			m_TextBoxControlForm.TextChangedEvent += TextBoxControlFormTextChanged;
			m_TextBoxControlForm.TextValue = m_Text;
			m_TextBoxControlForm.Location = Utils.PickGoodScreenPositionForForm(m_TextBoxControlForm.Size);
			m_TextBoxControlForm.Show(this);
			m_TextBoxControlForm.Activate();
			m_TextBoxControlForm.FormClosing += FormClosing;
		}
		base.OnGotFocus(e);
	}

	private void TextBoxControlFormTextChanged(string text)
	{
		m_Text = text;
		m_TextBox.Text = Utils.ExpandNewLineChars(text);
		Refresh();
	}

	private void FormClosing(object sender, FormClosingEventArgs e)
	{
		m_TextBoxControlForm.FormClosing -= FormClosing;
		m_TextBoxControlForm.TextChangedEvent -= TextBoxControlFormTextChanged;
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close: true);
		}
		m_TextBoxControlForm = null;
	}
}
