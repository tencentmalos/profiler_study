using System.IO;

namespace ProfilerStudy;

internal sealed class RequestStringLiteralPacket : SendPacket
{
	private long m_StringId;

	private StringLiteralType m_StringLiteralType;

	public RequestStringLiteralPacket(long string_id, StringLiteralType string_literal_type)
		: base(PacketType.RequestStringLiteral)
	{
		m_StringId = string_id;
		m_StringLiteralType = string_literal_type;
	}

	public override void Send(BinaryWriter writer)
	{
		base.Send(writer);
		writer.Write(m_StringId);
		writer.Write((int)m_StringLiteralType);
		writer.Write(5678);
	}
}
