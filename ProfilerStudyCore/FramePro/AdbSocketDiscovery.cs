using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace FramePro;

public static class AdbSocketDiscovery
{
	public const string DebugFrameProEndpoint = "localfilesystem:/data/user_de/0/org.azahar_emu.azahar.debug/files/framepro";
	public const string ReleaseFrameProEndpoint = "localfilesystem:/data/user_de/0/org.azahar_emu.azahar/files/framepro";
	private const int FrameProDefaultTcpPort = 8428;

	public static bool IsAdbSocketEndpoint(string endpoint)
	{
		endpoint = (endpoint ?? string.Empty).Trim();
		return endpoint.StartsWith("localfilesystem:", StringComparison.OrdinalIgnoreCase);
	}

	public static int AllocateLocalTcpPort()
	{
		while (true)
		{
			TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
			if (port != FrameProDefaultTcpPort)
			{
				return port;
			}
		}
	}

	public static string ResolveAdbExecutable()
	{
		string androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
		string androidSdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
		foreach (string root in new[] { androidHome, androidSdkRoot })
		{
			if (!string.IsNullOrEmpty(root))
			{
				string exe = Path.Combine(root, "platform-tools", "adb.exe");
				if (File.Exists(exe))
				{
					return exe;
				}
			}
		}
		return "adb";
	}

	public static AdbResult RunAdb(string adb, string[] arguments)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = adb;
			startInfo.Arguments = string.Join(" ", arguments.Select(QuoteProcessArgument));
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			startInfo.CreateNoWindow = true;

			using (Process process = Process.Start(startInfo))
			{
				if (!process.WaitForExit(5000))
				{
					process.Kill();
					return new AdbResult(false, -1, "adb timed out");
				}

				string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
				return new AdbResult(process.ExitCode == 0, process.ExitCode, output);
			}
		}
		catch (Exception e)
		{
			return new AdbResult(false, -1, e.Message);
		}
	}

	public static string NormalizeCommandOutput(string output)
	{
		if (string.IsNullOrEmpty(output))
		{
			return "<empty>";
		}
		return output.Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static string QuoteProcessArgument(string argument)
	{
		if (argument.IndexOf(' ') == -1 && argument.IndexOf('"') == -1 && argument.IndexOf('|') == -1)
		{
			return argument;
		}
		return "\"" + argument.Replace("\"", "\\\"") + "\"";
	}
}

public struct AdbResult
{
	public readonly bool Success;
	public readonly int ExitCode;
	public readonly string Output;

	public AdbResult(bool success, int exitCode, string output)
	{
		Success = success;
		ExitCode = exitCode;
		Output = output ?? string.Empty;
	}
}
