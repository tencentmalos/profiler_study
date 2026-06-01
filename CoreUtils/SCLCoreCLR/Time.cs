using System.Diagnostics;
#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace SCLCoreCLR;

public class Time
{
	private static long m_Freq;

#if WINDOWS
	[DllImport("Kernel32.dll")]
	private static extern int QueryPerformanceCounter(ref long count);

	[DllImport("Kernel32.dll")]
	private static extern int QueryPerformanceFrequency(ref long frequency);
#endif

	public static long Now_HiRes()
	{
#if WINDOWS
		long count = 0L;
		QueryPerformanceCounter(ref count);
		return count;
#else
		return Stopwatch.GetTimestamp();
#endif
	}

	public static long GetTicksPerSec()
	{
		if (m_Freq == 0L)
		{
#if WINDOWS
			QueryPerformanceFrequency(ref m_Freq);
#else
			m_Freq = Stopwatch.Frequency;
#endif
		}
		return m_Freq;
	}

	public static int Now()
	{
		return (int)(Now_HiRes() * 1000 / GetTicksPerSec());
	}
}
