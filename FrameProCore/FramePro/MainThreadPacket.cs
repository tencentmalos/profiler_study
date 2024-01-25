namespace FramePro;

internal sealed class MainThreadPacket : IPacket
{
	public int m_ThreadId;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_ThreadId = reader.ReadInt32();
	}

	public int GetSize()
	{
		return 4;
	}
}
