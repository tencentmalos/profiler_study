using System;
using System.Drawing;
using SCL;

namespace Editor;

internal class RectPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		Rectangle rect = (Rectangle)value;
		Row row = new Row(property_name, RectString(rect));
		row.ChildRows.Add(new PropertyName("X"), rect.X);
		row.ChildRows.Add(new PropertyName("Y"), rect.Y);
		row.ChildRows.Add(new PropertyName("Width"), rect.Width);
		row.ChildRows.Add(new PropertyName("Height"), rect.Height);
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
		Rectangle rect = (Rectangle)value;
		row.Cells[1].Value = RectString(rect);
		row.ChildRows[0].Cells[1].Value = rect.X;
		row.ChildRows[1].Cells[1].Value = rect.Y;
		row.ChildRows[2].Cells[1].Value = rect.Width;
		row.ChildRows[3].Cells[1].Value = rect.Height;
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		Rectangle rectangle = (Rectangle)old_value;
		PropertyName propertyName = (PropertyName)colrow.Row.Cells[0].Value;
		if (!propertyName.Equals(property_name))
		{
			int num = (int)colrow.GetCellValue();
			switch (propertyName.Name)
			{
			case "X":
				rectangle.X = num;
				break;
			case "Y":
				rectangle.Y = num;
				break;
			case "Width":
				rectangle.Width = num;
				break;
			case "Height":
				rectangle.Height = num;
				break;
			}
		}
		else
		{
			string[] array = colrow.GetCellValue().ToString().Split(',');
			try
			{
				int x = Convert.ToInt32(array[0]);
				int y = Convert.ToInt32(array[1]);
				int width = Convert.ToInt32(array[2]);
				int height = Convert.ToInt32(array[3]);
				rectangle = new Rectangle(x, y, width, height);
			}
			catch (Exception)
			{
				return null;
			}
		}
		return rectangle;
	}

	private static string RectString(Rectangle rect)
	{
		return rect.X + ", " + rect.Y + ", " + rect.Width + ", " + rect.Height;
	}
}
