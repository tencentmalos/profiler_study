namespace ProfilerStudy.Avalonia;

internal sealed class LogMessageRow
{
	public LogMessageRow(int index, long time, string message)
	{
		Index = index;
		Time = time;
		Message = message ?? string.Empty;
	}

	public int Index { get; }

	public long Time { get; }

	public string Message { get; }

	public string TimeText => Time.ToString();
}
