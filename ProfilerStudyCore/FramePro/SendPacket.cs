using System.IO;

namespace ProfilerStudy;

internal abstract class SendPacket
{
	private PacketType m_PacketType;

	public SendPacket(PacketType packet_type)
	{
		m_PacketType = packet_type;
	}

	public virtual void Send(BinaryWriter writer)
	{
		writer.Write((int)m_PacketType);
		writer.Write(1234);
	}
}
