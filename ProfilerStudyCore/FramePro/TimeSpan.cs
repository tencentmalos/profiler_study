using System.IO;
using SCLCoreCLR;

namespace FramePro;

public class TimeSpan
{
	private long m_StartTime;

	private long m_Duration;

	private int m_TimeSpanInfo;

	private bool m_TimeSpanEx;

	private TimeSpan m_Parent;

	private TimeSpan m_Children;

	private TimeSpan m_Prev;

	private TimeSpan m_Next;

	public bool IsTimeSpanEx => m_TimeSpanEx;

	public int TimeSpanInfoId => m_TimeSpanInfo;

	public long StartTime => m_StartTime;

	public long EndTime => m_StartTime + Duration;

	public long Duration => m_Duration;

	public int ChildCount
	{
		get
		{
			int num = 0;
			for (TimeSpan timeSpan = m_Children; timeSpan != null; timeSpan = timeSpan.Next)
			{
				num++;
			}
			return num;
		}
	}

	public TimeSpan Parent
	{
		get
		{
			return m_Parent;
		}
		set
		{
			m_Parent = value;
		}
	}

	public TimeSpan Children
	{
		get
		{
			return m_Children;
		}
		set
		{
			m_Children = value;
		}
	}

	public TimeSpan Prev
	{
		get
		{
			return m_Prev;
		}
		set
		{
			m_Prev = value;
		}
	}

	public TimeSpan Next
	{
		get
		{
			return m_Next;
		}
		set
		{
			m_Next = value;
		}
	}

	public TimeSpan()
	{
	}

	public TimeSpan(int time_span_info_id, long start_time, long end_time)
		: this(time_span_info_id, start_time, end_time, ex_time_span: false)
	{
	}

	public TimeSpan(int time_span_info_id, long start_time, long end_time, bool ex_time_span)
	{
		m_StartTime = start_time;
		m_Duration = end_time - start_time;
		m_TimeSpanInfo = time_span_info_id;
		m_TimeSpanEx = ex_time_span;
	}

