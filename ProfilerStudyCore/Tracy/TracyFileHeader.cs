namespace ProfilerStudy.Tracy;

public sealed class TracyFileHeader
{
	public TracyFileHeader(string version, string compression)
	{
		Version = version;
		Compression = compression;
	}

	public string Version { get; }

	public string Compression { get; }
}
