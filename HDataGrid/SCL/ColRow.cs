using System;

namespace SCL;

public class ColRow
{
	public static ColRow Invalid = new ColRow(-1, null);

	private int m_Col;

	private Row m_Row;

	public bool Valid
	{
		get
		{
			if (m_Col != Invalid.m_Col)
			{
				return m_Row != null;
			}
			return false;
		}
	}

	public int Col
	{
		get
		{
			return m_Col;
		}
		set
		{
			m_Col = value;
		}
	}

	public Row Row
	{
		get
		{
			return m_Row;
		}
		set
		{
			m_Row = value;
		}
	}

	public ColRow()
	{
	}

	public ColRow(int col, Row row)
	{
		m_Col = col;
		m_Row = row;
	}

	public override bool Equals(object obj)
	{
		if (obj is ColRow colRow && m_Col == colRow.m_Col)
		{
			return m_Row == colRow.m_Row;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return m_Col.GetHashCode() ^ m_Row.GetHashCode();
	}

	public Cell GetCell()
	{
		if (m_Col >= m_Row.Cells.Count)
		{
			return null;
		}
		return m_Row.Cells[m_Col];
	}

	public object GetCellValue()
	{
		return GetCell()?.Value;
	}

	public Type GetCellType()
	{
		return GetCell()?.Type;
	}

	public void SetCellValue(object value)
	{
		Cell cell = GetCell();
		if (cell != null)
		{
			cell.Value = value;
		}
	}

	public override string ToString()
	{
		Cell cell = GetCell();
		if (cell == null)
		{
			return "empty";
		}
		return cell.ToString();
	}
}
