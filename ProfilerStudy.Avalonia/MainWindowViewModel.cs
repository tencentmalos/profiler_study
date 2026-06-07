using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FramePro;

namespace ProfilerStudy.Avalonia;

internal sealed class MainWindowViewModel : ObservableObject
{
	private const int DefaultVisibleThreadTimelineThreads = 8;
	private const int CollapsedVisibleThreadTimelineThreads = 12;

	private readonly SessionLoader m_SessionLoader = new SessionLoader();
	private readonly RelayCommand m_OpenSessionCommand;
	private readonly RelayCommand m_LoadSampleCommand;
	private readonly RelayCommand m_ResetTimelineCommand;
	private readonly RelayCommand m_SelectSlowestFrameCommand;
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
	private readonly RelayCommand m_ToggleThreadsPanelCommand;
	private readonly ISourceViewerLauncher m_SourceViewerLauncher;
	private readonly IAppSettingsService m_AppSettingsService;
	private readonly AppSettings m_AppSettings;
	private CancellationTokenSource m_LoadCancellation;
	private SessionDocument m_CurrentDocument;
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
	private string m_FooterText = "Avalonia + SkiaSharp migration prototype";
	private string m_TimelineRangeText = string.Empty;
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
	private bool m_IsThreadsInfoPanelVisible = true;
	private bool m_IsThreadsFrameGraphVisible = true;
	private bool m_IsThreadsScopeGraphVisible = true;
	private bool m_IsThreadsCoreGraphVisible = true;
	private bool m_IsThreadsCustomStatsVisible = true;
	private bool m_IsThreadsDataGridVisible = true;
	private readonly List<string> m_ThreadTimelineThreadOrder = new List<string>();
	private readonly HashSet<string> m_HiddenThreadNames = new HashSet<string>(StringComparer.Ordinal);
	private string m_ScopeHotspotSortKey = "TotalTime";
	private bool m_ScopeHotspotSortDescending = true;
	private string m_SelectedFrameScopeSortKey = "Start";
	private bool m_SelectedFrameScopeSortDescending;
	private string m_SelectedFrameCounterSortKey = "Graph";
	private bool m_SelectedFrameCounterSortDescending;
	private bool m_IsLoading;

	public MainWindowViewModel(bool loadSampleOnStartup = false, string startupProfilerPath = null)
		: this(new SourceViewerLauncher(), new AppSettingsService(), loadSampleOnStartup, startupProfilerPath)
	{
	}

