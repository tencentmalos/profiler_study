using System;
using SCLCoreCLR;

namespace FramePro;

public class Connection
{
	private bool m_Interactive = true;

	public string m_Name;

	public string m_IP;

	public string m_Port;

	public bool m_RecordConectSwitches = true;

	public bool m_RecordCallstacks;

	public string Name
	{
		get
		{
			return m_Name;
		}
		set
		{
			m_Name = value;
		}
	}

	public string IP
	{
		get
		{
			return m_IP;
		}
		set
		{
			m_IP = value;
		}
	}

	public string Port
	{
		get
		{
			return m_Port;
		}
		set
		{
			m_Port = value;
		}
	}

	public bool Interactive
	{
		get
		{
			return m_Interactive;
		}
		set
		{
			m_Interactive = value;
		}
	}

	public bool RecordConectSwitches
	{
		get
		{
			return m_RecordConectSwitches;
		}
		set
		{
			m_RecordConectSwitches = value;
		}
	}

	public bool RecordCallstacks
	{
		get
		{
			return m_RecordCallstacks;
		}
		set
		{
			m_RecordCallstacks = value;
		}
	}

	public void Read(XmlReadStream stream)
	{
		stream.Read("Name", ref m_Name);
		stream.Read("IP", ref m_IP);
		stream.Read("Port", ref m_Port);
		stream.Read("Interactive", ref m_Interactive);
		stream.Read("RecordConectSwitches", ref m_RecordConectSwitches);
		stream.Read("RecordCallstacks", ref m_RecordCallstacks);
	}

	public void Write(XmlWriteStream stream)
	{
		stream.Write("Name", m_Name);
		stream.Write("IP", m_IP);
		stream.Write("Port", m_Port);
		stream.Write("Interactive", m_Interactive);
		stream.Write("RecordConectSwitches", m_RecordConectSwitches);
		stream.Write("RecordCallstacks", m_RecordCallstacks);
	}

	public int GetPortAsInt()
	{
		try
		{
			return Convert.ToInt32(m_Port);
		}
		catch (Exception)
		{
			return 0;
		}
	}
}
