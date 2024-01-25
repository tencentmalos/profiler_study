using System;
using System.Collections.Generic;

namespace Editor;

public class CmdGroup : ICommand, IDisposable
{
	private List<ICommand> m_Cmds = new List<ICommand>();

	private string m_Name;

	public string Description => "CmdGroup" + (string.IsNullOrEmpty(m_Name) ? "" : (": " + m_Name));

	public bool Empty => m_Cmds.Count == 0;

	public CmdGroup()
	{
	}

	public CmdGroup(string name)
	{
		m_Name = name;
	}

	public void Add(ICommand cmd)
	{
		m_Cmds.Add(cmd);
	}

	public void Do()
	{
		foreach (ICommand cmd in m_Cmds)
		{
			cmd.Do();
		}
	}

	public void Undo()
	{
		for (int num = m_Cmds.Count - 1; num >= 0; num--)
		{
			m_Cmds[num].Undo();
		}
	}

	public void Dispose()
	{
		foreach (ICommand cmd in m_Cmds)
		{
			cmd.Dispose();
		}
	}
}
