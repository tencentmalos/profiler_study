using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using SCLCoreCLR;
using static System.Net.Mime.MediaTypeNames;

namespace FramePro;

internal class Utils
{
	public struct IdleRange
	{
		public long m_StartTime;

		public long m_EndTime;
	}

	public const int MaxDrawValue = 1000000;

	private static ulong m_OriginalProcessAffinity;

	private static StringBuilder m_ProcessResult;

	private static bool m_LogResult;

	public static bool IsShiftHeld => (Control.ModifierKeys & Keys.Shift) != 0;

	public static void Swap<T>(ref T a, ref T b)
	{
		T val = a;
		a = b;
		b = val;
	}

	public static Color MultiplyColour(Color colour, float multipler)
	{
		int red = Math.Min((int)((float)(int)colour.R * multipler), 255);
		int green = Math.Min((int)((float)(int)colour.G * multipler), 255);
		int blue = Math.Min((int)((float)(int)colour.B * multipler), 255);
		return Color.FromArgb(red, green, blue);
	}

	public static int Lerp(int a, int b, float p)
	{
		return (int)((float)a + (float)(b - a) * p);
	}

	public static double Lerp(double a, double b, double p)
	{
		return a + (b - a) * p;
	}

	public static Color Lerp(Color colour1, Color colour2, float p)
	{
		return Color.FromArgb(Lerp(colour1.A, colour2.A, p), Lerp(colour1.R, colour2.R, p), Lerp(colour1.G, colour2.G, p), Lerp(colour1.B, colour2.B, p));
	}

	public static string GetTimeString(long time, long timer_frequency)
	{
		double num = (double)(time * 1000) / (double)timer_frequency;
		if (num >= 0.99999)
		{
			return num.ToString("0.0#") + " ms";
		}
		return (num * 1000.0).ToString("0.0") + " μs";
	}

	public static string DoubleToString(double value)
	{
		if (value > 100.0)
		{
			return value.ToString("0");
		}
		if (value > 1.0)
		{
			return value.ToString("0.#");
		}
		if (value > 0.1)
		{
			return value.ToString("0.##");
		}
		if (value > 0.01)
		{
			return value.ToString("0.###");
		}
		if (value > 0.001)
		{
			return value.ToString("0.####");
		}
		if (value > 0.0001)
		{
			return value.ToString("0.#####");
		}
		if (value > 1E-05)
		{
			return value.ToString("0.######");
		}
		return value.ToString("0.##########");
	}

	public static string GetTimeString(long time, long timer_frequency, TimeUnitsNEW time_units)
	{
		return DoubleToString(time_units switch
		{
			TimeUnitsNEW.Seconds => (double)time / (double)timer_frequency, 
			TimeUnitsNEW.Milliseconds => (double)(time * 1000) / (double)timer_frequency, 
			TimeUnitsNEW.Microseconds => (double)(time * 1000 * 1000) / (double)timer_frequency, 
			TimeUnitsNEW.Nanoseconds => (double)(time * 1000 * 1000 * 1000) / (double)timer_frequency, 
			_ => 0.0, 
		});
	}

	public static string GetTimeUnitsString(TimeUnitsNEW time_units)
	{
		return time_units switch
		{
			TimeUnitsNEW.Seconds => "sec", 
			TimeUnitsNEW.Milliseconds => "ms", 
			TimeUnitsNEW.Microseconds => "μs", 
			TimeUnitsNEW.Nanoseconds => "ns", 
			_ => "error", 
		};
	}

	public static void JumpToSourceCode(TimeSpan time_span, Session session, Settings settings)
	{
		JumpToSourceCode(time_span.TimeSpanInfoId, session, settings);
	}


    private static void OpenByViewerName(string viewerName, string fileName, int lineNumber, bool openShell)
    {
        if (viewerName == "VSCode")
        {
            OpenByVsCode(fileName, lineNumber, openShell);
        }
        else if (viewerName == "Clion")
        {
            OpenByClion(fileName, lineNumber, openShell);
        }
        else if (viewerName == "Visual Studio")
        {
            OpenByVisualStudio(fileName, lineNumber, openShell);
        }
        else
        {
            MessageBox.Show($"ERROR: Do not support source viewer:{viewerName} find here!");
			Log.WriteLine($"ERROR: Do not support source viewer:{viewerName} find here, fallback to VSCode!");
            OpenByVsCode(fileName, lineNumber, openShell);
        }
    }

