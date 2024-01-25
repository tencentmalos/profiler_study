using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using SCLCoreCLR;

namespace FramePro;

internal class Settings : ISettings
{
	private CoreSettings m_CoreSettings = new CoreSettings();

	public bool m_FirstConnect;

	private string m_EMail;

	private string m_RegKey;

	private Size m_MainWindowSize = new Size(1064, 770);

	private bool m_MainWindowMaximised = true;

	private bool m_Shown10xLinkForm;

	private System.Collections.Generic.LinkedList<string> m_RecentlyOpenFiles = new System.Collections.Generic.LinkedList<string>();

	private bool m_ThreadsViewInfoPanelVisible;

	private bool m_ThreadsViewFrameGraphVisible = true;

	private bool m_ThreadsViewTimeSpanGraphVisible = true;

	private bool m_ThreadsViewCoreViewVisible = true;

	private bool m_ThreadsViewCustomStatsVisible;

	private bool m_ThreadsViewDataGridVisible;

	private bool m_CoresViewInfoPanelVisible;

	private bool m_CoresViewFrameGraphVisible = true;

	private bool m_CoresViewTimeSpanGraphVisible;

	private bool m_SaveChangedQuery = true;

	private bool m_DisableInteractiveSessionsForLocalProfiles = true;

	private bool m_CheckForUpdates = true;

	private bool m_NotifyNewVersion;

	private bool m_NewVersionAvailable;

	private string m_LastVersionChecked;

	private Dictionary<string, ThreadFilter> m_ThreadFilters = new Dictionary<string, ThreadFilter>();

	private string m_FilterThreadsTextBoxText;

	private bool m_NotTrackingContextSwitchesDismissed;

	private bool m_ThreadsViewCoreGraphContextSwitchesVisible = true;

	private bool m_CoresViewContextSwitchesVisible = true;

	private bool m_CoresViewWaitEventsVisible = true;

	private bool m_CoresViewShowHeirachy = true;

	private ScopeColourMode m_ScopeColourMode = ScopeColourMode.Scope;

	private int m_FrameGraphHeight = 100;

	private int m_TimeSpanGraphHeight = 163;

	private bool m_ShowStartupPage = true;

	private bool m_ShowContextSwitchWarningBox = true;

	private double m_ScopeGraphYScale = 2.0;

	private List<int> m_CoreGraphCoreHeights = new List<int>();

	private bool m_UserSetScopesViewGraphFlags;

	private List<string> m_ScopesViewGraphedScopes = new List<string>();

	private List<string> m_CustomStatsViewGraphedStats = new List<string>();

	private double m_ThreadsViewFrameGraphYScale = 1.0;

	private double m_CoresViewFrameGraphYScale = 1.0;

	private int m_ThreadsViewCorePanelHeight = -1;

	private bool m_OutputWindowVisible;

	private int m_OutputWindowHeight = -1;

	private int m_ScopesGraphHeight = -1;

	private int m_CustomStatsDataGridHeight = -1;

	private bool m_ThreadsViewWaitEVentsVisible = true;

	private CustomStatXAxisMode m_ScopesViewXAxisMode;

	private SessionScrollBar.EMode m_SessionScrollBarMode = SessionScrollBar.EMode.Frame;

	private List<string> m_CustomStatsVisibleInThreadView = new List<string>();

	private TimeUnitsNEW m_ScopeDataGridTimeUnits = TimeUnitsNEW.Microseconds;

	private int m_ScopeDataGridWidth = 500;

	private int m_ThreadsViewCustomStatsGraphHeight = 80;

	private string m_AndroidADBExePath = "";

	private int m_AndroidContextSwitchRecordingDuration = 5;

	private string m_CustomStatsSortedColumn = "";

	private bool m_CustomStatsSortedColumnIncreasing;

	private int m_ThreadScopeHeight = 23;

	private List<string> m_SourceRoots = new List<string>();

	private ulong m_FrameProThreadAffinity = 4294967295uL;

	private ThreadIdMode m_ThreadIdMode;

	public string EMail
	{
		get
		{
			return m_EMail;
		}
		set
		{
			m_EMail = value;
		}
	}

	public string RegKey
	{
		get
		{
			return m_RegKey;
		}
		set
		{
			m_RegKey = value;
		}
	}

