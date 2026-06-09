using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ProfilerStudy;
using ProfilerStudy.Avalonia.Timeline;
using System;

namespace ProfilerStudy.Avalonia.ProfilerStats;

public partial class ucProfilerStats : UserControl
{
	public static readonly StyledProperty<bool> IsCompactModeProperty =
		AvaloniaProperty.Register<ucProfilerStats, bool>(nameof(IsCompactMode));

	private Grid RootGrid;
	private StackPanel ControlsPanel;
	private CheckBox AutoFollowCheckBox;
	private CheckBox AutoScaleCheckBox;
	private CheckBox ShowDetailPlotsCheckBox;
	private Button ShowAllPlotsButton;
	private Button HideAllPlotsButton;
	private ComboBox PlotHeightComboBox;
	private StackPanel DetailPlotControlsPanel;
	internal ucTimelineScope TimelineScope;
	private readonly ProfilerStatsController m_Controller;

	internal event Action<int, int> ViewportChangedByUser;
	internal event Action<int> FrameSelectedByUser;
	internal event Action<int> FrameHoveredByUser;

	public ucProfilerStats()
	{
		InitializeComponent();

		m_Controller = new ProfilerStatsController(this);
		m_Controller.Initialize();

		AutoFollowCheckBox.IsChecked = true;
		AutoFollowCheckBox.IsCheckedChanged += (_, _) => m_Controller.SetAutoFollow(AutoFollowCheckBox.IsChecked ?? false);

		AutoScaleCheckBox.IsChecked = true;
		AutoScaleCheckBox.IsCheckedChanged += (_, _) => m_Controller.SetAutoScaleY(AutoScaleCheckBox.IsChecked ?? false);

		ShowDetailPlotsCheckBox.IsChecked = true;
		ShowDetailPlotsCheckBox.IsCheckedChanged += (_, _) => m_Controller.SetShowDetails(ShowDetailPlotsCheckBox.IsChecked ?? false);

		ShowAllPlotsButton.Click += (_, _) => m_Controller.ShowAllDetailPlots();
		HideAllPlotsButton.Click += (_, _) => m_Controller.HideAllDetailPlots();

		PlotHeightComboBox.SelectionChanged += PlotHeightComboBox_SelectionChanged;
		PlotHeightComboBox.SelectedIndex = 1;
		ApplyCompactMode(IsCompactMode);
	}

	public bool IsCompactMode
	{
		get => GetValue(IsCompactModeProperty);
		set => SetValue(IsCompactModeProperty, value);
	}

	internal void ApplyDocument(SessionDocument document)
	{
		m_Controller.ApplyDocument(document);
	}

	internal void ApplyViewport(TimelineViewport viewport)
	{
		m_Controller.ApplyViewport(viewport);
	}

	internal void ApplySelection(TimelineSelection selection)
	{
		m_Controller.ApplySelection(selection);
	}

	internal void ApplyTheme()
	{
		m_Controller.ApplyTheme();
	}

	internal void NotifyViewportChangedByUser(int startFrame, int endFrame)
	{
		ViewportChangedByUser?.Invoke(startFrame, endFrame);
	}

	internal void NotifyFrameSelectedByUser(int frameIndex)
	{
		FrameSelectedByUser?.Invoke(frameIndex);
	}

	internal void NotifyFrameHoveredByUser(int frameIndex)
	{
		FrameHoveredByUser?.Invoke(frameIndex);
	}

	public StackPanel DetailPlotControlsHost => DetailPlotControlsPanel;

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == IsCompactModeProperty)
		{
			ApplyCompactMode(change.GetNewValue<bool>());
		}
	}

	private void ApplyCompactMode(bool isCompact)
	{
		if (RootGrid == null || ControlsPanel == null || TimelineScope == null)
		{
			return;
		}

		RootGrid.RowDefinitions = isCompact
			? new RowDefinitions("0,*")
			: new RowDefinitions("Auto,*");
		ControlsPanel.IsVisible = !isCompact;
		TimelineScope.SetCompactTimelineMode(isCompact);
		m_Controller?.SetCompactMode(isCompact);
		if (isCompact)
		{
			m_Controller?.SetShowDetails(false);
		}
	}

	private void PlotHeightComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (PlotHeightComboBox.SelectedItem is ComboBoxItem item)
		{
			m_Controller.SetPlotHeight(item.Content?.ToString() switch
			{
				"Small" => PlotHeight.Small,
				"Large" => PlotHeight.Large,
				"Extra Large" => PlotHeight.ExtraLarge,
				_ => PlotHeight.Medium
			});
		}
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
		RootGrid = this.FindControl<Grid>(nameof(RootGrid));
		ControlsPanel = this.FindControl<StackPanel>(nameof(ControlsPanel));
		AutoFollowCheckBox = this.FindControl<CheckBox>(nameof(AutoFollowCheckBox));
		AutoScaleCheckBox = this.FindControl<CheckBox>(nameof(AutoScaleCheckBox));
		ShowDetailPlotsCheckBox = this.FindControl<CheckBox>(nameof(ShowDetailPlotsCheckBox));
		ShowAllPlotsButton = this.FindControl<Button>(nameof(ShowAllPlotsButton));
		HideAllPlotsButton = this.FindControl<Button>(nameof(HideAllPlotsButton));
		PlotHeightComboBox = this.FindControl<ComboBox>(nameof(PlotHeightComboBox));
		DetailPlotControlsPanel = this.FindControl<StackPanel>(nameof(DetailPlotControlsPanel));
		TimelineScope = this.FindControl<ucTimelineScope>(nameof(TimelineScope));
	}
}
