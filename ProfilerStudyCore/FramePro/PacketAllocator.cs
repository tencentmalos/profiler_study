namespace FramePro;

internal class PacketAllocator
{
	private bool m_CalledFree;

	public bool CalledFree
	{
		get
		{
			return m_CalledFree;
		}
		set
		{
			m_CalledFree = value;
		}
	}

	public T Alloc<T>() where T : new()
	{
		return PacketAllocatorT<T>.Inst.Alloc();
	}

	public void Free<T>(T item) where T : new()
	{
		PacketAllocatorT<T>.Inst.Free(item);
		m_CalledFree = true;
	}
}
