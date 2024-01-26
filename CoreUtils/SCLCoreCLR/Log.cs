using System;
using System.IO;

namespace SCLCoreCLR;

public class Log
{
	private static StreamWriter m_Stream;

	private static bool m_OpenedDefaultLogFile;

	private static LogCallback m_LogCallback;

	public static void OpenFile(string path)
	{
		OpenFile(path, 8388608);
	}

	public static void OpenFile(string path, int max_size)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			if (directoryName != "" && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			if (m_Stream != null)
			{
				m_Stream.Close();
				m_Stream = null;
			}
			string path2 = path;
			for (int i = 1; i < 100; path = Path.Combine(Path.GetDirectoryName(path2), Path.GetFileNameWithoutExtension(path2)) + i + Path.GetExtension(path2), i++)
			{
				try
				{
					m_Stream = new StreamWriter(path, append: false);
				}
				catch (Exception)
				{
					continue;
				}
				break;
			}
			FileInfo fileInfo = new FileInfo(path);
			if (fileInfo.Length > max_size)
			{
				m_Stream.Close();
				m_Stream = null;
				char[] buffer = new char[max_size];
				StreamReader streamReader = new StreamReader(path);
				streamReader.BaseStream.Seek(fileInfo.Length - max_size, SeekOrigin.Begin);
				streamReader.Read(buffer, 0, max_size);
				streamReader.Close();
				m_Stream = new StreamWriter(path);
				m_Stream.Write("Cut...");
				m_Stream.Write(buffer);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void SetLogCallback(LogCallback callback)
	{
		m_LogCallback = callback;
	}

	public static void Write(string message)
	{
		Write(message, LogVerbosity.Normal);
	}

	public static void Write(string message, LogVerbosity verbosity)
	{
		try
		{
			if (verbosity != LogVerbosity.Verbose && m_LogCallback != null)
			{
				m_LogCallback(message);
			}
			if (m_Stream == null && !m_OpenedDefaultLogFile)
			{
				OpenFile("log.txt");
				m_OpenedDefaultLogFile = true;
			}
			if (m_Stream != null)
			{
				message = DateTime.Now.ToString() + ": " + message;
				message = message.Replace("\n", "\r\n");
				m_Stream.Write(message);
				m_Stream.Flush();
			}
		}
		catch (Exception)
		{
		}
	}

	public static void WriteLine(string message, LogVerbosity verbosity)
	{
		Write(message + "\n", verbosity);
	}

	public static void WriteLine(string message)
	{
		WriteLine(message, LogVerbosity.Normal);
	}
}
