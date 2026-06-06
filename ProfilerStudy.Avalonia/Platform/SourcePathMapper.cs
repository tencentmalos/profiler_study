using System;
using System.IO;
using System.Linq;

namespace ProfilerStudy.Avalonia;

internal static class SourcePathMapper
{
	public static string Resolve(string sourceFile, string capturedSourceRoot, string localSourceRoot)
	{
		if (string.IsNullOrWhiteSpace(sourceFile))
		{
			return sourceFile ?? string.Empty;
		}

		if (File.Exists(sourceFile))
		{
			return sourceFile;
		}

		if (string.IsNullOrWhiteSpace(capturedSourceRoot) || string.IsNullOrWhiteSpace(localSourceRoot))
		{
			return sourceFile;
		}

		string normalizedSource = NormalizeSeparators(sourceFile.Trim());
		string normalizedCapturedRoot = NormalizeRoot(capturedSourceRoot.Trim());
		if (!IsUnderRoot(normalizedSource, normalizedCapturedRoot))
		{
			return sourceFile;
		}

		string relativePath = normalizedSource.Length == normalizedCapturedRoot.Length
			? string.Empty
			: normalizedSource.Substring(normalizedCapturedRoot.Length + 1);
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return sourceFile;
		}

		string[] parts = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		string mappedPath = Path.Combine(new[] { localSourceRoot.Trim() }.Concat(parts).ToArray());
		return File.Exists(mappedPath) ? mappedPath : sourceFile;
	}

	private static bool IsUnderRoot(string sourceFile, string root)
	{
		return sourceFile.Length > root.Length &&
			sourceFile.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
			sourceFile[root.Length] == '/';
	}

	private static string NormalizeRoot(string path)
	{
		return NormalizeSeparators(path).TrimEnd('/');
	}

	private static string NormalizeSeparators(string path)
	{
		return path.Replace('\\', '/');
	}
}
