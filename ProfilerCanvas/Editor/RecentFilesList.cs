using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Editor;

public class RecentFilesList
{
	private List<string> m_RecentFiles;

	private ToolStripMenuItem m_MenuItem;

	private int m_MaxRecentFilesCount = 10;

	public int MaxRecentFilesCount
	{
		get
		{
			return m_MaxRecentFilesCount;
		}
		set
		{
			m_MaxRecentFilesCount = value;
			TrimList();
		}
	}

	public event RecentFileMenuItemClickedHandler RecentFileMenuItemClicked;

	public RecentFilesList(List<string> recent_files, ToolStripMenuItem recent_files_menu_item)
	{
		m_RecentFiles = recent_files;
		m_MenuItem = recent_files_menu_item;
		UpdateMenuItems();
	}

	private void UpdateMenuItems()
	{
		m_MenuItem.DropDownItems.Clear();
		foreach (string recentFile in m_RecentFiles)
		{
			ToolStripMenuItem value = CreateNewMenuItem(recentFile);
			m_MenuItem.DropDownItems.Add(value);
		}
	}

	private void RecentFileMenuItemClick(object sender, EventArgs e)
	{
		ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
		if (this.RecentFileMenuItemClicked != null)
		{
			this.RecentFileMenuItemClicked(toolStripMenuItem.Text);
		}
	}

	public void OnFileOpened(string filename)
	{
		int num = -1;
		string text = filename.ToLower();
		for (int i = 0; i < m_RecentFiles.Count; i++)
		{
			if (m_RecentFiles[i].ToLower() == text)
			{
				num = i;
				break;
			}
		}
		if (num != -1)
		{
			m_RecentFiles.RemoveAt(num);
			m_MenuItem.DropDownItems.RemoveAt(num);
		}
		m_RecentFiles.Insert(0, filename);
		ToolStripMenuItem value = CreateNewMenuItem(filename);
		m_MenuItem.DropDownItems.Insert(0, value);
		TrimList();
	}

	private ToolStripMenuItem CreateNewMenuItem(string filename)
	{
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(filename);
		toolStripMenuItem.Click += RecentFileMenuItemClick;
		return toolStripMenuItem;
	}

	public void TrimList()
	{
		while (m_RecentFiles.Count > m_MaxRecentFilesCount)
		{
			int index = m_RecentFiles.Count - 1;
			m_RecentFiles.RemoveAt(index);
			m_MenuItem.DropDownItems.RemoveAt(index);
		}
	}
}
