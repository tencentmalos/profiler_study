using System;
using ProfilerStudy;

namespace ProfilerStudy.McpServer;

internal interface IProfilerTcpForwarder
{
	ProfilerForwardedTarget Forward(ProfilerCaptureTarget target);
}

internal sealed class ProfilerForwardedTarget : IDisposable
{
	private readonly Action m_Dispose;

	public ProfilerForwardedTarget(string host, int port, string endpoint, Action dispose)
	{
		Host = host ?? string.Empty;
		Port = port;
		Endpoint = endpoint ?? string.Empty;
		m_Dispose = dispose;
	}

	public string Host { get; }

	public int Port { get; }

	public string Endpoint { get; }

	public void Dispose()
	{
		m_Dispose?.Invoke();
	}
}

internal sealed class AdbProfilerTcpForwarder : IProfilerTcpForwarder
{
	public ProfilerForwardedTarget Forward(ProfilerCaptureTarget target)
	{
		if (target.Kind == ProfilerCaptureTargetKind.Tcp)
		{
			return new ProfilerForwardedTarget(target.ConnectHost, target.ConnectPort, target.Endpoint, null);
		}
		if (target.Kind != ProfilerCaptureTargetKind.AndroidForward)
		{
			throw new InvalidOperationException("Unsupported capture target kind: " + target.Kind + ".");
		}

		int localPort = AdbSocketDiscovery.AllocateLocalTcpPort();
		string adb = AdbSocketDiscovery.ResolveAdbExecutable();
		AdbResult forwardResult = AdbSocketDiscovery.RunAdb(adb, new[] { "forward", "tcp:" + localPort, target.Endpoint });
		if (!forwardResult.Success)
		{
			throw new InvalidOperationException("ADB forward failed for " + target.Endpoint + ": " + AdbSocketDiscovery.NormalizeCommandOutput(forwardResult.Output));
		}

		bool removed = false;
		return new ProfilerForwardedTarget("127.0.0.1", localPort, target.Endpoint, () =>
		{
			if (removed)
			{
				return;
			}
			removed = true;
			AdbSocketDiscovery.RunAdb(adb, new[] { "forward", "--remove", "tcp:" + localPort });
		});
	}
}
