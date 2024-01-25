using System;
using System.Windows.Forms;

namespace SCL;

public class Cell : ICloneable
{
	private object m_Value;

	private Type m_Type;

	private bool m_Changed;

	private bool m_ReadOnly;

	private ContextMenuStrip m_ContextMenu;

	internal CellsPanel m_CellsPanel;

	public object Value
	{
		get
		{
			return m_Value;
		}
		set
		{
			if ((m_Value == null && value != null) || (value != null && !m_Value.Equals(value)))
			{
				object value2 = m_Value;
				m_Value = value;
				m_Changed = true;
				OnValueChanged();
				if (this.ValueChanged != null)
				{
					this.ValueChanged();
				}
				bool num = value2 is Control;
				bool flag = value is Control;
				if (num != flag && m_CellsPanel != null)
				{
					m_CellsPanel.ReAddCellControls();
				}
				if (m_Type == null)
				{
					m_Type = ((value != null) ? value.GetType() : typeof(string));
				}
			}
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

	public ContextMenuStrip ContextMenu
	{
		get
		{
			return m_ContextMenu;
		}
		set
		{
			m_ContextMenu = value;
		}
	}

	public bool Changed => m_Changed;

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

	public event CellValueChangedHandler ValueChanged;

	public Cell()
	{
	}

	public Cell(Type type)
	{
		m_Type = type;
	}

	public Cell(object value)
	{
		m_Value = value;
		m_Type = ((value != null) ? value.GetType() : typeof(string));
	}

	public object Clone()
	{
		Cell cell = new Cell();
		if (m_Value is ICloneable cloneable)
		{
			cell.m_Value = cloneable.Clone();
		}
		else if (m_Value is Control)
		{
			cell.m_Value = m_Value.GetType().GetConstructor(new Type[0]).Invoke(new object[0]);
		}
		else if (m_Value.GetType().IsValueType)
		{
			cell.m_Value = m_Value;
		}
		cell.m_Type = m_Type;
		cell.m_Changed = false;
		cell.m_ReadOnly = m_ReadOnly;
		cell.m_ContextMenu = m_ContextMenu;
		return cell;
	}

	public virtual void OnValueChanged()
	{
	}

	public override string ToString()
	{
		if (m_Value == null)
		{
			return "this cell is NULL!";
		}
		return m_Value.ToString();
	}
}
