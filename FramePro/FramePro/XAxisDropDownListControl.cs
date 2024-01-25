using System;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace FramePro;

internal class XAxisDropDownListControl : ComboBox, IHDataGridCellControl, IDataGridEditControl
{
	private string[] m_Values = new string[4] { "Per Frame", "Per Second", "Per Frame (acc)", "Per Second (acc)" };

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
					Text = (string)base.SelectedItem;
					flag = true;
					break;
				}
			}
		}
	}

	public event ControlCellChangedHandler CellChanged;

	public event EditControlValueChangedHandler EditControlValueChanged;

	public XAxisDropDownListControl()
	{
		BackColor = SystemColors.Control;
		base.FlatStyle = FlatStyle.Flat;
		Font = new Font("Monaco", 9f, FontStyle.Regular, GraphicsUnit.Point, 0);
		base.DropDownStyle = ComboBoxStyle.DropDownList;
	}

	public string[] GetValues()
	{
		return m_Values;
	}

	public string ObjectToString(object value)
	{
		return (CustomStatXAxisMode)value switch
		{
			CustomStatXAxisMode.Frame => "Per Frame", 
			CustomStatXAxisMode.Time => "Per Second", 
			CustomStatXAxisMode.AccFrame => "Per Frame (acc)", 
			CustomStatXAxisMode.AccTime => "Per Second (acc)", 
			_ => "error", 
		};
	}

	public object StringToObject(string value)
	{
		return value switch
		{
			"Per Frame" => CustomStatXAxisMode.Frame, 
			"Per Second" => CustomStatXAxisMode.Time, 
			"Per Frame (acc)" => CustomStatXAxisMode.AccFrame, 
			"Per Second (acc)" => CustomStatXAxisMode.AccTime, 
			_ => CustomStatXAxisMode.Frame, 
		};
	}

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

	protected override void OnSelectedIndexChanged(EventArgs e)
	{
		try
		{
            base.OnSelectedIndexChanged(e);
            Value = StringToObject(Text);
            if (this.CellChanged != null)
            {
                this.CellChanged(this);
            }
            base.SelectionLength = 0;
        }
		catch (Exception ex)
		{

		}
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
