using System;
using System.Windows.Forms;

namespace SCL;

public abstract class DropDownListControl : ComboBox, IDataGridEditControl
{
	private string[] m_values;

	private object m_value;

	private object m_OldValue;

	public object Value
	{
		get
		{
			return m_value;
		}
		set
		{
			m_value = value;
			if (m_values == null)
			{
				InitialiseItems();
			}
			if (m_OldValue == null)
			{
				m_OldValue = m_value;
			}
			string text = ObjectToString(value);
			bool flag = false;
			for (int i = 0; i < m_values.Length; i++)
			{
				if (text == m_values[i])
				{
					SelectedIndex = i;
					flag = true;
					break;
				}
			}
		}
	}

	public event EditControlValueChangedHandler EditControlValueChanged;

	public DropDownListControl()
	{
		base.DropDownStyle = ComboBoxStyle.DropDownList;
	}

	protected override void OnEnter(EventArgs e)
	{
		base.OnEnter(e);
		base.DroppedDown = true;
	}

	public abstract string[] GetValues();

	public abstract string ObjectToString(object value);

	public abstract object StringToObject(string value);

	private void InitialiseItems()
	{
		m_values = GetValues();
		base.Items.Clear();
		string[] values = m_values;
		foreach (string item in values)
		{
			base.Items.Add(item);
		}
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
		{
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	protected override void OnSelectedIndexChanged(EventArgs e)
	{
		Value = StringToObject(Text);
		base.OnSelectedIndexChanged(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			SubmitAndClose();
		}
		else if (e.KeyCode == Keys.Escape)
		{
			m_value = m_OldValue;
			SubmitAndClose();
		}
		base.OnKeyDown(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		SubmitAndClose();
		base.OnLostFocus(e);
	}

	private void SubmitAndClose()
	{
		bool close = true;
		if (this.EditControlValueChanged != null)
		{
			this.EditControlValueChanged(close);
		}
	}
}
