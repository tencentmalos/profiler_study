namespace FramePro;

internal class TimeUnits
{
	private static Interval[] m_Intervals = new Interval[20]
	{
		new Interval(1L, 1.0, "ns"),
		new Interval(1L, 5.0, "ns"),
		new Interval(1L, 10.0, "ns"),
		new Interval(1L, 100.0, "ns"),
		new Interval(1000L, 1.0, "us"),
		new Interval(1000L, 5.0, "us"),
		new Interval(1000L, 10.0, "us"),
		new Interval(1000L, 100.0, "us"),
		new Interval(1000000L, 1.0, "ms"),
		new Interval(1000000L, 5.0, "ms"),
		new Interval(1000000L, 10.0, "ms"),
		new Interval(1000000L, 100.0, "ms"),
		new Interval(1000000000L, 1.0, "sec"),
		new Interval(1000000000L, 10.0, "sec"),
		new Interval(1000000000L, 30.0, "sec"),
		new Interval(60000000000L, 1.0, "min"),
		new Interval(60000000000L, 5.0, "min"),
		new Interval(60000000000L, 10.0, "min"),
		new Interval(60000000000L, 30.0, "min"),
		new Interval(60000000000L, 60.0, "min")
	};

	public static Interval GetBestInterval(double total_time, int total_pixels, int min_interval_length)
	{
		int num = m_Intervals.Length;
		double num2;
		do
		{
			num--;
			num2 = m_Intervals[num].StepSize * (double)total_pixels / total_time;
		}
		while (num2 > (double)min_interval_length && num > 0);
		if (num < m_Intervals.Length - 1)
		{
			num++;
		}
		return m_Intervals[num];
	}
}
