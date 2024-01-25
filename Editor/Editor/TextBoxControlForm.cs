using System;
using System.Drawing;
using System.Windows.Forms;

namespace Editor;

internal class TextBoxControlForm : Form
{
	private TextBox m_TextBox = new TextBox();

	private static Size m_FormSize = new Size(200, 200);

	public string TextValue
	{
		get
		{
			return m_TextBox.Text;
		}
		set
		{
			m_TextBox.Text = value;
		}
	}

	public event TextChangedHandler TextChangedEvent;

	public TextBoxControlForm()
	{
		base.Size = m_FormSize;
		base.StartPosition = FormStartPosition.Manual;
		base.FormBorderStyle = FormBorderStyle.SizableToolWindow;
		base.ControlBox = false;
		m_TextBox.Dock = DockStyle.Fill;
		m_TextBox.Multiline = true;
		m_TextBox.TextChanged += TextBoxTextChanged;
		base.Controls.Add(m_TextBox);
	}

	private void TextBoxTextChanged(object sender, EventArgs e)
	{
		if (this.TextChangedEvent != null)
		{
			this.TextChangedEvent(TextValue);
		}
	}

	protected override void OnDeactivate(EventArgs e)
	{
		base.OnDeactivate(e);
		Close();
	}

	protected override void OnResize(EventArgs e)
	{
		m_FormSize = base.Size;
		base.OnResize(e);
	}
}
