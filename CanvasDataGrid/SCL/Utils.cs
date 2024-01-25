using System;
using System.Collections.Generic;
using System.Drawing;

namespace SCL;

internal class Utils
{
	public static void RemoveChildren(Set<Row> rows)
	{
		foreach (Row item in rows.ToList())
		{
			foreach (Row row in rows)
			{
				if (row.IsParentOf(item))
				{
					rows.Remove(item);
					break;
				}
			}
		}
	}

	public static Row GetFirstRow(ICollection<Row> rows)
	{
		if (rows.Count == 0)
		{
			return null;
		}
		Row row = null;
		using (IEnumerator<Row> enumerator = rows.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				row = enumerator.Current;
			}
		}
		row = row.Root;
		RowIterator rowIterator = new RowIterator(row);
		while (rowIterator.MoveNext())
		{
			if (rows.Contains(rowIterator.Current))
			{
				return rowIterator.Current;
			}
		}
		return null;
	}

	public static bool RowExpandedRecursive(Row row)
	{
		for (row = row.Parent; row != null; row = row.Parent)
		{
			if (!row.Expanded)
			{
				return false;
			}
		}
		return true;
	}

	public static void DrawBevelRect(Graphics graphics, Rectangle rect, Brush brush, Pen hi_pen, Pen low_pen)
	{
		DrawBevelRect(graphics, rect, brush, hi_pen, low_pen, invert: false);
	}

	public static void DrawBevelRect(Graphics graphics, Rectangle rect, Brush brush, Pen hi_pen, Pen low_pen, bool invert)
	{
		if (invert)
		{
			Pen pen = hi_pen;
			hi_pen = low_pen;
			low_pen = pen;
		}
		graphics.FillRectangle(brush, rect);
		graphics.DrawLine(hi_pen, rect.Left, rect.Top, rect.Right - 1, rect.Top);
		graphics.DrawLine(low_pen, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom - 1);
		graphics.DrawLine(low_pen, rect.Right - 1, rect.Bottom - 1, rect.Left, rect.Bottom - 1);
		graphics.DrawLine(hi_pen, rect.Left, rect.Bottom - 1, rect.Left, rect.Top);
	}

	public static bool IsValidCellChar(char c, object cell_value, Type type)
	{
		if (cell_value == null)
		{
			return true;
		}
		if (type == null)
		{
			type = cell_value.GetType();
		}
		if (type == typeof(float))
		{
			if ((c < '0' || c > '9') && c != '.')
			{
				return c == '-';
			}
			return true;
		}
		if (type == typeof(uint))
		{
			if (c >= '0')
			{
				return c <= '9';
			}
			return false;
		}
		return true;
	}

	public static ColRow GetColRow(Cell cell, RowCollection rows)
	{
		RowIterator rowIterator = new RowIterator(rows);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			for (int i = 0; i < current.Cells.Count; i++)
			{
				if (current.Cells[i] == cell)
				{
					return new ColRow(i, current);
				}
			}
		}
		return ColRow.Invalid;
	}

	public static ColRow GetColRow(object value, RowCollection rows)
	{
		RowIterator rowIterator = new RowIterator(rows);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			for (int i = 0; i < current.Cells.Count; i++)
			{
				Cell cell = current.Cells[i];
				if (cell != null && cell.Value == value)
				{
					return new ColRow(i, current);
				}
			}
		}
		return ColRow.Invalid;
	}

	public static RowCollection Clone(ICollection<Row> rows)
	{
		RowCollection rowCollection = new RowCollection(rows.Count);
		foreach (Row row in rows)
		{
			rowCollection.Add(row.Clone());
		}
		return rowCollection;
	}

	public static RowCollection Clone(RowCollection rows)
	{
		RowCollection rowCollection = new RowCollection(rows.Count);
		foreach (Row row in rows)
		{
			rowCollection.Add(row.Clone());
		}
		return rowCollection;
	}

	public static List<Row> GetSelectedRows(ICollection<ColRow> selected_cells)
	{
		Set<Row> set = new Set<Row>();
		foreach (ColRow selected_cell in selected_cells)
		{
			set.Add(selected_cell.Row);
		}
		List<Row> list = new List<Row>();
		foreach (Row item in set)
		{
			bool flag = false;
			foreach (Row item2 in set)
			{
				if (item2.IsParentOf(item))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static Row GetPrevRow(Row row)
	{
		ReverseRowIterator reverseRowIterator = new ReverseRowIterator(row);
		reverseRowIterator.MoveNext();
		reverseRowIterator.MoveNext();
		return reverseRowIterator.Current;
	}

	public static Row GetNextRow(Row row)
	{
		RowIterator rowIterator = new RowIterator(row);
		rowIterator.MoveNext();
		rowIterator.MoveNext();
		return rowIterator.Current;
	}

	public static Row GetMinRow(Row row1, Row row2)
	{
		Row row3 = row1;
		Row row4 = row2;
		while (row3 != null && row4 != null)
		{
			if (row3 == row2)
			{
				return row1;
			}
			if (row4 == row1)
			{
				return row2;
			}
			row3 = GetNextRow(row3);
			row4 = GetNextRow(row4);
		}
		if (row3 == null)
		{
			return row2;
		}
		return row1;
	}

	public static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			value = min;
		}
		else if (value > max)
		{
			value = max;
		}
		return value;
	}

	public static bool Equal(byte[] b1, byte[] b2)
	{
		if (b1.Length != b2.Length)
		{
			return false;
		}
		for (int i = 0; i < b1.Length; i++)
		{
			if (b1[i] != b2[i])
			{
				return false;
			}
		}
		return true;
	}

	public static Point Negate(Point pt)
	{
		return new Point(-pt.X, -pt.Y);
	}

	public static Image To96Dpi(Image image)
	{
		if (image != null)
		{
			Bitmap obj = (Bitmap)image;
			obj.SetResolution(96f, 96f);
			image = obj;
		}
		return image;
	}
}
