using System;
using System.Collections;

namespace ProfilerStudy.Trace;

public sealed class TraceDocument
{
	public TraceDocument(
		string id,
		string sourcePath,
		string sourceFormat,
		string displayName,
		DateTime createdUtc,
		ITraceQuerySession querySession,
		ArrayList importDiagnostics,
		string artifactId = null)
	{
		Id = id;
		SourcePath = sourcePath;
		SourceFormat = sourceFormat;
		DisplayName = displayName;
		CreatedUtc = createdUtc;
		QuerySession = querySession;
		ImportDiagnostics = importDiagnostics;
		ArtifactId = artifactId ?? string.Empty;
	}

	public string Id { get; }

	public string ArtifactId { get; private set; }

	public string SourcePath { get; }

	public string SourceFormat { get; }

	public string DisplayName { get; }

	public DateTime CreatedUtc { get; }

	public ITraceQuerySession QuerySession { get; }

	public ArrayList ImportDiagnostics { get; }

	internal void AttachArtifactId(string artifactId)
	{
		ArtifactId = artifactId ?? string.Empty;
	}
}
