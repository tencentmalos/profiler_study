namespace FramePro;

internal sealed class RequestRecordedDataPacket : SendPacket
{
	public RequestRecordedDataPacket()
		: base(PacketType.RequestRecordedDataPacket)
	{
	}
}
