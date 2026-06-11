using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ProfilerStudy.Tracy;

internal sealed class Tracy010LiveEventDecoder
{
	private enum QueueType : byte
	{
		ZoneText,
		ZoneName,
		Message,
		MessageColor,
		MessageCallstack,
		MessageColorCallstack,
		MessageAppInfo,
		ZoneBeginAllocSrcLoc,
		ZoneBeginAllocSrcLocCallstack,
		CallstackSerial,
		Callstack,
		CallstackAlloc,
		CallstackSample,
		CallstackSampleContextSwitch,
		FrameImage,
		ZoneBegin,
		ZoneBeginCallstack,
		ZoneEnd,
		LockWait,
		LockObtain,
		LockRelease,
		LockSharedWait,
		LockSharedObtain,
		LockSharedRelease,
		LockName,
		MemAlloc,
		MemAllocNamed,
		MemFree,
		MemFreeNamed,
		MemAllocCallstack,
		MemAllocCallstackNamed,
		MemFreeCallstack,
		MemFreeCallstackNamed,
		GpuZoneBegin,
		GpuZoneBeginCallstack,
		GpuZoneBeginAllocSrcLoc,
		GpuZoneBeginAllocSrcLocCallstack,
		GpuZoneEnd,
		GpuZoneBeginSerial,
		GpuZoneBeginCallstackSerial,
		GpuZoneBeginAllocSrcLocSerial,
		GpuZoneBeginAllocSrcLocCallstackSerial,
		GpuZoneEndSerial,
		PlotDataInt,
		PlotDataFloat,
		PlotDataDouble,
		ContextSwitch,
		ThreadWakeup,
		GpuTime,
		GpuContextName,
		CallstackFrameSize,
		SymbolInformation,
		ExternalNameMetadata,
		SymbolCodeMetadata,
		SourceCodeMetadata,
		FiberEnter,
		FiberLeave,
		Terminate,
		KeepAlive,
		ThreadContext,
		GpuCalibration,
		Crash,
		CrashReport,
		ZoneValidation,
		ZoneColor,
		ZoneValue,
		FrameMarkMsg,
		FrameMarkMsgStart,
		FrameMarkMsgEnd,
		FrameVsync,
		SourceLocation,
		LockAnnounce,
		LockTerminate,
		LockMark,
		MessageLiteral,
		MessageLiteralColor,
		MessageLiteralCallstack,
		MessageLiteralColorCallstack,
		GpuNewContext,
		CallstackFrame,
		SysTimeReport,
		SysPowerReport,
		TidToPid,
		HwSampleCpuCycle,
		HwSampleInstructionRetired,
		HwSampleCacheReference,
		HwSampleCacheMiss,
		BranchRetired,
		BranchMiss,
		PlotConfig,
		ParamSetup,
		AckServerQueryNoop,
		AckSourceCodeNotAvailable,
		AckSymbolCodeNotAvailable,
		CpuTopology,
		SingleStringData,
		SecondStringData,
		MemNamePayload,
		StringData,
		ThreadName,
		PlotName,
		SourceLocationPayload,
		CallstackPayload,
		CallstackAllocPayload,
		FrameName,
		FrameImageData,
		ExternalName,
		ExternalThreadName,
		SymbolCode,
		SourceCode,
		FiberName,
		NUM_TYPES
	}

	private enum ServerQuery : byte
	{
		Terminate,
		String,
		ThreadString,
		SourceLocation,
		PlotName,
		FrameName,
		Parameter,
		FiberName
	}

	private sealed class SourceLocationRecord
	{
		public ulong Name;
		public ulong Function;
		public ulong File;
		public uint Line;
	}

	private sealed class SourceLocationPayloadRecord
	{
		public string Name;
		public string Function;
		public string File;
		public uint Line;
	}

	private sealed class OpenZone
	{
		public ulong ThreadId;
		public ulong SourceLocation;
		public short SourceLocationIndex;
		public long Start;
		public int Depth;
		public long ChildDuration;
	}

	private sealed class LiveZone
	{
		public ulong ThreadId;
		public ulong SourceLocation;
		public short SourceLocationIndex;
		public long Start;
		public long End;
		public int Depth;
		public long SelfDuration;
	}

	private sealed class GpuContextState
	{
		public byte Context;
		public string Name;
		public uint ThreadId;
		public float Period;
		public byte Type;
		public byte Flags;
		public long CpuTime;
		public long GpuTime;
		public long TimeDiff;
		public bool HasPeriod;
		public bool HasCalibration;
		public long CalibratedGpuTime;
		public long CalibratedCpuTime;
		public double CalibrationMod = 1.0;
		public long LastGpuTime;
		public ulong Overflow;
		public ulong OverflowMul;
	}

	private sealed class GpuZoneState
	{
		public byte Context;
		public ushort BeginQueryId;
		public ushort EndQueryId;
		public uint ThreadId;
		public ulong SourceLocation;
		public short SourceLocationIndex;
		public long CpuStart;
		public long CpuEnd = -1;
		public long GpuStart = -1;
		public long GpuEnd = -1;
	}

	private sealed class PlotBuilder
	{
		public ulong NamePointer;
		public byte Format;
		public readonly List<TracyPlotSample> Samples = new List<TracyPlotSample>();
	}

	private sealed class HardwareSampleStats
	{
		public string Name;
		public int Count;
		public long FirstTime = -1L;
		public long LastTime = -1L;
		public readonly List<Dictionary<string, object>> Samples = new List<Dictionary<string, object>>();
	}

	private sealed class ThreadState
	{
		public ThreadState(ulong threadId)
		{
			ThreadId = threadId;
		}

		public ulong ThreadId { get; }

		public readonly Stack<OpenZone> Stack = new Stack<OpenZone>();
	}

	private const int TargetFrameSize = 256 * 1024;