	public long ConditionalScopeTimeSliderMinTime
	{
		get
		{
			return m_CoreSettings.ConditionalScopeTimeSliderMinTime;
		}
		set
		{
			m_CoreSettings.ConditionalScopeTimeSliderMinTime = value;
		}
	}

	public Size MainWindowSize
	{
		get
		{
			return m_MainWindowSize;
		}
		set
		{
			m_MainWindowSize = value;
		}
	}

	public bool MainWindowMaximised
	{
		get
		{
			return m_MainWindowMaximised;
		}
		set
		{
			m_MainWindowMaximised = value;
		}
	}

	public bool Shown10xLinkForm
	{
		get
		{
			return m_Shown10xLinkForm;
		}
		set
		{
			m_Shown10xLinkForm = value;
		}
	}

	public double TargetFrameMS
	{
		get
		{
			return m_CoreSettings.TargetFrameMS;
		}
		set
		{
			m_CoreSettings.TargetFrameMS = value;
		}
	}

	public double ScopeTargetTime
	{
		get
		{
			return m_CoreSettings.ScopeTargetTime;
		}
		set
		{
			m_CoreSettings.ScopeTargetTime = value;
		}
	}

	public CoreSettings CoreSettings => m_CoreSettings;

	public ICollection<string> RecentFiles => m_RecentlyOpenFiles;

	public bool ThreadsViewInfoPanelVisible
	{
		get
		{
			return m_ThreadsViewInfoPanelVisible;
		}
		set
		{
			m_ThreadsViewInfoPanelVisible = value;
		}
	}

	public bool ThreadsViewFrameGraphVisible
	{
		get
		{
			return m_ThreadsViewFrameGraphVisible;
		}
		set
		{
			m_ThreadsViewFrameGraphVisible = value;
		}
	}

	public bool ThreadsViewTimeSpanGraphVisible
	{
		get
		{
			return m_ThreadsViewTimeSpanGraphVisible;
		}
		set
		{
			m_ThreadsViewTimeSpanGraphVisible = value;
		}
	}

	public bool ThreadsViewCoreViewVisible
	{
		get
		{
			return m_ThreadsViewCoreViewVisible;
		}
		set
		{
			m_ThreadsViewCoreViewVisible = value;
		}
	}

	public bool ThreadsViewCustomStatsVisible
	{
		get
		{
			return m_ThreadsViewCustomStatsVisible;
		}
		set
		{
			m_ThreadsViewCustomStatsVisible = value;
		}
	}

	public bool ThreadsViewDataGridVisible
	{
		get
		{
			return m_ThreadsViewDataGridVisible;
		}
		set
		{
			m_ThreadsViewDataGridVisible = value;
		}
	}

	public bool CoresViewInfoPanelVisible
	{
		get
		{
			return m_CoresViewInfoPanelVisible;
		}
		set
		{
			m_CoresViewInfoPanelVisible = value;
		}
	}

	public bool CoresViewFrameGraphVisible
	{
		get
		{
			return m_CoresViewFrameGraphVisible;
		}
		set
		{
			m_CoresViewFrameGraphVisible = value;
		}
	}

	public bool CoresViewTimeSpanGraphVisible
	{
		get
		{
			return m_CoresViewTimeSpanGraphVisible;
		}
		set
		{
			m_CoresViewTimeSpanGraphVisible = value;
		}
	}

	public List<Connection> Connections
	{
		get
		{
			return m_CoreSettings.Connections;
		}
		set
		{
			m_CoreSettings.Connections = value;
		}
	}

	public bool SaveChangedQuery
	{
		get
		{
			return m_SaveChangedQuery;
		}
		set
		{
			m_SaveChangedQuery = value;
		}
	}

	public bool DisableInteractiveSessionsForLocalProfiles
	{
		get
		{
			return m_DisableInteractiveSessionsForLocalProfiles;
		}
		set
		{
			m_DisableInteractiveSessionsForLocalProfiles = value;
		}
	}

	public string LastVersionChecked
	{
		get
		{
			return m_LastVersionChecked;
		}
		set
		{
			m_LastVersionChecked = value;
		}
	}

	public bool CheckForUpdates
	{
		get
		{
			return m_CheckForUpdates;
		}
		set
		{
			m_CheckForUpdates = value;
		}
	}

	public bool NotifyNewVersion
	{
		get
		{
			return m_NotifyNewVersion;
		}
		set
		{
			m_NotifyNewVersion = value;
		}
	}

