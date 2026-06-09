using System;
using System.IO;

namespace ProfilerStudy;

public struct TimeSpanInfo : IEquatable<TimeSpanInfo>
{
	private long m_Name;

	private long m_SourceInfo;

	private int m_Core;

	private int m_CallstackId;

	private int m_HashCode;

	private const int m_InvalidInfoId = int.MaxValue;

	public long Name => m_Name;

	public long SourceInfo => m_SourceInfo;

	public int Core => m_Core;

	public int CallstackId => m_CallstackId;

	public static int InvalidInfoId => int.MaxValue;

	public TimeSpanInfo(long name, long source_info, int core, int callstack_id)
	{
		m_Name = name;
		m_SourceInfo = source_info;
		m_Core = core;
		m_CallstackId = callstack_id;
		long num = 16777619L;
		num ^= m_Name * 16777619;
		num ^= m_SourceInfo * 16777619;
		num ^= m_Core * 16777619;
		num ^= m_CallstackId * 16777619;
		m_HashCode = (int)(num ^ (num >> 32));
	}

	public void OnStringRemapped(long old_string_id, long new_string_id)
	{
		if (m_Name == old_string_id)
		{
			m_Name = new_string_id;
		}
	}

	public override int GetHashCode()
	{
		return m_HashCode;
	}

	public override bool Equals(object other)
	{
		if (!(other is TimeSpanInfo))
		{
			return false;
		}
		return Equals((TimeSpanInfo)other);
	}

	public bool Equals(TimeSpanInfo other)
	{
		if (m_Name == other.Name && m_SourceInfo == other.SourceInfo && m_Core == other.Core)
		{
			return m_CallstackId == other.m_CallstackId;
		}
		return false;
	}

	public void Read(BinaryReader binary_reader, int version)
	{
		m_Name = binary_reader.ReadInt64();
		m_SourceInfo = binary_reader.ReadInt64();
		m_Core = binary_reader.ReadInt32();
		if (version >= 35)
		{
			m_CallstackId = binary_reader.ReadInt32();
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Name);
		binary_writer.Write(m_SourceInfo);
		binary_writer.Write(m_Core);
		binary_writer.Write(m_CallstackId);
	}
}
