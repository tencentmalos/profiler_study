using System;
using System.Collections.Generic;
using System.IO;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class AndroidContextSwitchFileLoader
{
	public static bool Load(string filename, List<ContextSwitch> context_switches, ILog log)
	{
		if (!File.Exists(filename))
		{
			return false;
		}
		int i = 0;
		try
		{
			string[] array;
			for (array = File.ReadAllLines(filename); i < array.Length && !array[i].Contains("CPU#"); i++)
			{
			}
			if (i == array.Length - 1)
			{
				log.Write("Error parsing context switch file: unable to find column name line (CPU# TIMESTAMP etc)\n");
				return false;
			}
			string text = array[i];
			if (i == array.Length)
			{
				log.Write("Error parsing context switch file: truncated file\n");
				return false;
			}
			string text2 = array[i + 1];
			int num = text.IndexOf("CPU#");
			if (num == -1)
			{
				log.Write("Error parsing context switch file: unable to find column name line (CPU# TIMESTAMP etc)\n");
				return false;
			}
			int num2 = text2.IndexOf('|', num);
			if (num2 == -1)
			{
				log.Write("Error parsing context switch file: unable to find column name line (CPU# TIMESTAMP etc)\n");
				return false;
			}
			int num3 = text.IndexOf("TIMESTAMP");
			if (num3 == -1)
			{
				log.Write("Error parsing context switch file: unable to find column name line (CPU# TIMESTAMP etc)\n");
				return false;
			}
			int num4 = text2.IndexOf('|', num3);
			if (num4 == -1)
			{
				log.Write("Error parsing context switch file: unable to find column name line (CPU# TIMESTAMP etc)\n");
				return false;
			}
			int j;
			for (j = i; j < array.Length && !array[j].Contains("tracing_mark_write") && !array[j].Contains("parent_ts"); j++)
			{
			}
			if (j == array.Length)
			{
				log.Write("Error parsing context switch file: unable to find timestamp marker \"tracing_mark_write\"\n");
				return false;
			}
			long offset_ns = 0L;
			if (!ParseMarkerTimestamp(array[j], ref offset_ns))
			{
				log.Write("Error parsing context switch file: error parsing timestamp marker \"tracing_mark_write\"\n");
				return false;
			}
			for (; i < array.Length; i++)
			{
				string text3 = array[i];
				if (text3.Contains("sched_switch:"))
				{
					ParseContextSwitchLine(text3, context_switches, offset_ns, num2, num4, i, log);
				}
			}
		}
		catch (Exception ex)
		{
			log.Write("Error parsing context switch file (line " + i + ") : " + ex.Message + "\n");
			return false;
		}
		return true;
	}

	private static bool ParseMarkerTimestamp(string line, ref long offset_ns)
	{
		int num = line.IndexOf(']');
		if (num == -1)
		{
			return false;
		}
		int num2 = line.IndexOf(' ', num + 2);
		if (num2 == -1)
		{
			return false;
		}
		int num3 = line.IndexOf(':', num2);
		if (num3 == -1)
		{
			return false;
		}
		string timestamp = line.Substring(num2 + 1, num3 - (num2 + 1)).Trim();
		int num4 = line.IndexOf("parent_ts", num3);
		if (num4 == -1)
		{
			return false;
		}
		string timestamp2 = line.Substring(num4 + "parent_ts=".Length);
		long num5 = ConvertTimestampToNs(timestamp);
		long num6 = ConvertTimestampToNs(timestamp2);
		offset_ns = num6 - num5;
		return true;
	}

	private static long ConvertTimestampToNs(string timestamp)
	{
		return Convert.ToInt64(timestamp.Replace(".", "") + "000");
	}

	private static void ParseContextSwitchLine(string line, List<ContextSwitch> context_switches, long offset_ns, int core_column_offset, int timestamp_column_offset, int line_index, ILog log)
	{
		int num = core_column_offset;
		while (line[num] != '[')
		{
			num--;
			if (num < 0)
			{
				log.Write("Error parsing context switch file: error parsing CPU value (line " + line_index + ")");
				return;
			}
		}
		num++;
		int num2 = line.IndexOf(']', num);
		if (num2 == -1)
		{
			log.Write("Error parsing context switch file: error parsing CPU value (line " + line_index + ")");
			return;
		}
		string value = line.Substring(num, num2 - num);
		int num3 = timestamp_column_offset;
		while (line[num3] != ' ')
		{
			num3--;
			if (num3 < core_column_offset)
			{
				log.Write("Error parsing context switch file: error parsing TIMESTAMP value (line " + line_index + ")");
				return;
			}
		}
		num3++;
		int num4 = line.IndexOf(':', num3);
		if (num4 == -1)
		{
			log.Write("Error parsing context switch file: error parsing TIMESTAMP value (line " + line_index + ")");
			return;
		}
		string timestamp = line.Substring(num3, num4 - num3).Trim();
		int num5 = line.IndexOf("prev_pid", num4);
		if (num5 == -1)
		{
			log.Write("Error parsing context switch file: error parsing prev_pid value (line " + line_index + ")");
			return;
		}
		int num6 = line.IndexOf(' ', num5);
		if (num6 == -1)
		{
			log.Write("Error parsing context switch file: error parsing prev_pid value (line " + line_index + ")");
			return;
		}
		int num7 = num5 + "prev_pid=".Length;
		string value2 = line.Substring(num7, num6 - num7);
		int num8 = line.IndexOf("next_pid", num6);
		if (num8 == -1)
		{
			log.Write("Error parsing context switch file: error parsing next_pid value (line " + line_index + ")");
			return;
		}
		int num9 = line.IndexOf(' ', num8);
		if (num9 == -1)
		{
			log.Write("Error parsing context switch file: error parsing next_pid value (line " + line_index + ")");
			return;
		}
		int num10 = num8 + "next_pid=".Length;
		string value3 = line.Substring(num10, num9 - num10);
		long num11 = ConvertTimestampToNs(timestamp);
		ContextSwitch item = default(ContextSwitch);
		item.m_Timestamp = (num11 + offset_ns) * 100;
		item.m_ProcessId = -1;
		item.m_CPUId = Convert.ToInt32(value);
		item.m_OldThreadId = Convert.ToInt32(value2);
		item.m_NewThreadId = Convert.ToInt32(value3);
		item.m_OldThreadState = ((item.m_OldThreadId == 0) ? ThreadState.Waiting : ThreadState.Running);
		item.m_OldThreadWaitReason = ThreadWaitReason.WrExecutive;
		context_switches.Add(item);
	}
}
