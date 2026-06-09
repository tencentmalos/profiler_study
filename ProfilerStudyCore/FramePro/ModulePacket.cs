namespace ProfilerStudy;

internal sealed class ModulePacket : IPacket
{
	public int m_UseLookupFunctionForBaseAddress;

	public ulong m_ModuleBase;

	public ulong m_SigLow;

	public ulong m_SigHigh;

	public int m_Age;

	public string m_ModuleName;

	public string m_SymbolFilename;

	public void Read(ReceiveStream reader, int packed_value)
	{
		m_UseLookupFunctionForBaseAddress = reader.ReadInt32();
		m_ModuleBase = reader.ReadUInt64();
		m_SigLow = reader.ReadUInt64();
		m_SigHigh = reader.ReadUInt64();
		m_Age = reader.ReadInt32();
		reader.ReadInt32();
		m_ModuleName = StringReader.ReadInlineString(reader);
		m_SymbolFilename = StringReader.ReadInlineString(reader);
	}

	public int GetSize()
	{
		return 32 + StringReader.MaxInlineStringLength + StringReader.MaxInlineStringLength;
	}
}