	public bool NewVersionAvailable
	{
		get
		{
			return m_NewVersionAvailable;
		}
		set
		{
			m_NewVersionAvailable = value;
		}
	}

	public string FilterThreadsTextBoxText
	{
		get
		{
			return m_FilterThreadsTextBoxText;
		}
		set
		{
			m_FilterThreadsTextBoxText = value;
		}
	}

	public bool NotTrackingContextSwitchesDismissed
	{
		get
		{
			return m_NotTrackingContextSwitchesDismissed;
		}
		set
		{
			m_NotTrackingContextSwitchesDismissed = value;
		}
	}

	public bool ThreadsViewCoreGraphContextSwitchesVisible
	{
		get
		{
			return m_ThreadsViewCoreGraphContextSwitchesVisible;
		}
		set
		{
			m_ThreadsViewCoreGraphContextSwitchesVisible = value;
		}
	}

	public bool CoresViewContextSwitchesVisible
	{
		get
		{
			return m_CoresViewContextSwitchesVisible;
		}
		set
		{
			m_CoresViewContextSwitchesVisible = value;
		}
	}

	public bool CoresViewWaitEventsVisible
	{
		get
		{
			return m_CoresViewWaitEventsVisible;
		}
		set
		{
			m_CoresViewWaitEventsVisible = value;
		}
	}

	public bool CoresViewShowHeirachy
	{
		get
		{
			return m_CoresViewShowHeirachy;
		}
		set
		{
			m_CoresViewShowHeirachy = value;
		}
	}

	public ScopeColourMode ScopeColourMode
	{
		get
		{
			return m_ScopeColourMode;
		}
		set
		{
			m_ScopeColourMode = value;
		}
	}

	public int FrameGraphHeight
	{
		get
		{
			return m_FrameGraphHeight;
		}
		set
		{
			m_FrameGraphHeight = value;
		}
	}

	public int TimeSpanGraphHeight
	{
		get
		{
			return m_TimeSpanGraphHeight;
		}
		set
		{
			m_TimeSpanGraphHeight = value;
		}
	}

	public bool ShowStartupPage
	{
		get
		{
			return m_ShowStartupPage;
		}
		set
		{
			m_ShowStartupPage = value;
		}
	}

	public bool ShowContextSwitchWarningBox
	{
		get
		{
			return m_ShowContextSwitchWarningBox;
		}
		set
		{
			m_ShowContextSwitchWarningBox = value;
		}
	}

	public double ScopeGraphYScale
	{
		get
		{
			return m_ScopeGraphYScale;
		}
		set
		{
			m_ScopeGraphYScale = value;
		}
	}

	public List<int> CoreGraphCoreHeights
	{
		get
		{
			return m_CoreGraphCoreHeights;
		}
		set
		{
			m_CoreGraphCoreHeights = value;
		}
	}

	public bool UserSetScopesViewGraphFlags
	{
		get
		{
			return m_UserSetScopesViewGraphFlags;
		}
		set
		{
			m_UserSetScopesViewGraphFlags = value;
		}
	}

	public List<string> ScopesViewGraphedScopes
	{
		get
		{
			return m_ScopesViewGraphedScopes;
		}
		set
		{
			m_ScopesViewGraphedScopes = value;
		}
	}

	public List<string> CustomStatsViewGraphedStats
	{
		get
		{
			return m_CustomStatsViewGraphedStats;
		}
		set
		{
			m_CustomStatsViewGraphedStats = value;
		}
	}

	public double ThreadsViewFrameGraphYScale
	{
		get
		{
			return m_ThreadsViewFrameGraphYScale;
		}
		set
		{
			m_ThreadsViewFrameGraphYScale = value;
		}
	}

	public double CoresViewFrameGraphYScale
	{
		get
		{
			return m_CoresViewFrameGraphYScale;
		}
		set
		{
			m_CoresViewFrameGraphYScale = value;
		}
	}

	public int ThreadsViewCorePanelHeight
	{
		get
		{
			return m_ThreadsViewCorePanelHeight;
		}
		set
		{
			m_ThreadsViewCorePanelHeight = value;
		}
	}

	public bool OutputWindowVisible
	{
		get
		{
			return m_OutputWindowVisible;
		}
		set
		{
			m_OutputWindowVisible = value;
		}
	}

	public int OutputWindowHeight
	{
		get
		{
			return m_OutputWindowHeight;
		}
		set
		{
			m_OutputWindowHeight = value;
		}
	}

