using System;

namespace FramePro;

public struct ReadLockScope : IDisposable
{
	private ReadWriteLock m_Lock;

	public ReadLockScope(ReadWriteLock read_write_lock)
	{
		m_Lock = read_write_lock;
		m_Lock.EnterReadLock();
	}

	public void Dispose()
	{
		m_Lock.ExitReadLock();
	}
}
