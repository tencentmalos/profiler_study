using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SCLCoreCLR;

namespace FramePro;

public class ValuePerSecArray
{
	private const int m_AverageDuration = 1000;

	private const int m_IntervalInMs = 100;

	private long m_FirstIntervalTime;

	private double m_CurrentIntervalValue;

	private double m_CurrentIntervalCount;

	private double m_AccCurrentIntervalValue;

	private BlockArray<IntervalValue> m_Values = new BlockArray<IntervalValue>();

	private RollingTotal m_RollingValueTotal = new RollingTotal(10);

	private RollingTotal m_RollingCountTotal = new RollingTotal(10);

	private RollingTotal m_AccRollingValueTotal = new RollingTotal(10);

	private double m_MinValuePerSec = double.MaxValue;

	private double m_MaxValuePerSec;

	private double m_MinCountPerSec = double.MaxValue;

	private double m_MaxCountPerSec;

	private double m_AccMinValuePerSec = double.MaxValue;

	private double m_AccMaxValuePerSec;

	private int m_LastAddIndex;

	public static int IntervalInMs => 100;

	public double MinValuePerSec => m_MinValuePerSec;

	public double MaxValuePerSec => m_MaxValuePerSec;

	public double MinCountPerSec => m_MinCountPerSec;

	public double MaxCountPerSec => m_MaxCountPerSec;

	public double AccMinValuePerSec => m_AccMinValuePerSec;

	public double AccMaxValuePerSec => m_AccMaxValuePerSec;

	static ValuePerSecArray()
	{
	}

	public void AddFrameValue(double value, int count, long frame_start_time, long frame_end_time, long timer_frequency)
	{
		if (m_FirstIntervalTime == 0L)
		{
			BigInteger bigInteger = (BigInteger)frame_start_time * (BigInteger)1000 / timer_frequency / 100 * 100;
			m_FirstIntervalTime = (long)(bigInteger * timer_frequency / 1000);
		}
		int num = (m_LastAddIndex = GetIntervalIndex(frame_start_time, timer_frequency));
		while (m_Values.Count < num)
		{
			AddNewValue();
		}
		long num2 = frame_end_time - frame_start_time;
		_ = m_FirstIntervalTime;
		_ = num * 100 * timer_frequency / 1000;
		long val = m_FirstIntervalTime + (num + 1) * 100 * timer_frequency / 1000;
		long num3 = frame_start_time;
		while (num3 < frame_end_time)
		{
			long num4 = Math.Min(val, frame_end_time);
			double num5 = (double)(num4 - num3) / (double)num2;
			m_CurrentIntervalValue += value * num5;
			m_CurrentIntervalCount += (double)count * num5;
			m_AccCurrentIntervalValue += value * num5;
			num3 = num4;
			if (num3 < frame_end_time)
			{
				AddNewValue();
				num = (m_LastAddIndex = num + 1);
				_ = m_FirstIntervalTime;
				_ = num * 100 * timer_frequency / 1000;
				val = m_FirstIntervalTime + (num + 1) * 100 * timer_frequency / 1000;
			}
		}
	}

	private void AddNewValue()
	{
		m_RollingValueTotal.Add(m_CurrentIntervalValue);
		m_RollingCountTotal.Add(m_CurrentIntervalCount);
		m_AccRollingValueTotal.Add(m_AccCurrentIntervalValue);
		m_CurrentIntervalValue = 0.0;
		m_CurrentIntervalCount = 0.0;
		double total = m_RollingValueTotal.Total;
		double total2 = m_RollingCountTotal.Total;
		double total3 = m_AccRollingValueTotal.Total;
		IntervalValue value = default(IntervalValue);
		value.m_Value = total;
		value.m_Count = total2;
		value.m_AccValue = total3;
		m_Values.Add(value);
		if (total < m_MinValuePerSec)
		{
			m_MinValuePerSec = total;
		}
		if (total > m_MaxValuePerSec)
		{
			m_MaxValuePerSec = total;
		}
		if (total2 < m_MinCountPerSec)
		{
			m_MinCountPerSec = total2;
		}
		if (total2 > m_MaxCountPerSec)
		{
			m_MaxCountPerSec = total2;
		}
		if (total3 < m_AccMinValuePerSec)
		{
			m_AccMinValuePerSec = total3;
		}
		if (total3 > m_AccMaxValuePerSec)
		{
			m_AccMaxValuePerSec = total3;
		}
	}

