using System;
using System.Drawing;

namespace SCL;

public class Column
{
	public enum ESortMode
	{
		NotSorted,
		Increasing,
		Decreasing
	}

	public enum EWidthMode
	{
		Explicit,
		Fill
	}

	private string m_Name;

	private int m_Width = 100;

	private EWidthMode m_WidthMode;

	private bool m_Sortable = true;

	private ESortMode m_SortMode;

	private bool m_ScrollHorz;

	private int m_HScrollOffset;

	private Image m_BoolTrueImage;

	private Image m_BoolFalseImage;

	private bool m_DeleteKeyDeletesRow;

	private bool m_ReadOnly;

	private Type m_Type;

	private bool m_Visible = true;

	private ColumnComparer m_Comparer;

	public string Name
	{
		get
		{
			return m_Name;
		}
		set
		{
			m_Name = value;
		}
	}

	public int Width
	{
		get
		{
			return m_Width;
		}
		set
		{
			m_Width = value;
		}
	}

	public bool Sortable
	{
		get
		{
			return m_Sortable;
		}
		set
		{
			m_Sortable = value;
		}
	}

	public ESortMode SortMode
	{
		get
		{
			return m_SortMode;
		}
		set
		{
			m_SortMode = value;
		}
	}

	public EWidthMode WidthMode
	{
		get
		{
			return m_WidthMode;
		}
		set
		{
			m_WidthMode = value;
		}
	}

	public bool ScrollHorz
	{
		get
		{
			return m_ScrollHorz;
		}
		set
		{
			m_ScrollHorz = value;
		}
	}

	internal int HScrollOffset
	{
		get
		{
			return m_HScrollOffset;
		}
		set
		{
			m_HScrollOffset = value;
		}
	}

	public Image BoolTrueImage
	{
		get
		{
			return m_BoolTrueImage;
		}
		set
		{
			m_BoolTrueImage = Utils.To96Dpi(value);
		}
	}

	public Image BoolFalseImage
	{
		get
		{
			return m_BoolFalseImage;
		}
		set
		{
			m_BoolFalseImage = Utils.To96Dpi(value);
		}
	}

	public bool DeleteKeyDeletesRow
	{
		get
		{
			return m_DeleteKeyDeletesRow;
		}
		set
		{
			m_DeleteKeyDeletesRow = value;
		}
	}

	public bool ReadOnly
	{
		get
		{
			return m_ReadOnly;
		}
		set
		{
			m_ReadOnly = value;
		}
	}

	public Type Type
	{
		get
		{
			return m_Type;
		}
		set
		{
			m_Type = value;
		}
	}

	public bool Visible
	{
		get
		{
			return m_Visible;
		}
		set
		{
			m_Visible = value;
		}
	}

	public ColumnComparer Comparer
	{
		get
		{
			return m_Comparer;
		}
		set
		{
			m_Comparer = value;
		}
	}

	public Column()
	{
	}

	public Column(string name)
	{
		m_Name = name;
	}
}
