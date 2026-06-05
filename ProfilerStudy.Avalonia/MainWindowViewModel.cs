using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ProfilerStudy.Avalonia;

internal sealed class MainWindowViewModel : ObservableObject
{
	private readonly SessionPreviewService m_PreviewService = new SessionPreviewService();
	private readonly RelayCommand m_OpenSessionCommand;
	private readonly RelayCommand m_LoadSampleCommand;
	private ObservableCollection<FrameSample> m_FrameSamples = new ObservableCollection<FrameSample>();
	private string m_StatusText = "Open a profiler file or load generated sample data.";
	private string m_FooterText = "Avalonia + SkiaSharp migration prototype";

	public MainWindowViewModel()
	{
		Summary = new SessionSummaryViewModel();
		m_OpenSessionCommand = new RelayCommand(_ => _ = OpenSessionAsync());
		m_LoadSampleCommand = new RelayCommand(_ => LoadSample());
	}

	public RelayCommand OpenSessionCommand => m_OpenSessionCommand;

	public RelayCommand LoadSampleCommand => m_LoadSampleCommand;

	public SessionSummaryViewModel Summary { get; }

	public ObservableCollection<FrameSample> FrameSamples
	{
		get => m_FrameSamples;
		private set => SetProperty(ref m_FrameSamples, value);
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

			ApplyPreview(m_PreviewService.LoadFile(path));
			StatusText = "Loaded " + path;
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private void LoadSample()
	{
		try
		{
			ApplyPreview(m_PreviewService.LoadRandomSample());
			StatusText = "Loaded generated sample data.";
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private void ApplyPreview(SessionPreview preview)
	{
		FrameSamples = preview.FrameSamples;
		Summary.FrameCount = preview.Summary.FrameCount;
		Summary.AverageFrameTimeMs = preview.Summary.AverageFrameTimeMs;
		Summary.MaxFrameTimeMs = preview.Summary.MaxFrameTimeMs;
		Summary.TargetFrameTimeMs = preview.Summary.TargetFrameTimeMs;
		Summary.FirstFrameIndex = preview.Summary.FirstFrameIndex;
		Summary.LastFrameIndex = preview.Summary.LastFrameIndex;
		Summary.SourceName = preview.SourceName;
		FooterText = preview.Summary.FrameCount + " frames";
	}
}
