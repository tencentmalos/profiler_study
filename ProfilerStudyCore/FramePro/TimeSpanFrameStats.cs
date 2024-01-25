using System;
using System.Collections.Generic;
using System.IO;
using SCLCoreCLR;

namespace FramePro;

internal class TimeSpanFrameStats
{
	public BlockArray<TimeSpanFrame> m_Frames = new BlockArray<TimeSpanFrame>(128, 4096);

	public long m_TotalTime;

	public long m_TotalCount;

	public long m_MaxTime;

	public long m_MaxTimeStartTime;

	public long m_MaxFrameTime;

	public long m_MaxFrameCount;

	public Dictionary<int, TimeSpanThreadInfo> m_ThreadInfo = new Dictionary<int, TimeSpanThreadInfo>();

	public ReadWriteLock m_Lock = new ReadWriteLock();

	public void Read(BinaryReader binary_reader, int version)
	{
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			long frame_end_time = 0L;
			if (version >= 27)
			{
				frame_end_time = binary_reader.ReadInt64();
			}
			long total_time_span_duration = binary_reader.ReadInt64();
			int count = binary_reader.ReadInt32();
			TimeSpanFrame value = new TimeSpanFrame(frame_end_time, total_time_span_duration, count);
			m_Frames.Add(value);
		}
		m_TotalTime = binary_reader.ReadInt64();
		m_TotalCount = binary_reader.ReadInt64();
		if (version >= 45)
		{
			m_MaxTime = binary_reader.ReadInt64();
			m_MaxTimeStartTime = binary_reader.ReadInt64();
			m_MaxFrameTime = binary_reader.ReadInt64();
			m_MaxFrameCount = binary_reader.ReadInt64();
		}
		if (version < 43)
		{
			return;
		}
		int num2 = binary_reader.ReadInt32();
		for (int j = 0; j < num2; j++)
		{
			int key = binary_reader.ReadInt32();
			TimeSpanThreadInfo timeSpanThreadInfo = new TimeSpanThreadInfo();
			timeSpanThreadInfo.m_TotalTime = binary_reader.ReadInt64();
			timeSpanThreadInfo.m_TotalCount = binary_reader.ReadInt64();
			if (version >= 45)
			{
				timeSpanThreadInfo.m_MaxTime = binary_reader.ReadInt64();
				timeSpanThreadInfo.m_MaxTimeStartTime = binary_reader.ReadInt64();
			}
			m_ThreadInfo[key] = timeSpanThreadInfo;
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Frames.Count);
		foreach (TimeSpanFrame frame in m_Frames)
		{
			binary_writer.Write(frame.m_FrameEndTime);
			binary_writer.Write(frame.m_TotalTimeSpanDuration);
			binary_writer.Write(frame.m_Count);
		}
		binary_writer.Write(m_TotalTime);
		binary_writer.Write(m_TotalCount);
		binary_writer.Write(m_MaxTime);
		binary_writer.Write(m_MaxTimeStartTime);
		binary_writer.Write(m_MaxFrameTime);
		binary_writer.Write(m_MaxFrameCount);
		binary_writer.Write(m_ThreadInfo.Count);
		foreach (KeyValuePair<int, TimeSpanThreadInfo> item in m_ThreadInfo)
		{
			binary_writer.Write(item.Key);
			binary_writer.Write(item.Value.m_TotalTime);
			binary_writer.Write(item.Value.m_TotalCount);
			binary_writer.Write(item.Value.m_MaxTime);
			binary_writer.Write(item.Value.m_MaxTimeStartTime);
		}
	}

	public TimeSpanFrameStats Clone(int start_frame_index, int end_frame_index)
	{
		TimeSpanFrameStats timeSpanFrameStats = new TimeSpanFrameStats();
		start_frame_index = Math.Min(start_frame_index, m_Frames.Count);
		end_frame_index = Math.Min(end_frame_index, m_Frames.Count);
		if (start_frame_index < m_Frames.Count)
		{
			end_frame_index = Math.Min(end_frame_index, m_Frames.Count - 1);
			for (int i = start_frame_index; i <= end_frame_index; i++)
			{
				TimeSpanFrame value = m_Frames[i];
				timeSpanFrameStats.m_Frames.Add(value);
				timeSpanFrameStats.m_TotalTime += value.m_TotalTimeSpanDuration;
				timeSpanFrameStats.m_TotalCount += value.m_Count;
				if (value.m_TotalTimeSpanDuration > timeSpanFrameStats.m_MaxTime)
				{
					timeSpanFrameStats.m_MaxTime = value.m_TotalTimeSpanDuration;
				}
				if (value.m_TotalTimeSpanDuration > timeSpanFrameStats.m_MaxFrameTime)
				{
					timeSpanFrameStats.m_MaxFrameTime = value.m_TotalTimeSpanDuration;
				}
				if (value.m_Count > timeSpanFrameStats.m_MaxFrameCount)
				{
					timeSpanFrameStats.m_MaxFrameCount = value.m_Count;
				}
			}
		}
		return timeSpanFrameStats;
	}

	public TimeSpanThreadInfo GetTimeSpanThreadInfo(int thread_id)
	{
		TimeSpanThreadInfo value = null;
		if (!m_ThreadInfo.TryGetValue(thread_id, out value))
		{
			value = new TimeSpanThreadInfo();
			m_ThreadInfo[thread_id] = value;
		}
		return value;
	}

	public TimeSpanThreadInfo TryGetTimeSpanThreadInfo(int thread_id)
	{
		TimeSpanThreadInfo value = null;
		m_ThreadInfo.TryGetValue(thread_id, out value);
		return value;
	}
}