    private static void OpenByVisualStudio(string fileName, int lineNumber, bool openShell)
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "VisualStudioOpenFileAndLine.exe";
            process.StartInfo.Arguments = "\"" + fileName + "\" " + lineNumber;

			Log.WriteLine($"Open source by Visual Studio: {process.StartInfo.FileName} {process.StartInfo.Arguments}");


			process.StartInfo.CreateNoWindow = !openShell;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += ProcessOutputHandler;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += ProcessOutputHandler;
            process.Start();
            process.BeginOutputReadLine();


        }
        catch (Exception ex)
        {
            MessageBox.Show("ERORR: " + ex.Message);
        }
    }

    private static void OpenByVsCode(string fileName, int lineNumber, bool openShell)
    {
        try
        {
            Process process = new Process();
			if(PlatformTool.IsRunOnWine())
			{
				string path = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
				path = System.IO.Path.Combine(path, "open_source_vscode.sh");

                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = $"\"{fileName}\" {lineNumber}";
                
                // process.StartInfo.FileName = "start";
                //
                // path = path.Replace("Z:", "");
                // path = path.Replace("\\", "/");
                // //process.StartInfo.Arguments = $"\"{path}\"  \"{fileName}\" {lineNumber}";
                // process.StartInfo.Arguments = $"/exec /bin/bash \"{path}\"  \"{fileName}\" {lineNumber}";
			}
			else
			{
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C code --goto {fileName}:{lineNumber}";
            }
            

            Log.WriteLine($"Open source by vscode: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.StartInfo.CreateNoWindow = !openShell;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += ProcessOutputHandler;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += ProcessOutputHandler;
            process.Start();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
	        Log.WriteLine($"ERROR: {ex.ToString()}");
            MessageBox.Show("ERORR: " + ex.Message);
        }
    }

    private static void OpenByClion(string fileName, int lineNumber, bool openShell)
    {
        try
        {
            Process process = new Process();
            if (PlatformTool.IsRunOnWine())
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"clion --line {lineNumber} \"{fileName}\"";
            }
            else
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C clion --line {lineNumber} \"{fileName}\"";
            }
            

            Log.WriteLine($"Open source by clion: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            //file:///<file_path>:<line_number>
            process.StartInfo.CreateNoWindow = !openShell;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += ProcessOutputHandler;
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += ProcessOutputHandler;
            process.Start();
            process.BeginOutputReadLine();
        }
        catch (Exception ex)
        {
            MessageBox.Show("ERORR: " + ex.Message);
        }
    }



    public static void JumpToSourceCode(int time_span_info_id, Session session, Settings settings)
	{
		if (MainForm.Inst == null || time_span_info_id == TimeSpanInfo.InvalidInfoId)
		{
			return;
		}
		SourceInfoStruct sourceInfo = session.GetSourceInfo(session.GetTimeSpanInfo(time_span_info_id).SourceInfo);
		if (!sourceInfo.IsValid)
		{
			return;
		}
		string text = sourceInfo.Filename;
		if (!Path.IsPathRooted(text))
		{
			foreach (string sourceRoot in settings.SourceRoots)
			{
				string fullPath = Path.GetFullPath(Path.Combine(sourceRoot, text));
				if (File.Exists(fullPath))
				{
					text = fullPath;
					break;
				}
			}
			if (!File.Exists(text))
			{
				text = BrowseForPath(text, settings);
			}
		}
		if (!File.Exists(text))
		{
			MessageBox.Show("Unable to find file " + text, "FramePro Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		try
		{
			OpenByViewerName(settings.SourceViewerTool, text, sourceInfo.Line, false);
        }
		catch (Exception ex)
		{
			MessageBox.Show("ERORR: " + ex.Message);
		}
	}

	private static string BrowseForPath(string filename, Settings settings)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		filename = filename.Replace("/", "\\");
		openFileDialog.Title = filename;
		string fileName = Path.GetFileName(filename);
		openFileDialog.Filter = "File |" + fileName + "|All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(MainForm.Inst) == DialogResult.OK && File.Exists(openFileDialog.FileName))
		{
			int num = 0;
			string text = filename;
			while (text.StartsWith("..\\"))
			{
				text = text.Substring("..\\".Length);
				num++;
			}
			string text2 = openFileDialog.FileName.Substring(0, openFileDialog.FileName.Length - text.Length);
			for (int i = 0; i < num; i++)
			{
				text2 = Path.Combine(text2, "?");
			}
			bool flag = true;
			foreach (string sourceRoot in settings.SourceRoots)
			{
				if (sourceRoot.ToLower() == text2.ToLower())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				settings.SourceRoots.Add(text2);
				settings.Write();
			}
			filename = openFileDialog.FileName;
		}
		return filename;
	}

	private static void ProcessOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
	{
		if (!string.IsNullOrEmpty(outLine.Data))
		{
			Log.WriteLine(outLine.Data);
		}
	}

	public static void AddTimerInfoBoxLines(HoverBox timer_info_box, TimeSpan time_span, int frame_index, Session session)
	{
		AddTimerInfoBoxLines(timer_info_box, time_span, frame_index, session, "Name:");
	}

	public static void AddTimerInfoBoxLines(HoverBox timer_info_box, TimeSpan time_span, int frame_index, Session session, string name_label)
	{
		TimeSpanInfo timeSpanInfo = session.GetTimeSpanInfo(time_span.TimeSpanInfoId);
		timer_info_box.Title = "Scope";
		string timerName = session.GetTimerName(timeSpanInfo.Name);
		timer_info_box.AddLine(name_label, timerName);
		string timeString = GetTimeString(time_span.Duration, session.TimerFrequency);
		timer_info_box.AddLine("Duration:", timeString);
		session.GetTimeSpanFrameTime(timeSpanInfo.Name, frame_index, out var duration, out var count, out var max_duration, out var max_time_per_frame, out var max_count_per_frame);
		string timeString2 = GetTimeString(duration, session.TimerFrequency);
		string timeString3 = GetTimeString(max_duration, session.TimerFrequency);
		string timeString4 = GetTimeString(max_time_per_frame, session.TimerFrequency);
		timer_info_box.AddLine("Total time this frame", timeString2);
		timer_info_box.AddLine("Total count this frame:", count.ToString());
		timer_info_box.AddLine("Session Max", timeString3);
		timer_info_box.AddLine("Max time/frame:", timeString4);
		timer_info_box.AddLine("Max count/frame:", max_count_per_frame.ToString());
		SourceInfoStruct sourceInfo = session.GetSourceInfo(timeSpanInfo.SourceInfo);
		if (sourceInfo.IsValid)
		{
			string text = sourceInfo.Function;
			if (text == "")
			{
				text = session.GetFullFunctionName(timeSpanInfo.Name);
			}
			if (text != "")
			{
				timer_info_box.AddLine("Function:", text);
			}
			timer_info_box.AddLine("Filename:", Path.GetFileName(sourceInfo.Filename));
			timer_info_box.AddLine("Line:", sourceInfo.Line.ToString());
		}
		if (!time_span.IsTimeSpanEx)
		{
			return;
		}
		TimeSpanEx timeSpanEx = time_span as TimeSpanEx;
		if (timeSpanEx.CustomStats == null)
		{
			return;
		}
		foreach (TimeSpanCustomStat customStat in timeSpanEx.CustomStats)
		{
			string @string = session.GetString(customStat.m_Name);
			string text2 = customStat.m_ValueType switch
			{
				CustomStatValueType.Int64 => customStat.m_ValueInt64.ToString(), 
				CustomStatValueType.Double => customStat.m_ValueDouble.ToString(), 
				_ => "", 
			};
			string customStatUnit = session.GetCustomStatUnit(customStat.m_Name);
			if (customStatUnit.Length != 0)
			{
				text2 = text2 + " " + customStatUnit;
			}
			timer_info_box.AddLine(@string, text2);
		}
	}

	public static void AddTimerInfoBoxLines(HoverBox timer_info_box, HiResTimer hires_timer, long parent_duration, Session session)
	{
		timer_info_box.Title = "HiRes Timer";
		long name = hires_timer.m_Name;
		string value = ((name == -1) ? "Untracked" : session.GetString(name));
		timer_info_box.AddLine("Name:", value);
		string timeString = GetTimeString(hires_timer.m_Duration, session.TimerFrequency);
		timer_info_box.AddLine("Total Duration:", timeString);
		timer_info_box.AddLine("Count:", hires_timer.m_Count.ToString());
		string timeString2 = GetTimeString((hires_timer.m_Count != 0L) ? (hires_timer.m_Duration / hires_timer.m_Count) : 0, session.TimerFrequency);
		timer_info_box.AddLine("Average Duration:", timeString2);
		timer_info_box.AddLine("Percent of Parent:", (int)(hires_timer.m_Duration * 100 / parent_duration) + "%");
	}

	public static double ConvertToDouble(string text, double default_value)
	{
		try
		{
			return Convert.ToDouble(text);
		}
		catch (Exception ex)
		{
			Log.WriteLine(ex.Message);
			return default_value;
		}
	}

	public static void SetTimeRange(long start_time, long end_time, int width, long timer_frequency, TimeRange time_range)
	{
		time_range.m_StartTime = start_time;
		long num = end_time - start_time;
		time_range.m_TicksPerPixel = Math.Max(1L, (width != 0) ? (num / width) : 0);
		if (time_range.m_TicksPerPixel * width < num)
		{
			time_range.m_TicksPerPixel++;
		}
		time_range.m_Scale = (double)timer_frequency / ((double)time_range.m_TicksPerPixel * 1000.0);
	}

	public static string GetTimeAsMinSecString(long time, long ticks_per_second)
	{
		int num = (int)(time / ticks_per_second);
		int num2 = num / 60;
		num -= num2 * 60;
		return num2.ToString("00") + ":" + num.ToString("00");
	}

	public static string GetMemoryString(long bytes)
	{
		if (bytes < 131072)
		{
			return ((float)bytes / 1024f).ToString("0.0") + " K";
		}
		return ((float)bytes / 1024f / 1024f).ToString("0.0") + " MB";
	}

	public static double TruncateDouble(double value)
	{
		if (value < 0.0001)
		{
			return value;
		}
		double num = ((value >= 1.0) ? 100 : 10000);
		return (double)(long)(value * num) / num;
	}

	public static double CyclesToMs(double cycles, long timer_frequency)
	{
		return cycles * 1000.0 / (double)timer_frequency;
	}

	public static ThreadFilter CreateNewThreadFilter(Session session)
	{
		_ = session.SessionDetails.m_Name;
		ThreadFilter threadFilter = new ThreadFilter();
		List<string> threadOrder = session.GetThreadOrder();
		foreach (string item3 in threadOrder)
		{
			ThreadFilterRow item = new ThreadFilterRow(item3);
			threadFilter.m_Threads.Add(item);
		}
		List<int> list = new List<int>();
		session.GetThreads(list);
		foreach (int item4 in list)
		{
			string threadName = session.GetThreadName(item4);
			if (!threadOrder.Contains(threadName))
			{
				ThreadFilterRow item2 = new ThreadFilterRow(threadName);
				threadFilter.m_Threads.Add(item2);
			}
		}
		return threadFilter;
	}

	public static int GetThreadIndex(ThreadFilter thread_filter, string thread_name)
	{
		int num = 0;
		foreach (ThreadFilterRow thread in thread_filter.m_Threads)
		{
			if (thread.m_Name == thread_name)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public static void AddThreadToFilter(string thread_name, ThreadFilter thread_filter, Session session)
	{
		ThreadFilterRow item = new ThreadFilterRow(thread_name);
		List<string> threadOrder = session.GetThreadOrder();
		int num = threadOrder.IndexOf(thread_name);
		if (num != -1)
		{
			int num2 = -1;
			for (int num3 = num - 1; num3 >= 0; num3--)
			{
				string thread_name2 = threadOrder[num3];
				int threadIndex = GetThreadIndex(thread_filter, thread_name2);
				if (threadIndex != -1)
				{
					num2 = threadIndex + 1;
					break;
				}
			}
			if (num2 == -1)
			{
				for (int i = num + 1; i < threadOrder.Count; i++)
				{
					string thread_name3 = threadOrder[i];
					int threadIndex2 = GetThreadIndex(thread_filter, thread_name3);
					if (threadIndex2 != -1)
					{
						num2 = threadIndex2;
						break;
					}
				}
			}
			if (num2 != -1)
			{
				thread_filter.m_Threads.Insert(num2, item);
			}
			else
			{
				thread_filter.m_Threads.Add(item);
			}
		}
		else
		{
			thread_filter.m_Threads.Add(item);
		}
	}

	public static string TrimVersionString(string version)
	{
		while (version.EndsWith(".0"))
		{
			version = version.Substring(0, version.Length - 2);
		}
		return version;
	}

	public static bool ListsEquals<T>(List<T> list1, List<T> list2)
	{
		if (list1 == null && list2 == null)
		{
			return true;
		}
		if (list1 == null || list2 == null)
		{
			return false;
		}
		int count = list1.Count;
		int count2 = list2.Count;
		if (count != count2)
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			if (!list1[i].Equals(list2[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static int GetHashCode(List<Graph> list)
	{
		int num = int.MaxValue;
		foreach (Graph item in list)
		{
			num ^= item.m_StatName.GetHashCode();
			num ^= item.m_NameId.GetHashCode();
			num ^= item.m_Colour.GetHashCode();
		}
		return num;
	}

	public static T RemoveFirst<T>(List<T> list)
	{
		T result = list[0];
		list.RemoveAt(0);
		return result;
	}

	public static int GetDistanceFromLineSegment(Point p, Point p1, Point p2, out Point closest_point)
	{
		float num = p.X;
		float num2 = p.Y;
		float num3 = p1.X;
		float num4 = p1.Y;
		float num5 = p2.X;
		float num6 = p2.Y;
		float num7 = num - num3;
		float num8 = num2 - num4;
		float num9 = num5 - num3;
		float num10 = num6 - num4;
		float num11 = num7 * num9 + num8 * num10;
		float num12 = num9 * num9 + num10 * num10;
		float num13 = -1f;
		if (num12 != 0f)
		{
			num13 = num11 / num12;
		}
		float num14;
		float num15;
		if (num13 < 0f)
		{
			num14 = num3;
			num15 = num4;
		}
		else if (num13 > 1f)
		{
			num14 = num5;
			num15 = num6;
		}
		else
		{
			num14 = num3 + num13 * num9;
			num15 = num4 + num13 * num10;
		}
		float num16 = num - num14;
		float num17 = num2 - num15;
		closest_point = new Point((int)num14, (int)num15);
		return (int)Math.Sqrt(num16 * num16 + num17 * num17);
	}

	public static long ToNanoSec(long time, long timer_frequency)
	{
		return (long)(new BigInteger(time) * 1000L * 1000L * 1000L / timer_frequency);
	}

	public static long FromNanoSec(long nano_sec, long timer_frequency)
	{
		return (long)(new BigInteger(nano_sec) * timer_frequency / 1000L / 1000L / 1000L);
	}

	public static System.Drawing.Image To96Dpi(System.Drawing.Image image)
	{
		if (image != null)
		{
			Bitmap obj = (Bitmap)image;
			obj.SetResolution(96f, 96f);
			image = obj;
		}
		return image;
	}

	public static string FindAndroidADBExe()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("ANDROID_HOME");
		if (environmentVariable != null)
		{
			string text = Path.Combine(environmentVariable, "platform-tools\\adb.exe");
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	public static bool ExecuteProcess(string command, out string result)
	{
		return ExecuteProcess(command, out result, log_result: true);
	}

	public static bool ExecuteProcess(string command, out string result, bool log_result)
	{
		result = "";
		try
		{
			m_ProcessResult = new StringBuilder();
			m_LogResult = log_result;
			Log.WriteLine("Executing Command: " + command);
			string text = command.Split(' ')[0];
			string arguments = command.Substring(text.Length + 1);
			Process process = new Process();
			process.StartInfo.FileName = text;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += ExecuteProcessOutputHandler;
			process.StartInfo.RedirectStandardError = true;
			process.ErrorDataReceived += ExecuteProcessOutputHandler;
			if (!process.Start())
			{
				return false;
			}
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			lock (m_ProcessResult)
			{
				result = m_ProcessResult.ToString();
				m_ProcessResult = null;
				m_LogResult = false;
			}
			int exitCode = process.ExitCode;
			Log.WriteLine("exit code: " + exitCode);
			return exitCode == 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return false;
		}
	}

	private static void ExecuteProcessOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
	{
		if (outLine.Data == null || outLine.Data.Length == 0)
		{
			return;
		}
		lock (m_ProcessResult)
		{
			m_ProcessResult.AppendLine(outLine.Data);
			if (m_LogResult)
			{
				Log.WriteLine(outLine.Data);
			}
		}
	}

	private static bool IsMatchingThread(ContextSwitch context_switch, List<int> threads, bool include_end_context_switches)
	{
		if (threads.Contains(context_switch.m_NewThreadId))
		{
			return true;
		}
		if (include_end_context_switches && threads.Contains(context_switch.m_OldThreadId))
		{
			return true;
		}
		return false;
	}

	private static int GetNextContextSwitchForThread(int start_index, List<ContextSwitch> context_switches, List<int> threads, bool include_end_context_switches)
	{
		int i = start_index;
		for (int count = context_switches.Count; i < count && !IsMatchingThread(context_switches[i], threads, include_end_context_switches); i++)
		{
		}
		return i;
	}

	private static int GetCoreWithNextContextSwitch(List<int> threads, List<List<ContextSwitch>> context_switches, List<int> indices)
	{
		int result = -1;
		long num = long.MaxValue;
		for (int i = 0; i < context_switches.Count; i++)
		{
			int num2 = indices[i];
			List<ContextSwitch> list = context_switches[i];
			int count = list.Count;
			if (num2 < count)
			{
				ContextSwitch contextSwitch = list[num2];
				if (contextSwitch.m_Timestamp < num)
				{
					num = contextSwitch.m_Timestamp;
					result = i;
				}
			}
		}
		return result;
	}

	public static List<IdleRange> GetCPUIdleTimes(List<int> threads, long start_time, List<List<ContextSwitch>> context_switches)
	{
		List<IdleRange> list = new List<IdleRange>();
		List<int> list2 = new List<int>();
		foreach (List<ContextSwitch> context_switch in context_switches)
		{
			int nextContextSwitchForThread = GetNextContextSwitchForThread(0, context_switch, threads, include_end_context_switches: true);
			list2.Add(nextContextSwitchForThread);
		}
		int coreWithNextContextSwitch = GetCoreWithNextContextSwitch(threads, context_switches, list2);
		long num = start_time;
		while (coreWithNextContextSwitch != -1)
		{
			List<ContextSwitch> list3 = context_switches[coreWithNextContextSwitch];
			int count = list3.Count;
			int num2 = list2[coreWithNextContextSwitch];
			ContextSwitch contextSwitch = list3[num2];
			if (threads.Contains(contextSwitch.m_NewThreadId))
			{
				if (contextSwitch.m_Timestamp != num)
				{
					IdleRange item = default(IdleRange);
					item.m_StartTime = num;
					item.m_EndTime = contextSwitch.m_Timestamp;
					list.Add(item);
				}
				int i;
				for (i = num2 + 1; i < count; i++)
				{
					ContextSwitch contextSwitch2 = list3[i];
					if (!threads.Contains(contextSwitch2.m_NewThreadId))
					{
						num = contextSwitch2.m_Timestamp;
						break;
					}
				}
				list2[coreWithNextContextSwitch] = i;
			}
			else
			{
				num = contextSwitch.m_Timestamp;
			}
			list2[coreWithNextContextSwitch] = GetNextContextSwitchForThread(list2[coreWithNextContextSwitch] + 1, list3, threads, include_end_context_switches: false);
			coreWithNextContextSwitch = GetCoreWithNextContextSwitch(threads, context_switches, list2);
		}
		return list;
	}

	public static void GetTimeSpanIdleRanges(List<IdleRange> idle_ranges, TimeSpan time_span, List<IdleRange> time_span_idle_ranges)
	{
		IdleRange item = default(IdleRange);
		foreach (IdleRange idle_range in idle_ranges)
		{
			if (idle_range.m_StartTime < time_span.EndTime && idle_range.m_EndTime > time_span.StartTime)
			{
				item.m_StartTime = Math.Max(idle_range.m_StartTime, time_span.StartTime);
				item.m_EndTime = Math.Min(idle_range.m_EndTime, time_span.EndTime);
				if (item.m_StartTime != item.m_EndTime)
				{
					time_span_idle_ranges.Add(item);
				}
			}
			else if (idle_range.m_StartTime > time_span.EndTime)
			{
				break;
			}
		}
	}

	public static int ScaleDPI(int value, Graphics graphics)
	{
		return (int)(((float)value + 0.5f) * graphics.DpiX / 96f);
	}

	public static void SetProcessAffinity(ulong affinity_mask)
	{
		Process currentProcess = Process.GetCurrentProcess();
		if (m_OriginalProcessAffinity == 0L)
		{
			m_OriginalProcessAffinity = (ulong)currentProcess.ProcessorAffinity.ToInt64();
		}
		currentProcess.ProcessorAffinity = (IntPtr)(long)(affinity_mask & m_OriginalProcessAffinity);
	}
}
