namespace ProfilerStudy.Tracy;

public sealed class TracySourceLocationSummary
{
	public TracySourceLocationSummary(short id, string name, string function, string file, uint line, string dynamicName)
	{
		Id = id;
		Name = name ?? string.Empty;
		Function = function ?? string.Empty;
		File = file ?? string.Empty;
		Line = line;
		DynamicName = dynamicName ?? string.Empty;
	}

	public short Id { get; }

	public string Name { get; }

	public string Function { get; }

	public string File { get; }

	public uint Line { get; }

	public string DynamicName { get; }
}
