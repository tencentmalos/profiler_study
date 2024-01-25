using System;
using System.Collections.Generic;

namespace FramePro;

internal class Tests
{
	private class TestTimeSpan : TimeSpan
	{
		private TimeSpan m_OriginalTimeSpan;

		public TimeSpan OriginalTimeSpan => m_OriginalTimeSpan;

		public TestTimeSpan(TimeSpan time_span)
			: base(time_span.TimeSpanInfoId, time_span.StartTime, time_span.EndTime)
		{
			m_OriginalTimeSpan = time_span;
		}
	}

	public static void Test()
	{
		CheckRootFirstTimeIteratorMoveNext();
		CheckRootFirstTimeIteratorMovePrev();
		CheckRootFirstTimeIteratorRandomMoveNext();
		CheckRootFirstTimeIteratorRandomMovePrev();
		CheckTimeSpanListWithTestTimeSpanTree();
		CheckTimeSpanListWithTestTimeSpanTreeRandom();
		CheckTimeSpanListWithRandomTree();
	}

	private static TimeSpan CreateTestTimeSpanTree()
	{
		TimeSpan timeSpan = new TimeSpan(0, 0L, 100L);
		TimeSpan timeSpan2 = new TimeSpan(1, 10L, 20L);
		TimeSpan timeSpan3 = new TimeSpan(2, 11L, 12L);
		TimeSpan timeSpan4 = new TimeSpan(3, 12L, 19L);
		TimeSpan timeSpan5 = new TimeSpan(4, 13L, 18L);
		TimeSpan timeSpan6 = new TimeSpan(5, 20L, 90L);
		TimeSpan timeSpan7 = new TimeSpan(6, 20L, 80L);
		TimeSpan timeSpan8 = new TimeSpan(7, 21L, 30L);
		TimeSpan timeSpan9 = new TimeSpan(8, 31L, 40L);
		TimeSpan timeSpan10 = new TimeSpan(9, 32L, 40L);
		TimeSpan timeSpan11 = new TimeSpan(10, 50L, 70L);
		timeSpan2.Parent = timeSpan;
		timeSpan.Children = timeSpan2;
		timeSpan2.Next = timeSpan6;
		timeSpan3.Parent = timeSpan2;
		timeSpan2.Children = timeSpan3;
		timeSpan3.Next = timeSpan4;
		timeSpan4.Parent = timeSpan2;
		timeSpan4.Prev = timeSpan3;
		timeSpan5.Parent = timeSpan4;
		timeSpan4.Children = timeSpan5;
		timeSpan6.Parent = timeSpan;
		timeSpan6.Prev = timeSpan2;
		timeSpan7.Parent = timeSpan6;
		timeSpan6.Children = timeSpan7;
		timeSpan8.Parent = timeSpan7;
		timeSpan7.Children = timeSpan8;
		timeSpan8.Next = timeSpan9;
		timeSpan9.Parent = timeSpan7;
		timeSpan9.Prev = timeSpan8;
		timeSpan9.Next = timeSpan11;
		timeSpan10.Parent = timeSpan9;
		timeSpan9.Children = timeSpan10;
		timeSpan11.Parent = timeSpan7;
		timeSpan11.Prev = timeSpan9;
		return timeSpan;
	}

	private static void CheckRootFirstTimeIteratorMoveNext()
	{
		new RootFirstTimeSpanIterator(CreateTestTimeSpanTree());
	}

	private static void CheckRootFirstTimeIteratorMovePrev()
	{
		new RootFirstTimeSpanIterator(CreateTestTimeSpanTree().Children.Next.Children.Children.Next.Next);
	}

	private static void GetTimeSpans(TimeSpan time_span, List<TimeSpan> time_spans)
	{
		time_spans.Add(time_span);
		for (TimeSpan timeSpan = time_span.Children; timeSpan != null; timeSpan = timeSpan.Next)
		{
			GetTimeSpans(timeSpan, time_spans);
		}
	}

	private static TimeSpan FindRandomTimeSpan(TimeSpan r, Random random)
	{
		List<TimeSpan> list = new List<TimeSpan>();
		GetTimeSpans(r, list);
		return list[random.Next(1, list.Count - 1)];
	}

