using System.Collections.Generic;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class ThreadFilter
{
	public List<ThreadFilterRow> m_Threads = new List<ThreadFilterRow>();

	public void Read(XmlReadStream stream)
	{
		m_Threads.Clear();
		if (stream.StartElement("Threads"))
		{
			for (int i = 0; i < stream.Count; i++)
			{
				stream.StartElement(i);
				ThreadFilterRow threadFilterRow = new ThreadFilterRow();
				threadFilterRow.Read(stream);
				m_Threads.Add(threadFilterRow);
				stream.EndElement();
			}
			stream.EndElement();
		}
	}

	public void Write(XmlWriteStream stream)
	{
		stream.StartElement("Threads");
		foreach (ThreadFilterRow thread in m_Threads)
		{
			stream.StartElement("Thread");
			thread.Write(stream);
			stream.EndElement();
		}
		stream.EndElement();
	}

	public ThreadFilterRow GetThreadFilterRow(string thread_name)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				return thread;
			}
		}
		return null;
	}

	public int GetThreadFilterRowIndex(string thread_name)
	{
		int num = 0;
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public bool IsCollapsed(string thread_name)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				return thread.m_Collapsed;
			}
		}
		return false;
	}

	public void SetCollapsed(string thread_name, bool collapsed)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				thread.m_Collapsed = collapsed;
				break;
			}
		}
	}

	public int GetCustomHeight(string thread_name)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				return thread.m_CustomHeight;
			}
		}
		return 0;
	}

	public void SetCustomHeight(string thread_name, int height)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				thread.m_CustomHeight = height;
				break;
			}
		}
	}

	public void ClearCustomHeight(string thread_name)
	{
		foreach (ThreadFilterRow thread in m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				thread.m_CustomHeight = 0;
				break;
			}
		}
	}
}
