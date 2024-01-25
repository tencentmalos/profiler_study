using System.Drawing;
using System.IO;

namespace FramePro;

public class Event
{
	private long m_Name;

	private long m_Time;

	private Color m_Colour;

	public long Name => m_Name;

	public long Time => m_Time;

	public Color Colour => m_Colour;

	public Event()
	{
	}

	public Event(long name, long time, uint colour)
	{
		m_Name = name;
		m_Time = time;
		m_Colour = CoreUtils.ToColor(colour);
	}

	public void Read(BinaryReader reader)
	{
		m_Name = reader.ReadInt64();
		m_Time = reader.ReadInt64();
		m_Colour = Color.FromArgb(reader.ReadInt32());
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_Name);
		writer.Write(m_Time);
		writer.Write(m_Colour.ToArgb());
	}
}
