using System;

namespace Editor;

public class ChangePropertyCmd : ICommand, IDisposable
{
	private object m_Object;

	private string m_PropertyName;

	private object m_OldValue;

	private object m_NewValue;

	public string Description => "ChangePropertyCmd: " + m_PropertyName + " old: " + m_OldValue?.ToString() + " new: " + m_NewValue;

	public ChangePropertyCmd(object obj, string property_name, object old_value, object new_value)
	{
		m_Object = obj;
		m_PropertyName = property_name;
		m_OldValue = old_value;
		m_NewValue = new_value;
	}

	public void Do()
	{
		m_Object.GetType().GetProperty(m_PropertyName).SetValue(m_Object, m_NewValue, null);
	}

	public void Undo()
	{
		m_Object.GetType().GetProperty(m_PropertyName).SetValue(m_Object, m_OldValue, null);
	}

	public void Dispose()
	{
		m_Object = null;
		m_PropertyName = null;
		m_OldValue = null;
		m_NewValue = null;
	}
}
