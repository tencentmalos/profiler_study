using System;
using System.Windows.Forms;

namespace SCL;

internal class BoolControl : ComboBox
{
	public delegate void ValueChangedHandler();

	public bool Value
	{
		get
		{
			return SelectedIndex == 0;
		}
		set
		{
			SelectedIndex = ((!value) ? 1 : 0);
		}
	}

	public event ValueChangedHandler ValueChanged;

	public BoolControl()
	{
		Hide();
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		base.Items.Add("True");
		base.Items.Add("False");
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
		if (this.ValueChanged != null)
		{
			this.ValueChanged();
		}
		base.OnSelectedIndexChanged(e);
	}
}
