using System;

namespace FramePro;

public struct WriteLockScope : IDisposable
{
	private ReadWriteLock m_Lock;

	public WriteLockScope(ReadWriteLock read_write_lock)
	{
		m_Lock = read_write_lock;
		read_write_lock.EnterWriteLock();
	}

	public void Dispose()
	{
		m_Lock.ExitWriteLock();
	}
}
