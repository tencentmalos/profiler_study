using System.Collections.Generic;

namespace ProfilerStudy.Tracy;

public sealed class TracyStatus
{
	public TracyStatus(
		string lockedVersion,
		IReadOnlyList<string> supportedVersions,
		string sourceReferencePath,
		bool sourceReferenceAvailable,
		string reason)
	{
		LockedVersion = lockedVersion;
		SupportedVersions = supportedVersions;
		SourceReferencePath = sourceReferencePath;
		SourceReferenceAvailable = sourceReferenceAvailable;
		Reason = reason;
	}

	public string LockedVersion { get; }

	public IReadOnlyList<string> SupportedVersions { get; }

	public string SourceReferencePath { get; }

	public bool SourceReferenceAvailable { get; }

	public string Reason { get; }
}
