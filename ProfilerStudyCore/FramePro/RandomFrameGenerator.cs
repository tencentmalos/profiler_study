using System;
using System.Collections.Generic;

namespace FramePro;

internal class RandomFrameGenerator
{
	public static TimeSpan GenerateTimeSpans(long start_time, long frame_duration, Random rand, int max_depth, Session session)
	{
		int id = 0;
		Random rand2 = new Random(1234);
		TimeSpan timeSpan = new TimeSpan(session.GetTimeSpanInfoId(id++, 0L, 0, -1), start_time, start_time + frame_duration);
		GenerateTimeSpans(timeSpan, rand2, 0, ref id, max_depth, session);
		return timeSpan;
	}

	private static void GenerateTimeSpans(TimeSpan parent, Random rand, int depth, ref int id, int max_depth, Session session)
	{
		TimeSpan timeSpan = parent.Children;
		long num = parent.StartTime;
		long num2 = parent.EndTime - num;
		if (depth > 1 && rand.Next(100) < 50)
		{
			long num3 = rand.Next((int)(4 * num2 / 5), (int)num2);
			if (num3 == 0L)
			{
				return;
			}
			long num4 = num2 - num3;
			num += rand.Next((int)num4);
			num2 = num + num3 - num;
		}
		int num5 = rand.Next(1, 8);
		long num6 = 0L;
		List<long> list = new List<long>();
		for (int i = 0; i < num5; i++)
		{
			int num7 = rand.Next(1, (int)num2);
			list.Add(num7);
			num6 += num7;
		}
		long num8 = num;
		for (int j = 0; j < list.Count; j++)
		{
			long num9 = list[j] * num2 / num6;
			if (num9 != 0L && ShouldAdd(rand, depth, max_depth))
			{
				TimeSpan timeSpan2 = new TimeSpan(session.GetTimeSpanInfoId(id++, 0L, 0, -1), num8, num8 + num9);
				timeSpan2.Parent = parent;
				if (timeSpan == null)
				{
					parent.Children = timeSpan2;
				}
				else
				{
					timeSpan.Next = timeSpan2;
					timeSpan2.Prev = timeSpan;
				}
				timeSpan = timeSpan2;
				if (depth + 1 < max_depth)
				{
					GenerateTimeSpans(timeSpan2, rand, depth + 1, ref id, max_depth, session);
				}
			}
			num8 += num9;
		}
	}

	private static bool ShouldAdd(Random rand, int depth, int max_depth)
	{
		int num = rand.Next(100);
		int num2 = 50 + (max_depth - depth) * 50 / max_depth;
		return num <= num2;
	}
}
