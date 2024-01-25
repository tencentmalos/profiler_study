using System.Collections.Generic;

namespace FramePro;

internal sealed class HiResTimerScopePacket : IPacket
{
	public long m_StartTime;

	public long m_EndTime;

	public int m_Count;

	public int m_ThreadId;

	public List<HiResTimer> m_Timers;

	public void Read(ReceiveStream reader, int packed_value)
	{
		reader.ReadInt32();
		m_StartTime = reader.ReadInt64();
		m_EndTime = reader.ReadInt64();
		m_Count = reader.ReadInt32();
		m_ThreadId = reader.ReadInt32();
		m_Timers = new List<HiResTimer>();
		for (int i = 0; i < m_Count; i++)
		{
			HiResTimer item = default(HiResTimer);
			item.m_Name = reader.ReadInt64();
			item.m_Duration = reader.ReadInt64() * 100;
			item.m_Count = reader.ReadInt64();
			m_Timers.Add(item);
		}
	}

	public int GetSize()
	{
		return 24 + m_Count * 24;
	}
}
