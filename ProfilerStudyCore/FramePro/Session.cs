using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
////using ETLReader;
using SCLCoreCLR;
////using SymLibCLR;

namespace FramePro;

public class Session : IDisposable
{
	private struct FindTrigger
	{
		public long m_EventId;

		public long m_Time;
	}

	private struct ContextSwitchArrayKey
	{
		public long m_StartTime;

		public long m_EndTime;
	}

	private class PendingCustomStatValue
	{
		public double m_Value;

		public int m_Count;
	}

	private class TimeSpanCustomStatWithTime
	{
		public TimeSpanCustomStat m_CustomStat;

		public long m_Time;
	}

	private const int m_FrameProLibVersion = 17;

	private const int m_SaveFileVersion = 47;

	private const int m_DefaultFrameTime = 30;

	private const int m_ProcessEventsTickFrequency = 30;

	private const long m_FrameXPerFrame = 1000L;

	private byte[] m_FileMarker = new byte[8] { 102, 114, 97, 109, 101, 112, 114, 111 };

	private byte[] m_RecordingFileMarker = new byte[18]
	{
		102, 114, 97, 109, 101, 112, 114, 111, 95, 114,
		101, 99, 111, 114, 100, 105, 110, 103
	};

	private CoreSettings m_Settings;

	private ILog m_Log;

	private string m_SessionFilename;

	private TcpClient m_TcpCllient;

	private volatile bool m_Connected;

	private volatile int m_ConnectTime;

	private volatile int m_DisconnectTime;

	private volatile bool m_ReceivedConnectPacket;

	private Thread m_ReceiveThread;

	private Thread m_ProcessEventsThread;

	private Thread m_SendThread;

	private LargeArray<ReceivedPacket> m_ReceivedPackets = new LargeArray<ReceivedPacket>();

	private Frame m_CurrentFrame;

	private int m_MainThreadId = -1;

	private long m_RecordingFileSize;

	private long m_SendBufferSize;

	private long m_StringMemorySize;

	private long m_MiscMemorySize;

	private LargeArray<ReceivedPacket> m_ProcessPacketList = new LargeArray<ReceivedPacket>();

	private DisconnectReason m_DisconnectReason;

	private volatile int m_ReceivedFrameProLibVersion = -1;

	private long m_TotalTimeSpanCount;

	private SessionDetails m_SessionDetails = new SessionDetails();

	private List<SessionInfoPair> m_SessionInfoValues = new List<SessionInfoPair>();

	private ReadWriteLock m_SessionInfoValuesLock = new ReadWriteLock();

	private Dictionary<int, ThreadInfo> m_Threads = new Dictionary<int, ThreadInfo>();

	private ReadWriteLock m_ThreadsLock = new ReadWriteLock();

	private FrameArray m_Frames = new FrameArray();

	private ReadWriteLock m_FramesLock = new ReadWriteLock();

	private Dictionary<long, TimeSpanFrameStats> m_TimeSpanFrameStats = new Dictionary<long, TimeSpanFrameStats>();

	private ReadWriteLock m_TimeSpanFrameStatsLock = new ReadWriteLock();

	private List<UnassignedTimeSpan> m_UnassignedTimeSpans = new List<UnassignedTimeSpan>();

	private ClassAllocator<UnassignedTimeSpan> m_UnassignedTimeSpanAllocator = new ClassAllocator<UnassignedTimeSpan>();

	private Dictionary<int, TimeSpanList> m_TimeSpans = new Dictionary<int, TimeSpanList>();

	private ReadWriteLock m_TimeSpansLock = new ReadWriteLock();

	private Dictionary<long, string> m_Strings = new Dictionary<long, string>();

	private ReadWriteLock m_StringsLock = new ReadWriteLock();

	private Dictionary<string, long> m_StringIds = new Dictionary<string, long>();

	private const int m_DefaultStringRemappingsSize = 65535;

	private Dictionary<long, long> m_StringIdRemappings = new Dictionary<long, long>();

	private Set<long> m_TimeSpanNames = new Set<long>();

	private ReadWriteLock m_TimeSpanNamesLock = new ReadWriteLock();

	private Dictionary<long, SourceInfo> m_SourceInfos = new Dictionary<long, SourceInfo>();

	private ReadWriteLock m_SourceInfosLock = new ReadWriteLock();

	private List<string> m_ThreadOrder = new List<string>();

	private ReadWriteLock m_ThreadOrderLock = new ReadWriteLock();

	private long m_TimerFrequency;

	private long m_FirstFrameTime;

	private long m_LastFrameEndtime;

	private bool m_RecordingContextSwitches;

	private volatile int m_RemoteProcessId;

	private bool m_AssertedOnSourceString;

	private int m_MaxCoreIndex;

	private List<SendPacket> m_SendPacketQueue = new List<SendPacket>();

	private AutoResetEvent m_SendEvent = new AutoResetEvent(initialState: false);

	private volatile int m_FramesInBudget;

	private long m_TargetFrameTime;

	private long m_MaxFrameTime;

	private int m_MaxFrameIndex;

	private object m_MaxFrameTimeLock = new object();

	private object m_AverageLock = new object();

	private long m_AverageFrameTimeStart;

	private long m_AverageFrameTimeCount;

	private volatile bool m_ProcessingPackets;

	private volatile bool m_CancelProcessingPackets;

	private long m_PacketsToProcessCount;

	private bool m_Saved;

	private bool m_Interactive;

	private bool m_StartRecordingContextSwitches;

	private volatile bool m_IsReady;

	private int m_FrameTimeSpanCount;

	private int m_FrameBytesSentCount;

	private bool m_StartedXBoxOneEtlTrace;

	private Platform m_Platform = Platform.Unknown;

	private TimeSpanInfoSet m_TimeSpanInfoSet = new TimeSpanInfoSet();

	private ReadWriteLock m_TimeSpanInfoSetLock = new ReadWriteLock();

	private static bool m_ShownTimeSpanInfoErrorMessageBox;

	private string m_IP;

	private bool m_IsLocalIP;

	private volatile bool m_Disposing;

	private bool m_RecordingContextSwitchesStartedSucessfully;

	private List<ContextSwitchArray> m_ContextSwitchArray = new List<ContextSwitchArray>();

	private const int m_ContextSwitchArrayTimeBlockDuration = 100;

	private ReadWriteLock m_ContextSwitchArrayLock = new ReadWriteLock();

	private Dictionary<ContextSwitchArrayKey, List<List<ContextSwitch>>> m_ContextSwitchArrayCache = new Dictionary<ContextSwitchArrayKey, List<List<ContextSwitch>>>();

	private Queue<ContextSwitchArrayKey> m_ContextSwitchArrayCacheKeys = new Queue<ContextSwitchArrayKey>();

	private ReadWriteLock m_ContextSwitchArrayCacheLock = new ReadWriteLock();

	private const int m_MaxContextSwitchArrayCacheCount = 8;

	private volatile bool m_Recorded;

	private Dictionary<int, long> m_ProcessNames = new Dictionary<int, long>();

	private ReadWriteLock m_ProcessNamesLock = new ReadWriteLock();

	private Dictionary<int, string> m_LocalProcessNames = new Dictionary<int, string>();

	private ReadWriteLock m_LocalProcessNamesLock = new ReadWriteLock();

	private long m_LastTimeSpanTime;

	private volatile bool m_ShouldCapReceiveSpeed;

	private Dictionary<long, CustomStatSessionData> m_CustomStatSessionInfo = new Dictionary<long, CustomStatSessionData>();

	private ReadWriteLock m_CustomStatSessionInfoLock = new ReadWriteLock();

	private Dictionary<long, CustomStatValueType> m_CustomStatValueTypes = new Dictionary<long, CustomStatValueType>();

	private Dictionary<long, CustomStatValuePerSecArray> m_CustomStatValuePerSecArrays = new Dictionary<long, CustomStatValuePerSecArray>();

	private Dictionary<long, PendingCustomStatValue> m_PendingCustomStatIntervalValues = new Dictionary<long, PendingCustomStatValue>();

	private ManualResetEvent m_FinishedProcessingPacketsEvent = new ManualResetEvent(initialState: false);

	private List<LogMessage> m_LogMessages = new List<LogMessage>();

	private long m_TotalReceivedPackets;

	private PacketAllocator m_PacketAllocator = new PacketAllocator();

	private List<Event> m_Events = new List<Event>();

	private Dictionary<int, TimeArray<WaitEvent>> m_WaitEvents = new Dictionary<int, TimeArray<WaitEvent>>();

	private Dictionary<long, List<WaitEvent>> m_TempWaitEventsDict = new Dictionary<long, List<WaitEvent>>();

	private List<WaitEvent> m_TempWaitEventsList = new List<WaitEvent>();

	private WaitEventIterator m_TempWaitEventIterator = new WaitEventIterator();

	private Dictionary<int, Array<TimeSpanCustomStatWithTime>> m_TimeSpanCustomStatArrays = new Dictionary<int, Array<TimeSpanCustomStatWithTime>>();

	private Array<TimeSpanCustomStat> m_TimeSpanCustomStatArrayTemp = new Array<TimeSpanCustomStat>();

	////private SymLib m_SymLib = new SymLib(CoreSettings.UserLocalFolder);

	private int m_ReceivedModuleCount;

	private List<Module> m_Modules = new List<Module>();

	private volatile bool m_LoadedModuleSymbols;

	private bool m_RecordCallstacks;

	private Thread m_DeserialiseReceiveThread;

	private const int m_NetworkReceiveBufferSize = 32768;

	private const long m_ReadRecordingFileBufferSize = 32768L;

	private AutoResetEvent m_ReceiveThreadMainFinishedEvent = new AutoResetEvent(initialState: false);

	private Dictionary<long, Color> m_ScopeColours = new Dictionary<long, Color>();

	private Dictionary<long, long> m_CustomStatGraphs = new Dictionary<long, long>();

	private Dictionary<long, long> m_CustomStatUnits = new Dictionary<long, long>();

	private Dictionary<long, Color> m_CustomStatColours = new Dictionary<long, Color>();

	private const string m_StringNotFoundError = "ERROR STRING NOT FOUND";

	private Dictionary<long, string> m_StringLiteralNamedTimeSpanNames = new Dictionary<long, string>();

	private List<CustomStatPacket> m_PendingCustomStatPackets = new List<CustomStatPacket>();