	private static void CheckRootFirstTimeIteratorRandomMoveNext()
	{
		TimeSpan r = CreateTestTimeSpanTree();
		Random random = new Random(1234);
		for (int i = 0; i < 100; i++)
		{
			TimeSpan timeSpan = FindRandomTimeSpan(r, random);
			_ = timeSpan.TimeSpanInfoId;
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan);
			rootFirstTimeSpanIterator.MoveNext();
			while (rootFirstTimeSpanIterator.MoveNext())
			{
				_ = rootFirstTimeSpanIterator.Current.TimeSpanInfoId;
			}
		}
	}

	private static void CheckRootFirstTimeIteratorRandomMovePrev()
	{
		TimeSpan r = CreateTestTimeSpanTree();
		Random random = new Random(1234);
		for (int i = 0; i < 100; i++)
		{
			TimeSpan timeSpan = FindRandomTimeSpan(r, random);
			_ = timeSpan.TimeSpanInfoId;
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan);
			while (rootFirstTimeSpanIterator.MovePrev())
			{
				_ = rootFirstTimeSpanIterator.Current.TimeSpanInfoId;
			}
		}
	}

	private static string GetTimerName(TimeSpan time_span)
	{
		long num = ((time_span.StartTime == long.MinValue) ? long.MaxValue : time_span.EndTime);
		return time_span.TimeSpanInfoId + " " + time_span.StartTime + " " + num;
	}

	private static bool CheckTimeSpanTreesMatch(TimeSpan a, TimeSpan b)
	{
		if (a.ChildCount != b.ChildCount)
		{
			return false;
		}
		TimeSpan timeSpan = a.Children;
		TimeSpan timeSpan2 = b.Children;
		for (int i = 0; i < a.ChildCount; i++)
		{
			TestTimeSpan testTimeSpan = (TestTimeSpan)timeSpan2;
			if (timeSpan != testTimeSpan.OriginalTimeSpan)
			{
				return false;
			}
			if (!CheckTimeSpanTreesMatch(timeSpan, timeSpan2))
			{
				return false;
			}
			timeSpan = timeSpan.Next;
			timeSpan2 = timeSpan2.Next;
		}
		return true;
	}

	private static void CheckTimeSpanListWithTestTimeSpanTree()
	{
		TimeSpan timeSpan = CreateTestTimeSpanTree();
		TimeSpanList timeSpanList = new TimeSpanList(1000L, 0);
		List<TimeSpan> list = new List<TimeSpan>();
		GetTimeSpans(timeSpan, list);
		list.Remove(timeSpan);
		foreach (TimeSpan item in list)
		{
			TestTimeSpan time_span = new TestTimeSpan(item);
			timeSpanList.Add(time_span, GetTimerName);
		}
		if (!CheckTimeSpanTreesMatch(timeSpan, timeSpanList.RootTimeSpan))
		{
			CoreUtils.PrintHeirachy(timeSpan, GetTimerName);
			CoreUtils.PrintHeirachy(timeSpanList.RootTimeSpan, GetTimerName);
		}
	}

	private static void Reorder(List<TimeSpan> time_spans, Random random)
	{
		List<TimeSpan> list = new List<TimeSpan>(time_spans);
		time_spans.Clear();
		while (list.Count != 0)
		{
			int index = random.Next(list.Count - 1);
			TimeSpan item = list[index];
			list.RemoveAt(index);
			time_spans.Add(item);
		}
	}

	private static void CheckTimeSpanListWithTestTimeSpanTreeRandom()
	{
		CheckTimeSpanListWithTestTimeSpanTreeRandom(CreateTestTimeSpanTree());
	}

	private static void CheckTimeSpanListWithTestTimeSpanTreeRandom(TimeSpan r)
	{
		TimeSpanList timeSpanList = new TimeSpanList(1000L, 0);
		Random random = new Random(1234);
		List<TimeSpan> list = new List<TimeSpan>();
		GetTimeSpans(r, list);
		Reorder(list, random);
		list.Remove(r);
		foreach (TimeSpan item in list)
		{
			TestTimeSpan time_span = new TestTimeSpan(item);
			timeSpanList.Add(time_span, GetTimerName);
		}
		if (!CheckTimeSpanTreesMatch(r, timeSpanList.RootTimeSpan))
		{
			CoreUtils.PrintHeirachy(r, GetTimerName);
			CoreUtils.PrintHeirachy(timeSpanList.RootTimeSpan, GetTimerName);
		}
	}

	private static TimeSpan CreateRandomTree(int max_depth, int max_child_count, Random random)
	{
		ushort id = 0;
		TimeSpan timeSpan = new TimeSpan(id++, 0L, 1000000L);
		int depth = 0;
		CreateRandomTree(timeSpan, ref id, depth, random, max_depth, max_child_count);
		return timeSpan;
	}

	private static List<int> RandomValues(int max, int count, Random random)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < 2 * count + 1; i++)
		{
			list.Add(random.Next());
		}
		list.Sort();
		List<int> list2 = new List<int>();
		int num = 0;
		for (int j = 0; j < count; j++)
		{
			int item = list[num++];
			int item2 = list[num++];
			list2.Add(item);
			list2.Add(item2);
			if (random.Next(100) < 20)
			{
				num--;
			}
		}
		int num2 = list2[list2.Count - 1];
		for (int k = 0; k < list2.Count; k++)
		{
			list2[k] = ToInt((long)list2[k] * (long)max / num2);
		}
		List<int> list3 = new List<int>();
		int num3 = 0;
		for (int l = 0; l < count; l++)
		{
			int num4 = list2[num3++];
			int num5 = list2[num3++];
			if (num4 != num5)
			{
				list3.Add(num4);
				list3.Add(num5);
			}
		}
		if (list3.Count == 2 && list3[0] == 0 && list3[1] == max)
		{
			list3.Clear();
		}
		return list3;
	}

	private static void CreateRandomTree(TimeSpan parent, ref ushort id, int depth, Random random, int max_depth, int max_child_count)
	{
		if (depth == max_depth)
		{
			return;
		}
		int num = random.Next(max_child_count);
		if (num == 0)
		{
			return;
		}
		List<int> list = RandomValues(ToInt(parent.Duration), num, random);
		num = list.Count;
		if (num == 0)
		{
			return;
		}
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			long start_time = parent.StartTime + list[num2++];
			long num3 = parent.StartTime + list[num2++];
			TimeSpan timeSpan = new TimeSpan(id++, start_time, num3);
			timeSpan.Parent = parent;
			if (parent.Children == null)
			{
				parent.Children = timeSpan;
			}
			else
			{
				TimeSpan timeSpan2 = parent.Children;
				while (timeSpan2.Next != null)
				{
					timeSpan2 = timeSpan2.Next;
				}
				timeSpan2.Next = timeSpan;
				timeSpan.Prev = timeSpan2;
			}
			CreateRandomTree(timeSpan, ref id, depth + 1, random, max_depth, max_child_count);
			if (num3 == parent.EndTime)
			{
				break;
			}
		}
	}

	private static int ToInt(long value)
	{
		return (int)value;
	}

	private static void CheckTimeSpanListWithRandomTree()
	{
		CheckTimeSpanListWithRandomTree(3, 5, 783);
		CheckTimeSpanListWithRandomTree(3, 5, 27);
		CheckTimeSpanListWithRandomTree(3, 5, 1234);
		for (int i = 0; i < 1000; i++)
		{
			CheckTimeSpanListWithRandomTree(3, 5, i);
		}
		CheckTimeSpanListWithRandomTree(4, 6, 5678);
		CheckTimeSpanListWithRandomTree(5, 7, 2342);
		Random random = new Random(1234);
		for (int j = 0; j < 10000; j++)
		{
			int depth = random.Next(1, 6);
			int max_child_count = random.Next(1, 10);
			int seed = random.Next();
			CheckTimeSpanListWithRandomTree(depth, max_child_count, seed);
		}
	}

	private static void CheckTimeSpanListWithRandomTree(int depth, int max_child_count, int seed)
	{
		Random random = new Random(seed);
		CheckTimeSpanListWithTestTimeSpanTreeRandom(CreateRandomTree(depth, max_child_count, random));
	}
}
