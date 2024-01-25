using System.Runtime.InteropServices;

namespace SCLCoreCLR;

public class Time
{
	private static long m_Freq;

	[DllImport("Kernel32.dll")]
	private static extern int QueryPerformanceCounter(ref long count);

	[DllImport("Kernel32.dll")]
	private static extern int QueryPerformanceFrequency(ref long frequency);

	public static long Now_HiRes()
	{
		long count = 0L;
		QueryPerformanceCounter(ref count);
		return count;
	}

	public static long GetTicksPerSec()
	{
		if (m_Freq == 0L)
		{
			QueryPerformanceFrequency(ref m_Freq);
		}
		return m_Freq;
	}

	public static int Now()
	{
		return (int)(Now_HiRes() * 1000 / GetTicksPerSec());
	}
}
