namespace FramePro;

internal sealed class ContextSwitchPacket : IPacket
{
	public int m_Core;

	public long m_Timestamp;

	public int m_ProcessId;

	public int m_OldThreadId;

	public int m_NewThreadId;

	public ThreadState m_OldThreadState;

	public ThreadWaitReason m_OldThreadWaitReason;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Core = reader.ReadInt32();
		m_Timestamp = reader.ReadInt64();
		m_ProcessId = reader.ReadInt32();
		m_OldThreadId = reader.ReadInt32();
		m_NewThreadId = reader.ReadInt32();
		m_OldThreadState = (ThreadState)reader.ReadInt32();
		m_OldThreadWaitReason = (ThreadWaitReason)reader.ReadInt32();
		reader.ReadInt32();
	}

	public int GetSize()
	{
		return 32;
	}
}
