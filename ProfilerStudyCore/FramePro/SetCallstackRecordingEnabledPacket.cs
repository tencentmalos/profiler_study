using System.IO;

namespace ProfilerStudy;

internal sealed class SetCallstackRecordingEnabledPacket : SendPacket
{
	private int m_Enabled;

	public SetCallstackRecordingEnabledPacket(bool enabled)
		: base(PacketType.SetCallstackRecordingEnabledPacket)
	{
		m_Enabled = (enabled ? 1 : 0);
	}

	public override void Send(BinaryWriter writer)
	{
		base.Send(writer);
		writer.Write(m_Enabled);
	}
}
