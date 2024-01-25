using System.Drawing;
using SCL;
using SCLCoreCLR;

namespace Editor;

internal class ColourFPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		ColourF colour = (ColourF)value;
		Row row = new Row(property_name, new ColourFControl(colour));
		row.ChildRows.Add(new PropertyName("R"), colour.R);
		row.ChildRows.Add(new PropertyName("G"), colour.G);
		row.ChildRows.Add(new PropertyName("B"), colour.B);
		row.ChildRows.Add(new PropertyName("A"), colour.A);
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
		ColourF colour = (ColourF)value;
		((ColourFControl)row.Cells[1].Value).Colour = colour;
		row.ChildRows[0].Cells[1].Value = colour.R;
		row.ChildRows[1].Cells[1].Value = colour.G;
		row.ChildRows[2].Cells[1].Value = colour.B;
		row.ChildRows[3].Cells[1].Value = colour.A;
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		PropertyName propertyName = (PropertyName)colrow.Row.Cells[0].Value;
		if (!propertyName.Equals(property_name))
		{
			float num = (float)colrow.GetCellValue();
			ColourF colour = ((ColourFControl)colrow.Row.Parent.Cells[1].Value).Colour;
			float r = colour.R;
			float g = colour.G;
			float b = colour.B;
			float a = colour.A;
			switch (propertyName.Name)
			{
			case "R":
				r = num;
				break;
			case "G":
				g = num;
				break;
			case "B":
				b = num;
				break;
			case "A":
				a = num;
				break;
			}
			return new ColourF(r, g, b, a);
		}
		return ((ColourFControl)colrow.Row.Cells[1].Value).Colour;
	}

	private static string RectString(Rectangle rect)
	{
		return rect.X + ", " + rect.Y + ", " + rect.Width + ", " + rect.Height;
	}
}