	public int ScopesGraphHeight
	{
		get
		{
			return m_ScopesGraphHeight;
		}
		set
		{
			m_ScopesGraphHeight = value;
		}
	}

	public int CustomStatsDataGridHeight
	{
		get
		{
			return m_CustomStatsDataGridHeight;
		}
		set
		{
			m_CustomStatsDataGridHeight = value;
		}
	}

	public bool ThreadsViewWaitEVentsVisible
	{
		get
		{
			return m_ThreadsViewWaitEVentsVisible;
		}
		set
		{
			m_ThreadsViewWaitEVentsVisible = value;
		}
	}

	public static string DefaultPort => CoreSettings.DefaultPort;

	public CustomStatXAxisMode ScopesViewXAxisMode
	{
		get
		{
			return m_ScopesViewXAxisMode;
		}
		set
		{
			m_ScopesViewXAxisMode = value;
		}
	}

	public SessionScrollBar.EMode SessionScrollBarMode
	{
		get
		{
			return m_SessionScrollBarMode;
		}
		set
		{
			m_SessionScrollBarMode = value;
		}
	}

	public TimeUnitsNEW ScopeDataGridTimeUnits
	{
		get
		{
			return m_ScopeDataGridTimeUnits;
		}
		set
		{
			m_ScopeDataGridTimeUnits = value;
		}
	}

	public int ScopeDataGridWidth
	{
		get
		{
			return m_ScopeDataGridWidth;
		}
		set
		{
			m_ScopeDataGridWidth = value;
		}
	}

	public int ThreadsViewCustomStatsGraphHeight
	{
		get
		{
			return m_ThreadsViewCustomStatsGraphHeight;
		}
		set
		{
			m_ThreadsViewCustomStatsGraphHeight = value;
		}
	}

	public bool RegisterUsingPureDevReg => m_CoreSettings.RegisterUsingPureDevReg;

	public bool RegisterUsingGUID
	{
		get
		{
			return m_CoreSettings.RegisterUsingGUID;
		}
		set
		{
			m_CoreSettings.RegisterUsingGUID = value;
		}
	}

	public string AndroidADBExePath
	{
		get
		{
			return m_AndroidADBExePath;
		}
		set
		{
			m_AndroidADBExePath = value;
		}
	}

	public int AndroidContextSwitchRecordingDuration
	{
		get
		{
			return m_AndroidContextSwitchRecordingDuration;
		}
		set
		{
			m_AndroidContextSwitchRecordingDuration = value;
		}
	}

	public string CustomStatsSortedColumn
	{
		get
		{
			return m_CustomStatsSortedColumn;
		}
		set
		{
			m_CustomStatsSortedColumn = value;
		}
	}

	public bool CustomStatsSortedColumnIncreasing
	{
		get
		{
			return m_CustomStatsSortedColumnIncreasing;
		}
		set
		{
			m_CustomStatsSortedColumnIncreasing = value;
		}
	}

	public int ThreadScopeHeight
	{
		get
		{
			return m_ThreadScopeHeight;
		}
		set
		{
			m_ThreadScopeHeight = value;
		}
	}

	public List<string> SourceRoots
	{
		get
		{
			return m_SourceRoots;
		}
		set
		{
			m_SourceRoots = value;
		}
	}

	public ulong FrameProThreadAffinity
	{
		get
		{
			return m_FrameProThreadAffinity;
		}
		set
		{
			m_FrameProThreadAffinity = value;
		}
	}

	public ThreadIdMode ThreadIdMode
	{
		get
		{
			return m_ThreadIdMode;
		}
		set
		{
			m_ThreadIdMode = value;
		}
	}

	public string SourceViewerTool { get; set; } = "VSCode";

	public Settings()
	{
		try
		{
			Read();
		}
		catch (Exception ex)
		{
			Log.WriteLine(ex.Message);
		}
	}

