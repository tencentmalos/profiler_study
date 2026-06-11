using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProfilerStudy.Trace;

public static class TraceArtifactStore
{
	private const string RootEnvironmentVariable = "PROFILER_STUDY_TRACE_ARTIFACT_ROOT";
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

	public static string RootPath
	{
		get
		{
			string configured = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
			if (!string.IsNullOrWhiteSpace(configured))
			{
				return Path.GetFullPath(configured);
			}
			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrWhiteSpace(home))
			{
				home = Directory.GetCurrentDirectory();
			}
			return Path.Combine(home, ".profilerstudy", "traces", "captures");
		}
	}

	public static TraceArtifactManifest Register(
		TraceDocument document,
		string sourceKind,
		string tracyVersion,
		ArrayList diagnostics,
		Dictionary<string, object> capture)
	{
		string root = RootPath;
		Directory.CreateDirectory(root);
		string artifactId = CreateArtifactId(document.SourceFormat, sourceKind);
		string artifactDirectory = Path.Combine(root, artifactId);
		Directory.CreateDirectory(artifactDirectory);
		Directory.CreateDirectory(Path.Combine(artifactDirectory, "normalized"));

		string diagnosticsPath = Path.Combine(artifactDirectory, "import-diagnostics.json");
		File.WriteAllText(diagnosticsPath, JsonSerializer.Serialize(diagnostics ?? new ArrayList(), JsonOptions));

		TraceArtifactManifest manifest = new TraceArtifactManifest
		{
			ArtifactId = artifactId,
			SourceFormat = document.SourceFormat,
			SourceKind = sourceKind,
			SourcePath = document.SourcePath,
			NormalizedPath = "normalized",
			DiagnosticsPath = "import-diagnostics.json",
			CreatedUtc = DateTime.UtcNow.ToString("o"),
			Implementation = "ProfilerStudyCore.TraceArtifactStore",
			ReaderVersion = "0.1.0",
			TracyVersion = tracyVersion,
			Capture = capture
		};
		File.WriteAllText(Path.Combine(artifactDirectory, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
		return manifest;
	}

	public static ArrayList List(string format, int limit)
	{
		string root = RootPath;
		ArrayList artifacts = new ArrayList();
		if (!Directory.Exists(root))
		{
			return artifacts;
		}

		string normalizedFormat = string.IsNullOrWhiteSpace(format) ? string.Empty : format.Trim().ToLowerInvariant();
		IEnumerable<TraceArtifactManifest> manifests = Directory
			.EnumerateFiles(root, "manifest.json", SearchOption.AllDirectories)
			.Select(ReadManifest)
			.Where(manifest => manifest != null)
			.Where(manifest => normalizedFormat.Length == 0 || normalizedFormat == "all" || string.Equals(manifest.SourceFormat, normalizedFormat, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(manifest => manifest.GetCreatedUtc())
			.Take(Math.Max(1, limit));

		foreach (TraceArtifactManifest manifest in manifests)
		{
			artifacts.Add(new Dictionary<string, object>
			{
				["artifactId"] = manifest.ArtifactId,
				["sourceFormat"] = manifest.SourceFormat,
				["sourceKind"] = manifest.SourceKind,
				["sourcePath"] = manifest.SourcePath,
				["normalizedPath"] = manifest.NormalizedPath,
				["diagnosticsPath"] = manifest.DiagnosticsPath,
				["createdUtc"] = manifest.CreatedUtc,
				["tracyVersion"] = manifest.TracyVersion
			});
		}
		return artifacts;
	}

	private static TraceArtifactManifest ReadManifest(string path)
	{
		try
		{
			return JsonSerializer.Deserialize<TraceArtifactManifest>(File.ReadAllText(path));
		}
		catch
		{
			return null;
		}
	}

	private static string CreateArtifactId(string sourceFormat, string sourceKind)
	{
		string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
		return timestamp + "-" + Sanitize(sourceFormat) + "-" + Sanitize(sourceKind) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	}

	private static string Sanitize(string value)
	{
		string text = string.IsNullOrWhiteSpace(value) ? "trace" : value.Trim().ToLowerInvariant();
		return Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');
	}
}
