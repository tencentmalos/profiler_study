namespace ProfilerStudy;

internal sealed class SetCustomStatColourPacket : IPacket
{
	public long m_Name;

	public uint m_Colour;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Colour = reader.ReadUInt32();
		m_Name = reader.ReadInt64();
	}

	public int GetSize()
	{
		return 12;
	}
}
