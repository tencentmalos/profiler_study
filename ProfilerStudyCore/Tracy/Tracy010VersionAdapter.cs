namespace ProfilerStudy.Tracy;

public sealed class Tracy010VersionAdapter : ITracyVersionAdapter
{
	public const string SupportedVersion = "0.10.0";

	public string Version => SupportedVersion;
}
