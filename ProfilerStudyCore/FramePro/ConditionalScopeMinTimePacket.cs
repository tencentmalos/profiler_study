using System.IO;

namespace FramePro;

internal sealed class ConditionalScopeMinTimePacket : SendPacket
{
	private int m_MinTime;

	public ConditionalScopeMinTimePacket(int min_time)
		: base(PacketType.ConditionalScopeMinTime)
	{
		m_MinTime = min_time;
	}

	public override void Send(BinaryWriter writer)
	{
		base.Send(writer);
		writer.Write(m_MinTime);
	}
}
