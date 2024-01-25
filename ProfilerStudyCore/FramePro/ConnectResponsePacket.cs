using System.IO;

namespace FramePro;

internal sealed class ConnectResponsePacket : SendPacket
{
	private int m_Interactive;

	private int m_RecordContextSwitches;

	public ConnectResponsePacket(bool interactive, bool record_context_switches)
		: base(PacketType.ConnectResponsePacket)
	{
		m_Interactive = (interactive ? 1 : 0);
		m_RecordContextSwitches = (record_context_switches ? 1 : 0);
	}

	public override void Send(BinaryWriter writer)
	{
		base.Send(writer);
		writer.Write(m_Interactive);
		writer.Write(m_RecordContextSwitches);
	}
}
