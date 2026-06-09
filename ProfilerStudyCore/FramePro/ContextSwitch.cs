using System.IO;

namespace ProfilerStudy;

public struct ContextSwitch
{
	public long m_Timestamp;

	public int m_ProcessId;

	public int m_CPUId;

	public int m_OldThreadId;

	public int m_NewThreadId;

	public ThreadState m_OldThreadState;

	public ThreadWaitReason m_OldThreadWaitReason;

	public void Read(BinaryReader reader)
	{
		m_Timestamp = reader.ReadInt64();
		m_ProcessId = reader.ReadInt32();
		m_CPUId = reader.ReadInt32();
		m_OldThreadId = reader.ReadInt32();
		m_NewThreadId = reader.ReadInt32();
		m_OldThreadState = (ThreadState)reader.ReadInt32();
		m_OldThreadWaitReason = (ThreadWaitReason)reader.ReadInt32();
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_Timestamp);
		writer.Write(m_ProcessId);
		writer.Write(m_CPUId);
		writer.Write(m_OldThreadId);
		writer.Write(m_NewThreadId);
		writer.Write((int)m_OldThreadState);
		writer.Write((int)m_OldThreadWaitReason);
	}
}
