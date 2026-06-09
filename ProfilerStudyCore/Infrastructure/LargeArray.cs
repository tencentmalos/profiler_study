using System;
using System.Collections;
using System.Collections.Generic;

namespace ProfilerStudy;

internal class LargeArray<T> : IEnumerable<T>, IEnumerable
{
	private class Iterator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private List<List<T>> m_Arrays;

		private IEnumerator<List<T>> m_ListIter;

		private IEnumerator<T> m_Iter;

		public T Current => m_Iter.Current;

		object IEnumerator.Current => m_Iter.Current;

		public Iterator(List<List<T>> arrays)
		{
			m_Arrays = arrays;
			Reset();
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (m_Iter.MoveNext())
			{
				return true;
			}
			if (!m_ListIter.MoveNext())
			{
				return false;
			}
			m_Iter = m_ListIter.Current.GetEnumerator();
			return m_Iter.MoveNext();
		}

		public void Reset()
		{
			m_ListIter = m_Arrays.GetEnumerator();
			if (m_ListIter.MoveNext())
			{
				m_Iter = m_ListIter.Current.GetEnumerator();
			}
		}
	}

	private const int m_SubArraySize = 131072;

	private List<List<T>> m_Arrays;

	private List<T> m_LastArray;

	private long m_Count;

	public long Count => m_Count;

	public LargeArray()
	{
		Clear();
	}

	public void Add(T value)
	{
		if (m_LastArray.Count == 131072)
		{
			m_LastArray = new List<T>();
			m_Arrays.Add(m_LastArray);
		}
		m_LastArray.Add(value);
		m_Count++;
	}

	public void Add(List<T> values)
	{
		if (m_LastArray.Count + values.Count >= 131072)
		{
			m_LastArray = new List<T>();
			m_Arrays.Add(m_LastArray);
		}
		m_LastArray.AddRange(values);
		m_Count += values.Count;
	}

	public void MoveTo(LargeArray<T> large_array)
	{
		large_array.m_Arrays = m_Arrays;
		large_array.m_Count = m_Count;
		Clear();
	}

	public void Clear()
	{
		m_Arrays = new List<List<T>>();
		m_LastArray = new List<T>();
		m_Arrays.Add(m_LastArray);
		m_Count = 0L;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Iterator(m_Arrays);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Iterator(m_Arrays);
	}
}
