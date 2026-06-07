using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Windows.Input;
using System.ComponentModel;
using ProfilerStudy.Avalonia.ProfilerStats;
using SukiUI.Controls;

namespace ProfilerStudy.Avalonia;

public sealed partial class MainWindow : SukiWindow
{
	private MainWindowViewModel m_ViewModel;
	private TimelineViewport m_AttachedViewport;
	private TimelineSelection m_AttachedSelection;
	private readonly PropertyChangedEventHandler m_ViewModelPropertyChanged;
	private readonly PropertyChangedEventHandler m_ViewportPropertyChanged;
	private readonly PropertyChangedEventHandler m_SelectionPropertyChanged;
	private NativeMenuItem m_RecentFilesMenuItem;
	private NativeMenuItem m_ThreadsViewMenuItem;
	private NativeMenuItem m_CoresViewMenuItem;
	private NativeMenuItem m_ScopesViewMenuItem;
	private NativeMenuItem m_ThreadsInfoMenuItem;
	private NativeMenuItem m_ThreadsFrameGraphMenuItem;
	private NativeMenuItem m_ThreadsScopeGraphMenuItem;
	private NativeMenuItem m_ThreadsCoreGraphMenuItem;
	private NativeMenuItem m_ThreadsCustomStatsMenuItem;
	private NativeMenuItem m_ScopeColorThreadMenuItem;
	private NativeMenuItem m_ScopeColorScopeMenuItem;
	private NativeMenuItem m_OutputWindowMenuItem;
	private NativeMenuItem m_LightThemeMenuItem;
	private NativeMenuItem m_DarkThemeMenuItem;
	private readonly System.Collections.Generic.List<NativeMenuItem> m_ColorThemeMenuItems = new System.Collections.Generic.List<NativeMenuItem>();
	private bool m_IsApplyingProfilerStatsViewport;

	public MainWindow()
	{
		InitializeComponent();

		m_ViewModelPropertyChanged = OnViewModelPropertyChanged;
		m_ViewportPropertyChanged = OnViewportPropertyChanged;
		m_SelectionPropertyChanged = OnSelectionPropertyChanged;
		HookProfilerStatsControl(ProfilerStatsControl);
		HookProfilerStatsControl(ThreadsProfilerStatsControl);
		HookProfilerStatsControl(CoresProfilerStatsControl);
		HookProfilerStatsControl(ScopesProfilerStatsControl);
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
		ApplyNativeMenu(m_ViewModel);
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
			NativeMenu.SetMenu(this, null);
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
			ApplyProfilerStatsViewport(m_ViewModel?.Viewport);
		}
		else if (e.PropertyName == nameof(MainWindowViewModel.Selection))
		{
			AttachSelection(m_ViewModel?.Selection);
			ApplyProfilerStatsSelection(m_ViewModel?.Selection);
		}
		else if (e.PropertyName == nameof(MainWindowViewModel.RecentFiles))
		{
			UpdateRecentFilesNativeMenu();
		}
		else if (e.PropertyName == nameof(MainWindowViewModel.ThemeRevision))
		{
			ApplyVisualTheme();
		}

