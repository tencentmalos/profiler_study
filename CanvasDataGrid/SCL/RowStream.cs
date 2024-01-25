namespace SCL;

public class RowStream
{
	private RowCollection m_Rows = new RowCollection();

	private Row m_CurrentRow;

	public RowCollection Rows => m_Rows;

	public RowStream()
	{
		m_CurrentRow = null;
	}

	public Row Add<T1>(T1 value1)
	{
		Row row = new Row();
		row.Cells.Add(WrapCell(value1));
		Add(row);
		return row;
	}

	public Row Add<T1, T2>(T1 value1, T2 value2)
	{
		Row row = new Row();
		row.Cells.Add(WrapCell(value1));
		row.Cells.Add(WrapCell(value2));
		Add(row);
		return row;
	}

	public void Add(Row row)
	{
		if (m_CurrentRow != null)
		{
			m_CurrentRow.ChildRows.Add(row);
		}
		else
		{
			m_Rows.Add(row);
		}
	}

	public void Enter(string value)
	{
		Row row = new Row();
		row.Cells.Add(WrapCell(value));
		Add(row);
		m_CurrentRow = row;
	}

	public void Leave()
	{
		m_CurrentRow = ((m_CurrentRow.Parent.Parent != null) ? m_CurrentRow.Parent : null);
	}

	private Cell WrapCell(object value)
	{
		if (value is Cell)
		{
			return (Cell)value;
		}
		return new Cell(value);
	}
}
