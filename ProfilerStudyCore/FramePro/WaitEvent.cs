using System.IO;

namespace FramePro;

public sealed class WaitEvent : TimeItem
{
	public enum WaitEventMode
	{
		Start,
		Stop,
		Trigger
	}

	private long m_EventId;

	private long m_Time;

	private int m_ThreadId;

	private int m_Core;

	private WaitEventMode m_Mode;

	private int m_StableSortIndex;

	private static int m_NextStableSortIndex;

	public long EventId => m_EventId;

	public long Time => m_Time;

	public int ThreadId => m_ThreadId;

	public int Core => m_Core;

	public WaitEventMode Mode => m_Mode;

	public int StableSortIndex => m_StableSortIndex;

	public WaitEvent()
	{
		SetStableSortIndex();
	}

	public WaitEvent(long event_id, long time, int thread_id, int core, WaitEventMode mode)
	{
		m_EventId = event_id;
		m_Time = time;
		m_ThreadId = thread_id;
		m_Core = core;
		m_Mode = mode;
		SetStableSortIndex();
	}

	private void SetStableSortIndex()
	{
		m_StableSortIndex = m_NextStableSortIndex++;
	}

	public void Read(BinaryReader reader)
	{
		m_EventId = reader.ReadInt64();
		m_Time = reader.ReadInt64();
		m_ThreadId = reader.ReadInt32();
		m_Core = reader.ReadInt32();
		m_Mode = (WaitEventMode)reader.ReadInt32();
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_EventId);
		writer.Write(m_Time);
		writer.Write(m_ThreadId);
		writer.Write(m_Core);
		writer.Write((int)m_Mode);
	}
}
