using System;
using System.Drawing;

namespace FramePro;

internal class RectArray : IDisposable
{
	private int m_Length;

	public Rectangle[] m_Array;

	public int Length => m_Length;

	public void Dispose()
	{
		m_Array = null;
	}

	public void Resize(int length)
	{
		Resize(length, can_reduce: true);
	}

	public void Resize(int length, bool can_reduce)
	{
		if (length == m_Length && m_Array != null)
		{
			return;
		}
		if (m_Array == null || m_Array.Length < length || (can_reduce && m_Array.Length > 2 * length + 5))
		{
			Rectangle[] array = new Rectangle[length];
			if (m_Array != null)
			{
				int num = Math.Min(length, m_Array.Length);
				for (int i = 0; i < num; i++)
				{
					array[i] = m_Array[i];
				}
			}
			m_Array = array;
		}
		m_Length = length;
		for (int j = length; j < m_Array.Length; j++)
		{
			m_Array[j] = default(Rectangle);
		}
	}
}
