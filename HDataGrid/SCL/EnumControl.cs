using System;
using System.Windows.Forms;

namespace SCL;

internal class EnumControl : ComboBox
{
	public delegate void ValueChangedHandler();

	private Type m_EnumType;

	private object m_Value;

	public object Value
	{
		get
		{
			return m_Value;
		}
		set
		{
			Type type = value.GetType();
			if (!type.Equals(m_EnumType))
			{
				m_EnumType = type;
				base.Items.Clear();
				string[] names = Enum.GetNames(type);
				foreach (string item in names)
				{
					base.Items.Add(item);
				}
			}
			m_Value = value;
			string[] names2 = Enum.GetNames(type);
			for (int j = 0; j < names2.Length; j++)
			{
				if (names2[j] == value.ToString())
				{
					SelectedIndex = j;
					break;
				}
			}
		}
	}

	public event ValueChangedHandler ValueChanged;

	public EnumControl()
	{
		Hide();
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		base.LostFocus += LostFocusEvent;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
		{
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void LostFocusEvent(object sender, EventArgs e)
	{
		if (this.ValueChanged != null)
		{
			this.ValueChanged();
		}
	}

	protected override void OnSelectedIndexChanged(EventArgs e)
	{
		m_Value = Enum.Parse(m_EnumType, Text);
		if (this.ValueChanged != null)
		{
			this.ValueChanged();
		}
		base.OnSelectedIndexChanged(e);
	}
}
