using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	private CancellationTokenSource m_LoadCancellation;
	private SessionDocument m_CurrentDocument;
	private IReadOnlyList<FrameSample> m_FrameSamples = Array.Empty<FrameSample>();
	private TimelineSelection m_Selection = new TimelineSelection();
	private TimelineViewport m_Viewport = TimelineViewport.CreateForFrames(0);
	private IReadOnlyList<ScopeHotspotRow> m_ScopeHotspots = Array.Empty<ScopeHotspotRow>();
	private string m_StatusText = "Open a profiler file or load generated sample data.";
	private string m_FooterText = "Avalonia + SkiaSharp migration prototype";
	private string m_TimelineRangeText = string.Empty;
	private string m_ScopeHotspotSummaryText = "No profiler scope data loaded.";
	private bool m_IsLoading;

	public MainWindowViewModel(bool loadSampleOnStartup = false)
	{
		Summary = new SessionSummaryViewModel();
		m_OpenSessionCommand = new RelayCommand(_ => _ = OpenSessionAsync());
		m_LoadSampleCommand = new RelayCommand(_ => _ = LoadSampleAsync());
		m_ResetTimelineCommand = new RelayCommand(_ => ResetTimeline(), _ => CurrentDocument != null);
		AttachTimelineState(Selection, Viewport);
		if (loadSampleOnStartup)
		{
			_ = LoadSampleAsync();
		}
	}

	public RelayCommand OpenSessionCommand => m_OpenSessionCommand;

	public RelayCommand LoadSampleCommand => m_LoadSampleCommand;

	public RelayCommand ResetTimelineCommand => m_ResetTimelineCommand;

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

			await LoadDocumentAsync(() => m_SessionLoader.LoadFileAsync(path, m_LoadCancellation.Token), "Loading " + path);
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private Task LoadSampleAsync()
	{
		return LoadDocumentAsync(() => m_SessionLoader.LoadSampleAsync(m_LoadCancellation.Token), "Loading generated sample data");
	}

	private async Task LoadDocumentAsync(Func<Task<SessionDocument>> load, string loadingStatus)
	{
		m_LoadCancellation?.Cancel();
		m_LoadCancellation = new CancellationTokenSource();
		try
		{
			IsLoading = true;
			StatusText = loadingStatus + "...";
			SessionDocument document = await load();
			ApplyDocument(document);
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

	private void ApplyDocument(SessionDocument document)
	{
		DetachTimelineState(Selection, Viewport);
		CurrentDocument = document;
		FrameSamples = document.FrameSamples;
		Summary.Apply(document.Summary);
		Selection = document.Selection;
		Viewport = document.Viewport;
		ApplyScopeHotspots(document);
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
		UpdateTimelineText();
		UpdateFooterText();
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
