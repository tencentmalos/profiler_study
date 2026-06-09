using System.IO;

namespace ProfilerStudy;

public class SessionViewSaveData : IProfilerStudySerialisable
{
	public long m_FrameGraphVisibleRangeStart;

	public long m_FrameGraphVisibleRangeEnd;

	public long m_FrameGraphStart;

	public double m_FrameGraphViewScale;

	private bool m_IsValid;

	private ThreadsViewSaveData m_ThreadsViewSaveData = new ThreadsViewSaveData();

	public ThreadsViewSaveData ThreadsViewSaveData => m_ThreadsViewSaveData;

	public bool IsValid => m_IsValid;

	public void Read(BinaryReader binary_reader, int file_version)
	{
		m_FrameGraphVisibleRangeStart = binary_reader.ReadInt64();
		m_FrameGraphVisibleRangeEnd = binary_reader.ReadInt64();
		if (file_version >= 47)
		{
			m_FrameGraphStart = binary_reader.ReadInt64();
			m_FrameGraphViewScale = binary_reader.ReadDouble();
		}
		m_IsValid = true;
		if (file_version > 21)
		{
			m_ThreadsViewSaveData.Read(binary_reader);
		}
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_FrameGraphVisibleRangeStart);
		binary_writer.Write(m_FrameGraphVisibleRangeEnd);
		binary_writer.Write(m_FrameGraphStart);
		binary_writer.Write(m_FrameGraphViewScale);
		m_ThreadsViewSaveData.Write(binary_writer);
	}
}
