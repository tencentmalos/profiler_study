using System.Collections.Generic;

namespace Editor;

public class CmdStack
{
	private List<ICommand> m_Stack = new List<ICommand>();

	private int m_Index;

	private int m_LastSavedIndex;

	private bool m_Modified;

	public bool Modified => m_Modified;

	public int CurrentIndex => m_Index;

	public event ModifiedChangedHandler ModifiedChanged;

	public void Do(ICommand cmd)
	{
		cmd.Do();
		if (m_LastSavedIndex > m_Index)
		{
			m_LastSavedIndex = 0;
		}
		for (int i = m_Index; i < m_Stack.Count; i++)
		{
			m_Stack[i].Dispose();
		}
		m_Stack.RemoveRange(m_Index, m_Stack.Count - m_Index);
		m_Stack.Add(cmd);
		m_Index++;
		UpdateModifiedFlag();
	}

	public void Undo()
	{
		if (m_Index != 0)
		{
			m_Stack[--m_Index].Undo();
		}
		UpdateModifiedFlag();
	}

	public void Redo()
	{
		if (m_Index < m_Stack.Count)
		{
			m_Stack[m_Index++].Do();
		}
		UpdateModifiedFlag();
	}

	public void Clear()
	{
		for (int i = 0; i < m_Stack.Count; i++)
		{
			m_Stack[i].Dispose();
		}
		m_Stack.Clear();
		m_Index = 0;
		UpdateModifiedFlag();
	}

	public void SetSaved()
	{
		m_LastSavedIndex = m_Index;
		UpdateModifiedFlag();
	}

	private void UpdateModifiedFlag()
	{
		bool flag = m_Index != m_LastSavedIndex;
		if (flag != m_Modified)
		{
			m_Modified = flag;
			if (this.ModifiedChanged != null)
			{
				this.ModifiedChanged(flag);
			}
		}
	}
}