	private static Color[] m_ThreadOrderColours = new Color[10]
	{
		Misc.ColorFromHSV(213.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(245.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(112.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(50.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(159.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(22.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(295.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(0.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(240.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue),
		Misc.ColorFromHSV(87.0, Colours.ThreadColourSaturation, Colours.ThreadColourValue)
	};

	public static long FrameXPerFrame => 1000L;

	public long TimerFrequency => m_TimerFrequency;

	private long ReceivedPacketCount
	{
		get
		{
			lock (m_ReceivedPackets)
			{
				return m_ReceivedPackets.Count;
			}
		}
	}

	private long ProcessPacketListCount
	{
		get
		{
			lock (m_ProcessPacketList)
			{
				return m_ProcessPacketList.Count;
			}
		}
	}

	public long SendBufferSize => m_SendBufferSize;

	public long StringMemorySize => m_StringMemorySize;

	public long MiscMemorySize => m_MiscMemorySize;

	public long RecordingFileSize => m_RecordingFileSize;

	public bool IsReady => m_IsReady;

	public string IP => m_IP;

	public bool Connected => m_Connected;

	public bool ReceivedConnectPacket => m_ReceivedConnectPacket;

	public bool ProcessingPackets => m_ProcessingPackets;

	public long PacketsToProcessCount
	{
		get
		{
			long packetsToProcessCount = m_PacketsToProcessCount;
			lock (m_ReceivedPackets)
			{
				return packetsToProcessCount + m_ReceivedPackets.Count;
			}
		}
	}

	public long FirstFrameTime => Interlocked.Read(ref m_FirstFrameTime);

	public string SessionFilename
	{
		get
		{
			return m_SessionFilename;
		}
		set
		{
			if (m_SessionFilename != value)
			{
				m_SessionFilename = value;
				if (this.FilenameChanged != null)
				{
					this.FilenameChanged();
				}
			}
		}
	}

	public long LastFrameEndTime => Interlocked.Read(ref m_LastFrameEndtime);

	public int FrameCount
	{
		get
		{
			using (new ReadLockScope(m_FramesLock))
			{
				return m_Frames.Count;
			}
		}
	}

	public int MainThreadId => m_MainThreadId;

	public DisconnectReason DisconnectReason => m_DisconnectReason;

	public int ReceivedFrameProLibVersion => m_ReceivedFrameProLibVersion;

	public static int FrameProLibVersion => 17;

	public long AverageFrameTime
	{
		get
		{
			lock (m_AverageLock)
			{
				long averageFrameTimeCount = m_AverageFrameTimeCount;
				return (averageFrameTimeCount != 0L) ? ((LastFrameEndTime - m_AverageFrameTimeStart) / averageFrameTimeCount) : 0;
			}
		}
	}

	private long AverageTimeSpanCount => m_TotalTimeSpanCount / FrameCount;

	public int CoreCount => m_MaxCoreIndex + 1;

	public long TotalConnectTime => LastFrameEndTime - FirstFrameTime;

	public int FramesInBudget => m_FramesInBudget;

	public bool Saved => m_Saved;

	public bool Interactive => m_Interactive;

	public int BufferTime
	{
		get
		{
			if (PacketsToProcessCount != 0L)
			{
				int num = (m_Connected ? Environment.TickCount : m_DisconnectTime) - m_ConnectTime;
				int num2 = (int)((LastFrameEndTime - FirstFrameTime) * 1000 / m_TimerFrequency);
				return num - num2;
			}
			return 0;
		}
	}

	public int ProcessingCompletePercent
	{
		get
		{
			lock (m_ReceivedPackets)
			{
				return Misc.Clamp((int)((m_TotalReceivedPackets != 0L) ? ((m_TotalReceivedPackets - PacketsToProcessCount) * 100 / m_TotalReceivedPackets) : 0), 0, 100);
			}
		}
	}

	public int ConnectTime => m_ConnectTime;

	public int ThreadCount
	{
		get
		{
			using (new ReadLockScope(m_TimeSpansLock))
			{
				return m_TimeSpans.Keys.Count;
			}
		}
	}

	public long TimeSpanCount
	{
		get
		{
			long num = 0L;
			List<TimeSpanList> list;
			using (new ReadLockScope(m_TimeSpansLock))
			{
				list = new List<TimeSpanList>(m_TimeSpans.Values);
			}
			foreach (TimeSpanList item in list)
			{
				using (new ReadLockScope(item.Lock))
				{
					num += item.TotalTimeSpanCount;
				}
			}
			return num;
		}
	}

	public SessionDetails SessionDetails => m_SessionDetails;

	public string Filename => m_SessionFilename;

	public int ProcessId => m_RemoteProcessId;

	public bool RecordingContextSwitches
	{
		get
		{
			return m_RecordingContextSwitches;
		}
		set
		{
			if (m_RecordingContextSwitches != value)
			{
				m_RecordingContextSwitches = value;
				if (this.RecordingContextSwitchesChanged != null)
				{
					this.RecordingContextSwitchesChanged();
				}
			}
		}
	}

	public Platform Platform => m_Platform;

	public bool Recorded => m_Recorded;

	public bool ShouldCapReceiveSpeed
	{
		get
		{
			return m_ShouldCapReceiveSpeed;
		}
		set
		{
			m_ShouldCapReceiveSpeed = value;
		}
	}

	public static int SaveFileVersion => 47;

	public bool IsLocalConnnection
	{
		get
		{
			if (m_Connected)
			{
				return m_IsLocalIP;
			}
			return false;
		}
	}

	public long DefaultFrameTime => 30 * m_TimerFrequency / 1000;

	public bool RecordCallstacks
	{
		get
		{
			return m_RecordCallstacks;
		}
		set
		{
			if (m_RecordCallstacks != value)
			{
				m_RecordCallstacks = value;
				Send(new SetCallstackRecordingEnabledPacket(value));
				if (this.RecordCallstacksChanged != null)
				{
					this.RecordCallstacksChanged();
				}
			}
		}
	}

	public long MaxFrameTime
	{
		get
		{
			lock (m_MaxFrameTimeLock)
			{
				return m_MaxFrameTime;
			}
		}
	}

	public int MaxFrameIndex
	{
		get
		{
			lock (m_MaxFrameTimeLock)
			{
				return m_MaxFrameIndex;
			}
		}
	}

	public event SessionIsReadyHandler SessionIsReady;

	public event DisconnectedHandler Disconnected;

	public event ThreadAddedHandler ThreadAdded;

	public event ThreadNameChangedHandler ThreadNameChanged;

	public event ThreadOrderChangedHandler ThreadOrderChanged;

	public event NonInteractiveModeFinishedHandler NonInteractiveModeFinished;

	public event ReadStartedHandler ReadStarted;

	public event SessionClosedHandler SessionClosed;

	public event RecordingContextSwitchesChangedHandler RecordingContextSwitchesChanged;

	public event FilenameChangedHandler FilenameChanged;

	public event ETLTraceFinishedHandler ETLTraceFinished;

	public event ShowContextSwitchWarningHandler ShowContextSwitchWarning;

	public event ReceivedCustomStatStringPacketHandler ReceivedCustomStatStringPacket;

	public event TimerNameAddedHandler TimerNameAdded;

	public event FinishedProcessingPacketsHandler FinishedProcessingPackets;

	public event ShowErrorHandler ShowError;

	public event ShowWarningHandler ShowWarning;

	public event RecordCallstacksChangedHandler RecordCallstacksChanged;

	public event ScopeColourChangedHandler ScopeColourChanged;

	public event CustomStatInfoChangedHandler CustomStatInfoChanged;

	public event CustomStatColourChangedHandler CustomStatColourChanged;

	public event MaxCoreCountChangedHandler CoreCountChanged;

	public Session(CoreSettings settings, ILog log)
	{
		m_Settings = settings;
		m_Log = log;
		SessionFilename = CreateSessionName();
		settings.SymbolPathsChanged += SettingsSymbolPathsChanged;
		settings.CallstackFiltersChanged += SettingsCallstackFiltersChanged;
		SetSymLibSymbolPaths();
		SetSimLibFilters();
		////if (!m_SymLib.Initialise())
		////{
		////	LogLine("Error: failed to initialise SymLib");
		////}
	}

	private void LogLine(string message)
	{
		m_Log.Write(message + "\n");
	}

	private void SettingsCallstackFiltersChanged()
	{
		SetSimLibFilters();
	}

	private void SetSimLibFilters()
	{
		////m_SymLib.SetFilters(m_Settings.CallstackFilters.ToArray());
	}

	private void SettingsSymbolPathsChanged()
	{
		SetSymLibSymbolPaths();
	}

	private void SetSymLibSymbolPaths()
	{
		////m_SymLib.SetSymbolPaths(m_Settings.SymbolPaths.ToArray());
	}

	private static string CreateSessionName()
	{
		DateTime date = DateTime.Now.Date;
		string text = date.Day.ToString("00") + "_" + date.Month.ToString("00") + "_" + date.Year.ToString("####");
		System.TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
		string text2 = timeOfDay.Hours.ToString("00") + "_" + timeOfDay.Minutes.ToString("00") + "_" + timeOfDay.Seconds.ToString("00");
		return text + "+" + text2 + ".framepro";
	}

	public void Dispose()
	{
		m_TcpCllient.Close();
		m_Disposing = true;
		if (m_ProcessEventsThread != null)
		{
			m_ProcessEventsThread.Join();
		}
		if (m_DeserialiseReceiveThread != null)
		{
			m_DeserialiseReceiveThread.Join();
		}
		if (m_ReceiveThread != null)
		{
			m_ReceiveThread.Join();
		}
		if (m_SendThread != null)
		{
			m_SendThread.Join();
		}
		m_SendEvent.Dispose();
		m_ReceiveThreadMainFinishedEvent.Dispose();
	}

	public void Close()
	{
		m_Settings.SymbolPathsChanged -= SettingsSymbolPathsChanged;
		m_Settings.CallstackFiltersChanged -= SettingsCallstackFiltersChanged;
		Disconnect(DisconnectReason.Requested);
		m_CancelProcessingPackets = true;
		if (this.SessionClosed != null)
		{
			this.SessionClosed(this);
		}
	}

	public long FrameXToTime(long frame_x)
	{
		int num = Math.Max(0, (int)(frame_x / 1000));
		long num2 = frame_x % 1000;
		if (num < FrameCount)
		{
			Frame frame = GetFrame(num);
			return frame.StartTime + num2 * frame.Duration / 1000;
		}
		return LastFrameEndTime + (num - FrameCount) * DefaultFrameTime + num2 * DefaultFrameTime / 1000;
	}

	public long TimeToFrameX(long time)
	{
		int frameIndex = GetFrameIndex(time);
		GetFrameStartEndTime(frameIndex, out var start_time, out var end_time);
		long num = end_time - start_time;
		long num2 = ((num != 0L) ? ((time - start_time) * FrameXPerFrame / num) : 0);
		return frameIndex * FrameXPerFrame + num2;
	}

	public Frame GetFrame(int index)
	{
		using (new ReadLockScope(m_FramesLock))
		{
			if (index >= 0 && index < m_Frames.Count)
			{
				return m_Frames[index];
			}
		}
		return null;
	}

	public void GetFrameStartEndTime(int index, out long start_time, out long end_time)
	{
		long num = 30 * m_TimerFrequency / 1000;
		int frameCount = FrameCount;
		if (index < 0)
		{
			start_time = FirstFrameTime + index * num;
			end_time = start_time + num;
			return;
		}
		if (index < frameCount)
		{
			using (new ReadLockScope(m_FramesLock))
			{
				Frame frame = m_Frames[index];
				start_time = frame.StartTime;
				end_time = frame.EndTime;
				return;
			}
		}
		start_time = LastFrameEndTime + (index - frameCount) * num;
		end_time = start_time + num;
	}

	private void ProcessEventsThreadMain(object mode_obj)
	{
		ProcessingPacketsMode processingPacketsMode = (ProcessingPacketsMode)mode_obj;
		m_ProcessingPackets = true;
		while ((m_Connected || ReceivedPacketCount != 0L) && !m_Disposing)
		{
			int tickCount = Environment.TickCount;
			bool flag = true;
			while (flag)
			{
				lock (m_ReceivedPackets)
				{
					m_ReceivedPackets.MoveTo(m_ProcessPacketList);
					Interlocked.Exchange(ref m_PacketsToProcessCount, m_ProcessPacketList.Count);
				}
				flag = m_ProcessPacketList.Count != 0;
				if (!flag)
				{
					continue;
				}
				int num = 0;
				foreach (ReceivedPacket processPacket in m_ProcessPacketList)
				{
					ProcessPacket(processPacket);
					num++;
					if (num == 100)
					{
						Interlocked.Add(ref m_PacketsToProcessCount, -num);
						num = 0;
						if (m_CancelProcessingPackets)
						{
							break;
						}
					}
				}
				m_ProcessPacketList.Clear();
				Interlocked.Exchange(ref m_PacketsToProcessCount, 0L);
			}
			int num2 = Environment.TickCount - tickCount;
			if (num2 < 30)
			{
				Thread.Sleep(30 - num2);
			}
		}
		m_ProcessPacketList.Clear();
		lock (m_ReceivedPackets)
		{
			m_ReceivedPackets.Clear();
		}
		m_ProcessingPackets = false;
		m_FinishedProcessingPacketsEvent.Set();
		if (this.FinishedProcessingPackets != null)
		{
			this.FinishedProcessingPackets();
		}
		if (processingPacketsMode == ProcessingPacketsMode.Recording)
		{
			LogLine("Read Recording Time: " + (Environment.TickCount - m_ConnectTime));
		}
	}

	private void ProcessPacket(ReceivedPacket packet)
	{
		_ = m_FrameBytesSentCount;
		m_PacketAllocator.CalledFree = false;
		switch (packet.m_PacketType)
		{
		case PacketType.Connect:
			HandleConnectPacket(packet.m_Packet as ConnectPacket);
			break;
		case PacketType.FrameStart:
			HandleFrameStartPacket(packet.m_Packet as FrameStartPacket);
			break;
		case PacketType.TimeSpan:
			HandleTimeSpanPacket(packet.m_Packet as TimeSpanPacket, StringLiteralType.NameAndSourceInfo, free_packet: true, -1);
			break;
		case PacketType.TimeSpanW:
			HandleTimeSpanPacket(packet.m_Packet as TimeSpanPacket, StringLiteralType.NameAndSourceInfoW, free_packet: true, -1);
			break;
		case PacketType.NamedTimeSpan:
			HandleNamedTimeSpanPacket(packet.m_Packet as NamedTimeSpanPacket, free_packet: true, -1);
			break;
		case PacketType.StringLiteralNamedTimeSpan:
			HandleStringLiteralNamedTimeSpanPacket(packet.m_Packet as NamedTimeSpanPacket, free_packet: true, -1);
			break;
		case PacketType.ThreadName:
			HandleThreadNamePacket(packet.m_Packet as ThreadNamePacket);
			break;
		case PacketType.ThreadOrder:
			HandleThreadOrderPacket(packet.m_Packet as ThreadOrderPacket);
			break;
		case PacketType.String:
			HandleStringPacket(packet.m_Packet as StringPacket);
			break;
		case PacketType.WString:
			HandleWStringPacket(packet.m_Packet as WStringPacket);
			break;
		case PacketType.NameAndSourceInfo:
			HandleNameAndSourceInfoPacket(packet.m_Packet as StringPacket);
			break;
		case PacketType.NameAndSourceInfoW:
			HandleNameAndSourceInfoPacket(packet.m_Packet as WStringPacket);
			break;
		case PacketType.SourceInfo:
			HandleFileAndLinePacket(packet.m_Packet as StringPacket);
			break;
		case PacketType.MainThread:
			HandleMainThreadPacket(packet.m_Packet as MainThreadPacket);
			break;
		case PacketType.SessionStatsPacket:
			HandleSessionStatsPacket(packet.m_Packet as SessionStatsPacket);
			break;
		case PacketType.legacy_SessionDetailsPacket:
			HandleSessionDetailsPacket(packet.m_Packet as SessionDetailsPacket);
			break;
		case PacketType.ContextSwitchPacket:
			HandleContextSwitchPacket(packet.m_Packet as ContextSwitchPacket);
			break;
		case PacketType.ContextSwitchRecordingStartedPacket:
			HandleContextSwitchRecordingStartedPacket(packet.m_Packet as ContextSwitchRecordingStartedPacket);
			break;
		case PacketType.ProcessNamePacket:
			HandleProcessNamePacket(packet.m_Packet as ProcessNamePacket);
			break;
		case PacketType.CustomStatPacket_Depreciated:
			HandleCustomStatPacket(packet.m_Packet as CustomStatPacket_Depreciated, StringLiteralType.GeneralString);
			break;
		case PacketType.StringLiteralTimerNamePacket:
			HandleStringLiteralTimerNamePacket(packet.m_Packet as StringPacket);
			break;
		case PacketType.HiResTimerScopePacket:
			HandleHiResTimerScopePacket(packet.m_Packet as HiResTimerScopePacket);
			break;
		case PacketType.LogPacket:
			HandleLogPacket((LogPacket)packet.m_Packet);
			break;
		case PacketType.EventPacket:
			HandleEventPacket((EventPacket)packet.m_Packet);
			break;
		case PacketType.StartWaitEventPacket:
			HandleWaitEventPacket((WaitEventPacket)packet.m_Packet, WaitEvent.WaitEventMode.Start);
			break;
		case PacketType.StopWaitEventPacket:
			HandleWaitEventPacket((WaitEventPacket)packet.m_Packet, WaitEvent.WaitEventMode.Stop);
			break;
		case PacketType.TriggerWaitEventPacket:
			HandleWaitEventPacket((WaitEventPacket)packet.m_Packet, WaitEvent.WaitEventMode.Trigger);
			break;
		case PacketType.TimeSpanCustomStatPacket_Depreciated:
			HandleTimeSpanCustomStatPacket(packet.m_Packet as TimeSpanCustomStatPacket_Depreciated, StringLiteralType.GeneralString);
			break;
		case PacketType.TimeSpanCustomStatPacketW:
			HandleTimeSpanCustomStatPacket(packet.m_Packet as TimeSpanCustomStatPacket, StringLiteralType.GeneralStringW);
			break;
		case PacketType.TimeSpanWithCallstack:
			HandleTimeSpanPacketWithCallstack(packet.m_Packet as TimeSpanPacketWithCallstack, StringLiteralType.NameAndSourceInfo);
			break;
		case PacketType.TimeSpanWWithCallstack:
			HandleTimeSpanPacketWithCallstack(packet.m_Packet as TimeSpanPacketWithCallstack, StringLiteralType.NameAndSourceInfoW);
			break;
		case PacketType.NamedTimeSpanWithCallstack:
			HandleNamedTimeSpanPacketWithCallstack(packet.m_Packet as NamedTimeSpanPacketWithCallstack);
			break;
		case PacketType.StringLiteralNamedTimeSpanWithCallstack:
			HandleStringLiteralNamedTimeSpanPacketWithCallstack(packet.m_Packet as NamedTimeSpanPacketWithCallstack);
			break;
		case PacketType.ModulePacket:
			HandleModulePacket(packet.m_Packet as ModulePacket);
			break;
		case PacketType.CustomStatPacket_Depreciated2:
			HandleCustomStatPacket_Depreciated2(packet.m_Packet as CustomStatPacket_Depreciated2, StringLiteralType.GeneralString);
			break;
		case PacketType.TimeSpanCustomStatPacket:
			HandleTimeSpanCustomStatPacket(packet.m_Packet as TimeSpanCustomStatPacket, StringLiteralType.GeneralString);
			break;
		case PacketType.CustomStatPacketW_Depreciated2:
			HandleCustomStatPacket_Depreciated2(packet.m_Packet as CustomStatPacket_Depreciated2, StringLiteralType.GeneralStringW);
			break;
		case PacketType.SetScopeColourPacket:
			HandleSetScopeColourPacket(packet.m_Packet as SetScopeColourPacket);
			break;
		case PacketType.SetCustomStatGraphPacket:
		case PacketType.SetCustomStatUnitPacket:
			HandleSetCustomStatInfoPacket(packet.m_Packet as SetCustomStatInfoPacket, packet.m_PacketType);
			break;
		case PacketType.SetCustomStatColourPacket:
			HandleSetCustomStatColourPacket(packet.m_Packet as SetCustomStatColourPacket);
			break;
		case PacketType.CallstackPacket:
			HandleCallstackPacket(packet.m_Packet as CallstackPacket);
			break;
		case PacketType.SessionInfoPacket:
			HandleSessionInfoPacket(packet.m_Packet as SessionInfoPacket);
			break;
		case PacketType.CustomStatPacket:
			HandleCustomStatPacket(packet.m_Packet as CustomStatPacket, StringLiteralType.GeneralString);
			break;
		case PacketType.CustomStatPacketW:
			HandleCustomStatPacket(packet.m_Packet as CustomStatPacket, StringLiteralType.GeneralStringW);
			break;
		case PacketType.RequestStringLiteral:
		case PacketType.ConditionalScopeMinTime:
		case PacketType.ConnectResponsePacket:
		case PacketType.RequestRecordedDataPacket:
		case PacketType.SetCallstackRecordingEnabledPacket:
			break;
		}
	}

	private void StartXBoxOneEtlTrace()
	{
		if (CoreUtils.RunbatchFile("StartXBoxOneEtlTrace.bat", m_IP))
		{
			m_StartedXBoxOneEtlTrace = true;
		}
		else if (this.ShowError != null)
		{
			this.ShowError("Failed to start context switch trace");
		}
	}

	private bool StopXBoxOneEtlTrace()
	{
		Thread.Sleep(1000);
		if (!CoreUtils.RunbatchFile("StopXBoxOneEtlTrace.bat", m_IP))
		{
			this.ShowError("Failed to copy context switch trace from XBoxOne");
			return false;
		}
		string text = Path.GetTempPath() + "FramePro\\FramePro.etl";
		if (File.Exists(text))
		{
			ReadXBoxOneEtlTrace(text);
			return true;
		}
		this.ShowError("ERROR: Failed to read etl file: " + text);
		return false;
	}

	private void ReadXBoxOneEtlTrace(string filename)
	{
		////global::ETLReader.ETLReader eTLReader = new global::ETLReader.ETLReader();
		////List<ETLReader.ContextSwitch> list = new List<ETLReader.ContextSwitch>();
		////eTLReader.Read(filename, list, 100L);
		////List<ContextSwitch> list2 = new List<ContextSwitch>(list.Count);
		////foreach (ETLReader.ContextSwitch item2 in list)
		////{
		////	ContextSwitch item = default(ContextSwitch);
		////	item.m_Timestamp = item2.m_Timestamp;
		////	item.m_ProcessId = item2.m_ProcessId;
		////	item.m_CPUId = item2.m_CPUId;
		////	item.m_OldThreadId = item2.m_OldThreadId;
		////	item.m_NewThreadId = item2.m_NewThreadId;
		////	item.m_OldThreadState = (ThreadState)item2.m_OldThreadState;
		////	item.m_OldThreadWaitReason = (ThreadWaitReason)item2.m_OldThreadWaitReason;
		////	list2.Add(item);
		////}
		////if (list2.Count != 0)
		////{
		////	foreach (ContextSwitch item3 in list2)
		////	{
		////		AddContextSwitch(item3);
		////	}
		////	RecordingContextSwitches = true;
		////}
		////else if (this.ShowWarning != null)
		////{
		////	this.ShowWarning("Warning: Failed to find any context switches in ETL trace file.\nPlease check your log file for errors.");
		////}
	}

	private void HandleConnectPacket(ConnectPacket packet)
	{
		if (packet.m_Platform == Platform.XBoxOne && m_StartRecordingContextSwitches)
		{
			StartXBoxOneEtlTrace();
		}
		m_FrameBytesSentCount += packet.GetSize();
		m_Platform = packet.m_Platform;
		if (!IsValidFrameProLibVersion(17, packet.m_FrameProLibVersion))
		{
			Disconnect(DisconnectReason.BadVersion);
		}
		m_TimerFrequency = packet.m_TimerFrequency * 100;
		m_RemoteProcessId = packet.m_ProcessId;
		using (new WriteLockScope(m_FramesLock))
		{
			m_Frames.SetBlockDuration(m_TimerFrequency);
		}
		SendConditionalScopeMinTime();
		bool record_context_switches = m_StartRecordingContextSwitches && packet.m_Platform != Platform.XBoxOne;
		Send(new ConnectResponsePacket(m_Interactive, record_context_switches));
		m_ReceivedConnectPacket = true;
		Connection currentConnection = m_Settings.GetCurrentConnection();
		RecordCallstacks = currentConnection.RecordCallstacks;
		m_PacketAllocator.Free(packet);
	}

	private bool IsValidFrameProLibVersion(int local_version, int remote_version)
	{
		if (remote_version >= 14)
		{
			return local_version >= remote_version;
		}
		if (remote_version >= 11 && local_version >= 12)
		{
			return true;
		}
		return local_version == remote_version;
	}

	private void SetIsReady()
	{
		UpdateTargetFrameTime();
		m_IsReady = true;
		if (this.SessionIsReady != null)
		{
			this.SessionIsReady();
		}
	}

	private void HandleFrameStartPacket(FrameStartPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		bool flag = FirstFrameTime == 0;
		if (flag)
		{
			long num = packet.m_FrameStartTime * 100;
			SetFirstFrameTime(num);
			SetLastFrameEndtime(num);
			m_AverageFrameTimeStart = num;
		}
		if (m_CurrentFrame == null || packet.m_FrameStartTime * 100 >= m_CurrentFrame.StartTime)
		{
			if (m_CurrentFrame != null)
			{
				m_CurrentFrame.Finalise(packet.m_FrameStartTime * 100, m_FrameTimeSpanCount, m_FrameBytesSentCount, 0L, packet.m_PrevFrameSendTime * 100);
				using (new WriteLockScope(m_FramesLock))
				{
					m_Frames.Add(m_CurrentFrame);
				}
				lock (m_AverageLock)
				{
					m_AverageFrameTimeCount++;
				}
				if (m_CurrentFrame.Duration <= m_TargetFrameTime)
				{
					m_FramesInBudget++;
				}
				lock (m_MaxFrameTimeLock)
				{
					if (m_CurrentFrame.Duration > m_MaxFrameTime)
					{
						m_MaxFrameTime = m_CurrentFrame.Duration;
						m_MaxFrameIndex = m_Frames.Count - 1;
					}
				}
				m_TotalTimeSpanCount += m_FrameTimeSpanCount;
				m_FrameTimeSpanCount = 0;
				m_FrameBytesSentCount = 0;
				AssignUnassignedTimeSpans();
				AssignCustomStatPacketsToFrames();
				AddPendingCustomStatIntervalValues(m_CurrentFrame.StartTime, m_CurrentFrame.EndTime);
				SetLastFrameEndtime(m_CurrentFrame.EndTime);
			}
			Frame frame = new Frame(FrameCount, packet.m_FrameStartTime * 100);
			if (m_CurrentFrame != null)
			{
				frame.AddAccumulatedCustomStats(m_CurrentFrame);
			}
			m_CurrentFrame = frame;
		}
		else
		{
			Log.WriteLine("Error: received frame with timestamp older than the previous frame");
		}
		m_PacketAllocator.Free(packet);
		if (flag)
		{
			SetIsReady();
		}
	}

	private void AddCustomStatToTotal(CustomStat custom_stat)
	{
		bool flag = false;
		using (new WriteLockScope(m_CustomStatSessionInfoLock))
		{
			if (m_CustomStatSessionInfo.TryGetValue(custom_stat.Name, out var value))
			{
				value.m_TotalValueInt64 += custom_stat.ValueInt64;
				value.m_TotalValueDouble += custom_stat.ValueDouble;
				value.m_TotalCount += custom_stat.Count;
				if (custom_stat.ValueInt64 < value.m_MinValuePerFrameInt64)
				{
					value.m_MinValuePerFrameInt64 = custom_stat.ValueInt64;
				}
				if (custom_stat.ValueInt64 > value.m_MaxValuePerFrameInt64)
				{
					value.m_MaxValuePerFrameInt64 = custom_stat.ValueInt64;
				}
				if (custom_stat.ValueDouble < value.m_MinValuePerFrameDouble)
				{
					value.m_MinValuePerFrameDouble = custom_stat.ValueDouble;
				}
				if (custom_stat.ValueDouble > value.m_MaxValuePerFrameDouble)
				{
					value.m_MaxValuePerFrameDouble = custom_stat.ValueDouble;
				}
				if (custom_stat.Count < value.m_MinCountPerFrame)
				{
					value.m_MinCountPerFrame = custom_stat.Count;
				}
				if (custom_stat.Count > value.m_MaxCountPerFrame)
				{
					value.m_MaxCountPerFrame = custom_stat.Count;
				}
				value.m_AccTotalValueInt64 += custom_stat.AccValueInt64;
				value.m_AccTotalValueDouble += custom_stat.AccValueDouble;
				if (custom_stat.AccValueInt64 < value.m_AccMinValuePerFrameInt64)
				{
					value.m_AccMinValuePerFrameInt64 = custom_stat.AccValueInt64;
				}
				if (custom_stat.AccValueInt64 > value.m_AccMaxValuePerFrameInt64)
				{
					value.m_AccMaxValuePerFrameInt64 = custom_stat.AccValueInt64;
				}
				if (custom_stat.AccValueDouble < value.m_AccMinValuePerFrameDouble)
				{
					value.m_AccMinValuePerFrameDouble = custom_stat.AccValueDouble;
				}
				if (custom_stat.AccValueDouble > value.m_AccMaxValuePerFrameDouble)
				{
					value.m_AccMaxValuePerFrameDouble = custom_stat.AccValueDouble;
				}
			}
			else
			{
				CustomStatValueType customstatValueType = GetCustomstatValueType(custom_stat.Name);
				value = new CustomStatSessionData(custom_stat.Name, customstatValueType, m_Frames.Count);
				value.m_TotalValueInt64 = custom_stat.ValueInt64;
				value.m_TotalValueDouble = custom_stat.ValueDouble;
				value.m_TotalCount = custom_stat.Count;
				value.m_MinValuePerFrameInt64 = custom_stat.ValueInt64;
				value.m_MaxValuePerFrameInt64 = custom_stat.ValueInt64;
				value.m_MinValuePerFrameDouble = custom_stat.ValueDouble;
				value.m_MaxValuePerFrameDouble = custom_stat.ValueDouble;
				value.m_MinCountPerFrame = custom_stat.Count;
				value.m_MaxCountPerFrame = custom_stat.Count;
				value.m_AccTotalValueInt64 = custom_stat.AccValueInt64;
				value.m_AccTotalValueDouble = custom_stat.AccValueDouble;
				value.m_AccMinValuePerFrameInt64 = custom_stat.AccValueInt64;
				value.m_AccMaxValuePerFrameInt64 = custom_stat.AccValueInt64;
				value.m_AccMinValuePerFrameDouble = custom_stat.AccValueDouble;
				value.m_AccMaxValuePerFrameDouble = custom_stat.AccValueDouble;
				m_CustomStatSessionInfo[custom_stat.Name] = value;
				flag = true;
			}
		}
		if (flag && this.ReceivedCustomStatStringPacket != null)
		{
			this.ReceivedCustomStatStringPacket();
		}
	}

	private TimeSpanList GetTimeSpanList(int thread_id)
	{
		bool flag = false;
		using (new ReadLockScope(m_ThreadsLock))
		{
			if (!m_Threads.ContainsKey(thread_id))
			{
				flag = true;
			}
		}
		if (flag)
		{
			using (new WriteLockScope(m_ThreadsLock))
			{
				m_Threads[thread_id] = new ThreadInfo("thread" + thread_id);
			}
		}
		TimeSpanList value;
		using (new ReadLockScope(m_TimeSpansLock))
		{
			if (m_TimeSpans.TryGetValue(thread_id, out value))
			{
				return value;
			}
		}
		value = new TimeSpanList(m_TimerFrequency, thread_id);
		using (new WriteLockScope(m_TimeSpansLock))
		{
			m_TimeSpans[thread_id] = value;
		}
		OnThreadAdded();
		return value;
	}

	private void GetCustomTimeSpanStat(int thread_id, long start_time, long end_time, Array<TimeSpanCustomStat> time_span_custom_stat_array)
	{
		Array<TimeSpanCustomStatWithTime> value = null;
		if (!m_TimeSpanCustomStatArrays.TryGetValue(thread_id, out value))
		{
			return;
		}
		int num = value.Count;
		for (int i = 0; i < num; i++)
		{
			TimeSpanCustomStatWithTime timeSpanCustomStatWithTime = value[i];
			long time = timeSpanCustomStatWithTime.m_Time;
			if (time >= start_time && time < end_time)
			{
				time_span_custom_stat_array.Add(timeSpanCustomStatWithTime.m_CustomStat);
				num--;
				value[i] = value[num];
				i--;
				value.RemoveLast();
			}
		}
	}

	private TimeSpan CreateTimeSpan(int thread_id, int time_span_info_id, long start_time, long end_time)
	{
		GetCustomTimeSpanStat(thread_id, start_time, end_time, m_TimeSpanCustomStatArrayTemp);
		TimeSpan result;
		if (m_TimeSpanCustomStatArrayTemp.Count != 0)
		{
			result = new TimeSpanEx(time_span_info_id, start_time, end_time, m_TimeSpanCustomStatArrayTemp);
			m_TimeSpanCustomStatArrayTemp.Clear();
		}
		else
		{
			result = new TimeSpan(time_span_info_id, start_time, end_time);
		}
		return result;
	}

	private void HandleTimeSpanPacket(TimeSpanPacket packet, StringLiteralType string_literal_type, bool free_packet, int callstack_id)
	{
		m_FrameBytesSentCount += packet.GetSize();
		m_FrameTimeSpanCount++;
		TimeSpanList timeSpanList = GetTimeSpanList(packet.m_ThreadID);
		long nameAndSourceInfo = packet.m_NameAndSourceInfo;
		HandleNewStringId(nameAndSourceInfo, string_literal_type);
		int timeSpanInfoId = GetTimeSpanInfoId(nameAndSourceInfo, nameAndSourceInfo, packet.m_Core, callstack_id);
		TimeSpan time_span = CreateTimeSpan(packet.m_ThreadID, timeSpanInfoId, packet.m_StartTime * 100, packet.m_EndTime * 100);
		UpdateCoreCount(packet.m_Core);
		using (new WriteLockScope(timeSpanList.Lock))
		{
			timeSpanList.Add(time_span);
		}
		AddTimeSpanToFrameStats(time_span, packet.m_ThreadID);
		if (free_packet)
		{
			m_PacketAllocator.Free(packet);
		}
	}

	private void HandleNamedTimeSpanPacket(NamedTimeSpanPacket packet, bool free_packet, int callstack_id)
	{
		m_FrameTimeSpanCount++;
		m_FrameBytesSentCount += packet.GetSize();
		TimeSpanList timeSpanList = GetTimeSpanList(packet.m_ThreadID);
		long num = RemapStringId(packet.m_Name);
		long sourceInfo = packet.m_SourceInfo;
		HandleNewStringId(sourceInfo, StringLiteralType.SourceInfo);
		if (num != -1 && packet.m_StartTime <= packet.m_EndTime)
		{
			int timeSpanInfoId = GetTimeSpanInfoId(num, sourceInfo, packet.m_Core, callstack_id);
			TimeSpan time_span = CreateTimeSpan(packet.m_ThreadID, timeSpanInfoId, packet.m_StartTime * 100, packet.m_EndTime * 100);
			UpdateCoreCount(packet.m_Core);
			bool flag = AddTimeSpanName(num);
			using (new WriteLockScope(timeSpanList.Lock))
			{
				timeSpanList.Add(time_span);
			}
			AddTimeSpanToFrameStats(time_span, packet.m_ThreadID);
			if (flag && this.TimerNameAdded != null)
			{
				this.TimerNameAdded(num, GetString(num));
			}
		}
		if (free_packet)
		{
			m_PacketAllocator.Free(packet);
		}
	}

	private void HandleStringLiteralNamedTimeSpanPacket(NamedTimeSpanPacket packet, bool free_packet, int callstack_id)
	{
		m_FrameBytesSentCount += packet.GetSize();
		if (packet.m_StartTime <= packet.m_EndTime)
		{
			m_FrameTimeSpanCount++;
			TimeSpanList timeSpanList = GetTimeSpanList(packet.m_ThreadID);
			long name = RemapAndHandleNewStringId(packet.m_Name, StringLiteralType.StringLiteralTimerName);
			HandleNewStringId(packet.m_SourceInfo, StringLiteralType.SourceInfo);
			long sourceInfo = packet.m_SourceInfo;
			int timeSpanInfoId = GetTimeSpanInfoId(name, sourceInfo, packet.m_Core, callstack_id);
			TimeSpan time_span = CreateTimeSpan(packet.m_ThreadID, timeSpanInfoId, packet.m_StartTime * 100, packet.m_EndTime * 100);
			UpdateCoreCount(packet.m_Core);
			AddTimeSpanName(name);
			using (new WriteLockScope(timeSpanList.Lock))
			{
				timeSpanList.Add(time_span);
			}
			AddTimeSpanToFrameStats(time_span, packet.m_ThreadID);
		}
		if (free_packet)
		{
			m_PacketAllocator.Free(packet);
		}
	}

	private void HandleThreadNamePacket(ThreadNamePacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		string threadName = GetThreadName(packet.m_ThreadId);
		string @string = GetString(packet.m_Name);
		using (new WriteLockScope(m_ThreadsLock))
		{
			if (m_Threads.TryGetValue(packet.m_ThreadId, out var value))
			{
				value.Name = @string;
			}
			else
			{
				m_Threads[packet.m_ThreadId] = new ThreadInfo(@string);
			}
		}
		if (this.ThreadNameChanged != null)
		{
			this.ThreadNameChanged(threadName, @string);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleThreadOrderPacket(ThreadOrderPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		bool flag = false;
		using (new WriteLockScope(m_ThreadOrderLock))
		{
			string @string = GetString(packet.m_ThreadNameId);
			if (!m_ThreadOrder.Contains(@string))
			{
				m_ThreadOrder.Add(@string);
				flag = true;
			}
		}
		if (flag && this.ThreadOrderChanged != null)
		{
			this.ThreadOrderChanged();
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleStringPacket(StringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		HandleStringPacket(packet.m_StringId, packet.m_String);
		m_PacketAllocator.Free(packet);
	}

	private void HandleWStringPacket(WStringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		HandleStringPacket(packet.m_StringId, packet.m_String);
		m_PacketAllocator.Free(packet);
	}

	private void HandleStringPacket(long string_id, string string_value)
	{
		long value = -1L;
		bool flag = false;
		bool flag2 = false;
		using (new WriteLockScope(m_StringsLock))
		{
			if (m_StringIds.TryGetValue(string_value, out value))
			{
				m_StringIdRemappings[string_id] = value;
				flag = true;
			}
			else
			{
				m_StringIdRemappings[string_id] = string_id;
				m_StringIds[string_value] = string_id;
			}
			flag2 = m_Strings.ContainsKey(string_id);
			m_Strings[string_id] = string_value;
		}
		if (flag2)
		{
			OnPendingStringResolved(string_id);
		}
		if (flag)
		{
			OnStringRemapped(string_id, value);
		}
	}

	private void OnPendingStringResolved(long string_id)
	{
		if (this.ScopeColourChanged != null)
		{
			this.ScopeColourChanged();
		}
		if (this.CustomStatColourChanged != null)
		{
			this.CustomStatColourChanged();
		}
	}

	public long GetStringId(string name)
	{
		using (new ReadLockScope(m_StringsLock))
		{
			long value = -1L;
			m_StringIds.TryGetValue(name, out value);
			return value;
		}
	}

	public long RemapStringId(long string_id)
	{
		if (!m_StringIdRemappings.TryGetValue(string_id, out var value))
		{
			return string_id;
		}
		return value;
	}

	private void HandleNameAndSourceInfoPacket(StringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		HandleNameAndSourceInfoPacket(packet.m_StringId, packet.m_String);
		m_PacketAllocator.Free(packet);
	}

	private void HandleNameAndSourceInfoPacket(WStringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		HandleNameAndSourceInfoPacket(packet.m_StringId, packet.m_String);
		m_PacketAllocator.Free(packet);
	}

	private void HandleNameAndSourceInfoPacket(long string_id, string string_value)
	{
		try
		{
			string[] array = string_value.Split('|');
			if (array.Length >= 5)
			{
				string name = array[0];
				string filename = array[1];
				string function = array[2];
				string value = array[3];
				string time_span_type_str = array[4];
				int line = Convert.ToInt32(value);
				name = CoreUtils.StripOffFunctionTypes(name);
				HandleStringPacket(string_id, name);
				AddTimeSpanName(string_id);
				TimeSpanType time_span_type = ToTimeSpanType(time_span_type_str);
				using (new WriteLockScope(m_SourceInfosLock))
				{
					m_SourceInfos[string_id] = new SourceInfo(filename, function, line, time_span_type);
				}
				if (this.TimerNameAdded != null)
				{
					this.TimerNameAdded(string_id, name);
				}
			}
		}
		catch (Exception)
		{
			if (!m_AssertedOnSourceString)
			{
				m_AssertedOnSourceString = true;
			}
		}
	}

	private void HandleFileAndLinePacket(StringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		try
		{
			string[] array = packet.m_String.Split('|');
			if (array.Length >= 4)
			{
				string filename = array[0];
				string function = array[1];
				string value = array[2];
				string time_span_type_str = array[3];
				int line = Convert.ToInt32(value);
				TimeSpanType time_span_type = ToTimeSpanType(time_span_type_str);
				using (new WriteLockScope(m_SourceInfosLock))
				{
					m_SourceInfos[packet.m_StringId] = new SourceInfo(filename, function, line, time_span_type);
				}
			}
		}
		catch (Exception)
		{
			if (!m_AssertedOnSourceString)
			{
				m_AssertedOnSourceString = true;
			}
		}
		m_PacketAllocator.Free(packet);
	}

	private static TimeSpanType ToTimeSpanType(string time_span_type_str)
	{
		TimeSpanType result = TimeSpanType.Working;
		if (time_span_type_str == "Idle")
		{
			result = TimeSpanType.Idle;
		}
		return result;
	}

	private void HandleMainThreadPacket(MainThreadPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		m_MainThreadId = packet.m_ThreadId;
		m_PacketAllocator.Free(packet);
	}

	private void HandleSessionStatsPacket(SessionStatsPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		m_SendBufferSize = packet.m_SendBufferSize;
		m_StringMemorySize = packet.m_StringMemorySize;
		m_MiscMemorySize = packet.m_MiscMemorySize;
		m_RecordingFileSize = packet.m_RecordingFileSize;
		m_PacketAllocator.Free(packet);
	}

	private void HandleSessionDetailsPacket(SessionDetailsPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		m_SessionDetails.m_Name = GetString(packet.m_Name);
		m_SessionDetails.m_BuildId = GetString(packet.m_BuildId);
		m_SessionDetails.m_Date = GetString(packet.m_Date);
		m_PacketAllocator.Free(packet);
	}

	private void HandleContextSwitchPacket(ContextSwitchPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		ContextSwitch context_switch = default(ContextSwitch);
		context_switch.m_Timestamp = packet.m_Timestamp * 100;
		context_switch.m_ProcessId = packet.m_ProcessId;
		context_switch.m_CPUId = packet.m_Core;
		context_switch.m_OldThreadId = packet.m_OldThreadId;
		context_switch.m_NewThreadId = packet.m_NewThreadId;
		context_switch.m_OldThreadState = packet.m_OldThreadState;
		context_switch.m_OldThreadWaitReason = packet.m_OldThreadWaitReason;
		UpdateCoreCount(packet.m_Core);
		AddContextSwitch(context_switch);
		if (m_IsLocalIP && packet.m_ProcessId != -1)
		{
			bool flag = false;
			using (new ReadLockScope(m_ProcessNamesLock))
			{
				flag = m_ProcessNames.ContainsKey(packet.m_ProcessId);
			}
			if (!flag)
			{
				bool flag2 = false;
				using (new ReadLockScope(m_LocalProcessNamesLock))
				{
					flag2 = m_LocalProcessNames.ContainsKey(packet.m_ProcessId);
				}
				if (!flag2)
				{
					string processName = CoreUtils.GetProcessName(packet.m_ProcessId);
					using (new WriteLockScope(m_LocalProcessNamesLock))
					{
						m_LocalProcessNames[packet.m_ProcessId] = processName;
					}
				}
			}
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleContextSwitchRecordingStartedPacket(ContextSwitchRecordingStartedPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		m_RecordingContextSwitchesStartedSucessfully = packet.m_StartedSucessfully;
		if (m_RecordingContextSwitchesStartedSucessfully)
		{
			RecordingContextSwitches = true;
		}
		else if (this.ShowContextSwitchWarning != null)
		{
			this.ShowContextSwitchWarning(packet.m_Error);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleProcessNamePacket(ProcessNamePacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long num = RemapStringId(packet.m_Name);
		using (new WriteLockScope(m_ProcessNamesLock))
		{
			m_ProcessNames[packet.m_ProcessId] = num;
		}
		if (packet.m_ProcessId == m_RemoteProcessId && m_SessionDetails.m_Name == SessionDetails.DefaultName)
		{
			m_SessionDetails.m_Name = GetString(num);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleCustomStatPacket(CustomStatPacket_Depreciated packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long num = RemapStringId(packet.m_Name);
		long graph = packet.m_Graph;
		long unit = packet.m_Unit;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		using (new ReadLockScope(m_StringsLock))
		{
			if (!m_Strings.ContainsKey(num))
			{
				flag = true;
			}
			if (!m_Strings.ContainsKey(graph))
			{
				flag2 = true;
			}
			if (!m_Strings.ContainsKey(unit))
			{
				flag3 = true;
			}
		}
		if (flag || flag2 || flag3)
		{
			using (new WriteLockScope(m_StringsLock))
			{
				if (flag)
				{
					m_Strings[num] = "pending name " + num;
				}
				if (flag3)
				{
					m_Strings[unit] = "pending unit " + unit;
				}
				if (flag2)
				{
					m_Strings[graph] = "pending graph " + graph;
				}
			}
			if (flag)
			{
				RequestStringLiteralValue(num, string_literal_type);
			}
			if (flag2)
			{
				RequestStringLiteralValue(graph, string_literal_type);
			}
			if (flag3)
			{
				RequestStringLiteralValue(unit, string_literal_type);
			}
		}
		if (m_CurrentFrame != null)
		{
			m_CurrentFrame.AddCustomStat(num, packet.m_ValueInt64, packet.m_ValueDouble, packet.m_Count);
			LegacySetGraphUnit(num, graph, unit);
			double num2 = packet.m_ValueType switch
			{
				CustomStatValueType.Int64 => packet.m_ValueInt64, 
				CustomStatValueType.Double => packet.m_ValueDouble, 
				_ => 0.0, 
			};
			if (!m_PendingCustomStatIntervalValues.TryGetValue(num, out var value))
			{
				value = new PendingCustomStatValue();
				m_PendingCustomStatIntervalValues[num] = value;
			}
			value.m_Value += num2;
			value.m_Count += packet.m_Count;
		}
		lock (m_CustomStatValueTypes)
		{
			if (!m_CustomStatValueTypes.ContainsKey(num))
			{
				m_CustomStatValueTypes[num] = packet.m_ValueType;
			}
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleCustomStatPacket_Depreciated2(CustomStatPacket_Depreciated2 packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		CustomStatPacket customStatPacket = m_PacketAllocator.Alloc<CustomStatPacket>();
		customStatPacket.m_Count = packet.m_Count;
		customStatPacket.m_Name = RemapAndHandleNewStringId(packet.m_Name, string_literal_type);
		customStatPacket.m_ValueInt64 = packet.m_ValueInt64;
		customStatPacket.m_ValueDouble = packet.m_ValueDouble;
		customStatPacket.m_ValueType = packet.m_ValueType;
		customStatPacket.m_Time = m_CurrentFrame.StartTime / 100;
		m_PendingCustomStatPackets.Add(customStatPacket);
		m_PacketAllocator.Free(packet);
	}

	private void HandleCustomStatPacket(CustomStatPacket packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		packet.m_Name = RemapAndHandleNewStringId(packet.m_Name, string_literal_type);
		m_PendingCustomStatPackets.Add(packet);
	}

	private int GetFrameIndexUsingScanBack(long time)
	{
		using (new ReadLockScope(m_FramesLock))
		{
			int count = m_Frames.Count;
			if (count == 0 || time > m_Frames[count - 1].EndTime)
			{
				return -1;
			}
			int num = Math.Max(0, count - 1 - 20);
			for (int num2 = count - 1; num2 >= num; num2--)
			{
				if (time >= m_Frames[num2].StartTime)
				{
					return num2;
				}
			}
		}
		return GetFrameIndex(time);
	}

	private void AssignCustomStatPacketsToFrames()
	{
		List<CustomStatPacket> list = new List<CustomStatPacket>();
		foreach (CustomStatPacket pendingCustomStatPacket in m_PendingCustomStatPackets)
		{
			int frameIndexUsingScanBack = GetFrameIndexUsingScanBack(pendingCustomStatPacket.m_Time * 100);
			if (frameIndexUsingScanBack >= 0)
			{
				lock (m_CustomStatValueTypes)
				{
					if (!m_CustomStatValueTypes.ContainsKey(pendingCustomStatPacket.m_Name))
					{
						m_CustomStatValueTypes[pendingCustomStatPacket.m_Name] = pendingCustomStatPacket.m_ValueType;
					}
				}
				using (new ReadLockScope(m_FramesLock))
				{
					m_Frames[frameIndexUsingScanBack].AddCustomStat(pendingCustomStatPacket.m_Name, pendingCustomStatPacket.m_ValueInt64, pendingCustomStatPacket.m_ValueDouble, pendingCustomStatPacket.m_Count);
					int count = m_Frames.Count;
					for (int i = frameIndexUsingScanBack + 1; i < count; i++)
					{
						m_Frames[i].AddAccumulatedCustomStats(pendingCustomStatPacket.m_Name, pendingCustomStatPacket.m_ValueInt64, pendingCustomStatPacket.m_ValueDouble);
					}
				}
				AddCustomStatToTotal(new CustomStat(pendingCustomStatPacket.m_Name, pendingCustomStatPacket.m_ValueInt64, pendingCustomStatPacket.m_ValueDouble, pendingCustomStatPacket.m_Count));
				double num = pendingCustomStatPacket.m_ValueType switch
				{
					CustomStatValueType.Int64 => pendingCustomStatPacket.m_ValueInt64, 
					CustomStatValueType.Double => pendingCustomStatPacket.m_ValueDouble, 
					_ => 0.0, 
				};
				if (!m_PendingCustomStatIntervalValues.TryGetValue(pendingCustomStatPacket.m_Name, out var value))
				{
					value = new PendingCustomStatValue();
					m_PendingCustomStatIntervalValues[pendingCustomStatPacket.m_Name] = value;
				}
				value.m_Value += num;
				value.m_Count += pendingCustomStatPacket.m_Count;
				m_PacketAllocator.Free(pendingCustomStatPacket);
			}
			else
			{
				list.Add(pendingCustomStatPacket);
			}
		}
		m_PendingCustomStatPackets.Clear();
		m_PendingCustomStatPackets.AddRange(list);
	}

	private void AddPendingCustomStatIntervalValues(long frame_start_time, long frame_end_time)
	{
		foreach (long key in m_PendingCustomStatIntervalValues.Keys)
		{
			PendingCustomStatValue pendingCustomStatValue = m_PendingCustomStatIntervalValues[key];
			CustomStatValuePerSecArray orCreateCustomStatValuePerSecArray = GetOrCreateCustomStatValuePerSecArray(key);
			lock (orCreateCustomStatValuePerSecArray.m_ValuePerSecArray)
			{
				orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.AddFrameValue(pendingCustomStatValue.m_Value, pendingCustomStatValue.m_Count, frame_start_time, frame_end_time, m_TimerFrequency);
			}
		}
		m_PendingCustomStatIntervalValues.Clear();
	}

	private CustomStatValuePerSecArray GetOrCreateCustomStatValuePerSecArray(long name)
	{
		CustomStatValuePerSecArray value;
		lock (m_CustomStatValuePerSecArrays)
		{
			if (!m_CustomStatValuePerSecArrays.TryGetValue(name, out value))
			{
				value = new CustomStatValuePerSecArray();
				m_CustomStatValuePerSecArrays[name] = value;
			}
		}
		return value;
	}

	public void GetCustomStatMinMaxValuesPerSec(long custom_stat_name, out double min_value_per_sec, out double max_value_per_sec, out double min_count_per_sec, out double max_count_per_sec)
	{
		CustomStatValuePerSecArray orCreateCustomStatValuePerSecArray = GetOrCreateCustomStatValuePerSecArray(custom_stat_name);
		min_value_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MinValuePerSec;
		max_value_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MaxValuePerSec;
		min_count_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MinCountPerSec;
		max_count_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MaxCountPerSec;
	}

	public void GetCustomStatMinMaxAccValuesPerSec(long custom_stat_name, out double min_value_per_sec, out double max_value_per_sec, out double min_count_per_sec, out double max_count_per_sec)
	{
		CustomStatValuePerSecArray orCreateCustomStatValuePerSecArray = GetOrCreateCustomStatValuePerSecArray(custom_stat_name);
		min_value_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.AccMinValuePerSec;
		max_value_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.AccMaxValuePerSec;
		min_count_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MinCountPerSec;
		max_count_per_sec = orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.MaxCountPerSec;
	}

	public void GetCustomStatsPerSec(long name, long start_time, long end_time, bool acc, List<PerSecValue> values, out long first_interval_time)
	{
		CustomStatValuePerSecArray orCreateCustomStatValuePerSecArray = GetOrCreateCustomStatValuePerSecArray(name);
		lock (orCreateCustomStatValuePerSecArray.m_ValuePerSecArray)
		{
			orCreateCustomStatValuePerSecArray.m_ValuePerSecArray.GetValues(start_time, end_time, m_TimerFrequency, acc, values, out first_interval_time);
		}
	}

	private void HandleStringLiteralTimerNamePacket(StringPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		string @string = packet.m_String;
		@string = CoreUtils.StripOffFunctionTypes(@string);
		lock (m_StringLiteralNamedTimeSpanNames)
		{
			m_StringLiteralNamedTimeSpanNames[packet.m_StringId] = packet.m_String;
		}
		HandleStringPacket(packet.m_StringId, @string);
		if (this.TimerNameAdded != null)
		{
			this.TimerNameAdded(packet.m_StringId, @string);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleHiResTimerScopePacket(HiResTimerScopePacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		TimeSpanList timeSpanList = GetTimeSpanList(packet.m_ThreadId);
		List<HiResTimer> list = new List<HiResTimer>();
		foreach (HiResTimer timer in packet.m_Timers)
		{
			HiResTimer item = default(HiResTimer);
			item.m_Name = RemapStringId(timer.m_Name);
			item.m_Duration = timer.m_Duration;
			item.m_Count = timer.m_Count;
			list.Add(item);
		}
		TimeSpan time_span = new TimeSpanEx(packet.m_StartTime * 100, packet.m_EndTime * 100, list);
		List<long> list2 = null;
		List<long> list3 = null;
		using (new ReadLockScope(m_StringsLock))
		{
			using (new ReadLockScope(m_TimeSpanNamesLock))
			{
				foreach (HiResTimer item2 in list)
				{
					long name = item2.m_Name;
					if (!m_Strings.ContainsKey(name))
					{
						if (list2 == null)
						{
							list2 = new List<long>();
						}
						list2.Add(name);
					}
					if (!m_TimeSpanNames.Contains(name))
					{
						if (list3 == null)
						{
							list3 = new List<long>();
						}
						list3.Add(name);
					}
				}
			}
		}
		if (list2 != null)
		{
			using (new WriteLockScope(m_StringsLock))
			{
				foreach (long item3 in list2)
				{
					m_Strings[item3] = "pending hires time span " + item3;
					RequestStringLiteralValue(item3, StringLiteralType.StringLiteralTimerName);
				}
			}
		}
		if (list3 != null)
		{
			foreach (long item4 in list3)
			{
				AddTimeSpanName(item4);
			}
		}
		using (new WriteLockScope(timeSpanList.Lock))
		{
			timeSpanList.Add(time_span);
		}
		AddTimeSpanToFrameStats(time_span, packet.m_ThreadId);
		m_PacketAllocator.Free(packet);
	}

	private void HandleLogPacket(LogPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		LogViewLog(packet.m_Time * 100, packet.m_String);
		m_PacketAllocator.Free(packet);
	}

	private void LogViewLog(long time, string message)
	{
		LogMessage item = new LogMessage(time, message);
		lock (m_LogMessages)
		{
			m_LogMessages.Add(item);
		}
	}

	private void HandleEventPacket(EventPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		Event item = new Event(RemapAndHandleNewStringId(packet.m_Name, StringLiteralType.GeneralString), packet.m_Time * 100, packet.m_Colour);
		lock (m_Events)
		{
			m_Events.Add(item);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleWaitEventPacket(WaitEventPacket packet, WaitEvent.WaitEventMode mode)
	{
		m_FrameBytesSentCount += packet.GetSize();
		WaitEvent item = new WaitEvent(packet.m_EventId, packet.m_Time * 100, packet.m_Thread, packet.m_Core, mode);
		TimeArray<WaitEvent> waitEvents = GetWaitEvents(packet.m_Thread);
		lock (waitEvents)
		{
			waitEvents.Add(item);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleTimeSpanCustomStatPacket(TimeSpanCustomStatPacket_Depreciated packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long num = RemapStringId(packet.m_Name);
		long num2 = RemapStringId(packet.m_Unit);
		bool flag = false;
		bool flag2 = false;
		using (new ReadLockScope(m_StringsLock))
		{
			if (!m_Strings.ContainsKey(num))
			{
				flag = true;
			}
			if (!m_Strings.ContainsKey(num2))
			{
				flag2 = true;
			}
		}
		if (flag || flag2)
		{
			using (new WriteLockScope(m_StringsLock))
			{
				if (flag)
				{
					m_Strings[num] = "pending name " + num;
				}
				if (flag2)
				{
					m_Strings[num2] = "pending unit " + num2;
				}
			}
			if (flag)
			{
				RequestStringLiteralValue(num, string_literal_type);
			}
			if (flag2)
			{
				RequestStringLiteralValue(num2, string_literal_type);
			}
		}
		TimeSpanCustomStatWithTime timeSpanCustomStatWithTime = new TimeSpanCustomStatWithTime();
		timeSpanCustomStatWithTime.m_CustomStat = new TimeSpanCustomStat();
		timeSpanCustomStatWithTime.m_CustomStat.m_Name = num;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueType = packet.m_ValueType;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueInt64 = packet.m_ValueInt64;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueDouble = packet.m_ValueDouble;
		timeSpanCustomStatWithTime.m_Time = packet.m_Time * 100;
		lock (m_CustomStatUnits)
		{
			m_CustomStatUnits[num] = num2;
		}
		int thread = packet.m_Thread;
		Array<TimeSpanCustomStatWithTime> value = null;
		if (!m_TimeSpanCustomStatArrays.TryGetValue(thread, out value))
		{
			value = new Array<TimeSpanCustomStatWithTime>();
			m_TimeSpanCustomStatArrays[thread] = value;
		}
		value.Add(timeSpanCustomStatWithTime);
		m_PacketAllocator.Free(packet);
	}

	private void HandleTimeSpanCustomStatPacket(TimeSpanCustomStatPacket packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long name = RemapAndHandleNewStringId(packet.m_Name, string_literal_type);
		TimeSpanCustomStatWithTime timeSpanCustomStatWithTime = new TimeSpanCustomStatWithTime();
		timeSpanCustomStatWithTime.m_CustomStat = new TimeSpanCustomStat();
		timeSpanCustomStatWithTime.m_CustomStat.m_Name = name;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueType = packet.m_ValueType;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueInt64 = packet.m_ValueInt64;
		timeSpanCustomStatWithTime.m_CustomStat.m_ValueDouble = packet.m_ValueDouble;
		timeSpanCustomStatWithTime.m_Time = packet.m_Time * 100;
		int thread = packet.m_Thread;
		Array<TimeSpanCustomStatWithTime> value = null;
		if (!m_TimeSpanCustomStatArrays.TryGetValue(thread, out value))
		{
			value = new Array<TimeSpanCustomStatWithTime>();
			m_TimeSpanCustomStatArrays[thread] = value;
		}
		value.Add(timeSpanCustomStatWithTime);
		m_PacketAllocator.Free(packet);
	}

	private void HandleTimeSpanPacketWithCallstack(TimeSpanPacketWithCallstack packet, StringLiteralType string_literal_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		HandleTimeSpanPacket(packet.m_TimeSpanPacket, string_literal_type, free_packet: false, packet.m_CallstackPacket.m_Id);
		HandleCallstackPacket(packet.m_CallstackPacket);
		m_PacketAllocator.Free(packet);
	}

	private void HandleNamedTimeSpanPacketWithCallstack(NamedTimeSpanPacketWithCallstack packet)
	{
		HandleNamedTimeSpanPacket(packet.m_NamedTimeSpanPacket, free_packet: false, packet.m_CallstackPacket.m_Id);
		HandleCallstackPacket(packet.m_CallstackPacket);
		m_PacketAllocator.Free(packet);
	}

	private void HandleStringLiteralNamedTimeSpanPacketWithCallstack(NamedTimeSpanPacketWithCallstack packet)
	{
		HandleStringLiteralNamedTimeSpanPacket(packet.m_NamedTimeSpanPacket, free_packet: false, packet.m_CallstackPacket.m_Id);
		HandleCallstackPacket(packet.m_CallstackPacket);
		m_PacketAllocator.Free(packet);
	}

	private void HandleCallstackPacket(TimeSpanCallstackPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		if (packet.m_Stack != null)
		{
			////m_SymLib.AddCallstack(packet.m_Id, packet.m_Stack);
		}
	}

	private void HandleModulePacket(ModulePacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		Module module = new Module();
		module.m_SymbolFilename = packet.m_SymbolFilename;
		module.m_Base = packet.m_ModuleBase;
		module.m_ModuleName = packet.m_ModuleName;
		module.m_IsMainModule = m_ReceivedModuleCount == 0;
		module.m_UseBaseAddrLookupFunction = packet.m_UseLookupFunctionForBaseAddress != 0;
		module.m_Age = packet.m_Age;
		module.m_SigLow = packet.m_SigLow;
		module.m_SigHigh = packet.m_SigHigh;
		module.m_ModuleName = module.m_ModuleName.Replace("/", "\\");
		lock (m_Modules)
		{
			m_Modules.Add(module);
		}
		m_LoadedModuleSymbols = false;
		m_ReceivedModuleCount++;
		m_PacketAllocator.Free(packet);
	}

	private void HandleSetScopeColourPacket(SetScopeColourPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long key = RemapStringId(packet.m_Name);
		lock (m_ScopeColours)
		{
			m_ScopeColours[key] = CoreUtils.ToColor(packet.m_Colour);
		}
		m_PacketAllocator.Free(packet);
		if (this.ScopeColourChanged != null)
		{
			this.ScopeColourChanged();
		}
	}

	private void HandleSetCustomStatInfoPacket(SetCustomStatInfoPacket packet, PacketType packet_type)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long key = RemapStringId(packet.m_Name);
		long value = RemapStringId(packet.m_Value);
		switch (packet_type)
		{
		case PacketType.SetCustomStatGraphPacket:
			lock (m_CustomStatGraphs)
			{
				m_CustomStatGraphs[key] = value;
			}
			break;
		case PacketType.SetCustomStatUnitPacket:
			lock (m_CustomStatUnits)
			{
				m_CustomStatUnits[key] = value;
			}
			break;
		}
		m_PacketAllocator.Free(packet);
		if (this.CustomStatInfoChanged != null)
		{
			this.CustomStatInfoChanged();
		}
	}

	private void HandleSetCustomStatColourPacket(SetCustomStatColourPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		long key = RemapStringId(packet.m_Name);
		lock (m_CustomStatColours)
		{
			m_CustomStatColours[key] = CoreUtils.ToColor(packet.m_Colour);
		}
		m_PacketAllocator.Free(packet);
		if (this.CustomStatColourChanged != null)
		{
			this.CustomStatColourChanged();
		}
	}

	private void HandleCallstackPacket(CallstackPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		if (packet.m_Stack != null)
		{
			////m_SymLib.AddCallstack(packet.m_Id, packet.m_Stack);
		}
		m_PacketAllocator.Free(packet);
	}

	private void HandleSessionInfoPacket(SessionInfoPacket packet)
	{
		m_FrameBytesSentCount += packet.GetSize();
		SessionInfoPair item = default(SessionInfoPair);
		item.m_Name = packet.m_Name;
		item.m_Value = packet.m_Value;
		using (new WriteLockScope(m_SessionInfoValuesLock))
		{
			m_SessionInfoValues.Add(item);
		}
		string @string = GetString(packet.m_Name);
		string string2 = GetString(packet.m_Value);
		switch (@string.ToLower())
		{
		case "name":
			m_SessionDetails.m_Name = string2;
			break;
		case "build id":
		case "buildid":
			m_SessionDetails.m_BuildId = string2;
			break;
		case "date":
			m_SessionDetails.m_Date = string2;
			break;
		}
		string message = "Session Info: " + @string + ": " + string2;
		LogViewLog(0L, message);
		m_PacketAllocator.Free(packet);
	}

	public void ReloadSymbols()
	{
		m_LoadedModuleSymbols = false;
		////m_SymLib.ClearSymbols();
	}

	public void LoadModuleSymbols()
	{
		if (m_LoadedModuleSymbols)
		{
			return;
		}
		m_LoadedModuleSymbols = true;
		List<Module> list = null;
		lock (m_Modules)
		{
			if (m_Modules.Count != 0)
			{
				list = new List<Module>(m_Modules);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Module item in list)
		{
			string error = "unknown error";
			////if (!m_SymLib.LoadSymbols(item.m_SymbolFilename, item.m_Base, item.m_ModuleName, item.m_IsMainModule, item.m_UseBaseAddrLookupFunction, item.m_Age, item.m_SigLow, item.m_SigHigh, ref error))
			////{
			////	LogLine("Failed reading symbols for " + item.m_ModuleName + " : " + error);
			////}
		}
	}

	public void GetEvents(long start_time, long end_time, List<Event> events)
	{
		lock (m_Events)
		{
			foreach (Event @event in m_Events)
			{
				if (@event.Time >= start_time && @event.Time < end_time)
				{
					events.Add(@event);
				}
			}
		}
	}

	private static WaitEvent FindLastStartWaitEvent(List<WaitEvent> wait_events)
	{
		for (int num = wait_events.Count - 1; num >= 0; num--)
		{
			if (wait_events[num].Mode == WaitEvent.WaitEventMode.Start)
			{
				return wait_events[num];
			}
		}
		return null;
	}

	private static WaitEvent FindFirstStopWaitEvent(List<WaitEvent> wait_events)
	{
		for (int i = 0; i < wait_events.Count; i++)
		{
			if (wait_events[i].Mode == WaitEvent.WaitEventMode.Stop)
			{
				return wait_events[i];
			}
		}
		return null;
	}

	public void GetWaitEvents(long start_time, long end_time, List<WaitEvent> events)
	{
		foreach (long key in m_TempWaitEventsDict.Keys)
		{
			m_TempWaitEventsDict[key].Clear();
		}
		List<FindTrigger> list = new List<FindTrigger>();
		List<FindTrigger> list2 = new List<FindTrigger>();
		Dictionary<long, List<WaitEvent>> tempWaitEventsDict = m_TempWaitEventsDict;
		lock (m_WaitEvents)
		{
			foreach (int key2 in m_WaitEvents.Keys)
			{
				TimeArray<WaitEvent> timeArray = m_WaitEvents[key2];
				lock (timeArray)
				{
					TimeArray<WaitEvent>.Index index = timeArray.GetIndex(start_time);
					if (index.IsValid && timeArray[index].Time < start_time)
					{
						index = timeArray.MoveNext(index);
					}
					if (!index.IsValid || timeArray[index].Time > end_time)
					{
						continue;
					}
					TimeArray<WaitEvent>.Index index2 = index;
					while (index.IsValid && timeArray[index].Mode != 0)
					{
						index2 = index;
						index = timeArray.MovePrev(index);
					}
					if (!index.IsValid)
					{
						index = index2;
					}
					if (index.IsValid && timeArray[index].Mode == WaitEvent.WaitEventMode.Start && timeArray[index].Time < start_time)
					{
						FindTrigger item = default(FindTrigger);
						item.m_EventId = timeArray[index].EventId;
						item.m_Time = timeArray[index].Time;
						list.Add(item);
					}
					bool flag = index.IsValid && timeArray[index].Mode == WaitEvent.WaitEventMode.Start;
					WaitEvent waitEvent = null;
					while (index.IsValid)
					{
						WaitEvent waitEvent2 = timeArray[index];
						if (waitEvent2.Time > end_time && !flag)
						{
							break;
						}
						if (!tempWaitEventsDict.TryGetValue(waitEvent2.EventId, out var value))
						{
							value = new List<WaitEvent>();
							tempWaitEventsDict[waitEvent2.EventId] = value;
						}
						value.Add(waitEvent2);
						if (waitEvent2.Mode == WaitEvent.WaitEventMode.Start)
						{
							flag = true;
						}
						else if (waitEvent2.Mode == WaitEvent.WaitEventMode.Stop)
						{
							flag = false;
						}
						waitEvent = waitEvent2;
						index = timeArray.MoveNext(index);
					}
					if (waitEvent != null && waitEvent.Time > end_time && waitEvent.Mode == WaitEvent.WaitEventMode.Stop)
					{
						FindTrigger item2 = default(FindTrigger);
						item2.m_EventId = waitEvent.EventId;
						item2.m_Time = waitEvent.Time;
						list2.Add(item2);
					}
				}
			}
			foreach (FindTrigger item3 in list)
			{
				foreach (int key3 in m_WaitEvents.Keys)
				{
					TimeArray<WaitEvent> timeArray2 = m_WaitEvents[key3];
					TimeArray<WaitEvent>.Index index3 = timeArray2.GetIndex(item3.m_Time);
					if (index3.IsValid && timeArray2[index3].Time < item3.m_Time)
					{
						index3 = timeArray2.MoveNext(index3);
					}
					while (index3.IsValid)
					{
						WaitEvent waitEvent3 = timeArray2[index3];
						if (waitEvent3.Time >= start_time)
						{
							break;
						}
						if (waitEvent3.EventId == item3.m_EventId && waitEvent3.Mode != 0)
						{
							if (waitEvent3.Mode == WaitEvent.WaitEventMode.Trigger)
							{
								tempWaitEventsDict[waitEvent3.EventId].Add(waitEvent3);
							}
							break;
						}
						index3 = timeArray2.MoveNext(index3);
					}
				}
			}
			foreach (FindTrigger item4 in list2)
			{
				foreach (int key4 in m_WaitEvents.Keys)
				{
					TimeArray<WaitEvent> timeArray3 = m_WaitEvents[key4];
					TimeArray<WaitEvent>.Index index4 = timeArray3.GetIndex(end_time);
					while (index4.IsValid)
					{
						WaitEvent waitEvent4 = timeArray3[index4];
						if (waitEvent4.Time > item4.m_Time)
						{
							break;
						}
						if (waitEvent4.EventId == item4.m_EventId && waitEvent4.Mode != WaitEvent.WaitEventMode.Stop)
						{
							if (waitEvent4.Mode == WaitEvent.WaitEventMode.Trigger)
							{
								tempWaitEventsDict[waitEvent4.EventId].Add(waitEvent4);
							}
							break;
						}
						index4 = timeArray3.MoveNext(index4);
					}
				}
			}
		}
		foreach (List<WaitEvent> value2 in tempWaitEventsDict.Values)
		{
			value2.Sort(WaitEventComparer);
		}
		m_TempWaitEventsList.Clear();
		List<WaitEvent> tempWaitEventsList = m_TempWaitEventsList;
		foreach (long key5 in tempWaitEventsDict.Keys)
		{
			List<WaitEvent> list3 = tempWaitEventsDict[key5];
			tempWaitEventsList.Clear();
			tempWaitEventsList.AddRange(list3);
			list3.Clear();
			WaitEvent.WaitEventMode waitEventMode = WaitEvent.WaitEventMode.Stop;
			int num = 0;
			foreach (WaitEvent item5 in tempWaitEventsList)
			{
				switch (item5.Mode)
				{
				case WaitEvent.WaitEventMode.Start:
					list3.Add(item5);
					num++;
					break;
				case WaitEvent.WaitEventMode.Trigger:
					if (waitEventMode != WaitEvent.WaitEventMode.Trigger && num != 0)
					{
						list3.Add(item5);
					}
					break;
				case WaitEvent.WaitEventMode.Stop:
					if (num <= 0)
					{
						break;
					}
					num--;
					if (waitEventMode == WaitEvent.WaitEventMode.Start)
					{
						if (list3.Count != 0)
						{
							list3.RemoveAt(list3.Count - 1);
						}
					}
					else
					{
						list3.Add(item5);
					}
					break;
				}
				waitEventMode = item5.Mode;
			}
		}
		events.Clear();
		foreach (long key6 in tempWaitEventsDict.Keys)
		{
			events.AddRange(tempWaitEventsDict[key6]);
		}
		events.Sort(WaitEventComparer);
	}

	private int WaitEventComparer(WaitEvent event1, WaitEvent event2)
	{
		int num = event1.Time.CompareTo(event2.Time);
		if (num == 0)
		{
			num = event1.StableSortIndex.CompareTo(event2.StableSortIndex);
		}
		return num;
	}

	private TimeArray<WaitEvent>.Index GetWaitEventIndex(long time, int thread_id)
	{
		lock (m_WaitEvents)
		{
			if (m_WaitEvents.TryGetValue(thread_id, out var value))
			{
				lock (value)
				{
					return value.GetIndex(time);
				}
			}
		}
		return TimeArray<WaitEvent>.InvalidIndex;
	}

	private WaitEvent GetPrevWaitEvent(WaitEvent wait_event, WaitEvent.WaitEventMode mode, int event_thread_id)
	{
		lock (m_WaitEvents)
		{
			m_TempWaitEventIterator.Initialise(m_WaitEvents);
			WaitEventIterator tempWaitEventIterator = m_TempWaitEventIterator;
			tempWaitEventIterator.JumpTo(wait_event.Time, WaitEventIterator.JumpMode.LessThanOrEqual);
			WaitEvent waitEvent = null;
			while (!tempWaitEventIterator.Done)
			{
				WaitEvent current = tempWaitEventIterator.Current;
				if (current.EventId == wait_event.EventId && current != wait_event && (event_thread_id == -1 || current.ThreadId == event_thread_id))
				{
					if (current.Mode == mode)
					{
						if (mode != WaitEvent.WaitEventMode.Trigger)
						{
							return current;
						}
						waitEvent = current;
					}
					else
					{
						if (mode == WaitEvent.WaitEventMode.Trigger && waitEvent != null)
						{
							return waitEvent;
						}
						if (current.Mode == WaitEvent.WaitEventMode.Stop)
						{
							return null;
						}
					}
				}
				tempWaitEventIterator.MovePrev();
			}
			return waitEvent;
		}
	}

	private WaitEvent GetNextWaitEvent(WaitEvent wait_event, WaitEvent.WaitEventMode mode, int event_thread_id)
	{
		lock (m_WaitEvents)
		{
			m_TempWaitEventIterator.Initialise(m_WaitEvents);
			WaitEventIterator tempWaitEventIterator = m_TempWaitEventIterator;
			tempWaitEventIterator.JumpTo(wait_event.Time, WaitEventIterator.JumpMode.GreaterThanOrEqual);
			while (!tempWaitEventIterator.Done)
			{
				WaitEvent current = tempWaitEventIterator.Current;
				if (current.EventId == wait_event.EventId && current != wait_event && (event_thread_id == -1 || current.ThreadId == event_thread_id))
				{
					if (current.Mode == mode)
					{
						return current;
					}
					if (current.Mode == WaitEvent.WaitEventMode.Start)
					{
						return null;
					}
				}
				tempWaitEventIterator.MoveNext();
			}
		}
		return null;
	}

	private void AddContextSwitch(ContextSwitch context_switch)
	{
		using (new WriteLockScope(m_ContextSwitchArrayLock))
		{
			while (m_ContextSwitchArray.Count <= context_switch.m_CPUId)
			{
				m_ContextSwitchArray.Add(CreateContextSwitchArray(m_TimerFrequency));
			}
			m_ContextSwitchArray[context_switch.m_CPUId].Add(context_switch);
		}
	}

	private static ContextSwitchArray CreateContextSwitchArray(long timer_frequency)
	{
		return new ContextSwitchArray(100 * timer_frequency / 1000);
	}

	public void GetContextSwitches(long start_time, long end_time, out List<List<ContextSwitch>> context_switches)
	{
		ContextSwitchArrayKey contextSwitchArrayKey = default(ContextSwitchArrayKey);
		contextSwitchArrayKey.m_StartTime = start_time;
		contextSwitchArrayKey.m_EndTime = end_time;
		List<List<ContextSwitch>> value;
		using (new ReadLockScope(m_ContextSwitchArrayCacheLock))
		{
			if (m_ContextSwitchArrayCache.TryGetValue(contextSwitchArrayKey, out value))
			{
				context_switches = value;
				return;
			}
		}
		using (new ReadLockScope(m_ContextSwitchArrayLock))
		{
			context_switches = new List<List<ContextSwitch>>();
			int count = m_ContextSwitchArray.Count;
			while (context_switches.Count < count)
			{
				context_switches.Add(new List<ContextSwitch>());
			}
			for (int i = 0; i < count; i++)
			{
				m_ContextSwitchArray[i].GetContextSwitches(start_time, end_time, context_switches[i]);
			}
		}
		using (new WriteLockScope(m_ContextSwitchArrayCacheLock))
		{
			if (m_ContextSwitchArrayCache.TryGetValue(contextSwitchArrayKey, out value))
			{
				context_switches = value;
				return;
			}
			m_ContextSwitchArrayCache[contextSwitchArrayKey] = context_switches;
			m_ContextSwitchArrayCacheKeys.Enqueue(contextSwitchArrayKey);
			if (m_ContextSwitchArrayCacheKeys.Count > 8)
			{
				ContextSwitchArrayKey key = m_ContextSwitchArrayCacheKeys.Dequeue();
				m_ContextSwitchArrayCache.Remove(key);
			}
		}
	}

	private void RequestStringLiteralValue(long string_id, StringLiteralType string_literal_type)
	{
		Send(new RequestStringLiteralPacket(string_id, string_literal_type));
	}

	private string GetTimerName(TimeSpan time_span)
	{
		if (time_span.IsTimeSpanEx && ((TimeSpanEx)time_span).HiResTimers != null && ((TimeSpanEx)time_span).HiResTimers.Count != 0)
		{
			return "hires timers: " + ((TimeSpanEx)time_span).HiResTimers.Count;
		}
		if (time_span.TimeSpanInfoId == int.MaxValue)
		{
			return "root";
		}
		return GetTimerName(GetTimeSpanInfo(time_span.TimeSpanInfoId).Name);
	}

	public string GetTimerName(long name)
	{
		using (new ReadLockScope(m_StringsLock))
		{
			if (m_Strings.TryGetValue(name, out var value))
			{
				return value;
			}
		}
		if (name != -1)
		{
			return "Timer " + name;
		}
		return "Root";
	}

	public Set<long> GetTimerNames(string filter)
	{
		Set<long> set = new Set<long>();
		filter = filter.ToLower();
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			foreach (long timeSpanName in m_TimeSpanNames)
			{
				if (GetTimerName(timeSpanName).ToLower().Contains(filter))
				{
					set.Add(timeSpanName);
				}
			}
			return set;
		}
	}

	public SourceInfoStruct GetSourceInfo(long source_info_id)
	{
		using (new ReadLockScope(m_SourceInfosLock))
		{
			if (m_SourceInfos.TryGetValue(source_info_id, out var value))
			{
				return new SourceInfoStruct(value);
			}
		}
		return new SourceInfoStruct(null);
	}

	private void StartProcessEventsThread(ProcessingPacketsMode processing_packets_mode)
	{
		m_ProcessEventsThread = new Thread(ProcessEventsThreadMain);
		m_ProcessEventsThread.Priority = ThreadPriority.AboveNormal;
		m_ProcessEventsThread.Name = "Process Events";
		m_ProcessEventsThread.Start(processing_packets_mode);
	}

	private void StartDeserialiseThread(ReceiveStream receive_stream, long file_size, ThreadJobContext context)
	{
		m_DeserialiseReceiveThread = new Thread((ThreadStart)delegate
		{
			DeserialiseThreadMain(receive_stream, file_size, context);
		});
		m_DeserialiseReceiveThread.Priority = ThreadPriority.AboveNormal;
		m_DeserialiseReceiveThread.Name = "DeserialiseThread";
		m_DeserialiseReceiveThread.Start();
	}

	private void ReceiveThreadMain()
	{
		ReceiveStream receiveStream = new ReceiveStream();
		StartDeserialiseThread(receiveStream, -1L, null);
		BinaryReader binaryReader = new BinaryReader(m_TcpCllient.GetStream());
		try
		{
			while (m_TcpCllient.Connected && !m_Disposing)
			{
				byte[] array = new byte[32768];
				int i = 0;
				if (m_TcpCllient.Available == 0)
				{
					i = binaryReader.Read(array, 0, 4);
					if (m_TcpCllient == null || !m_TcpCllient.Connected)
					{
						break;
					}
				}
				int count;
				for (; i != array.Length; i += binaryReader.Read(array, i, count))
				{
					if (m_TcpCllient.Available == 0)
					{
						break;
					}
					count = Math.Min(m_TcpCllient.Available, array.Length - i);
				}
				if (i != 0)
				{
					receiveStream.EnqueueBuffer(array, i);
				}
			}
		}
		catch (Exception)
		{
		}
		receiveStream.SetFinished();
		binaryReader.Close();
		if (!m_Interactive && this.NonInteractiveModeFinished != null)
		{
			this.NonInteractiveModeFinished();
		}
	}

	private void Send(SendPacket packet)
	{
		lock (m_SendPacketQueue)
		{
			m_SendPacketQueue.Add(packet);
		}
		m_SendEvent.Set();
	}

	private void SendThreadMain()
	{
		BinaryWriter binaryWriter = new BinaryWriter(m_TcpCllient.GetStream());
		List<SendPacket> list = new List<SendPacket>();
		while (Connected && !m_Disposing)
		{
			m_SendEvent.WaitOne(100);
			if (!Connected)
			{
				break;
			}
			lock (m_SendPacketQueue)
			{
				list.AddRange(m_SendPacketQueue);
				m_SendPacketQueue.Clear();
			}
			try
			{
				foreach (SendPacket item in list)
				{
					item.Send(binaryWriter);
				}
			}
			catch (Exception ex)
			{
				LogLine(ex.Message);
				break;
			}
			list.Clear();
		}
		lock (m_SendPacketQueue)
		{
			m_SendPacketQueue.Clear();
		}
		binaryWriter.Close();
	}

	private PacketT ReadPacket<PacketT>(ReceiveStream reader, int packed_value) where PacketT : IPacket, new()
	{
		PacketT result = m_PacketAllocator.Alloc<PacketT>();
		result.Read(reader, packed_value);
		return result;
	}

	private void CapReceiveSpeed(long frame_start_time)
	{
		if (m_TimerFrequency == 0L || ConnectTime == 0)
		{
			return;
		}
		long num = frame_start_time - FirstFrameTime;
		long num2 = (Environment.TickCount - ConnectTime) * m_TimerFrequency / 1000;
		long num3 = num - num2;
		num3 = num3 * Stopwatch.Frequency / m_TimerFrequency;
		num3 = Math.Min(num3, Stopwatch.Frequency);
		if (num3 != 0L)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedTicks < num3)
			{
			}
		}
	}

	private void DeserialiseThreadMain(ReceiveStream reader, long file_size, ThreadJobContext context)
	{
		List<ReceivedPacket> list = new List<ReceivedPacket>();
		int num = Environment.TickCount;
		bool flag = false;
		int num2 = 0;
		while (!reader.FinishedReceiving)
		{
			ReceivedPacket item = default(ReceivedPacket);
			try
			{
				int num3 = reader.ReadInt32();
				if (reader.FinishedReceiving)
				{
					break;
				}
				item.m_PacketType = (PacketType)(num3 & 0xFFFF);
				int packed_value = (num3 >> 16) & 0xFFFF;
				if (!flag && item.m_PacketType != PacketType.Connect)
				{
					Disconnect(DisconnectReason.UnexpectedPacket);
					break;
				}
				switch (item.m_PacketType)
				{
				case PacketType.Connect:
					item.m_Packet = ReadPacket<ConnectPacket>(reader, packed_value);
					break;
				case PacketType.FrameStart:
					item.m_Packet = ReadPacket<FrameStartPacket>(reader, packed_value);
					if (m_ShouldCapReceiveSpeed)
					{
						CapReceiveSpeed(((FrameStartPacket)item.m_Packet).m_FrameStartTime * 100);
					}
					break;
				case PacketType.TimeSpan:
					item.m_Packet = ReadPacket<TimeSpanPacket>(reader, packed_value);
					break;
				case PacketType.TimeSpanW:
					item.m_Packet = ReadPacket<TimeSpanPacket>(reader, packed_value);
					break;
				case PacketType.NamedTimeSpan:
					item.m_Packet = ReadPacket<NamedTimeSpanPacket>(reader, packed_value);
					break;
				case PacketType.StringLiteralNamedTimeSpan:
					item.m_Packet = ReadPacket<NamedTimeSpanPacket>(reader, packed_value);
					break;
				case PacketType.ThreadName:
					item.m_Packet = ReadPacket<ThreadNamePacket>(reader, packed_value);
					break;
				case PacketType.ThreadOrder:
					item.m_Packet = ReadPacket<ThreadOrderPacket>(reader, packed_value);
					break;
				case PacketType.String:
					item.m_Packet = ReadPacket<StringPacket>(reader, packed_value);
					break;
				case PacketType.WString:
					item.m_Packet = ReadPacket<WStringPacket>(reader, packed_value);
					break;
				case PacketType.NameAndSourceInfo:
					item.m_Packet = ReadPacket<StringPacket>(reader, packed_value);
					break;
				case PacketType.NameAndSourceInfoW:
					item.m_Packet = ReadPacket<WStringPacket>(reader, packed_value);
					break;
				case PacketType.SourceInfo:
					item.m_Packet = ReadPacket<StringPacket>(reader, packed_value);
					break;
				case PacketType.MainThread:
					item.m_Packet = ReadPacket<MainThreadPacket>(reader, packed_value);
					break;
				case PacketType.SessionStatsPacket:
					item.m_Packet = ReadPacket<SessionStatsPacket>(reader, packed_value);
					break;
				case PacketType.legacy_SessionDetailsPacket:
					item.m_Packet = ReadPacket<SessionDetailsPacket>(reader, packed_value);
					break;
				case PacketType.ContextSwitchPacket:
					item.m_Packet = ReadPacket<ContextSwitchPacket>(reader, packed_value);
					break;
				case PacketType.ContextSwitchRecordingStartedPacket:
					item.m_Packet = ReadPacket<ContextSwitchRecordingStartedPacket>(reader, packed_value);
					break;
				case PacketType.ProcessNamePacket:
					item.m_Packet = ReadPacket<ProcessNamePacket>(reader, packed_value);
					break;
				case PacketType.CustomStatPacket_Depreciated:
					item.m_Packet = ReadPacket<CustomStatPacket_Depreciated>(reader, packed_value);
					break;
				case PacketType.CustomStatPacketW_Depreciated2:
					item.m_Packet = ReadPacket<CustomStatPacket_Depreciated2>(reader, packed_value);
					break;
				case PacketType.StringLiteralTimerNamePacket:
					item.m_Packet = ReadPacket<StringPacket>(reader, packed_value);
					break;
				case PacketType.HiResTimerScopePacket:
					item.m_Packet = ReadPacket<HiResTimerScopePacket>(reader, packed_value);
					break;
				case PacketType.LogPacket:
					item.m_Packet = ReadPacket<LogPacket>(reader, packed_value);
					break;
				case PacketType.EventPacket:
					item.m_Packet = ReadPacket<EventPacket>(reader, packed_value);
					break;
				case PacketType.StartWaitEventPacket:
					item.m_Packet = ReadPacket<WaitEventPacket>(reader, packed_value);
					break;
				case PacketType.StopWaitEventPacket:
					item.m_Packet = ReadPacket<WaitEventPacket>(reader, packed_value);
					break;
				case PacketType.TriggerWaitEventPacket:
					item.m_Packet = ReadPacket<WaitEventPacket>(reader, packed_value);
					break;
				case PacketType.TimeSpanCustomStatPacket_Depreciated:
					item.m_Packet = ReadPacket<TimeSpanCustomStatPacket_Depreciated>(reader, packed_value);
					break;
				case PacketType.TimeSpanCustomStatPacketW:
					item.m_Packet = ReadPacket<TimeSpanCustomStatPacket>(reader, packed_value);
					break;
				case PacketType.TimeSpanWithCallstack:
					item.m_Packet = ReadPacket<TimeSpanPacketWithCallstack>(reader, packed_value);
					break;
				case PacketType.TimeSpanWWithCallstack:
					item.m_Packet = ReadPacket<TimeSpanPacketWithCallstack>(reader, packed_value);
					break;
				case PacketType.NamedTimeSpanWithCallstack:
					item.m_Packet = ReadPacket<NamedTimeSpanPacketWithCallstack>(reader, packed_value);
					break;
				case PacketType.StringLiteralNamedTimeSpanWithCallstack:
					item.m_Packet = ReadPacket<NamedTimeSpanPacketWithCallstack>(reader, packed_value);
					break;
				case PacketType.ModulePacket:
					item.m_Packet = ReadPacket<ModulePacket>(reader, packed_value);
					break;
				case PacketType.CustomStatPacket_Depreciated2:
					item.m_Packet = ReadPacket<CustomStatPacket_Depreciated2>(reader, packed_value);
					break;
				case PacketType.TimeSpanCustomStatPacket:
					item.m_Packet = ReadPacket<TimeSpanCustomStatPacket>(reader, packed_value);
					break;
				case PacketType.SetScopeColourPacket:
					item.m_Packet = ReadPacket<SetScopeColourPacket>(reader, packed_value);
					break;
				case PacketType.SetCustomStatGraphPacket:
					item.m_Packet = ReadPacket<SetCustomStatInfoPacket>(reader, packed_value);
					break;
				case PacketType.SetCustomStatUnitPacket:
					item.m_Packet = ReadPacket<SetCustomStatInfoPacket>(reader, packed_value);
					break;
				case PacketType.SetCustomStatColourPacket:
					item.m_Packet = ReadPacket<SetCustomStatColourPacket>(reader, packed_value);
					break;
				case PacketType.CallstackPacket:
					item.m_Packet = ReadPacket<CallstackPacket>(reader, packed_value);
					break;
				case PacketType.SessionInfoPacket:
					item.m_Packet = ReadPacket<SessionInfoPacket>(reader, packed_value);
					break;
				case PacketType.CustomStatPacket:
					item.m_Packet = ReadPacket<CustomStatPacket>(reader, packed_value);
					break;
				case PacketType.CustomStatPacketW:
					item.m_Packet = ReadPacket<CustomStatPacket>(reader, packed_value);
					break;
				}
				if (reader.FinishedReceiving)
				{
					goto IL_0581;
				}
				if (item.m_Packet == null)
				{
					this.ShowError("ERROR: Received bad packet from app. Shutting down connection.");
					Disconnect(DisconnectReason.UnexpectedPacket);
					break;
				}
				if (flag)
				{
					goto IL_0579;
				}
				flag = true;
				ConnectPacket connectPacket = (ConnectPacket)item.m_Packet;
				m_ReceivedFrameProLibVersion = connectPacket.m_FrameProLibVersion;
				if (IsValidFrameProLibVersion(17, connectPacket.m_FrameProLibVersion))
				{
					goto IL_0579;
				}
				Disconnect(DisconnectReason.BadVersion);
				goto end_IL_001d;
				IL_0581:
				int tickCount = Environment.TickCount;
				if (tickCount - num >= 30)
				{
					num = tickCount;
					lock (m_ReceivedPackets)
					{
						m_ReceivedPackets.Add(list);
						m_TotalReceivedPackets += list.Count;
					}
					list.Clear();
				}
				goto IL_05ee;
				IL_0579:
				list.Add(item);
				goto IL_0581;
				end_IL_001d:;
			}
			catch (Exception ex)
			{
				LogLine(ex.Message);
			}
			break;
			IL_05ee:
			if (context != null && (num2 & 0x40) == 64)
			{
				context.Progress.PercentComplete = CoreUtils.GetPercentComplete(reader, file_size);
				if (context.Cancel)
				{
					break;
				}
			}
			num2++;
		}
		m_DisconnectTime = Environment.TickCount;
		if (!flag)
		{
			Disconnect(DisconnectReason.NoData);
		}
		bool flag2 = m_DisconnectReason > DisconnectReason.Errors;
		if (!flag2)
		{
			lock (m_ReceivedPackets)
			{
				m_ReceivedPackets.Add(list);
				m_TotalReceivedPackets += list.Count;
			}
			list.Clear();
		}
		m_Connected = false;
		if (this.Disconnected != null)
		{
			this.Disconnected();
		}
		if (context != null)
		{
			context.Progress.PercentComplete = 100;
		}
		if (!flag2 && m_StartedXBoxOneEtlTrace && StopXBoxOneEtlTrace() && this.ETLTraceFinished != null)
		{
			this.ETLTraceFinished();
		}
		m_ReceiveThreadMainFinishedEvent.Set();
	}

	public void GetFrames(long start_time, long end_time, List<Frame> frames)
	{
		int num = Math.Max(0, GetFrameIndex(start_time));
		int frameCount = FrameCount;
		using (new ReadLockScope(m_FramesLock))
		{
			for (int i = num; i < frameCount; i++)
			{
				Frame frame = m_Frames[i];
				if (frame.StartTime > end_time)
				{
					break;
				}
				frames.Add(frame);
			}
		}
	}

	public void GetFrames(int start_frame_index, int end_frame_index, List<FrameStruct> frames)
	{
		int i = start_frame_index;
		long num = 30 * m_TimerFrequency / 1000;
		for (; i < 0; i++)
		{
			FrameStruct item = default(FrameStruct);
			item.m_StartTime = -i * num;
			item.m_EndTime = item.m_StartTime + num;
			frames.Add(item);
		}
		long num2 = 0L;
		using (new ReadLockScope(m_FramesLock))
		{
			int count = m_Frames.Count;
			while (i <= end_frame_index && i < count)
			{
				Frame frame = m_Frames[i];
				FrameStruct item2 = default(FrameStruct);
				item2.m_StartTime = frame.StartTime;
				item2.m_EndTime = frame.EndTime;
				item2.m_Frame = frame;
				frames.Add(item2);
				i++;
				num2 = frame.EndTime;
			}
		}
		int num3 = i;
		for (; i <= end_frame_index; i++)
		{
			FrameStruct item3 = default(FrameStruct);
			item3.m_StartTime = num2 + (i - num3) * num;
			item3.m_EndTime = item3.m_StartTime + num;
			frames.Add(item3);
		}
	}

	public int GetFrameIndex(long time)
	{
		long num = 30 * m_TimerFrequency / 1000;
		long firstFrameTime = FirstFrameTime;
		if (time < firstFrameTime)
		{
			return (int)((time - firstFrameTime + 1) / num - 1);
		}
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count == 0)
			{
				return 0;
			}
			if (time < m_Frames[m_Frames.Count - 1].EndTime)
			{
				return m_Frames.GetIndex(time);
			}
		}
		return FrameCount + (int)((time - LastFrameEndTime) / num);
	}

	private int GetFrameIndex_NoLock(long time)
	{
		long num = 30 * m_TimerFrequency / 1000;
		long firstFrameTime = FirstFrameTime;
		if (time < firstFrameTime)
		{
			return -(int)((firstFrameTime - time + 1) / num - 1);
		}
		if (m_Frames.Count == 0)
		{
			return 0;
		}
		if (time < m_Frames[m_Frames.Count - 1].EndTime)
		{
			return m_Frames.GetIndex(time);
		}
		return m_Frames.Count + (int)((time - LastFrameEndTime) / num);
	}

	private bool ReadRecording(string filename, ThreadJobContext context, ref string error)
	{
		m_Connected = true;
		m_ConnectTime = Environment.TickCount;
		FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
		BinaryReader binaryReader = new BinaryReader(fileStream);
		byte[] array = new byte[m_RecordingFileMarker.Length];
		fileStream.Read(array, 0, array.Length);
		if (!array.SequenceEqual(m_RecordingFileMarker))
		{
			fileStream.Seek(0L, SeekOrigin.Begin);
		}
		ReceiveStream receiveStream = new ReceiveStream();
		StartDeserialiseThread(receiveStream, binaryReader.BaseStream.Length, context);
		StartProcessEventsThread(ProcessingPacketsMode.Recording);
		long num = fileStream.Length - fileStream.Position;
		while (num != 0L && !m_Disposing)
		{
			int num2 = (int)Math.Min(num, 32768L);
			byte[] buffer = new byte[num2];
			int num3 = fileStream.Read(buffer, 0, num2);
			if (num3 != 0)
			{
				receiveStream.EnqueueBuffer(buffer, num3);
			}
			num -= num3;
		}
		receiveStream.SetFinished();
		m_ReceiveThreadMainFinishedEvent.WaitOne();
		switch (m_DisconnectReason)
		{
		case DisconnectReason.BadVersion:
			error = "Incorrect FrameProLib version.";
			error += "Your version of FramePro does not match the version of FrameProLib compiled into your app.\n";
			error = error + "Found version " + m_ReceivedFrameProLibVersion + ", expected version " + 17;
			break;
		case DisconnectReason.UnexpectedPacket:
		case DisconnectReason.NoData:
			error = "Corrupt file";
			break;
		default:
			if (m_DisconnectReason > DisconnectReason.Errors)
			{
				error = m_DisconnectReason.ToString();
			}
			break;
		}
		return m_DisconnectReason < DisconnectReason.Errors;
	}

	private bool CanConvertSaveFile(int file_version)
	{
		return file_version <= 47;
	}

	public bool Read(string filename, ref string error)
	{
		SessionViewSaveData session_gui_data = new SessionViewSaveData();
		ThreadJobContext threadJobContext = new ThreadJobContext();
		threadJobContext.Progress.Start();
		return Read(filename, session_gui_data, threadJobContext, ref error);
	}

	public bool Read(string filename, SessionViewSaveData session_gui_data, ThreadJobContext context, ref string error)
	{
		LogLine("Reading " + filename);
		m_Saved = true;
		bool flag = false;
		bool flag2 = (m_Recorded = IsRecordingFile(filename));
		if (this.ReadStarted != null)
		{
			this.ReadStarted(flag2);
		}
		if (flag2)
		{
			LogLine("Reading Recording file...");
			return ReadRecording(filename, context, ref error);
		}
		return ReadInternal(filename, session_gui_data, context, ref error);
	}

	public bool IsRecordingFile(string filename)
	{
		if (Path.GetExtension(filename).ToLower() == ".framepro_recording" || Path.GetExtension(filename).ToLower() == ".framepro_dump")
		{
			return true;
		}
		try
		{
			FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			byte[] array = new byte[m_RecordingFileMarker.Length];
			fileStream.Read(array, 0, array.Length);
			bool num = array.SequenceEqual(m_RecordingFileMarker);
			fileStream.Close();
			if (num)
			{
				Log.WriteLine("File has recording marker", LogVerbosity.Verbose);
			}
			return num;
		}
		catch (Exception ex)
		{
			LogLine(ex.Message);
		}
		return false;
	}

	private bool ReadInternal(string filename, IFrameProSerialisable session_gui_data, ThreadJobContext context, ref string error)
	{
		try
		{
			SessionFilename = filename;
			FileStream fileStream = new FileStream(m_SessionFilename, FileMode.Open, FileAccess.Read);
			byte[] array = new byte[m_FileMarker.Length];
			fileStream.Read(array, 0, array.Length);
			if (!array.SequenceEqual(m_FileMarker))
			{
				fileStream.Seek(0L, SeekOrigin.Begin);
			}
			BinaryReader binary_reader = new BinaryReader(fileStream);
			bool result = ReadInternal(binary_reader, session_gui_data, context, ref error);
			m_FinishedProcessingPacketsEvent.Set();
			return result;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private bool ReadInternal(BinaryReader binary_reader, IFrameProSerialisable session_gui_data, ThreadJobContext context, ref string error)
	{
		long length = binary_reader.BaseStream.Length;
		try
		{
			int num = binary_reader.ReadInt32();
			if (num != 47 && !CanConvertSaveFile(num))
			{
				if (num < 47)
				{
					error = "Unable to open file because it was saved with an old version of FramePro";
				}
				else
				{
					error = "Unable to open file because it was saved with a newer version of FramePro";
				}
				return false;
			}
			if (num >= 21)
			{
				session_gui_data.Read(binary_reader, num);
			}
			if (num >= 12)
			{
				m_TimerFrequency = binary_reader.ReadInt64();
				SetFirstFrameTime(binary_reader.ReadInt64());
			}
			else
			{
				m_TimerFrequency = 174560372300L;
			}
			if (num >= 24)
			{
				long lastFrameEndtime = binary_reader.ReadInt64();
				SetLastFrameEndtime(lastFrameEndtime);
			}
			if (num >= 17)
			{
				RecordingContextSwitches = binary_reader.ReadBoolean();
				m_RemoteProcessId = binary_reader.ReadInt32();
			}
			if (num >= 23)
			{
				using (new WriteLockScope(m_CustomStatSessionInfoLock))
				{
					if (num < 40)
					{
						CustomStatSessionData.m_LegacySetGraphUnitDelegate = LegacySetGraphUnit;
					}
					int num2 = binary_reader.ReadInt32();
					for (int i = 0; i < num2; i++)
					{
						CustomStatSessionData customStatSessionData = new CustomStatSessionData();
						customStatSessionData.Read(binary_reader, num);
						m_CustomStatSessionInfo[customStatSessionData.Name] = customStatSessionData;
					}
					if (num < 40)
					{
						CustomStatSessionData.m_LegacySetGraphUnitDelegate = null;
					}
				}
				lock (m_CustomStatValueTypes)
				{
					int num3 = binary_reader.ReadInt32();
					for (int j = 0; j < num3; j++)
					{
						long key = binary_reader.ReadInt64();
						CustomStatValueType value = (CustomStatValueType)binary_reader.ReadInt32();
						m_CustomStatValueTypes[key] = value;
					}
				}
			}
			if (num >= 25)
			{
				lock (m_CustomStatValuePerSecArrays)
				{
					int num4 = binary_reader.ReadInt32();
					for (int k = 0; k < num4; k++)
					{
						long key2 = binary_reader.ReadInt64();
						CustomStatValuePerSecArray customStatValuePerSecArray = new CustomStatValuePerSecArray();
						customStatValuePerSecArray.m_ValuePerSecArray.Read(binary_reader, num);
						m_CustomStatValuePerSecArrays[key2] = customStatValuePerSecArray;
					}
				}
			}
			if (num >= 29)
			{
				lock (m_LogMessages)
				{
					int num5 = binary_reader.ReadInt32();
					for (int l = 0; l < num5; l++)
					{
						LogMessage logMessage = new LogMessage();
						logMessage.Read(binary_reader);
						m_LogMessages.Add(logMessage);
					}
				}
			}
			if (num >= 30)
			{
				lock (m_Events)
				{
					int num6 = binary_reader.ReadInt32();
					for (int m = 0; m < num6; m++)
					{
						Event @event = new Event();
						@event.Read(binary_reader);
						m_Events.Add(@event);
					}
				}
			}
			if (num >= 31)
			{
				lock (m_WaitEvents)
				{
					int num7 = binary_reader.ReadInt32();
					for (int n = 0; n < num7; n++)
					{
						int key3 = binary_reader.ReadInt32();
						long num8 = binary_reader.ReadInt64();
						TimeArray<WaitEvent> timeArray = new TimeArray<WaitEvent>(m_TimerFrequency);
						for (long num9 = 0L; num9 < num8; num9++)
						{
							WaitEvent waitEvent = new WaitEvent();
							waitEvent.Read(binary_reader);
							timeArray.Add(waitEvent);
						}
						m_WaitEvents[key3] = timeArray;
					}
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			int num10 = binary_reader.ReadInt32();
			using (new WriteLockScope(m_ThreadsLock))
			{
				for (int num11 = 0; num11 < num10; num11++)
				{
					int key4 = binary_reader.ReadInt32();
					ThreadInfo threadInfo = new ThreadInfo();
					threadInfo.Read(binary_reader, num);
					m_Threads[key4] = threadInfo;
					if ((num11 & 0x40) == 64)
					{
						context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
					}
					if (context.Cancel)
					{
						return false;
					}
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			using (new WriteLockScope(m_FramesLock))
			{
				m_Frames = new FrameArray();
				m_Frames.SetBlockDuration(m_TimerFrequency);
				if (num < 40)
				{
					CustomStat.m_LegacySetGraphUnitDelegate = LegacySetGraphUnit;
				}
				int num12 = binary_reader.ReadInt32();
				for (int num13 = 0; num13 < num12; num13++)
				{
					Frame frame = new Frame(num13);
					frame.Read(binary_reader, num);
					m_Frames.Add(frame);
					if ((num13 & 0x40) == 64)
					{
						context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
					}
					if (context.Cancel)
					{
						return false;
					}
				}
				if (num < 40)
				{
					CustomStat.m_LegacySetGraphUnitDelegate = null;
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			if (num >= 15)
			{
				using (new WriteLockScope(m_TimeSpanInfoSetLock))
				{
					m_TimeSpanInfoSet.Read(binary_reader, num);
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			int num14 = binary_reader.ReadInt32();
			for (int num15 = 0; num15 < num14; num15++)
			{
				int num16 = binary_reader.ReadInt32();
				TimeSpanList timeSpanList = new TimeSpanList(m_TimerFrequency, num16);
				if (num < 40)
				{
					TimeSpanEx.m_SetCustomStatUnitDelegate_Depreciated = LegacySetCustomStatUnit;
				}
				using (new ReadLockScope(m_TimeSpanInfoSetLock))
				{
					timeSpanList.Read(binary_reader, num, context, m_TimeSpanInfoSet, length);
				}
				if (num < 40)
				{
					TimeSpanEx.m_SetCustomStatUnitDelegate_Depreciated = null;
				}
				using (new WriteLockScope(m_TimeSpansLock))
				{
					m_TimeSpans[num16] = timeSpanList;
				}
				if ((num15 & 0x40) == 64)
				{
					context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
				}
				if (context.Cancel)
				{
					return false;
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			int num17 = binary_reader.ReadInt32();
			for (int num18 = 0; num18 < num17; num18++)
			{
				long num19 = binary_reader.ReadInt64();
				string text = binary_reader.ReadString();
				using (new WriteLockScope(m_StringsLock))
				{
					m_Strings[num19] = text;
					if (!m_StringIds.ContainsKey(text))
					{
						m_StringIds[text] = num19;
					}
				}
				if ((num18 & 0x40) == 64)
				{
					context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
				}
				if (context.Cancel)
				{
					return false;
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			int num20 = binary_reader.ReadInt32();
			for (int num21 = 0; num21 < num20; num21++)
			{
				long key5 = binary_reader.ReadInt64();
				SourceInfo sourceInfo = new SourceInfo();
				sourceInfo.Read(binary_reader, num);
				using (new WriteLockScope(m_SourceInfosLock))
				{
					m_SourceInfos[key5] = sourceInfo;
				}
				if ((num21 & 0x40) == 64)
				{
					context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
				}
				if (context.Cancel)
				{
					return false;
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			int num22 = binary_reader.ReadInt32();
			using (new WriteLockScope(m_ThreadOrderLock))
			{
				for (int num23 = 0; num23 < num22; num23++)
				{
					m_ThreadOrder.Add(binary_reader.ReadString());
				}
			}
			if (num > 8)
			{
				int num24 = binary_reader.ReadInt32();
				using (new WriteLockScope(m_TimeSpanNamesLock))
				{
					for (int num25 = 0; num25 < num24; num25++)
					{
						m_TimeSpanNames.Add(binary_reader.ReadInt64());
					}
				}
			}
			else
			{
				using (new ReadLockScope(m_TimeSpansLock))
				{
					foreach (TimeSpanList value2 in m_TimeSpans.Values)
					{
						using (new ReadLockScope(value2.Lock))
						{
							TimeSpanIterator timeSpanIterator = new TimeSpanIterator(value2.RootTimeSpan);
							while (timeSpanIterator.MoveNext())
							{
								TimeSpanInfo timeSpanInfo = GetTimeSpanInfo(timeSpanIterator.Current.TimeSpanInfoId);
								using (new WriteLockScope(m_TimeSpanNamesLock))
								{
									if (!m_TimeSpanNames.Contains(timeSpanInfo.Name))
									{
										m_TimeSpanNames.Add(timeSpanInfo.Name);
									}
								}
							}
						}
					}
				}
			}
			if (num > 9)
			{
				using (new WriteLockScope(m_TimeSpanFrameStatsLock))
				{
					int num26 = binary_reader.ReadInt32();
					for (int num27 = 0; num27 < num26; num27++)
					{
						long key6 = binary_reader.ReadInt64();
						TimeSpanFrameStats timeSpanFrameStats = new TimeSpanFrameStats();
						timeSpanFrameStats.Read(binary_reader, num);
						m_TimeSpanFrameStats[key6] = timeSpanFrameStats;
					}
				}
			}
			if (num >= 17)
			{
				using (new WriteLockScope(m_ContextSwitchArrayLock))
				{
					int num28 = binary_reader.ReadInt32();
					for (int num29 = 0; num29 < num28; num29++)
					{
						ContextSwitchArray contextSwitchArray = CreateContextSwitchArray(m_TimerFrequency);
						contextSwitchArray.Read(binary_reader);
						m_ContextSwitchArray.Add(contextSwitchArray);
					}
				}
			}
			if (num <= 11)
			{
				m_TimerFrequency = binary_reader.ReadInt64();
				SetFirstFrameTime(binary_reader.ReadInt64());
			}
			if (num > 2)
			{
				m_TotalTimeSpanCount = binary_reader.ReadInt64();
			}
			else
			{
				m_TotalTimeSpanCount = 0L;
				using (new ReadLockScope(m_TimeSpansLock))
				{
					foreach (TimeSpanList value3 in m_TimeSpans.Values)
					{
						using (new ReadLockScope(value3.Lock))
						{
							TimeSpanIterator timeSpanIterator2 = new TimeSpanIterator(value3.RootTimeSpan);
							while (timeSpanIterator2.MoveNext())
							{
								m_TotalTimeSpanCount++;
							}
						}
					}
				}
			}
			int num30 = 0;
			if (num > 5)
			{
				m_MainThreadId = binary_reader.ReadInt32();
				num30 = binary_reader.ReadInt32();
			}
			else
			{
				using (new ReadLockScope(m_ThreadsLock))
				{
					foreach (int key7 in m_Threads.Keys)
					{
						if (m_Threads[key7].m_Name.ToLower().Contains("main"))
						{
							m_MainThreadId = key7;
						}
					}
				}
				foreach (TimeSpanList value4 in m_TimeSpans.Values)
				{
					using (new ReadLockScope(value4.Lock))
					{
						TimeSpanIterator timeSpanIterator3 = new TimeSpanIterator(value4.RootTimeSpan);
						while (timeSpanIterator3.MoveNext())
						{
							TimeSpanInfo timeSpanInfo2 = GetTimeSpanInfo(timeSpanIterator3.Current.TimeSpanInfoId);
							if (timeSpanInfo2.Core > num30)
							{
								num30 = timeSpanInfo2.Core;
							}
						}
					}
				}
			}
			if (num > 6)
			{
				m_TargetFrameTime = binary_reader.ReadInt64();
				m_FramesInBudget = binary_reader.ReadInt32();
				m_AverageFrameTimeStart = binary_reader.ReadInt64();
				m_AverageFrameTimeCount = binary_reader.ReadInt64();
			}
			else
			{
				m_AverageFrameTimeStart = FirstFrameTime;
				m_AverageFrameTimeCount = FrameCount;
			}
			if (num > 13)
			{
				m_SendBufferSize = binary_reader.ReadInt64();
				m_StringMemorySize = binary_reader.ReadInt64();
				m_MiscMemorySize = binary_reader.ReadInt64();
				m_RecordingFileSize = binary_reader.ReadInt64();
			}
			if (num >= 18)
			{
				m_Platform = (Platform)binary_reader.ReadInt32();
			}
			if (num >= 39)
			{
				lock (m_ScopeColours)
				{
					ReadColours(m_ScopeColours, binary_reader);
				}
				lock (m_CustomStatColours)
				{
					ReadColours(m_CustomStatColours, binary_reader);
				}
			}
			if (num >= 40)
			{
				lock (m_CustomStatGraphs)
				{
					ReadCustomStatInfoArray(m_CustomStatGraphs, binary_reader);
				}
				lock (m_CustomStatUnits)
				{
					ReadCustomStatInfoArray(m_CustomStatUnits, binary_reader);
				}
			}
			if (num >= 19)
			{
				using (new WriteLockScope(m_ProcessNamesLock))
				{
					CoreUtils.Read(m_ProcessNames, binary_reader);
				}
			}
			if (num >= 20)
			{
				using (new WriteLockScope(m_LocalProcessNamesLock))
				{
					CoreUtils.Read(m_LocalProcessNames, binary_reader);
				}
			}
			if (num > 15)
			{
				m_SessionDetails.Read(binary_reader, num);
			}
			if (num >= 46)
			{
				using (new WriteLockScope(m_SessionInfoValuesLock))
				{
					int num31 = binary_reader.ReadInt32();
					for (int num32 = 0; num32 < num31; num32++)
					{
						SessionInfoPair item = default(SessionInfoPair);
						item.m_Name = binary_reader.ReadInt64();
						item.m_Value = binary_reader.ReadInt64();
						m_SessionInfoValues.Add(item);
					}
				}
			}
			////if (num >= 37 && !m_SymLib.Read(binary_reader))
			////{
			////	error = "Failed reading SymLib";
			////	return false;
			////}
			if (num < 24)
			{
				SetLastFrameEndtime(m_Frames[m_Frames.Count - 1].EndTime);
			}
			if (num >= 47)
			{
				lock (m_Modules)
				{
					int num33 = binary_reader.ReadInt32();
					for (int num34 = 0; num34 < num33; num34++)
					{
						Module module = new Module();
						module.Read(binary_reader);
						m_Modules.Add(module);
					}
				}
			}
			if (num >= 44)
			{
				lock (m_MaxFrameTimeLock)
				{
					m_MaxFrameTime = binary_reader.ReadInt64();
					m_MaxFrameIndex = binary_reader.ReadInt32();
				}
			}
			context.Progress.PercentComplete = CoreUtils.GetPercentComplete(binary_reader.BaseStream, length);
			binary_reader.Close();
			m_ReceivedConnectPacket = true;
			UpdateCoreCount(num30);
			SetIsReady();
			context.Progress.PercentComplete = 100;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		return true;
	}

	public bool Write(IFrameProSerialisable session_gui_data, ThreadJobContext context, ref string error)
	{
		return Write(m_SessionFilename, session_gui_data, context, ref error);
	}

	private bool Write(string filename, IFrameProSerialisable session_gui_data, ThreadJobContext context, ref string error)
	{
		int tickCount = Environment.TickCount;
		try
		{
			using FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
			fileStream.Write(m_FileMarker, 0, m_FileMarker.Length);
			BinaryWriter binaryWriter = new BinaryWriter(fileStream);
			Write(binaryWriter, session_gui_data, context);
			binaryWriter.Close();
		}
		catch (Exception ex)
		{
			LogLine(ex.Message);
			error = ex.Message;
			return false;
		}
		if (context.Cancel)
		{
			File.Delete(filename);
		}
		else
		{
			Log.WriteLine("Write time: " + (Environment.TickCount - tickCount));
			m_Saved = true;
		}
		return true;
	}

	private void Write(BinaryWriter binary_writer, IFrameProSerialisable session_gui_data, ThreadJobContext context)
	{
		binary_writer.Write(47);
		session_gui_data.Write(binary_writer);
		binary_writer.Write(m_TimerFrequency);
		binary_writer.Write(FirstFrameTime);
		binary_writer.Write(LastFrameEndTime);
		binary_writer.Write(RecordingContextSwitches);
		binary_writer.Write(m_RemoteProcessId);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			binary_writer.Write(m_CustomStatSessionInfo.Count);
			foreach (CustomStatSessionData value in m_CustomStatSessionInfo.Values)
			{
				value.Write(binary_writer);
			}
		}
		lock (m_CustomStatValueTypes)
		{
			binary_writer.Write(m_CustomStatValueTypes.Count);
			foreach (long key in m_CustomStatValueTypes.Keys)
			{
				binary_writer.Write(key);
				binary_writer.Write((int)m_CustomStatValueTypes[key]);
			}
		}
		lock (m_CustomStatValuePerSecArrays)
		{
			binary_writer.Write(m_CustomStatValuePerSecArrays.Count);
			foreach (long key2 in m_CustomStatValuePerSecArrays.Keys)
			{
				binary_writer.Write(key2);
				m_CustomStatValuePerSecArrays[key2].m_ValuePerSecArray.Write(binary_writer);
			}
		}
		lock (m_LogMessages)
		{
			binary_writer.Write(m_LogMessages.Count);
			foreach (LogMessage logMessage in m_LogMessages)
			{
				logMessage.Write(binary_writer);
			}
		}
		lock (m_Events)
		{
			binary_writer.Write(m_Events.Count);
			foreach (Event @event in m_Events)
			{
				@event.Write(binary_writer);
			}
		}
		lock (m_WaitEvents)
		{
			binary_writer.Write(m_WaitEvents.Count);
			foreach (int key3 in m_WaitEvents.Keys)
			{
				binary_writer.Write(key3);
				TimeArray<WaitEvent> timeArray = m_WaitEvents[key3];
				lock (timeArray)
				{
					binary_writer.Write(timeArray.Count);
					foreach (WaitEvent item in timeArray)
					{
						item.Write(binary_writer);
					}
				}
			}
		}
		using (new ReadLockScope(m_ThreadsLock))
		{
			binary_writer.Write(m_Threads.Count);
			foreach (int key4 in m_Threads.Keys)
			{
				binary_writer.Write(key4);
				m_Threads[key4].Write(binary_writer);
			}
		}
		using (new ReadLockScope(m_FramesLock))
		{
			binary_writer.Write(m_Frames.Count);
			foreach (Frame frame in m_Frames)
			{
				frame.Write(binary_writer);
			}
		}
		using (new ReadLockScope(m_TimeSpanInfoSetLock))
		{
			m_TimeSpanInfoSet.Write(binary_writer);
		}
		using (new ReadLockScope(m_TimeSpansLock))
		{
			binary_writer.Write(m_TimeSpans.Count);
			foreach (int key5 in m_TimeSpans.Keys)
			{
				context.Progress.Push(100 / m_TimeSpans.Count);
				binary_writer.Write(key5);
				m_TimeSpans[key5].Write(binary_writer, context);
				context.Progress.Pop();
			}
		}
		if (context.Cancel)
		{
			return;
		}
		using (new ReadLockScope(m_StringsLock))
		{
			binary_writer.Write(m_Strings.Count);
			foreach (long key6 in m_Strings.Keys)
			{
				binary_writer.Write(key6);
				binary_writer.Write(m_Strings[key6]);
			}
		}
		if (context.Cancel)
		{
			return;
		}
		using (new ReadLockScope(m_SourceInfosLock))
		{
			binary_writer.Write(m_SourceInfos.Count);
			foreach (long key7 in m_SourceInfos.Keys)
			{
				binary_writer.Write(key7);
				m_SourceInfos[key7].Write(binary_writer);
			}
		}
		if (context.Cancel)
		{
			return;
		}
		using (new ReadLockScope(m_ThreadOrderLock))
		{
			binary_writer.Write(m_ThreadOrder.Count);
			foreach (string item2 in m_ThreadOrder)
			{
				binary_writer.Write(item2);
			}
		}
		if (context.Cancel)
		{
			return;
		}
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			binary_writer.Write(m_TimeSpanNames.Count);
			foreach (long timeSpanName in m_TimeSpanNames)
			{
				binary_writer.Write(timeSpanName);
			}
		}
		if (context.Cancel)
		{
			return;
		}
		using (new ReadLockScope(m_TimeSpanFrameStatsLock))
		{
			binary_writer.Write(m_TimeSpanFrameStats.Count);
			foreach (long key8 in m_TimeSpanFrameStats.Keys)
			{
				binary_writer.Write(key8);
				TimeSpanFrameStats timeSpanFrameStats = m_TimeSpanFrameStats[key8];
				using (new ReadLockScope(timeSpanFrameStats.m_Lock))
				{
					timeSpanFrameStats.Write(binary_writer);
				}
			}
		}
		using (new ReadLockScope(m_ContextSwitchArrayLock))
		{
			binary_writer.Write(m_ContextSwitchArray.Count);
			foreach (ContextSwitchArray item3 in m_ContextSwitchArray)
			{
				item3.Write(binary_writer);
			}
		}
		if (context.Cancel)
		{
			return;
		}
		binary_writer.Write(m_TotalTimeSpanCount);
		binary_writer.Write(m_MainThreadId);
		binary_writer.Write(m_MaxCoreIndex);
		binary_writer.Write(m_FramesInBudget);
		binary_writer.Write(m_TargetFrameTime);
		binary_writer.Write(m_AverageFrameTimeStart);
		binary_writer.Write(m_AverageFrameTimeCount);
		binary_writer.Write(m_SendBufferSize);
		binary_writer.Write(m_StringMemorySize);
		binary_writer.Write(m_MiscMemorySize);
		binary_writer.Write(m_RecordingFileSize);
		binary_writer.Write((int)m_Platform);
		lock (m_ScopeColours)
		{
			WriteColours(m_ScopeColours, binary_writer);
		}
		lock (m_CustomStatColours)
		{
			WriteColours(m_CustomStatColours, binary_writer);
		}
		lock (m_CustomStatGraphs)
		{
			WriteCustomStatInfoArray(m_CustomStatGraphs, binary_writer);
		}
		lock (m_CustomStatUnits)
		{
			WriteCustomStatInfoArray(m_CustomStatUnits, binary_writer);
		}
		using (new ReadLockScope(m_ProcessNamesLock))
		{
			CoreUtils.Write(m_ProcessNames, binary_writer);
		}
		using (new ReadLockScope(m_LocalProcessNamesLock))
		{
			CoreUtils.Write(m_LocalProcessNames, binary_writer);
		}
		m_SessionDetails.Write(binary_writer);
		using (new ReadLockScope(m_SessionInfoValuesLock))
		{
			binary_writer.Write(m_SessionInfoValues.Count);
			foreach (SessionInfoPair sessionInfoValue in m_SessionInfoValues)
			{
				binary_writer.Write(sessionInfoValue.m_Name);
				binary_writer.Write(sessionInfoValue.m_Value);
			}
		}
		////m_SymLib.Write(binary_writer, include_all_symbols: true);
		lock (m_Modules)
		{
			binary_writer.Write(m_Modules.Count);
			foreach (Module module in m_Modules)
			{
				module.Write(binary_writer);
			}
		}
		binary_writer.Write(m_MaxFrameTime);
		binary_writer.Write(m_MaxFrameIndex);
		context.Progress.PercentComplete = 100;
	}

	private static void ReadColours(Dictionary<long, Color> colours, BinaryReader binary_reader)
	{
		colours.Clear();
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			long key = binary_reader.ReadInt64();
			Color value = Color.FromArgb(binary_reader.ReadInt32());
			colours[key] = value;
		}
	}

	private static void WriteColours(Dictionary<long, Color> colours, BinaryWriter binary_writer)
	{
		binary_writer.Write(colours.Count);
		foreach (KeyValuePair<long, Color> colour in colours)
		{
			binary_writer.Write(colour.Key);
			binary_writer.Write(colour.Value.ToArgb());
		}
	}

	private static void ReadCustomStatInfoArray(Dictionary<long, long> info, BinaryReader binary_reader)
	{
		info.Clear();
		int num = binary_reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			long key = binary_reader.ReadInt64();
			long value = binary_reader.ReadInt64();
			info[key] = value;
		}
	}

	private static void WriteCustomStatInfoArray(Dictionary<long, long> info, BinaryWriter binary_writer)
	{
		binary_writer.Write(info.Count);
		foreach (KeyValuePair<long, long> item in info)
		{
			binary_writer.Write(item.Key);
			binary_writer.Write(item.Value);
		}
	}

	private void CalculateFramesInBudget()
	{
		m_FramesInBudget = 0;
		using (new ReadLockScope(m_FramesLock))
		{
			int count = m_Frames.Count;
			for (int i = 0; i < count; i++)
			{
				if (m_Frames[i].Duration <= m_TargetFrameTime)
				{
					m_FramesInBudget++;
				}
			}
		}
	}

	public bool Connect()
	{
		m_TcpCllient = new TcpClient();
		Connection currentConnection = m_Settings.GetCurrentConnection();
		m_IP = currentConnection.m_IP;
		Log.WriteLine("Connect: IP: " + m_IP);
		string text = m_IP.Trim().ToLower();
		m_IsLocalIP = text == "localhost" || text == "127.0.0.1";
		IAsyncResult asyncResult = m_TcpCllient.BeginConnect(currentConnection.m_IP, currentConnection.GetPortAsInt(), null, null);
		Log.WriteLine("m_TcpCllient.BeginConnect result: " + asyncResult);
		m_Interactive = currentConnection.Interactive;
		Log.WriteLine("m_Interactive: " + m_Interactive);
		m_StartRecordingContextSwitches = currentConnection.RecordConectSwitches;
		Log.WriteLine("m_StartRecordingContextSwitches: " + m_StartRecordingContextSwitches);
		Log.WriteLine("Waiting " + 5000 + " for connection...");
		asyncResult.AsyncWaitHandle.WaitOne(5000);
		Log.WriteLine("m_TcpCllient.Connected: " + m_TcpCllient.Connected);
		if (!m_TcpCllient.Connected)
		{
			return false;
		}
		Log.WriteLine("Connected!");
		m_Connected = true;
		m_ConnectTime = Environment.TickCount;
		Log.WriteLine("m_ConnectTime: " + m_ConnectTime);
		m_ReceiveThread = new Thread(ReceiveThreadMain);
		m_ReceiveThread.Name = "ReceiveThread";
		m_ReceiveThread.Priority = ThreadPriority.Highest;
		Log.WriteLine("m_ReceiveThread.Start");
		StartProcessEventsThread(ProcessingPacketsMode.Connection);
		Log.WriteLine("StartProcessEventsThread");
		m_SendThread = new Thread(SendThreadMain);
		m_SendThread.Name = "SendThread";
		m_SendThread.Start();
		Log.WriteLine("m_SendThread.Start");
		return true;
	}

	public void StartReceiving()
	{
		m_ReceiveThread.Start();
	}

	public void Disconnect(DisconnectReason reason)
	{
		m_DisconnectReason = reason;
		if (m_TcpCllient != null)
		{
			m_TcpCllient.Close();
			m_TcpCllient = null;
		}
		m_SendEvent.Set();
		m_SendPacketQueue.Clear();
	}

	public void WaitforProcessingToFinish(int timeout)
	{
		m_FinishedProcessingPacketsEvent.WaitOne(timeout);
	}

	public void ForceStopProcessingPackets()
	{
		m_CancelProcessingPackets = true;
	}

	public string GetThreadName(int thread_id)
	{
		using (new ReadLockScope(m_ThreadsLock))
		{
			if (m_Threads.TryGetValue(thread_id, out var value))
			{
				return value.Name;
			}
		}
		return "Thread " + thread_id;
	}

	private void SetFirstFrameTime(long value)
	{
		Interlocked.Exchange(ref m_FirstFrameTime, value);
	}

	public void GetThreads(List<int> threads)
	{
		using (new ReadLockScope(m_TimeSpansLock))
		{
			threads.AddRange(m_TimeSpans.Keys);
		}
	}

	public void GetThreadNames(List<string> thread_names)
	{
		Set<string> set = new Set<string>();
		foreach (string thread_name in thread_names)
		{
			set.Add(thread_name);
		}
		using (new ReadLockScope(m_TimeSpansLock))
		{
			foreach (int key in m_TimeSpans.Keys)
			{
				string threadName = GetThreadName(key);
				if (!set.Contains(threadName))
				{
					thread_names.Add(threadName);
					set.Add(threadName);
				}
			}
		}
	}

	public List<int> GetAllThreadsOfSameName(int thread_id)
	{
		string threadName = GetThreadName(thread_id);
		List<int> list = new List<int>();
		using (new ReadLockScope(m_ThreadsLock))
		{
			foreach (KeyValuePair<int, ThreadInfo> thread in m_Threads)
			{
				int key = thread.Key;
				if (thread.Value.Name == threadName)
				{
					list.Add(key);
				}
			}
			return list;
		}
	}

	public TimeSpan GetTimeSpan(int thread_id, long time)
	{
		TimeSpanList timeSpanList = GetTimeSpanList(thread_id);
		using (new ReadLockScope(timeSpanList.Lock))
		{
			return timeSpanList.GetTimeSpan_Legacy(time);
		}
	}

	public void FillWithRandomData()
	{
		m_TimerFrequency = 155889500L;
		SetFirstFrameTime(10 * m_TimerFrequency);
		using (new WriteLockScope(m_FramesLock))
		{
			m_Frames.SetBlockDuration(m_TimerFrequency);
			long num = FirstFrameTime;
			System.Random random = new System.Random(1234);
			for (int i = 0; i < 40; i++)
			{
				Frame frame = new Frame(i, num);
				m_Frames.Add(frame);
				int num2 = 0;
				System.Random random2 = new System.Random(1234);
				long num3 = random.Next(25, 35) * m_TimerFrequency / 1000;
				FillWithRandomData(num2++, num, num3, random2, 6);
				frame.Finalise(num + num3, 0, 0, 0L, 0L);
				for (int j = 1; j < 15; j++)
				{
					int max_depth = random2.Next(1, 5);
					FillWithRandomData(num2++, num, num3, random2, max_depth);
				}
				num += num3;
			}
			UpdateTargetFrameTime();
		}
	}

	private void FillWithRandomData(int thread_id, long frame_start_time, long frame_duration, System.Random rand, int max_depth)
	{
		TimeSpan time_span = RandomFrameGenerator.GenerateTimeSpans(frame_start_time, frame_duration, rand, max_depth, this);
		TimeSpanList timeSpanList = GetTimeSpanList(thread_id);
		TimeSpanIterator timeSpanIterator = new TimeSpanIterator(time_span);
		while (timeSpanIterator.MoveNext())
		{
			TimeSpan time_span2 = new TimeSpan(timeSpanIterator.Current.TimeSpanInfoId, timeSpanIterator.Current.StartTime, timeSpanIterator.Current.EndTime);
			using (new WriteLockScope(timeSpanList.Lock))
			{
				timeSpanList.Add(time_span2, GetTimerName);
			}
			AddTimeSpanToFrameStats(time_span2, thread_id);
		}
	}

	private void SetLastFrameEndtime(long value)
	{
		Interlocked.Exchange(ref m_LastFrameEndtime, value);
	}

	public string GetString(long string_id)
	{
		string value;
		using (new ReadLockScope(m_StringsLock))
		{
			if (!m_Strings.TryGetValue(string_id, out value))
			{
				value = "ERROR STRING NOT FOUND: " + string_id;
				return value;
			}
		}
		return value;
	}

	private void OnThreadAdded()
	{
		if (this.ThreadAdded != null)
		{
			this.ThreadAdded();
		}
	}

	public List<string> GetThreadOrder()
	{
		using (new ReadLockScope(m_ThreadOrderLock))
		{
			return new List<string>(m_ThreadOrder);
		}
	}

	public bool FrameProStall(Frame frame, Frame next_frame)
	{
		int frameCount = FrameCount;
		long num = ((frameCount != 0) ? ((LastFrameEndTime - FirstFrameTime) / frameCount) : 0);
		return frame.WaitForSendCompleteTime > num;
	}

	public bool FrameProTimeSpanSpike(Frame frame, Frame next_frame)
	{
		if (frame.TimeSpanCount > 1000)
		{
			return frame.TimeSpanCount > 10 * AverageTimeSpanCount;
		}
		return false;
	}

	public void SendConditionalScopeMinTime()
	{
		int min_time = (int)(m_Settings.ConditionalScopeTimeSliderMinTime * m_TimerFrequency / 100000000);
		Send(new ConditionalScopeMinTimePacket(min_time));
	}

	public Color GetThreadColour(string thread_name)
	{
		int num = -1;
		using (new ReadLockScope(m_ThreadOrderLock))
		{
			num = ((m_ThreadOrder.Count != 0) ? m_ThreadOrder.IndexOf(thread_name) : ((!(thread_name == GetThreadName(m_MainThreadId))) ? (-1) : 0));
		}
		if (num != -1 && num < m_ThreadOrderColours.Length)
		{
			return m_ThreadOrderColours[num];
		}
		return CoreUtils.GenerateColour(thread_name, Colours.ThreadColourSaturation);
	}

	public void GetTimeSpanFrameTime(long time_span_name, int frame_index, out long duration, out int count, out long max_duration, out long max_time_per_frame, out long max_count_per_frame)
	{
		duration = 0L;
		count = 0;
		max_duration = 0L;
		max_time_per_frame = 0L;
		max_count_per_frame = 0L;
		if (frame_index < 0)
		{
			return;
		}
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			int count2 = timeSpanFrameStats.m_Frames.Count;
			if (frame_index < count2)
			{
				TimeSpanFrame timeSpanFrame = timeSpanFrameStats.m_Frames[frame_index];
				duration = timeSpanFrame.m_TotalTimeSpanDuration;
				count = timeSpanFrame.m_Count;
			}
			max_duration = timeSpanFrameStats.m_MaxTime;
			max_time_per_frame = timeSpanFrameStats.m_MaxFrameTime;
			max_count_per_frame = timeSpanFrameStats.m_MaxFrameCount;
		}
	}

	public void GetTimeSpanFrameTimes(int start_frame_index, int end_frame_index, long time_span_name, List<FrameTimeSpanStruct> frames)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		int i = start_frame_index;
		_ = 30 * m_TimerFrequency / 1000;
		for (; i < 0; i++)
		{
			frames.Add(default(FrameTimeSpanStruct));
		}
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			for (int count = timeSpanFrameStats.m_Frames.Count; i <= end_frame_index && i < count; i++)
			{
				TimeSpanFrame timeSpanFrame = timeSpanFrameStats.m_Frames[i];
				FrameTimeSpanStruct item = default(FrameTimeSpanStruct);
				item.m_Duration = timeSpanFrame.m_TotalTimeSpanDuration;
				item.m_Count = timeSpanFrame.m_Count;
				frames.Add(item);
			}
		}
		for (; i <= end_frame_index; i++)
		{
			frames.Add(default(FrameTimeSpanStruct));
		}
	}

	public void GetTimeSpanFrameTimes(int start_frame_index, int end_frame_index, long time_span_name, List<FrameValue> values)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		int i = start_frame_index;
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			using (new ReadLockScope(m_FramesLock))
			{
				for (int count = timeSpanFrameStats.m_Frames.Count; i <= end_frame_index && i < count; i++)
				{
					TimeSpanFrame timeSpanFrame = timeSpanFrameStats.m_Frames[i];
					long totalTimeSpanDuration = timeSpanFrame.m_TotalTimeSpanDuration;
					values.Add(new FrameValue(timeSpanFrame.m_FrameEndTime, totalTimeSpanDuration, timeSpanFrame.m_Count));
				}
			}
		}
		for (; i <= end_frame_index; i++)
		{
			values.Add(default(FrameValue));
		}
	}

	private void UpdateTargetFrameTime()
	{
		long num = (long)(m_Settings.TargetFrameMS * (double)m_TimerFrequency / 1000.0);
		if (m_TargetFrameTime != num)
		{
			m_TargetFrameTime = num;
			CalculateFramesInBudget();
		}
	}

	public TimeSpan FindPrevTimeSpan(Set<long> time_spans, long start_time, ref int thread_id)
	{
		TimeSpan timeSpan = null;
		List<int> list = new List<int>();
		GetThreads(list);
		foreach (int item in list)
		{
			TimeSpan timeSpan2 = FindPrevTimeSpan(time_spans, start_time, item);
			if (timeSpan2 != null && (timeSpan == null || timeSpan2.StartTime > timeSpan.StartTime))
			{
				timeSpan = timeSpan2;
				thread_id = item;
			}
		}
		return timeSpan;
	}

	public TimeSpan FindNextTimeSpan(Set<long> time_spans, long start_time, ref int thread_id)
	{
		TimeSpan timeSpan = null;
		List<int> list = new List<int>();
		GetThreads(list);
		foreach (int item in list)
		{
			TimeSpan timeSpan2 = FindNextTimeSpan(time_spans, start_time, item);
			if (timeSpan2 != null && (timeSpan == null || timeSpan2.StartTime < timeSpan.StartTime))
			{
				timeSpan = timeSpan2;
				thread_id = item;
			}
		}
		return timeSpan;
	}

	public TimeSpan FindPrevTimeSpan(Set<long> time_spans, long start_time, int thread_id)
	{
		RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(GetTimeSpan(thread_id, start_time));
		while (rootFirstTimeSpanIterator.MovePrev())
		{
			if (rootFirstTimeSpanIterator.Current.IsTimeSpanEx && ((TimeSpanEx)rootFirstTimeSpanIterator.Current).HiResTimers != null)
			{
				foreach (HiResTimer hiResTimer in (rootFirstTimeSpanIterator.Current as TimeSpanEx).HiResTimers)
				{
					long value = RemapStringId(hiResTimer.m_Name);
					if (time_spans.Contains(value) && rootFirstTimeSpanIterator.Current.StartTime < start_time)
					{
						return rootFirstTimeSpanIterator.Current;
					}
				}
			}
			else if (time_spans.Contains(GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name) && rootFirstTimeSpanIterator.Current.StartTime < start_time)
			{
				return rootFirstTimeSpanIterator.Current;
			}
		}
		return null;
	}

	public TimeSpan FindNextTimeSpan(Set<long> time_spans, long start_time, int thread_id)
	{
		RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(GetTimeSpan(thread_id, start_time));
		while (rootFirstTimeSpanIterator.MoveNext())
		{
			if (rootFirstTimeSpanIterator.Current.IsTimeSpanEx && ((TimeSpanEx)rootFirstTimeSpanIterator.Current).HiResTimers != null)
			{
				foreach (HiResTimer hiResTimer in (rootFirstTimeSpanIterator.Current as TimeSpanEx).HiResTimers)
				{
					long value = RemapStringId(hiResTimer.m_Name);
					if (time_spans.Contains(value) && rootFirstTimeSpanIterator.Current.StartTime > start_time)
					{
						return rootFirstTimeSpanIterator.Current;
					}
				}
			}
			else if (time_spans.Contains(GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name) && rootFirstTimeSpanIterator.Current.StartTime > start_time)
			{
				return rootFirstTimeSpanIterator.Current;
			}
		}
		return null;
	}

	private TimeSpanFrameStats GetTimeSpanFrameStats(long time_span_name)
	{
		TimeSpanFrameStats value;
		using (new ReadLockScope(m_TimeSpanFrameStatsLock))
		{
			if (m_TimeSpanFrameStats.TryGetValue(time_span_name, out value))
			{
				return value;
			}
		}
		value = new TimeSpanFrameStats();
		using (new WriteLockScope(m_TimeSpanFrameStatsLock))
		{
			m_TimeSpanFrameStats[time_span_name] = value;
			return value;
		}
	}

	private void AssignUnassignedTimeSpans()
	{
		int num = m_UnassignedTimeSpans.Count;
		for (int i = 0; i < num; i++)
		{
			UnassignedTimeSpan unassignedTimeSpan = m_UnassignedTimeSpans[i];
			AssignTimeSpanToFrame(unassignedTimeSpan);
			if (unassignedTimeSpan.m_EndTime == unassignedTimeSpan.m_StartTime)
			{
				m_UnassignedTimeSpans[i] = m_UnassignedTimeSpans[num - 1];
				m_UnassignedTimeSpans.RemoveAt(num - 1);
				m_UnassignedTimeSpanAllocator.Free(unassignedTimeSpan);
				i--;
				num--;
			}
		}
	}

	private void AssignTimeSpanToFrame(UnassignedTimeSpan unassigned_time_span)
	{
		long num = unassigned_time_span.m_StartTime;
		long endTime = unassigned_time_span.m_EndTime;
		int frameCount = FrameCount;
		int num2 = frameCount - 1;
		if (frameCount != 0 && unassigned_time_span.m_StartTime < GetFrame(num2).EndTime)
		{
			if (num >= GetFrame(0).StartTime)
			{
				using (new ReadLockScope(m_FramesLock))
				{
					while (m_Frames[num2].StartTime > num && num2 > 0)
					{
						num2--;
					}
				}
				int num3 = num2;
				using (new ReadLockScope(m_FramesLock))
				{
				}
				if (unassigned_time_span.m_HiResTimers != null)
				{
					long num4 = unassigned_time_span.m_TimeSpanStartTime;
					foreach (HiResTimer hiResTimer in unassigned_time_span.m_HiResTimers)
					{
						long num5 = num4 + hiResTimer.m_Duration;
						long time_span_name = RemapStringId(hiResTimer.m_Name);
						TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
						num2 = num3;
						using (new WriteLockScope(timeSpanFrameStats.m_Lock))
						{
							if (hiResTimer.m_Duration > timeSpanFrameStats.m_MaxTime)
							{
								timeSpanFrameStats.m_MaxTime = hiResTimer.m_Duration;
								timeSpanFrameStats.m_MaxTimeStartTime = num;
							}
							TimeSpanThreadInfo timeSpanThreadInfo = timeSpanFrameStats.GetTimeSpanThreadInfo(unassigned_time_span.m_ThreadId);
							if (hiResTimer.m_Duration > timeSpanThreadInfo.m_MaxTime)
							{
								timeSpanThreadInfo.m_MaxTime = hiResTimer.m_Duration;
								timeSpanThreadInfo.m_MaxTimeStartTime = num;
							}
							BlockArray<TimeSpanFrame> frames = timeSpanFrameStats.m_Frames;
							while (num < num5 && num2 < frameCount)
							{
								while (frames.Count <= num2)
								{
									frames.Add(default(TimeSpanFrame));
								}
								Frame frame;
								using (new ReadLockScope(m_FramesLock))
								{
									frame = m_Frames[num2];
								}
								long num6 = Math.Max(frame.StartTime, num4);
								long num7 = Math.Min(frame.EndTime, num5) - num6;
								if (num7 > 0)
								{
									long num8 = hiResTimer.m_Count * num7 / hiResTimer.m_Duration;
									TimeSpanFrame value = frames[num2];
									value.m_FrameEndTime = frame.EndTime;
									value.m_TotalTimeSpanDuration += num7;
									value.m_Count += (int)num8;
									frames[num2] = value;
									timeSpanFrameStats.m_TotalTime += num7;
									timeSpanFrameStats.m_TotalCount += num8;
									timeSpanThreadInfo.m_TotalTime += num7;
									timeSpanThreadInfo.m_TotalCount += num8;
									if (value.m_TotalTimeSpanDuration > timeSpanFrameStats.m_MaxFrameTime)
									{
										timeSpanFrameStats.m_MaxFrameTime = value.m_TotalTimeSpanDuration;
									}
									if (value.m_Count > timeSpanFrameStats.m_MaxFrameCount)
									{
										timeSpanFrameStats.m_MaxFrameCount = value.m_Count;
									}
								}
								num += num7;
								num2++;
							}
						}
						num4 += hiResTimer.m_Duration;
					}
					num = unassigned_time_span.m_EndTime;
				}
				else
				{
					TimeSpanFrameStats timeSpanFrameStats2 = GetTimeSpanFrameStats(unassigned_time_span.m_TimeSpanName);
					long num9 = endTime - num;
					using (new WriteLockScope(timeSpanFrameStats2.m_Lock))
					{
						if (num9 > timeSpanFrameStats2.m_MaxTime)
						{
							timeSpanFrameStats2.m_MaxTime = num9;
							timeSpanFrameStats2.m_MaxTimeStartTime = num;
						}
						TimeSpanThreadInfo timeSpanThreadInfo2 = timeSpanFrameStats2.GetTimeSpanThreadInfo(unassigned_time_span.m_ThreadId);
						if (num9 > timeSpanThreadInfo2.m_MaxTime)
						{
							timeSpanThreadInfo2.m_MaxTime = num9;
							timeSpanThreadInfo2.m_MaxTimeStartTime = num;
						}
						BlockArray<TimeSpanFrame> frames2 = timeSpanFrameStats2.m_Frames;
						while (num < endTime && num2 < frameCount)
						{
							while (frames2.Count <= num2)
							{
								frames2.Add(default(TimeSpanFrame));
							}
							Frame frame2;
							using (new ReadLockScope(m_FramesLock))
							{
								frame2 = m_Frames[num2];
							}
							long num10 = Math.Max(frame2.StartTime, num);
							long num11 = Math.Min(frame2.EndTime, endTime) - num10;
							TimeSpanFrame value2 = frames2[num2];
							value2.m_FrameEndTime = frame2.EndTime;
							value2.m_TotalTimeSpanDuration += num11;
							value2.m_Count++;
							frames2[num2] = value2;
							num += num11;
							timeSpanFrameStats2.m_TotalTime += num11;
							timeSpanFrameStats2.m_TotalCount++;
							timeSpanThreadInfo2.m_TotalTime += num11;
							timeSpanThreadInfo2.m_TotalCount++;
							if (value2.m_TotalTimeSpanDuration > timeSpanFrameStats2.m_MaxFrameTime)
							{
								timeSpanFrameStats2.m_MaxFrameTime = value2.m_TotalTimeSpanDuration;
							}
							if (value2.m_Count > timeSpanFrameStats2.m_MaxFrameCount)
							{
								timeSpanFrameStats2.m_MaxFrameCount = value2.m_Count;
							}
							num2++;
						}
					}
				}
			}
			else
			{
				num = unassigned_time_span.m_EndTime;
			}
		}
		unassigned_time_span.m_StartTime = num;
		unassigned_time_span.m_EndTime = endTime;
	}

	private void AddTimeSpanToFrameStats(TimeSpan time_span, int thread_id)
	{
		UnassignedTimeSpan unassignedTimeSpan = m_UnassignedTimeSpanAllocator.Alloc();
		if (time_span.IsTimeSpanEx && ((TimeSpanEx)time_span).HiResTimers != null)
		{
			unassignedTimeSpan.m_TimeSpanName = -1L;
			TimeSpanEx timeSpanEx = time_span as TimeSpanEx;
			unassignedTimeSpan.m_HiResTimers = new List<HiResTimer>(timeSpanEx.HiResTimers);
		}
		else
		{
			unassignedTimeSpan.m_TimeSpanName = GetTimeSpanInfo(time_span.TimeSpanInfoId).Name;
			unassignedTimeSpan.m_HiResTimers = null;
		}
		unassignedTimeSpan.m_TimeSpanStartTime = time_span.StartTime;
		unassignedTimeSpan.m_StartTime = time_span.StartTime;
		unassignedTimeSpan.m_EndTime = time_span.EndTime;
		unassignedTimeSpan.m_ThreadId = thread_id;
		m_UnassignedTimeSpans.Add(unassignedTimeSpan);
		m_LastTimeSpanTime = time_span.StartTime;
	}

	public long GetTimeSpanFrameAverageTime(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			int count = timeSpanFrameStats.m_Frames.Count;
			return (count != 0) ? (timeSpanFrameStats.m_TotalTime / count) : 0;
		}
	}

	public long GetTimeSpanFrameAverageTime(long time_span_name, int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			if (timeSpanFrameStats.m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					num += timeSpanFrameStats.m_Frames[i].m_TotalTimeSpanDuration;
				}
				int num4 = num3 + 1 - num2;
				num /= num4;
			}
		}
		return num;
	}

	public long GetTimeSpanFrameAverageTimeForThread(long time_span_name, int thread_id)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			TimeSpanThreadInfo timeSpanThreadInfo = timeSpanFrameStats.TryGetTimeSpanThreadInfo(thread_id);
			long num = 0L;
			if (timeSpanThreadInfo != null)
			{
				num = timeSpanThreadInfo.m_TotalTime;
			}
			int count = timeSpanFrameStats.m_Frames.Count;
			return (count != 0) ? (num / count) : 0;
		}
	}

	public long GetTimeSpanMax(long time_span_name, int thread_id, out long start_time)
	{
		start_time = 0L;
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			TimeSpanThreadInfo timeSpanThreadInfo = timeSpanFrameStats.TryGetTimeSpanThreadInfo(thread_id);
			long result = 0L;
			if (timeSpanThreadInfo != null)
			{
				result = timeSpanThreadInfo.m_MaxTime;
				start_time = timeSpanThreadInfo.m_MaxTimeStartTime;
			}
			return result;
		}
	}

	public TimeSpan GetTimeSpan(long name, long start_time)
	{
		List<int> list = new List<int>();
		GetThreads(list);
		foreach (int item in list)
		{
			TimeSpanList timeSpanList = GetTimeSpanList(item);
			using (new ReadLockScope(timeSpanList.Lock))
			{
				TimeSpan timeSpan_Legacy = timeSpanList.GetTimeSpan_Legacy(start_time);
				if (timeSpan_Legacy == null)
				{
					continue;
				}
				RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan_Legacy, is_root: true);
				while (rootFirstTimeSpanIterator.MoveNext())
				{
					if (rootFirstTimeSpanIterator.Current.StartTime == start_time && GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name == name)
					{
						return rootFirstTimeSpanIterator.Current;
					}
				}
			}
		}
		return null;
	}

	public long GetTimeSpanTotalTimeForSession(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			return timeSpanFrameStats.m_TotalTime;
		}
	}

	public long GetTimeSpanFrameMaxTime(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			return timeSpanFrameStats.m_MaxFrameTime;
		}
	}

	public long GetTimeSpanFrameMaxTime(long time_span_name, int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			if (timeSpanFrameStats.m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					long totalTimeSpanDuration = timeSpanFrameStats.m_Frames[i].m_TotalTimeSpanDuration;
					if (totalTimeSpanDuration > num)
					{
						num = totalTimeSpanDuration;
					}
				}
			}
		}
		return num;
	}

	public long GetTimeSpanFrameMaxCount(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			return timeSpanFrameStats.m_MaxFrameCount;
		}
	}

	public double GetTimeSpanFrameAverageCount(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			int count = timeSpanFrameStats.m_Frames.Count;
			return (count != 0) ? ((double)timeSpanFrameStats.m_TotalCount / (double)count) : 0.0;
		}
	}

	public double GetTimeSpanFrameAverageCount(long time_span_name, int start_frame_index, int end_frame_index)
	{
		int num = 0;
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			if (timeSpanFrameStats.m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, timeSpanFrameStats.m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					num += timeSpanFrameStats.m_Frames[i].m_Count;
				}
				int num4 = num3 + 1 - num2;
				num /= num4;
			}
		}
		return num;
	}

	public long GetTimeSpanMax(long time_span_name, out long start_time)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			start_time = timeSpanFrameStats.m_MaxTimeStartTime;
			return timeSpanFrameStats.m_MaxTime;
		}
	}

	public long GetTimeSpanAverage(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			return (timeSpanFrameStats.m_TotalCount != 0L) ? (timeSpanFrameStats.m_TotalTime / timeSpanFrameStats.m_TotalCount) : 0;
		}
	}

	public long GetTimeSpanAverage(long time_span_name, int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		long num4 = 0L;
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count == 0)
			{
				return 0L;
			}
			int index = Math.Min(start_frame_index, m_Frames.Count - 1);
			num3 = m_Frames[index].StartTime;
			int index2 = Math.Min(end_frame_index, m_Frames.Count - 1);
			num4 = m_Frames[index2].EndTime;
		}
		List<int> list = new List<int>();
		GetThreads(list);
		using (new ReadLockScope(m_TimeSpansLock))
		{
			foreach (int item in list)
			{
				TimeSpan timeSpanAtTime = m_TimeSpans[item].GetTimeSpanAtTime(num3);
				if (timeSpanAtTime == null)
				{
					continue;
				}
				RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpanAtTime);
				while (rootFirstTimeSpanIterator.MoveNext() && rootFirstTimeSpanIterator.Current.StartTime < num4)
				{
					if (rootFirstTimeSpanIterator.Current.TimeSpanInfoId != TimeSpanInfo.InvalidInfoId && rootFirstTimeSpanIterator.Current.StartTime >= num3 && rootFirstTimeSpanIterator.Current.EndTime <= num4 && GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name == time_span_name)
					{
						num += rootFirstTimeSpanIterator.Current.Duration;
						num2++;
					}
				}
			}
		}
		return num / num2;
	}

	public long GetTimeSpanMax(long time_span_name, int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count == 0)
			{
				return 0L;
			}
			int index = Math.Min(start_frame_index, m_Frames.Count - 1);
			num2 = m_Frames[index].StartTime;
			int index2 = Math.Min(end_frame_index, m_Frames.Count - 1);
			num3 = m_Frames[index2].EndTime;
		}
		List<int> list = new List<int>();
		GetThreads(list);
		using (new ReadLockScope(m_TimeSpansLock))
		{
			foreach (int item in list)
			{
				TimeSpan timeSpanAtTime = m_TimeSpans[item].GetTimeSpanAtTime(num2);
				if (timeSpanAtTime == null)
				{
					continue;
				}
				RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpanAtTime);
				while (rootFirstTimeSpanIterator.MoveNext() && rootFirstTimeSpanIterator.Current.StartTime < num3)
				{
					if (rootFirstTimeSpanIterator.Current.TimeSpanInfoId != TimeSpanInfo.InvalidInfoId && rootFirstTimeSpanIterator.Current.StartTime >= num2 && rootFirstTimeSpanIterator.Current.EndTime <= num3 && GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name == time_span_name && rootFirstTimeSpanIterator.Current.Duration > num)
					{
						num = rootFirstTimeSpanIterator.Current.Duration;
					}
				}
			}
			return num;
		}
	}

	public double GetTimeSpanFrameAverageCountForThread(long time_span_name, int thread_id)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			TimeSpanThreadInfo timeSpanThreadInfo = timeSpanFrameStats.TryGetTimeSpanThreadInfo(thread_id);
			if (timeSpanThreadInfo != null)
			{
				int count = timeSpanFrameStats.m_Frames.Count;
				return (count != 0) ? ((double)timeSpanThreadInfo.m_TotalCount / (double)count) : 0.0;
			}
			return 0.0;
		}
	}

	public long GetTimeSpanTotalCountForSession(long time_span_name)
	{
		TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(time_span_name);
		using (new ReadLockScope(timeSpanFrameStats.m_Lock))
		{
			return timeSpanFrameStats.m_TotalCount;
		}
	}

	public long GetTimeSpanNameId(string name)
	{
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			foreach (long timeSpanName in m_TimeSpanNames)
			{
				if (GetTimerName(timeSpanName) == name)
				{
					return timeSpanName;
				}
			}
		}
		return -1L;
	}

	private static void WriteCSVLine(List<string> info_lines, string line, StreamWriter stream)
	{
		if (info_lines.Count != 0)
		{
			string text = info_lines[0];
			info_lines.RemoveAt(0);
			stream.Write(text + "," + line);
		}
		else
		{
			stream.Write(",,," + line);
		}
		stream.Write("\n");
	}

	public void WriteFrameGraphToCSV(string filename)
	{
		using StreamWriter streamWriter = new StreamWriter(filename);
		double num = m_TimerFrequency;
		using (new ReadLockScope(m_FramesLock))
		{
			foreach (Frame frame in m_Frames)
			{
				string value = (double)frame.Duration * 1000.0 / num + ",";
				streamWriter.WriteLine(value);
			}
		}
	}

	public void WriteToCSV(string filename)
	{
		StreamWriter streamWriter = new StreamWriter(filename);
		List<string> list = new List<string>();
		list.Add("Session Name," + m_SessionDetails.m_Name + ",");
		list.Add("Build Id," + m_SessionDetails.m_BuildId + ",");
		list.Add("Date," + m_SessionDetails.m_Date + ",");
		double num = m_TimerFrequency;
		string text = "";
		text += "Frame,";
		text += ",";
		Set<long> set;
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			set = new Set<long>(m_TimeSpanNames);
		}
		List<long> list2 = new List<long>();
		foreach (long item in set)
		{
			list2.Add(item);
		}
		list2.Sort(TimerNameComparer);
		foreach (long item2 in list2)
		{
			string timerName = GetTimerName(item2);
			text = text + timerName + ",";
		}
		List<TimeSpanFrameStats> list3 = new List<TimeSpanFrameStats>();
		foreach (long item3 in list2)
		{
			TimeSpanFrameStats timeSpanFrameStats = GetTimeSpanFrameStats(item3);
			list3.Add(timeSpanFrameStats);
		}
		Dictionary<long, List<double>> dictionary = new Dictionary<long, List<double>>();
		List<string> list4 = new List<string>();
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			foreach (long key in m_CustomStatSessionInfo.Keys)
			{
				List<double> list6 = (dictionary[key] = new List<double>());
				string @string = GetString(key);
				CustomStatInfo customStatInfo = m_Settings.GetCustomStatInfo(@string);
				bool acc = customStatInfo != null && (customStatInfo.m_XAxisMode == CustomStatXAxisMode.AccFrame || customStatInfo.m_XAxisMode == CustomStatXAxisMode.AccTime);
				if (customStatInfo == null || customStatInfo.m_XAxisMode == CustomStatXAxisMode.Frame)
				{
					List<FrameValue> list7 = new List<FrameValue>();
					GetCustomStats(0, FrameCount - 1, key, acc, list7);
					foreach (FrameValue item4 in list7)
					{
						list6.Add(item4.m_Value);
					}
				}
				else
				{
					List<PerSecValue> list8 = new List<PerSecValue>();
					GetCustomStatsPerSec(key, FirstFrameTime, LastFrameEndTime, acc, list8, out var _);
					foreach (PerSecValue item5 in list8)
					{
						list6.Add(item5.m_Value);
					}
				}
				list4.Add(@string);
			}
		}
		list4.Sort();
		text += ",";
		foreach (string item6 in list4)
		{
			string text2 = item6;
			CustomStatInfo customStatInfo2 = m_Settings.GetCustomStatInfo(item6);
			if (customStatInfo2 != null && customStatInfo2.m_XAxisMode == CustomStatXAxisMode.Time)
			{
				text2 += " (per sec)";
			}
			text = text + text2 + ",";
		}
		WriteCSVLine(list, text, streamWriter);
		using (new ReadLockScope(m_FramesLock))
		{
			int num2 = 0;
			foreach (Frame frame in m_Frames)
			{
				string text3 = (double)frame.Duration * 1000.0 / num + ",";
				text3 += ",";
				foreach (TimeSpanFrameStats item7 in list3)
				{
					using (new ReadLockScope(item7.m_Lock))
					{
						double num3 = 0.0;
						if (num2 < item7.m_Frames.Count)
						{
							num3 = (double)item7.m_Frames[num2].m_TotalTimeSpanDuration * 1000.0 / num;
						}
						text3 = text3 + num3 + ",";
					}
				}
				text3 += ",";
				foreach (string item8 in list4)
				{
					long customStatNameId = GetCustomStatNameId(item8);
					List<double> list9 = dictionary[customStatNameId];
					if (num2 < list9.Count)
					{
						double num4 = list9[num2];
						if (GetCustomStatUnit(customStatNameId) == "cycles")
						{
							num4 = num4 * 1000.0 / num;
						}
						text3 = text3 + num4 + ",";
					}
					else
					{
						text3 += ",";
					}
				}
				WriteCSVLine(list, text3, streamWriter);
				num2++;
			}
		}
		streamWriter.Close();
	}

