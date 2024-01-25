using System.Drawing;
using SCL;

namespace Editor;

internal class ColorPropertyConverter : PropertyConverter
{
	public Row CreateRow(object parent, PropertyName property_name, object value, bool read_only)
	{
		Color colour = (Color)value;
		Row row = new Row(property_name, new ColourControl(colour));
		row.ChildRows.Add(new PropertyName("R"), (int)colour.R);
		row.ChildRows.Add(new PropertyName("G"), (int)colour.G);
		row.ChildRows.Add(new PropertyName("B"), (int)colour.B);
		row.ChildRows.Add(new PropertyName("A"), (int)colour.A);
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
		Color colour = (Color)value;
		((ColourControl)row.Cells[1].Value).Colour = colour;
		row.ChildRows[0].Cells[1].Value = (int)colour.R;
		row.ChildRows[1].Cells[1].Value = (int)colour.G;
		row.ChildRows[2].Cells[1].Value = (int)colour.B;
		row.ChildRows[3].Cells[1].Value = (int)colour.A;
	}

	public object GetNewValue(PropertyName property_name, ColRow colrow, object old_value)
	{
		PropertyName propertyName = (PropertyName)colrow.Row.Cells[0].Value;
		if (!propertyName.Equals(property_name))
		{
			int num = (int)colrow.GetCellValue();
			Color colour = ((ColourControl)colrow.Row.Parent.Cells[1].Value).Colour;
			byte red = colour.R;
			byte green = colour.G;
			byte blue = colour.B;
			byte alpha = colour.A;
			switch (propertyName.Name)
			{
			case "R":
				red = (byte)num;
				break;
			case "G":
				green = (byte)num;
				break;
			case "B":
				blue = (byte)num;
				break;
			case "A":
				alpha = (byte)num;
				break;
			}
			return Color.FromArgb(alpha, red, green, blue);
		}
		return ((ColourControl)colrow.Row.Cells[1].Value).Colour;
	}

	private static string RectString(Rectangle rect)
	{
		return rect.X + ", " + rect.Y + ", " + rect.Width + ", " + rect.Height;
	}
}
