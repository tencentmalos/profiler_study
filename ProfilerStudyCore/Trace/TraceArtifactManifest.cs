using System;
using System.Collections.Generic;

namespace ProfilerStudy.Trace;

public sealed class TraceArtifactManifest
{
	public string ArtifactId { get; set; }

	public string SourceFormat { get; set; }

	public string SourceKind { get; set; }

	public string SourcePath { get; set; }

	public string NormalizedPath { get; set; }

	public string DiagnosticsPath { get; set; }

	public long SourceByteCount { get; set; }

	public long NormalizedByteCount { get; set; }

	public long DiagnosticsByteCount { get; set; }

	public string CreatedUtc { get; set; }

	public string Implementation { get; set; }

	public string ReaderVersion { get; set; }

	public string TracyVersion { get; set; }

	public Dictionary<string, object> Capture { get; set; }

	public DateTime GetCreatedUtc()
	{
		return DateTime.TryParse(CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime value)
			? value
			: DateTime.MinValue;
	}
}
