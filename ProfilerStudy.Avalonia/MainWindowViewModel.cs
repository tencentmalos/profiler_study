using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FramePro;
using SCLCoreCLR;

namespace ProfilerStudy.Avalonia;

internal sealed class MainWindowViewModel : ObservableObject
{
	private const int DefaultVisibleThreadTimelineThreads = 8;
	private const int CollapsedVisibleThreadTimelineThreads = 12;

	private readonly SessionLoader m_SessionLoader = new SessionLoader();
	private readonly RelayCommand m_OpenSessionCommand;
	private readonly RelayCommand m_LoadSampleCommand;
	private readonly RelayCommand m_ShowStartupPageCommand;
	private readonly RelayCommand m_CloseStartupPageCommand;
	private readonly RelayCommand m_CloseSessionCommand;
	private readonly RelayCommand m_CreateSessionFromSelectionCommand;
	private readonly RelayCommand m_SaveSessionCommand;
	private readonly RelayCommand m_SaveSessionAsCommand;
	private readonly RelayCommand m_ExportFramesToCsvCommand;
	private readonly RelayCommand m_ResetTimelineCommand;
	private readonly RelayCommand m_TrackSelectedFrameCommand;
	private readonly RelayCommand m_SelectLastFrameCommand;
	private readonly RelayCommand m_SelectSlowestFrameCommand;
	private readonly RelayCommand m_SelectPreviousSpikeCommand;
	private readonly RelayCommand m_SelectNextSpikeCommand;
	private readonly RelayCommand m_PanTimelineLeftCommand;
	private readonly RelayCommand m_PanTimelineRightCommand;
	private readonly RelayCommand m_ZoomTimelineInCommand;
	private readonly RelayCommand m_ZoomTimelineOutCommand;
	private readonly RelayCommand m_OpenRecentSessionCommand;
	private readonly RelayCommand m_OpenScopeSourceCommand;
	private readonly RelayCommand m_SortTableCommand;
	private readonly RelayCommand m_SelectSessionViewCommand;
	private readonly RelayCommand m_ClearThreadFilterCommand;
	private readonly RelayCommand m_ScrollThreadTimelineUpCommand;
	private readonly RelayCommand m_ScrollThreadTimelineDownCommand;
	private readonly RelayCommand m_FocusThreadCommand;
	private readonly RelayCommand m_HideFocusedThreadCommand;
	private readonly RelayCommand m_MoveFocusedThreadUpCommand;
	private readonly RelayCommand m_MoveFocusedThreadDownCommand;
	private readonly RelayCommand m_CollapseAllThreadsCommand;
	private readonly RelayCommand m_ExpandAllThreadsCommand;
	private readonly RelayCommand m_ThreadSettingsCommand;
	private readonly RelayCommand m_CloseThreadSettingsCommand;
	private readonly RelayCommand m_ToggleThreadsPanelCommand;
	private readonly RelayCommand m_ToggleOutputWindowCommand;
	private readonly RelayCommand m_ToggleToolboxCommand;
	private readonly RelayCommand m_ToggleMainSessionDockCommand;
	private readonly RelayCommand m_MinimizeOutputWindowCommand;
	private readonly RelayCommand m_RestoreOutputWindowCommand;
	private readonly RelayCommand m_MinimizeToolboxCommand;
	private readonly RelayCommand m_RestoreToolboxCommand;
	private readonly RelayCommand m_MinimizeMainSessionDockCommand;
	private readonly RelayCommand m_RestoreMainSessionDockCommand;
	private readonly RelayCommand m_FloatOutputWindowCommand;
	private readonly RelayCommand m_DockOutputWindowCommand;
	private readonly RelayCommand m_FloatToolboxCommand;
	private readonly RelayCommand m_DockToolboxCommand;
	private readonly RelayCommand m_FloatMainSessionDockCommand;
	private readonly RelayCommand m_DockMainSessionDockCommand;
	private readonly RelayCommand m_FindPreviousScopeCommand;
	private readonly RelayCommand m_FindNextScopeCommand;
	private readonly RelayCommand m_ClearScopeFindCommand;
	private readonly RelayCommand m_OpenFindScopeCommand;
	private readonly RelayCommand m_SetScopeColorModeCommand;
	private readonly RelayCommand m_ToggleScopeColorModeCommand;
	private readonly RelayCommand m_ExitCommand;
	private readonly RelayCommand m_DemoActionCommand;
	private readonly RelayCommand m_AdjustConditionalScopeTimeCommand;
	private readonly RelayCommand m_OpenSettingsCommand;
	private readonly RelayCommand m_CloseSettingsCommand;
	private readonly RelayCommand m_OpenAndroidCommand;
	private readonly RelayCommand m_CloseAndroidCommand;
	private readonly RelayCommand m_AndroidActionCommand;
	private readonly RelayCommand m_OpenConnectionCommand;
	private readonly RelayCommand m_CloseConnectionCommand;
	private readonly RelayCommand m_ConnectionActionCommand;
	private readonly RelayCommand m_OpenCallstacksCommand;
	private readonly RelayCommand m_CloseCallstacksCommand;
	private readonly RelayCommand m_CallstacksActionCommand;
	private readonly RelayCommand m_OpenHelpCommand;
	private readonly RelayCommand m_CloseHelpCommand;
	private readonly RelayCommand m_OpenRegistrationCommand;
	private readonly RelayCommand m_CloseRegistrationCommand;
	private readonly RelayCommand m_OpenUpdateCheckCommand;
	private readonly RelayCommand m_CloseUpdateCheckCommand;
	private readonly RelayCommand m_OpenAboutCommand;
	private readonly RelayCommand m_CloseAboutCommand;
	private readonly RelayCommand m_SetLightThemeCommand;
	private readonly RelayCommand m_SetDarkThemeCommand;
	private readonly RelayCommand m_SetThemeColorCommand;
	private readonly ISourceViewerLauncher m_SourceViewerLauncher;
	private readonly IAppSettingsService m_AppSettingsService;
	private readonly AppSettings m_AppSettings;
	private readonly AppThemeService m_AppThemeService;
	private CancellationTokenSource m_LoadCancellation;
	private Session m_LiveConnectionSession;
	private DispatcherTimer m_LiveConnectionRefreshTimer;
	private string m_LiveConnectionName;
	private int m_LastLiveFrameCount = -1;
	private Process m_GameSimulatorProcess;
	private Process m_RecordingPlayerProcess;
	private SessionDocument m_CurrentDocument;
	private bool m_IsStartupPageVisible;
	private IReadOnlyList<FrameSample> m_FrameSamples = Array.Empty<FrameSample>();
	private IReadOnlyList<string> m_RecentFiles = Array.Empty<string>();
	private string m_SelectedRecentFile;
	private string m_CapturedSourceRoot;
	private string m_LocalSourceRoot;
	private TimelineSelection m_Selection = new TimelineSelection();
	private TimelineViewport m_Viewport = TimelineViewport.CreateForFrames(0);
	private IReadOnlyList<ScopeHotspotRow> m_AllScopeHotspots = Array.Empty<ScopeHotspotRow>();
	private IReadOnlyList<ScopeHotspotRow> m_ScopeHotspots = Array.Empty<ScopeHotspotRow>();
	private IReadOnlyList<ThreadTimelineScopeRow> m_VisibleThreadScopes = Array.Empty<ThreadTimelineScopeRow>();
	private IReadOnlyList<ScopeFrameDetailRow> m_SelectedFrameScopes = Array.Empty<ScopeFrameDetailRow>();
	private IReadOnlyList<SelectedFrameCounterRow> m_SelectedFrameCounters = Array.Empty<SelectedFrameCounterRow>();
	private IReadOnlyList<LogMessageRow> m_LogRows = Array.Empty<LogMessageRow>();
	private IReadOnlyList<CoreSummaryRow> m_CoreRows = Array.Empty<CoreSummaryRow>();
	private string m_StatusText = "Open a profiler file or load generated sample data.";
	private string m_OutputLogText = "Open a profiler file or load generated sample data.";
	private string m_FooterText = "No profiler session loaded";
	private string m_TimelineRangeText = string.Empty;
	private string m_SessionStatusText = "Session: none";
	private string m_SelectedFrameStatusText = "Frame: none";
	private string m_StatusBarSessionText = "none";
	private string m_StatusBarSelectedFrameText = "none";
	private string m_StatusBarHoveredFrameText = "none";
	private string m_ConditionalScopeTimeText = "All scope times";
	private double m_ConditionalScopeTimeMs;
	private double m_ConditionalScopeTimeWidth;
	private string m_ScopeFilterText = string.Empty;
	private string m_ThreadFilterText = string.Empty;
	private string m_ThreadTimelineSummaryText = "No profiler thread scope data loaded.";
	private string m_ScopeHotspotSummaryText = "No profiler scope data loaded.";
	private string m_SelectedFrameScopeSummaryText = "Select a frame to inspect scopes.";
	private string m_SelectedFrameCounterSummaryText = "Select a frame to inspect counters.";
	private string m_LogSummaryText = "No profiler log messages loaded.";
	private string m_CoreSummaryText = "No profiler core data loaded.";
	private string m_ActiveSessionView = "Threads";
	private int m_ThreadTimelineFirstVisibleThreadIndex;
	private int m_ThreadTimelineTotalThreadCount;
	private int m_MaxVisibleThreadTimelineThreads = DefaultVisibleThreadTimelineThreads;
	private string m_FocusedThreadName;
	private bool m_AreThreadScopesCollapsed;
	private bool m_IsThreadsInfoPanelVisible;
	private bool m_IsThreadsFrameGraphVisible = true;
	private bool m_IsThreadsScopeGraphVisible = true;
	private bool m_IsThreadsCoreGraphVisible = true;
	private bool m_IsThreadsCustomStatsVisible;
	private bool m_IsThreadsDataGridVisible;
	private bool m_IsThreadSettingsPanelVisible;
	private bool m_IsOutputWindowVisible = true;
	private bool m_IsOutputWindowMinimized;
	private bool m_IsOutputWindowFloating;
	private bool m_IsToolboxVisible;
	private bool m_IsToolboxMinimized;
	private bool m_IsToolboxFloating;
	private bool m_IsMainSessionDockVisible = true;
	private bool m_IsMainSessionDockMinimized;
	private bool m_IsMainSessionDockFloating;
	private bool m_IsSettingsPanelVisible;
	private bool m_IsAndroidPanelVisible;
	private string m_AndroidPanelStatusText = "No Android recording loaded.";
	private string m_AndroidEndpoint = AdbSocketDiscovery.DebugFrameProEndpoint;
	private string m_AndroidRecordingDuration = "5";
	private bool m_IsConnectionPanelVisible;
	private bool m_IsConnecting;
	private string m_ConnectionHost = "localhost";
	private string m_ConnectionPort = "8428";
	private string m_ConnectionPanelStatusText = "Not connected.";
	private bool m_IsCallstacksPanelVisible;
	private string m_CallstacksPanelStatusText = "No callstack capture loaded.";
	private bool m_IsHelpPanelVisible;
	private bool m_IsRegistrationPanelVisible;
	private bool m_IsUpdateCheckPanelVisible;
	private bool m_IsAboutPanelVisible;
	private int m_ThemeRevision;
	private string m_ScopeColorMode = "Thread";
	private readonly List<string> m_ThreadTimelineThreadOrder = new List<string>();
	private readonly HashSet<string> m_HiddenThreadNames = new HashSet<string>(StringComparer.Ordinal);
	private string m_ScopeHotspotSortKey = "TotalTime";
	private bool m_ScopeHotspotSortDescending = true;
	private string m_SelectedFrameScopeSortKey = "Start";
	private bool m_SelectedFrameScopeSortDescending;
	private string m_SelectedFrameCounterSortKey = "Graph";
	private bool m_SelectedFrameCounterSortDescending;
	private bool m_IsLoading;

	private readonly struct ProcessRunResult
	{
		public ProcessRunResult(bool success, string output)
		{
			Success = success;
			Output = output ?? string.Empty;
		}

		public bool Success { get; }

		public string Output { get; }
	}

	public MainWindowViewModel(bool loadSampleOnStartup = false, string startupProfilerPath = null)
		: this(new SourceViewerLauncher(), new AppSettingsService(), loadSampleOnStartup, startupProfilerPath)
	{
	}

