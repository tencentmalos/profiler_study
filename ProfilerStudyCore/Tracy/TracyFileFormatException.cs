using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfilerStudy.Tracy;

public sealed class TracyFileFormatException : InvalidOperationException
{
	public TracyFileFormatException(string errorCode, string message)
		: this(errorCode, message, string.Empty, null, string.Empty)
	{
	}

	public TracyFileFormatException(
		string errorCode,
		string message,
		string detectedVersion,
		IEnumerable<string> supportedVersions,
		string lockedVersion)
		: base(message)
	{
		ErrorCode = errorCode;
		DetectedVersion = detectedVersion ?? string.Empty;
		SupportedVersions = supportedVersions == null ? Array.Empty<string>() : supportedVersions.ToArray();
		LockedVersion = lockedVersion ?? string.Empty;
	}

	public string ErrorCode { get; }

	public string DetectedVersion { get; }

	public IReadOnlyList<string> SupportedVersions { get; }

	public string LockedVersion { get; }
}
