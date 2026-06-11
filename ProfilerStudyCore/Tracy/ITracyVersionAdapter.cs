namespace ProfilerStudy.Tracy;

public interface ITracyVersionAdapter
{
	string Version { get; }

	bool CanReadFile(string version);

	bool CanConnect(uint protocolVersion);
}