	internal MainWindowViewModel(ISourceViewerLauncher sourceViewerLauncher, IAppSettingsService appSettingsService, bool loadSampleOnStartup = false, string startupProfilerPath = null)
	{
		m_SourceViewerLauncher = sourceViewerLauncher ?? new SourceViewerLauncher();
		m_AppSettingsService = appSettingsService ?? new AppSettingsService();
		m_AppSettings = m_AppSettingsService.Load() ?? new AppSettings();
		Summary = new SessionSummaryViewModel();
		m_OpenSessionCommand = new RelayCommand(_ => _ = OpenSessionAsync());
		m_LoadSampleCommand = new RelayCommand(_ => _ = LoadSampleAsync());
		m_ResetTimelineCommand = new RelayCommand(_ => ResetTimeline(), _ => CurrentDocument != null);
		m_SelectSlowestFrameCommand = new RelayCommand(_ => SelectSlowestFrame(), _ => CurrentDocument?.FrameSamples?.Length > 0);
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
		m_ThreadSettingsCommand = new RelayCommand(_ => ShowThreadSettingsStatus());
		m_ToggleThreadsPanelCommand = new RelayCommand(ToggleThreadsPanel);
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

	public RelayCommand ResetTimelineCommand => m_ResetTimelineCommand;

	public RelayCommand SelectSlowestFrameCommand => m_SelectSlowestFrameCommand;

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

	public RelayCommand ToggleThreadsPanelCommand => m_ToggleThreadsPanelCommand;

	public SessionSummaryViewModel Summary { get; }

	public SessionDocument CurrentDocument
	{
		get => m_CurrentDocument;
		private set
		{
			if (SetProperty(ref m_CurrentDocument, value))
			{
				m_ResetTimelineCommand.RaiseCanExecuteChanged();
				m_SelectSlowestFrameCommand.RaiseCanExecuteChanged();
				m_PanTimelineLeftCommand.RaiseCanExecuteChanged();
				m_PanTimelineRightCommand.RaiseCanExecuteChanged();
				m_ZoomTimelineInCommand.RaiseCanExecuteChanged();
				m_ZoomTimelineOutCommand.RaiseCanExecuteChanged();
			}
		}
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
		private set => SetProperty(ref m_StatusText, value);
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
		private set => SetProperty(ref m_IsThreadsInfoPanelVisible, value);
	}

	public bool IsThreadsFrameGraphVisible
	{
		get => m_IsThreadsFrameGraphVisible;
		private set => SetProperty(ref m_IsThreadsFrameGraphVisible, value);
	}

	public bool IsThreadsScopeGraphVisible
	{
		get => m_IsThreadsScopeGraphVisible;
		private set => SetProperty(ref m_IsThreadsScopeGraphVisible, value);
	}

	public bool IsThreadsCoreGraphVisible
	{
		get => m_IsThreadsCoreGraphVisible;
		private set => SetProperty(ref m_IsThreadsCoreGraphVisible, value);
	}

	public bool IsThreadsCustomStatsVisible
	{
		get => m_IsThreadsCustomStatsVisible;
		private set => SetProperty(ref m_IsThreadsCustomStatsVisible, value);
	}

	public bool IsThreadsDataGridVisible
	{
		get => m_IsThreadsDataGridVisible;
		private set => SetProperty(ref m_IsThreadsDataGridVisible, value);
	}

	public IBrush ThreadsViewBrush => IsThreadsViewActive ? Brushes.RoyalBlue : Brushes.DimGray;

	public IBrush CoresViewBrush => IsCoresViewActive ? Brushes.RoyalBlue : Brushes.DimGray;

	public IBrush ScopesViewBrush => IsScopesViewActive ? Brushes.RoyalBlue : Brushes.DimGray;

	public IBrush CustomStatsViewBrush => IsCustomStatsViewActive ? Brushes.RoyalBlue : Brushes.DimGray;

	public IBrush LogViewBrush => IsLogViewActive ? Brushes.RoyalBlue : Brushes.DimGray;

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
		StatusText = $"Selected frame {frame.Index} from profiler scope timeline ({frame.DurationMs:0.###} ms).";
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

	private async Task OpenStartupProfilerAsync(string path)
	{
		if (!File.Exists(path))
		{
			StatusText = "Startup profiler file not found: " + path;
			return;
		}

		await LoadFileDocumentAsync(path);
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
		m_AppSettingsService.Save(m_AppSettings);
	}

	private void SaveSourcePathSettings()
	{
		m_AppSettings.CapturedSourceRoot = CapturedSourceRoot ?? string.Empty;
		m_AppSettings.LocalSourceRoot = LocalSourceRoot ?? string.Empty;
		m_AppSettingsService.Save(m_AppSettings);
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
		int visibleCount = Math.Min(CurrentDocument.Summary.FrameCount, 120);
		int startFrame = Math.Max(0, slowestFrame.Index - (visibleCount / 2));
		int endFrame = Math.Min(CurrentDocument.Summary.FrameCount - 1, startFrame + visibleCount - 1);
		startFrame = Math.Max(0, endFrame - visibleCount + 1);

		Viewport.SetRange(startFrame, endFrame);
		Selection.SelectedFrameIndex = slowestFrame.Index;
		Selection.SelectedFrameTimeMs = slowestFrame.DurationMs;
		StatusText = $"Selected slowest frame {slowestFrame.Index} ({slowestFrame.DurationMs:0.###} ms).";
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

	private void ShowThreadSettingsStatus()
	{
		StatusText = "Thread settings: right-click a lane, then use Hide, Move Up, Move Down, Collapse All, or Expand All.";
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
			FooterText = "Avalonia + SkiaSharp migration prototype";
			return;
		}

		string selectedText = Selection.SelectedFrameIndex >= 0
			? $"selected frame {Selection.SelectedFrameIndex} ({Selection.SelectedFrameTimeMs:0.###} ms)"
			: "no frame selected";
		string hoverText = Selection.HoveredFrameIndex >= 0
			? $"hover frame {Selection.HoveredFrameIndex} ({Selection.HoveredFrameTimeMs:0.###} ms)"
			: "hover none";
		FooterText = $"{CurrentDocument.Summary.FrameCount} frames | {Viewport.RangeText} | {selectedText} | {hoverText}";
	}
}
