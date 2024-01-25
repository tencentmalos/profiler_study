using System;
using System.Windows.Forms;

namespace SCL;

internal class StringControl : TextBox
{
	public delegate void ValueChangedHandler();

	private object m_Value;

	private Type m_Type;

	private object m_LastSubmittedValue;

	public object Value
	{
		get
		{
			return m_Value;
		}
		set
		{
			m_Value = value;
		}
	}

	public Type Type
	{
		get
		{
			return m_Type;
		}
		set
		{
			m_Type = value;
		}
	}

	public event ValueChangedHandler ValueChanged;

	public StringControl()
	{
		Hide();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		if (m_Value != null)
		{
			m_Value = ConvertValue(Text, m_Value, m_Type);
			SubmitValueEvent();
		}
		base.OnLostFocus(e);
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
		{
			return true;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private static float ConvertFloat(object value)
	{
		if (value.ToString() == "-")
		{
			return 0f;
		}
		return Convert.ToSingle(value);
	}

	private static object ConvertValue(object new_value, object original_value, Type type)
	{
		if (type == null)
		{
			type = original_value.GetType();
		}
		try
		{
			if (type == typeof(string))
			{
				return new_value.ToString();
			}
			if (type == typeof(int))
			{
				return Convert.ToInt32(new_value);
			}
			if (type == typeof(float))
			{
				return ConvertFloat(new_value);
			}
			if (type == typeof(bool))
			{
				return Convert.ToBoolean(new_value);
			}
			if (type == typeof(uint))
			{
				return Convert.ToUInt32(new_value);
			}
		}
		catch (Exception)
		{
		}
		return original_value;
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		UpdateValue();
	}

	public void UpdateValue()
	{
		m_Value = ConvertValue(Text, m_Value, m_Type);
		SubmitValueEvent();
	}

	private void SubmitValueEvent()
	{
		if (m_LastSubmittedValue != Value)
		{
			m_LastSubmittedValue = Value;
			if (this.ValueChanged != null)
			{
				this.ValueChanged();
			}
		}
	}
}