	public bool Read()
	{
		if (!File.Exists(CoreSettings.Path))
		{
			return false;
		}
		XmlReadStream xmlReadStream = new XmlReadStream();
		xmlReadStream.Load(CoreSettings.Path);
		if (xmlReadStream.StartElement("FramePro"))
		{
			m_CoreSettings.Read(xmlReadStream);
			if (xmlReadStream.StartElement("Connection"))
			{
				xmlReadStream.Read("FirstConnect", ref m_FirstConnect);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("Registration"))
			{
				xmlReadStream.Read("EMail", ref m_EMail);
				xmlReadStream.Read("RegKey", ref m_RegKey);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("UI"))
			{
				xmlReadStream.Read("MainWindowSize", ref m_MainWindowSize);
				xmlReadStream.Read("MainWindowMaximised", ref m_MainWindowMaximised);
				xmlReadStream.Read("Shown10xLinkForm2", ref m_Shown10xLinkForm);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("Files"))
			{
				xmlReadStream.Read("RecentlyOpenedFiles", ref m_RecentlyOpenFiles);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("ThreadsView"))
			{
				xmlReadStream.Read("ThreadsViewInfoPanelVisible", ref m_ThreadsViewInfoPanelVisible);
				xmlReadStream.Read("ThreadsViewFrameGraphVisible", ref m_ThreadsViewFrameGraphVisible);
				xmlReadStream.Read("ThreadsViewTimeSpanGraphVisible", ref m_ThreadsViewTimeSpanGraphVisible);
				xmlReadStream.Read("ThreadsViewCoreViewVisible", ref m_ThreadsViewCoreViewVisible);
				xmlReadStream.Read("ThreadsViewCustomStatsVisible", ref m_ThreadsViewCustomStatsVisible);
				xmlReadStream.Read("ThreadsViewDataGridVisible", ref m_ThreadsViewDataGridVisible);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("CoresView"))
			{
				xmlReadStream.Read("CoresViewInfoPanelVisible", ref m_CoresViewInfoPanelVisible);
				xmlReadStream.Read("CoresViewFrameGraphVisible", ref m_CoresViewFrameGraphVisible);
				xmlReadStream.Read("CoresViewTimeSpanGraphVisible", ref m_CoresViewTimeSpanGraphVisible);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("ThreadFilters"))
			{
				m_ThreadFilters.Clear();
				for (int i = 0; i < xmlReadStream.Count; i++)
				{
					xmlReadStream.StartElement(i);
					string value = "";
					if (xmlReadStream.Read("SessionName", ref value))
					{
						ThreadFilter threadFilter = new ThreadFilter();
						threadFilter.Read(xmlReadStream);
						m_ThreadFilters[value] = threadFilter;
					}
					xmlReadStream.EndElement();
				}
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("Misc"))
			{
				xmlReadStream.Read("SaveChangedQuery", ref m_SaveChangedQuery);
				xmlReadStream.Read("DisableInteractiveSessionsForLocalProfiles", ref m_DisableInteractiveSessionsForLocalProfiles);
				xmlReadStream.Read("CheckForUpdates", ref m_CheckForUpdates);
				xmlReadStream.Read("NotifyNewVersion", ref m_NotifyNewVersion);
				xmlReadStream.Read("NewVersionAvailable", ref m_NewVersionAvailable);
				xmlReadStream.Read("LastVersionChecked", ref m_LastVersionChecked);
				xmlReadStream.Read("NotTrackingContextSwitchesDismissed", ref m_NotTrackingContextSwitchesDismissed);
				xmlReadStream.Read("ThreadsViewCoreGraphContextSwitchesVisible", ref m_ThreadsViewCoreGraphContextSwitchesVisible);
				xmlReadStream.Read("CoresViewContextSwitchesVisible", ref m_CoresViewContextSwitchesVisible);
				xmlReadStream.Read("CoresViewWaitEventsVisible", ref m_CoresViewWaitEventsVisible);
				xmlReadStream.Read("CoresViewShowHeirachy", ref m_CoresViewShowHeirachy);
				xmlReadStream.Read("ScopeColourMode", ref m_ScopeColourMode);
				xmlReadStream.Read("FrameGraphHeight", ref m_FrameGraphHeight);
				xmlReadStream.Read("TimeSpanGraphHeight_new", ref m_TimeSpanGraphHeight);
				xmlReadStream.Read("ShowStartupPage", ref m_ShowStartupPage);
				xmlReadStream.Read("ShowContextSwitchWarningBox", ref m_ShowContextSwitchWarningBox);
				xmlReadStream.Read("ScopeGraphYScale", ref m_ScopeGraphYScale);
				xmlReadStream.Read("CoreGraphCoreHeights", ref m_CoreGraphCoreHeights);
				xmlReadStream.Read("UserSetScopesViewGraphFlags", ref m_UserSetScopesViewGraphFlags);
				xmlReadStream.Read("ScopesViewGraphedScopes", ref m_ScopesViewGraphedScopes);
				xmlReadStream.Read("CustomStatsViewGraphedStats", ref m_CustomStatsViewGraphedStats);
				xmlReadStream.Read("ThreadsViewFrameGraphYScale", ref m_ThreadsViewFrameGraphYScale);
				xmlReadStream.Read("CoresViewFrameGraphYScale", ref m_CoresViewFrameGraphYScale);
				xmlReadStream.Read("ThreadsViewCorePanelHeight", ref m_ThreadsViewCorePanelHeight);
				xmlReadStream.Read("OutputWindowVisible", ref m_OutputWindowVisible);
				xmlReadStream.Read("OutputWindowHeight", ref m_OutputWindowHeight);
				xmlReadStream.Read("ScopesGraphHeight", ref m_ScopesGraphHeight);
				xmlReadStream.Read("CustomStatsDataGridHeight", ref m_CustomStatsDataGridHeight);
				xmlReadStream.Read("ThreadsViewWaitEVentsVisible", ref m_ThreadsViewWaitEVentsVisible);
				xmlReadStream.Read("ScopesViewXAxisMode", ref m_ScopesViewXAxisMode);
				xmlReadStream.Read("SessionScrollBarMode", ref m_SessionScrollBarMode);
				xmlReadStream.Read("CustomStatsVisibleInThreadView", ref m_CustomStatsVisibleInThreadView);
				xmlReadStream.Read("ScopeDataGridTimeUnits", ref m_ScopeDataGridTimeUnits);
				xmlReadStream.Read("ScopeDataGridWidth", ref m_ScopeDataGridWidth);
				xmlReadStream.Read("ThreadsViewCustomStatsGraphHeight", ref m_ThreadsViewCustomStatsGraphHeight);
				xmlReadStream.Read("AndroidADBExePath", ref m_AndroidADBExePath);
				xmlReadStream.Read("AndroidContextSwitchRecordingDuration", ref m_AndroidContextSwitchRecordingDuration);
				xmlReadStream.Read("CustomStatsSortedColumn", ref m_CustomStatsSortedColumn);
				xmlReadStream.Read("CustomStatsSortedColumnIncreasing", ref m_CustomStatsSortedColumnIncreasing);
				xmlReadStream.Read("ThreadScopeHeight", ref m_ThreadScopeHeight);
				xmlReadStream.Read("SourceRoots", ref m_SourceRoots);
				xmlReadStream.Read("FrameProThreadAffinity", ref m_FrameProThreadAffinity);
				xmlReadStream.Read("ThreadIdMode", ref m_ThreadIdMode);
				string viewerTool = "";
				xmlReadStream.Read("SourceViewerTool", ref viewerTool);
				SourceViewerTool = viewerTool;
				xmlReadStream.EndElement();
			}
			xmlReadStream.EndElement();
		}
		return true;
	}

