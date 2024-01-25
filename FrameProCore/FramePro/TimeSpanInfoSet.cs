using System.Collections.Generic;
using System.IO;

namespace FramePro;

public class TimeSpanInfoSet : IFrameProSerialisable
{
	private Dictionary<TimeSpanInfo, int> m_TimeSpanInfoMap = new Dictionary<TimeSpanInfo, int>();

	private List<TimeSpanInfo> m_TimeSpanInfoList = new List<TimeSpanInfo>();

	private Dictionary<long, int> m_NameToIdMap = new Dictionary<long, int>();

	public int Count => m_TimeSpanInfoList.Count;

	public int GetId(long name, long source_info, int core, int callstack_id)
	{
		TimeSpanInfo timeSpanInfo = new TimeSpanInfo(name, source_info, core, callstack_id);
		if (!m_TimeSpanInfoMap.TryGetValue(timeSpanInfo, out var value))
		{
			value = m_TimeSpanInfoList.Count;
			m_TimeSpanInfoList.Add(timeSpanInfo);
			m_TimeSpanInfoMap[timeSpanInfo] = value;
		}
		return value;
	}

	public TimeSpanInfo GetTimeSpanInfo(int id)
	{
		return m_TimeSpanInfoList[id];
	}

	public void OnStringRemapped(long old_string_id, long new_string_id)
	{
		if (m_NameToIdMap.TryGetValue(old_string_id, out var value))
		{
			TimeSpanInfo timeSpanInfo = m_TimeSpanInfoList[value];
			timeSpanInfo.OnStringRemapped(old_string_id, new_string_id);
			m_TimeSpanInfoList[value] = timeSpanInfo;
			m_TimeSpanInfoMap[timeSpanInfo] = value;
		}
	}

	public void Read(BinaryReader binary_reader, int version)
	{
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			TimeSpanInfo timeSpanInfo = default(TimeSpanInfo);
			timeSpanInfo.Read(binary_reader, version);
			m_TimeSpanInfoList.Add(timeSpanInfo);
			m_TimeSpanInfoList[i] = timeSpanInfo;
			m_NameToIdMap[timeSpanInfo.Name] = i;
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_TimeSpanInfoList.Count);
		foreach (TimeSpanInfo timeSpanInfo in m_TimeSpanInfoList)
		{
			timeSpanInfo.Write(binary_writer);
		}
	}

	public int GetIdFromName(long name)
	{
		if (m_NameToIdMap.TryGetValue(name, out var value))
		{
			return value;
		}
		return TimeSpanInfo.InvalidInfoId;
	}
}