	internal MainWindowViewModel(ISourceViewerLauncher sourceViewerLauncher, IAppSettingsService appSettingsService, bool loadSampleOnStartup = false, string startupProfilerPath = null)
	{
		m_SourceViewerLauncher = sourceViewerLauncher ?? new SourceViewerLauncher();
		m_AppSettingsService = appSettingsService ?? new AppSettingsService();
		m_AppSettings = m_AppSettingsService.Load() ?? new AppSettings();
		m_AppThemeService = AppThemeService.Create(m_AppSettings, SaveAppSettings);
		m_AppThemeService.Changed += OnThemeServiceChanged;
		Summary = new SessionSummaryViewModel();
		m_OpenSessionCommand = new RelayCommand(_ => _ = OpenSessionAsync());
		m_LoadSampleCommand = new RelayCommand(_ => _ = LoadSampleAsync());
		m_ShowStartupPageCommand = new RelayCommand(_ => IsStartupPageVisible = true);
		m_CloseStartupPageCommand = new RelayCommand(_ => IsStartupPageVisible = false);
		m_CloseSessionCommand = new RelayCommand(_ => CloseSession(), _ => CurrentDocument != null);
		m_CreateSessionFromSelectionCommand = new RelayCommand(_ => _ = CreateSessionFromSelectionAsync(), _ => CurrentDocument?.Session != null);
		m_SaveSessionCommand = new RelayCommand(_ => _ = SaveSessionAsync(false), _ => CurrentDocument?.Session != null);
		m_SaveSessionAsCommand = new RelayCommand(_ => _ = SaveSessionAsync(true), _ => CurrentDocument?.Session != null);
		m_ExportFramesToCsvCommand = new RelayCommand(_ => _ = ExportFramesToCsvAsync(), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_ResetTimelineCommand = new RelayCommand(_ => ResetTimeline(), _ => CurrentDocument != null);
		m_TrackSelectedFrameCommand = new RelayCommand(_ => TrackSelectedFrame(), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_SelectLastFrameCommand = new RelayCommand(_ => SelectLastFrame(), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_SelectSlowestFrameCommand = new RelayCommand(_ => SelectSlowestFrame(), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_SelectPreviousSpikeCommand = new RelayCommand(_ => SelectAdjacentSpike(-1), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_SelectNextSpikeCommand = new RelayCommand(_ => SelectAdjacentSpike(1), _ => CurrentDocument?.FrameSamples?.Length > 0);
		m_PanTimelineLeftCommand = new RelayCommand(_ => PanTimeline(-1), _ => CurrentDocument != null);
		m_PanTimelineRightCommand = new RelayCommand(_ => PanTimeline(1), _ => CurrentDocument != null);
		m_ZoomTimelineInCommand = new RelayCommand(_ => ZoomTimeline(0.8), _ => CurrentDocument != null);
		m_ZoomTimelineOutCommand = new RelayCommand(_ => ZoomTimeline(1.25), _ => CurrentDocument != null);
		m_OpenRecentSessionCommand = new RelayCommand(parameter => _ = OpenRecentSessionAsync(parameter as string ?? SelectedRecentFile), _ => !string.IsNullOrWhiteSpace(SelectedRecentFile));
		m_OpenScopeSourceCommand = new RelayCommand(OpenScopeSource, parameter => parameter is ScopeFrameDetailRow row && row.CanOpenSource);
		m_SortTableCommand = new RelayCommand(SortTable);
		m_SelectSessionViewCommand = new RelayCommand(SelectSessionView);
		m_ClearThreadFilterCommand = new RelayCommand(_ => ThreadFilterText = string.Empty, _ => !string.IsNullOrWhiteSpace(ThreadFilterText));
		m_ScrollThreadTimelineUpCommand = new RelayCommand(_ => ScrollThreadTimeline(-1), _ => ThreadTimelineFirstVisibleThreadIndex > 0);
		m_ScrollThreadTimelineDownCommand = new RelayCommand(_ => ScrollThreadTimeline(1), _ => ThreadTimelineFirstVisibleThreadIndex + m_MaxVisibleThreadTimelineThreads < ThreadTimelineTotalThreadCount);
		m_FocusThreadCommand = new RelayCommand(FocusThread, parameter => parameter is string threadName && !string.IsNullOrWhiteSpace(threadName));
		m_HideFocusedThreadCommand = new RelayCommand(_ => HideFocusedThread(), _ => !string.IsNullOrWhiteSpace(FocusedThreadName));
		m_MoveFocusedThreadUpCommand = new RelayCommand(_ => MoveFocusedThread(-1), _ => CanMoveFocusedThread(-1));
		m_MoveFocusedThreadDownCommand = new RelayCommand(_ => MoveFocusedThread(1), _ => CanMoveFocusedThread(1));
		m_CollapseAllThreadsCommand = new RelayCommand(_ => SetThreadScopeCollapseState(true));
		m_ExpandAllThreadsCommand = new RelayCommand(_ => SetThreadScopeCollapseState(false));
		m_ThreadSettingsCommand = new RelayCommand(_ => OpenThreadSettingsPanel());
		m_CloseThreadSettingsCommand = new RelayCommand(_ => CloseThreadSettingsPanel());
		m_ToggleThreadsPanelCommand = new RelayCommand(ToggleThreadsPanel);
		m_ToggleOutputWindowCommand = new RelayCommand(_ => ToggleOutputWindow());
		m_ToggleToolboxCommand = new RelayCommand(_ => ToggleToolbox());
		m_ToggleMainSessionDockCommand = new RelayCommand(_ => ToggleMainSessionDock());
		m_MinimizeOutputWindowCommand = new RelayCommand(_ => MinimizeOutputWindow());
		m_RestoreOutputWindowCommand = new RelayCommand(_ => RestoreOutputWindow());
		m_MinimizeToolboxCommand = new RelayCommand(_ => MinimizeToolbox());
		m_RestoreToolboxCommand = new RelayCommand(_ => RestoreToolbox());
		m_MinimizeMainSessionDockCommand = new RelayCommand(_ => MinimizeMainSessionDock());
		m_RestoreMainSessionDockCommand = new RelayCommand(_ => RestoreMainSessionDock());
		m_FloatOutputWindowCommand = new RelayCommand(_ => FloatOutputWindow());
		m_DockOutputWindowCommand = new RelayCommand(_ => DockOutputWindow());
		m_FloatToolboxCommand = new RelayCommand(_ => FloatToolbox());
		m_DockToolboxCommand = new RelayCommand(_ => DockToolbox());
		m_FloatMainSessionDockCommand = new RelayCommand(_ => FloatMainSessionDock());
		m_DockMainSessionDockCommand = new RelayCommand(_ => DockMainSessionDock());
		m_FindPreviousScopeCommand = new RelayCommand(_ => FindScope(-1), _ => CanFindScope());
		m_FindNextScopeCommand = new RelayCommand(_ => FindScope(1), _ => CanFindScope());
		m_ClearScopeFindCommand = new RelayCommand(_ => ScopeFilterText = string.Empty, _ => !string.IsNullOrWhiteSpace(ScopeFilterText));
		m_OpenFindScopeCommand = new RelayCommand(_ => OpenFindScope());
		m_SetScopeColorModeCommand = new RelayCommand(SetScopeColorMode);
		m_ToggleScopeColorModeCommand = new RelayCommand(_ => ToggleScopeColorMode());
		m_ExitCommand = new RelayCommand(_ => ExitApplication());
		m_DemoActionCommand = new RelayCommand(parameter => _ = HandleDemoActionAsync(parameter));
		m_AdjustConditionalScopeTimeCommand = new RelayCommand(AdjustConditionalScopeTime);
		m_OpenSettingsCommand = new RelayCommand(_ => OpenSettingsPanel());
		m_CloseSettingsCommand = new RelayCommand(_ => CloseSettingsPanel());
		m_OpenAndroidCommand = new RelayCommand(_ => OpenAndroidPanel());
		m_CloseAndroidCommand = new RelayCommand(_ => CloseAndroidPanel());
		m_AndroidActionCommand = new RelayCommand(parameter => _ = HandleAndroidActionAsync(parameter));
		m_OpenConnectionCommand = new RelayCommand(_ => OpenConnectionPanel());
		m_CloseConnectionCommand = new RelayCommand(_ => CloseConnectionPanel());
		m_ConnectionActionCommand = new RelayCommand(parameter => _ = HandleConnectionActionAsync(parameter));
		m_OpenCallstacksCommand = new RelayCommand(_ => OpenCallstacksPanel());
		m_CloseCallstacksCommand = new RelayCommand(_ => CloseCallstacksPanel());
		m_CallstacksActionCommand = new RelayCommand(ShowCallstacksActionStatus);
		m_OpenHelpCommand = new RelayCommand(_ => OpenHelpPanel());
		m_CloseHelpCommand = new RelayCommand(_ => CloseHelpPanel());
		m_OpenRegistrationCommand = new RelayCommand(_ => OpenRegistrationPanel());
		m_CloseRegistrationCommand = new RelayCommand(_ => CloseRegistrationPanel());
		m_OpenUpdateCheckCommand = new RelayCommand(_ => OpenUpdateCheckPanel());
		m_CloseUpdateCheckCommand = new RelayCommand(_ => CloseUpdateCheckPanel());
		m_OpenAboutCommand = new RelayCommand(_ => OpenAboutPanel());
		m_CloseAboutCommand = new RelayCommand(_ => CloseAboutPanel());
		m_SetLightThemeCommand = new RelayCommand(_ => SetBaseTheme("Light"));
		m_SetDarkThemeCommand = new RelayCommand(_ => SetBaseTheme("Dark"));
		m_SetThemeColorCommand = new RelayCommand(SetThemeColor);
		m_CapturedSourceRoot = m_AppSettings.CapturedSourceRoot ?? string.Empty;
		m_LocalSourceRoot = m_AppSettings.LocalSourceRoot ?? string.Empty;
		ApplyRecentFiles(m_AppSettings.RecentFiles);
		AttachTimelineState(Selection, Viewport);
		if (!string.IsNullOrWhiteSpace(startupProfilerPath))
		{
			_ = OpenStartupProfilerSafelyAsync(startupProfilerPath);
		}
		else if (loadSampleOnStartup)
		{
			_ = LoadSampleAsync();
		}
	}

	private async Task OpenStartupProfilerSafelyAsync(string startupProfilerPath)
	{
		try
		{
			await OpenStartupProfilerAsync(startupProfilerPath);
		}
		catch (Exception ex)
		{
			StatusText = "Failed to load startup profiler file: " + ex.Message;
		}
	}

	public RelayCommand OpenSessionCommand => m_OpenSessionCommand;

	public RelayCommand LoadSampleCommand => m_LoadSampleCommand;

	public RelayCommand ShowStartupPageCommand => m_ShowStartupPageCommand;

	public RelayCommand CloseStartupPageCommand => m_CloseStartupPageCommand;

	public RelayCommand CloseSessionCommand => m_CloseSessionCommand;

	public RelayCommand CreateSessionFromSelectionCommand => m_CreateSessionFromSelectionCommand;

	public RelayCommand SaveSessionCommand => m_SaveSessionCommand;

	public RelayCommand SaveSessionAsCommand => m_SaveSessionAsCommand;

	public RelayCommand ExportFramesToCsvCommand => m_ExportFramesToCsvCommand;

	public RelayCommand ResetTimelineCommand => m_ResetTimelineCommand;

	public RelayCommand TrackSelectedFrameCommand => m_TrackSelectedFrameCommand;

	public RelayCommand SelectLastFrameCommand => m_SelectLastFrameCommand;

	public RelayCommand SelectSlowestFrameCommand => m_SelectSlowestFrameCommand;

	public RelayCommand SelectPreviousSpikeCommand => m_SelectPreviousSpikeCommand;

	public RelayCommand SelectNextSpikeCommand => m_SelectNextSpikeCommand;

	public RelayCommand PanTimelineLeftCommand => m_PanTimelineLeftCommand;

	public RelayCommand PanTimelineRightCommand => m_PanTimelineRightCommand;

	public RelayCommand ZoomTimelineInCommand => m_ZoomTimelineInCommand;

	public RelayCommand ZoomTimelineOutCommand => m_ZoomTimelineOutCommand;

	public RelayCommand OpenRecentSessionCommand => m_OpenRecentSessionCommand;

	public RelayCommand OpenScopeSourceCommand => m_OpenScopeSourceCommand;

	public RelayCommand SortTableCommand => m_SortTableCommand;

	public RelayCommand SelectSessionViewCommand => m_SelectSessionViewCommand;

	public RelayCommand ClearThreadFilterCommand => m_ClearThreadFilterCommand;

	public RelayCommand ScrollThreadTimelineUpCommand => m_ScrollThreadTimelineUpCommand;

	public RelayCommand ScrollThreadTimelineDownCommand => m_ScrollThreadTimelineDownCommand;

	public RelayCommand FocusThreadCommand => m_FocusThreadCommand;

	public RelayCommand HideFocusedThreadCommand => m_HideFocusedThreadCommand;

	public RelayCommand MoveFocusedThreadUpCommand => m_MoveFocusedThreadUpCommand;

	public RelayCommand MoveFocusedThreadDownCommand => m_MoveFocusedThreadDownCommand;

	public RelayCommand CollapseAllThreadsCommand => m_CollapseAllThreadsCommand;

	public RelayCommand ExpandAllThreadsCommand => m_ExpandAllThreadsCommand;

	public RelayCommand ThreadSettingsCommand => m_ThreadSettingsCommand;

	public RelayCommand CloseThreadSettingsCommand => m_CloseThreadSettingsCommand;

	public RelayCommand ToggleThreadsPanelCommand => m_ToggleThreadsPanelCommand;

	public RelayCommand ToggleOutputWindowCommand => m_ToggleOutputWindowCommand;

	public RelayCommand ToggleToolboxCommand => m_ToggleToolboxCommand;

	public RelayCommand ToggleMainSessionDockCommand => m_ToggleMainSessionDockCommand;

	public RelayCommand MinimizeOutputWindowCommand => m_MinimizeOutputWindowCommand;

	public RelayCommand RestoreOutputWindowCommand => m_RestoreOutputWindowCommand;

	public RelayCommand MinimizeToolboxCommand => m_MinimizeToolboxCommand;

	public RelayCommand RestoreToolboxCommand => m_RestoreToolboxCommand;

	public RelayCommand MinimizeMainSessionDockCommand => m_MinimizeMainSessionDockCommand;

	public RelayCommand RestoreMainSessionDockCommand => m_RestoreMainSessionDockCommand;

	public RelayCommand FloatOutputWindowCommand => m_FloatOutputWindowCommand;

	public RelayCommand DockOutputWindowCommand => m_DockOutputWindowCommand;

	public RelayCommand FloatToolboxCommand => m_FloatToolboxCommand;

	public RelayCommand DockToolboxCommand => m_DockToolboxCommand;

	public RelayCommand FloatMainSessionDockCommand => m_FloatMainSessionDockCommand;

	public RelayCommand DockMainSessionDockCommand => m_DockMainSessionDockCommand;

	public RelayCommand FindPreviousScopeCommand => m_FindPreviousScopeCommand;

	public RelayCommand FindNextScopeCommand => m_FindNextScopeCommand;

	public RelayCommand ClearScopeFindCommand => m_ClearScopeFindCommand;

	public RelayCommand OpenFindScopeCommand => m_OpenFindScopeCommand;

	public RelayCommand SetScopeColorModeCommand => m_SetScopeColorModeCommand;

	public RelayCommand ToggleScopeColorModeCommand => m_ToggleScopeColorModeCommand;

	public RelayCommand ExitCommand => m_ExitCommand;

	public RelayCommand DemoActionCommand => m_DemoActionCommand;

	public RelayCommand AdjustConditionalScopeTimeCommand => m_AdjustConditionalScopeTimeCommand;

	public RelayCommand OpenSettingsCommand => m_OpenSettingsCommand;

	public RelayCommand CloseSettingsCommand => m_CloseSettingsCommand;

	public RelayCommand OpenAndroidCommand => m_OpenAndroidCommand;

	public RelayCommand CloseAndroidCommand => m_CloseAndroidCommand;

	public RelayCommand AndroidActionCommand => m_AndroidActionCommand;

	public RelayCommand OpenConnectionCommand => m_OpenConnectionCommand;

	public RelayCommand CloseConnectionCommand => m_CloseConnectionCommand;

	public RelayCommand ConnectionActionCommand => m_ConnectionActionCommand;

	public RelayCommand OpenCallstacksCommand => m_OpenCallstacksCommand;

	public RelayCommand CloseCallstacksCommand => m_CloseCallstacksCommand;

	public RelayCommand CallstacksActionCommand => m_CallstacksActionCommand;

	public RelayCommand OpenHelpCommand => m_OpenHelpCommand;

	public RelayCommand CloseHelpCommand => m_CloseHelpCommand;

	public RelayCommand OpenRegistrationCommand => m_OpenRegistrationCommand;

	public RelayCommand CloseRegistrationCommand => m_CloseRegistrationCommand;

	public RelayCommand OpenUpdateCheckCommand => m_OpenUpdateCheckCommand;

	public RelayCommand CloseUpdateCheckCommand => m_CloseUpdateCheckCommand;

	public RelayCommand OpenAboutCommand => m_OpenAboutCommand;

	public RelayCommand CloseAboutCommand => m_CloseAboutCommand;

	public RelayCommand SetLightThemeCommand => m_SetLightThemeCommand;

	public RelayCommand SetDarkThemeCommand => m_SetDarkThemeCommand;

	public RelayCommand SetThemeColorCommand => m_SetThemeColorCommand;

	public IReadOnlyList<AppThemeColorOption> ThemeColorOptions => m_AppThemeService.ColorThemes;

	public bool IsLightThemeActive => m_AppThemeService.IsLightThemeActive;

	public bool IsDarkThemeActive => m_AppThemeService.IsDarkThemeActive;

	public string ActiveColorThemeName => m_AppThemeService.ActiveColorThemeName;

	public int ThemeRevision
	{
		get => m_ThemeRevision;
		private set => SetProperty(ref m_ThemeRevision, value);
	}

	public SessionSummaryViewModel Summary { get; }

	public SessionDocument CurrentDocument
	{
		get => m_CurrentDocument;
		private set
		{
			if (SetProperty(ref m_CurrentDocument, value))
			{
				if (value != null)
				{
					IsStartupPageVisible = false;
				}
				m_CloseSessionCommand.RaiseCanExecuteChanged();
				m_CreateSessionFromSelectionCommand.RaiseCanExecuteChanged();
				m_SaveSessionCommand.RaiseCanExecuteChanged();
				m_SaveSessionAsCommand.RaiseCanExecuteChanged();
				m_ExportFramesToCsvCommand.RaiseCanExecuteChanged();
				m_ResetTimelineCommand.RaiseCanExecuteChanged();
				m_TrackSelectedFrameCommand.RaiseCanExecuteChanged();
				m_SelectLastFrameCommand.RaiseCanExecuteChanged();
				m_SelectSlowestFrameCommand.RaiseCanExecuteChanged();
				m_SelectPreviousSpikeCommand.RaiseCanExecuteChanged();
				m_SelectNextSpikeCommand.RaiseCanExecuteChanged();
				m_PanTimelineLeftCommand.RaiseCanExecuteChanged();
				m_PanTimelineRightCommand.RaiseCanExecuteChanged();
				m_ZoomTimelineInCommand.RaiseCanExecuteChanged();
				m_ZoomTimelineOutCommand.RaiseCanExecuteChanged();
				m_FindPreviousScopeCommand.RaiseCanExecuteChanged();
				m_FindNextScopeCommand.RaiseCanExecuteChanged();
				UpdateShellStatusSegments();
			}
		}
	}

	public bool IsStartupPageVisible
	{
		get => m_IsStartupPageVisible;
		private set => SetProperty(ref m_IsStartupPageVisible, value);
	}

	public IReadOnlyList<FrameSample> FrameSamples
	{
		get => m_FrameSamples;
		private set => SetProperty(ref m_FrameSamples, value);
	}

	public IReadOnlyList<string> RecentFiles
	{
		get => m_RecentFiles;
		private set => SetProperty(ref m_RecentFiles, value);
	}

	public string SelectedRecentFile
	{
		get => m_SelectedRecentFile;
		set
		{
			if (SetProperty(ref m_SelectedRecentFile, value))
			{
				m_OpenRecentSessionCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public string CapturedSourceRoot
	{
		get => m_CapturedSourceRoot;
		set
		{
			if (SetProperty(ref m_CapturedSourceRoot, value ?? string.Empty))
			{
				SaveSourcePathSettings();
			}
		}
	}

	public string LocalSourceRoot
	{
		get => m_LocalSourceRoot;
		set
		{
			if (SetProperty(ref m_LocalSourceRoot, value ?? string.Empty))
			{
				SaveSourcePathSettings();
			}
		}
	}

	public TimelineSelection Selection
	{
		get => m_Selection;
		private set
		{
			if (SetProperty(ref m_Selection, value))
			{
				RaisePropertyChanged(nameof(Selection));
			}
		}
	}

	public TimelineViewport Viewport
	{
		get => m_Viewport;
		private set
		{
			if (SetProperty(ref m_Viewport, value))
			{
				RaisePropertyChanged(nameof(Viewport));
			}
		}
	}

	public string StatusText
	{
		get => m_StatusText;
		private set
		{
			if (SetProperty(ref m_StatusText, value))
			{
				AppendOutputLog(value);
			}
		}
	}

	public string OutputLogText
	{
		get => m_OutputLogText;
		private set => SetProperty(ref m_OutputLogText, value);
	}

	public string FooterText
	{
		get => m_FooterText;
		private set => SetProperty(ref m_FooterText, value);
	}

	public string TimelineRangeText
	{
		get => m_TimelineRangeText;
		private set => SetProperty(ref m_TimelineRangeText, value);
	}

	public string SessionStatusText
	{
		get => m_SessionStatusText;
		private set => SetProperty(ref m_SessionStatusText, value);
	}

	public string SelectedFrameStatusText
	{
		get => m_SelectedFrameStatusText;
		private set => SetProperty(ref m_SelectedFrameStatusText, value);
	}

	public string StatusBarSessionText
	{
		get => m_StatusBarSessionText;
		private set => SetProperty(ref m_StatusBarSessionText, value);
	}

	public string StatusBarSelectedFrameText
	{
		get => m_StatusBarSelectedFrameText;
		private set => SetProperty(ref m_StatusBarSelectedFrameText, value);
	}

	public string StatusBarHoveredFrameText
	{
		get => m_StatusBarHoveredFrameText;
		private set => SetProperty(ref m_StatusBarHoveredFrameText, value);
	}

	public string ConditionalScopeTimeText
	{
		get => m_ConditionalScopeTimeText;
		private set => SetProperty(ref m_ConditionalScopeTimeText, value);
	}

	public double ConditionalScopeTimeWidth
	{
		get => m_ConditionalScopeTimeWidth;
		private set => SetProperty(ref m_ConditionalScopeTimeWidth, value);
	}

	public IReadOnlyList<ScopeHotspotRow> ScopeHotspots
	{
		get => m_ScopeHotspots;
		private set => SetProperty(ref m_ScopeHotspots, value);
	}

	public string ScopeFilterText
	{
		get => m_ScopeFilterText;
		set
		{
			if (SetProperty(ref m_ScopeFilterText, value ?? string.Empty))
			{
				ApplyScopeHotspotFilter();
				m_FindPreviousScopeCommand.RaiseCanExecuteChanged();
				m_FindNextScopeCommand.RaiseCanExecuteChanged();
				m_ClearScopeFindCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public string ScopeHotspotSummaryText
	{
		get => m_ScopeHotspotSummaryText;
		private set => SetProperty(ref m_ScopeHotspotSummaryText, value);
	}

	public string ThreadFilterText
	{
		get => m_ThreadFilterText;
		set
		{
			if (SetProperty(ref m_ThreadFilterText, value ?? string.Empty))
			{
				ThreadTimelineFirstVisibleThreadIndex = 0;
				m_ClearThreadFilterCommand.RaiseCanExecuteChanged();
				ApplyVisibleThreadTimeline();
			}
		}
	}

	public IReadOnlyList<ThreadTimelineScopeRow> VisibleThreadScopes
	{
		get => m_VisibleThreadScopes;
		private set => SetProperty(ref m_VisibleThreadScopes, value);
	}

	public string ThreadTimelineSummaryText
	{
		get => m_ThreadTimelineSummaryText;
		private set => SetProperty(ref m_ThreadTimelineSummaryText, value);
	}

	public int ThreadTimelineFirstVisibleThreadIndex
	{
		get => m_ThreadTimelineFirstVisibleThreadIndex;
		private set
		{
			if (SetProperty(ref m_ThreadTimelineFirstVisibleThreadIndex, Math.Max(0, value)))
			{
				m_ScrollThreadTimelineUpCommand.RaiseCanExecuteChanged();
				m_ScrollThreadTimelineDownCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public int ThreadTimelineTotalThreadCount
	{
		get => m_ThreadTimelineTotalThreadCount;
		private set
		{
			if (SetProperty(ref m_ThreadTimelineTotalThreadCount, Math.Max(0, value)))
			{
				m_ScrollThreadTimelineUpCommand.RaiseCanExecuteChanged();
				m_ScrollThreadTimelineDownCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public string FocusedThreadName
	{
		get => m_FocusedThreadName;
		private set
		{
			if (SetProperty(ref m_FocusedThreadName, value ?? string.Empty))
			{
				m_HideFocusedThreadCommand.RaiseCanExecuteChanged();
				m_MoveFocusedThreadUpCommand.RaiseCanExecuteChanged();
				m_MoveFocusedThreadDownCommand.RaiseCanExecuteChanged();
			}
		}
	}

	public IReadOnlyList<ScopeFrameDetailRow> SelectedFrameScopes
	{
		get => m_SelectedFrameScopes;
		private set => SetProperty(ref m_SelectedFrameScopes, value);
	}

	public string SelectedFrameScopeSummaryText
	{
		get => m_SelectedFrameScopeSummaryText;
		private set => SetProperty(ref m_SelectedFrameScopeSummaryText, value);
	}

	public IReadOnlyList<SelectedFrameCounterRow> SelectedFrameCounters
	{
		get => m_SelectedFrameCounters;
		private set => SetProperty(ref m_SelectedFrameCounters, value);
	}

	public string SelectedFrameCounterSummaryText
	{
		get => m_SelectedFrameCounterSummaryText;
		private set => SetProperty(ref m_SelectedFrameCounterSummaryText, value);
	}

	public IReadOnlyList<LogMessageRow> LogRows
	{
		get => m_LogRows;
		private set => SetProperty(ref m_LogRows, value);
	}

	public string LogSummaryText
	{
		get => m_LogSummaryText;
		private set => SetProperty(ref m_LogSummaryText, value);
	}

	public IReadOnlyList<CoreSummaryRow> CoreRows
	{
		get => m_CoreRows;
		private set => SetProperty(ref m_CoreRows, value);
	}

	public string CoreSummaryText
	{
		get => m_CoreSummaryText;
		private set => SetProperty(ref m_CoreSummaryText, value);
	}

	public string ActiveSessionView
	{
		get => m_ActiveSessionView;
		private set
		{
			if (SetProperty(ref m_ActiveSessionView, value))
			{
				RaisePropertyChanged(nameof(ActiveSessionViewTitle));
				RaisePropertyChanged(nameof(IsThreadsViewActive));
				RaisePropertyChanged(nameof(IsCoresViewActive));
				RaisePropertyChanged(nameof(IsScopesViewActive));
				RaisePropertyChanged(nameof(IsCustomStatsViewActive));
				RaisePropertyChanged(nameof(IsLogViewActive));
				RaisePropertyChanged(nameof(ThreadsViewBrush));
				RaisePropertyChanged(nameof(CoresViewBrush));
				RaisePropertyChanged(nameof(ScopesViewBrush));
				RaisePropertyChanged(nameof(CustomStatsViewBrush));
				RaisePropertyChanged(nameof(LogViewBrush));
			}
		}
	}

	public string ActiveSessionViewTitle => ActiveSessionView + " View";

	public bool IsThreadsViewActive => ActiveSessionView == "Threads";

	public bool IsCoresViewActive => ActiveSessionView == "Cores";

	public bool IsScopesViewActive => ActiveSessionView == "Scopes";

	public bool IsCustomStatsViewActive => ActiveSessionView == "Custom Stats";

	public bool IsLogViewActive => ActiveSessionView == "Log";

	public bool IsThreadsInfoPanelVisible
	{
		get => m_IsThreadsInfoPanelVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsInfoPanelVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsInfoPanelCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsInfoPanelCollapsed => !IsThreadsInfoPanelVisible;

	public bool IsThreadsFrameGraphVisible
	{
		get => m_IsThreadsFrameGraphVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsFrameGraphVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsFrameGraphCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsFrameGraphCollapsed => !IsThreadsFrameGraphVisible;

	public bool IsThreadsScopeGraphVisible
	{
		get => m_IsThreadsScopeGraphVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsScopeGraphVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsScopeGraphCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsScopeGraphCollapsed => !IsThreadsScopeGraphVisible;

	public bool IsThreadsCoreGraphVisible
	{
		get => m_IsThreadsCoreGraphVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsCoreGraphVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsCoreGraphCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsCoreGraphCollapsed => !IsThreadsCoreGraphVisible;

	public bool IsThreadsCustomStatsVisible
	{
		get => m_IsThreadsCustomStatsVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsCustomStatsVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsCustomStatsCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsCustomStatsCollapsed => !IsThreadsCustomStatsVisible;

	public bool IsThreadsDataGridVisible
	{
		get => m_IsThreadsDataGridVisible;
		private set
		{
			if (SetProperty(ref m_IsThreadsDataGridVisible, value))
			{
				RaisePropertyChanged(nameof(IsThreadsDataGridCollapsed));
				RaiseThreadsLayoutPropertiesChanged();
			}
		}
	}

	public bool IsThreadsDataGridCollapsed => !IsThreadsDataGridVisible;

	public GridLength ThreadsInfoPanelHeight => IsThreadsInfoPanelVisible ? new GridLength(118) : new GridLength(0);

	public GridLength ThreadsFrameScrollbarHeight => IsThreadsFrameGraphVisible ? new GridLength(24) : new GridLength(0);

	public GridLength ThreadsFrameGraphHeight => IsThreadsFrameGraphVisible ? new GridLength(100) : new GridLength(0);

	public GridLength ThreadsFrameGraphSplitterHeight => IsThreadsFrameGraphVisible ? new GridLength(4) : new GridLength(0);

	public GridLength ThreadsScopeGraphHeight => IsThreadsScopeGraphVisible ? new GridLength(163) : new GridLength(0);

	public GridLength ThreadsScopeGraphSplitterHeight => IsThreadsScopeGraphVisible ? new GridLength(4) : new GridLength(0);

	public GridLength ThreadsDataGridSplitterWidth => IsThreadsDataGridVisible ? new GridLength(4) : new GridLength(0);

	public GridLength ThreadsDataGridColumnWidth => IsThreadsDataGridVisible ? new GridLength(422) : new GridLength(0);

	private void RaiseThreadsLayoutPropertiesChanged()
	{
		RaisePropertyChanged(nameof(ThreadsInfoPanelHeight));
		RaisePropertyChanged(nameof(ThreadsFrameScrollbarHeight));
		RaisePropertyChanged(nameof(ThreadsFrameGraphHeight));
		RaisePropertyChanged(nameof(ThreadsFrameGraphSplitterHeight));
		RaisePropertyChanged(nameof(ThreadsScopeGraphHeight));
		RaisePropertyChanged(nameof(ThreadsScopeGraphSplitterHeight));
		RaisePropertyChanged(nameof(ThreadsDataGridSplitterWidth));
		RaisePropertyChanged(nameof(ThreadsDataGridColumnWidth));
	}

	public bool IsThreadSettingsPanelVisible
	{
		get => m_IsThreadSettingsPanelVisible;
		private set => SetProperty(ref m_IsThreadSettingsPanelVisible, value);
	}

	public string ScopeColorMode
	{
		get => m_ScopeColorMode;
		private set
		{
			if (SetProperty(ref m_ScopeColorMode, value))
			{
				RaisePropertyChanged(nameof(IsScopeColorModeThread));
				RaisePropertyChanged(nameof(IsScopeColorModeScope));
				RaisePropertyChanged(nameof(ScopeColorModeText));
			}
		}
	}

	public bool IsScopeColorModeThread => ScopeColorMode == "Thread";

	public bool IsScopeColorModeScope => ScopeColorMode == "Scope";

	public string ScopeColorModeText => IsScopeColorModeThread ? "Thread" : "Scope";

	public bool IsOutputWindowVisible
	{
		get => m_IsOutputWindowVisible;
		private set
		{
			if (SetProperty(ref m_IsOutputWindowVisible, value))
			{
				RaisePropertyChanged(nameof(OutputWindowHeight));
				RaisePropertyChanged(nameof(IsOutputWindowExpanded));
				RaisePropertyChanged(nameof(IsOutputWindowMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsOutputWindowFloatingVisible));
			}
		}
	}

	public bool IsOutputWindowMinimized
	{
		get => m_IsOutputWindowMinimized;
		private set
		{
			if (SetProperty(ref m_IsOutputWindowMinimized, value))
			{
				RaisePropertyChanged(nameof(OutputWindowHeight));
				RaisePropertyChanged(nameof(IsOutputWindowExpanded));
				RaisePropertyChanged(nameof(IsOutputWindowMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsOutputWindowFloatingVisible));
			}
		}
	}

	public bool IsOutputWindowFloating
	{
		get => m_IsOutputWindowFloating;
		private set
		{
			if (SetProperty(ref m_IsOutputWindowFloating, value))
			{
				RaisePropertyChanged(nameof(OutputWindowHeight));
				RaisePropertyChanged(nameof(IsOutputWindowExpanded));
				RaisePropertyChanged(nameof(IsOutputWindowMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsOutputWindowFloatingVisible));
			}
		}
	}

	public bool IsOutputWindowExpanded => IsOutputWindowVisible && !IsOutputWindowMinimized && !IsOutputWindowFloating;

	public bool IsOutputWindowMinimizedBarVisible => IsOutputWindowVisible && IsOutputWindowMinimized && !IsOutputWindowFloating;

	public bool IsOutputWindowFloatingVisible => IsOutputWindowVisible && IsOutputWindowFloating;

	public GridLength OutputWindowHeight => !IsOutputWindowVisible || IsOutputWindowFloating ? new GridLength(0) : IsOutputWindowMinimized ? new GridLength(26) : new GridLength(150);

	public bool IsToolboxVisible
	{
		get => m_IsToolboxVisible;
		private set
		{
			if (SetProperty(ref m_IsToolboxVisible, value))
			{
				RaisePropertyChanged(nameof(ToolboxColumnWidth));
				RaisePropertyChanged(nameof(ToolboxSplitterWidth));
				RaisePropertyChanged(nameof(IsToolboxExpanded));
				RaisePropertyChanged(nameof(IsToolboxMinimizedTabVisible));
				RaisePropertyChanged(nameof(IsToolboxFloatingVisible));
			}
		}
	}

	public bool IsToolboxMinimized
	{
		get => m_IsToolboxMinimized;
		private set
		{
			if (SetProperty(ref m_IsToolboxMinimized, value))
			{
				RaisePropertyChanged(nameof(ToolboxColumnWidth));
				RaisePropertyChanged(nameof(ToolboxSplitterWidth));
				RaisePropertyChanged(nameof(IsToolboxExpanded));
				RaisePropertyChanged(nameof(IsToolboxMinimizedTabVisible));
				RaisePropertyChanged(nameof(IsToolboxFloatingVisible));
			}
		}
	}

	public bool IsToolboxFloating
	{
		get => m_IsToolboxFloating;
		private set
		{
			if (SetProperty(ref m_IsToolboxFloating, value))
			{
				RaisePropertyChanged(nameof(ToolboxColumnWidth));
				RaisePropertyChanged(nameof(ToolboxSplitterWidth));
				RaisePropertyChanged(nameof(IsToolboxExpanded));
				RaisePropertyChanged(nameof(IsToolboxMinimizedTabVisible));
				RaisePropertyChanged(nameof(IsToolboxFloatingVisible));
			}
		}
	}

	public bool IsToolboxExpanded => IsToolboxVisible && !IsToolboxMinimized && !IsToolboxFloating;

	public bool IsToolboxMinimizedTabVisible => IsToolboxVisible && IsToolboxMinimized && !IsToolboxFloating;

	public bool IsToolboxFloatingVisible => IsToolboxVisible && IsToolboxFloating;

	public GridLength ToolboxColumnWidth => !IsToolboxVisible || IsToolboxFloating ? new GridLength(0) : IsToolboxMinimized ? new GridLength(28) : new GridLength(220);

	public GridLength ToolboxSplitterWidth => IsToolboxExpanded ? new GridLength(4) : new GridLength(0);

	public bool IsMainSessionDockVisible
	{
		get => m_IsMainSessionDockVisible;
		private set
		{
			if (SetProperty(ref m_IsMainSessionDockVisible, value))
			{
				RaisePropertyChanged(nameof(IsMainSessionDockExpanded));
				RaisePropertyChanged(nameof(IsMainSessionDockMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsMainSessionDockFloatingVisible));
			}
		}
	}

	public bool IsMainSessionDockMinimized
	{
		get => m_IsMainSessionDockMinimized;
		private set
		{
			if (SetProperty(ref m_IsMainSessionDockMinimized, value))
			{
				RaisePropertyChanged(nameof(IsMainSessionDockExpanded));
				RaisePropertyChanged(nameof(IsMainSessionDockMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsMainSessionDockFloatingVisible));
			}
		}
	}

	public bool IsMainSessionDockFloating
	{
		get => m_IsMainSessionDockFloating;
		private set
		{
			if (SetProperty(ref m_IsMainSessionDockFloating, value))
			{
				RaisePropertyChanged(nameof(IsMainSessionDockExpanded));
				RaisePropertyChanged(nameof(IsMainSessionDockMinimizedBarVisible));
				RaisePropertyChanged(nameof(IsMainSessionDockFloatingVisible));
			}
		}
	}

	public bool IsMainSessionDockExpanded => IsMainSessionDockVisible && !IsMainSessionDockMinimized && !IsMainSessionDockFloating;

	public bool IsMainSessionDockMinimizedBarVisible => IsMainSessionDockVisible && IsMainSessionDockMinimized && !IsMainSessionDockFloating;

	public bool IsMainSessionDockFloatingVisible => IsMainSessionDockVisible && IsMainSessionDockFloating;

	public bool IsSettingsPanelVisible
	{
		get => m_IsSettingsPanelVisible;
		private set => SetProperty(ref m_IsSettingsPanelVisible, value);
	}

	public bool IsAndroidPanelVisible
	{
		get => m_IsAndroidPanelVisible;
		private set => SetProperty(ref m_IsAndroidPanelVisible, value);
	}

	public string AndroidPanelStatusText
	{
		get => m_AndroidPanelStatusText;
		private set => SetProperty(ref m_AndroidPanelStatusText, value);
	}

	public string AndroidEndpoint
	{
		get => m_AndroidEndpoint;
		set => SetProperty(ref m_AndroidEndpoint, value);
	}

	public string AndroidRecordingDuration
	{
		get => m_AndroidRecordingDuration;
		set => SetProperty(ref m_AndroidRecordingDuration, value);
	}

	public bool IsConnectionPanelVisible
	{
		get => m_IsConnectionPanelVisible;
		private set => SetProperty(ref m_IsConnectionPanelVisible, value);
	}

	public string ConnectionHost
	{
		get => m_ConnectionHost;
		set => SetProperty(ref m_ConnectionHost, value ?? string.Empty);
	}

	public bool IsAboutPanelVisible
	{
		get => m_IsAboutPanelVisible;
		private set => SetProperty(ref m_IsAboutPanelVisible, value);
	}

	public bool IsHelpPanelVisible
	{
		get => m_IsHelpPanelVisible;
		private set => SetProperty(ref m_IsHelpPanelVisible, value);
	}

	public bool IsRegistrationPanelVisible
	{
		get => m_IsRegistrationPanelVisible;
		private set => SetProperty(ref m_IsRegistrationPanelVisible, value);
	}

	public bool IsUpdateCheckPanelVisible
	{
		get => m_IsUpdateCheckPanelVisible;
		private set => SetProperty(ref m_IsUpdateCheckPanelVisible, value);
	}

	public string AboutVersionText => FrameProCore.Version;

	public string AboutProductText => "ProfilerStudy";

	public string AboutCopyrightText => "Copyright (C) PureDev Software Ltd";

	public string AboutWebsiteText => "www.puredevsoftware.com";

	public string AboutEmailText => "slynch@puredevsoftware.com";

	public string ConnectionPort
	{
		get => m_ConnectionPort;
		set => SetProperty(ref m_ConnectionPort, value ?? string.Empty);
	}

	public string ConnectionPanelStatusText
	{
		get => m_ConnectionPanelStatusText;
		private set => SetProperty(ref m_ConnectionPanelStatusText, value);
	}

	public bool IsCallstacksPanelVisible
	{
		get => m_IsCallstacksPanelVisible;
		private set => SetProperty(ref m_IsCallstacksPanelVisible, value);
	}

	public string CallstacksPanelStatusText
	{
		get => m_CallstacksPanelStatusText;
		private set => SetProperty(ref m_CallstacksPanelStatusText, value);
	}

	public IBrush ThreadsViewBrush => GetViewTabBrush(IsThreadsViewActive);

	public IBrush CoresViewBrush => GetViewTabBrush(IsCoresViewActive);

	public IBrush ScopesViewBrush => GetViewTabBrush(IsScopesViewActive);

	public IBrush CustomStatsViewBrush => GetViewTabBrush(IsCustomStatsViewActive);

	public IBrush LogViewBrush => GetViewTabBrush(IsLogViewActive);

	private static IBrush GetViewTabBrush(bool isActive)
	{
		string key = isActive ? "SukiPrimaryColor" : "TimelineScopeSubtleTextBrush";
		string fallback = isActive ? "#FF2169D6" : "#FF5D6977";
		if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var value) == true &&
			value is IBrush brush)
		{
			return brush;
		}

		return new SolidColorBrush(Color.Parse(fallback));
	}

	public bool IsLoading
	{
		get => m_IsLoading;
		private set => SetProperty(ref m_IsLoading, value);
	}

	private void SelectSessionView(object parameter)
	{
		string viewName = parameter as string;
		if (viewName == "Threads" ||
			viewName == "Cores" ||
			viewName == "Scopes" ||
			viewName == "Custom Stats" ||
			viewName == "Log")
		{
			ActiveSessionView = viewName;
		}
	}

	private void ToggleThreadsPanel(object parameter)
	{
		string panelName = parameter as string;
		if (string.Equals(panelName, "Info", StringComparison.Ordinal))
		{
			IsThreadsInfoPanelVisible = !IsThreadsInfoPanelVisible;
			StatusText = FormatThreadsPanelStatus("Info", IsThreadsInfoPanelVisible);
		}
		else if (string.Equals(panelName, "FrameGraph", StringComparison.Ordinal))
		{
			IsThreadsFrameGraphVisible = !IsThreadsFrameGraphVisible;
			StatusText = FormatThreadsPanelStatus("Frame Graph", IsThreadsFrameGraphVisible);
		}
		else if (string.Equals(panelName, "ScopeGraph", StringComparison.Ordinal))
		{
			IsThreadsScopeGraphVisible = !IsThreadsScopeGraphVisible;
			StatusText = FormatThreadsPanelStatus("Scope Graph", IsThreadsScopeGraphVisible);
		}
		else if (string.Equals(panelName, "CoreGraph", StringComparison.Ordinal))
		{
			IsThreadsCoreGraphVisible = !IsThreadsCoreGraphVisible;
			StatusText = FormatThreadsPanelStatus("Core Graph", IsThreadsCoreGraphVisible);
		}
		else if (string.Equals(panelName, "CustomStats", StringComparison.Ordinal))
		{
			IsThreadsCustomStatsVisible = !IsThreadsCustomStatsVisible;
			StatusText = FormatThreadsPanelStatus("Custom Stats", IsThreadsCustomStatsVisible);
		}
		else if (string.Equals(panelName, "DataGrid", StringComparison.Ordinal))
		{
			IsThreadsDataGridVisible = !IsThreadsDataGridVisible;
			StatusText = FormatThreadsPanelStatus("Data Grid", IsThreadsDataGridVisible);
		}
	}

	private static string FormatThreadsPanelStatus(string panelName, bool isVisible)
	{
		return panelName + (isVisible ? " panel visible." : " panel hidden.");
	}

	private void OpenFindScope()
	{
		ActiveSessionView = "Threads";
		IsThreadsDataGridVisible = true;
		StatusText = string.IsNullOrWhiteSpace(ScopeFilterText)
			? "Enter scope text in the Find Scope toolbar, then use Prev or Next."
			: "Find Scope ready for \"" + ScopeFilterText.Trim() + "\".";
	}

	private void ToggleOutputWindow()
	{
		IsOutputWindowVisible = !IsOutputWindowVisible;
		if (!IsOutputWindowVisible)
		{
			IsOutputWindowMinimized = false;
			IsOutputWindowFloating = false;
		}
		StatusText = IsOutputWindowVisible ? "Output Window visible." : "Output Window hidden.";
	}

	private void MinimizeOutputWindow()
	{
		IsOutputWindowVisible = true;
		IsOutputWindowFloating = false;
		IsOutputWindowMinimized = true;
		StatusText = "Output Window minimized.";
	}

	private void RestoreOutputWindow()
	{
		IsOutputWindowVisible = true;
		IsOutputWindowFloating = false;
		IsOutputWindowMinimized = false;
		StatusText = "Output Window restored.";
	}

	private void FloatOutputWindow()
	{
		IsOutputWindowVisible = true;
		IsOutputWindowMinimized = false;
		IsOutputWindowFloating = true;
		StatusText = "Output Window floating.";
	}

	private void DockOutputWindow()
	{
		IsOutputWindowVisible = true;
		IsOutputWindowFloating = false;
		IsOutputWindowMinimized = false;
		StatusText = "Output Window docked.";
	}

	private void AppendOutputLog(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}

		OutputLogText = string.IsNullOrEmpty(m_OutputLogText)
			? message
			: m_OutputLogText + Environment.NewLine + message;
	}

	private void ToggleToolbox()
	{
		IsToolboxVisible = !IsToolboxVisible;
		if (!IsToolboxVisible)
		{
			IsToolboxMinimized = false;
			IsToolboxFloating = false;
		}
		StatusText = IsToolboxVisible ? "Toolbox visible." : "Toolbox hidden.";
	}

	private void MinimizeToolbox()
	{
		IsToolboxVisible = true;
		IsToolboxFloating = false;
		IsToolboxMinimized = true;
		StatusText = "Toolbox minimized.";
	}

	private void RestoreToolbox()
	{
		IsToolboxVisible = true;
		IsToolboxFloating = false;
		IsToolboxMinimized = false;
		StatusText = "Toolbox restored.";
	}

	private void FloatToolbox()
	{
		IsToolboxVisible = true;
		IsToolboxMinimized = false;
		IsToolboxFloating = true;
		StatusText = "Toolbox floating.";
	}

	private void DockToolbox()
	{
		IsToolboxVisible = true;
		IsToolboxFloating = false;
		IsToolboxMinimized = false;
		StatusText = "Toolbox docked.";
	}

	private void ToggleMainSessionDock()
	{
		IsMainSessionDockVisible = !IsMainSessionDockVisible;
		if (!IsMainSessionDockVisible)
		{
			IsMainSessionDockMinimized = false;
			IsMainSessionDockFloating = false;
		}
		StatusText = IsMainSessionDockVisible ? "Main Session View visible." : "Main Session View hidden.";
	}

	private void MinimizeMainSessionDock()
	{
		IsMainSessionDockVisible = true;
		IsMainSessionDockFloating = false;
		IsMainSessionDockMinimized = true;
		StatusText = "Main Session View minimized.";
	}

	private void RestoreMainSessionDock()
	{
		IsMainSessionDockVisible = true;
		IsMainSessionDockFloating = false;
		IsMainSessionDockMinimized = false;
		StatusText = "Main Session View restored.";
	}

	private void FloatMainSessionDock()
	{
		IsMainSessionDockVisible = true;
		IsMainSessionDockMinimized = false;
		IsMainSessionDockFloating = true;
		StatusText = "Main Session View floating.";
	}

	private void DockMainSessionDock()
	{
		IsMainSessionDockVisible = true;
		IsMainSessionDockFloating = false;
		IsMainSessionDockMinimized = false;
		StatusText = "Main Session View docked.";
	}

	private void SetScopeColorMode(object parameter)
	{
		string mode = parameter as string;
		if (mode != "Thread" && mode != "Scope")
		{
			return;
		}

		ScopeColorMode = mode;
		StatusText = "Scope colouring by " + mode.ToLowerInvariant() + ".";
	}

	private void ToggleScopeColorMode()
	{
		SetScopeColorMode(IsScopeColorModeThread ? "Scope" : "Thread");
	}

	private void ExitApplication()
	{
		if (global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.Shutdown();
		}
	}

	private async Task HandleDemoActionAsync(object parameter)
	{
		string actionName = parameter as string;
		if (string.Equals(actionName, "LaunchGameSimulator", StringComparison.Ordinal))
		{
			LaunchGameSimulator();
		}
		else if (string.Equals(actionName, "PlaybackRecordingFile", StringComparison.Ordinal))
		{
			await PlaybackRecordingFileAsync();
		}
	}

	private void LaunchGameSimulator()
	{
		if (IsProcessRunning(m_GameSimulatorProcess))
		{
			StatusText = "Game Simulator is already running.";
			return;
		}

		if (!TryFindProfilerTool("Profiler_GameSimulator.exe", out string toolPath))
		{
			StatusText = "Profiler_GameSimulator.exe was not found.";
			return;
		}

		try
		{
			m_GameSimulatorProcess = StartProfilerTool(toolPath, null);
			StatusText = "Launched FramePro Game Simulator.";
		}
		catch (Exception ex)
		{
			StatusText = "Failed to launch game simulator. " + ex.Message;
		}
	}

	private async Task PlaybackRecordingFileAsync()
	{
		if (IsProcessRunning(m_RecordingPlayerProcess))
		{
			StatusText = "Recording Player is already running.";
			return;
		}

		if (!TryFindProfilerTool("Profiler_RecordingPlayer.exe", out string toolPath))
		{
			StatusText = "Profiler_RecordingPlayer.exe was not found.";
			return;
		}

		Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
			? desktop.MainWindow
			: null;
		if (window == null)
		{
			StatusText = "No application window is available for selecting a recording.";
			return;
		}

		IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			AllowMultiple = false,
			Title = "Playback profiler recording file",
			FileTypeFilter = new List<FilePickerFileType>
			{
				new FilePickerFileType("Profiler recording files")
				{
					Patterns = new[] { "*.profiler_recording" }
				},
				new FilePickerFileType("All files")
				{
					Patterns = new[] { "*" }
				}
			}
		});

		if (files.Count == 0)
		{
			return;
		}

		string path = files[0].TryGetLocalPath();
		if (string.IsNullOrWhiteSpace(path))
		{
			StatusText = "Selected recording file is not available as a local path.";
			return;
		}

		try
		{
			m_RecordingPlayerProcess = StartProfilerTool(toolPath, path);
			StatusText = "Launched Recording Player for " + Path.GetFileName(path) + ".";
			await HandleConnectionActionAsync("Connect");
		}
		catch (Exception ex)
		{
			StatusText = "Failed to launch recording player. " + ex.Message;
		}
	}

	private static bool IsProcessRunning(Process process)
	{
		try
		{
			return process != null && !process.HasExited;
		}
		catch
		{
			return false;
		}
	}

	private static Process StartProfilerTool(string toolPath, string argument)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = toolPath,
			UseShellExecute = true,
			WorkingDirectory = Path.GetDirectoryName(toolPath) ?? string.Empty,
			WindowStyle = ProcessWindowStyle.Minimized
		};
		if (!string.IsNullOrWhiteSpace(argument))
		{
			startInfo.ArgumentList.Add(argument);
		}

		Process process = new Process
		{
			StartInfo = startInfo
		};
		process.Start();
		return process;
	}

	private static bool TryFindProfilerTool(string fileName, out string path)
	{
		foreach (string directory in GetProfilerToolSearchDirectories())
		{
			if (string.IsNullOrWhiteSpace(directory))
			{
				continue;
			}

			string candidate = Path.Combine(directory, fileName);
			if (File.Exists(candidate))
			{
				path = candidate;
				return true;
			}
		}

		path = null;
		return false;
	}

	private static IEnumerable<string> GetProfilerToolSearchDirectories()
	{
		string baseDirectory = AppContext.BaseDirectory;
		yield return baseDirectory;
		yield return Directory.GetCurrentDirectory();
		yield return Path.Combine(baseDirectory, "ProfilerStudy");
		yield return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "ProfilerStudy"));
		yield return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ProfilerStudy"));
	}

	private void OpenSettingsPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsSettingsPanelVisible = true;
		StatusText = "Settings panel opened.";
	}

	private void CloseSettingsPanel()
	{
		IsSettingsPanelVisible = false;
		StatusText = "Settings saved.";
	}

	private void SetBaseTheme(string themeName)
	{
		m_AppThemeService.ChangeBaseTheme(themeName);
		StatusText = "Theme changed to " + m_AppThemeService.ActiveBaseThemeName + ".";
	}

	private void SetThemeColor(object parameter)
	{
		string themeName = parameter is AppThemeColorOption option ? option.Name : parameter as string;
		if (string.IsNullOrWhiteSpace(themeName))
		{
			return;
		}

		m_AppThemeService.ChangeColorTheme(themeName);
		StatusText = "Theme color changed to " + m_AppThemeService.ActiveColorThemeName + ".";
	}

	private void OnThemeServiceChanged(object sender, EventArgs e)
	{
		RaisePropertyChanged(nameof(IsLightThemeActive));
		RaisePropertyChanged(nameof(IsDarkThemeActive));
		RaisePropertyChanged(nameof(ActiveColorThemeName));
		ThemeRevision++;
	}

	private void OpenAndroidPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsAndroidPanelVisible = true;
		StatusText = "Android panel opened.";
	}

	private void CloseAndroidPanel()
	{
		IsAndroidPanelVisible = false;
		StatusText = "Android panel closed.";
	}

	private async Task HandleAndroidActionAsync(object parameter)
	{
		string actionName = parameter as string;
		if (string.Equals(actionName, "RecordContextSwitches", StringComparison.Ordinal))
		{
			await RecordAndroidContextSwitchesAsync();
		}
		else if (string.Equals(actionName, "ConnectAndroid", StringComparison.Ordinal))
		{
			await ConnectToAndroidAsync();
		}
		else if (string.Equals(actionName, "UseDebugEndpoint", StringComparison.Ordinal))
		{
			AndroidEndpoint = AdbSocketDiscovery.DebugFrameProEndpoint;
			AndroidPanelStatusText = "Android target set to debug endpoint.";
			StatusText = AndroidPanelStatusText;
		}
		else if (string.Equals(actionName, "UseReleaseEndpoint", StringComparison.Ordinal))
		{
			AndroidEndpoint = AdbSocketDiscovery.ReleaseFrameProEndpoint;
			AndroidPanelStatusText = "Android target set to release endpoint.";
			StatusText = AndroidPanelStatusText;
		}
		else if (string.Equals(actionName, "LoadContextSwitchFile", StringComparison.Ordinal))
		{
			await LoadAndroidContextSwitchFileAsync();
		}
		else
		{
			AndroidPanelStatusText = string.IsNullOrWhiteSpace(actionName)
				? "No Android action was selected."
				: "Unknown Android action: " + actionName + ".";
			StatusText = AndroidPanelStatusText;
		}
	}

	private async Task RecordAndroidContextSwitchesAsync()
	{
		if (!int.TryParse((AndroidRecordingDuration ?? string.Empty).Trim(), out int duration) || duration <= 0 || duration > 600)
		{
			AndroidPanelStatusText = "Android context switch recording duration must be between 1 and 600 seconds.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
			? desktop.MainWindow
			: null;
		if (window == null)
		{
			AndroidPanelStatusText = "No application window is available for saving a context switch recording.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		string adb = AdbSocketDiscovery.ResolveAdbExecutable();
		AndroidPanelStatusText = "Starting Android context switch recording for " + duration + " seconds...";
		StatusText = AndroidPanelStatusText;
		ProcessRunResult tracingOn = await RunProcessAsync(adb, new[] { "shell", "echo 1 > /d/tracing/tracing_on" }, 5000);
		if (!tracingOn.Success)
		{
			AndroidPanelStatusText = "Error starting context switch recording: " + tracingOn.Output;
			StatusText = AndroidPanelStatusText;
			return;
		}

		ProcessRunResult marker = await RunProcessAsync(adb, new[] { "shell", "echo echo userlandclock\\(\\) > /d/tracing/trace_marker" }, 5000);
		if (!marker.Success)
		{
			await RunProcessAsync(adb, new[] { "shell", "echo 0 > /d/tracing/tracing_on" }, 5000);
			AndroidPanelStatusText = "Error writing context switch trace marker: " + marker.Output;
			StatusText = AndroidPanelStatusText;
			return;
		}

		ProcessRunResult recording = await RunProcessAsync(adb, new[] { "shell", "atrace", "-t", duration.ToString(), "sched" }, (duration * 1000) + 10000);
		await RunProcessAsync(adb, new[] { "shell", "echo 0 > /d/tracing/tracing_on" }, 5000);
		if (!recording.Success)
		{
			AndroidPanelStatusText = "Error recording Android context switches: " + recording.Output;
			StatusText = AndroidPanelStatusText;
			return;
		}

		IStorageFile file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Save Android context switch recording",
			SuggestedFileName = "android-context-switch.profiler_context_switch",
			FileTypeChoices = new List<FilePickerFileType>
			{
				new FilePickerFileType("Android context switch files")
				{
					Patterns = new[] { "*.profiler_context_switch" }
				}
			}
		});

		if (file == null)
		{
			AndroidPanelStatusText = "Android context switch recording completed but was not saved.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		string path = file.TryGetLocalPath();
		if (string.IsNullOrWhiteSpace(path))
		{
			AndroidPanelStatusText = "Selected context switch output file is not available as a local path.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		await File.WriteAllTextAsync(path, recording.Output);
		AndroidPanelStatusText = "Saved Android context switch recording: " + Path.GetFileName(path);
		StatusText = AndroidPanelStatusText;
	}

	private static async Task<ProcessRunResult> RunProcessAsync(string executable, string[] arguments, int timeoutMs)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = executable,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			foreach (string argument in arguments)
			{
				startInfo.ArgumentList.Add(argument);
			}

			using (Process process = Process.Start(startInfo))
			{
				if (process == null)
				{
					return new ProcessRunResult(false, "Failed to start process: " + executable);
				}

				Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
				Task<string> errorTask = process.StandardError.ReadToEndAsync();
				Task waitTask = process.WaitForExitAsync();
				Task completedTask = await Task.WhenAny(waitTask, Task.Delay(timeoutMs));
				if (completedTask != waitTask)
				{
					process.Kill(entireProcessTree: true);
					return new ProcessRunResult(false, executable + " timed out.");
				}

				string output = await outputTask;
				string error = await errorTask;
				string result = (output + error).Trim();
				return new ProcessRunResult(process.ExitCode == 0, string.IsNullOrWhiteSpace(result) ? "<empty>" : result);
			}
		}
		catch (Exception ex)
		{
			return new ProcessRunResult(false, ex.Message);
		}
	}

	private async Task ConnectToAndroidAsync()
	{
		if (m_IsConnecting)
		{
			AndroidPanelStatusText = "Connection attempt is already in progress.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		string endpoint = (AndroidEndpoint ?? string.Empty).Trim();
		if (!AdbSocketDiscovery.IsAdbSocketEndpoint(endpoint))
		{
			AndroidPanelStatusText = "Invalid Android FramePro endpoint: " + endpoint;
			StatusText = AndroidPanelStatusText;
			return;
		}

		DisconnectFromFramePro(updateStatus: false);
		m_IsConnecting = true;
		AndroidPanelStatusText = "Connecting to Android endpoint...";
		ConnectionPanelStatusText = AndroidPanelStatusText;
		StatusText = AndroidPanelStatusText;
		Session session = new Session(new CoreSettings(), new NullLog());
		string connectionName = "Android";
		session.SessionIsReady += () => Dispatcher.UIThread.Post(() => ApplyLiveConnectionDocument(session, connectionName));
		session.Disconnected += () => Dispatcher.UIThread.Post(() => OnLiveConnectionDisconnected(session));
		bool connected = false;
		try
		{
			connected = await Task.Run(() => session.ConnectToAndroid(endpoint));
		}
		catch (Exception ex)
		{
			AndroidPanelStatusText = "Android connection failed: " + ex.Message;
			ConnectionPanelStatusText = AndroidPanelStatusText;
			StatusText = AndroidPanelStatusText;
		}
		finally
		{
			m_IsConnecting = false;
		}

		if (connected)
		{
			m_LiveConnectionSession = session;
			m_LiveConnectionName = connectionName;
			AndroidPanelStatusText = "Connected to Android. Waiting for profiler packets.";
			ConnectionPanelStatusText = AndroidPanelStatusText;
			SessionStatusText = "Session: live Android connection";
			StatusText = AndroidPanelStatusText;
			return;
		}

		if (string.IsNullOrWhiteSpace(AndroidPanelStatusText) || AndroidPanelStatusText.StartsWith("Connecting", StringComparison.Ordinal))
		{
			AndroidPanelStatusText = string.IsNullOrWhiteSpace(session.LastConnectionError)
				? "Android connection failed: " + endpoint
				: session.LastConnectionError;
			ConnectionPanelStatusText = AndroidPanelStatusText;
			StatusText = AndroidPanelStatusText;
		}

		session.Disconnect(DisconnectReason.Requested);
	}

	private async Task LoadAndroidContextSwitchFileAsync()
	{
		if (CurrentDocument?.Session == null)
		{
			AndroidPanelStatusText = "Open a profiler session before loading an Android context switch file.";
			StatusText = AndroidPanelStatusText;
			return;
		}

		try
		{
			Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
				? desktop.MainWindow
				: null;
			if (window == null)
			{
				AndroidPanelStatusText = "No application window is available for loading a context switch file.";
				StatusText = AndroidPanelStatusText;
				return;
			}

			IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				AllowMultiple = false,
				Title = "Load Android context switch file",
				FileTypeFilter = new List<FilePickerFileType>
				{
					new FilePickerFileType("Android context switch files")
					{
						Patterns = new[] { "*.profiler_context_switch", "*.txt", "*" }
					}
				}
			});

			if (files.Count == 0)
			{
				return;
			}

			string path = files[0].TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(path))
			{
				AndroidPanelStatusText = "Selected context switch file is not available as a local path.";
				StatusText = AndroidPanelStatusText;
				return;
			}

			AndroidPanelStatusText = "Loading Android context switch file...";
			StatusText = AndroidPanelStatusText;
			Session session = CurrentDocument.Session;
			bool loaded = await Task.Run(() => session.LoadContextSwitchFile(path));
			if (!loaded)
			{
				AndroidPanelStatusText = "Failed to load Android context switch file: " + path;
				StatusText = AndroidPanelStatusText;
				return;
			}

			ApplyDocument(CurrentDocument);
			AndroidPanelStatusText = "Loaded Android context switch file: " + Path.GetFileName(path);
			StatusText = AndroidPanelStatusText;
		}
		catch (Exception ex)
		{
			AndroidPanelStatusText = "Failed to load Android context switch file: " + ex.Message;
			StatusText = AndroidPanelStatusText;
		}
	}

	private void OpenConnectionPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsConnectionPanelVisible = true;
		StatusText = "Connection panel opened.";
	}

	private void CloseConnectionPanel()
	{
		IsConnectionPanelVisible = false;
		StatusText = "Connection panel closed.";
	}

	private async Task HandleConnectionActionAsync(object parameter)
	{
		string actionName = parameter as string;
		if (string.Equals(actionName, "Connect", StringComparison.Ordinal))
		{
			await ConnectToFrameProAsync();
		}
		else if (string.Equals(actionName, "Disconnect", StringComparison.Ordinal))
		{
			DisconnectFromFramePro();
		}
		else
		{
			OpenConnectionPanel();
			ConnectionPanelStatusText = "Edit the connection profile, then press Connect.";
			StatusText = ConnectionPanelStatusText;
		}
	}

	private async Task ConnectToFrameProAsync()
	{
		if (m_IsConnecting)
		{
			ConnectionPanelStatusText = "Connection attempt is already in progress.";
			StatusText = ConnectionPanelStatusText;
			return;
		}

		string host = (ConnectionHost ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(host))
		{
			ConnectionPanelStatusText = "Connection host is required.";
			StatusText = ConnectionPanelStatusText;
			return;
		}

		if (!int.TryParse((ConnectionPort ?? string.Empty).Trim(), out int port) || port <= 0 || port > 65535)
		{
			ConnectionPanelStatusText = "Connection port must be between 1 and 65535.";
			StatusText = ConnectionPanelStatusText;
			return;
		}

		DisconnectFromFramePro(updateStatus: false);
		m_IsConnecting = true;
		ConnectionPanelStatusText = "Connecting to " + host + ":" + port + "...";
		StatusText = ConnectionPanelStatusText;
		Session session = new Session(new CoreSettings(), new NullLog());
		string connectionName = host + ":" + port;
		session.SessionIsReady += () => Dispatcher.UIThread.Post(() => ApplyLiveConnectionDocument(session, connectionName));
		session.Disconnected += () => Dispatcher.UIThread.Post(() => OnLiveConnectionDisconnected(session));
		bool connected = false;
		try
		{
			connected = await Task.Run(() => session.ConnectToTcp(host, port, connectionName, interactive: true, recordContextSwitches: false));
		}
		catch (Exception ex)
		{
			ConnectionPanelStatusText = "Connection failed: " + ex.Message;
			StatusText = ConnectionPanelStatusText;
		}
		finally
		{
			m_IsConnecting = false;
		}

		if (connected)
		{
			m_LiveConnectionSession = session;
			m_LiveConnectionName = connectionName;
			ConnectionPanelStatusText = "Connected to " + host + ":" + port + ". Waiting for profiler packets.";
			SessionStatusText = "Session: live connection";
			StatusText = ConnectionPanelStatusText;
			return;
		}

		if (string.IsNullOrWhiteSpace(ConnectionPanelStatusText) || ConnectionPanelStatusText.StartsWith("Connecting", StringComparison.Ordinal))
		{
			ConnectionPanelStatusText = string.IsNullOrWhiteSpace(session.LastConnectionError)
				? "Connection failed: " + host + ":" + port
				: session.LastConnectionError;
			StatusText = ConnectionPanelStatusText;
		}

		session.Disconnect(DisconnectReason.Requested);
	}

	private void ApplyLiveConnectionDocument(Session session, string connectionName)
	{
		if (session == null || session != m_LiveConnectionSession)
		{
			return;
		}

		ApplyLiveConnectionDocument(session, connectionName, updateStatus: true);
		StartLiveConnectionRefreshTimer();
	}

	private void ApplyLiveConnectionDocument(Session session, string connectionName, bool updateStatus)
	{
		int selectedFrameIndex = Selection.SelectedFrameIndex;
		int hoveredFrameIndex = Selection.HoveredFrameIndex;
		double selectedFrameTimeMs = Selection.SelectedFrameTimeMs;
		double hoveredFrameTimeMs = Selection.HoveredFrameTimeMs;
		int startFrame = Viewport.StartFrame;
		int endFrame = Viewport.EndFrame;
		SessionDocument document = BuildLiveConnectionDocument(session, connectionName);
		document.Selection.SelectedFrameIndex = Math.Min(selectedFrameIndex, Math.Max(-1, document.Summary.FrameCount - 1));
		document.Selection.HoveredFrameIndex = Math.Min(hoveredFrameIndex, Math.Max(-1, document.Summary.FrameCount - 1));
		document.Selection.SelectedFrameTimeMs = selectedFrameTimeMs;
		document.Selection.HoveredFrameTimeMs = hoveredFrameTimeMs;
		document.Viewport.SetRange(startFrame, Math.Max(startFrame, Math.Min(endFrame, Math.Max(0, document.Summary.FrameCount - 1))));
		ApplyDocument(document);
		m_LastLiveFrameCount = session.FrameCount;
		if (!updateStatus)
		{
			ConnectionPanelStatusText = "Live session frames: " + m_LastLiveFrameCount + ".";
			return;
		}

		ConnectionPanelStatusText = "Live session ready: " + connectionName + ".";
		StatusText = ConnectionPanelStatusText;
	}

	private static SessionDocument BuildLiveConnectionDocument(Session session, string connectionName)
	{
		SessionQueryService queryService = new SessionQueryService(session, 33.333333333333336);
		FrameSample[] samples = queryService.GetFrameSamples(0, Math.Max(0, session.FrameCount - 1), 0);
		SessionSummary summary = queryService.CreateSummary("Live: " + connectionName, samples);
		summary.SourceName = "Live: " + connectionName;
		return new SessionDocument("Live: " + connectionName, session, summary, samples);
	}

	private void StartLiveConnectionRefreshTimer()
	{
		if (m_LiveConnectionRefreshTimer == null)
		{
			m_LiveConnectionRefreshTimer = new DispatcherTimer
			{
				Interval = System.TimeSpan.FromMilliseconds(750)
			};
			m_LiveConnectionRefreshTimer.Tick += LiveConnectionRefreshTimerTick;
		}

		m_LiveConnectionRefreshTimer.Start();
	}

	private void StopLiveConnectionRefreshTimer()
	{
		m_LiveConnectionRefreshTimer?.Stop();
	}

	private void LiveConnectionRefreshTimerTick(object sender, EventArgs e)
	{
		if (m_LiveConnectionSession == null || string.IsNullOrWhiteSpace(m_LiveConnectionName))
		{
			StopLiveConnectionRefreshTimer();
			return;
		}

		int frameCount = m_LiveConnectionSession.FrameCount;
		if (frameCount == m_LastLiveFrameCount)
		{
			return;
		}

		ApplyLiveConnectionDocument(m_LiveConnectionSession, m_LiveConnectionName, updateStatus: false);
	}

	private void OnLiveConnectionDisconnected(Session session)
	{
		if (session != m_LiveConnectionSession)
		{
			return;
		}

		ConnectionPanelStatusText = "FramePro transport disconnected: " + session.DisconnectReason + ".";
		StopLiveConnectionRefreshTimer();
		StatusText = ConnectionPanelStatusText;
	}

	private void DisconnectFromFramePro(bool updateStatus = true)
	{
		if (m_LiveConnectionSession != null)
		{
			m_LiveConnectionSession.Disconnect(DisconnectReason.Requested);
			m_LiveConnectionSession = null;
		}
		m_LiveConnectionName = null;
		m_LastLiveFrameCount = -1;
		StopLiveConnectionRefreshTimer();

		if (updateStatus)
		{
			ConnectionPanelStatusText = "Disconnected from FramePro transport.";
			UpdateShellStatusSegments();
			StatusText = ConnectionPanelStatusText;
		}
	}

	private void OpenCallstacksPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsCallstacksPanelVisible = true;
		StatusText = "Callstacks panel opened.";
	}

	private void CloseCallstacksPanel()
	{
		IsCallstacksPanelVisible = false;
		StatusText = "Callstacks panel closed.";
	}

	private void OpenHelpPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsAboutPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsHelpPanelVisible = true;
		StatusText = "Help panel opened.";
	}

	private void CloseHelpPanel()
	{
		IsHelpPanelVisible = false;
		StatusText = "Help panel closed.";
	}

	private void OpenRegistrationPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsRegistrationPanelVisible = true;
		StatusText = "Registration panel opened.";
	}

	private void CloseRegistrationPanel()
	{
		IsRegistrationPanelVisible = false;
		StatusText = "Registration panel closed.";
	}

	private void OpenUpdateCheckPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsAboutPanelVisible = false;
		IsUpdateCheckPanelVisible = true;
		StatusText = "Check for Updates panel opened.";
	}

	private void CloseUpdateCheckPanel()
	{
		IsUpdateCheckPanelVisible = false;
		StatusText = "Check for Updates panel closed.";
	}

	private void OpenAboutPanel()
	{
		IsThreadSettingsPanelVisible = false;
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = true;
		StatusText = "About panel opened.";
	}

	private void CloseAboutPanel()
	{
		IsAboutPanelVisible = false;
		StatusText = "About panel closed.";
	}

	private void ShowCallstacksActionStatus(object parameter)
	{
		string actionName = parameter as string;
		if (string.Equals(actionName, "Capture", StringComparison.Ordinal))
		{
			ToggleCallstackRecording();
		}
		else if (string.Equals(actionName, "Resolve", StringComparison.Ordinal))
		{
			ReloadCallstackSymbols();
		}
		else
		{
			CallstacksPanelStatusText = "No callstack capture loaded.";
			StatusText = CallstacksPanelStatusText;
		}
	}

	private void ToggleCallstackRecording()
	{
		Session session = CurrentDocument?.Session ?? m_LiveConnectionSession;
		if (session == null)
		{
			CallstacksPanelStatusText = "Open or connect a profiler session before changing callstack recording.";
			StatusText = CallstacksPanelStatusText;
			return;
		}

		if (!session.Connected)
		{
			CallstacksPanelStatusText = "Callstack recording can only be changed while a live session is connected.";
			StatusText = CallstacksPanelStatusText;
			return;
		}

		bool enabled = !session.RecordCallstacks;
		session.RecordCallstacks = enabled;
		CallstacksPanelStatusText = enabled
			? "Callstack recording enabled for the live session."
			: "Callstack recording disabled for the live session.";
		StatusText = CallstacksPanelStatusText;
	}

	private void ReloadCallstackSymbols()
	{
		Session session = CurrentDocument?.Session ?? m_LiveConnectionSession;
		if (session == null)
		{
			CallstacksPanelStatusText = "Open or connect a profiler session before reloading callstack symbols.";
			StatusText = CallstacksPanelStatusText;
			return;
		}

		session.ReloadSymbols();
		session.LoadModuleSymbols();
		CallstacksPanelStatusText = "Requested module symbol reload for the current session.";
		StatusText = CallstacksPanelStatusText;
	}

	private void AdjustConditionalScopeTime(object parameter)
	{
		string direction = parameter as string;
		double delta = string.Equals(direction, "Down", StringComparison.Ordinal) ? -0.5 : 0.5;
		m_ConditionalScopeTimeMs = Math.Max(0, Math.Min(20, m_ConditionalScopeTimeMs + delta));
		UpdateConditionalScopeTimeStatus();
		StatusText = m_ConditionalScopeTimeMs <= 0
			? "Conditional Scope Time disabled."
			: $"Conditional Scope Time threshold set to {m_ConditionalScopeTimeMs:0.0} ms.";
	}

	private void UpdateConditionalScopeTimeStatus()
	{
		ConditionalScopeTimeText = m_ConditionalScopeTimeMs <= 0
			? "All scope times"
			: $"Only scopes >= {m_ConditionalScopeTimeMs:0.0} ms";
		ConditionalScopeTimeWidth = Math.Round(8 + (m_ConditionalScopeTimeMs / 20.0 * 120.0), 1);
	}

	private void UpdateShellStatusSegments()
	{
		if (CurrentDocument == null)
		{
			SessionStatusText = "Session: none";
			SelectedFrameStatusText = "Frame: none";
			StatusBarSessionText = "none";
			StatusBarSelectedFrameText = "none";
			StatusBarHoveredFrameText = "none";
			FooterText = "No session";
			return;
		}

		SessionStatusText = $"Session: {CurrentDocument.Summary.FrameCount} frames";
		StatusBarSessionText = $"{CurrentDocument.Summary.FrameCount} frames";
		FooterText = $"{CurrentDocument.Summary.FrameCount} frames";
		SelectedFrameStatusText = Selection.SelectedFrameIndex >= 0
			? $"Frame: {Selection.SelectedFrameIndex} ({Selection.SelectedFrameTimeMs:0.###} ms)"
			: "Frame: none";
		StatusBarSelectedFrameText = Selection.SelectedFrameIndex >= 0
			? $"{Selection.SelectedFrameIndex} ({Selection.SelectedFrameTimeMs:0.###} ms)"
			: "none";
		StatusBarHoveredFrameText = Selection.HoveredFrameIndex >= 0
			? $"{Selection.HoveredFrameIndex} ({Selection.HoveredFrameTimeMs:0.###} ms)"
			: "none";
	}

	public void SelectFrameFromProfilerStats(int frameIndex)
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		FrameSample frame = default;
		bool hasFrame = false;
		foreach (FrameSample sample in CurrentDocument.FrameSamples)
		{
			if (sample.Index == frameIndex)
			{
				frame = sample;
				hasFrame = true;
				break;
			}
		}

		if (hasFrame is false)
		{
			StatusText = $"Frame {frameIndex} is not available in the current session.";
			return;
		}

		if (Viewport.Contains(frame.Index) is false)
		{
			int visibleCount = Math.Min(CurrentDocument.Summary.FrameCount, Math.Max(1, Viewport.VisibleFrameCount));
			int startFrame = Math.Max(0, frame.Index - (visibleCount / 2));
			int endFrame = Math.Min(CurrentDocument.Summary.FrameCount - 1, startFrame + visibleCount - 1);
			startFrame = Math.Max(0, endFrame - visibleCount + 1);
			Viewport.SetRange(startFrame, endFrame);
		}

		Selection.SelectedFrameIndex = frame.Index;
		Selection.SelectedFrameTimeMs = frame.DurationMs;
		UpdateShellStatusSegments();
		StatusText = $"Selected frame {frame.Index} from profiler scope timeline ({frame.DurationMs:0.###} ms).";
	}

	public void HoverFrameFromProfilerStats(int frameIndex)
	{
		if (frameIndex < 0)
		{
			Selection.HoveredFrameIndex = -1;
			Selection.HoveredFrameTimeMs = 0.0;
			return;
		}

		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			return;
		}

		foreach (FrameSample sample in CurrentDocument.FrameSamples)
		{
			if (sample.Index == frameIndex)
			{
				Selection.HoveredFrameIndex = sample.Index;
				Selection.HoveredFrameTimeMs = sample.DurationMs;
				StatusText = $"Hover frame {sample.Index} from profiler scope timeline ({sample.DurationMs:0.###} ms).";
				return;
			}
		}
	}

	private async Task OpenSessionAsync()
	{
		try
		{
			Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
				? desktop.MainWindow
				: null;
			if (window == null)
			{
				return;
			}

			IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				AllowMultiple = false,
				Title = "Open profiler session",
				FileTypeFilter = new List<FilePickerFileType>
				{
					new FilePickerFileType("Profiler files")
					{
						Patterns = new[] { "*.profiler", "*.profiler_recording", "*.profiler_dump", "*" }
					}
				}
			});

			if (files.Count == 0)
			{
				return;
			}

			string path = files[0].TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(path))
			{
				StatusText = "Selected file is not available as a local path.";
				return;
			}

			await LoadFileDocumentAsync(path);
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private async Task OpenRecentSessionAsync(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			StatusText = "No recent file selected.";
			return;
		}

		if (!File.Exists(path))
		{
			StatusText = "Recent file not found: " + path;
			RemoveRecentFile(path);
			return;
		}

		await LoadFileDocumentAsync(path);
	}