		if (IsNativeMenuStateProperty(e.PropertyName))
		{
			UpdateNativeMenuCheckedStates();
		}
	}

	private void ApplyNativeMenu(MainWindowViewModel viewModel)
	{
		if (viewModel == null)
		{
			NativeMenu.SetMenu(this, null);
			return;
		}

		var menu = new NativeMenu();
		menu.Add(SubMenu("_File",
			Item("_Open", viewModel.OpenSessionCommand, gesture: "Ctrl+O"),
			Item("_Save", viewModel.SaveSessionCommand, gesture: "Ctrl+S"),
			Item("Save _As...", viewModel.SaveSessionAsCommand),
			Item("_Close", viewModel.CloseSessionCommand),
			Item("Close A_ll", viewModel.CloseSessionCommand),
			Separator(),
			Item("_Export to CSV", viewModel.ExportFramesToCsvCommand),
			Item("Export _Frame Graph to CSV", viewModel.ExportFramesToCsvCommand),
			Separator(),
			CreateRecentFilesMenuItem(),
			Item("E_xit", viewModel.ExitCommand)));
		menu.Add(SubMenu("_View",
			m_ThreadsViewMenuItem = Item("_Threads View", viewModel.SelectSessionViewCommand, "Threads", toggleType: NativeMenuItemToggleType.Radio),
			m_CoresViewMenuItem = Item("_Cores View", viewModel.SelectSessionViewCommand, "Cores", toggleType: NativeMenuItemToggleType.Radio),
			m_ScopesViewMenuItem = Item("_Scopes View", viewModel.SelectSessionViewCommand, "Scopes", toggleType: NativeMenuItemToggleType.Radio),
			Separator(),
			SubMenu("_Threads View",
				m_ThreadsInfoMenuItem = Item("_Info", viewModel.ToggleThreadsPanelCommand, "Info", toggleType: NativeMenuItemToggleType.CheckBox),
				m_ThreadsFrameGraphMenuItem = Item("_Frame Graph", viewModel.ToggleThreadsPanelCommand, "FrameGraph", toggleType: NativeMenuItemToggleType.CheckBox),
				m_ThreadsScopeGraphMenuItem = Item("_Scope Graph", viewModel.ToggleThreadsPanelCommand, "ScopeGraph", toggleType: NativeMenuItemToggleType.CheckBox),
				m_ThreadsCoreGraphMenuItem = Item("_CPU Graph", viewModel.ToggleThreadsPanelCommand, "CoreGraph", toggleType: NativeMenuItemToggleType.CheckBox),
				m_ThreadsCustomStatsMenuItem = Item("_Custom Stats Graph", viewModel.ToggleThreadsPanelCommand, "CustomStats", toggleType: NativeMenuItemToggleType.CheckBox)),
			SubMenu("Scope _Colouring",
				m_ScopeColorThreadMenuItem = Item("Colour by _Thread", viewModel.SetScopeColorModeCommand, "Thread", toggleType: NativeMenuItemToggleType.Radio),
				m_ScopeColorScopeMenuItem = Item("Colour by _Scope", viewModel.SetScopeColorModeCommand, "Scope", toggleType: NativeMenuItemToggleType.Radio)),
			Separator(),
			m_OutputWindowMenuItem = Item("_Output Window", viewModel.ToggleOutputWindowCommand, toggleType: NativeMenuItemToggleType.CheckBox)));
		menu.Add(SubMenu("Theme",
			SubMenu("Base Skin",
				m_LightThemeMenuItem = Item("Light", viewModel.SetLightThemeCommand, toggleType: NativeMenuItemToggleType.Radio),
				m_DarkThemeMenuItem = Item("Dark", viewModel.SetDarkThemeCommand, toggleType: NativeMenuItemToggleType.Radio)),
			CreateColorThemeMenu(viewModel)));
		menu.Add(SubMenu("_Connection",
			Item("_New Connection...", viewModel.OpenConnectionCommand),
			Item("_Connect...", viewModel.OpenConnectionCommand),
			Item("_Disconnect", viewModel.ConnectionActionCommand, "Disconnect")));
		menu.Add(SubMenu("_Tools",
			Separator(),
			Item("_Find", viewModel.OpenFindScopeCommand, gesture: "Ctrl+F"),
			Separator(),
			Item("_Create Session from Selection", viewModel.CreateSessionFromSelectionCommand),
			Separator(),
			SubMenu("_Android",
				Item("_Start Recording Context Switches...", viewModel.AndroidActionCommand, "RecordContextSwitches"),
				Item("_Load Context Switch File", viewModel.AndroidActionCommand, "LoadContextSwitchFile")),
			Separator(),
			Item("_Settings", viewModel.OpenSettingsCommand)));
		menu.Add(SubMenu("_Help",
			Item("_Registration...", viewModel.OpenRegistrationCommand),
			Item("_Check for Updates", viewModel.OpenUpdateCheckCommand),
			Separator(),
			SubMenu("_Demo",
				Item("_Launch FramePro Game Simulator", viewModel.DemoActionCommand, "LaunchGameSimulator"),
				Item("_Playback Recording File...", viewModel.DemoActionCommand, "PlaybackRecordingFile")),
			Item("_Show Startup Page", viewModel.ShowStartupPageCommand),
			Separator(),
			Item("_About", viewModel.OpenAboutCommand)));

		NativeMenu.SetMenu(this, menu);
		UpdateRecentFilesNativeMenu();
		UpdateNativeMenuCheckedStates();
	}

	private NativeMenuItem CreateRecentFilesMenuItem()
	{
		m_RecentFilesMenuItem = SubMenu("_Recent Files");
		return m_RecentFilesMenuItem;
	}

	private void UpdateRecentFilesNativeMenu()
	{
		if (m_ViewModel == null || m_RecentFilesMenuItem?.Menu == null)
		{
			return;
		}

		m_RecentFilesMenuItem.Menu.Items.Clear();
		foreach (var recentFile in m_ViewModel.RecentFiles)
		{
			m_RecentFilesMenuItem.Menu.Add(Item(recentFile, m_ViewModel.OpenRecentSessionCommand, recentFile));
		}

		m_RecentFilesMenuItem.IsEnabled = m_RecentFilesMenuItem.Menu.Items.Count > 0;
	}

	private void UpdateNativeMenuCheckedStates()
	{
		if (m_ViewModel == null)
		{
			return;
		}

		SetChecked(m_ThreadsViewMenuItem, m_ViewModel.IsThreadsViewActive);
		SetChecked(m_CoresViewMenuItem, m_ViewModel.IsCoresViewActive);
		SetChecked(m_ScopesViewMenuItem, m_ViewModel.IsScopesViewActive);
		SetChecked(m_ThreadsInfoMenuItem, m_ViewModel.IsThreadsInfoPanelVisible);
		SetChecked(m_ThreadsFrameGraphMenuItem, m_ViewModel.IsThreadsFrameGraphVisible);
		SetChecked(m_ThreadsScopeGraphMenuItem, m_ViewModel.IsThreadsScopeGraphVisible);
		SetChecked(m_ThreadsCoreGraphMenuItem, m_ViewModel.IsThreadsCoreGraphVisible);
		SetChecked(m_ThreadsCustomStatsMenuItem, m_ViewModel.IsThreadsCustomStatsVisible);
		SetChecked(m_ScopeColorThreadMenuItem, m_ViewModel.IsScopeColorModeThread);
		SetChecked(m_ScopeColorScopeMenuItem, m_ViewModel.IsScopeColorModeScope);
		SetChecked(m_OutputWindowMenuItem, m_ViewModel.IsOutputWindowVisible);
		SetChecked(m_LightThemeMenuItem, m_ViewModel.IsLightThemeActive);
		SetChecked(m_DarkThemeMenuItem, m_ViewModel.IsDarkThemeActive);
		foreach (var item in m_ColorThemeMenuItems)
		{
			SetChecked(item, string.Equals(item.CommandParameter as string, m_ViewModel.ActiveColorThemeName, StringComparison.Ordinal));
		}
	}

	private static bool IsNativeMenuStateProperty(string propertyName)
	{
		return propertyName == nameof(MainWindowViewModel.IsThreadsViewActive) ||
			propertyName == nameof(MainWindowViewModel.IsCoresViewActive) ||
			propertyName == nameof(MainWindowViewModel.IsScopesViewActive) ||
			propertyName == nameof(MainWindowViewModel.IsThreadsInfoPanelVisible) ||
			propertyName == nameof(MainWindowViewModel.IsThreadsFrameGraphVisible) ||
			propertyName == nameof(MainWindowViewModel.IsThreadsScopeGraphVisible) ||
			propertyName == nameof(MainWindowViewModel.IsThreadsCoreGraphVisible) ||
			propertyName == nameof(MainWindowViewModel.IsThreadsCustomStatsVisible) ||
			propertyName == nameof(MainWindowViewModel.IsScopeColorModeThread) ||
			propertyName == nameof(MainWindowViewModel.IsScopeColorModeScope) ||
			propertyName == nameof(MainWindowViewModel.IsOutputWindowVisible) ||
			propertyName == nameof(MainWindowViewModel.IsLightThemeActive) ||
			propertyName == nameof(MainWindowViewModel.IsDarkThemeActive) ||
			propertyName == nameof(MainWindowViewModel.ActiveColorThemeName) ||
			propertyName == nameof(MainWindowViewModel.ThemeRevision);
	}

	private NativeMenuItem CreateColorThemeMenu(MainWindowViewModel viewModel)
	{
		m_ColorThemeMenuItems.Clear();
		var colorMenu = SubMenu("Color Theme");
		foreach (var theme in viewModel.ThemeColorOptions)
		{
			var item = Item(theme.Name, viewModel.SetThemeColorCommand, theme.Name, toggleType: NativeMenuItemToggleType.Radio);
			m_ColorThemeMenuItems.Add(item);
			colorMenu.Menu.Add(item);
		}

		return colorMenu;
	}

	private static NativeMenuItem SubMenu(string header, params NativeMenuItemBase[] children)
	{
		var item = new NativeMenuItem { Header = header, Menu = new NativeMenu() };
		foreach (var child in children)
		{
			item.Menu.Add(child);
		}

		return item;
	}

	private static NativeMenuItem Item(string header, ICommand command = null, object commandParameter = null, string gesture = null, NativeMenuItemToggleType toggleType = NativeMenuItemToggleType.None)
	{
		var item = new NativeMenuItem
		{
			Header = header,
			Command = command,
			CommandParameter = commandParameter,
			ToggleType = toggleType
		};

		if (!string.IsNullOrWhiteSpace(gesture))
		{
			item.Gesture = KeyGesture.Parse(gesture);
		}

		return item;
	}

	private static NativeMenuItemSeparator Separator()
	{
		return new NativeMenuItemSeparator();
	}

	private static void SetChecked(NativeMenuItem item, bool isChecked)
	{
		if (item != null)
		{
			item.IsChecked = isChecked;
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
				ApplyProfilerStatsViewport(m_ViewModel?.Viewport);
			}
		}
	}

	private void OnSelectionPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(TimelineSelection.SelectedFrameIndex) ||
			e.PropertyName == nameof(TimelineSelection.SelectedFrameTimeMs))
		{
			ApplyProfilerStatsSelection(m_ViewModel?.Selection);
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
		ApplyProfilerStatsDocument(m_ViewModel?.CurrentDocument);
		ApplyProfilerStatsViewport(m_ViewModel?.Viewport);
		ApplyProfilerStatsSelection(m_ViewModel?.Selection);
	}

	private void HookProfilerStatsControl(ucProfilerStats control)
	{
		if (control == null)
		{
			return;
		}

		control.ViewportChangedByUser += OnProfilerStatsViewportChangedByUser;
		control.FrameSelectedByUser += OnProfilerStatsFrameSelectedByUser;
		control.FrameHoveredByUser += OnProfilerStatsFrameHoveredByUser;
	}

	private void ApplyProfilerStatsDocument(SessionDocument document)
	{
		ProfilerStatsControl?.ApplyDocument(document);
		ThreadsProfilerStatsControl?.ApplyDocument(document);
		CoresProfilerStatsControl?.ApplyDocument(document);
		ScopesProfilerStatsControl?.ApplyDocument(document);
	}

	private void ApplyProfilerStatsViewport(TimelineViewport viewport)
	{
		ProfilerStatsControl?.ApplyViewport(viewport);
		ThreadsProfilerStatsControl?.ApplyViewport(viewport);
		CoresProfilerStatsControl?.ApplyViewport(viewport);
		ScopesProfilerStatsControl?.ApplyViewport(viewport);
	}

	private void ApplyProfilerStatsSelection(TimelineSelection selection)
	{
		ProfilerStatsControl?.ApplySelection(selection);
		ThreadsProfilerStatsControl?.ApplySelection(selection);
		CoresProfilerStatsControl?.ApplySelection(selection);
		ScopesProfilerStatsControl?.ApplySelection(selection);
	}

	private void ApplyVisualTheme()
	{
		ProfilerStatsControl?.ApplyTheme();
		ThreadsProfilerStatsControl?.ApplyTheme();
		CoresProfilerStatsControl?.ApplyTheme();
		ScopesProfilerStatsControl?.ApplyTheme();
		InvalidateVisual();
	}
}