	public void Write()
	{
		string directoryName = Path.GetDirectoryName(CoreSettings.Path);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		XmlWriteStream xmlWriteStream = new XmlWriteStream();
		xmlWriteStream.StartElement("FramePro");
		m_CoreSettings.Write(xmlWriteStream);
		xmlWriteStream.StartElement("Connection");
		xmlWriteStream.Write("FirstConnect", m_FirstConnect);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("Registration");
		xmlWriteStream.Write("EMail", m_EMail);
		xmlWriteStream.Write("RegKey", m_RegKey);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("UI");
		xmlWriteStream.Write("MainWindowSize", m_MainWindowSize);
		xmlWriteStream.Write("MainWindowMaximised", m_MainWindowMaximised);
		xmlWriteStream.Write("Shown10xLinkForm2", m_Shown10xLinkForm);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("Files");
		xmlWriteStream.Write("RecentlyOpenedFiles", m_RecentlyOpenFiles);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("ThreadsView");
		xmlWriteStream.Write("ThreadsViewInfoPanelVisible", m_ThreadsViewInfoPanelVisible);
		xmlWriteStream.Write("ThreadsViewFrameGraphVisible", m_ThreadsViewFrameGraphVisible);
		xmlWriteStream.Write("ThreadsViewTimeSpanGraphVisible", m_ThreadsViewTimeSpanGraphVisible);
		xmlWriteStream.Write("ThreadsViewCoreViewVisible", m_ThreadsViewCoreViewVisible);
		xmlWriteStream.Write("ThreadsViewCustomStatsVisible", m_ThreadsViewCustomStatsVisible);
		xmlWriteStream.Write("ThreadsViewDataGridVisible", m_ThreadsViewDataGridVisible);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("CoresView");
		xmlWriteStream.Write("CoresViewInfoPanelVisible", m_CoresViewInfoPanelVisible);
		xmlWriteStream.Write("CoresViewFrameGraphVisible", m_CoresViewFrameGraphVisible);
		xmlWriteStream.Write("CoresViewTimeSpanGraphVisible", m_CoresViewTimeSpanGraphVisible);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("ThreadFilters");
		foreach (string key in m_ThreadFilters.Keys)
		{
			xmlWriteStream.StartElement("ThreadFilter");
			xmlWriteStream.Write("SessionName", key);
			m_ThreadFilters[key].Write(xmlWriteStream);
			xmlWriteStream.EndElement();
		}
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("Misc");
		xmlWriteStream.Write("SaveChangedQuery", m_SaveChangedQuery);
		xmlWriteStream.Write("DisableInteractiveSessionsForLocalProfiles", m_DisableInteractiveSessionsForLocalProfiles);
		xmlWriteStream.Write("CheckForUpdates", m_CheckForUpdates);
		xmlWriteStream.Write("NotifyNewVersion", m_NotifyNewVersion);
		xmlWriteStream.Write("NewVersionAvailable", m_NewVersionAvailable);
		xmlWriteStream.Write("LastVersionChecked", m_LastVersionChecked);
		xmlWriteStream.Write("NotTrackingContextSwitchesDismissed", m_NotTrackingContextSwitchesDismissed);
		xmlWriteStream.Write("ThreadsViewCoreGraphContextSwitchesVisible", m_ThreadsViewCoreGraphContextSwitchesVisible);
		xmlWriteStream.Write("CoresViewContextSwitchesVisible", m_CoresViewContextSwitchesVisible);
		xmlWriteStream.Write("CoresViewWaitEventsVisible", m_CoresViewWaitEventsVisible);
		xmlWriteStream.Write("CoresViewShowHeirachy", m_CoresViewShowHeirachy);
		xmlWriteStream.Write("ScopeColourMode", m_ScopeColourMode);
		xmlWriteStream.Write("FrameGraphHeight", m_FrameGraphHeight);
		xmlWriteStream.Write("TimeSpanGraphHeight_new", m_TimeSpanGraphHeight);
		xmlWriteStream.Write("ShowStartupPage", m_ShowStartupPage);
		xmlWriteStream.Write("ShowContextSwitchWarningBox", m_ShowContextSwitchWarningBox);
		xmlWriteStream.Write("ScopeGraphYScale", m_ScopeGraphYScale);
		xmlWriteStream.Write("CoreGraphCoreHeights", m_CoreGraphCoreHeights);
		xmlWriteStream.Write("UserSetScopesViewGraphFlags", m_UserSetScopesViewGraphFlags);
		xmlWriteStream.Write("ScopesViewGraphedScopes", m_ScopesViewGraphedScopes);
		xmlWriteStream.Write("CustomStatsViewGraphedStats", m_CustomStatsViewGraphedStats);
		xmlWriteStream.Write("ThreadsViewFrameGraphYScale", m_ThreadsViewFrameGraphYScale);
		xmlWriteStream.Write("CoresViewFrameGraphYScale", m_CoresViewFrameGraphYScale);
		xmlWriteStream.Write("ThreadsViewCorePanelHeight", m_ThreadsViewCorePanelHeight);
		xmlWriteStream.Write("OutputWindowVisible", m_OutputWindowVisible);
		xmlWriteStream.Write("OutputWindowHeight", m_OutputWindowHeight);
		xmlWriteStream.Write("ScopesGraphHeight", m_ScopesGraphHeight);
		xmlWriteStream.Write("CustomStatsDataGridHeight", m_CustomStatsDataGridHeight);
		xmlWriteStream.Write("ThreadsViewWaitEVentsVisible", m_ThreadsViewWaitEVentsVisible);
		xmlWriteStream.Write("ScopesViewXAxisMode", m_ScopesViewXAxisMode);
		xmlWriteStream.Write("SessionScrollBarMode", m_SessionScrollBarMode);
		xmlWriteStream.Write("CustomStatsVisibleInThreadView", m_CustomStatsVisibleInThreadView);
		xmlWriteStream.Write("ScopeDataGridTimeUnits", m_ScopeDataGridTimeUnits);
		xmlWriteStream.Write("ScopeDataGridWidth", m_ScopeDataGridWidth);
		xmlWriteStream.Write("ThreadsViewCustomStatsGraphHeight", m_ThreadsViewCustomStatsGraphHeight);
		xmlWriteStream.Write("AndroidADBExePath", m_AndroidADBExePath);
		xmlWriteStream.Write("AndroidContextSwitchRecordingDuration", m_AndroidContextSwitchRecordingDuration);
		xmlWriteStream.Write("CustomStatsSortedColumn", m_CustomStatsSortedColumn);
		xmlWriteStream.Write("CustomStatsSortedColumnIncreasing", m_CustomStatsSortedColumnIncreasing);
		xmlWriteStream.Write("ThreadScopeHeight", m_ThreadScopeHeight);
		xmlWriteStream.Write("SourceRoots", m_SourceRoots);
		xmlWriteStream.Write("FrameProThreadAffinity", m_FrameProThreadAffinity);
		xmlWriteStream.Write("ThreadIdMode", m_ThreadIdMode);
        xmlWriteStream.Write("SourceViewerTool", SourceViewerTool);
        xmlWriteStream.EndElement();
		xmlWriteStream.EndElement();

		////string text = CoreSettings.Path + ".temp";
		////xmlWriteStream.Save(text);
		////File.Delete(CoreSettings.Path);
		////File.Move(text, CoreSettings.Path);

		try
		{
			xmlWriteStream.Save(CoreSettings.Path);
		}
		catch(Exception ex)
		{
			//Just ignore here
			;
		}
	}

