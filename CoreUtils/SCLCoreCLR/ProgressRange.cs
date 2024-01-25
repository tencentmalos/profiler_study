namespace SCLCoreCLR;

internal class ProgressRange
{
	public int Progress;

	public int start;

	public int end;

	public ProgressRange(int range_start, int range_end)
	{
		Progress = range_start;
		start = range_start;
		end = range_end;
	}
}
