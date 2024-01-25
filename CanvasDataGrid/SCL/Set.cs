using System;
using System.Collections;
using System.Collections.Generic;

namespace SCL;

internal class Set<T> : IEnumerable<T>, IEnumerable, ICollection<T>
{
	private Dictionary<T, bool> m_Dictionary = new Dictionary<T, bool>();

	public int Count => m_Dictionary.Count;

	public bool IsReadOnly => false;

	public Set()
	{
	}

	public Set(Set<T> other)
	{
		m_Dictionary = new Dictionary<T, bool>(other.m_Dictionary);
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

	public void AddRange(ICollection<T> range)
	{
		foreach (T item in range)
		{
			Add(item);
		}
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

	public bool Remove(T value)
	{
		if (Contains(value))
		{
			m_Dictionary.Remove(value);
			return true;
		}
		return false;
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

	public void CopyTo(T[] array, int array_index)
	{
		foreach (T key in m_Dictionary.Keys)
		{
			array[array_index++] = key;
		}
	}

	public void Remove(Set<T> other)
	{
		foreach (T item in other)
		{
			Remove(item);
		}
	}

	public static Set<T> Union(Set<T> s1, Set<T> s2)
	{
		Set<T> set = new Set<T>();
		foreach (T item in s1)
		{
			set.Add(item);
		}
		foreach (T item2 in s2)
		{
			set.Add(item2);
		}
		return set;
	}

	public static Set<T> Intersection(Set<T> s1, Set<T> s2)
	{
		Set<T> set = new Set<T>();
		foreach (T item in s2)
		{
			if (s2.Contains(item))
			{
				set.Add(item);
			}
		}
		return set;
	}

	public static Set<T> Difference(Set<T> s1, Set<T> s2)
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

	public static Set<T> InverseUnion(Set<T> s1, Set<T> s2)
	{
		Set<T> set = new Set<T>();
		foreach (T item in s1)
		{
			if (!s2.Contains(item))
			{
				set.Add(item);
			}
		}
		foreach (T item2 in s2)
		{
			if (!s1.Contains(item2))
			{
				set.Add(item2);
			}
		}
		return set;
	}
}
