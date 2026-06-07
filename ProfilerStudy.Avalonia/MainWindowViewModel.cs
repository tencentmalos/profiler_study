using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FramePro;

namespace ProfilerStudy.Avalonia;

internal sealed class MainWindowViewModel : ObservableObject
{
	private readonly SessionLoader m_SessionLoader = new SessionLoader();
	private readonly RelayCommand m_OpenSessionCommand;
	private readonly RelayCommand m_LoadSampleCommand;
	private readonly RelayCommand m_ResetTimelineCommand;
	private readonly RelayCommand m_SelectSlowestFrameCommand;
	private readonly RelayCommand m_OpenRecentSessionCommand;
	private readonly RelayCommand m_OpenScopeSourceCommand;
	private readonly RelayCommand m_SortTableCommand;
	private readonly RelayCommand m_SelectSessionViewCommand;
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
	private IReadOnlyList<ScopeFrameDetailRow> m_SelectedFrameScopes = Array.Empty<ScopeFrameDetailRow>();
	private IReadOnlyList<SelectedFrameCounterRow> m_SelectedFrameCounters = Array.Empty<SelectedFrameCounterRow>();
	private IReadOnlyList<LogMessageRow> m_LogRows = Array.Empty<LogMessageRow>();
	private string m_StatusText = "Open a profiler file or load generated sample data.";
	private string m_FooterText = "Avalonia + SkiaSharp migration prototype";
	private string m_TimelineRangeText = string.Empty;
	private string m_ScopeFilterText = string.Empty;
	private string m_ScopeHotspotSummaryText = "No profiler scope data loaded.";
	private string m_SelectedFrameScopeSummaryText = "Select a frame to inspect scopes.";
	private string m_SelectedFrameCounterSummaryText = "Select a frame to inspect counters.";
	private string m_LogSummaryText = "No profiler log messages loaded.";
	private string m_ActiveSessionView = "Threads";
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
		m_OpenRecentSessionCommand = new RelayCommand(parameter => _ = OpenRecentSessionAsync(parameter as string ?? SelectedRecentFile), _ => !string.IsNullOrWhiteSpace(SelectedRecentFile));
		m_OpenScopeSourceCommand = new RelayCommand(OpenScopeSource, parameter => parameter is ScopeFrameDetailRow row && row.CanOpenSource);
		m_SortTableCommand = new RelayCommand(SortTable);
		m_SelectSessionViewCommand = new RelayCommand(SelectSessionView);
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

	public RelayCommand OpenRecentSessionCommand => m_OpenRecentSessionCommand;

	public RelayCommand OpenScopeSourceCommand => m_OpenScopeSourceCommand;

	public RelayCommand SortTableCommand => m_SortTableCommand;

	public RelayCommand SelectSessionViewCommand => m_SelectSessionViewCommand;

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
			}
		}
	}

	public string ActiveSessionViewTitle => ActiveSessionView + " View";

	public bool IsThreadsViewActive => ActiveSessionView == "Threads";

	public bool IsCoresViewActive => ActiveSessionView == "Cores";

	public bool IsScopesViewActive => ActiveSessionView == "Scopes";

	public bool IsCustomStatsViewActive => ActiveSessionView == "Custom Stats";

	public bool IsLogViewActive => ActiveSessionView == "Log";

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
		ApplySelectedFrameScopes();
		ApplySelectedFrameCounters();
		ApplyLogMessages(document);
		AttachTimelineState(Selection, Viewport);
		UpdateTimelineText();
		UpdateFooterText();
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