	private int TimerNameComparer(long name_a, long name_b)
	{
		string timerName = GetTimerName(name_a);
		string timerName2 = GetTimerName(name_b);
		return timerName.CompareTo(timerName2);
	}

	public List<int> GetThreadIds()
	{
		using (new ReadLockScope(m_ThreadsLock))
		{
			return new List<int>(m_Threads.Keys);
		}
	}

	public List<int> GetThreadIds(string thread_name)
	{
		List<int> list = new List<int>();
		using (new ReadLockScope(m_ThreadsLock))
		{
			foreach (int key in m_Threads.Keys)
			{
				if (m_Threads[key].Name == thread_name)
				{
					list.Add(key);
				}
			}
			return list;
		}
	}

	public float AverageScopesPerFrame(string thread_name)
	{
		int frameCount = FrameCount;
		if (frameCount == 0)
		{
			return 0f;
		}
		long num = 0L;
		List<int> threadIds = GetThreadIds(thread_name);
		using (new ReadLockScope(m_TimeSpansLock))
		{
			foreach (int item in threadIds)
			{
				TimeSpanList timeSpanList = m_TimeSpans[item];
				using (new ReadLockScope(timeSpanList.Lock))
				{
					num += timeSpanList.TotalTimeSpanCount;
				}
			}
		}
		return (float)((double)num / (double)frameCount);
	}

