using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ProfilerStudy.Avalonia;

internal sealed class MainWindowViewModel : ObservableObject
{
	private readonly SessionLoader m_SessionLoader = new SessionLoader();
	private readonly RelayCommand m_OpenSessionCommand;
	private readonly RelayCommand m_LoadSampleCommand;
	private readonly RelayCommand m_ResetTimelineCommand;
	private readonly RelayCommand m_OpenRecentSessionCommand;
	private readonly RelayCommand m_OpenScopeSourceCommand;
	private readonly ISourceViewerLauncher m_SourceViewerLauncher;
	private readonly IAppSettingsService m_AppSettingsService;
	private readonly AppSettings m_AppSettings;
	private CancellationTokenSource m_LoadCancellation;
	private SessionDocument m_CurrentDocument;
	private IReadOnlyList<FrameSample> m_FrameSamples = Array.Empty<FrameSample>();
	private IReadOnlyList<string> m_RecentFiles = Array.Empty<string>();
	private string m_SelectedRecentFile;
	private TimelineSelection m_Selection = new TimelineSelection();
	private TimelineViewport m_Viewport = TimelineViewport.CreateForFrames(0);
	private IReadOnlyList<ScopeHotspotRow> m_ScopeHotspots = Array.Empty<ScopeHotspotRow>();
	private IReadOnlyList<ScopeFrameDetailRow> m_SelectedFrameScopes = Array.Empty<ScopeFrameDetailRow>();
	private string m_StatusText = "Open a profiler file or load generated sample data.";
	private string m_FooterText = "Avalonia + SkiaSharp migration prototype";
	private string m_TimelineRangeText = string.Empty;
	private string m_ScopeHotspotSummaryText = "No profiler scope data loaded.";
	private string m_SelectedFrameScopeSummaryText = "Select a frame to inspect scopes.";
	private bool m_IsLoading;

	public MainWindowViewModel(bool loadSampleOnStartup = false)
		: this(new SourceViewerLauncher(), new AppSettingsService(), loadSampleOnStartup)
	{
	}

	internal MainWindowViewModel(ISourceViewerLauncher sourceViewerLauncher, IAppSettingsService appSettingsService, bool loadSampleOnStartup = false)
	{
		m_SourceViewerLauncher = sourceViewerLauncher ?? new SourceViewerLauncher();
		m_AppSettingsService = appSettingsService ?? new AppSettingsService();
		m_AppSettings = m_AppSettingsService.Load() ?? new AppSettings();
		Summary = new SessionSummaryViewModel();
		m_OpenSessionCommand = new RelayCommand(_ => _ = OpenSessionAsync());
		m_LoadSampleCommand = new RelayCommand(_ => _ = LoadSampleAsync());
		m_ResetTimelineCommand = new RelayCommand(_ => ResetTimeline(), _ => CurrentDocument != null);
		m_OpenRecentSessionCommand = new RelayCommand(parameter => _ = OpenRecentSessionAsync(parameter as string ?? SelectedRecentFile), _ => !string.IsNullOrWhiteSpace(SelectedRecentFile));
		m_OpenScopeSourceCommand = new RelayCommand(OpenScopeSource, parameter => parameter is ScopeFrameDetailRow row && row.CanOpenSource);
		ApplyRecentFiles(m_AppSettings.RecentFiles);
		AttachTimelineState(Selection, Viewport);
		if (loadSampleOnStartup)
		{
			_ = LoadSampleAsync();
		}
	}

	public RelayCommand OpenSessionCommand => m_OpenSessionCommand;

	public RelayCommand LoadSampleCommand => m_LoadSampleCommand;

	public RelayCommand ResetTimelineCommand => m_ResetTimelineCommand;

	public RelayCommand OpenRecentSessionCommand => m_OpenRecentSessionCommand;

	public RelayCommand OpenScopeSourceCommand => m_OpenScopeSourceCommand;

	public SessionSummaryViewModel Summary { get; }

	public SessionDocument CurrentDocument
	{
		get => m_CurrentDocument;
		private set
		{
			if (SetProperty(ref m_CurrentDocument, value))
			{
				m_ResetTimelineCommand.RaiseCanExecuteChanged();
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

	public bool IsLoading
	{
		get => m_IsLoading;
		private set => SetProperty(ref m_IsLoading, value);
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
		AttachTimelineState(Selection, Viewport);
		UpdateTimelineText();
		UpdateFooterText();
	}

	private void ApplyScopeHotspots(SessionDocument document)
	{
		ScopeHotspots = ScopeHotspotAnalyzer.Build(document);
		if (document?.Session == null)
		{
			ScopeHotspotSummaryText = "Generated sample has no profiler scope stream.";
		}
		else if (ScopeHotspots.Count == 0)
		{
			ScopeHotspotSummaryText = "No scope hotspots found in this session.";
		}
		else
		{
			ScopeHotspotSummaryText = $"Top {ScopeHotspots.Count} scopes by total time";
		}
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
		}
		UpdateTimelineText();
		UpdateFooterText();
	}

	private void ApplySelectedFrameScopes()
	{
		int selectedFrameIndex = Selection?.SelectedFrameIndex ?? -1;
		SelectedFrameScopes = ScopeFrameDetailAnalyzer.Build(CurrentDocument, selectedFrameIndex);
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

	private void OpenScopeSource(object parameter)
	{
		if (parameter is not ScopeFrameDetailRow row || !row.CanOpenSource)
		{
			StatusText = "Selected scope has no source location.";
			return;
		}

		if (m_SourceViewerLauncher.TryLaunch(row.SourceFile, row.SourceLine, out string error))
		{
			StatusText = "Opened source: " + row.SourceText;
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
