using System.Collections.Generic;

namespace ProfilerStudy;

internal class PacketAllocatorT<T> where T : new()
{
	private static PacketAllocatorT<T> m_Inst;

	private List<T> m_FreeList = new List<T>();

	private ReadWriteLock m_Lock = new ReadWriteLock();

	public static PacketAllocatorT<T> Inst
	{
		get
		{
			if (m_Inst == null)
			{
				m_Inst = new PacketAllocatorT<T>();
			}
			return m_Inst;
		}
	}

	public T Alloc()
	{
		using (new WriteLockScope(m_Lock))
		{
			int count = m_FreeList.Count;
			if (count != 0)
			{
				int index = count - 1;
				T result = m_FreeList[index];
				m_FreeList.RemoveAt(index);
				return result;
			}
		}
		return new T();
	}

	public void Free(T packet)
	{
		using (new WriteLockScope(m_Lock))
		{
			m_FreeList.Add(packet);
		}
	}
}
