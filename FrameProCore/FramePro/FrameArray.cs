using System.Collections;
using System.Collections.Generic;
using SCLCoreCLR;

namespace FramePro;

internal class FrameArray : IEnumerable<Frame>, IEnumerable
{
	private class TimeBlock
	{
		public List<Frame> m_Frames = new List<Frame>();
	}

	private List<TimeBlock> m_TimeBlocks = new List<TimeBlock>();

	private List<Frame> m_Frames = new List<Frame>();

	private long m_StartTime = long.MaxValue;

	private long m_BlockDuration;

	public int Count => m_Frames.Count;

	public Frame this[int index] => m_Frames[index];

	public void SetBlockDuration(long duration)
	{
		m_BlockDuration = duration;
	}

	private long AlignToBlockDuration(long time)
	{
		return time / m_BlockDuration * m_BlockDuration;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return m_Frames.GetEnumerator();
	}

	public IEnumerator<Frame> GetEnumerator()
	{
		return m_Frames.GetEnumerator();
	}

	public void Add(Frame frame)
	{
		m_Frames.Add(frame);
		long startTime = frame.StartTime;
		if (m_StartTime == long.MaxValue)
		{
			m_StartTime = AlignToBlockDuration(startTime);
		}
		int num = (int)((startTime - m_StartTime) / m_BlockDuration);
		if (num >= 0 && num < m_TimeBlocks.Count)
		{
			m_TimeBlocks[num].m_Frames.Add(frame);
			return;
		}
		int count = m_TimeBlocks.Count;
		int num2 = num + 1;
		for (int i = count; i < num2; i++)
		{
			m_TimeBlocks.Add(new TimeBlock());
		}
		m_TimeBlocks[num].m_Frames.Add(frame);
	}

	public int GetIndex(long time)
	{
		int count = m_TimeBlocks.Count;
		if (count == 0)
		{
			return -1;
		}
		int num = Misc.Clamp((int)((time - m_StartTime) / m_BlockDuration), 0, count - 1);
		List<Frame> frames = m_TimeBlocks[num].m_Frames;
		while (frames.Count == 0 || (frames[0].StartTime > time && num > 0))
		{
			num--;
			frames = m_TimeBlocks[num].m_Frames;
		}
		foreach (Frame item in frames)
		{
			if (item.EndTime > time)
			{
				return item.Index;
			}
		}
		return -1;
	}

	public FrameArray Clone(int start_frame_index, int end_frame_index)
	{
		FrameArray frameArray = new FrameArray();
		frameArray.SetBlockDuration(m_BlockDuration);
		int num = 0;
		for (int i = start_frame_index; i <= end_frame_index; i++)
		{
			Frame frame = new Frame(num);
			CoreUtils.Copy(frame, m_Frames[i]);
			frameArray.Add(frame);
			num++;
		}
		return frameArray;
	}
}
