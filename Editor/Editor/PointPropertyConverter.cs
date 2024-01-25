using System;
using System.Drawing;
using SCL;

namespace Editor;

internal class PointPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		Point point = (Point)value;
		Row row = new Row(property_name, PointString(point));
		row.ChildRows.Add(new PropertyName("X"), point.X);
		row.ChildRows.Add(new PropertyName("Y"), point.Y);
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
		Point point = (Point)value;
		row.Cells[1].Value = PointString(point);
		row.ChildRows[0].Cells[1].Value = point.X;
		row.ChildRows[1].Cells[1].Value = point.Y;
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		Point point = (Point)old_value;
		PropertyName propertyName = (PropertyName)colrow.Row.Cells[0].Value;
		if (!propertyName.Equals(property_name))
		{
			int num = (int)colrow.GetCellValue();
			string name = propertyName.Name;
			if (!(name == "X"))
			{
				if (name == "Y")
				{
					point.Y = num;
				}
			}
			else
			{
				point.X = num;
			}
		}
		else
		{
			string[] array = colrow.GetCellValue().ToString().Split(',');
			try
			{
				int x = Convert.ToInt32(array[0]);
				int y = Convert.ToInt32(array[1]);
				point = new Point(x, y);
			}
			catch (Exception)
			{
				return null;
			}
		}
		return point;
	}

	private static string PointString(Point point)
	{
		return point.X + ", " + point.Y;
	}
}