	public void WriteToLog()
	{
		m_CoreSettings.WriteToLog();
		Log.WriteLine("FirstConnect: " + m_FirstConnect);
		Log.WriteLine("EMail: " + m_EMail);
		Log.WriteLine("RegKey: " + m_RegKey);
		Size mainWindowSize = m_MainWindowSize;
		Log.WriteLine("MainWindowSize: " + mainWindowSize.ToString());
		Log.WriteLine("MainWindowMaximised: " + m_MainWindowMaximised);
		Log.WriteLine("Shown10xLinkForm: " + m_Shown10xLinkForm);
		Log.WriteLine("RecentlyOpenedFiles:");
		foreach (string recentlyOpenFile in m_RecentlyOpenFiles)
		{
			Log.WriteLine("    " + recentlyOpenFile);
		}
	}

	public void AddRecentFile(string file)
	{
		if (m_RecentlyOpenFiles.Count == 0 || m_RecentlyOpenFiles.First.Value != file)
		{
			m_RecentlyOpenFiles.Remove(file);
			m_RecentlyOpenFiles.AddFirst(file);
			if (m_RecentlyOpenFiles.Count > 5)
			{
				m_RecentlyOpenFiles.RemoveLast();
			}
			Write();
		}
	}

	public void RemoveRecentFile(string file)
	{
		m_RecentlyOpenFiles.Remove(file);
		Write();
	}

