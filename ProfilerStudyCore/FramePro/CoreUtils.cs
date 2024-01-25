using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using SCLCoreCLR;

namespace FramePro;

public class CoreUtils
{
	public enum FrameTimeCategory
	{
		InBudget,
		Warning,
		Alert
	}

	public const long TimeScale = 100L;

	public static int GetPercentComplete(Stream file_stream, long file_stream_length)
	{
		return (int)(file_stream.Position * 100 / file_stream_length);
	}

	internal static int GetPercentComplete(ReceiveStream stream, long file_stream_length)
	{
		return (int)(stream.BytesReceived * 100 / file_stream_length);
	}

	private static void ProcessOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
	{
		if (!string.IsNullOrEmpty(outLine.Data))
		{
			Log.WriteLine(outLine.Data);
		}
	}

	public static void Read(Dictionary<int, long> map, BinaryReader binary_reader)
	{
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			int key = binary_reader.ReadInt32();
			long value = binary_reader.ReadInt64();
			map[key] = value;
		}
	}

	public static void Write(Dictionary<int, long> map, BinaryWriter binary_writer)
	{
		binary_writer.Write(map.Count);
		foreach (int key in map.Keys)
		{
			binary_writer.Write(key);
			binary_writer.Write(map[key]);
		}
	}

	public static void Read(Dictionary<int, string> map, BinaryReader binary_reader)
	{
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			int key = binary_reader.ReadInt32();
			string value = binary_reader.ReadString();
			map[key] = value;
		}
	}

	public static void Write(Dictionary<int, string> map, BinaryWriter binary_writer)
	{
		binary_writer.Write(map.Count);
		foreach (int key in map.Keys)
		{
			binary_writer.Write(key);
			binary_writer.Write(map[key]);
		}
	}

	public static string GetProcessName(int process_id)
	{
		try
		{
			Process processById = Process.GetProcessById(process_id);
			if (processById != null)
			{
				return processById.ProcessName;
			}
		}
		catch (Exception)
		{
		}
		return "unknown";
	}

	public static T Clone<T>(T original) where T : IFrameProSerialisable, new()
	{
		T val = new T();
		Copy(val, original);
		return val;
	}

	public static void Copy<T>(T dest, T source) where T : IFrameProSerialisable
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binary_writer = new BinaryWriter(memoryStream);
		source.Write(binary_writer);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		BinaryReader binary_reader = new BinaryReader(memoryStream);
		dest.Read(binary_reader, Session.SaveFileVersion);
	}

	public static Color GenerateColour(string name, double saturation)
	{
		uint num = MurmurHash2.Hash(name);
		double num2 = 360.0 * (double)(num % 4079) / 4079.0;
		num2 = 10.0 + num2 * 300.0 / 360.0;
		return Misc.ColorFromHSV(num2, saturation, Colours.ThreadColourValue);
	}

	public static bool RunbatchFile(string filename, string args)
	{
		Process process = new Process();
		process.StartInfo.FileName = filename;
		process.StartInfo.Arguments = args;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.RedirectStandardOutput = true;
		process.OutputDataReceived += ProcessOutputHandler;
		process.StartInfo.RedirectStandardError = true;
		process.ErrorDataReceived += ProcessOutputHandler;
		process.Start();
		process.BeginOutputReadLine();
		process.WaitForExit();
		return process.ExitCode == 0;
	}

	public static FrameTimeCategory GetFrameTimeCategory(long duration, long timer_frequency, double target_ms)
	{
		double num = (double)(duration * 1000) / (double)timer_frequency * 100.0 / target_ms - 100.0;
		if (num > 100.0)
		{
			return FrameTimeCategory.Alert;
		}
		if (num > 0.0)
		{
			return FrameTimeCategory.Warning;
		}
		return FrameTimeCategory.InBudget;
	}

	public static bool ListsEqual(List<string> list1, List<string> list2)
	{
		if (list1.Count != list2.Count)
		{
			return false;
		}
		for (int i = 0; i < list1.Count; i++)
		{
			if (list1[i] != list2[i])
			{
				return false;
			}
		}
		return true;
	}

	public static Color ToColor(uint colour)
	{
		return Color.FromArgb(-16777216 | (int)colour);
	}

	public static bool EnumsMatch(Type enum_type_1, Type enum_type_2)
	{
		string[] names = Enum.GetNames(enum_type_1);
		string[] names2 = Enum.GetNames(enum_type_2);
		if (names.Length != names2.Length)
		{
			return false;
		}
		for (int i = 0; i < names.Length; i++)
		{
			if (names[i] != names[2])
			{
				return false;
			}
		}
		return true;
	}

	public static void Remap<T>(Dictionary<long, T> map, long old_string_id, long new_string_id)
	{
		if (map.ContainsKey(old_string_id))
		{
			T value = map[old_string_id];
			map.Remove(old_string_id);
			map[new_string_id] = value;
		}
	}

	public static void PrintHeirachy(TimeSpan time_span, GetTimerNameDelegate get_timer_name)
	{
		PrintHeirachyRecursive(time_span, 0, get_timer_name);
	}

	public static void PrintHeirachyRecursive(TimeSpan time_span, int depth, GetTimerNameDelegate get_timer_name)
	{
		for (int i = 0; i < depth; i++)
		{
		}
		for (TimeSpan timeSpan = time_span.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			PrintHeirachyRecursive(timeSpan, depth + 1, get_timer_name);
		}
	}

	private static bool IsWhiteSpace(char c)
	{
		if (c != ' ' && c != '\t' && c != '\r')
		{
			return c == '\n';
		}
		return true;
	}

	public static string StripOffFunctionTypes(string name)
	{
		int i = name.Length - 1;
		while (i > 0 && name[i] == ')')
		{
			i--;
		}
		if (i >= 0)
		{
			int num = 1;
			while (i > 0 && num > 0)
			{
				switch (name[i])
				{
				case '(':
					num--;
					break;
				case ')':
					num++;
					break;
				}
				i--;
			}
			if (i >= 0)
			{
				while (i > 0 && IsWhiteSpace(name[i]))
				{
					i--;
				}
			}
			if (i >= 0)
			{
				int num2 = i;
				int num3 = 0;
				while (i > 0 && !IsWhiteSpace(name[i]) && num3 <= 2)
				{
					if (name[i] == ':')
					{
						num3++;
					}
					i--;
				}
				if (i >= 0)
				{
					for (; IsWhiteSpace(name[i]) || name[i] == ':'; i++)
					{
					}
					if (i < num2)
					{
						return name.Substring(i, num2 + 1 - i);
					}
				}
			}
		}
		return name;
	}
}