	private static readonly int[] QueueDataSize = BuildQueueDataSize();

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueHeader
	{
		public byte Type;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueThreadContext
	{
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneBeginLean
	{
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneBegin
	{
		public long Time;
		public ulong SourceLocation;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneEnd
	{
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockWait
	{
		public uint Thread;
		public uint Id;
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockRelease
	{
		public uint Id;
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockReleaseShared
	{
		public uint Id;
		public long Time;
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockName
	{
		public uint Id;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMemAlloc
	{
		public long Time;
		public uint Thread;
		public ulong Pointer;
		public byte Size0;
		public byte Size1;
		public byte Size2;
		public byte Size3;
		public byte Size4;
		public byte Size5;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMemFree
	{
		public long Time;
		public uint Thread;
		public ulong Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuZoneBeginLean
	{
		public long CpuTime;
		public uint Thread;
		public ushort QueryId;
		public byte Context;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuZoneBegin
	{
		public long CpuTime;
		public uint Thread;
		public ushort QueryId;
		public byte Context;
		public ulong SourceLocation;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuZoneEnd
	{
		public long CpuTime;
		public uint Thread;
		public ushort QueryId;
		public byte Context;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueuePlotDataInt
	{
		public ulong Name;
		public long Time;
		public long Value;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueuePlotDataFloat
	{
		public ulong Name;
		public long Time;
		public float Value;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueuePlotDataDouble
	{
		public ulong Name;
		public long Time;
		public double Value;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueContextSwitch
	{
		public long Time;
		public uint OldThread;
		public uint NewThread;
		public byte Cpu;
		public byte Reason;
		public byte State;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueThreadWakeup
	{
		public long Time;
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuTime
	{
		public long GpuTime;
		public ushort QueryId;
		public byte Context;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuContextName
	{
		public byte Context;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueCallstackFrameSize
	{
		public ulong Pointer;
		public byte Size;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueSymbolInformation
	{
		public uint Line;
		public ulong SymbolAddress;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueFiberEnter
	{
		public long Time;
		public ulong Fiber;
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueFiberLeave
	{
		public long Time;
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuCalibration
	{
		public long GpuTime;
		public long CpuTime;
		public long CpuDelta;
		public byte Context;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueCrashReport
	{
		public long Time;
		public ulong Text;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneValidation
	{
		public uint Id;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneColor
	{
		public byte B;
		public byte G;
		public byte R;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueZoneValue
	{
		public ulong Value;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueFrameMark
	{
		public long Time;
		public ulong Name;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueFrameImage
	{
		public uint Frame;
		public ushort Width;
		public ushort Height;
		public byte Flip;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueFrameVsync
	{
		public long Time;
		public uint Id;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueSourceLocation
	{
		public ulong Name;
		public ulong Function;
		public ulong File;
		public uint Line;
		public byte B;
		public byte G;
		public byte R;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockAnnounce
	{
		public uint Id;
		public long Time;
		public ulong Location;
		public byte Type;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockTerminate
	{
		public uint Id;
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueLockMark
	{
		public uint Thread;
		public uint Id;
		public ulong SourceLocation;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMessage
	{
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMessageColor
	{
		public long Time;
		public byte B;
		public byte G;
		public byte R;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMessageLiteral
	{
		public long Time;
		public ulong Text;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMessageColorLiteral
	{
		public long Time;
		public byte B;
		public byte G;
		public byte R;
		public ulong Text;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueGpuNewContext
	{
		public long CpuTime;
		public long GpuTime;
		public uint Thread;
		public float Period;
		public byte Context;
		public byte Flags;
		public byte Type;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueCallstackFrame
	{
		public uint Line;
		public ulong SymbolAddress;
		public uint SymbolLength;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueSysTime
	{
		public long Time;
		public float SystemTime;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueSysPower
	{
		public long Time;
		public ulong Delta;
		public ulong Name;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueTidToPid
	{
		public ulong Tid;
		public ulong Pid;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueHwSample
	{
		public ulong Ip;
		public long Time;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueuePlotConfig
	{
		public ulong Name;
		public byte Type;
		public byte Step;
		public byte Fill;
		public uint Color;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueParamSetup
	{
		public uint Index;
		public ulong Name;
		public byte IsBool;
		public int Value;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueSourceCodeNotAvailable
	{
		public uint Id;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueCpuTopology
	{
		public uint Package;
		public uint Core;
		public uint Thread;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueMemNamePayload
	{
		public ulong Name;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueStringTransfer
	{
		public ulong Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QueueCallstackSample
	{
		public long Time;
		public uint Thread;
	}

	private static int[] BuildQueueDataSize()
	{
		int header = Marshal.SizeOf<QueueHeader>();
		int[] sizes = new int[(int)QueueType.NUM_TYPES];
		sizes[(int)QueueType.ZoneText] = header;
		sizes[(int)QueueType.ZoneName] = header;
		sizes[(int)QueueType.Message] = SizeOf<QueueMessage>();
		sizes[(int)QueueType.MessageColor] = SizeOf<QueueMessageColor>();
		sizes[(int)QueueType.MessageCallstack] = SizeOf<QueueMessage>();
		sizes[(int)QueueType.MessageColorCallstack] = SizeOf<QueueMessageColor>();
		sizes[(int)QueueType.MessageAppInfo] = SizeOf<QueueMessage>();
		sizes[(int)QueueType.ZoneBeginAllocSrcLoc] = SizeOf<QueueZoneBeginLean>();
		sizes[(int)QueueType.ZoneBeginAllocSrcLocCallstack] = SizeOf<QueueZoneBeginLean>();
		sizes[(int)QueueType.CallstackSerial] = header;
		sizes[(int)QueueType.Callstack] = header;
		sizes[(int)QueueType.CallstackAlloc] = header;
		sizes[(int)QueueType.CallstackSample] = SizeOf<QueueCallstackSample>();
		sizes[(int)QueueType.CallstackSampleContextSwitch] = SizeOf<QueueCallstackSample>();
		sizes[(int)QueueType.FrameImage] = SizeOf<QueueFrameImage>();
		sizes[(int)QueueType.ZoneBegin] = SizeOf<QueueZoneBegin>();
		sizes[(int)QueueType.ZoneBeginCallstack] = SizeOf<QueueZoneBegin>();
		sizes[(int)QueueType.ZoneEnd] = SizeOf<QueueZoneEnd>();
		sizes[(int)QueueType.LockWait] = SizeOf<QueueLockWait>();
		sizes[(int)QueueType.LockObtain] = SizeOf<QueueLockWait>();
		sizes[(int)QueueType.LockRelease] = SizeOf<QueueLockRelease>();
		sizes[(int)QueueType.LockSharedWait] = SizeOf<QueueLockWait>();
		sizes[(int)QueueType.LockSharedObtain] = SizeOf<QueueLockWait>();
		sizes[(int)QueueType.LockSharedRelease] = SizeOf<QueueLockReleaseShared>();
		sizes[(int)QueueType.LockName] = SizeOf<QueueLockName>();
		sizes[(int)QueueType.MemAlloc] = SizeOf<QueueMemAlloc>();
		sizes[(int)QueueType.MemAllocNamed] = SizeOf<QueueMemAlloc>();
		sizes[(int)QueueType.MemFree] = SizeOf<QueueMemFree>();
		sizes[(int)QueueType.MemFreeNamed] = SizeOf<QueueMemFree>();
		sizes[(int)QueueType.MemAllocCallstack] = SizeOf<QueueMemAlloc>();
		sizes[(int)QueueType.MemAllocCallstackNamed] = SizeOf<QueueMemAlloc>();
		sizes[(int)QueueType.MemFreeCallstack] = SizeOf<QueueMemFree>();
		sizes[(int)QueueType.MemFreeCallstackNamed] = SizeOf<QueueMemFree>();
		sizes[(int)QueueType.GpuZoneBegin] = SizeOf<QueueGpuZoneBegin>();
		sizes[(int)QueueType.GpuZoneBeginCallstack] = SizeOf<QueueGpuZoneBegin>();
		sizes[(int)QueueType.GpuZoneBeginAllocSrcLoc] = SizeOf<QueueGpuZoneBeginLean>();
		sizes[(int)QueueType.GpuZoneBeginAllocSrcLocCallstack] = SizeOf<QueueGpuZoneBeginLean>();
		sizes[(int)QueueType.GpuZoneEnd] = SizeOf<QueueGpuZoneEnd>();
		sizes[(int)QueueType.GpuZoneBeginSerial] = SizeOf<QueueGpuZoneBegin>();
		sizes[(int)QueueType.GpuZoneBeginCallstackSerial] = SizeOf<QueueGpuZoneBegin>();
		sizes[(int)QueueType.GpuZoneBeginAllocSrcLocSerial] = SizeOf<QueueGpuZoneBeginLean>();
		sizes[(int)QueueType.GpuZoneBeginAllocSrcLocCallstackSerial] = SizeOf<QueueGpuZoneBeginLean>();
		sizes[(int)QueueType.GpuZoneEndSerial] = SizeOf<QueueGpuZoneEnd>();
		sizes[(int)QueueType.PlotDataInt] = SizeOf<QueuePlotDataInt>();
		sizes[(int)QueueType.PlotDataFloat] = SizeOf<QueuePlotDataFloat>();
		sizes[(int)QueueType.PlotDataDouble] = SizeOf<QueuePlotDataDouble>();
		sizes[(int)QueueType.ContextSwitch] = SizeOf<QueueContextSwitch>();
		sizes[(int)QueueType.ThreadWakeup] = SizeOf<QueueThreadWakeup>();
		sizes[(int)QueueType.GpuTime] = SizeOf<QueueGpuTime>();
		sizes[(int)QueueType.GpuContextName] = SizeOf<QueueGpuContextName>();
		sizes[(int)QueueType.CallstackFrameSize] = SizeOf<QueueCallstackFrameSize>();
		sizes[(int)QueueType.SymbolInformation] = SizeOf<QueueSymbolInformation>();
		sizes[(int)QueueType.ExternalNameMetadata] = header;
		sizes[(int)QueueType.SymbolCodeMetadata] = header;
		sizes[(int)QueueType.SourceCodeMetadata] = header;
		sizes[(int)QueueType.FiberEnter] = SizeOf<QueueFiberEnter>();
		sizes[(int)QueueType.FiberLeave] = SizeOf<QueueFiberLeave>();
		sizes[(int)QueueType.Terminate] = header;
		sizes[(int)QueueType.KeepAlive] = header;
		sizes[(int)QueueType.ThreadContext] = SizeOf<QueueThreadContext>();
		sizes[(int)QueueType.GpuCalibration] = SizeOf<QueueGpuCalibration>();
		sizes[(int)QueueType.Crash] = header;
		sizes[(int)QueueType.CrashReport] = SizeOf<QueueCrashReport>();
		sizes[(int)QueueType.ZoneValidation] = SizeOf<QueueZoneValidation>();
		sizes[(int)QueueType.ZoneColor] = SizeOf<QueueZoneColor>();
		sizes[(int)QueueType.ZoneValue] = SizeOf<QueueZoneValue>();
		sizes[(int)QueueType.FrameMarkMsg] = SizeOf<QueueFrameMark>();
		sizes[(int)QueueType.FrameMarkMsgStart] = SizeOf<QueueFrameMark>();
		sizes[(int)QueueType.FrameMarkMsgEnd] = SizeOf<QueueFrameMark>();
		sizes[(int)QueueType.FrameVsync] = SizeOf<QueueFrameVsync>();
		sizes[(int)QueueType.SourceLocation] = SizeOf<QueueSourceLocation>();
		sizes[(int)QueueType.LockAnnounce] = SizeOf<QueueLockAnnounce>();
		sizes[(int)QueueType.LockTerminate] = SizeOf<QueueLockTerminate>();
		sizes[(int)QueueType.LockMark] = SizeOf<QueueLockMark>();
		sizes[(int)QueueType.MessageLiteral] = SizeOf<QueueMessageLiteral>();
		sizes[(int)QueueType.MessageLiteralColor] = SizeOf<QueueMessageColorLiteral>();
		sizes[(int)QueueType.MessageLiteralCallstack] = SizeOf<QueueMessageLiteral>();
		sizes[(int)QueueType.MessageLiteralColorCallstack] = SizeOf<QueueMessageColorLiteral>();
		sizes[(int)QueueType.GpuNewContext] = SizeOf<QueueGpuNewContext>();
		sizes[(int)QueueType.CallstackFrame] = SizeOf<QueueCallstackFrame>();
		sizes[(int)QueueType.SysTimeReport] = SizeOf<QueueSysTime>();
		sizes[(int)QueueType.SysPowerReport] = SizeOf<QueueSysPower>();
		sizes[(int)QueueType.TidToPid] = SizeOf<QueueTidToPid>();
		sizes[(int)QueueType.HwSampleCpuCycle] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.HwSampleInstructionRetired] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.HwSampleCacheReference] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.HwSampleCacheMiss] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.BranchRetired] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.BranchMiss] = SizeOf<QueueHwSample>();
		sizes[(int)QueueType.PlotConfig] = SizeOf<QueuePlotConfig>();
		sizes[(int)QueueType.ParamSetup] = SizeOf<QueueParamSetup>();
		sizes[(int)QueueType.AckServerQueryNoop] = header;
		sizes[(int)QueueType.AckSourceCodeNotAvailable] = SizeOf<QueueSourceCodeNotAvailable>();
		sizes[(int)QueueType.AckSymbolCodeNotAvailable] = header;
		sizes[(int)QueueType.CpuTopology] = SizeOf<QueueCpuTopology>();
		sizes[(int)QueueType.SingleStringData] = header;
		sizes[(int)QueueType.SecondStringData] = header;
		sizes[(int)QueueType.MemNamePayload] = SizeOf<QueueMemNamePayload>();
		sizes[(int)QueueType.StringData] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.ThreadName] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.PlotName] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.SourceLocationPayload] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.CallstackPayload] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.CallstackAllocPayload] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.FrameName] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.FrameImageData] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.ExternalName] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.ExternalThreadName] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.SymbolCode] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.SourceCode] = SizeOf<QueueStringTransfer>();
		sizes[(int)QueueType.FiberName] = SizeOf<QueueStringTransfer>();
		return sizes;
	}

	private static int SizeOf<T>() where T : struct
	{
		return Marshal.SizeOf<QueueHeader>() + Marshal.SizeOf<T>();
	}

	private readonly TracyTraceMetadata m_InitialMetadata;
	private readonly long m_BaseTime;
	private readonly double m_TimerMultiplier;
	private readonly Action<byte, ulong, uint> m_SendQuery;
	private readonly Dictionary<ulong, ThreadState> m_Threads = new Dictionary<ulong, ThreadState>();
	private readonly Dictionary<ulong, string> m_Strings = new Dictionary<ulong, string>();
	private readonly Dictionary<ulong, string> m_ThreadNames = new Dictionary<ulong, string>();
	private readonly Dictionary<ulong, SourceLocationRecord> m_SourceLocations = new Dictionary<ulong, SourceLocationRecord>();
	private readonly Dictionary<ulong, short> m_SourceLocationIndexes = new Dictionary<ulong, short>();
	private readonly Dictionary<ulong, List<long>> m_ContinuousFrameStarts = new Dictionary<ulong, List<long>>();
	private readonly Dictionary<ulong, List<TracyFrameSummary>> m_DiscreteFrames = new Dictionary<ulong, List<TracyFrameSummary>>();
	private readonly Dictionary<ulong, TracyFrameSummary> m_OpenFrames = new Dictionary<ulong, TracyFrameSummary>();
	private readonly Dictionary<ulong, PlotBuilder> m_Plots = new Dictionary<ulong, PlotBuilder>();
	private readonly Dictionary<byte, GpuContextState> m_GpuContexts = new Dictionary<byte, GpuContextState>();
	private readonly Dictionary<string, Stack<GpuZoneState>> m_OpenGpuZoneStacks = new Dictionary<string, Stack<GpuZoneState>>();
	private readonly Dictionary<string, Queue<GpuZoneState>> m_GpuQueryZones = new Dictionary<string, Queue<GpuZoneState>>();
	private readonly List<GpuZoneState> m_GpuZones = new List<GpuZoneState>();
	private readonly Dictionary<ulong, string> m_SourceLocationPayloadNames = new Dictionary<ulong, string>();
	private readonly Dictionary<ulong, SourceLocationPayloadRecord> m_SourceLocationPayloads = new Dictionary<ulong, SourceLocationPayloadRecord>();
	private readonly Dictionary<string, HardwareSampleStats> m_HardwareSamples = new Dictionary<string, HardwareSampleStats>(StringComparer.Ordinal);
	private readonly List<Dictionary<string, object>> m_GpuTimeWithoutZoneSamples = new List<Dictionary<string, object>>();
	private readonly Queue<ulong> m_PendingSourceLocations = new Queue<ulong>();
	private readonly HashSet<ulong> m_RequestedStrings = new HashSet<ulong>();
	private readonly HashSet<ulong> m_RequestedThreadNames = new HashSet<ulong>();
	private readonly HashSet<ulong> m_RequestedSourceLocations = new HashSet<ulong>();
	private readonly HashSet<ulong> m_RequestedPlotNames = new HashSet<ulong>();
	private readonly HashSet<ulong> m_RequestedFrameNames = new HashSet<ulong>();
	private readonly Dictionary<string, int> m_UnsupportedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
	private ulong m_CurrentThread;
	private long m_RefTimeThread;
	private long m_RefTimeSerial;
	private long m_RefTimeGpu;
	private long m_LastTime;
	private int m_EventCount;
	private int m_StringPayloadCount;
	private int m_ServerAckCount;
	private int m_PendingGpuContextName = -1;
	private ulong m_PendingGpuSourceLocationPayload;
	private int m_GpuTimeEventCount;
	private int m_GpuTimeMatchedCount;
	private int m_DynamicZoneNameCount;
	private int m_GpuTimeWithoutZoneCount;

	public Tracy010LiveEventDecoder(TracyTraceMetadata initialMetadata, long baseTime, Action<byte, ulong, uint> sendQuery)
	{
		m_InitialMetadata = initialMetadata ?? throw new ArgumentNullException(nameof(initialMetadata));
		m_BaseTime = baseTime;
		m_TimerMultiplier = initialMetadata.TimerMultiplier;
		m_SendQuery = sendQuery ?? throw new ArgumentNullException(nameof(sendQuery));
		m_CurrentThread = 0;
		m_LastTime = initialMetadata.LastTime;
	}

	public int ProcessBlock(byte[] decodedBlock)
	{
		if (decodedBlock == null)
		{
			throw new ArgumentNullException(nameof(decodedBlock));
		}
		if (decodedBlock.Length > TargetFrameSize)
		{
			throw new TracyFileFormatException("TracyLiveDecodeFailed", "Decoded Tracy live block is larger than the protocol frame size.");
		}

		int offset = 0;
		int processed = 0;
		QueueType? previousType = null;
		Queue<string> recentEvents = new Queue<string>();
		while (offset < decodedBlock.Length)
		{
			int eventOffset = offset;
			byte typeValue = decodedBlock[offset];
			if (typeValue >= (byte)QueueType.NUM_TYPES || typeValue >= QueueDataSize.Length)
			{
				throw new TracyFileFormatException(
					"TracyLiveDecodeFailed",
					"Tracy live queue item type is outside the 0.10.0 protocol range at block offset " + offset + " with type byte " + typeValue + ", processed events " + processed + ", previous type " + (previousType.HasValue ? previousType.Value.ToString() : "none") + ", recent events [" + string.Join(", ", recentEvents.ToArray()) + "], decoded block size " + decodedBlock.Length + ".");
			}

			QueueType type = (QueueType)typeValue;
			if (type >= QueueType.StringData)
			{
				ProcessStringTransfer(decodedBlock, ref offset, type);
			}
			else if (type == QueueType.SingleStringData || type == QueueType.SecondStringData)
			{
				ProcessInlineString(decodedBlock, ref offset);
			}
			else
			{
				ProcessFixedItem(decodedBlock, offset, type);
				offset += QueueDataSize[typeValue];
			}
			previousType = type;
			recentEvents.Enqueue(type + "@" + eventOffset + "+" + (offset - eventOffset));
			while (recentEvents.Count > 8)
			{
				recentEvents.Dequeue();
			}
			processed++;
		}
		m_EventCount += processed;
		return processed;
	}

	public TracyEventStream CreateEventStream(int compressedBlockCount, long compressedByteCount, long decodedByteCount, long payloadByteCount, ArrayList diagnostics)
	{
		List<TracyCpuZoneSummary> zones = m_Threads.Values
			.SelectMany(thread => CloseOpenZones(thread))
			.Concat(BuildClosedZones())
			.OrderBy(zone => zone.Start)
			.ToList();
		List<TracyFrameSetSummary> frameSets = BuildFrameSets();
		List<TracyPlotSummary> plots = BuildPlots();
		List<TracyThreadSummary> threads = BuildThreads();
		List<TracyGpuContextSummary> gpuContexts = BuildGpuContexts();
		List<TracyGpuZoneSummary> gpuZones = BuildGpuZones();
		List<TracySourceLocationSummary> sourceLocations = BuildSourceLocations();
		int gpuTimeZoneCount = gpuZones.Count(zone => string.Equals(zone.TimeSource, "gpu-time", StringComparison.OrdinalIgnoreCase));
		int cpuSubmitZoneCount = gpuZones.Count(zone => string.Equals(zone.TimeSource, "cpu-submit-time", StringComparison.OrdinalIgnoreCase));
		TracyTraceMetadata metadata = new TracyTraceMetadata(
			m_InitialMetadata.Delay,
			m_InitialMetadata.Resolution,
			m_InitialMetadata.TimerMultiplier,
			Math.Max(m_LastTime, zones.Count == 0 ? m_InitialMetadata.LastTime : zones.Max(zone => zone.End)),
			m_InitialMetadata.FrameOffset,
			m_InitialMetadata.ProcessId,
			m_InitialMetadata.SamplingPeriod,
			m_InitialMetadata.CpuArchitecture,
			m_InitialMetadata.CpuId,
			m_InitialMetadata.CpuManufacturer,
			m_InitialMetadata.OnDemand,
			m_InitialMetadata.CaptureName,
			m_InitialMetadata.CaptureProgram,
			m_InitialMetadata.CaptureTime,
			m_InitialMetadata.ExecutableTime,
			m_InitialMetadata.HostInfo,
			frameSets,
			m_Strings.Count,
			m_ThreadNames.Count);

		diagnostics.Add(new Dictionary<string, object>
		{
			["severity"] = zones.Count > 0 || plots.Count > 0 || frameSets.Sum(frameSet => frameSet.FrameCount) > 0 ? "info" : "warning",
			["code"] = "TracyLiveEventsDecoded",
			["message"] = "Tracy live LZ4 queue stream was decoded into the normalized query model.",
			["eventCount"] = m_EventCount,
			["stringPayloadCount"] = m_StringPayloadCount,
			["serverAckCount"] = m_ServerAckCount,
			["threadCount"] = threads.Count,
			["cpuZoneCount"] = zones.Count,
			["gpuContextCount"] = gpuContexts.Count,
			["gpuZoneCount"] = gpuZones.Count,
			["gpuTimeZoneCount"] = gpuTimeZoneCount,
			["cpuSubmitGpuZoneCount"] = cpuSubmitZoneCount,
			["gpuTimeEventCount"] = m_GpuTimeEventCount,
			["gpuTimeMatchedCount"] = m_GpuTimeMatchedCount,
			["gpuTimeWithoutZoneCount"] = m_GpuTimeWithoutZoneCount,
			["dynamicZoneNameCount"] = m_DynamicZoneNameCount,
			["hardwareSampleCount"] = m_HardwareSamples.Values.Sum(value => value.Count),
			["hardwareSampleTypeCount"] = m_HardwareSamples.Count,
			["frameCount"] = frameSets.Sum(frameSet => frameSet.FrameCount),
			["plotCount"] = plots.Count
		});
		if (cpuSubmitZoneCount > 0)
		{
			diagnostics.Add(new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyGpuTimestampFallback",
				["message"] = "Some Tracy GPU zones did not receive GpuTime samples during this capture and are exposed with CPU submit timestamps.",
				["gpuZoneCount"] = gpuZones.Count,
				["gpuTimeZoneCount"] = gpuTimeZoneCount,
				["cpuSubmitGpuZoneCount"] = cpuSubmitZoneCount,
				["gpuTimeEventCount"] = m_GpuTimeEventCount,
				["gpuTimeMatchedCount"] = m_GpuTimeMatchedCount
			});
		}
		if (m_DynamicZoneNameCount > 0)
		{
			diagnostics.Add(new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyDynamicZoneNamesDecoded",
				["message"] = "Tracy ZoneName events were observed. The current decoder records their count; binding dynamic names to zone instances is reserved for the hierarchy pass.",
				["dynamicZoneNameCount"] = m_DynamicZoneNameCount
			});
		}
		if (m_HardwareSamples.Count > 0)
		{
			diagnostics.Add(new Dictionary<string, object>
			{
				["severity"] = "info",
				["code"] = "TracyHardwareSamplesDecoded",
				["message"] = "Tracy hardware sample queue events were decoded into bounded summaries. IP symbolization and CPU zone attribution are not implemented yet.",
				["sampleTypes"] = BuildHardwareSampleDiagnostics()
			});
		}
		if (m_GpuTimeWithoutZoneCount > 0)
		{
			diagnostics.Add(new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyGpuTimeWithoutZone",
				["message"] = "Some Tracy GpuTime events did not match an open GPU query zone. This usually means the capture window missed the matching begin/end event or the runtime emitted an orphan query timestamp.",
				["count"] = m_GpuTimeWithoutZoneCount,
				["samples"] = new ArrayList(m_GpuTimeWithoutZoneSamples)
			});
		}
		if (m_UnsupportedCounts.Count > 0)
		{
			diagnostics.Add(new Dictionary<string, object>
			{
				["severity"] = "warning",
				["code"] = "TracyLiveEventsUnsupported",
				["message"] = "Some Tracy live queue event types were observed but are not exported by the current normalized schema.",
				["counts"] = new Dictionary<string, object>(m_UnsupportedCounts.ToDictionary(pair => pair.Key, pair => (object)pair.Value))
			});
		}

		return new TracyEventStream(
			new TracyFileHeader(TracyVersionRegistry.LockedVersion, "live-lz4"),
			compressedBlockCount,
			compressedByteCount,
			decodedByteCount,
			payloadByteCount,
			metadata,
			zones,
			plots,
			threads,
			threads.Count,
			diagnostics,
			gpuContexts,
			gpuZones,
			sourceLocations);
	}

	private readonly List<LiveZone> m_ClosedZones = new List<LiveZone>();

	private IEnumerable<TracyCpuZoneSummary> BuildClosedZones()
	{
		foreach (LiveZone zone in m_ClosedZones)
		{
			yield return new TracyCpuZoneSummary(zone.ThreadId, zone.SourceLocationIndex, ResolveSourceLocationName(zone.SourceLocation), zone.Start, zone.End, zone.Depth, zone.SelfDuration);
		}
	}

	private ArrayList BuildHardwareSampleDiagnostics()
	{
		ArrayList sampleTypes = new ArrayList();
		foreach (HardwareSampleStats stats in m_HardwareSamples.Values.OrderBy(value => value.Name))
		{
			sampleTypes.Add(new Dictionary<string, object>
			{
				["name"] = stats.Name,
				["count"] = stats.Count,
				["firstTime"] = stats.FirstTime,
				["lastTime"] = stats.LastTime,
				["samples"] = new ArrayList(stats.Samples)
			});
		}
		return sampleTypes;
	}

	private IEnumerable<TracyCpuZoneSummary> CloseOpenZones(ThreadState thread)
	{
		while (thread.Stack.Count > 0)
		{
			OpenZone zone = thread.Stack.Pop();
			long end = m_LastTime > zone.Start ? m_LastTime : zone.Start;
			long duration = Math.Max(0L, end - zone.Start);
			long selfDuration = Math.Max(0L, duration - zone.ChildDuration);
			if (thread.Stack.Count > 0)
			{
				thread.Stack.Peek().ChildDuration += duration;
			}
			yield return new TracyCpuZoneSummary(zone.ThreadId, zone.SourceLocationIndex, ResolveSourceLocationName(zone.SourceLocation), zone.Start, end, zone.Depth, selfDuration);
		}
	}

	private List<TracyGpuContextSummary> BuildGpuContexts()
	{
		return m_GpuContexts.Values
			.OrderBy(context => context.Context)
			.Select(context => new TracyGpuContextSummary(
				context.Context,
				string.IsNullOrWhiteSpace(context.Name) ? "GPU Context " + context.Context : context.Name,
				context.ThreadId,
				context.Period,
				context.Type,
				context.Flags,
				context.CpuTime,
				context.GpuTime))
			.ToList();
	}

	private List<TracyGpuZoneSummary> BuildGpuZones()
	{
		int id = 0;
		foreach (GpuZoneState zone in m_GpuZones)
		{
			if (zone.CpuEnd < 0)
			{
				zone.CpuEnd = m_LastTime > zone.CpuStart ? m_LastTime : zone.CpuStart;
			}
		}
		return m_GpuZones
			.Where(zone => (zone.GpuStart >= 0 && zone.GpuEnd >= zone.GpuStart) || zone.CpuEnd >= zone.CpuStart)
			.OrderBy(zone => zone.GpuStart >= 0 ? zone.GpuStart : zone.CpuStart)
			.Select(zone => new TracyGpuZoneSummary(
				id++,
				zone.Context,
				zone.BeginQueryId,
				zone.ThreadId,
				zone.SourceLocationIndex,
				ResolveSourceLocationName(zone.SourceLocation),
				zone.GpuStart >= 0 && zone.GpuEnd >= zone.GpuStart ? zone.GpuStart : zone.CpuStart,
				zone.GpuStart >= 0 && zone.GpuEnd >= zone.GpuStart ? zone.GpuEnd : zone.CpuEnd,
				zone.GpuStart >= 0 && zone.GpuEnd >= zone.GpuStart ? "gpu-time" : "cpu-submit-time",
				0,
				-1,
				ClassifyGpuZoneKind(ResolveSourceLocationName(zone.SourceLocation)),
				zone.CpuStart,
				zone.CpuEnd))
			.ToList();
	}

	private List<TracySourceLocationSummary> BuildSourceLocations()
	{
		return m_SourceLocationIndexes
			.OrderBy(pair => pair.Value)
			.Select(pair => BuildSourceLocationSummary(pair.Value, pair.Key))
			.ToList();
	}

	private TracySourceLocationSummary BuildSourceLocationSummary(short id, ulong pointer)
	{
		if (m_SourceLocationPayloads.TryGetValue(pointer, out SourceLocationPayloadRecord payload))
		{
			return new TracySourceLocationSummary(id, payload.Name, payload.Function, payload.File, payload.Line, string.Empty);
		}
		if (m_SourceLocations.TryGetValue(pointer, out SourceLocationRecord record))
		{
			return new TracySourceLocationSummary(
				id,
				ResolveString(record.Name),
				ResolveString(record.Function),
				ResolveString(record.File),
				record.Line,
				string.Empty);
		}
		return new TracySourceLocationSummary(id, ResolveSourceLocationName(pointer), string.Empty, string.Empty, 0U, string.Empty);
	}

	private static string ClassifyGpuZoneKind(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "unknown";
		}
		string normalized = name.ToLowerInvariant();
		if (normalized.Contains("blit"))
		{
			return "blit";
		}
		if (normalized.Contains("draw"))
		{
			return "draw";
		}
		if (normalized.Contains("pass"))
		{
			return "pass";
		}
		return "marker";
	}

	private void ProcessStringTransfer(byte[] buffer, ref int offset, QueueType type)
	{
		if (type == QueueType.FrameImageData || type == QueueType.SymbolCode || type == QueueType.SourceCode)
		{
			ProcessLargeStringTransfer(buffer, ref offset, type);
			return;
		}
		EnsureAvailable(buffer, offset, 11);
		ulong pointer = ReadUInt64(buffer, offset + 1);
		int size = ReadUInt16(buffer, offset + 9);
		EnsureAvailable(buffer, offset, 11 + size);
		string value = Encoding.UTF8.GetString(buffer, offset + 11, size);
		switch (type)
		{
			case QueueType.StringData:
				m_Strings[pointer] = value;
				break;
			case QueueType.ThreadName:
				m_ThreadNames[pointer] = value;
				break;
			case QueueType.PlotName:
				m_Strings[pointer] = value;
				break;
			case QueueType.FrameName:
				m_Strings[pointer] = value;
				break;
			case QueueType.SourceLocationPayload:
				ProcessSourceLocationPayload(pointer, buffer, offset + 11, size);
				break;
			default:
				IncrementUnsupported(type);
				break;
		}
		m_StringPayloadCount++;
		offset += 11 + size;
	}

	private void ProcessLargeStringTransfer(byte[] buffer, ref int offset, QueueType type)
	{
		EnsureAvailable(buffer, offset, 13);
		int size = checked((int)ReadUInt32(buffer, offset + 9));
		EnsureAvailable(buffer, offset, 13 + size);
		IncrementUnsupported(type);
		m_StringPayloadCount++;
		offset += 13 + size;
	}

	private void ProcessInlineString(byte[] buffer, ref int offset)
	{
		EnsureAvailable(buffer, offset, 3);
		int size = ReadUInt16(buffer, offset + 1);
		EnsureAvailable(buffer, offset, 3 + size);
		if (m_PendingGpuContextName >= 0)
		{
			byte contextId = (byte)m_PendingGpuContextName;
			GpuContextState context = EnsureGpuContext(contextId);
			context.Name = Encoding.UTF8.GetString(buffer, offset + 3, size);
			m_PendingGpuContextName = -1;
		}
		offset += 3 + size;
	}

	private void ProcessFixedItem(byte[] buffer, int offset, QueueType type)
	{
		switch (type)
		{
			case QueueType.ZoneName:
				ProcessZoneName();
				break;
			case QueueType.ThreadContext:
				m_RefTimeThread = 0;
				m_CurrentThread = ReadUInt32(buffer, offset + 1);
				EnsureThread(m_CurrentThread);
				break;
			case QueueType.ZoneBegin:
			case QueueType.ZoneBeginCallstack:
				ProcessZoneBegin(ReadInt64(buffer, offset + 1), ReadUInt64(buffer, offset + 9));
				break;
			case QueueType.ZoneEnd:
				ProcessZoneEnd(ReadInt64(buffer, offset + 1));
				break;
			case QueueType.FrameMarkMsg:
				AddContinuousFrame(ReadUInt64(buffer, offset + 9), ToTime(ReadInt64(buffer, offset + 1)));
				break;
			case QueueType.FrameMarkMsgStart:
				AddFrameStart(ReadUInt64(buffer, offset + 9), ToTime(ReadInt64(buffer, offset + 1)));
				break;
			case QueueType.FrameMarkMsgEnd:
				AddFrameEnd(ReadUInt64(buffer, offset + 9), ToTime(ReadInt64(buffer, offset + 1)));
				break;
			case QueueType.SourceLocation:
				ProcessSourceLocation(buffer, offset);
				break;
			case QueueType.PlotDataInt:
				ProcessPlot(ReadUInt64(buffer, offset + 1), ReadInt64(buffer, offset + 9), ReadInt64(buffer, offset + 17));
				break;
			case QueueType.PlotDataFloat:
				ProcessPlot(ReadUInt64(buffer, offset + 1), ReadInt64(buffer, offset + 9), BitConverter.ToSingle(buffer, offset + 17));
				break;
			case QueueType.PlotDataDouble:
				ProcessPlot(ReadUInt64(buffer, offset + 1), ReadInt64(buffer, offset + 9), BitConverter.ToDouble(buffer, offset + 17));
				break;
			case QueueType.PlotConfig:
				ProcessPlotConfig(buffer, offset);
				break;
			case QueueType.GpuNewContext:
				ProcessGpuNewContext(buffer, offset);
				break;
			case QueueType.GpuContextName:
				m_PendingGpuContextName = buffer[offset + 1];
				EnsureGpuContext(buffer[offset + 1]);
				break;
			case QueueType.GpuZoneBeginSerial:
			case QueueType.GpuZoneBeginCallstackSerial:
				ProcessGpuZoneBegin(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], ReadUInt64(buffer, offset + 16), serial: true);
				break;
			case QueueType.GpuZoneBeginAllocSrcLocSerial:
			case QueueType.GpuZoneBeginAllocSrcLocCallstackSerial:
				ProcessGpuZoneBegin(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], ConsumeGpuSourceLocationPayload(), serial: true);
				break;
			case QueueType.GpuZoneEndSerial:
				ProcessGpuZoneEnd(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], serial: true);
				break;
			case QueueType.GpuZoneBegin:
			case QueueType.GpuZoneBeginCallstack:
				ProcessGpuZoneBegin(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], ReadUInt64(buffer, offset + 16), serial: false);
				break;
			case QueueType.GpuZoneBeginAllocSrcLoc:
			case QueueType.GpuZoneBeginAllocSrcLocCallstack:
				ProcessGpuZoneBegin(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], ConsumeGpuSourceLocationPayload(), serial: false);
				break;
			case QueueType.GpuZoneEnd:
				ProcessGpuZoneEnd(ReadInt64(buffer, offset + 1), ReadUInt32(buffer, offset + 9), ReadUInt16(buffer, offset + 13), buffer[offset + 15], serial: false);
				break;
			case QueueType.GpuTime:
				ProcessGpuTime(ReadInt64(buffer, offset + 1), ReadUInt16(buffer, offset + 9), buffer[offset + 11]);
				break;
			case QueueType.GpuCalibration:
				ProcessGpuCalibration(ReadInt64(buffer, offset + 1), ReadInt64(buffer, offset + 9), ReadInt64(buffer, offset + 17), buffer[offset + 25]);
				break;
			case QueueType.HwSampleCpuCycle:
			case QueueType.HwSampleInstructionRetired:
			case QueueType.HwSampleCacheReference:
			case QueueType.HwSampleCacheMiss:
			case QueueType.BranchRetired:
			case QueueType.BranchMiss:
				ProcessHardwareSample(type, ReadUInt64(buffer, offset + 1), ReadInt64(buffer, offset + 9));
				break;
			case QueueType.AckServerQueryNoop:
			case QueueType.AckSourceCodeNotAvailable:
			case QueueType.AckSymbolCodeNotAvailable:
				m_ServerAckCount++;
				break;
			case QueueType.KeepAlive:
			case QueueType.Terminate:
				break;
			default:
				IncrementUnsupported(type);
				break;
		}
	}

	private void ProcessGpuNewContext(byte[] buffer, int offset)
	{
		byte contextId = buffer[offset + 25];
		GpuContextState context = EnsureGpuContext(contextId);
		context.CpuTime = ToTime(ReadInt64(buffer, offset + 1));
		context.GpuTime = ReadInt64(buffer, offset + 9);
		context.ThreadId = ReadUInt32(buffer, offset + 17);
		context.Period = BitConverter.ToSingle(buffer, offset + 21);
		context.Flags = buffer[offset + 26];
		context.Type = buffer[offset + 27];
		long calibratedGpuTime = context.Period == 1.0f ? context.GpuTime : (long)(context.Period * context.GpuTime);
		context.TimeDiff = context.CpuTime - calibratedGpuTime;
		context.HasPeriod = context.Period != 1.0f;
		context.HasCalibration = (context.Flags & 1) != 0;
		context.CalibratedGpuTime = calibratedGpuTime;
		context.CalibratedCpuTime = context.CpuTime;
		context.CalibrationMod = 1.0;
		context.LastGpuTime = 0L;
		context.Overflow = 0UL;
		context.OverflowMul = 0UL;
		UpdateLastTime(context.CpuTime);
	}

	private void ProcessGpuZoneBegin(long cpuTimeDelta, uint thread, ushort queryId, byte context, ulong sourceLocation, bool serial)
	{
		if (sourceLocation != 0 && !m_SourceLocationPayloadNames.ContainsKey(sourceLocation))
		{
			EnsureSourceLocation(sourceLocation);
		}
		GpuContextState contextState = EnsureGpuContext(context);
		long start = ToTime(serial ? RefTimeSerial(cpuTimeDelta) : RefTime(cpuTimeDelta));
		GpuZoneState zone = new GpuZoneState
		{
			Context = context,
			BeginQueryId = queryId,
			ThreadId = thread,
			SourceLocation = sourceLocation,
			SourceLocationIndex = sourceLocation == 0 ? (short)-1 : GetSourceLocationIndex(sourceLocation),
			CpuStart = start
		};
		m_GpuZones.Add(zone);
		GetGpuStack(contextState, context, thread).Push(zone);
		EnqueueGpuQueryZone(context, queryId, zone);
		UpdateLastTime(start);
	}

	private void ProcessGpuZoneEnd(long cpuTimeDelta, uint thread, ushort queryId, byte context, bool serial)
	{
		long end = ToTime(serial ? RefTimeSerial(cpuTimeDelta) : RefTime(cpuTimeDelta));
		UpdateLastTime(end);
		GpuContextState contextState = EnsureGpuContext(context);
		Stack<GpuZoneState> stack = GetGpuStack(contextState, context, thread);
		if (stack.Count == 0 && thread != 0)
		{
			stack = GetGpuStack(contextState, context, 0);
		}
		if (stack.Count == 0)
		{
			IncrementUnsupported("GpuZoneEndWithoutBegin");
			return;
		}
		GpuZoneState zone = stack.Pop();
		zone.EndQueryId = queryId;
		zone.CpuEnd = end;
		EnqueueGpuQueryZone(context, queryId, zone);
	}

	private void ProcessGpuTime(long gpuTimeDelta, ushort queryId, byte context)
	{
		m_GpuTimeEventCount++;
		GpuContextState contextState = EnsureGpuContext(context);
		long gpuTick = RefTimeGpu(gpuTimeDelta);
		if (gpuTick < contextState.LastGpuTime - (1L << 31))
		{
			if (contextState.Overflow == 0UL)
			{
				contextState.Overflow = NextPowerOfTwo(contextState.LastGpuTime);
			}
			contextState.OverflowMul++;
		}
		contextState.LastGpuTime = gpuTick;
		if (contextState.Overflow != 0UL)
		{
			gpuTick = checked((long)((ulong)gpuTick + contextState.Overflow * contextState.OverflowMul));
		}

		long resolvedGpuTime = ResolveGpuTime(contextState, gpuTick);
		string key = GpuZoneKey(context, queryId);
		if (!m_GpuQueryZones.TryGetValue(key, out Queue<GpuZoneState> queue) || queue.Count == 0)
		{
			m_GpuTimeWithoutZoneCount++;
			if (m_GpuTimeWithoutZoneSamples.Count < 16)
			{
				m_GpuTimeWithoutZoneSamples.Add(new Dictionary<string, object>
				{
					["context"] = context,
					["queryId"] = queryId,
					["gpuTick"] = gpuTick,
					["resolvedGpuTime"] = resolvedGpuTime
				});
			}
			return;
		}
		GpuZoneState zone = queue.Dequeue();
		if (queue.Count == 0)
		{
			m_GpuQueryZones.Remove(key);
		}
		if (zone.GpuStart < 0)
		{
			zone.GpuStart = resolvedGpuTime;
		}
		else
		{
			zone.GpuEnd = resolvedGpuTime;
		}
		m_GpuTimeMatchedCount++;
		UpdateLastTime(resolvedGpuTime);
	}

	private void ProcessGpuCalibration(long gpuTime, long cpuTime, long cpuDelta, byte context)
	{
		GpuContextState contextState = EnsureGpuContext(context);
		long calibratedGpuTime = contextState.HasPeriod ? (long)(contextState.Period * gpuTime) : gpuTime;
		long gpuDelta = calibratedGpuTime - contextState.CalibratedGpuTime;
		if (gpuDelta != 0)
		{
			contextState.CalibrationMod = (double)cpuDelta / gpuDelta;
		}
		contextState.CalibratedGpuTime = calibratedGpuTime;
		contextState.CalibratedCpuTime = ToTime(cpuTime);
		contextState.HasCalibration = true;
	}

	private void ProcessZoneName()
	{
		m_DynamicZoneNameCount++;
	}

	private void ProcessHardwareSample(QueueType type, ulong ip, long timeDelta)
	{
		string name = type.ToString();
		if (!m_HardwareSamples.TryGetValue(name, out HardwareSampleStats stats))
		{
			stats = new HardwareSampleStats
			{
				Name = name
			};
			m_HardwareSamples[name] = stats;
		}
		long time = ToTime(RefTime(timeDelta));
		stats.Count++;
		if (stats.FirstTime < 0)
		{
			stats.FirstTime = time;
		}
		stats.LastTime = time;
		if (stats.Samples.Count < 8)
		{
			stats.Samples.Add(new Dictionary<string, object>
			{
				["ip"] = "0x" + ip.ToString("x"),
				["time"] = time
			});
		}
		UpdateLastTime(time);
	}

	private void ProcessZoneBegin(long timeDelta, ulong sourceLocation)
	{
		ThreadState thread = EnsureThread(m_CurrentThread);
		EnsureSourceLocation(sourceLocation);
		long start = ToTime(RefTime(timeDelta));
		thread.Stack.Push(new OpenZone
		{
			ThreadId = thread.ThreadId,
			SourceLocation = sourceLocation,
			SourceLocationIndex = GetSourceLocationIndex(sourceLocation),
			Start = start,
			Depth = thread.Stack.Count,
			ChildDuration = 0L
		});
		UpdateLastTime(start);
	}

	private void ProcessZoneEnd(long timeDelta)
	{
		ThreadState thread = EnsureThread(m_CurrentThread);
		long end = ToTime(RefTime(timeDelta));
		UpdateLastTime(end);
		if (thread.Stack.Count == 0)
		{
			IncrementUnsupported("ZoneEndWithoutBegin");
			return;
		}
		OpenZone openZone = thread.Stack.Pop();
		long duration = Math.Max(0L, end - openZone.Start);
		long selfDuration = Math.Max(0L, duration - openZone.ChildDuration);
		if (thread.Stack.Count > 0)
		{
			thread.Stack.Peek().ChildDuration += duration;
		}
		m_ClosedZones.Add(new LiveZone
		{
			ThreadId = openZone.ThreadId,
			SourceLocation = openZone.SourceLocation,
			SourceLocationIndex = openZone.SourceLocationIndex,
			Start = openZone.Start,
			End = end,
			Depth = openZone.Depth,
			SelfDuration = selfDuration
		});
	}

	private void ProcessSourceLocation(byte[] buffer, int offset)
	{
		if (m_PendingSourceLocations.Count == 0)
		{
			IncrementUnsupported("SourceLocationWithoutQuery");
			return;
		}
		ulong pointer = m_PendingSourceLocations.Dequeue();
		SourceLocationRecord record = new SourceLocationRecord
		{
			Name = ReadUInt64(buffer, offset + 1),
			Function = ReadUInt64(buffer, offset + 9),
			File = ReadUInt64(buffer, offset + 17),
			Line = ReadUInt32(buffer, offset + 25)
		};
		m_SourceLocations[pointer] = record;
		EnsureString(record.Name);
		EnsureString(record.Function);
		EnsureString(record.File);
	}

	private void ProcessSourceLocationPayload(ulong pointer, byte[] buffer, int offset, int size)
	{
		if (size < 8)
		{
			IncrementUnsupported("SourceLocationPayloadTruncated");
			return;
		}

		uint line = ReadUInt32(buffer, offset + 4);
		int cursor = offset + 8;
		int end = offset + size;
		string function = ReadNullTerminatedUtf8(buffer, ref cursor, end);
		string source = ReadNullTerminatedUtf8(buffer, ref cursor, end);
		string name = cursor <= end ? Encoding.UTF8.GetString(buffer, cursor, end - cursor) : string.Empty;
		string label = !string.IsNullOrWhiteSpace(name)
			? name
			: (!string.IsNullOrWhiteSpace(function) ? function : (line == 0 ? source : source + ":" + line));
		if (pointer == 0UL)
		{
			pointer = 0x8000000000000000UL | (ulong)(uint)(m_SourceLocationPayloadNames.Count + 1);
		}
		m_SourceLocationPayloadNames[pointer] = label;
		m_SourceLocationPayloads[pointer] = new SourceLocationPayloadRecord
		{
			Name = name,
			Function = function,
			File = source,
			Line = line
		};
		m_PendingGpuSourceLocationPayload = pointer;
	}

	private static string ReadNullTerminatedUtf8(byte[] buffer, ref int cursor, int end)
	{
		if (cursor >= end)
		{
			return string.Empty;
		}
		int start = cursor;
		while (cursor < end && buffer[cursor] != 0)
		{
			cursor++;
		}
		string value = Encoding.UTF8.GetString(buffer, start, cursor - start);
		if (cursor < end && buffer[cursor] == 0)
		{
			cursor++;
		}
		return value;
	}

	private void ProcessPlot(ulong name, long timeDelta, double value)
	{
		EnsurePlotName(name);
		PlotBuilder plot = EnsurePlot(name);
		long time = ToTime(RefTime(timeDelta));
		plot.Samples.Add(new TracyPlotSample(time, value));
		UpdateLastTime(time);
	}

	private void ProcessPlotConfig(byte[] buffer, int offset)
	{
		ulong name = ReadUInt64(buffer, offset + 1);
		EnsurePlotName(name);
		PlotBuilder plot = EnsurePlot(name);
		plot.Format = buffer[offset + 9];
	}

	private void AddContinuousFrame(ulong name, long time)
	{
		EnsureFrameName(name);
		if (!m_ContinuousFrameStarts.TryGetValue(name, out List<long> starts))
		{
			starts = new List<long>();
			m_ContinuousFrameStarts[name] = starts;
		}
		starts.Add(time);
		UpdateLastTime(time);
	}

	private void AddFrameStart(ulong name, long time)
	{
		EnsureFrameName(name);
		m_OpenFrames[name] = new TracyFrameSummary(0, time, -1);
		UpdateLastTime(time);
	}

	private void AddFrameEnd(ulong name, long time)
	{
		EnsureFrameName(name);
		if (!m_OpenFrames.TryGetValue(name, out TracyFrameSummary start))
		{
			IncrementUnsupported("FrameEndWithoutStart");
			return;
		}
		m_OpenFrames.Remove(name);
		if (!m_DiscreteFrames.TryGetValue(name, out List<TracyFrameSummary> frames))
		{
			frames = new List<TracyFrameSummary>();
			m_DiscreteFrames[name] = frames;
		}
		frames.Add(new TracyFrameSummary(frames.Count, start.Start, time));
		UpdateLastTime(time);
	}

	private List<TracyFrameSetSummary> BuildFrameSets()
	{
		List<TracyFrameSetSummary> frameSets = new List<TracyFrameSetSummary>();
		foreach (KeyValuePair<ulong, List<long>> pair in m_ContinuousFrameStarts.OrderBy(value => value.Key))
		{
			List<TracyFrameSummary> frames = new List<TracyFrameSummary>();
			for (int i = 0; i + 1 < pair.Value.Count; i++)
			{
				frames.Add(new TracyFrameSummary(i, pair.Value[i], pair.Value[i + 1]));
			}
			if (frames.Count > 0)
			{
				frameSets.Add(new TracyFrameSetSummary(pair.Key, true, frames));
			}
		}
		foreach (KeyValuePair<ulong, List<TracyFrameSummary>> pair in m_DiscreteFrames.OrderBy(value => value.Key))
		{
			if (pair.Value.Count > 0)
			{
				frameSets.Add(new TracyFrameSetSummary(pair.Key, false, pair.Value));
			}
		}
		return frameSets;
	}

	private List<TracyPlotSummary> BuildPlots()
	{
		List<TracyPlotSummary> plots = new List<TracyPlotSummary>();
		foreach (PlotBuilder builder in m_Plots.Values.OrderBy(value => ResolveString(value.NamePointer)))
		{
			if (builder.Samples.Count == 0)
			{
				continue;
			}
			double min = builder.Samples.Min(sample => sample.Value);
			double max = builder.Samples.Max(sample => sample.Value);
			double sum = builder.Samples.Sum(sample => sample.Value);
			plots.Add(new TracyPlotSummary(ResolveString(builder.NamePointer), 0, builder.Format, min, max, sum, builder.Samples.OrderBy(sample => sample.Time).ToList()));
		}
		return plots;
	}

	private List<TracyThreadSummary> BuildThreads()
	{
		return m_Threads.Keys
			.OrderBy(thread => thread)
			.Select(thread => new TracyThreadSummary(thread, m_ThreadNames.TryGetValue(thread, out string name) ? name : string.Empty))
			.ToList();
	}

	private ThreadState EnsureThread(ulong thread)
	{
		if (!m_Threads.TryGetValue(thread, out ThreadState state))
		{
			state = new ThreadState(thread);
			m_Threads[thread] = state;
			if (thread != 0 && m_RequestedThreadNames.Add(thread))
			{
				m_SendQuery((byte)ServerQuery.ThreadString, thread, 0);
			}
		}
		return state;
	}

	private void EnsureSourceLocation(ulong pointer)
	{
		if (pointer == 0 || m_SourceLocations.ContainsKey(pointer) || !m_RequestedSourceLocations.Add(pointer))
		{
			return;
		}
		m_SourceLocations[pointer] = new SourceLocationRecord();
		m_PendingSourceLocations.Enqueue(pointer);
		m_SendQuery((byte)ServerQuery.SourceLocation, pointer, 0);
	}

	private void EnsureString(ulong pointer)
	{
		if (pointer == 0 || m_Strings.ContainsKey(pointer) || !m_RequestedStrings.Add(pointer))
		{
			return;
		}
		m_SendQuery((byte)ServerQuery.String, pointer, 0);
	}

	private void EnsurePlotName(ulong pointer)
	{
		if (pointer == 0 || m_Strings.ContainsKey(pointer) || !m_RequestedPlotNames.Add(pointer))
		{
			return;
		}
		m_SendQuery((byte)ServerQuery.PlotName, pointer, 0);
	}

	private void EnsureFrameName(ulong pointer)
	{
		if (pointer == 0 || m_Strings.ContainsKey(pointer) || !m_RequestedFrameNames.Add(pointer))
		{
			return;
		}
		m_SendQuery((byte)ServerQuery.FrameName, pointer, 0);
	}

	private PlotBuilder EnsurePlot(ulong name)
	{
		if (!m_Plots.TryGetValue(name, out PlotBuilder plot))
		{
			plot = new PlotBuilder
			{
				NamePointer = name
			};
			m_Plots[name] = plot;
		}
		return plot;
	}

	private GpuContextState EnsureGpuContext(byte context)
	{
		if (!m_GpuContexts.TryGetValue(context, out GpuContextState state))
		{
			state = new GpuContextState
			{
				Context = context
			};
			m_GpuContexts[context] = state;
		}
		return state;
	}

	private void EnqueueGpuQueryZone(byte context, ushort queryId, GpuZoneState zone)
	{
		string key = GpuZoneKey(context, queryId);
		if (!m_GpuQueryZones.TryGetValue(key, out Queue<GpuZoneState> queue))
		{
			queue = new Queue<GpuZoneState>();
			m_GpuQueryZones[key] = queue;
		}
		queue.Enqueue(zone);
	}

	private Stack<GpuZoneState> GetGpuStack(GpuContextState contextState, byte context, uint thread)
	{
		uint stackThread = contextState.ThreadId == 0 ? thread : 0U;
		string key = GpuStackKey(context, stackThread);
		if (!m_OpenGpuZoneStacks.TryGetValue(key, out Stack<GpuZoneState> stack))
		{
			stack = new Stack<GpuZoneState>();
			m_OpenGpuZoneStacks[key] = stack;
		}
		return stack;
	}

	private ulong ConsumeGpuSourceLocationPayload()
	{
		ulong pointer = m_PendingGpuSourceLocationPayload;
		m_PendingGpuSourceLocationPayload = 0UL;
		if (pointer == 0UL)
		{
			IncrementUnsupported("GpuSourceLocationPayloadMissing");
		}
		return pointer;
	}

	private static string GpuZoneKey(byte context, ushort queryId)
	{
		return context + ":" + queryId;
	}

	private static string GpuStackKey(byte context, uint thread)
	{
		return context + ":" + thread;
	}

	private short GetSourceLocationIndex(ulong pointer)
	{
		if (!m_SourceLocationIndexes.TryGetValue(pointer, out short index))
		{
			index = (short)m_SourceLocationIndexes.Count;
			m_SourceLocationIndexes[pointer] = index;
		}
		return index;
	}

	private string ResolveSourceLocationName(ulong pointer)
	{
		if (m_SourceLocationPayloadNames.TryGetValue(pointer, out string payloadName) && !string.IsNullOrWhiteSpace(payloadName))
		{
			return payloadName;
		}
		if (m_SourceLocations.TryGetValue(pointer, out SourceLocationRecord record))
		{
			string name = ResolveString(record.Name);
			if (!string.IsNullOrWhiteSpace(name) && name != "0x0")
			{
				return name;
			}
			name = ResolveString(record.Function);
			if (!string.IsNullOrWhiteSpace(name) && name != "0x0")
			{
				return name;
			}
			string file = ResolveString(record.File);
			if (!string.IsNullOrWhiteSpace(file) && file != "0x0")
			{
				return record.Line == 0 ? file : file + ":" + record.Line;
			}
		}
		return "srcloc:0x" + pointer.ToString("x");
	}

	private string ResolveString(ulong pointer)
	{
		if (pointer == 0)
		{
			return string.Empty;
		}
		return m_Strings.TryGetValue(pointer, out string value) ? value : "0x" + pointer.ToString("x");
	}

	private long RefTime(long delta)
	{
		m_RefTimeThread += delta;
		return m_RefTimeThread;
	}

	private long RefTimeSerial(long delta)
	{
		m_RefTimeSerial += delta;
		return m_RefTimeSerial;
	}

	private long RefTimeGpu(long delta)
	{
		m_RefTimeGpu += delta;
		return m_RefTimeGpu;
	}

	private long ResolveGpuTime(GpuContextState context, long gpuTick)
	{
		if (!context.HasPeriod)
		{
			if (!context.HasCalibration)
			{
				return gpuTick + context.TimeDiff;
			}
			return (long)((gpuTick - context.CalibratedGpuTime) * context.CalibrationMod + context.CalibratedCpuTime);
		}
		double periodTime = context.Period * gpuTick;
		if (!context.HasCalibration)
		{
			return (long)periodTime + context.TimeDiff;
		}
		return (long)((periodTime - context.CalibratedGpuTime) * context.CalibrationMod + context.CalibratedCpuTime);
	}

	private static ulong NextPowerOfTwo(long value)
	{
		if (value <= 0)
		{
			return 1UL;
		}
		ulong result = 1UL;
		ulong target = (ulong)value;
		while (result <= target && result < (1UL << 63))
		{
			result <<= 1;
		}
		return result;
	}

	private long ToTime(long tsc)
	{
		return (long)((tsc - m_BaseTime) * m_TimerMultiplier);
	}

	private void UpdateLastTime(long time)
	{
		if (m_LastTime < time)
		{
			m_LastTime = time;
		}
	}

	private void IncrementUnsupported(QueueType type)
	{
		IncrementUnsupported(type.ToString());
	}

	private void IncrementUnsupported(string key)
	{
		m_UnsupportedCounts.TryGetValue(key, out int count);
		m_UnsupportedCounts[key] = count + 1;
	}

	private static void EnsureAvailable(byte[] buffer, int offset, int size)
	{
		if (offset < 0 || size < 0 || offset + size > buffer.Length)
		{
			throw new TracyFileFormatException("TracyLiveDecodeFailed", "Tracy live queue item is truncated.");
		}
	}

	private static ushort ReadUInt16(byte[] buffer, int offset)
	{
		return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
	}

	private static uint ReadUInt32(byte[] buffer, int offset)
	{
		return (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
	}

	private static ulong ReadUInt64(byte[] buffer, int offset)
	{
		ulong value = 0;
		for (int i = 0; i < 8; i++)
		{
			value |= (ulong)buffer[offset + i] << (8 * i);
		}
		return value;
	}

	private static long ReadInt64(byte[] buffer, int offset)
	{
		return unchecked((long)ReadUInt64(buffer, offset));
	}
}
