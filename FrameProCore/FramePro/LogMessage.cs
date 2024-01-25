using System.IO;

namespace FramePro;

public class LogMessage
{
	private long m_Time;

	private string m_Message;

	public long Time => m_Time;

	public string Message => m_Message;

	public LogMessage()
	{
	}

	public LogMessage(long time, string message)
	{
		m_Time = time;
		m_Message = message;
	}

	public void Read(BinaryReader binary_reader)
	{
		m_Time = binary_reader.ReadInt64();
		m_Message = binary_reader.ReadString();
	}

	public void Write(BinaryWriter binary_writer)
	{
		binary_writer.Write(m_Time);
		binary_writer.Write(m_Message);
	}
}
