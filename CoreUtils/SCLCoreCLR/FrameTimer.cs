using System;
using System.Diagnostics;

namespace SCLCoreCLR;

public class FrameTimer
{
	private string m_Name;

	private Stopwatch m_Stopwatch = new Stopwatch();

	private bool m_Started;

	private int m_Count;

	private static Set<FrameTimer> m_Timers = new Set<FrameTimer>();

	private static int m_LastPrintTime;

	public FrameTimer(string name)
	{
		m_Timers.Add(this);
		m_Name = name;
	}

	public void Start()
	{
		m_Started = true;
		m_Stopwatch.Start();
	}

	public void Stop()
	{
		m_Stopwatch.Stop();
		m_Started = false;
	}

	public void Clear()
	{
		m_Stopwatch.Reset();
		m_Count = 0;
	}

	public void Print()
	{
		float num = ((m_Count != 0) ? ((float)((double)m_Stopwatch.ElapsedTicks * 1000.0 / (double)Stopwatch.Frequency / (double)m_Count)) : 0f);
		Log.WriteLine("Timer " + m_Name + ": " + num);
	}

	public static void Update()
	{
		foreach (FrameTimer timer in m_Timers)
		{
			timer.m_Count++;
		}
		int tickCount = Environment.TickCount;
		if (tickCount - m_LastPrintTime <= 5000)
		{
			return;
		}
		foreach (FrameTimer timer2 in m_Timers)
		{
			timer2.Print();
			timer2.Clear();
		}
		m_LastPrintTime = tickCount;
	}
}
