using SCL;

namespace Editor;

internal class DefaultPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		Row row = new Row(property_name, value);
		row.Cells[1].ReadOnly = read_only;
		return row;
	}

	public void UpdateRow(Row row, object value)
	{
		if (!row.Cells[1].Value.Equals(value))
		{
			row.Cells[1].Value = value;
		}
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		return colrow.GetCellValue();
	}
}