	private int GetIntervalIndex(long time, long timer_frequency)
	{
		return (int)((time - m_FirstIntervalTime) * 1000 / timer_frequency / 100);
	}

	public void GetValues(long start_time, long end_time, long timer_frequency, bool acc, List<PerSecValue> frame_values, out long first_interval_time)
	{
		frame_values.Clear();
		int num = GetIntervalIndex(start_time, timer_frequency) - 1;
		int intervalIndex = GetIntervalIndex(end_time, timer_frequency);
		first_interval_time = m_FirstIntervalTime + num * 100 * timer_frequency / 1000;
		int i;
		for (i = num; i < 0; i++)
		{
			frame_values.Add(default(PerSecValue));
		}
		int num2 = Math.Min(intervalIndex, m_Values.Count - 1);
		if (acc)
		{
			for (; i <= num2; i++)
			{
				PerSecValue item = default(PerSecValue);
				item.m_Value = m_Values[i].m_AccValue;
				item.m_Count = m_Values[i].m_Count;
				frame_values.Add(item);
			}
		}
		else
		{
			for (; i <= num2; i++)
			{
				PerSecValue item2 = default(PerSecValue);
				item2.m_Value = m_Values[i].m_Value;
				item2.m_Count = m_Values[i].m_Count;
				frame_values.Add(item2);
			}
		}
		for (; i < intervalIndex; i++)
		{
			frame_values.Add(default(PerSecValue));
		}
	}

	public void Read(BinaryReader binary_reader, int file_version)
	{
		m_FirstIntervalTime = binary_reader.ReadInt64();
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			IntervalValue value = default(IntervalValue);
			value.m_Value = binary_reader.ReadDouble();
			value.m_Count = binary_reader.ReadDouble();
			if (file_version >= 41)
			{
				value.m_AccValue = binary_reader.ReadDouble();
			}
			m_Values.Add(value);
		}
		m_RollingValueTotal.Read(binary_reader);
		m_RollingCountTotal.Read(binary_reader);
		if (file_version >= 26)
		{
			m_MinValuePerSec = ((file_version >= 32) ? binary_reader.ReadDouble() : 0.0);
			m_MaxValuePerSec = binary_reader.ReadDouble();
			m_MinCountPerSec = ((file_version >= 32) ? binary_reader.ReadDouble() : 0.0);
			m_MaxCountPerSec = binary_reader.ReadDouble();
		}
		if (file_version >= 41)
		{
			m_AccRollingValueTotal.Read(binary_reader);
			m_AccMinValuePerSec = binary_reader.ReadDouble();
			m_AccMaxValuePerSec = binary_reader.ReadDouble();
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_FirstIntervalTime);
		binary_writer.Write(m_Values.Count);
		foreach (IntervalValue value in m_Values)
		{
			binary_writer.Write(value.m_Value);
			binary_writer.Write(value.m_Count);
			binary_writer.Write(value.m_AccValue);
		}
		m_RollingValueTotal.Write(binary_writer);
		m_RollingCountTotal.Write(binary_writer);
		binary_writer.Write(m_MinValuePerSec);
		binary_writer.Write(m_MaxValuePerSec);
		binary_writer.Write(m_MinCountPerSec);
		binary_writer.Write(m_MaxCountPerSec);
		m_AccRollingValueTotal.Write(binary_writer);
		binary_writer.Write(m_AccMinValuePerSec);
		binary_writer.Write(m_AccMaxValuePerSec);
	}
}
