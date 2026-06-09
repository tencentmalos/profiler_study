using System.Collections.Generic;

namespace ProfilerStudy;

public class ClassAllocator<T> where T : new()
{
	private List<T> m_FreeList = new List<T>();

	public T Alloc()
	{
		int count = m_FreeList.Count;
		if (count != 0)
		{
			T result = m_FreeList[count - 1];
			m_FreeList.RemoveAt(count - 1);
			return result;
		}
		return new T();
	}

	public void Free(T item)
	{
		m_FreeList.Add(item);
	}
}