	public Connection GetCurrentConnection()
	{
		return m_CoreSettings.GetCurrentConnection();
	}

	public ThreadFilter GetThreadFilter(string session_name)
	{
		if (!m_ThreadFilters.TryGetValue(session_name, out var value))
		{
			return null;
		}
		return value;
	}

	public void SetThreadFilter(string session_name, ThreadFilter thread_filter)
	{
		m_ThreadFilters[session_name] = thread_filter;
	}

	public void RemoveThreadFilter(string session_name)
	{
		m_ThreadFilters.Remove(session_name);
	}

	public bool GetCustomStatVisibleInThreadView(string name)
	{
		return m_CustomStatsVisibleInThreadView.Contains(name);
	}

	public void SetCustomStatVisibleInThreadView(string name, bool visible)
	{
		if (visible)
		{
			if (!m_CustomStatsVisibleInThreadView.Contains(name))
			{
				m_CustomStatsVisibleInThreadView.Add(name);
			}
		}
		else if (m_CustomStatsVisibleInThreadView.Contains(name))
		{
			m_CustomStatsVisibleInThreadView.Remove(name);
		}
	}

	public void SetRegisterUsingPureDevReg(bool value, bool require_restart)
	{
		m_CoreSettings.SetRegisterUsingPureDevReg(value, require_restart);
	}
}
