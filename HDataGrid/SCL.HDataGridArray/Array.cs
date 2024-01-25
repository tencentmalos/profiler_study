using System;
using System.Collections;
using System.Collections.Generic;

namespace SCL.HDataGridArray;

public class Array<T> : IEnumerable<T>, IEnumerable
{
	public class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private int m_Index = -1;

		private Array<T> m_Array;

		public T Current => m_Array[m_Index];

		object IEnumerator.Current => m_Array[m_Index];

		public Enumerator(Array<T> array)
		{
			m_Array = array;
		}

		public void Reset()
		{
			m_Index = -1;
		}

		public bool MoveNext()
		{
			if (m_Index == m_Array.Count)
			{
				return false;
			}
			m_Index++;
			return m_Index != m_Array.Count;
		}

		public void Dispose()
		{
		}
	}

	private int m_Capacity;

	private int m_Count;

	private T[] m_Array;

	public int Count => m_Count;

	public T this[int index]
	{
		get
		{
			return m_Array[index];
		}
		set
		{
			m_Array[index] = value;
		}
	}

	public Array()
	{
	}

	public Array(Array<T> other)
	{
		m_Capacity = other.m_Count;
		m_Count = other.m_Count;
		m_Array = new T[m_Count];
		for (int i = 0; i < Count; i++)
		{
			m_Array[i] = other.m_Array[i];
		}
	}

	public void Add(T value)
	{
		if (m_Count == m_Capacity)
		{
			Grow();
		}
		m_Array[m_Count++] = value;
	}

	private void Grow()
	{
		m_Capacity = ((m_Capacity == 0) ? 1 : (2 * m_Capacity));
		T[] array = new T[m_Capacity];
		for (int i = 0; i < m_Count; i++)
		{
			array[i] = m_Array[i];
		}
		m_Array = array;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public void Clear()
	{
		m_Count = 0;
	}
}
