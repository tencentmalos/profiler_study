using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProfilerStudy.Tracy;

public static class TracyVersionRegistry
{
	public const string LockedVersion = Tracy010VersionAdapter.SupportedVersion;
	public const string SourceReferencePath = "tools/profiler_tracy_bridge";

	private static readonly IReadOnlyList<ITracyVersionAdapter> Adapters = new ITracyVersionAdapter[]
	{
		new Tracy010VersionAdapter()
	};

	public static IReadOnlyList<string> SupportedVersions => Adapters.Select(adapter => adapter.Version).ToArray();

	public static ITracyVersionAdapter ResolveFileAdapter(string version)
	{
		foreach (ITracyVersionAdapter adapter in Adapters)
		{
			if (adapter.CanReadFile(version))
			{
				return adapter;
			}
		}

		throw new TracyFileFormatException(
			"TracyUnsupportedFileVersion",
			"Unsupported Tracy file version " + version + ". Supported version is " + LockedVersion + ".",
			version,
			SupportedVersions,
			LockedVersion);
	}

	public static ITracyVersionAdapter ResolveLiveAdapter(uint protocolVersion)
	{
		foreach (ITracyVersionAdapter adapter in Adapters)
		{
			if (adapter.CanConnect(protocolVersion))
			{
				return adapter;
			}
		}

		throw new TracyFileFormatException(
			"TracyProtocolMismatch",
			"Unsupported Tracy live protocol version " + protocolVersion + ". Supported protocol version is " + Tracy010VersionAdapter.SupportedProtocolVersion + ".",
			"protocol:" + protocolVersion,
			SupportedVersions,
			LockedVersion);
	}

	public static TracyStatus GetStatus()
	{
		string referencePath = ResolveSourceReferencePath();
		bool available = Directory.Exists(referencePath);
		string reason = available
			? "Tracy source reference submodule is available."
			: "Tracy source reference submodule is not initialized; built-in C# Tracy support remains available.";
		return new TracyStatus(
			LockedVersion,
			SupportedVersions,
			referencePath,
			available,
			reason);
	}

	private static string ResolveSourceReferencePath()
	{
		string current = Directory.GetCurrentDirectory();
		for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(current); i++)
		{
			string candidate = Path.Combine(current, SourceReferencePath);
			if (Directory.Exists(candidate) || Directory.Exists(Path.Combine(current, ".git")))
			{
				return candidate;
			}
			current = Directory.GetParent(current)?.FullName;
		}
		return SourceReferencePath;
	}
}
