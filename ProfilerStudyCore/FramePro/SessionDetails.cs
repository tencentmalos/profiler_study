using System;
using System.IO;

namespace FramePro;

public class SessionDetails : IFrameProSerialisable
{
	public static string DefaultName = "Unnamed Session";

	public string m_Name = DefaultName;

	public string m_BuildId = "-";

	public string m_Date = DateTime.Now.ToShortTimeString();

	public void Read(BinaryReader binary_reader, int version)
	{
		m_Name = binary_reader.ReadString();
		m_BuildId = binary_reader.ReadString();
		m_Date = binary_reader.ReadString();
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Name);
		binary_writer.Write(m_BuildId);
		binary_writer.Write(m_Date);
	}
}
