using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

public sealed partial class MainWindow : Window
{
	private MainWindowViewModel m_ViewModel;
	private TimelineViewport m_AttachedViewport;
	private TimelineSelection m_AttachedSelection;
	private readonly PropertyChangedEventHandler m_ViewModelPropertyChanged;
	private readonly PropertyChangedEventHandler m_ViewportPropertyChanged;
	private readonly PropertyChangedEventHandler m_SelectionPropertyChanged;
	private bool m_IsApplyingProfilerStatsViewport;

	public MainWindow()
	{
		InitializeComponent();

		m_ViewModelPropertyChanged = OnViewModelPropertyChanged;
		m_ViewportPropertyChanged = OnViewportPropertyChanged;
		m_SelectionPropertyChanged = OnSelectionPropertyChanged;
		ProfilerStatsControl.ViewportChangedByUser += OnProfilerStatsViewportChangedByUser;
		ProfilerStatsControl.FrameSelectedByUser += OnProfilerStatsFrameSelectedByUser;
		ProfilerStatsControl.FrameHoveredByUser += OnProfilerStatsFrameHoveredByUser;
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
			AttachSelection(null);
		}

		m_ViewModel = viewModel;
		m_ViewModel.PropertyChanged += m_ViewModelPropertyChanged;
		AttachViewport(m_ViewModel.Viewport);
		AttachSelection(m_ViewModel.Selection);
		ApplyProfilerStatsDocumentAndViewport();
	}

	private void OnUnloaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (m_ViewModel != null)
		{
			m_ViewModel.PropertyChanged -= m_ViewModelPropertyChanged;
			AttachViewport(null);
			AttachSelection(null);
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
		else if (e.PropertyName == nameof(MainWindowViewModel.Selection))
		{
			AttachSelection(m_ViewModel?.Selection);
			ProfilerStatsControl?.ApplySelection(m_ViewModel?.Selection);
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

	private void AttachSelection(TimelineSelection selection)
	{
		if (ReferenceEquals(m_AttachedSelection, selection))
		{
			return;
		}

		if (m_AttachedSelection != null)
		{
			m_AttachedSelection.PropertyChanged -= m_SelectionPropertyChanged;
		}

		m_AttachedSelection = selection;
		if (m_AttachedSelection != null)
		{
			m_AttachedSelection.PropertyChanged += m_SelectionPropertyChanged;
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

	private void OnSelectionPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex) ||
			e.PropertyName == nameof(TimelineSelection.SelectedFrameTimeMs))
		{
			ProfilerStatsControl?.ApplySelection(m_ViewModel?.Selection);
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

	private void OnProfilerStatsFrameSelectedByUser(int frameIndex)
	{
		m_ViewModel?.SelectFrameFromProfilerStats(frameIndex);
	}

	private void OnProfilerStatsFrameHoveredByUser(int frameIndex)
	{
		m_ViewModel?.HoverFrameFromProfilerStats(frameIndex);
	}

	private void ApplyProfilerStatsDocumentAndViewport()
	{
		ProfilerStatsControl?.ApplyDocument(m_ViewModel?.CurrentDocument);
		ProfilerStatsControl?.ApplyViewport(m_ViewModel?.Viewport);
		ProfilerStatsControl?.ApplySelection(m_ViewModel?.Selection);
	}
}
