using System;
using System.Collections;
using System.Collections.Generic;

namespace FramePro;

internal class TimeArray<T> : IEnumerable<T>, IEnumerable where T : TimeItem
{
	public class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private TimeArray<T> m_Array;

		private Index m_Index;

		private bool m_Started;

		public T Current => m_Array[m_Index];

		object IEnumerator.Current => m_Array[m_Index];

		public Enumerator(TimeArray<T> array)
		{
			m_Array = array;
		}

		public bool MoveNext()
		{
			if (!m_Started)
			{
				m_Index = m_Array.FirstIndex;
				m_Started = true;
			}
			else
			{
				m_Index = m_Array.MoveNext(m_Index);
			}
			return m_Index.IsValid;
		}

		public void Dispose()
		{
			m_Array = null;
		}

		public void Reset()
		{
			m_Index = TimeArray<T>.InvalidIndex;
			m_Started = false;
		}
	}

	private class TimeBlock
	{
		public List<T> m_Items = new List<T>();
	}

	public struct Index
	{
		private int m_BlockIndex;

		private int m_ItemIndex;

		public int BlockIndex => m_BlockIndex;

		public int ItemIndex => m_ItemIndex;

		public bool IsValid => m_BlockIndex != -1;

		public Index(int block_index, int item_index)
		{
			m_BlockIndex = block_index;
			m_ItemIndex = item_index;
		}

		public override bool Equals(object obj)
		{
			if (obj is Index)
			{
				if (m_BlockIndex == ((Index)obj).m_BlockIndex)
				{
					return m_ItemIndex == ((Index)obj).m_ItemIndex;
				}
				return false;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_BlockIndex.GetHashCode() ^ m_ItemIndex.GetHashCode();
		}
	}

	private const long m_DefaultBlockDurationMS = 100L;

	private long m_FirstItemTime = -1L;

	private long m_BlockDuration;

	private long m_Count;

	private List<TimeBlock> m_TimeBlocks = new List<TimeBlock>();

	public T this[Index index] => m_TimeBlocks[index.BlockIndex].m_Items[index.ItemIndex];

	public Index FirstIndex
	{
		get
		{
			if (m_Count == 0L)
			{
				return InvalidIndex;
			}
			return new Index(0, 0);
		}
	}

	public Index LastIndex
	{
		get
		{
			if (m_TimeBlocks.Count == 0)
			{
				return InvalidIndex;
			}
			return new Index(m_TimeBlocks.Count - 1, m_TimeBlocks[m_TimeBlocks.Count - 1].m_Items.Count - 1);
		}
	}

	public static Index InvalidIndex => new Index(-1, -1);

	public long Count => m_Count;

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public TimeArray(long timer_frequency)
	{
		m_BlockDuration = timer_frequency * 100 / 1000;
	}

	public void Add(T item)
	{
		long time = item.Time;
		if (m_Count == 0L)
		{
			m_FirstItemTime = time;
		}
		int num = (int)((time - m_FirstItemTime) / m_BlockDuration);
		while (m_TimeBlocks.Count <= num)
		{
			m_TimeBlocks.Add(new TimeBlock());
		}
		m_TimeBlocks[num].m_Items.Add(item);
		m_Count++;
	}

	public Index GetIndex(long time)
	{
		Index indexInternal = GetIndexInternal(time);
		if (indexInternal.IsValid)
		{
			MoveNext(indexInternal);
		}
		return indexInternal;
	}

	private Index GetIndexInternal(long time)
	{
		int num = (int)((time - m_FirstItemTime) / m_BlockDuration);
		if (num < 0 || time < m_FirstItemTime)
		{
			return FirstIndex;
		}
		if (num >= m_TimeBlocks.Count)
		{
			return LastIndex;
		}
		TimeBlock timeBlock = m_TimeBlocks[num];
		if (timeBlock.m_Items.Count == 0 || timeBlock.m_Items[0].Time > time)
		{
			num--;
			while (m_TimeBlocks[num].m_Items.Count == 0)
			{
				num--;
			}
			return new Index(num, m_TimeBlocks[num].m_Items.Count - 1);
		}
		int item_index = 0;
		for (int i = 1; i < timeBlock.m_Items.Count && timeBlock.m_Items[i].Time <= time; i++)
		{
			item_index = i;
		}
		return new Index(num, item_index);
	}

	public Index MoveNext(Index index)
	{
		if (index.ItemIndex < m_TimeBlocks[index.BlockIndex].m_Items.Count - 1)
		{
			return new Index(index.BlockIndex, index.ItemIndex + 1);
		}
		if (index.BlockIndex < m_TimeBlocks.Count - 1)
		{
			int i;
			for (i = index.BlockIndex + 1; i < m_TimeBlocks.Count && m_TimeBlocks[i].m_Items.Count == 0; i++)
			{
			}
			if (i < m_TimeBlocks.Count)
			{
				return new Index(i, 0);
			}
		}
		return InvalidIndex;
	}

	public Index MovePrev(Index index)
	{
		if (index.ItemIndex > 0)
		{
			return new Index(index.BlockIndex, index.ItemIndex - 1);
		}
		if (index.BlockIndex > 0)
		{
			int num = index.BlockIndex - 1;
			while (num >= 0 && m_TimeBlocks[num].m_Items.Count == 0)
			{
				num--;
			}
			if (num != 0)
			{
				return new Index(num, m_TimeBlocks[num].m_Items.Count - 1);
			}
		}
		return InvalidIndex;
	}
}