	private async Task ExportFramesToCsvAsync()
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		try
		{
			Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
				? desktop.MainWindow
				: null;
			if (window == null)
			{
				StatusText = "No application window is available for export.";
				return;
			}

			string sourceName = string.IsNullOrWhiteSpace(CurrentDocument.Summary.SourceName) ? "frames" : Path.GetFileNameWithoutExtension(CurrentDocument.Summary.SourceName);
			IStorageFile file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = "Export frame graph to CSV",
				SuggestedFileName = sourceName + "-frames.csv",
				FileTypeChoices = new List<FilePickerFileType>
				{
					new FilePickerFileType("CSV files")
					{
						Patterns = new[] { "*.csv" },
						MimeTypes = new[] { "text/csv" }
					}
				}
			});

			if (file == null)
			{
				return;
			}

			string path = file.TryGetLocalPath();
			if (string.IsNullOrWhiteSpace(path))
			{
				StatusText = "Selected export file is not available as a local path.";
				return;
			}

			await File.WriteAllTextAsync(path, BuildFrameCsv(CurrentDocument.FrameSamples));
			StatusText = "Exported frame CSV to " + path;
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private async Task CreateSessionFromSelectionAsync()
	{
		SessionDocument sourceDocument = CurrentDocument;
		Session sourceSession = sourceDocument?.Session;
		if (sourceSession == null)
		{
			StatusText = "No profiler session is available to clone.";
			return;
		}

		if (sourceSession.FrameCount == 0 || sourceDocument.Viewport == null)
		{
			StatusText = "No frames are available for creating a session.";
			return;
		}

		int startFrame = Math.Max(0, Math.Min(sourceDocument.Viewport.StartFrame, sourceSession.FrameCount - 1));
		int endFrame = Math.Max(startFrame, Math.Min(sourceDocument.Viewport.EndFrame, sourceSession.FrameCount - 1));
		if (endFrame < startFrame)
		{
			(startFrame, endFrame) = (endFrame, startFrame);
		}

		sourceSession.GetFrameStartEndTime(startFrame, out long startTime, out _);
		sourceSession.GetFrameStartEndTime(endFrame, out _, out long endTime);
		if (endTime <= startTime)
		{
			StatusText = "Selected frame range is empty.";
			return;
		}

		try
		{
			StatusText = "Cloning session from visible frame range...";
			Session clonedSession = new Session(new CoreSettings(), new NullLog());
			ThreadJobContext context = new ThreadJobContext();
			string error = string.Empty;
			bool cloned = await Task.Run(() => sourceSession.CopyTo(clonedSession, startTime, endTime, context, ref error));
			if (!cloned)
			{
				StatusText = "Error cloning session: " + error;
				return;
			}

			SessionDocument clonedDocument = CreateSessionDocumentFromSession(
				clonedSession,
				"Selection: " + sourceDocument.Summary.SourceName,
				"Selection " + startFrame + "-" + endFrame);
			ApplyDocument(clonedDocument);
			StatusText = "Created session from visible frames " + startFrame + " - " + endFrame + ".";
		}
		catch (Exception ex)
		{
			StatusText = "Error cloning session: " + ex.Message;
		}
	}

	private static SessionDocument CreateSessionDocumentFromSession(Session session, string sourcePath, string sourceName)
	{
		SessionQueryService queryService = new SessionQueryService(session, 33.333);
		FrameSample[] samples = queryService.GetFrameSamples(0, Math.Max(0, session.FrameCount - 1), 0);
		SessionSummary summary = queryService.CreateSummary(sourcePath, samples);
		summary.SourceName = sourceName;
		return new SessionDocument(sourcePath, session, summary, samples);
	}

	private async Task SaveSessionAsync(bool saveAs)
	{
		SessionDocument document = CurrentDocument;
		Session session = document?.Session;
		if (session == null)
		{
			StatusText = "No profiler session is available to save.";
			return;
		}

		if (session.Connected)
		{
			StatusText = "Please disconnect before saving.";
			return;
		}

		try
		{
			if (saveAs || NeedsSaveFilename(session.SessionFilename))
			{
				string path = await AskUserForSaveFilenameAsync(session.SessionFilename);
				if (string.IsNullOrWhiteSpace(path))
				{
					return;
				}

				session.SessionFilename = Path.GetFullPath(path);
			}

			if (session.ProcessingPackets)
			{
				StatusText = "Still processing received packets. Please wait...";
				await Task.Run(() =>
				{
					while (session.ProcessingPackets)
					{
						session.WaitforProcessingToFinish(100);
					}
				});
			}

			StatusText = "Writing " + session.SessionFilename + "...";
			SessionViewSaveData saveData = CreateSessionViewSaveData(document);
			ThreadJobContext context = new ThreadJobContext();
			string error = string.Empty;
			bool saved = await Task.Run(() => session.Write(saveData, context, ref error));
			if (saved)
			{
				AddRecentFile(session.SessionFilename);
				StatusText = "Saved " + session.SessionFilename;
			}
			else
			{
				StatusText = "Error writing file " + session.SessionFilename + ": " + error;
			}
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private async Task<string> AskUserForSaveFilenameAsync(string sessionFilename)
	{
		Window window = global::Avalonia.Application.Current?.ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
			? desktop.MainWindow
			: null;
		if (window == null)
		{
			StatusText = "No application window is available for saving.";
			return null;
		}

		string suggestedFileName = sessionFilename ?? "session.profiler";
		if (suggestedFileName.ToLowerInvariant().Trim().EndsWith(".profiler_recording", StringComparison.Ordinal))
		{
			suggestedFileName = suggestedFileName.Substring(0, suggestedFileName.Length - ".profiler_recording".Length);
		}

		IStorageFile file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Save profiler session",
			SuggestedFileName = suggestedFileName,
			FileTypeChoices = new List<FilePickerFileType>
			{
				new FilePickerFileType("Profiler files")
				{
					Patterns = new[] { "*.profiler" }
				},
				new FilePickerFileType("All files")
				{
					Patterns = new[] { "*" }
				}
			}
		});

		if (file == null)
		{
			return null;
		}

		string path = file.TryGetLocalPath();
		if (string.IsNullOrWhiteSpace(path))
		{
			StatusText = "Selected save file is not available as a local path.";
			return null;
		}

		return path;
	}

	private static bool NeedsSaveFilename(string sessionFilename)
	{
		return string.IsNullOrWhiteSpace(sessionFilename)
			|| !Path.IsPathRooted(sessionFilename)
			|| sessionFilename.ToLowerInvariant().Trim().EndsWith("profiler_recording", StringComparison.Ordinal);
	}

	private static SessionViewSaveData CreateSessionViewSaveData(SessionDocument document)
	{
		SessionViewSaveData saveData = new SessionViewSaveData();
		if (document?.Viewport == null)
		{
			return saveData;
		}

		saveData.m_FrameGraphVisibleRangeStart = document.Viewport.StartFrame;
		saveData.m_FrameGraphVisibleRangeEnd = document.Viewport.EndFrame;
		saveData.m_FrameGraphStart = document.Viewport.StartFrame;
		saveData.m_FrameGraphViewScale = Math.Max(1, document.Viewport.VisibleFrameCount);
		return saveData;
	}

	private async Task OpenStartupProfilerAsync(string path)
	{
		if (!File.Exists(path))
		{
			StatusText = "Startup profiler file not found: " + path;
			return;
		}

		await LoadFileDocumentAsync(path);
	}

	private static string BuildFrameCsv(IEnumerable<FrameSample> samples)
	{
		var writer = new StringWriter();
		writer.WriteLine("Frame,DurationMs");
		foreach (FrameSample sample in samples ?? Array.Empty<FrameSample>())
		{
			writer.Write(sample.Index);
			writer.Write(',');
			writer.WriteLine(sample.DurationMs.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
		}

		return writer.ToString();
	}

	private Task LoadFileDocumentAsync(string path)
	{
		return LoadDocumentAsync(() => m_SessionLoader.LoadFileAsync(path, m_LoadCancellation.Token), "Loading " + path, path);
	}

	private Task LoadSampleAsync()
	{
		return LoadDocumentAsync(() => m_SessionLoader.LoadSampleAsync(m_LoadCancellation.Token), "Loading generated sample data");
	}

	private async Task LoadDocumentAsync(Func<Task<SessionDocument>> load, string loadingStatus, string recentPath = null)
	{
		m_LoadCancellation?.Cancel();
		m_LoadCancellation = new CancellationTokenSource();
		try
		{
			IsLoading = true;
			StatusText = loadingStatus + "...";
			SessionDocument document = await load();
			ApplyDocument(document);
			if (!string.IsNullOrWhiteSpace(recentPath))
			{
				AddRecentFile(recentPath);
			}
			StatusText = "Loaded " + document.SourcePath;
		}
		catch (OperationCanceledException)
		{
			StatusText = "Load canceled.";
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void ApplyRecentFiles(IEnumerable<string> recentFiles)
	{
		RecentFiles = (recentFiles ?? Array.Empty<string>())
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(10)
			.ToArray();
		SelectedRecentFile = RecentFiles.FirstOrDefault();
	}

	private void AddRecentFile(string path)
	{
		string fullPath = Path.GetFullPath(path);
		List<string> files = new List<string> { fullPath };
		files.AddRange(RecentFiles.Where(item => !string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase)));
		ApplyRecentFiles(files);
		SaveRecentFiles();
	}

	private void RemoveRecentFile(string path)
	{
		ApplyRecentFiles(RecentFiles.Where(item => !string.Equals(item, path, StringComparison.OrdinalIgnoreCase)));
		SaveRecentFiles();
	}

	private void SaveRecentFiles()
	{
		m_AppSettings.RecentFiles = RecentFiles.ToList();
		SaveAppSettings(m_AppSettings);
	}

	private void SaveSourcePathSettings()
	{
		m_AppSettings.CapturedSourceRoot = CapturedSourceRoot ?? string.Empty;
		m_AppSettings.LocalSourceRoot = LocalSourceRoot ?? string.Empty;
		SaveAppSettings(m_AppSettings);
	}

	private void SaveAppSettings(AppSettings settings)
	{
		m_AppSettingsService.Save(settings);
	}

	private void ApplyDocument(SessionDocument document)
	{
		DetachTimelineState(Selection, Viewport);
		CurrentDocument = document;
		FrameSamples = document.FrameSamples;
		Summary.Apply(document.Summary);
		Selection = document.Selection;
		Viewport = document.Viewport;
		ApplyScopeHotspots(document);
		ApplyVisibleThreadTimeline();
		ApplySelectedFrameScopes();
		ApplySelectedFrameCounters();
		ApplyLogMessages(document);
		ApplyCoreSummary();
		AttachTimelineState(Selection, Viewport);
		UpdateTimelineText();
		UpdateFooterText();
	}

	private void CloseSession()
	{
		if (CurrentDocument == null && FrameSamples.Count == 0)
		{
			StatusText = "No session loaded.";
			return;
		}

		m_LoadCancellation?.Cancel();
		DetachTimelineState(Selection, Viewport);
		CurrentDocument = null;
		FrameSamples = Array.Empty<FrameSample>();
		Summary.Reset();
		Selection = new TimelineSelection();
		Viewport = TimelineViewport.CreateForFrames(0);
		m_AllScopeHotspots = Array.Empty<ScopeHotspotRow>();
		ScopeHotspots = Array.Empty<ScopeHotspotRow>();
		VisibleThreadScopes = Array.Empty<ThreadTimelineScopeRow>();
		SelectedFrameScopes = Array.Empty<ScopeFrameDetailRow>();
		SelectedFrameCounters = Array.Empty<SelectedFrameCounterRow>();
		LogRows = Array.Empty<LogMessageRow>();
		CoreRows = Array.Empty<CoreSummaryRow>();
		ThreadTimelineSummaryText = "No profiler thread scope data loaded.";
		ScopeHotspotSummaryText = "No profiler scope data loaded.";
		SelectedFrameScopeSummaryText = "Select a frame to inspect scopes.";
		SelectedFrameCounterSummaryText = "Select a frame to inspect counters.";
		LogSummaryText = "No profiler log messages loaded.";
		CoreSummaryText = "No profiler core data loaded.";
		ThreadTimelineFirstVisibleThreadIndex = 0;
		ThreadTimelineTotalThreadCount = 0;
		FocusedThreadName = string.Empty;
		m_ThreadTimelineThreadOrder.Clear();
		m_HiddenThreadNames.Clear();
		AttachTimelineState(Selection, Viewport);
		UpdateTimelineText();
		UpdateFooterText();
		StatusText = "Closed profiler session.";
	}

	private void ApplyCoreSummary()
	{
		if (CurrentDocument?.Session == null)
		{
			CoreRows = Array.Empty<CoreSummaryRow>();
			CoreSummaryText = "Generated sample has no profiler core stream.";
			return;
		}

		int coreCount = Math.Max(0, CurrentDocument.Session.CoreCount);
		List<CoreSummaryRow> rows = new List<CoreSummaryRow>(coreCount);
		if (coreCount == 0)
		{
			CoreRows = rows;
			CoreSummaryText = "No core data found in this session.";
			return;
		}

		long startTime = 0L;
		long endTime = 0L;
		List<List<ContextSwitch>> contextSwitches = null;
		if (TryGetVisibleFrameTimeRange(out long visibleStartTime, out long visibleEndTime))
		{
			startTime = visibleStartTime;
			endTime = visibleEndTime;
		}
		if (CurrentDocument.Session.RecordingContextSwitches && endTime > startTime)
		{
			CurrentDocument.Session.GetContextSwitches(startTime, endTime, out contextSwitches);
		}

		for (int i = 0; i < coreCount; i++)
		{
			IEnumerable<long> contextSwitchTimes = contextSwitches != null && i < contextSwitches.Count
				? contextSwitches[i].Select(item => item.m_Timestamp)
				: Array.Empty<long>();
			rows.Add(new CoreSummaryRow(i, contextSwitchTimes, startTime, endTime));
		}

		CoreRows = rows;
		CoreSummaryText = CurrentDocument.Session.RecordingContextSwitches
			? $"{coreCount} cores, context switches in {Viewport.RangeText}"
			: $"{coreCount} cores, context switch recording not available";
	}

	private bool TryGetVisibleFrameTimeRange(out long startTime, out long endTime)
	{
		startTime = 0L;
		endTime = 0L;
		if (CurrentDocument?.Session == null || Viewport == null || CurrentDocument.Session.FrameCount == 0)
		{
			return false;
		}

		int startFrame = Math.Max(0, Math.Min(Viewport.StartFrame, CurrentDocument.Session.FrameCount - 1));
		int endFrame = Math.Max(startFrame, Math.Min(Viewport.EndFrame, CurrentDocument.Session.FrameCount - 1));
		Frame start = CurrentDocument.Session.GetFrame(startFrame);
		Frame end = CurrentDocument.Session.GetFrame(endFrame);
		if (start == null || end == null || !start.Valid || !end.Valid)
		{
			return false;
		}

		startTime = start.StartTime;
		endTime = end.EndTime;
		return true;
	}

	private void ApplyLogMessages(SessionDocument document)
	{
		if (document?.Session == null)
		{
			LogRows = Array.Empty<LogMessageRow>();
			LogSummaryText = "Generated sample has no profiler log stream.";
			return;
		}

		List<LogMessage> messages = new List<LogMessage>();
		document.Session.GetLogMessages(0, messages);
		LogRows = messages
			.Select((message, index) => new LogMessageRow(index, message.Time, message.Message))
			.ToArray();
		LogSummaryText = LogRows.Count == 0
			? "No log messages found in this session."
			: $"{LogRows.Count} log messages";
	}

	private void ApplyScopeHotspots(SessionDocument document)
	{
		m_AllScopeHotspots = ScopeHotspotAnalyzer.Build(document);
		ApplyScopeHotspotFilter();
	}

	private void ApplyScopeHotspotFilter()
	{
		IReadOnlyList<ScopeHotspotRow> filteredRows;
		if (CurrentDocument?.Session == null)
		{
			filteredRows = m_AllScopeHotspots;
			ScopeHotspotSummaryText = "Generated sample has no profiler scope stream.";
		}
		else if (m_AllScopeHotspots.Count == 0)
		{
			filteredRows = m_AllScopeHotspots;
			ScopeHotspotSummaryText = "No scope hotspots found in this session.";
		}
		else if (string.IsNullOrWhiteSpace(ScopeFilterText))
		{
			filteredRows = m_AllScopeHotspots;
			ScopeHotspotSummaryText = $"Top {m_AllScopeHotspots.Count} scopes by total time";
		}
		else
		{
			string filter = ScopeFilterText.Trim();
			filteredRows = m_AllScopeHotspots
				.Where(item => item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				.ToArray();
			ScopeHotspotSummaryText = $"{filteredRows.Count} of {m_AllScopeHotspots.Count} scopes match \"{filter}\"";
		}

		ScopeHotspots = ProfilerTableSorter.SortHotspots(filteredRows, m_ScopeHotspotSortKey, m_ScopeHotspotSortDescending);
	}

	private void SelectSlowestFrame()
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		FrameSample slowestFrame = CurrentDocument.FrameSamples.OrderByDescending(item => item.DurationMs).First();
		SelectAndCenterFrame(slowestFrame);
		StatusText = $"Selected slowest frame {slowestFrame.Index} ({slowestFrame.DurationMs:0.###} ms).";
	}

	private void TrackSelectedFrame()
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		int frameIndex = Selection.SelectedFrameIndex >= 0
			? Selection.SelectedFrameIndex
			: Math.Min(CurrentDocument.Summary.FrameCount - 1, Math.Max(0, Viewport.StartFrame));
		FrameSample frame = CurrentDocument.FrameSamples.FirstOrDefault(item => item.Index == frameIndex);
		if (frame.Index != frameIndex)
		{
			frame = CurrentDocument.FrameSamples.OrderBy(item => Math.Abs(item.Index - frameIndex)).First();
		}

		SelectAndCenterFrame(frame);
		StatusText = $"Tracking frame {frame.Index} ({frame.DurationMs:0.###} ms).";
	}

	private void SelectLastFrame()
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		FrameSample frame = CurrentDocument.FrameSamples.OrderByDescending(item => item.Index).First();
		SelectAndCenterFrame(frame);
		StatusText = $"Selected last frame {frame.Index} ({frame.DurationMs:0.###} ms).";
	}

	private bool CanFindScope()
	{
		return CurrentDocument?.Session != null &&
			CurrentDocument.FrameSamples != null &&
			CurrentDocument.FrameSamples.Length > 0 &&
			!string.IsNullOrWhiteSpace(ScopeFilterText);
	}

	private void FindScope(int direction)
	{
		if (!CanFindScope())
		{
			StatusText = string.IsNullOrWhiteSpace(ScopeFilterText)
				? "Enter scope text to find."
				: "No profiler scope stream is loaded.";
			return;
		}

		string filter = ScopeFilterText.Trim();
		FrameSample? matchingFrame = FindScopeFrame(filter, direction);
		if (!matchingFrame.HasValue)
		{
			StatusText = direction < 0
				? $"No previous scope matching \"{filter}\"."
				: $"No next scope matching \"{filter}\".";
			return;
		}

		FrameSample frame = matchingFrame.Value;
		SelectAndCenterFrame(frame);
		ActiveSessionView = "Threads";
		IsThreadsDataGridVisible = true;
		StatusText = direction < 0
			? $"Found previous \"{filter}\" in frame {frame.Index}."
			: $"Found next \"{filter}\" in frame {frame.Index}.";
	}

	private FrameSample? FindScopeFrame(string filter, int direction)
	{
		int currentFrame = Selection.SelectedFrameIndex >= 0
			? Selection.SelectedFrameIndex
			: direction < 0 ? Viewport.EndFrame + 1 : Viewport.StartFrame - 1;
		IEnumerable<FrameSample> samples = direction < 0
			? CurrentDocument.FrameSamples.Where(item => item.Index < currentFrame).OrderByDescending(item => item.Index)
			: CurrentDocument.FrameSamples.Where(item => item.Index > currentFrame).OrderBy(item => item.Index);
		foreach (FrameSample sample in samples)
		{
			IReadOnlyList<ScopeFrameDetailRow> scopes = ScopeFrameDetailAnalyzer.Build(CurrentDocument, sample.Index);
			if (scopes.Any(item => item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
			{
				return sample;
			}
		}

		return null;
	}

	private void SelectAdjacentSpike(int direction)
	{
		if (CurrentDocument?.FrameSamples == null || CurrentDocument.FrameSamples.Length == 0)
		{
			StatusText = "No frame samples loaded.";
			return;
		}

		double thresholdMs = CurrentDocument.Summary.TargetFrameTimeMs > 0.0
			? CurrentDocument.Summary.TargetFrameTimeMs
			: CurrentDocument.FrameSamples.Average(item => item.DurationMs);
		int currentFrame = Selection.SelectedFrameIndex >= 0
			? Selection.SelectedFrameIndex
			: direction < 0 ? Viewport.EndFrame + 1 : Viewport.StartFrame - 1;
		FrameSample spikeFrame = direction < 0
			? CurrentDocument.FrameSamples
				.Where(item => item.DurationMs >= thresholdMs && item.Index < currentFrame)
				.OrderByDescending(item => item.Index)
				.FirstOrDefault()
			: CurrentDocument.FrameSamples
				.Where(item => item.DurationMs >= thresholdMs && item.Index > currentFrame)
				.OrderBy(item => item.Index)
				.FirstOrDefault();

		if (spikeFrame.Index == 0 && spikeFrame.DurationMs == 0.0)
		{
			StatusText = direction < 0
				? $"No previous spike frame over {thresholdMs:0.###} ms."
				: $"No next spike frame over {thresholdMs:0.###} ms.";
			return;
		}

		SelectAndCenterFrame(spikeFrame);
		StatusText = direction < 0
			? $"Selected previous spike frame {spikeFrame.Index} ({spikeFrame.DurationMs:0.###} ms)."
			: $"Selected next spike frame {spikeFrame.Index} ({spikeFrame.DurationMs:0.###} ms).";
	}

	private void SelectAndCenterFrame(FrameSample frame)
	{
		int visibleCount = Math.Min(CurrentDocument.Summary.FrameCount, 120);
		int startFrame = Math.Max(0, frame.Index - (visibleCount / 2));
		int endFrame = Math.Min(CurrentDocument.Summary.FrameCount - 1, startFrame + visibleCount - 1);
		startFrame = Math.Max(0, endFrame - visibleCount + 1);

		Viewport.SetRange(startFrame, endFrame);
		Selection.SelectedFrameIndex = frame.Index;
		Selection.SelectedFrameTimeMs = frame.DurationMs;
	}

	private void ResetTimeline()
	{
		if (CurrentDocument == null)
		{
			return;
		}
		CurrentDocument.Viewport.Reset(CurrentDocument.Summary.FrameCount);
		CurrentDocument.Selection.SelectedFrameIndex = -1;
		CurrentDocument.Selection.HoveredFrameIndex = -1;
		CurrentDocument.Selection.SelectedFrameTimeMs = 0.0;
		CurrentDocument.Selection.HoveredFrameTimeMs = 0.0;
		ApplySelectedFrameScopes();
		ApplySelectedFrameCounters();
		UpdateTimelineText();
		UpdateFooterText();
	}

	private void PanTimeline(int direction)
	{
		if (CurrentDocument == null || Viewport == null)
		{
			return;
		}

		int deltaFrames = Math.Max(1, Viewport.VisibleFrameCount / 5);
		Viewport.ScrollFrames(direction * deltaFrames);
		StatusText = direction < 0
			? "Scrolled timeline left to " + Viewport.RangeText + "."
			: "Scrolled timeline right to " + Viewport.RangeText + ".";
	}

	private void ZoomTimeline(double factor)
	{
		if (CurrentDocument == null || Viewport == null)
		{
			return;
		}

		Viewport.Zoom(factor, 0.5);
		StatusText = factor < 1.0
			? "Zoomed timeline in to " + Viewport.RangeText + "."
			: "Zoomed timeline out to " + Viewport.RangeText + ".";
	}

	private void AttachTimelineState(TimelineSelection selection, TimelineViewport viewport)
	{
		if (selection != null)
		{
			selection.PropertyChanged += TimelineStatePropertyChanged;
		}
		if (viewport != null)
		{
			viewport.PropertyChanged += TimelineStatePropertyChanged;
		}
	}

	private void DetachTimelineState(TimelineSelection selection, TimelineViewport viewport)
	{
		if (selection != null)
		{
			selection.PropertyChanged -= TimelineStatePropertyChanged;
		}
		if (viewport != null)
		{
			viewport.PropertyChanged -= TimelineStatePropertyChanged;
		}
	}

	private void TimelineStatePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex))
		{
			ApplySelectedFrameScopes();
			ApplySelectedFrameCounters();
		}
		if (e.PropertyName == nameof(TimelineViewport.StartFrame) ||
			e.PropertyName == nameof(TimelineViewport.EndFrame))
		{
			ApplyCoreSummary();
			ApplyVisibleThreadTimeline();
		}
		UpdateTimelineText();
		UpdateFooterText();
	}

	private void ApplySelectedFrameScopes()
	{
		int selectedFrameIndex = Selection?.SelectedFrameIndex ?? -1;
		SelectedFrameScopes = ProfilerTableSorter.SortFrameScopes(
			ScopeFrameDetailAnalyzer.Build(CurrentDocument, selectedFrameIndex),
			m_SelectedFrameScopeSortKey,
			m_SelectedFrameScopeSortDescending);
		m_OpenScopeSourceCommand.RaiseCanExecuteChanged();
		if (CurrentDocument?.Session == null)
		{
			SelectedFrameScopeSummaryText = "Generated sample has no profiler scope stream.";
		}
		else if (selectedFrameIndex < 0)
		{
			SelectedFrameScopeSummaryText = "Select a frame to inspect scopes.";
		}
		else if (SelectedFrameScopes.Count == 0)
		{
			SelectedFrameScopeSummaryText = $"No scopes found for frame {selectedFrameIndex}.";
		}
		else
		{
			SelectedFrameScopeSummaryText = $"Frame {selectedFrameIndex}: {SelectedFrameScopes.Count} scopes";
		}
	}

	private void ApplyVisibleThreadTimeline()
	{
		IReadOnlyList<ThreadTimelineScopeRow> allRows = ThreadTimelineScopeAnalyzer.Build(CurrentDocument, Viewport);
		string filter = ThreadFilterText?.Trim();
		if (!string.IsNullOrWhiteSpace(filter))
		{
			allRows = allRows
				.Where(item =>
					item.ThreadName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
					item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				.ToArray();
		}
		if (m_AreThreadScopesCollapsed)
		{
			allRows = allRows.Where(item => item.Depth == 0).ToArray();
		}

		UpdateThreadOrder(allRows);
		IReadOnlyList<IGrouping<string, ThreadTimelineScopeRow>> threadGroups = allRows
			.Where(item => !m_HiddenThreadNames.Contains(item.ThreadName))
			.GroupBy(item => item.ThreadName, StringComparer.Ordinal)
			.OrderBy(group => GetThreadOrderIndex(group.Key))
			.ToArray();
		ThreadTimelineTotalThreadCount = threadGroups.Count;
		int maxFirstThreadIndex = Math.Max(0, ThreadTimelineTotalThreadCount - m_MaxVisibleThreadTimelineThreads);
		if (ThreadTimelineFirstVisibleThreadIndex > maxFirstThreadIndex)
		{
			ThreadTimelineFirstVisibleThreadIndex = maxFirstThreadIndex;
		}

		string[] visibleThreadNames = threadGroups
			.Skip(ThreadTimelineFirstVisibleThreadIndex)
			.Take(m_MaxVisibleThreadTimelineThreads)
			.Select(group => group.Key)
			.ToArray();
		HashSet<string> visibleThreadNameSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (string threadName in visibleThreadNames)
		{
			visibleThreadNameSet.Add(threadName);
		}
		VisibleThreadScopes = allRows.Where(item => visibleThreadNameSet.Contains(item.ThreadName)).ToArray();
		if (CurrentDocument?.Session == null)
		{
			ThreadTimelineSummaryText = "Generated sample has no profiler scope stream.";
		}
		else if (VisibleThreadScopes.Count == 0)
		{
			ThreadTimelineSummaryText = string.IsNullOrWhiteSpace(filter)
				? "No scopes found in " + (Viewport?.RangeText ?? "visible range") + "."
				: "No thread scopes match \"" + filter + "\" in " + (Viewport?.RangeText ?? "visible range") + ".";
		}
		else
		{
			int threadCount = VisibleThreadScopes.Select(item => item.ThreadName).Distinct(StringComparer.Ordinal).Count();
			string visibleWindowText = ThreadTimelineTotalThreadCount > m_MaxVisibleThreadTimelineThreads
				? $" showing threads {ThreadTimelineFirstVisibleThreadIndex + 1}-{ThreadTimelineFirstVisibleThreadIndex + threadCount} of {ThreadTimelineTotalThreadCount}"
				: string.Empty;
			string collapseText = m_AreThreadScopesCollapsed ? ", top-level scopes" : string.Empty;
			ThreadTimelineSummaryText = string.IsNullOrWhiteSpace(filter)
				? $"{VisibleThreadScopes.Count} scope events across {threadCount} threads in {Viewport.RangeText}{collapseText}"
				: $"{VisibleThreadScopes.Count} matching scope events across {threadCount} threads in {Viewport.RangeText}{collapseText}";
			ThreadTimelineSummaryText += visibleWindowText;
		}
	}

	private void ScrollThreadTimeline(int deltaThreads)
	{
		int maxFirstThreadIndex = Math.Max(0, ThreadTimelineTotalThreadCount - m_MaxVisibleThreadTimelineThreads);
		int nextIndex = Math.Max(0, Math.Min(maxFirstThreadIndex, ThreadTimelineFirstVisibleThreadIndex + deltaThreads));
		if (nextIndex == ThreadTimelineFirstVisibleThreadIndex)
		{
			return;
		}

		ThreadTimelineFirstVisibleThreadIndex = nextIndex;
		ApplyVisibleThreadTimeline();
		StatusText = $"Showing thread lanes {ThreadTimelineFirstVisibleThreadIndex + 1}-{Math.Min(ThreadTimelineTotalThreadCount, ThreadTimelineFirstVisibleThreadIndex + m_MaxVisibleThreadTimelineThreads)} of {ThreadTimelineTotalThreadCount}.";
	}

	private void FocusThread(object parameter)
	{
		FocusedThreadName = parameter as string;
		StatusText = string.IsNullOrWhiteSpace(FocusedThreadName)
			? "No thread selected."
			: "Thread menu target: " + FocusedThreadName;
	}

	private void HideFocusedThread()
	{
		if (string.IsNullOrWhiteSpace(FocusedThreadName))
		{
			return;
		}

		m_HiddenThreadNames.Add(FocusedThreadName);
		ApplyVisibleThreadTimeline();
		StatusText = "Hidden thread lane: " + FocusedThreadName;
	}

	private bool CanMoveFocusedThread(int direction)
	{
		if (string.IsNullOrWhiteSpace(FocusedThreadName))
		{
			return false;
		}

		int index = m_ThreadTimelineThreadOrder.IndexOf(FocusedThreadName);
		int nextIndex = index + direction;
		return index >= 0 && nextIndex >= 0 && nextIndex < m_ThreadTimelineThreadOrder.Count;
	}

	private void MoveFocusedThread(int direction)
	{
		if (!CanMoveFocusedThread(direction))
		{
			return;
		}

		int index = m_ThreadTimelineThreadOrder.IndexOf(FocusedThreadName);
		int nextIndex = index + direction;
		string threadName = m_ThreadTimelineThreadOrder[index];
		m_ThreadTimelineThreadOrder[index] = m_ThreadTimelineThreadOrder[nextIndex];
		m_ThreadTimelineThreadOrder[nextIndex] = threadName;
		ApplyVisibleThreadTimeline();
		StatusText = direction < 0 ? "Moved thread up: " + threadName : "Moved thread down: " + threadName;
	}

	private void SetThreadScopeCollapseState(bool collapsed)
	{
		m_AreThreadScopesCollapsed = collapsed;
		m_MaxVisibleThreadTimelineThreads = collapsed ? CollapsedVisibleThreadTimelineThreads : DefaultVisibleThreadTimelineThreads;
		ThreadTimelineFirstVisibleThreadIndex = 0;
		ApplyVisibleThreadTimeline();
		StatusText = collapsed ? "Collapsed all thread scopes to top-level lanes." : "Expanded all thread scopes.";
	}

	private void OpenThreadSettingsPanel()
	{
		IsSettingsPanelVisible = false;
		IsAndroidPanelVisible = false;
		IsConnectionPanelVisible = false;
		IsCallstacksPanelVisible = false;
		IsHelpPanelVisible = false;
		IsRegistrationPanelVisible = false;
		IsUpdateCheckPanelVisible = false;
		IsAboutPanelVisible = false;
		IsThreadSettingsPanelVisible = true;
		ActiveSessionView = "Threads";
		StatusText = "Thread View Settings panel opened.";
	}

	private void CloseThreadSettingsPanel()
	{
		IsThreadSettingsPanelVisible = false;
		StatusText = "Thread View Settings panel closed.";
	}

	private void UpdateThreadOrder(IReadOnlyList<ThreadTimelineScopeRow> rows)
	{
		string[] discoveredNames = rows
			.Select(item => item.ThreadName)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		HashSet<string> discoveredSet = new HashSet<string>(discoveredNames, StringComparer.Ordinal);
		m_ThreadTimelineThreadOrder.RemoveAll(item => !discoveredSet.Contains(item));
		foreach (string threadName in discoveredNames)
		{
			if (!m_ThreadTimelineThreadOrder.Contains(threadName))
			{
				m_ThreadTimelineThreadOrder.Add(threadName);
			}
		}
		m_HiddenThreadNames.RemoveWhere(item => !discoveredSet.Contains(item));
		m_MoveFocusedThreadUpCommand.RaiseCanExecuteChanged();
		m_MoveFocusedThreadDownCommand.RaiseCanExecuteChanged();
	}

	private int GetThreadOrderIndex(string threadName)
	{
		int index = m_ThreadTimelineThreadOrder.IndexOf(threadName);
		return index < 0 ? int.MaxValue : index;
	}

	private void SortTable(object parameter)
	{
		string[] parts = (parameter as string)?.Split(':');
		if (parts == null || parts.Length != 2)
		{
			return;
		}

		string tableName = parts[0];
		string sortKey = parts[1];
		if (string.Equals(tableName, "Hotspot", StringComparison.Ordinal))
		{
			ToggleSort(ref m_ScopeHotspotSortKey, ref m_ScopeHotspotSortDescending, sortKey, IsDescendingByDefault(sortKey));
			ApplyScopeHotspotFilter();
			StatusText = "Sorted scope hotspots by " + sortKey + ".";
		}
		else if (string.Equals(tableName, "FrameScope", StringComparison.Ordinal))
		{
			ToggleSort(ref m_SelectedFrameScopeSortKey, ref m_SelectedFrameScopeSortDescending, sortKey, IsDescendingByDefault(sortKey));
			ApplySelectedFrameScopes();
			StatusText = "Sorted selected frame scopes by " + sortKey + ".";
		}
		else if (string.Equals(tableName, "Counter", StringComparison.Ordinal))
		{
			ToggleSort(ref m_SelectedFrameCounterSortKey, ref m_SelectedFrameCounterSortDescending, sortKey, IsDescendingByDefault(sortKey));
			ApplySelectedFrameCounters();
			StatusText = "Sorted selected frame counters by " + sortKey + ".";
		}
	}

	private static void ToggleSort(ref string currentKey, ref bool currentDescending, string newKey, bool defaultDescending)
	{
		if (string.Equals(currentKey, newKey, StringComparison.Ordinal))
		{
			currentDescending = !currentDescending;
			return;
		}

		currentKey = newKey;
		currentDescending = defaultDescending;
	}

	private static bool IsDescendingByDefault(string sortKey)
	{
		return sortKey is "TotalTime" or "Calls" or "Average" or "MaxTime" or "MaxCount" or "Duration" or "Value" or "Count";
	}

	private void ApplySelectedFrameCounters()
	{
		int selectedFrameIndex = Selection?.SelectedFrameIndex ?? -1;
		SelectedFrameCounters = ProfilerTableSorter.SortCounters(
			SelectedFrameCounterAnalyzer.Build(CurrentDocument, selectedFrameIndex),
			m_SelectedFrameCounterSortKey,
			m_SelectedFrameCounterSortDescending);
		if (CurrentDocument?.Session == null)
		{
			SelectedFrameCounterSummaryText = "Generated sample has no profiler counter stream.";
		}
		else if (selectedFrameIndex < 0)
		{
			SelectedFrameCounterSummaryText = "Select a frame to inspect counters.";
		}
		else if (SelectedFrameCounters.Count == 0)
		{
			SelectedFrameCounterSummaryText = $"No counters found for frame {selectedFrameIndex}.";
		}
		else
		{
			SelectedFrameCounterSummaryText = $"Frame {selectedFrameIndex}: {SelectedFrameCounters.Count} counters";
		}
	}

	private void OpenScopeSource(object parameter)
	{
		if (parameter is not ScopeFrameDetailRow row || !row.CanOpenSource)
		{
			StatusText = "Selected scope has no source location.";
			return;
		}

		string sourceFile = SourcePathMapper.Resolve(row.SourceFile, CapturedSourceRoot, LocalSourceRoot);
		if (m_SourceViewerLauncher.TryLaunch(sourceFile, row.SourceLine, out string error))
		{
			StatusText = string.Equals(sourceFile, row.SourceFile, StringComparison.OrdinalIgnoreCase)
				? "Opened source: " + row.SourceText
				: "Opened mapped source: " + sourceFile;
		}
		else
		{
			StatusText = error;
		}
	}

	private void UpdateTimelineText()
	{
		TimelineRangeText = Viewport == null ? string.Empty : Viewport.RangeText;
	}

	private void UpdateFooterText()
	{
		if (CurrentDocument == null)
		{
			FooterText = "No profiler session loaded";
			StatusBarSessionText = "none";
			StatusBarSelectedFrameText = "none";
			StatusBarHoveredFrameText = "none";
			return;
		}

		string selectedText = Selection.SelectedFrameIndex >= 0
			? $"selected frame {Selection.SelectedFrameIndex} ({Selection.SelectedFrameTimeMs:0.###} ms)"
			: "no frame selected";
		string hoverText = Selection.HoveredFrameIndex >= 0
			? $"hover frame {Selection.HoveredFrameIndex} ({Selection.HoveredFrameTimeMs:0.###} ms)"
			: "hover none";
		StatusBarSessionText = $"{CurrentDocument.Summary.FrameCount} frames";
		StatusBarSelectedFrameText = Selection.SelectedFrameIndex >= 0
			? $"{Selection.SelectedFrameIndex} ({Selection.SelectedFrameTimeMs:0.###} ms)"
			: "none";
		StatusBarHoveredFrameText = Selection.HoveredFrameIndex >= 0
			? $"{Selection.HoveredFrameIndex} ({Selection.HoveredFrameTimeMs:0.###} ms)"
			: "none";
		FooterText = $"{CurrentDocument.Summary.FrameCount} frames | {Viewport.RangeText} | {selectedText} | {hoverText}";
	}
}
