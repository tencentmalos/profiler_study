using System;
using System.Drawing;
using SCL;

namespace Editor;

internal class SizePropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		Size size = (Size)value;
		Row row = new Row(property_name, SizeString(size));
		row.ChildRows.Add(new PropertyName("Width"), size.Width);
		row.ChildRows.Add(new PropertyName("Height"), size.Height);
		row.Cells[1].ReadOnly = read_only;
		foreach (Row childRow in row.ChildRows)
		{
			childRow.Cells[1].ReadOnly = read_only;
		}
		row.Expanded = false;
		return row;
	}

	public void UpdateRow(Row row, object value)
	{
		Size size = (Size)value;
		row.Cells[1].Value = SizeString(size);
		row.ChildRows[0].Cells[1].Value = size.Width;
		row.ChildRows[1].Cells[1].Value = size.Height;
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		Size size = (Size)old_value;
		PropertyName propertyName = (PropertyName)colrow.Row.Cells[0].Value;
		if (!propertyName.Equals(property_name))
		{
			int num = (int)colrow.GetCellValue();
			string name = propertyName.Name;
			if (!(name == "Width"))
			{
				if (name == "Height")
				{
					size.Height = num;
				}
			}
			else
			{
				size.Width = num;
			}
		}
		else
		{
			string[] array = colrow.GetCellValue().ToString().Split(',');
			try
			{
				int width = Convert.ToInt32(array[0]);
				int height = Convert.ToInt32(array[1]);
				size = new Size(width, height);
			}
			catch (Exception)
			{
				return null;
			}
		}
		return size;
	}

	private static string SizeString(Size size)
	{
		return size.Width + ", " + size.Height;
	}
}
