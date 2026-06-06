using Avalonia;
using System;
using System.ComponentModel;
using SukiUI.Controls;
using ProfilerStudy.Avalonia.ProfilerStats;

namespace ProfilerStudy.Avalonia;

public sealed partial class MainWindow : SukiWindow
{
	private MainWindowViewModel m_ViewModel;
	private readonly PropertyChangedEventHandler m_ViewModelPropertyChanged;

	public MainWindow()
	{
		InitializeComponent();

		m_ViewModelPropertyChanged = OnViewModelPropertyChanged;
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
		}

		m_ViewModel = viewModel;
		m_ViewModel.PropertyChanged += m_ViewModelPropertyChanged;
		ProfilerStatsControl?.ApplyDocument(m_ViewModel.CurrentDocument);
	}

	private void OnUnloaded(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (m_ViewModel != null)
		{
			m_ViewModel.PropertyChanged -= m_ViewModelPropertyChanged;
			m_ViewModel = null;
		}
	}

	private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MainWindowViewModel.CurrentDocument))
		{
			ProfilerStatsControl?.ApplyDocument(m_ViewModel.CurrentDocument);
		}
	}
}