	public void RequestRecordedData()
	{
		Send(new RequestRecordedDataPacket());
	}

	public TimeSpanInfo GetTimeSpanInfo(int time_span_info_id)
	{
		using (new ReadLockScope(m_TimeSpanInfoSetLock))
		{
			if (m_TimeSpanInfoSet.Count == int.MaxValue && !m_ShownTimeSpanInfoErrorMessageBox)
			{
				m_ShownTimeSpanInfoErrorMessageBox = true;
				string text = "ERROR: FramePro can not handle more than 0xffff unique scopes. Additional scopes will have incorrect names.";
				if (this.ShowError != null)
				{
					this.ShowWarning(text);
				}
				LogLine(text);
			}
			return m_TimeSpanInfoSet.GetTimeSpanInfo(time_span_info_id);
		}
	}

	public int GetTimeSpanInfoId(long name, long source_info, int core, int callstack_id)
	{
		using (new WriteLockScope(m_TimeSpanInfoSetLock))
		{
			return m_TimeSpanInfoSet.GetId(name, source_info, core, callstack_id);
		}
	}

	public int FindPrevSpike(long time)
	{
		long timerFrequency = TimerFrequency;
		int num = GetFrameIndex(time) - 1;
		using (new ReadLockScope(m_FramesLock))
		{
			while (num >= 0)
			{
				if (CoreUtils.GetFrameTimeCategory(m_Frames[num].Duration, timerFrequency, m_Settings.TargetFrameMS) == CoreUtils.FrameTimeCategory.Alert)
				{
					return num;
				}
				num--;
			}
		}
		return -1;
	}

