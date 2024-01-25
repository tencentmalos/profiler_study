using SCL;

namespace Editor;

internal class MultiLineTextPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		MultiLineText multiLineText = (MultiLineText)value;
		Row row = ((!multiLineText.MultiLine) ? new Row(property_name, multiLineText.Text) : new Row(property_name, new MultiLineTextBoxControl(multiLineText.Text)));
		row.Cells[1].ReadOnly = read_only;
		return row;
	}

	public void UpdateRow(Row row, object value)
	{
		MultiLineText multiLineText = (MultiLineText)value;
		if (multiLineText.MultiLine)
		{
			if (!(row.Cells[1].Value is MultiLineTextBoxControl))
			{
				row.Cells[1].Value = new MultiLineTextBoxControl(multiLineText.Text);
			}
			((MultiLineTextBoxControl)row.Cells[1].Value).TextValue = multiLineText.Text;
		}
		else
		{
			row.Cells[1].Value = multiLineText.Text;
		}
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		if (((MultiLineText)old_value).MultiLine)
		{
			MultiLineTextBoxControl multiLineTextBoxControl = (MultiLineTextBoxControl)colrow.Row.Cells[1].Value;
			MultiLineText multiLineText = default(MultiLineText);
			multiLineText.Text = multiLineTextBoxControl.TextValue;
			return multiLineText;
		}
		MultiLineText multiLineText2 = default(MultiLineText);
		multiLineText2.Text = (string)colrow.Row.Cells[1].Value;
		multiLineText2.MultiLine = false;
		return multiLineText2;
	}
}
