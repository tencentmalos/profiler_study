using Avalonia;
using Avalonia.Controls;
using ProfilerStudy;
using ProfilerStudy.Avalonia.Timeline;
using System;

namespace ProfilerStudy.Avalonia.DeviceInfo;

public partial class ucDeviceInfo : UserControl
{
	private readonly ProfilerDeviceInfoController m_Controller;
	public ucDeviceInfo()
	{
		InitializeComponent();

		m_Controller = new ProfilerDeviceInfoController(this);
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
	}

	internal void ApplyDocument(SessionDocument document)
	{
		m_Controller.ApplyDocument(document);
	}

	public StackPanel DetailPlotControlsHost => DetailPlotControlsPanel;

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
}
