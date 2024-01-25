namespace FramePro;

internal sealed class CallstackPacket : IPacket
{
	public int m_Id;

	public int m_Size;

	public ulong[] m_Stack;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_Id = reader.ReadInt32();
		m_Size = reader.ReadInt32();
		if (m_Size != 0)
		{
			if (m_Stack == null || m_Stack.Length < m_Size)
			{
				m_Stack = new ulong[m_Size];
			}
			for (int i = 0; i < m_Size; i++)
			{
				m_Stack[i] = reader.ReadUInt64();
			}
		}
		else if (m_Stack != null)
		{
			m_Stack = null;
		}
	}

	public int GetSize()
	{
		return 8 + m_Size * 8;
	}
}
