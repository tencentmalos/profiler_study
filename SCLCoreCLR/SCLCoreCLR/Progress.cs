using System.Collections.Generic;

namespace SCLCoreCLR;

public class Progress
{
	private List<ProgressRange> m_Ranges = new List<ProgressRange>();

	private bool m_Started;

	private bool m_Finished;

	public int PercentComplete
	{
		get
		{
			return Misc.Clamp(m_Ranges[m_Ranges.Count - 1].Progress, 0, 100);
		}
		set
		{
			Set(value, 100L);
		}
	}

	public bool Finished => m_Finished;

	public Progress()
	{
		Reset();
	}

	public void Start()
	{
		Reset();
		m_Started = true;
	}

	private void Check()
	{
		_ = m_Ranges[m_Ranges.Count - 1].Progress;
	}

	public void Set(long value, long max = 100L)
	{
		value = (int)(value * 100 / max);
		ProgressRange progressRange = m_Ranges[m_Ranges.Count - 1];
		int num = progressRange.end - progressRange.start;
		progressRange.Progress = progressRange.start + (int)(value * num / 100);
		Check();
	}

	private void Reset()
	{
		m_Finished = false;
		m_Ranges.Clear();
		m_Started = true;
		Push(100);
		m_Started = false;
	}

	public void Push()
	{
		ProgressRange progressRange = m_Ranges[m_Ranges.Count - 1];
		Push(progressRange.end - progressRange.Progress);
	}

	public void Push(int length)
	{
		int num = ((m_Ranges.Count != 0) ? PercentComplete : 0);
		ProgressRange item = new ProgressRange(num, num + length);
		m_Ranges.Add(item);
		Check();
	}

	public void Pop()
	{
		int index = m_Ranges.Count - 1;
		ProgressRange progressRange = m_Ranges[index];
		m_Ranges.RemoveAt(index);
		m_Ranges[m_Ranges.Count - 1].Progress = progressRange.end;
		Check();
	}

	public void Finish()
	{
		m_Finished = true;
	}
}
