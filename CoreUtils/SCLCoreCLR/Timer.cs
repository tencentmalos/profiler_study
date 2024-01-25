using System.Collections.Generic;
using System.Diagnostics;

namespace SCLCoreCLR;

public class Timer
{
	private static Dictionary<string, Stopwatch> m_Timers = new Dictionary<string, Stopwatch>();

	public static void StartTimer(string name)
	{
		Stopwatch stopwatch = new Stopwatch();
		m_Timers[name] = stopwatch;
		stopwatch.Start();
	}

	public static void StopTimer(string name)
	{
		Stopwatch stopwatch = m_Timers[name];
		stopwatch.Stop();
		float num = (float)((double)stopwatch.ElapsedTicks * 1000.0 / (double)Stopwatch.Frequency);
		m_Timers[name] = null;
		Log.WriteLine("Timer " + name + " " + num);
	}
}
