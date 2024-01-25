using System.Threading;

namespace FramePro;

public class ReadWriteLock
{
	private ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim();

	public void EnterReadLock()
	{
		m_Lock.EnterReadLock();
	}

	public void ExitReadLock()
	{
		m_Lock.ExitReadLock();
	}

	public void EnterWriteLock()
	{
		m_Lock.EnterWriteLock();
	}

	public void ExitWriteLock()
	{
		m_Lock.ExitWriteLock();
	}
}
