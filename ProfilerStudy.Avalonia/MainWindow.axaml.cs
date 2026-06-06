using Avalonia;
using System;
using System.ComponentModel;
using SukiUI.Controls;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

public sealed partial class MainWindow : SukiWindow
{
	private MainWindowViewModel m_ViewModel;
	private TimelineViewport m_AttachedViewport;
	private readonly PropertyChangedEventHandler m_ViewModelPropertyChanged;
	private readonly PropertyChangedEventHandler m_ViewportPropertyChanged;
	private bool m_IsApplyingProfilerStatsViewport;

	public MainWindow()
	{
		InitializeComponent();

		m_ViewModelPropertyChanged = OnViewModelPropertyChanged;
		m_ViewportPropertyChanged = OnViewportPropertyChanged;
		ProfilerStatsControl.ViewportChangedByUser += OnProfilerStatsViewportChangedByUser;
		DataContextChanged += OnDataContextChanged;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel)
		{
			AttachViewModel(viewModel);
		}
	}

	private void OnDataContextChanged(object sender, EventArgs e)
	{
		if (DataContext is MainWindowViewModel viewModel)
		{
			AttachViewModel(viewModel);
		}
	}

	private void AttachViewModel(MainWindowViewModel viewModel)
	{
		if (ReferenceEquals(m_ViewModel, viewModel))
		{
			return;
		}

		if (m_ViewModel != null)
		{
			m_ViewModel.PropertyChanged -= m_ViewModelPropertyChanged;
			AttachViewport(null);
		}

		m_ViewModel = viewModel;
		m_ViewModel.PropertyChanged += m_ViewModelPropertyChanged;
		AttachViewport(m_ViewModel.Viewport);
		ApplyProfilerStatsDocumentAndViewport();
	}

	private void OnUnloaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (m_ViewModel != null)
		{
			m_ViewModel.PropertyChanged -= m_ViewModelPropertyChanged;
			AttachViewport(null);
			m_ViewModel = null;
		}
	}

	private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MainWindowViewModel.CurrentDocument))
		{
			ApplyProfilerStatsDocumentAndViewport();
		}
		else if (e.PropertyName == nameof(MainWindowViewModel.Viewport))
		{
			AttachViewport(m_ViewModel?.Viewport);
			ProfilerStatsControl?.ApplyViewport(m_ViewModel?.Viewport);
		}
	}

	private void AttachViewport(TimelineViewport viewport)
	{
		if (ReferenceEquals(m_AttachedViewport, viewport))
		{
			return;
		}

		if (m_AttachedViewport != null)
		{
			m_AttachedViewport.PropertyChanged -= m_ViewportPropertyChanged;
		}

		m_AttachedViewport = viewport;
		if (m_AttachedViewport != null)
		{
			m_AttachedViewport.PropertyChanged += m_ViewportPropertyChanged;
		}
	}

	private void OnViewportPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineViewport.StartFrame) ||
			e.PropertyName == nameof(TimelineViewport.EndFrame))
		{
			if (m_IsApplyingProfilerStatsViewport is false)
			{
				ProfilerStatsControl?.ApplyViewport(m_ViewModel?.Viewport);
			}
		}
	}

	private void OnProfilerStatsViewportChangedByUser(int startFrame, int endFrame)
	{
		if (m_ViewModel?.Viewport == null)
		{
			return;
		}

		m_IsApplyingProfilerStatsViewport = true;
		try
		{
			m_ViewModel.Viewport.SetRange(startFrame, endFrame);
		}
		finally
		{
			m_IsApplyingProfilerStatsViewport = false;
		}
	}

	private void ApplyProfilerStatsDocumentAndViewport()
	{
		ProfilerStatsControl?.ApplyDocument(m_ViewModel?.CurrentDocument);
		ProfilerStatsControl?.ApplyViewport(m_ViewModel?.Viewport);
	}
}
