namespace FramePro;

internal class MemoryUnits
{
	private static Interval[] m_Intervals = new Interval[38]
	{
		new Interval(1L, 0.0, "bytes"),
		new Interval(1L, 1.0, "bytes"),
		new Interval(1L, 2.0, "bytes"),
		new Interval(1L, 4.0, "bytes"),
		new Interval(1L, 8.0, "bytes"),
		new Interval(1L, 16.0, "bytes"),
		new Interval(1L, 32.0, "bytes"),
		new Interval(1L, 64.0, "bytes"),
		new Interval(1L, 128.0, "bytes"),
		new Interval(1L, 256.0, "bytes"),
		new Interval(1L, 512.0, "bytes"),
		new Interval(1024L, 1.0, "KB"),
		new Interval(1024L, 2.0, "KB"),
		new Interval(1024L, 4.0, "KB"),
		new Interval(1024L, 8.0, "KB"),
		new Interval(1024L, 16.0, "KB"),
		new Interval(1024L, 32.0, "KB"),
		new Interval(1024L, 64.0, "KB"),
		new Interval(1024L, 128.0, "KB"),
		new Interval(1024L, 256.0, "KB"),
		new Interval(1024L, 512.0, "KB"),
		new Interval(1048576L, 1.0, "MB"),
		new Interval(1048576L, 2.0, "MB"),
		new Interval(1048576L, 4.0, "MB"),
		new Interval(1048576L, 8.0, "MB"),
		new Interval(1048576L, 16.0, "MB"),
		new Interval(1048576L, 32.0, "MB"),
		new Interval(1048576L, 64.0, "MB"),
		new Interval(1048576L, 128.0, "MB"),
		new Interval(1048576L, 256.0, "MB"),
		new Interval(1048576L, 512.0, "MB"),
		new Interval(1073741824L, 1.0, "GB"),
		new Interval(1073741824L, 2.0, "GB"),
		new Interval(1073741824L, 4.0, "GB"),
		new Interval(1073741824L, 8.0, "GB"),
		new Interval(1073741824L, 16.0, "GB"),
		new Interval(1073741824L, 32.0, "GB"),
		new Interval(1073741824L, 64.0, "GB")
	};

	public static Interval GetBestInterval(double total_bytes, int total_pixels, int min_interval_length)
	{
		int num = m_Intervals.Length;
		double num2;
		do
		{
			num--;
			num2 = m_Intervals[num].StepSize * (double)total_pixels / total_bytes;
		}
		while (num2 > (double)min_interval_length && num > 0);
		if (num < m_Intervals.Length - 1)
		{
			num++;
		}
		return m_Intervals[num];
	}
}
