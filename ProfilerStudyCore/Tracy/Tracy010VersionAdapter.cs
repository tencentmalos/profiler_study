namespace ProfilerStudy.Tracy;

public sealed class Tracy010VersionAdapter : ITracyVersionAdapter
{
	public const string SupportedVersion = "0.10.0";
	public const uint SupportedProtocolVersion = Tracy010LiveCaptureClient.ProtocolVersion;

	public string Version => SupportedVersion;

	public bool CanReadFile(string version)
	{
		return string.Equals(version, SupportedVersion, System.StringComparison.Ordinal);
	}

	public bool CanConnect(uint protocolVersion)
	{
		return protocolVersion == SupportedProtocolVersion;
	}
}
