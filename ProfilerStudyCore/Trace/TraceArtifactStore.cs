using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProfilerStudy.Tracy;

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
		string normalizedDirectory = Path.Combine(artifactDirectory, "normalized");
		Directory.CreateDirectory(normalizedDirectory);

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
			Implementation = ResolveImplementation(document.SourceFormat),
			ReaderVersion = "0.1.0",
			TracyVersion = tracyVersion,
			Capture = capture
		};
		File.WriteAllText(Path.Combine(artifactDirectory, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
		WriteLiveCaptureDebugMaterial(artifactDirectory, manifest, diagnostics);
		if (document.QuerySession is TracyTraceQuerySession tracyQuerySession)
		{
			TracyNormalizedArtifact.Write(normalizedDirectory, document, tracyQuerySession, diagnostics);
		}
		return manifest;
	}

	private static void WriteLiveCaptureDebugMaterial(string artifactDirectory, TraceArtifactManifest manifest, ArrayList diagnostics)
	{
		if (!string.Equals(manifest.SourceFormat, "tracy", StringComparison.OrdinalIgnoreCase) ||
			!string.Equals(manifest.SourceKind, "tracy-live-normalized-only", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		Dictionary<string, object> record = new Dictionary<string, object>
		{
			["artifactId"] = manifest.ArtifactId,
			["sourceFormat"] = manifest.SourceFormat,
			["sourceKind"] = manifest.SourceKind,
			["sourcePath"] = manifest.SourcePath,
			["tracyVersion"] = manifest.TracyVersion,
			["createdUtc"] = manifest.CreatedUtc,
			["capture"] = manifest.Capture ?? new Dictionary<string, object>(),
			["diagnostics"] = diagnostics ?? new ArrayList()
		};
		File.WriteAllText(Path.Combine(artifactDirectory, "capture.ndjson"), JsonSerializer.Serialize(record) + Environment.NewLine);
	}

	public static TraceDocument Load(string artifactId)
	{
		string artifactDirectory = GetArtifactDirectory(artifactId);
		string manifestPath = Path.Combine(artifactDirectory, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			throw new FileNotFoundException("Trace artifact manifest does not exist.", manifestPath);
		}

		TraceArtifactManifest manifest = ReadManifest(manifestPath);
		if (manifest == null)
		{
			throw new InvalidOperationException("Trace artifact manifest could not be read: " + artifactId + ".");
		}
		if (!string.Equals(manifest.SourceFormat, "tracy", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Unsupported trace artifact format: " + manifest.SourceFormat + ".");
		}
		return TracyNormalizedArtifact.Load(artifactDirectory, manifest);
	}

	public static Dictionary<string, object> GetDiagnostics(string artifactId)
	{
		string artifactDirectory = GetArtifactDirectory(artifactId);
		string manifestPath = Path.Combine(artifactDirectory, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			throw new FileNotFoundException("Trace artifact manifest does not exist.", manifestPath);
		}

		TraceArtifactManifest manifest = ReadManifest(manifestPath);
		if (manifest == null)
		{
			throw new InvalidOperationException("Trace artifact manifest could not be read: " + artifactId + ".");
		}

		string diagnosticsPath = ResolveArtifactFilePath(
			artifactDirectory,
			string.IsNullOrWhiteSpace(manifest.DiagnosticsPath) ? "import-diagnostics.json" : manifest.DiagnosticsPath,
			"Trace artifact diagnostics path escapes the artifact directory.");
		return new Dictionary<string, object>
		{
			["artifactId"] = manifest.ArtifactId,
			["source"] = manifest.SourcePath,
			["sourceFormat"] = manifest.SourceFormat,
			["sourceKind"] = manifest.SourceKind,
			["diagnosticsPath"] = manifest.DiagnosticsPath,
			["diagnostics"] = ReadDiagnostics(diagnosticsPath)
		};
	}

	private static string ResolveArtifactFilePath(string artifactDirectory, string relativePath, string escapeMessage)
	{
		string fullArtifactDirectory = Path.GetFullPath(artifactDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string fullPath = Path.GetFullPath(Path.Combine(fullArtifactDirectory, relativePath ?? string.Empty));
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (!fullPath.Equals(fullArtifactDirectory, comparison) && !fullPath.StartsWith(fullArtifactDirectory + Path.DirectorySeparatorChar, comparison))
		{
			throw new InvalidOperationException(escapeMessage);
		}
		return fullPath;
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
			.EnumerateDirectories(root)
			.Select(directory => Path.Combine(directory, "manifest.json"))
			.Where(File.Exists)
			.Select(ReadManifest)
			.Where(manifest => manifest != null)
			.Where(manifest => !string.IsNullOrWhiteSpace(manifest.ArtifactId))
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

	private static ArrayList ReadDiagnostics(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return new ArrayList();
		}
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return ToArrayList(document.RootElement);
	}

	private static Dictionary<string, object> ToDictionary(JsonElement element)
	{
		Dictionary<string, object> values = new Dictionary<string, object>();
		foreach (JsonProperty property in element.EnumerateObject())
		{
			values[property.Name] = ToObject(property.Value);
		}
		return values;
	}

	private static ArrayList ToArrayList(JsonElement element)
	{
		ArrayList values = new ArrayList();
		if (element.ValueKind != JsonValueKind.Array)
		{
			return values;
		}
		foreach (JsonElement item in element.EnumerateArray())
		{
			values.Add(ToObject(item));
		}
		return values;
	}

	private static object ToObject(JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				return ToDictionary(element);
			case JsonValueKind.Array:
				return ToArrayList(element);
			case JsonValueKind.String:
				return element.GetString();
			case JsonValueKind.Number:
				if (element.TryGetInt64(out long longValue))
				{
					return longValue;
				}
				return element.GetDouble();
			case JsonValueKind.True:
				return true;
			case JsonValueKind.False:
				return false;
			default:
				return null;
		}
	}

	private static string GetArtifactDirectory(string artifactId)
	{
		if (string.IsNullOrWhiteSpace(artifactId))
		{
			throw new ArgumentException("artifact_id is required.", nameof(artifactId));
		}
		string root = RootPath;
		string artifactDirectory = Path.GetFullPath(Path.Combine(root, artifactId));
		string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (!artifactDirectory.Equals(normalizedRoot, comparison) && !artifactDirectory.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison))
		{
			throw new InvalidOperationException("Trace artifact path escapes the artifact root.");
		}
		return artifactDirectory;
	}

	private static string CreateArtifactId(string sourceFormat, string sourceKind)
	{
		string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
		return timestamp + "-" + Sanitize(sourceFormat) + "-" + Sanitize(sourceKind) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
	}

	private static string ResolveImplementation(string sourceFormat)
	{
		return string.Equals(sourceFormat, "tracy", StringComparison.OrdinalIgnoreCase)
			? "ProfilerStudyCore.Tracy"
			: "ProfilerStudyCore.TraceArtifactStore";
	}

	private static string Sanitize(string value)
	{
		string text = string.IsNullOrWhiteSpace(value) ? "trace" : value.Trim().ToLowerInvariant();
		return Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');
	}
}
