namespace FramePro;

internal interface IPacket
{
	void Read(ReceiveStream reader, int packed_value);
}
