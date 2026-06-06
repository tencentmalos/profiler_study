using System;
using FramePro;

namespace ProfilerStudy.McpServer;

internal enum ProfilerCaptureTargetKind
{
	AndroidForward,
	Tcp
}

internal sealed class ProfilerCaptureTarget
{
	public ProfilerCaptureTargetKind Kind { get; private set; }
	public string Url { get; private set; }
	public string Endpoint { get; private set; }
	public string ConnectHost { get; private set; }
	public int ConnectPort { get; private set; }

	private ProfilerCaptureTarget()
	{
	}

	public static ProfilerCaptureTarget Parse(string url)
	{
		url = (url ?? string.Empty).Trim();
		if (url.Length == 0)
		{
			throw new ArgumentException("Capture url is required.");
		}

		int schemeSeparator = url.IndexOf("://", StringComparison.Ordinal);
		if (schemeSeparator <= 0)
		{
			throw new ArgumentException("Capture url must use android:// or pc://.");
		}

		string scheme = url.Substring(0, schemeSeparator).ToLowerInvariant();
		string rest = url.Substring(schemeSeparator + 3);
		switch (scheme)
		{
			case "android":
				return ParseAndroid(url, rest);
			case "pc":
				return ParsePc(url);
			default:
				throw new ArgumentException("Unsupported capture url scheme: " + scheme);
		}
	}

	public static ProfilerCaptureTarget FromAndroidEndpoint(string source, string endpoint)
	{
		endpoint = (endpoint ?? string.Empty).Trim();
		if (!AdbSocketDiscovery.IsAdbSocketEndpoint(endpoint))
		{
			throw new ArgumentException("Android endpoint must be localfilesystem:<path>, localabstract:<name>, or tcp:<port>.");
		}

		return new ProfilerCaptureTarget
		{
			Kind = ProfilerCaptureTargetKind.AndroidForward,
			Url = source,
			Endpoint = endpoint,
			ConnectHost = "127.0.0.1",
			ConnectPort = 0
		};
	}

	private static ProfilerCaptureTarget ParseAndroid(string url, string forwardName)
	{
		forwardName = Uri.UnescapeDataString((forwardName ?? string.Empty).Trim());
		if (forwardName.Length == 0)
		{
			throw new ArgumentException("Android capture url must include a unix socket name, path, or port.");
		}

		string endpoint = ResolveAndroidEndpoint(forwardName);
		if (!AdbSocketDiscovery.IsAdbSocketEndpoint(endpoint))
		{
			throw new ArgumentException("Android capture url must resolve to localfilesystem:<path>, localabstract:<name>, or tcp:<port>.");
		}

		return new ProfilerCaptureTarget
		{
			Kind = ProfilerCaptureTargetKind.AndroidForward,
			Url = url,
			Endpoint = endpoint,
			ConnectHost = "127.0.0.1",
			ConnectPort = 0
		};
	}

	private static string ResolveAndroidEndpoint(string forwardName)
	{
		if (forwardName.StartsWith("localfilesystem:", StringComparison.OrdinalIgnoreCase)
			|| forwardName.StartsWith("localabstract:", StringComparison.OrdinalIgnoreCase)
			|| forwardName.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
		{
			return forwardName;
		}
		if (int.TryParse(forwardName, out int port) && port > 0 && port <= 65535)
		{
			return "tcp:" + port;
		}
		return "localfilesystem:" + forwardName;
	}

	private static ProfilerCaptureTarget ParsePc(string url)
	{
		Uri uri;
		if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || !string.Equals(uri.Scheme, "pc", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException("Invalid pc capture url: " + url);
		}
		if (string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0)
		{
			throw new ArgumentException("PC capture url must be pc://host:port.");
		}
		if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
		{
			throw new ArgumentException("PC capture url must not include a path.");
		}

		return new ProfilerCaptureTarget
		{
			Kind = ProfilerCaptureTargetKind.Tcp,
			Url = url,
			Endpoint = uri.Host + ":" + uri.Port,
			ConnectHost = uri.Host,
			ConnectPort = uri.Port
		};
	}
}
