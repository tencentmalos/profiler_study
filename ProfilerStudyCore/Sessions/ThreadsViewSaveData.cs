using System.IO;

namespace ProfilerStudy;

public class ThreadsViewSaveData
{
	private bool m_IsValid;

	public bool IsValid => m_IsValid;

	public void Read(BinaryReader binary_reader)
	{
		m_IsValid = true;
	}

	public void Write(BinaryWriter binary_writer)
	{
	}
}
