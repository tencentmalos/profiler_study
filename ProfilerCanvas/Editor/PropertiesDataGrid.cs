using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SCL;
using SCLCoreCLR;

namespace Editor;

public class PropertiesDataGrid : UserControl
{
	private HDataGrid m_DataGrid = new HDataGrid();

	private CmdStack m_CmdStack;

	private object m_Object;

	private DefaultPropertyConverter m_DefaultPropertyConverter = new DefaultPropertyConverter();

	private Dictionary<Type, PropertyConverter> m_Converters = new Dictionary<Type, PropertyConverter>();

	private bool m_IgnoreCellChanges;

	public CmdStack CmdStack
	{
		get
		{
			return m_CmdStack;
		}
		set
		{
			m_CmdStack = value;
		}
	}

	public event PropertyChangingHandler PropertyChanging;

	public event PropertyChangedHandler PropertyChanged;

	public PropertiesDataGrid()
	{
		m_DataGrid.Dock = DockStyle.Fill;
		m_DataGrid.ColumnTitlePanelVisible = false;
		m_DataGrid.RowTitelPanelVisible = false;
		InitialiseDataGrid();
		m_DataGrid.CellChanged += OnCellChanged;
		AddConverter(typeof(Point), new PointPropertyConverter());
		AddConverter(typeof(Size), new SizePropertyConverter());
		AddConverter(typeof(Rectangle), new RectPropertyConverter());
		AddConverter(typeof(Color), new ColorPropertyConverter());
		AddConverter(typeof(ColourF), new ColourFPropertyConverter());
		AddConverter(typeof(MultiLineText), new MultiLineTextPropertyConverter());
		m_DataGrid.RegisterEditControl(typeof(ColourControl), typeof(ColourEditControl));
		m_DataGrid.RegisterEditControl(typeof(ColourFControl), typeof(ColourFEditControl));
		m_DataGrid.RegisterEditControl(typeof(MultiLineTextBoxControl), typeof(MultiLineTextBoxEditControl));
		base.Controls.Add(m_DataGrid);
	}

	public void AddConverter(Type type, PropertyConverter converter)
	{
		m_Converters[type] = converter;
	}

	private PropertyConverter GetConverter(Type type)
	{
		if (m_Converters.ContainsKey(type))
		{
			return m_Converters[type];
		}
		return m_DefaultPropertyConverter;
	}

	private void OnCellChanged(ICollection<ColRow> sel_cells)
	{
		if (m_IgnoreCellChanges)
		{
			return;
		}
		CmdGroup cmdGroup = new CmdGroup();
		foreach (ColRow sel_cell in sel_cells)
		{
			PropertyName propertyName = (PropertyName)sel_cell.Row.Cells[0].Value;
			if (sel_cell.Row.Parent.Parent != null)
			{
				propertyName = (PropertyName)sel_cell.Row.Parent.Cells[0].Value;
			}
			PropertyInfo property = m_Object.GetType().GetProperty(propertyName.Name);
			object value = property.GetValue(m_Object, null);
			object newValue = GetConverter(property.PropertyType).GetNewValue(propertyName, sel_cell, value);
			if (newValue != null)
			{
				CmdGroup cmdGroup2 = new CmdGroup();
				ChangePropertyCmd cmd = new ChangePropertyCmd(m_Object, propertyName.Name, value, newValue);
				cmdGroup2.Add(cmd);
				if (this.PropertyChanging != null)
				{
					this.PropertyChanging(m_Object, propertyName.Name, value, newValue, cmdGroup2);
				}
				cmdGroup.Add(cmdGroup2);
			}
		}
		if (m_CmdStack != null)
		{
			m_CmdStack.Do(cmdGroup);
		}
		else
		{
			cmdGroup.Do();
		}
		RefreshValues();
		foreach (ColRow sel_cell2 in sel_cells)
		{
			PropertyName propertyName2 = (PropertyName)sel_cell2.Row.Cells[0].Value;
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(m_Object, propertyName2.Name);
			}
		}
	}

	private void InitialiseDataGrid()
	{
		Column column = new Column();
		column.Width = 100;
		column.ReadOnly = true;
		m_DataGrid.Add(column);
		Column column2 = new Column();
		column2.BoolFalseImage = Resource1.FalseImage;
		column2.BoolTrueImage = Resource1.TrueImage;
		column2.WidthMode = Column.EWidthMode.Fill;
		m_DataGrid.Add(column2);
	}

	private static object GetCustomAttribute(PropertyInfo property_info, Type attrib_type)
	{
		object[] customAttributes = property_info.GetCustomAttributes(attrib_type, inherit: true);
		if (customAttributes.Length == 0)
		{
			return null;
		}
		return customAttributes[0];
	}

	private static int PropertySorter(PropertyInfo prop0, PropertyInfo prop1)
	{
		int num = ((EditablePropertyAttribute)GetCustomAttribute(prop0, typeof(EditablePropertyAttribute)))?.Index ?? int.MaxValue;
		int value = ((EditablePropertyAttribute)GetCustomAttribute(prop1, typeof(EditablePropertyAttribute)))?.Index ?? int.MaxValue;
		return num.CompareTo(value);
	}

	private static PropertyInfo[] GetProperties(object obj)
	{
		PropertyInfo[] properties = obj.GetType().GetProperties();
		List<PropertyInfo> list = new List<PropertyInfo>();
		PropertyInfo[] array = properties;
		foreach (PropertyInfo propertyInfo in array)
		{
			if (GetCustomAttribute(propertyInfo, typeof(EditablePropertyAttribute)) != null)
			{
				list.Add(propertyInfo);
			}
		}
		list.Sort(PropertySorter);
		return list.ToArray();
	}

	public void Initialise(object obj)
	{
		m_Object = obj;
		m_DataGrid.Clear();
		if (obj != null)
		{
			PropertyInfo[] properties = GetProperties(obj);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.GetGetMethod() != null)
				{
					object value = propertyInfo.GetValue(obj, null);
					PropertyConverter converter = GetConverter(propertyInfo.PropertyType);
					PropertyName property_name = new PropertyName(propertyInfo.Name);
					bool read_only = propertyInfo.GetSetMethod() == null;
					Row row = converter.CreateRow(obj, property_name, value, read_only);
					m_DataGrid.Rows.Add(row);
				}
			}
		}
		m_DataGrid.RefreshDataGrid();
	}

	private Row FindRow(PropertyName property_name)
	{
		foreach (Row row in m_DataGrid.Rows)
		{
			if (((PropertyName)row.Cells[0].Value).Equals(property_name))
			{
				return row;
			}
		}
		return null;
	}

	public void RefreshValues()
	{
		if (m_Object == null)
		{
			return;
		}
		m_IgnoreCellChanges = true;
		PropertyInfo[] properties = GetProperties(m_Object);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetGetMethod() != null)
			{
				PropertyName property_name = new PropertyName(propertyInfo.Name);
				Row row = FindRow(property_name);
				PropertyConverter converter = GetConverter(propertyInfo.PropertyType);
				object value = propertyInfo.GetValue(m_Object, null);
				converter.UpdateRow(row, value);
			}
		}
		m_IgnoreCellChanges = false;
		Refresh();
	}

	public void RegisterCustomControl(Type value_type, Type edit_control_type)
	{
		m_DataGrid.RegisterEditControl(value_type, edit_control_type);
	}
}
