using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SCLCoreCLR;
using System.Windows.Forms;

namespace FramePro;

public class CoreSettings : ISettings
{
	public delegate void SymbolPathsChangedHandler();

	public delegate void CallstackFiltersChangedHandler();

    //Environment.GetFolderPath(  Environment.SpecialFolder.ApplicationData)
    private static string m_UserLocalFolder = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\cache\\";

	private static string m_Path = m_UserLocalFolder + "FramePro.settings";

	private List<Connection> m_Connections = new List<Connection>();

	private const string m_DefaultIP = "localhost";

	private const string m_DefaultPort = "8428";

	private long m_ConditionalScopeTimeSliderMinTime = 50L;

	private double m_TargetFrameMS = 33.333333333333336;

	private double m_ScopeTargetTime = 33.333333333333336;

	private Dictionary<string, CustomStatInfo> m_CustomStatXAxisModes = new Dictionary<string, CustomStatInfo>();

	private List<string> m_CallstackFilters = new List<string>();

	private bool m_RegisterUsingPureDevReg;

	private bool m_RegisterUsingPureDevRegCurrent;

	private bool m_RegisterUsingGUID;

	private List<string> m_SymbolPaths = new List<string>();

	private int m_MaxVisibleThreads = 100;

	private Dictionary<string, Color> m_ScopeColours = new Dictionary<string, Color>();

	private Dictionary<string, Color> m_CustomStatColours = new Dictionary<string, Color>();

	public static string Path => m_Path;

	public static string UserLocalFolder => m_UserLocalFolder;

	public long ConditionalScopeTimeSliderMinTime
	{
		get
		{
			return m_ConditionalScopeTimeSliderMinTime;
		}
		set
		{
			m_ConditionalScopeTimeSliderMinTime = value;
		}
	}

	public List<Connection> Connections
	{
		get
		{
			return m_Connections;
		}
		set
		{
			m_Connections = value;
		}
	}

	public double TargetFrameMS
	{
		get
		{
			return m_TargetFrameMS;
		}
		set
		{
			m_TargetFrameMS = value;
		}
	}

	public double ScopeTargetTime
	{
		get
		{
			return m_ScopeTargetTime;
		}
		set
		{
			m_ScopeTargetTime = value;
		}
	}

	public static string DefaultPort => "8428";

	public ICollection<string> CallstackFilters => m_CallstackFilters;

	public bool RegisterUsingPureDevReg => m_RegisterUsingPureDevRegCurrent;

	public bool RegisterUsingGUID
	{
		get
		{
			return m_RegisterUsingGUID;
		}
		set
		{
			m_RegisterUsingGUID = value;
		}
	}

	public List<string> SymbolPaths
	{
		get
		{
			return m_SymbolPaths;
		}
		set
		{
			if (!CoreUtils.ListsEqual(m_SymbolPaths, value))
			{
				m_SymbolPaths = new List<string>(value);
				if (this.SymbolPathsChanged != null)
				{
					this.SymbolPathsChanged();
				}
			}
		}
	}

	public int MaxVisibleThreads
	{
		get
		{
			return m_MaxVisibleThreads;
		}
		set
		{
			m_MaxVisibleThreads = value;
		}
	}

	public event SymbolPathsChangedHandler SymbolPathsChanged;

	public event CallstackFiltersChangedHandler CallstackFiltersChanged;

	public CoreSettings()
	{
		m_CallstackFilters.Add("FramePro::*");
	}

	public bool Read()
	{
		if (!File.Exists(m_Path))
		{
			return false;
		}
		XmlReadStream xmlReadStream = new XmlReadStream();
		xmlReadStream.Load(m_Path);
		if (xmlReadStream.StartElement("FramePro"))
		{
			Read(xmlReadStream);
			xmlReadStream.EndElement();
		}
		return true;
	}

	public Connection GetCurrentConnection()
	{
		if (m_Connections.Count != 0)
		{
			return m_Connections[0];
		}
		return new Connection
		{
			m_IP = "localhost",
			m_Port = "8428"
		};
	}

