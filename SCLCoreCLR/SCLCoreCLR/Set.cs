using System;
using System.Collections;
using System.Collections.Generic;

namespace SCLCoreCLR;

public class Set<T> : IEnumerable<T>, IEnumerable
{
	private Dictionary<T, bool> m_Dictionary = new Dictionary<T, bool>();

	public int Count => m_Dictionary.Count;

	public Set()
	{
	}

	public Set(Set<T> other)
	{
		m_Dictionary = new Dictionary<T, bool>(other.m_Dictionary);
	}

	public Set(IEnumerable<T> other)
	{
		foreach (T item in other)
		{
			Add(item);
		}
	}

	public void Clear()
	{
		m_Dictionary.Clear();
	}

	public void Add(T value)
	{
		if (!Contains(value))
		{
			m_Dictionary[value] = true;
		}
	}

	public void Remove(T value)
	{
		if (Contains(value))
		{
			m_Dictionary.Remove(value);
		}
	}

	public bool Contains(T value)
	{
		return m_Dictionary.ContainsKey(value);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return m_Dictionary.Keys.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new Exception("non generic GetEnumerator called!");
	}

	public T[] ToArray()
	{
		T[] array = new T[m_Dictionary.Count];
		int num = 0;
		foreach (T key in m_Dictionary.Keys)
		{
			array[num++] = key;
		}
		return array;
	}

	public List<T> ToList()
	{
		List<T> list = new List<T>(m_Dictionary.Count);
		foreach (T key in m_Dictionary.Keys)
		{
			list.Add(key);
		}
		return list;
	}

	public T Remove()
	{
		using (Dictionary<T, bool>.KeyCollection.Enumerator enumerator = m_Dictionary.Keys.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				m_Dictionary.Remove(current);
				return current;
			}
		}
		return default(T);
	}

	public static Set<T> operator -(Set<T> s1, Set<T> s2)
	{
		Set<T> set = new Set<T>();
		foreach (T item in s1)
		{
			if (!s2.Contains(item))
			{
				set.Add(item);
			}
		}
		return set;
	}

	public static Set<T> operator +(Set<T> s1, List<T> s2)
	{
		Set<T> set = new Set<T>(s1);
		foreach (T item in s2)
		{
			set.Add(item);
		}
		return set;
	}

	public Set<T> GetIntersection(Set<T> other)
	{
		Set<T> set = new Set<T>();
		foreach (T item in other)
		{
			if (m_Dictionary.ContainsKey(item))
			{
				set.Add(item);
			}
		}
		return set;
	}
}
