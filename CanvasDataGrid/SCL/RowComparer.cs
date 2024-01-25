using System.Collections.Generic;

namespace SCL;

internal sealed class RowComparer : IComparer<Row>
{
	private int m_ColIndex;

	private bool m_Reverse;

	private ColumnComparer m_Comparer;

	public RowComparer(int col_index, bool reverse, ColumnComparer comparer)
	{
		m_ColIndex = col_index;
		m_Reverse = reverse;
		m_Comparer = comparer;
	}

	public int Compare(Row row_a, Row row_b)
	{
		object cellValue = row_a.GetCellValue(m_ColIndex);
		object cellValue2 = row_b.GetCellValue(m_ColIndex);
		int num = m_Comparer(cellValue, cellValue2);
		if (num == 0)
		{
			num = row_a.StableSortIndex.CompareTo(row_b.StableSortIndex);
		}
		if (!m_Reverse)
		{
			return num;
		}
		return -num;
	}
}
