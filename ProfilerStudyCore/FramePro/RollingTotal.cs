using System.Collections.Generic;
using System.IO;

namespace FramePro;

internal class RollingTotal
{
	private List<double> m_Values = new List<double>();

	private int m_CurrentIndex;

	private double m_Total;

	public double Total => m_Total;

	public RollingTotal(int count)
	{
		for (int i = 0; i < count; i++)
		{
			m_Values.Add(0.0);
		}
	}

	public void Add(double value)
	{
		m_Total -= m_Values[m_CurrentIndex];
		m_Total += value;
		m_Values[m_CurrentIndex] = value;
		m_CurrentIndex = (m_CurrentIndex + 1) % m_Values.Count;
	}

	public void Read(BinaryReader binary_reader)
	{
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			m_Values.Add(binary_reader.ReadDouble());
		}
		m_CurrentIndex = binary_reader.ReadInt32();
		m_Total = binary_reader.ReadDouble();
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Values.Count);
		foreach (double value in m_Values)
		{
			binary_writer.Write(value);
		}
		binary_writer.Write(m_CurrentIndex);
		binary_writer.Write(m_Total);
	}
}
