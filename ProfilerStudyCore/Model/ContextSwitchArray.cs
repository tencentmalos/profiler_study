using System;
using System.Collections.Generic;
using System.IO;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class ContextSwitchArray
{
	private class TimeBlock
	{
		public List<ContextSwitch> m_ContextSwitches = new List<ContextSwitch>();

		public void Read(BinaryReader reader)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ContextSwitch item = default(ContextSwitch);
				item.Read(reader);
				m_ContextSwitches.Add(item);
			}
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(m_ContextSwitches.Count);
			foreach (ContextSwitch contextSwitch in m_ContextSwitches)
			{
				contextSwitch.Write(writer);
			}
		}
	}

	private long m_StartTime = long.MaxValue;

	private long m_BlockDuration;

	private List<TimeBlock> m_TimeBlocks = new List<TimeBlock>();

	public ContextSwitchArray(long block_duration)
	{
		m_BlockDuration = block_duration;
	}

	private long AlignTimeToTimeBlock(long time)
	{
		return time / m_BlockDuration * m_BlockDuration;
	}

	public void Add(ContextSwitch context_switch)
	{
		long timestamp = context_switch.m_Timestamp;
		if (m_StartTime == long.MaxValue)
		{
			m_StartTime = AlignTimeToTimeBlock(timestamp);
		}
		int count = m_TimeBlocks.Count;
		int num = (int)((timestamp >= m_StartTime) ? ((timestamp - m_StartTime) / m_BlockDuration) : ((int)((timestamp - m_StartTime + 1) / m_BlockDuration) - 1));
		if (num < 0)
		{
			int num2 = -num;
			List<TimeBlock> list = new List<TimeBlock>(num2);
			for (int i = 0; i < num2; i++)
			{
				list.Add(new TimeBlock());
			}
			m_TimeBlocks.InsertRange(0, list);
			m_StartTime = AlignTimeToTimeBlock(timestamp);
			num = 0;
		}
		else if (num >= count)
		{
			int num3 = num + 1 - count;
			for (int j = 0; j < num3; j++)
			{
				m_TimeBlocks.Add(new TimeBlock());
			}
		}
		List<ContextSwitch> contextSwitches = m_TimeBlocks[num].m_ContextSwitches;
		int count2 = contextSwitches.Count;
		if (count2 == 0 || contextSwitches[count2 - 1].m_Timestamp <= timestamp)
		{
			contextSwitches.Add(context_switch);
			return;
		}
		int num4 = 0;
		using (List<ContextSwitch>.Enumerator enumerator = contextSwitches.GetEnumerator())
		{
			while (enumerator.MoveNext() && enumerator.Current.m_Timestamp <= timestamp)
			{
				num4++;
			}
		}
		contextSwitches.Insert(num4, context_switch);
	}

	private void FindFirstContextSwitch(long start_time, out int block_index, out int index_in_block)
	{
		index_in_block = 0;
		if (m_TimeBlocks.Count == 0)
		{
			block_index = 0;
			return;
		}
		block_index = Misc.Clamp((int)((start_time - m_StartTime) / m_BlockDuration), 0, m_TimeBlocks.Count - 1);
		TimeBlock timeBlock = m_TimeBlocks[block_index];
		while (index_in_block < timeBlock.m_ContextSwitches.Count - 1 && timeBlock.m_ContextSwitches[index_in_block].m_Timestamp < start_time)
		{
			index_in_block++;
		}
		if (timeBlock.m_ContextSwitches.Count != 0 && timeBlock.m_ContextSwitches[index_in_block].m_Timestamp < start_time)
		{
			return;
		}
		if (index_in_block != 0)
		{
			index_in_block--;
			return;
		}
		if (block_index > 0)
		{
			block_index--;
			while (block_index > 0 && m_TimeBlocks[block_index].m_ContextSwitches.Count == 0)
			{
				block_index--;
			}
		}
		index_in_block = Math.Max(0, m_TimeBlocks[block_index].m_ContextSwitches.Count - 1);
	}

	public void GetContextSwitches(long start_time, long end_time, List<ContextSwitch> context_switches)
	{
		if (m_TimeBlocks.Count == 0)
		{
			return;
		}
		FindFirstContextSwitch(start_time, out var block_index, out var index_in_block);
		for (int i = block_index; i < m_TimeBlocks.Count; i++)
		{
			List<ContextSwitch> contextSwitches = m_TimeBlocks[i].m_ContextSwitches;
			for (int j = index_in_block; j < contextSwitches.Count; j++)
			{
				ContextSwitch item = contextSwitches[j];
				context_switches.Add(item);
				if (item.m_Timestamp > end_time)
				{
					return;
				}
			}
			index_in_block = 0;
		}
	}

	public void Read(BinaryReader reader)
	{
		m_StartTime = reader.ReadInt64();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			TimeBlock timeBlock = new TimeBlock();
			timeBlock.Read(reader);
			m_TimeBlocks.Add(timeBlock);
		}
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_StartTime);
		writer.Write(m_TimeBlocks.Count);
		foreach (TimeBlock timeBlock in m_TimeBlocks)
		{
			timeBlock.Write(writer);
		}
	}

	public ContextSwitchArray Clone(long start_time, long end_time)
	{
		ContextSwitchArray contextSwitchArray = new ContextSwitchArray(m_BlockDuration);
		List<ContextSwitch> list = new List<ContextSwitch>();
		GetContextSwitches(start_time, end_time, list);
		foreach (ContextSwitch item in list)
		{
			contextSwitchArray.Add(item);
		}
		return contextSwitchArray;
	}
}
