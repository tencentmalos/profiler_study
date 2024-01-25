using System;
using System.Collections;
using System.Collections.Generic;

namespace SCLCoreCLR;

public class LinkedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : LinkedListNode
{
	private class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private LinkedList<T> m_List;

		private T m_Current;

		object IEnumerator.Current => m_Current;

		public T Current => m_Current;

		public Enumerator(LinkedList<T> list)
		{
			m_List = list;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			m_Current = ((m_Current != null) ? ((T)m_Current.m_Next) : m_List.Head);
			return m_Current != null;
		}

		public void Reset()
		{
			m_Current = null;
		}
	}

	private T m_Head;

	private T m_Tail;

	private int m_Count;

	private bool m_ReadOnly;

	public int Count => m_Count;

	public bool IsReadOnly
	{
		get
		{
			return m_ReadOnly;
		}
		set
		{
			m_ReadOnly = value;
		}
	}

	public T Head => m_Head;

	public T Tail => m_Tail;

	public void AddFirst(T node)
	{
		if (m_Head != null)
		{
			m_Head.m_Prev = node;
		}
		if (m_Tail == null)
		{
			m_Tail = node;
		}
		node.m_Next = m_Head;
		m_Head = node;
		m_Count++;
	}

	public void AddLast(T node)
	{
		if (m_Head == null)
		{
			m_Head = node;
		}
		if (m_Tail != null)
		{
			m_Tail.m_Next = node;
		}
		node.m_Prev = m_Tail;
		m_Tail = node;
		m_Count++;
	}

	public void Add(T item)
	{
		AddLast(item);
	}

	public void Clear()
	{
		m_Head = (m_Tail = null);
		m_Count = 0;
	}

	public bool Contains(T item)
	{
		for (LinkedListNode linkedListNode = m_Head; linkedListNode != null; linkedListNode = linkedListNode.m_Next)
		{
			if (linkedListNode == item)
			{
				return true;
			}
		}
		return false;
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		T val = m_Head;
		int num = 0;
		while (val != null)
		{
			array[num + arrayIndex] = val;
			val = (T)val.m_Next;
		}
	}

	public bool Remove(T item)
	{
		if (item.m_Prev == null)
		{
			m_Head = (T)item.m_Next;
		}
		else
		{
			item.m_Prev.m_Next = item.m_Next;
		}
		if (item.m_Next == null)
		{
			m_Tail = (T)item.m_Prev;
		}
		else
		{
			item.m_Next.m_Prev = item.m_Prev;
		}
		m_Count--;
		item.m_Prev = (item.m_Next = null);
		return true;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new Exception("non generic GetEnumerator called!");
	}

	public void InsertAfter(T node, T prev_node)
	{
		if (prev_node == null)
		{
			AddFirst(node);
		}
		else if (prev_node.m_Next == null)
		{
			AddLast(node);
		}
		else
		{
			node.m_Prev = prev_node;
			node.m_Next = prev_node.m_Next;
			prev_node.m_Next.m_Prev = node;
			prev_node.m_Next = node;
		}
		m_Count++;
	}

	public void InsertBefore(T node, T next_node)
	{
		if (next_node == null)
		{
			AddLast(node);
		}
		else if (next_node.m_Prev == null)
		{
			AddFirst(node);
		}
		else
		{
			node.m_Next = next_node;
			node.m_Prev = next_node.m_Prev;
			next_node.m_Prev.m_Next = node;
			next_node.m_Prev = node;
		}
		m_Count++;
	}
}
