using System;
using System.Collections;
using System.Collections.Generic;

namespace SCLCoreCLR;

public class BlockArray<T> : IEnumerable<T>, IEnumerable
{
	private class Block
	{
		public T[] m_Array;

		public int m_Count;

		public Block(int capacity)
		{
			m_Array = new T[capacity];
		}

		public void Add(T value)
		{
			m_Array[m_Count++] = value;
		}
	}

	private class BlockArrayEnumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private BlockArray<T> m_BlockArray;

		private int m_BlockSize;

		private IEnumerator<Block> m_BlockIter;

		private Block m_CurrentBlock;

		private int m_IndexInBlock;

		T IEnumerator<T>.Current => m_CurrentBlock.m_Array[m_IndexInBlock];

		public object Current => m_CurrentBlock.m_Array[m_IndexInBlock];

		public BlockArrayEnumerator(BlockArray<T> block_array)
		{
			m_BlockArray = block_array;
			m_BlockSize = m_BlockArray.BlockSize;
			m_BlockIter = block_array.m_Blocks.GetEnumerator();
			m_IndexInBlock = m_BlockSize - 1;
		}

		public void Dispose()
		{
			m_CurrentBlock = null;
			m_BlockArray = null;
		}

		public bool MoveNext()
		{
			m_IndexInBlock++;
			if (m_IndexInBlock == m_BlockSize && !MoveToNextBlock())
			{
				return false;
			}
			return m_IndexInBlock != m_CurrentBlock.m_Count;
		}

		private bool MoveToNextBlock()
		{
			m_IndexInBlock = 0;
			if (!m_BlockIter.MoveNext())
			{
				return false;
			}
			m_CurrentBlock = m_BlockIter.Current;
			return true;
		}

		public void Reset()
		{
			m_BlockIter.Reset();
			m_CurrentBlock = null;
			m_IndexInBlock = m_BlockSize - 1;
		}
	}

	private const int m_DefaultMinBlockCapacity = 8;

	private const int m_DefaultMaxBlockCapacity = 65536;

	private int m_MinBlockCapacity;

	private int m_MaxBlockCapacity;

	private int m_CurrentBlockSize;

	private int m_BlockShift;

	private List<Block> m_Blocks = new List<Block>();

	private int m_Count;

	private Block m_CurrentBlock;

	private int m_CurrentBlockCapacity;

	public int Count => m_Count;

	public int BlockSize => m_MaxBlockCapacity;

	public T this[int index]
	{
		get
		{
			int index2 = index >> m_BlockShift;
			int num = index & (m_MaxBlockCapacity - 1);
			return m_Blocks[index2].m_Array[num];
		}
		set
		{
			int index2 = index >> m_BlockShift;
			int num = index & (m_MaxBlockCapacity - 1);
			m_Blocks[index2].m_Array[num] = value;
		}
	}

	public BlockArray()
	{
		Initialise(8, 65536);
	}

	public BlockArray(int min_block_capacity, int max_block_capacity)
	{
		Initialise(min_block_capacity, max_block_capacity);
	}

	private void Initialise(int min_block_capacity, int max_block_capacity)
	{
		if (min_block_capacity == 0)
		{
			min_block_capacity = 1;
		}
		m_MinBlockCapacity = min_block_capacity;
		m_MaxBlockCapacity = max_block_capacity;
		m_BlockShift = 0;
		while (1 << m_BlockShift < max_block_capacity)
		{
			m_BlockShift++;
		}
		m_CurrentBlockCapacity = 0;
	}

	public void Add(T value)
	{
		if (m_CurrentBlockSize == m_CurrentBlockCapacity)
		{
			AddNewBlock();
		}
		m_CurrentBlock.Add(value);
		m_CurrentBlockSize++;
		m_Count++;
	}

	private void AddNewBlock()
	{
		if (m_CurrentBlockCapacity == m_MaxBlockCapacity)
		{
			m_CurrentBlock = new Block(m_CurrentBlockCapacity);
			m_Blocks.Add(m_CurrentBlock);
			m_CurrentBlockSize = 0;
			return;
		}
		m_CurrentBlockCapacity = ((m_CurrentBlockCapacity != 0) ? m_MaxBlockCapacity : m_MinBlockCapacity);
		m_CurrentBlock = new Block(m_CurrentBlockCapacity);
		if (m_Blocks.Count == 0)
		{
			m_Blocks.Add(m_CurrentBlock);
			return;
		}
		int num = 0;
		Block block = m_Blocks[0];
		T[] array = block.m_Array;
		foreach (T val in array)
		{
			m_CurrentBlock.m_Array[num++] = val;
		}
		m_CurrentBlock.m_Count = block.m_Count;
		m_Blocks[0] = m_CurrentBlock;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new BlockArrayEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new Exception("non generic GetEnumerator called!");
	}
}
