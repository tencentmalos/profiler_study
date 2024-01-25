namespace FramePro;

internal class MiscUnits
{
	private static Interval[] m_Intervals = new Interval[54]
	{
		new Interval(1L, 1E-09, ""),
		new Interval(1L, 2E-09, ""),
		new Interval(1L, 5E-09, ""),
		new Interval(1L, 1E-08, ""),
		new Interval(1L, 2E-08, ""),
		new Interval(1L, 5E-08, ""),
		new Interval(1L, 1E-07, ""),
		new Interval(1L, 2E-07, ""),
		new Interval(1L, 5E-07, ""),
		new Interval(1L, 1E-06, ""),
		new Interval(1L, 2E-06, ""),
		new Interval(1L, 5E-06, ""),
		new Interval(1L, 1E-05, ""),
		new Interval(1L, 2E-05, ""),
		new Interval(1L, 5E-05, ""),
		new Interval(1L, 0.0001, ""),
		new Interval(1L, 0.0002, ""),
		new Interval(1L, 0.0005, ""),
		new Interval(1L, 0.001, ""),
		new Interval(1L, 0.002, ""),
		new Interval(1L, 0.005, ""),
		new Interval(1L, 0.01, ""),
		new Interval(1L, 0.02, ""),
		new Interval(1L, 0.05, ""),
		new Interval(1L, 0.1, ""),
		new Interval(1L, 0.5, ""),
		new Interval(1L, 1.0, ""),
		new Interval(1L, 5.0, ""),
		new Interval(1L, 10.0, ""),
		new Interval(1L, 20.0, ""),
		new Interval(1L, 50.0, ""),
		new Interval(1L, 100.0, ""),
		new Interval(1L, 200.0, ""),
		new Interval(1L, 500.0, ""),
		new Interval(1L, 1000.0, ""),
		new Interval(1L, 5000.0, ""),
		new Interval(1L, 10000.0, ""),
		new Interval(1L, 20000.0, ""),
		new Interval(1L, 50000.0, ""),
		new Interval(1L, 100000.0, ""),
		new Interval(1L, 200000.0, ""),
		new Interval(1L, 500000.0, ""),
		new Interval(1L, 1000000.0, ""),
		new Interval(1L, 2000000.0, ""),
		new Interval(1L, 5000000.0, ""),
		new Interval(1L, 10000000.0, ""),
		new Interval(1L, 20000000.0, ""),
		new Interval(1L, 50000000.0, ""),
		new Interval(1L, 100000000.0, ""),
		new Interval(1L, 200000000.0, ""),
		new Interval(1L, 500000000.0, ""),
		new Interval(1L, 1000000000.0, ""),
		new Interval(1L, 2000000000.0, ""),
		new Interval(1L, 5000000000.0, "")
	};

	public static Interval GetBestInterval(double total_value, int total_pixels, int min_interval_length, string unit_name)
	{
		int num = m_Intervals.Length;
		double num2;
		do
		{
			num--;
			num2 = m_Intervals[num].StepSize * (double)total_pixels / total_value;
		}
		while (num2 > (double)min_interval_length && num > 0);
		if (num < m_Intervals.Length - 1)
		{
			num++;
		}
		Interval interval = m_Intervals[num];
		return new Interval(interval.Unit, interval.Step, unit_name);
	}
}