	public virtual void Read(BinaryReader binary_reader, int version, ThreadJobContext context, TimeSpanInfoSet time_span_info_set, long file_size)
	{
		if (version < 28)
		{
			if (version < 15)
			{
				long name = binary_reader.ReadInt64();
				long source_info = binary_reader.ReadInt64();
				m_StartTime = binary_reader.ReadInt64();
				long duration = binary_reader.ReadInt64() - m_StartTime;
				int core = 0;
				if (version > 4)
				{
					core = binary_reader.ReadInt32();
				}
				int id = time_span_info_set.GetId(name, source_info, core, -1);
				m_Duration = duration;
				m_TimeSpanInfo = id;
				m_TimeSpanEx = false;
				int num = binary_reader.ReadInt32();
				TimeSpan timeSpan = null;
				for (int i = 0; i < num; i++)
				{
					if (context.Cancel)
					{
						break;
					}
					TimeSpan timeSpan2 = new TimeSpan();
					timeSpan2.Read(binary_reader, version, context, time_span_info_set, file_size);
					timeSpan2.Parent = this;
					if (m_Children == null)
					{
						m_Children = timeSpan2;
					}
					else
					{
						timeSpan.Next = timeSpan2;
						timeSpan2.Prev = timeSpan;
					}
					timeSpan = timeSpan2;
					if ((i & 0x40) == 64)
					{
						context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, file_size);
					}
				}
				return;
			}
			m_StartTime = binary_reader.ReadInt64();
			ulong num2 = binary_reader.ReadUInt64();
			m_Duration = (long)(num2 & 0x7FFFFFFFFFFFL);
			m_TimeSpanInfo = (ushort)((num2 >> 48) & 0xFFFF);
			m_TimeSpanEx = ((num2 >> 47) & 1) != 0;
			int num3 = binary_reader.ReadInt32();
			TimeSpan timeSpan3 = null;
			for (int j = 0; j < num3; j++)
			{
				if (context.Cancel)
				{
					break;
				}
				TimeSpan timeSpan4 = new TimeSpan();
				timeSpan4.Read(binary_reader, version, context, time_span_info_set, file_size);
				timeSpan4.Parent = this;
				if (m_Children == null)
				{
					m_Children = timeSpan4;
				}
				else
				{
					timeSpan3.Next = timeSpan4;
					timeSpan4.Prev = timeSpan3;
				}
				timeSpan3 = timeSpan4;
				if ((j & 0x40) == 64)
				{
					context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, file_size);
				}
			}
			return;
		}
		if (version < 38)
		{
			m_StartTime = binary_reader.ReadInt64();
			ulong num4 = binary_reader.ReadUInt64();
			m_Duration = (long)(num4 & 0x7FFFFFFFFFFFL);
			m_TimeSpanInfo = (ushort)((num4 >> 48) & 0xFFFF);
			m_TimeSpanEx = ((num4 >> 47) & 1) != 0;
			int num5 = binary_reader.ReadInt32();
			TimeSpan timeSpan5 = null;
			for (int k = 0; k < num5; k++)
			{
				if (context.Cancel)
				{
					break;
				}
				TimeSpan timeSpan6 = (binary_reader.ReadBoolean() ? new TimeSpanEx() : new TimeSpan());
				timeSpan6.Read(binary_reader, version, context, time_span_info_set, file_size);
				timeSpan6.Parent = this;
				if (m_Children == null)
				{
					m_Children = timeSpan6;
				}
				else
				{
					timeSpan5.Next = timeSpan6;
					timeSpan6.Prev = timeSpan5;
				}
				timeSpan5 = timeSpan6;
				if ((k & 0x40) == 64)
				{
					context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, file_size);
				}
			}
			return;
		}
		m_StartTime = binary_reader.ReadInt64();
		m_Duration = binary_reader.ReadInt64();
		m_TimeSpanInfo = binary_reader.ReadInt32();
		m_TimeSpanEx = binary_reader.ReadBoolean();
		int num6 = binary_reader.ReadInt32();
		TimeSpan timeSpan7 = null;
		for (int l = 0; l < num6; l++)
		{
			if (context.Cancel)
			{
				break;
			}
			TimeSpan timeSpan8 = (binary_reader.ReadBoolean() ? new TimeSpanEx() : new TimeSpan());
			timeSpan8.Read(binary_reader, version, context, time_span_info_set, file_size);
			timeSpan8.Parent = this;
			if (m_Children == null)
			{
				m_Children = timeSpan8;
			}
			else
			{
				timeSpan7.Next = timeSpan8;
				timeSpan8.Prev = timeSpan7;
			}
			timeSpan7 = timeSpan8;
			if ((l & 0x40) == 64)
			{
				context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, file_size);
			}
		}
	}

	public virtual void Write(BinaryWriter binary_writer, ThreadJobContext context, ref int count, int max)
	{
		count++;
		if ((count & 0x20) == 0)
		{
			context.Progress.Set(count, max);
		}
		if (!context.Cancel)
		{
			binary_writer.Write(m_StartTime);
			binary_writer.Write(m_Duration);
			binary_writer.Write(m_TimeSpanInfo);
			binary_writer.Write(m_TimeSpanEx);
			binary_writer.Write(ChildCount);
			for (TimeSpan timeSpan = m_Children; timeSpan != null; timeSpan = timeSpan.Next)
			{
				binary_writer.Write(timeSpan.IsTimeSpanEx);
				timeSpan.Write(binary_writer, context, ref count, max);
			}
		}
	}

	public TimeSpan Clone()
	{
		TimeSpan timeSpan = new TimeSpan();
		timeSpan.Copy(this);
		return timeSpan;
	}

	public void Copy(TimeSpan time_span)
	{
		m_StartTime = time_span.m_StartTime;
		m_Duration = time_span.m_Duration;
		m_TimeSpanInfo = time_span.m_TimeSpanInfo;
		m_TimeSpanEx = time_span.m_TimeSpanEx;
	}

	public void Unlink()
	{
		if (m_Prev != null)
		{
			m_Prev.Next = m_Next;
		}
		else
		{
			m_Parent.Children = m_Parent.Children.Next;
		}
		if (m_Next != null)
		{
			m_Next.Prev = m_Prev;
		}
		m_Parent = null;
		m_Prev = null;
		m_Next = null;
	}

	public override string ToString()
	{
		long num = ((m_StartTime == long.MinValue) ? long.MaxValue : EndTime);
		return "TimeSpan " + TimeSpanInfoId + ": " + m_StartTime + " - " + num;
	}
}