	public int FindNextSpike(long time)
	{
		long timerFrequency = TimerFrequency;
		int i = GetFrameIndex(time) + 1;
		using (new ReadLockScope(m_FramesLock))
		{
			for (; i < m_Frames.Count; i++)
			{
				if (CoreUtils.GetFrameTimeCategory(m_Frames[i].Duration, timerFrequency, m_Settings.TargetFrameMS) == CoreUtils.FrameTimeCategory.Alert)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public TimeSpan FindPrevTimeSpan(long time, int thread_id, TimeSpan matching_time_span)
	{
		if (matching_time_span == null || matching_time_span.TimeSpanInfoId == TimeSpanInfo.InvalidInfoId)
		{
			return null;
		}
		using (new ReadLockScope(m_TimeSpansLock))
		{
			TimeSpan timeSpan = m_TimeSpans[thread_id].GetTimeSpanAtTime(time);
			if (timeSpan == null)
			{
				return null;
			}
			while (timeSpan.ChildCount != 0)
			{
				timeSpan = timeSpan.Children;
				while (timeSpan.Next != null)
				{
					timeSpan = timeSpan.Next;
				}
			}
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpan);
			if (timeSpan != null && matching_time_span != null)
			{
				long name = GetTimeSpanInfo(matching_time_span.TimeSpanInfoId).Name;
				while (rootFirstTimeSpanIterator.MovePrev())
				{
					if (rootFirstTimeSpanIterator.Current.EndTime < time && GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name == name)
					{
						return rootFirstTimeSpanIterator.Current;
					}
				}
				return null;
			}
			while (rootFirstTimeSpanIterator.MovePrev())
			{
				if (rootFirstTimeSpanIterator.Current.EndTime > time)
				{
					return rootFirstTimeSpanIterator.Current;
				}
			}
			return null;
		}
	}

	public TimeSpan FindNextTimeSpan(long time, int thread_id, TimeSpan matching_time_span)
	{
		if (matching_time_span == null || matching_time_span.TimeSpanInfoId == TimeSpanInfo.InvalidInfoId)
		{
			return null;
		}
		using (new ReadLockScope(m_TimeSpansLock))
		{
			TimeSpan timeSpanAtTime = m_TimeSpans[thread_id].GetTimeSpanAtTime(time);
			if (timeSpanAtTime == null)
			{
				return null;
			}
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(timeSpanAtTime);
			if (timeSpanAtTime != null && matching_time_span != null)
			{
				long name = GetTimeSpanInfo(matching_time_span.TimeSpanInfoId).Name;
				while (rootFirstTimeSpanIterator.MoveNext())
				{
					if (rootFirstTimeSpanIterator.Current.StartTime >= time && GetTimeSpanInfo(rootFirstTimeSpanIterator.Current.TimeSpanInfoId).Name == name)
					{
						return rootFirstTimeSpanIterator.Current;
					}
				}
				return null;
			}
			while (rootFirstTimeSpanIterator.MoveNext())
			{
				if (rootFirstTimeSpanIterator.Current.StartTime >= time)
				{
					return rootFirstTimeSpanIterator.Current;
				}
			}
			return null;
		}
	}

	public List<ScopeSessionStats> GetTimeSpanStats()
	{
		List<ScopeSessionStats> list = new List<ScopeSessionStats>();
		_ = FrameCount;
		List<long> list2 = null;
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			list2 = new List<long>(m_TimeSpanNames);
		}
		foreach (long item2 in list2)
		{
			ScopeSessionStats item = default(ScopeSessionStats);
			item.m_Name = GetTimerName(item2);
			item.m_TotalTime = GetTimeSpanTotalTimeForSession(item2);
			item.m_TotalCount = GetTimeSpanTotalCountForSession(item2);
			item.m_MaxTimePerFrame = GetTimeSpanFrameMaxTime(item2);
			item.m_MaxCountPerFrame = GetTimeSpanFrameMaxCount(item2);
			list.Add(item);
		}
		return list;
	}

	public string GetProcessName(int process_id)
	{
		if (process_id == -1)
		{
			return "unknown";
		}
		if (process_id == m_RemoteProcessId && m_Platform == Platform.XBoxOne)
		{
			return SessionDetails.m_Name;
		}
		using (new ReadLockScope(m_ProcessNamesLock))
		{
			if (m_ProcessNames.TryGetValue(process_id, out var value))
			{
				return GetString(value);
			}
		}
		using (new ReadLockScope(m_LocalProcessNamesLock))
		{
			if (m_LocalProcessNames.TryGetValue(process_id, out var value2))
			{
				return value2;
			}
		}
		return "Process: " + process_id;
	}

	public bool IsSessionThread(int thread_id)
	{
		using (new ReadLockScope(m_ThreadsLock))
		{
			return m_Threads.ContainsKey(thread_id);
		}
	}

	private void CalculateSessionStats(int start_frame_index, int end_frame_index)
	{
		long startTime;
		using (new ReadLockScope(m_FramesLock))
		{
			startTime = m_Frames[start_frame_index].StartTime;
		}
		lock (m_AverageLock)
		{
			m_AverageFrameTimeStart = startTime;
			m_AverageFrameTimeCount = end_frame_index + 1 - start_frame_index;
		}
		CalculateFramesInBudget();
	}

	public bool CopyTo(Session session, long start_time, long end_time, ThreadJobContext thread_job_context, ref string error)
	{
		if (start_time >= LastFrameEndTime)
		{
			error = "No frames in selected range";
			return false;
		}
		int index = m_Frames.GetIndex(start_time);
		int end_frame_index = ((end_time >= LastFrameEndTime) ? (FrameCount - 1) : m_Frames.GetIndex(end_time));
		session.m_ReceivedConnectPacket = m_ReceivedConnectPacket;
		session.m_MainThreadId = m_MainThreadId;
		session.m_StringMemorySize = m_StringMemorySize;
		session.m_MiscMemorySize = m_MiscMemorySize;
		session.m_ReceivedFrameProLibVersion = m_ReceivedFrameProLibVersion;
		session.m_TotalTimeSpanCount = m_TotalTimeSpanCount;
		session.m_SessionDetails = CoreUtils.Clone(m_SessionDetails);
		session.m_TimerFrequency = m_TimerFrequency;
		session.m_RecordingContextSwitches = m_RecordingContextSwitches;
		session.m_RemoteProcessId = m_RemoteProcessId;
		session.UpdateCoreCount(m_MaxCoreIndex);
		session.m_FramesInBudget = m_FramesInBudget;
		session.m_Interactive = m_Interactive;
		session.m_StartRecordingContextSwitches = m_StartRecordingContextSwitches;
		session.m_FrameTimeSpanCount = m_FrameTimeSpanCount;
		session.m_FrameBytesSentCount = m_FrameBytesSentCount;
		session.m_StartedXBoxOneEtlTrace = m_StartedXBoxOneEtlTrace;
		session.m_Platform = m_Platform;
		session.m_IP = m_IP;
		session.m_IsLocalIP = m_IsLocalIP;
		session.m_RecordingContextSwitchesStartedSucessfully = m_RecordingContextSwitchesStartedSucessfully;
		session.m_LastTimeSpanTime = m_LastTimeSpanTime;
		thread_job_context.Progress.Set(10L, 100L);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			foreach (long key in m_CustomStatSessionInfo.Keys)
			{
				session.m_CustomStatSessionInfo[key] = CoreUtils.Clone(m_CustomStatSessionInfo[key]);
			}
		}
		lock (m_CustomStatValueTypes)
		{
			foreach (long key2 in m_CustomStatValueTypes.Keys)
			{
				session.m_CustomStatValueTypes[key2] = m_CustomStatValueTypes[key2];
			}
		}
		using (new ReadLockScope(m_ThreadsLock))
		{
			foreach (int key3 in m_Threads.Keys)
			{
				session.m_Threads[key3] = CoreUtils.Clone(m_Threads[key3]);
			}
		}
		thread_job_context.Progress.Set(20L, 100L);
		using (new ReadLockScope(m_FramesLock))
		{
			session.m_Frames = m_Frames.Clone(index, end_frame_index);
			session.SetFirstFrameTime(session.m_Frames[0].StartTime);
			session.SetLastFrameEndtime(session.m_Frames[session.m_Frames.Count - 1].EndTime);
		}
		thread_job_context.Progress.Set(30L, 100L);
		using (new ReadLockScope(m_TimeSpanFrameStatsLock))
		{
			foreach (long key4 in m_TimeSpanFrameStats.Keys)
			{
				TimeSpanFrameStats timeSpanFrameStats = m_TimeSpanFrameStats[key4];
				using (new ReadLockScope(timeSpanFrameStats.m_Lock))
				{
					session.m_TimeSpanFrameStats[key4] = timeSpanFrameStats.Clone(index, end_frame_index);
				}
			}
		}
		thread_job_context.Progress.Set(40L, 100L);
		using (new ReadLockScope(m_TimeSpanInfoSetLock))
		{
			session.m_TimeSpanInfoSet = CoreUtils.Clone(m_TimeSpanInfoSet);
		}
		thread_job_context.Progress.Set(50L, 100L);
		using (new ReadLockScope(m_TimeSpansLock))
		{
			foreach (int key5 in m_TimeSpans.Keys)
			{
				session.m_TimeSpans[key5] = m_TimeSpans[key5].Clone(session.m_TimeSpanInfoSet, start_time, end_time);
			}
		}
		thread_job_context.Progress.Set(60L, 100L);
		using (new ReadLockScope(m_StringsLock))
		{
			session.m_Strings = new Dictionary<long, string>(m_Strings);
		}
		thread_job_context.Progress.Set(70L, 100L);
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			session.m_TimeSpanNames = new Set<long>(m_TimeSpanNames);
		}
		thread_job_context.Progress.Set(80L, 100L);
		using (new ReadLockScope(m_SourceInfosLock))
		{
			foreach (long key6 in m_SourceInfos.Keys)
			{
				session.m_SourceInfos[key6] = CoreUtils.Clone(m_SourceInfos[key6]);
			}
		}
		thread_job_context.Progress.Set(90L, 100L);
		using (new ReadLockScope(m_ThreadOrderLock))
		{
			session.m_ThreadOrder = new List<string>(m_ThreadOrder);
		}
		using (new ReadLockScope(m_ContextSwitchArrayLock))
		{
			foreach (ContextSwitchArray item in m_ContextSwitchArray)
			{
				session.m_ContextSwitchArray.Add(item.Clone(start_time, end_time));
			}
		}
		using (new ReadLockScope(m_ProcessNamesLock))
		{
			session.m_ProcessNames = m_ProcessNames;
		}
		using (new ReadLockScope(m_LocalProcessNamesLock))
		{
			session.m_LocalProcessNames = new Dictionary<int, string>(m_LocalProcessNames);
		}
		session.CalculateSessionStats(0, session.FrameCount);
		session.SetIsReady();
		session.m_FinishedProcessingPacketsEvent.Set();
		thread_job_context.Progress.Set(100L, 100L);
		return true;
	}

	public Color GetScopeColour(long name_id)
	{
		Color colour = Color.Red;
		string @string = GetString(name_id);
		if (!m_Settings.GetScopeColour(@string, ref colour))
		{
			bool flag = false;
			lock (m_ScopeColours)
			{
				flag = m_ScopeColours.TryGetValue(name_id, out colour);
			}
			if (!flag)
			{
				colour = CoreUtils.GenerateColour(@string, Colours.ScopeColourSaturation);
			}
		}
		return colour;
	}

	public void SetScopeColour(string name, Color colour)
	{
		m_Settings.SetScopeColour(name, colour);
		if (this.ScopeColourChanged != null)
		{
			this.ScopeColourChanged();
		}
	}

	public void SetCustomStatColour(string name, Color colour)
	{
		m_Settings.SetCustomStatColour(name, colour);
		if (this.CustomStatColourChanged != null)
		{
			this.CustomStatColourChanged();
		}
	}

	public Color GetCustomStatColour(long name)
	{
		Color colour = Color.Red;
		string @string = GetString(name);
		if (!m_Settings.GetCustomStatColour(@string, ref colour))
		{
			bool flag = false;
			lock (m_CustomStatColours)
			{
				flag = m_CustomStatColours.TryGetValue(name, out colour);
			}
			if (!flag)
			{
				colour = CoreUtils.GenerateColour(@string, Colours.CustomStatColourSaturation);
			}
		}
		return colour;
	}

	public CustomStatValueType GetCustomstatValueType(long name)
	{
		lock (m_CustomStatValueTypes)
		{
			if (m_CustomStatValueTypes.TryGetValue(name, out var value))
			{
				return value;
			}
		}
		return CustomStatValueType.Double;
	}

	public List<CustomStatSessionData> GetCustomStats()
	{
		List<CustomStatSessionData> list = new List<CustomStatSessionData>();
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			foreach (CustomStatSessionData value in m_CustomStatSessionInfo.Values)
			{
				list.Add(new CustomStatSessionData(value));
			}
			return list;
		}
	}

	public double GetCustomStatMinPerFrame(long name)
	{
		long num = RemapStringId(name);
		CustomStatValueType customstatValueType = GetCustomstatValueType(num);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			CustomStatSessionData value = new CustomStatSessionData();
			if (m_CustomStatSessionInfo.TryGetValue(num, out value))
			{
				switch (customstatValueType)
				{
				case CustomStatValueType.Int64:
					return value.m_MinValuePerFrameInt64;
				case CustomStatValueType.Double:
					return value.m_MinValuePerFrameDouble;
				}
			}
		}
		return 0.0;
	}

	public double GetCustomStatMinAccPerFrame(long name)
	{
		long num = RemapStringId(name);
		CustomStatValueType customstatValueType = GetCustomstatValueType(num);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			CustomStatSessionData value = new CustomStatSessionData();
			if (m_CustomStatSessionInfo.TryGetValue(num, out value))
			{
				switch (customstatValueType)
				{
				case CustomStatValueType.Int64:
					return value.m_AccMinValuePerFrameInt64;
				case CustomStatValueType.Double:
					return value.m_AccMinValuePerFrameDouble;
				}
			}
		}
		return 0.0;
	}

	public double GetCustomStatMaxPerFrame(long name)
	{
		long num = RemapStringId(name);
		CustomStatValueType customstatValueType = GetCustomstatValueType(num);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			CustomStatSessionData value = new CustomStatSessionData();
			if (m_CustomStatSessionInfo.TryGetValue(num, out value))
			{
				switch (customstatValueType)
				{
				case CustomStatValueType.Int64:
					return value.m_MaxValuePerFrameInt64;
				case CustomStatValueType.Double:
					return value.m_MaxValuePerFrameDouble;
				}
			}
		}
		return 0.0;
	}

	public double GetCustomStatMaxAccPerFrame(long name)
	{
		long num = RemapStringId(name);
		CustomStatValueType customstatValueType = GetCustomstatValueType(num);
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			CustomStatSessionData value = new CustomStatSessionData();
			if (m_CustomStatSessionInfo.TryGetValue(num, out value))
			{
				switch (customstatValueType)
				{
				case CustomStatValueType.Int64:
					return value.m_AccMaxValuePerFrameInt64;
				case CustomStatValueType.Double:
					return value.m_AccMaxValuePerFrameDouble;
				}
			}
		}
		return 0.0;
	}

	public long GetCustomStatNameId(string name)
	{
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			foreach (CustomStatSessionData value in m_CustomStatSessionInfo.Values)
			{
				if (GetString(value.Name) == name)
				{
					return value.Name;
				}
			}
		}
		return -1L;
	}

	public void GetCustomStats(int start_frame_index, int end_frame_index, long custom_stat_name, bool acc, List<FrameValue> points)
	{
		int i = start_frame_index;
		CustomStatValueType customstatValueType = GetCustomstatValueType(custom_stat_name);
		using (new ReadLockScope(m_FramesLock))
		{
			int count = m_Frames.Count;
			int num = Math.Min(end_frame_index, count - 1);
			if (acc)
			{
				for (; i <= num; i++)
				{
					Frame frame = m_Frames[i];
					double value = customstatValueType switch
					{
						CustomStatValueType.Int64 => frame.GetCustomStatAccValueInt64(custom_stat_name), 
						CustomStatValueType.Double => frame.GetCustomStatAccValueDouble(custom_stat_name), 
						_ => 0.0, 
					};
					long customStatCount = frame.GetCustomStatCount(custom_stat_name);
					points.Add(new FrameValue(frame.EndTime, value, customStatCount));
				}
			}
			else
			{
				for (; i <= num; i++)
				{
					Frame frame2 = m_Frames[i];
					double value2 = customstatValueType switch
					{
						CustomStatValueType.Int64 => frame2.GetCustomStatValueInt64(custom_stat_name), 
						CustomStatValueType.Double => frame2.GetCustomStatValueDouble(custom_stat_name), 
						_ => 0.0, 
					};
					long customStatCount2 = frame2.GetCustomStatCount(custom_stat_name);
					points.Add(new FrameValue(frame2.EndTime, value2, customStatCount2));
				}
			}
		}
		if (points.Count != 0)
		{
			long num2 = points[points.Count - 1].m_FrameEndTime;
			for (; i <= end_frame_index; i++)
			{
				num2 += 30;
				FrameValue item = default(FrameValue);
				item.m_FrameEndTime = num2;
				points.Add(item);
			}
		}
	}

	public void GetLogMessages(int start_index, List<LogMessage> log_messages)
	{
		for (int i = start_index; i < m_LogMessages.Count; i++)
		{
			log_messages.Add(m_LogMessages[i]);
		}
	}

	public void SampleFrameTimesForSession(long[] frame_times)
	{
		int num = frame_times.Length;
		using (new ReadLockScope(m_FramesLock))
		{
			double num2 = (double)m_Frames.Count / (double)num;
			int num3 = (int)((double)m_Frames.Count / num2);
			for (int i = 0; i < num3; i++)
			{
				frame_times[i] = m_Frames[(int)((double)i * num2)].Duration;
			}
		}
	}

	public void SampleFrameTimesForSessionByTime(long[] frame_times, XToTimeDelegate x_to_time_delegate)
	{
		int num = frame_times.Length;
		using (new ReadLockScope(m_FramesLock))
		{
			for (int i = 0; i < num; i++)
			{
				long time = x_to_time_delegate(i);
				int frameIndex_NoLock = GetFrameIndex_NoLock(time);
				long num2 = 0L;
				if (frameIndex_NoLock >= 0 && frameIndex_NoLock < m_Frames.Count)
				{
					num2 = m_Frames[frameIndex_NoLock].Duration;
				}
				frame_times[i] = num2;
			}
		}
	}

	private TimeArray<WaitEvent> GetWaitEvents(int thread_id)
	{
		TimeArray<WaitEvent> value;
		lock (m_WaitEvents)
		{
			if (!m_WaitEvents.TryGetValue(thread_id, out value))
			{
				value = new TimeArray<WaitEvent>(m_TimerFrequency);
				m_WaitEvents[thread_id] = value;
			}
		}
		return value;
	}

	////public string[] GetCallstack(int callstack_id)
	////{
	////	LoadModuleSymbols();
	////	string[] p_filters = m_Settings.CallstackFilters.ToArray();
	////	return m_SymLib.GetCallstackSymbols(callstack_id, p_filters);
	////}

	public bool LoadContextSwitchFile(string filename)
	{
		_ = m_Platform;
		if (6 == 6)
		{
			List<ContextSwitch> list = new List<ContextSwitch>();
			if (!AndroidContextSwitchFileLoader.Load(filename, list, m_Log))
			{
				return false;
			}
			foreach (ContextSwitch item in list)
			{
				AddContextSwitch(item);
			}
			m_RecordingContextSwitches = true;
			return true;
		}
		return false;
	}

	public string GetCustomStatGraph(long stat_name)
	{
		lock (m_CustomStatGraphs)
		{
			if (m_CustomStatGraphs.TryGetValue(stat_name, out var value))
			{
				return GetString(value);
			}
		}
		return "default";
	}

	public string GetCustomStatUnit(long stat_name)
	{
		lock (m_CustomStatUnits)
		{
			if (m_CustomStatUnits.TryGetValue(stat_name, out var value))
			{
				return GetString(value);
			}
		}
		return "none";
	}

	private void HandleNewStringId(long string_id, StringLiteralType string_literal_type)
	{
		bool flag = false;
		using (new ReadLockScope(m_StringsLock))
		{
			if (!m_Strings.ContainsKey(string_id))
			{
				flag = true;
			}
		}
		if (flag)
		{
			using (new WriteLockScope(m_StringsLock))
			{
				m_Strings[string_id] = "pending " + string_id;
			}
			RequestStringLiteralValue(string_id, string_literal_type);
		}
	}

	private bool AddTimeSpanName(long name)
	{
		bool flag = false;
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			flag = !m_TimeSpanNames.Contains(name);
		}
		if (flag)
		{
			using (new WriteLockScope(m_TimeSpanNamesLock))
			{
				m_TimeSpanNames.Add(name);
			}
		}
		return flag;
	}

	private long RemapAndHandleNewStringId(long string_id, StringLiteralType string_literal_type)
	{
		long num = RemapStringId(string_id);
		HandleNewStringId(num, string_literal_type);
		return num;
	}

	private void OnStringRemapped(long old_string_id, long new_string_id)
	{
		using (new WriteLockScope(m_TimeSpanInfoSetLock))
		{
			m_TimeSpanInfoSet.OnStringRemapped(old_string_id, new_string_id);
		}
		using (new WriteLockScope(m_ProcessNamesLock))
		{
			foreach (int key in m_ProcessNames.Keys)
			{
				if (m_ProcessNames[key] == old_string_id)
				{
					m_ProcessNames[key] = new_string_id;
				}
			}
		}
		TimeSpanFrameStats value = null;
		using (new ReadLockScope(m_TimeSpanFrameStatsLock))
		{
			m_TimeSpanFrameStats.TryGetValue(old_string_id, out value);
		}
		if (value != null)
		{
			using (new WriteLockScope(m_TimeSpanFrameStatsLock))
			{
				m_TimeSpanFrameStats.Remove(old_string_id);
				m_TimeSpanFrameStats[new_string_id] = value;
			}
		}
		bool flag = false;
		using (new ReadLockScope(m_TimeSpanNamesLock))
		{
			flag = m_TimeSpanNames.Contains(old_string_id);
		}
		if (flag)
		{
			using (new WriteLockScope(m_TimeSpanNamesLock))
			{
				m_TimeSpanNames.Remove(old_string_id);
				m_TimeSpanNames.Add(new_string_id);
			}
		}
		if (m_PendingCustomStatIntervalValues.TryGetValue(old_string_id, out var value2))
		{
			m_PendingCustomStatIntervalValues.Remove(old_string_id);
			m_PendingCustomStatIntervalValues[new_string_id] = value2;
		}
		CustomStatValuePerSecArray value3 = null;
		lock (m_CustomStatValuePerSecArrays)
		{
			if (m_CustomStatValuePerSecArrays.TryGetValue(old_string_id, out value3))
			{
				m_CustomStatValuePerSecArrays.Remove(old_string_id);
				m_CustomStatValuePerSecArrays[new_string_id] = value3;
			}
		}
		lock (m_CustomStatValueTypes)
		{
			if (m_CustomStatValueTypes.TryGetValue(old_string_id, out var value4))
			{
				m_CustomStatValueTypes.Remove(old_string_id);
				m_CustomStatValueTypes[new_string_id] = value4;
			}
		}
		lock (m_ScopeColours)
		{
			CoreUtils.Remap(m_ScopeColours, old_string_id, new_string_id);
		}
		lock (m_CustomStatGraphs)
		{
			CoreUtils.Remap(m_CustomStatGraphs, old_string_id, new_string_id);
		}
		lock (m_CustomStatUnits)
		{
			CoreUtils.Remap(m_CustomStatUnits, old_string_id, new_string_id);
		}
		lock (m_CustomStatColours)
		{
			CoreUtils.Remap(m_CustomStatColours, old_string_id, new_string_id);
		}
		CustomStatSessionData value5 = null;
		using (new ReadLockScope(m_CustomStatSessionInfoLock))
		{
			m_CustomStatSessionInfo.TryGetValue(old_string_id, out value5);
		}
		if (value5 != null)
		{
			using (new WriteLockScope(m_CustomStatSessionInfoLock))
			{
				m_CustomStatSessionInfo.Remove(old_string_id);
				value5.OnNameRemapped(old_string_id, new_string_id);
				m_CustomStatSessionInfo[new_string_id] = value5;
				for (int i = value5.m_FirstFrameSeen; i < m_Frames.Count; i++)
				{
					m_Frames[i].OnCustomStatStringRemapped(old_string_id, new_string_id);
				}
				if (m_CurrentFrame != null)
				{
					m_CurrentFrame.OnCustomStatStringRemapped(old_string_id, new_string_id);
				}
			}
		}
		foreach (CustomStatPacket pendingCustomStatPacket in m_PendingCustomStatPackets)
		{
			if (pendingCustomStatPacket.m_Name == old_string_id)
			{
				pendingCustomStatPacket.m_Name = new_string_id;
			}
		}
	}

	private void LegacySetGraphUnit(long name, long graph, long unit)
	{
		long key = RemapStringId(name);
		lock (m_CustomStatGraphs)
		{
			m_CustomStatGraphs[key] = RemapStringId(graph);
		}
		lock (m_CustomStatUnits)
		{
			m_CustomStatUnits[key] = RemapStringId(unit);
		}
	}

	private void LegacySetCustomStatUnit(int time_span_info_id, long unit)
	{
		TimeSpanInfo timeSpanInfo = GetTimeSpanInfo(time_span_info_id);
		lock (m_CustomStatUnits)
		{
			m_CustomStatUnits[timeSpanInfo.Name] = RemapStringId(unit);
		}
	}

	public long GetAverageFrameTime(int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					num += m_Frames[i].Duration;
				}
			}
		}
		int num4 = end_frame_index + 1 - start_frame_index;
		return (num4 != 0) ? (num / num4) : 0;
	}

	public long GetMaxFrameTime(int start_frame_index, int end_frame_index)
	{
		long num = 0L;
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					long duration = m_Frames[i].Duration;
					if (duration > num)
					{
						num = duration;
					}
				}
			}
		}
		return num;
	}

	public int GetFramesInBudget(int start_frame_index, int end_frame_index)
	{
		int num = 0;
		using (new ReadLockScope(m_FramesLock))
		{
			if (m_Frames.Count != 0)
			{
				int num2 = Misc.Clamp(start_frame_index, 0, m_Frames.Count - 1);
				int num3 = Misc.Clamp(end_frame_index, 0, m_Frames.Count - 1);
				for (int i = num2; i <= num3; i++)
				{
					if (m_Frames[i].Duration < m_TargetFrameTime)
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	public string GetFullFunctionName(long timer)
	{
		if (!m_StringLiteralNamedTimeSpanNames.TryGetValue(timer, out var value))
		{
			return "";
		}
		return value;
	}

	private void UpdateCoreCount(int new_core_index)
	{
		if (new_core_index > m_MaxCoreIndex)
		{
			m_MaxCoreIndex = new_core_index;
			if (this.CoreCountChanged != null)
			{
				this.CoreCountChanged();
			}
		}
	}

	public int GetTimeSpanInfoIdFromName(string time_span_name)
	{
		using (new ReadLockScope(m_TimeSpanInfoSetLock))
		{
			long stringId = GetStringId(time_span_name);
			return m_TimeSpanInfoSet.GetIdFromName(stringId);
		}
	}

	public List<SessionInfoPair> GetSessionInfoValues()
	{
		using (new ReadLockScope(m_SessionInfoValuesLock))
		{
			return new List<SessionInfoPair>(m_SessionInfoValues);
		}
	}
}