	public void Read(XmlReadStream stream)
	{
		if (stream.StartElement("Connections"))
		{
			for (int i = 0; i < stream.Count; i++)
			{
				stream.StartElement(i);
				Connection connection = new Connection();
				connection.Read(stream);
				m_Connections.Add(connection);
				stream.EndElement();
			}
			stream.EndElement();
		}
		if (stream.StartElement("Target"))
		{
			stream.Read("ScopeTargetTime", ref m_ScopeTargetTime);
			stream.Read("TargetFrameMS", ref m_TargetFrameMS);
			stream.EndElement();
		}
		if (stream.StartElement("CustomStatXAxisModes"))
		{
			for (int j = 0; j < stream.Count; j++)
			{
				stream.StartElement(j);
				string value = "error";
				if (stream.Read("Name", ref value))
				{
					CustomStatInfo customStatInfo = new CustomStatInfo();
					if (customStatInfo.Read(stream))
					{
						m_CustomStatXAxisModes[value] = customStatInfo;
					}
				}
				stream.EndElement();
			}
			stream.EndElement();
		}
		stream.Read("CallstackFilters", ref m_CallstackFilters);
		stream.Read("RegisterUsingPureDevReg", ref m_RegisterUsingPureDevReg);
		m_RegisterUsingPureDevRegCurrent = m_RegisterUsingPureDevReg;
		stream.Read("RegisterUsingGUID", ref m_RegisterUsingGUID);
		stream.Read("SymbolPaths", ref m_SymbolPaths);
		stream.Read("MaxVisibleThreads", ref m_MaxVisibleThreads);
		if (stream.StartElement("ScopeColours"))
		{
			for (int k = 0; k < stream.Count; k++)
			{
				stream.StartElement(k);
				string value2 = "";
				Color value3 = Color.Red;
				if (stream.Read("Name", ref value2) && stream.Read("Colour", ref value3) && value2.Length != 0)
				{
					m_ScopeColours[value2] = value3;
				}
				stream.EndElement();
			}
			stream.EndElement();
		}
		if (!stream.StartElement("CustomStatColours"))
		{
			return;
		}
		for (int l = 0; l < stream.Count; l++)
		{
			stream.StartElement(l);
			string value4 = "";
			Color value5 = Color.Red;
			if (stream.Read("Name", ref value4) && stream.Read("Colour", ref value5) && value4.Length != 0)
			{
				m_CustomStatColours[value4] = value5;
			}
			stream.EndElement();
		}
		stream.EndElement();
	}

	public void Write(XmlWriteStream stream)
	{
		stream.StartElement("Connections");
		foreach (Connection connection in m_Connections)
		{
			stream.StartElement("Connection");
			connection.Write(stream);
			stream.EndElement();
		}
		stream.EndElement();
		stream.StartElement("Target");
		stream.Write("TargetFrameMS", m_TargetFrameMS);
		stream.Write("ScopeTargetTime", m_ScopeTargetTime);
		stream.EndElement();
		stream.StartElement("CustomStatXAxisModes");
		foreach (string key in m_CustomStatXAxisModes.Keys)
		{
			if (!key.Contains("pending"))
			{
				stream.StartElement("CustomStat");
				stream.Write("Name", key);
				m_CustomStatXAxisModes[key].Write(stream);
				stream.EndElement();
			}
		}
		stream.EndElement();
		stream.Write("CallstackFilters", m_CallstackFilters);
		stream.Write("RegisterUsingPureDevReg", m_RegisterUsingPureDevReg);
		stream.Write("RegisterUsingGUID", m_RegisterUsingGUID);
		stream.Write("SymbolPaths", m_SymbolPaths);
		stream.Write("MaxVisibleThreads", m_MaxVisibleThreads);
		stream.StartElement("ScopeColours");
		foreach (string key2 in m_ScopeColours.Keys)
		{
			stream.StartElement("ScopeColour");
			stream.Write("Name", key2);
			stream.Write("Colour", m_ScopeColours[key2]);
			stream.EndElement();
		}
		stream.EndElement();
		stream.StartElement("CustomStatColours");
		foreach (string key3 in m_CustomStatColours.Keys)
		{
			stream.StartElement("CustomStatColour");
			stream.Write("Name", key3);
			stream.Write("Colour", m_CustomStatColours[key3]);
			stream.EndElement();
		}
		stream.EndElement();
	}

	public void WriteToLog()
	{
		Log.WriteLine("TargetFrameMS: " + m_TargetFrameMS);
	}

	public CustomStatInfo GetCustomStatInfo(string name)
	{
		if (!m_CustomStatXAxisModes.TryGetValue(name, out var value))
		{
			value = new CustomStatInfo();
			m_CustomStatXAxisModes[name] = value;
		}
		return value;
	}

	public void SetCallstackFilters(List<string> filters)
	{
		if (!CoreUtils.ListsEqual(m_CallstackFilters, filters))
		{
			m_CallstackFilters = new List<string>(filters);
			if (this.CallstackFiltersChanged != null)
			{
				this.CallstackFiltersChanged();
			}
		}
	}

	public void SetRegisterUsingPureDevReg(bool value, bool require_restart)
	{
		m_RegisterUsingPureDevReg = value;
		if (!require_restart)
		{
			m_RegisterUsingPureDevRegCurrent = value;
		}
	}

	public bool GetScopeColour(string name, ref Color colour)
	{
		return m_ScopeColours.TryGetValue(name, out colour);
	}

	public void SetScopeColour(string name, Color colour)
	{
		m_ScopeColours[name] = colour;
	}

	public bool GetCustomStatColour(string name, ref Color colour)
	{
		return m_CustomStatColours.TryGetValue(name, out colour);
	}

	public void SetCustomStatColour(string name, Color colour)
	{
		m_CustomStatColours[name] = colour;
	}
}
