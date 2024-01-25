using System.IO;

namespace FramePro;

internal class Module
{
	public string m_SymbolFilename;

	public ulong m_Base;

	public string m_ModuleName;

	public bool m_IsMainModule;

	public bool m_UseBaseAddrLookupFunction;

	public int m_Age;

	public ulong m_SigLow;

	public ulong m_SigHigh;

	public void Read(BinaryReader reader)
	{
		m_SymbolFilename = reader.ReadString();
		m_Base = reader.ReadUInt64();
		m_ModuleName = reader.ReadString();
		m_IsMainModule = reader.ReadBoolean();
		m_UseBaseAddrLookupFunction = reader.ReadBoolean();
		m_Age = reader.ReadInt32();
		m_SigLow = reader.ReadUInt64();
		m_SigHigh = reader.ReadUInt64();
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(m_SymbolFilename);
		writer.Write(m_Base);
		writer.Write(m_ModuleName);
		writer.Write(m_IsMainModule);
		writer.Write(m_UseBaseAddrLookupFunction);
		writer.Write(m_Age);
		writer.Write(m_SigLow);
		writer.Write(m_SigHigh);
	}
}
