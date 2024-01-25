using System;
using System.Collections.Generic;
using System.Drawing;

namespace FramePro;

internal class BrushSet : IDisposable
{
	private Dictionary<Color, Brush> m_Brushes = new Dictionary<Color, Brush>();

	public void Dispose()
	{
		foreach (Color key in m_Brushes.Keys)
		{
			m_Brushes[key].Dispose();
		}
		m_Brushes.Clear();
	}

	public Brush GetBrush(Color colour)
	{
		Brush value = null;
		if (!m_Brushes.TryGetValue(colour, out value))
		{
			value = new SolidBrush(colour);
			m_Brushes[colour] = value;
		}
		return value;
	}
}
