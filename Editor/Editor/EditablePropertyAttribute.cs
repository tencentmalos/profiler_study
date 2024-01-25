using System;

namespace Editor;

[AttributeUsage(AttributeTargets.Property)]
public class EditablePropertyAttribute : Attribute
{
	private int m_Index;

	public int Index => m_Index;

	public EditablePropertyAttribute(int index)
	{
		m_Index = index;
	}
}
