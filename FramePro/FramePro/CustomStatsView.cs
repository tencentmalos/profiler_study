using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Editor;
using FramePro.Properties;
using SCL;
using SCLCoreCLR;

namespace FramePro;

internal class CustomStatsView : SessionView
{
	private const int m_ValueColumnWidth = 130;

	private const int m_MaxScopesToGraphByDefault = 10;

	private const int m_DefaultGraphWindowHeight = 250;

	private Session m_Session;

	private Settings m_Settings;

	private CustomStatXAxisMode m_XAxisMode;

	private List<LineGraphWindow> m_GraphWindows = new List<LineGraphWindow>();

	private Column m_NameColumn;

	private Column m_MinValueColumn;

	private Column m_MaxValueColumn;

	private Column m_MinCountColumn;

	private Column m_MaxCountColumn;

	private Column m_AverageValueColumn;

	private Column m_AverageCountColumm;

	private Column m_GraphNameColumn;

	private Column m_UnitColumn;

	private Column m_XAxisModeColumn;

	private Column m_GraphColumn;

	private Column m_ColourColumn;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private const int m_UpdateFrequency = 1000;

	private int m_LastUpdateTime;

	private bool m_SessionIsReady;

	private List<string> m_GraphedStats = new List<string>();

	private const double m_MinYScaleAverageChangedPercent = 0.3;

	private const int m_MaxDataGridControlCount = 1000;

	private int m_DataGridControlCount;

	private bool m_LoggedTooManyControlsWarning;

	private bool m_CustomStatInfoChanged;

	private int m_CustomStatInfoChangedLastTime;

	private const int m_CustomStatInfoChangedMinInterval = 1000;

	private float m_DPIScale;

	private bool m_IgnoreDataGridCellChanged;

	private IContainer components;

	private HDataGrid m_DataGrid;

	private Panel panel1;

	private System.Windows.Forms.Button button3;

	private System.Windows.Forms.Button button2;

	private Editor.Button button1;

	private Label label1;

	private TextBox m_FilterTextBox;

	private Splitter splitter2;

	private ScrollPanel m_GraphsPanel;

	private Label label2;

	private CheckButton m_XAxisTimeButton;

	private CheckButton m_XAxisFrameButton;

	private System.Windows.Forms.Button button4;

	public override string ViewName => "Custom Stats";

	public override Session Session => m_Session;

	private int GraphFlagCellIndex => m_DataGrid.IndexOf(m_GraphColumn);

	public CustomStatsView(Session session, Settings settings)
	{
		m_Session = session;
		m_Settings = settings;
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		if (settings.CustomStatsDataGridHeight != -1)
		{
			m_DataGrid.Size = new Size(m_DataGrid.Width, settings.CustomStatsDataGridHeight);
		}
		m_XAxisMode = settings.ScopesViewXAxisMode;
		UpdateXAxisButtonStates();
		InitialiseDataGrid();
		if (m_Settings.UserSetScopesViewGraphFlags)
		{
			ReadGraphedStatsFromSettings();
		}
		HookSession();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void ReadGraphedStatsFromSettings()
	{
		m_GraphedStats = new List<string>(m_Settings.CustomStatsViewGraphedStats);
	}

	private void WriteGraphedStatsToSettings()
	{
		m_Settings.CustomStatsViewGraphedStats = new List<string>(m_GraphedStats);
		m_Settings.Write();
	}

	private LineGraphWindow CreateGraphWindow(string graph, string unit)
	{
		LineGraphWindow lineGraphWindow = new LineGraphWindow(graph, unit, m_Session, m_Settings, GetGraphValuesPerFrame, GetGraphValuesPerSec);
		lineGraphWindow.Size = new Size(base.ClientSize.Width, 250);
		lineGraphWindow.Dock = DockStyle.Top;
		lineGraphWindow.StopTrackingEnd += base.StopTrackingEnd;
		lineGraphWindow.TimeRangeChanged += GraphWindowTimeRangeChanged;
		lineGraphWindow.FrameGraphWindowClose += FrameGraphWindowClose;
		m_GraphWindows.Add(lineGraphWindow);
		Splitter splitter = new Splitter();
		splitter.Dock = DockStyle.Top;
		m_GraphsPanel.Controls.Add(splitter);
		m_GraphsPanel.Controls.Add(lineGraphWindow);
		lineGraphWindow.BringToFront();
		splitter.BringToFront();
		if (!m_Session.Connected)
		{
			lineGraphWindow.ResetXAxisZoom();
		}
		return lineGraphWindow;
	}

	private void FrameGraphWindowClose(LineGraphWindow sender)
	{
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row childRow in row.ChildRows)
			{
				string name = (string)childRow.Cells[0].Value;
				long customStatNameId = m_Session.GetCustomStatNameId(name);
				string item = (string)childRow.Cells[0].Value;
				if (m_Session.GetCustomStatGraph(customStatNameId) == sender.Graph && m_GraphedStats.Contains(item))
				{
					childRow.Cells[GraphFlagCellIndex].Value = false;
					m_GraphedStats.Remove(item);
				}
			}
		}
		UpdateParentRowGraphedFlags();
		m_DataGrid.RefreshDataGrid();
		m_Settings.UserSetScopesViewGraphFlags = true;
		UpdateGraphWindows();
		WriteGraphedStatsToSettings();
	}

	private void UpdateParentRowGraphedFlags()
	{
		m_IgnoreDataGridCellChanged = true;
		foreach (Row row in m_DataGrid.Rows)
		{
			bool flag = true;
			foreach (Row childRow in row.ChildRows)
			{
				if (!(bool)childRow.Cells[GraphFlagCellIndex].Value)
				{
					flag = false;
					break;
				}
			}
			row.Cells[GraphFlagCellIndex].Value = flag;
		}
		m_IgnoreDataGridCellChanged = false;
	}

	private void GraphWindowTimeRangeChanged(Control sender, long start_time, long end_time, double scale)
	{
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			if (graphWindow != sender)
			{
				graphWindow.SetTimeRange(start_time, scale);
			}
		}
	}

	private void RemoveGraphWindow(LineGraphWindow graph_window)
	{
		int num = m_GraphsPanel.Controls.IndexOf(graph_window);
		int index = num - 1;
		m_GraphsPanel.Controls.RemoveAt(index);
		m_GraphsPanel.Controls.RemoveAt(num - 1);
		graph_window.Dispose();
		m_GraphWindows.Remove(graph_window);
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private bool IsAccumulated(long custom_stat_name)
	{
		string @string = m_Session.GetString(custom_stat_name);
		CustomStatXAxisMode xAxisMode = GetXAxisMode(@string);
		if (xAxisMode != CustomStatXAxisMode.AccFrame)
		{
			return xAxisMode == CustomStatXAxisMode.AccTime;
		}
		return true;
	}

	private void GetGraphValuesPerSec(long graph_name, long start_time, long end_time, List<PerSecValue> values, out long first_interval_time)
	{
		bool acc = IsAccumulated(graph_name);
		m_Session.GetCustomStatsPerSec(graph_name, start_time, end_time, acc, values, out first_interval_time);
	}

	private void GetGraphValuesPerFrame(int start_frame_index, int end_frame_index, long graph_name, List<FrameValue> values)
	{
		bool acc = IsAccumulated(graph_name);
		m_Session.GetCustomStats(start_frame_index, end_frame_index, graph_name, acc, values);
	}

	public override void OnTargetFrameTimeChanged()
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void HookSession()
	{
		m_Session.SessionIsReady += OnSessionIsReady;
		m_Session.Disconnected += SessionDisconnected;
		m_Session.ReceivedCustomStatStringPacket += ReceivedCustomStatStringPacket;
		m_Session.TimerNameAdded += SessionTimerNameAdded;
		m_Session.FinishedProcessingPackets += SessionFinishedProcessingPAckets;
	}

	private void SessionFinishedProcessingPAckets()
	{
		m_ControlTaskDispatcher.QueueTask(SessionFinishedProcessingPAckets_Main);
	}

	private void SessionFinishedProcessingPAckets_Main()
	{
		UpdateDataGrid();
		UpdateGraphWindows();
	}

	private void SessionTimerNameAdded(long name_id, string name)
	{
		m_ControlTaskDispatcher.QueueTask(SessionTimerNameAdded_Main);
	}

	private void SessionTimerNameAdded_Main()
	{
		UpdateGraphWindows();
	}

	private void ReceivedCustomStatStringPacket()
	{
		m_ControlTaskDispatcher.QueueTask(RecehvedCustomStatStringPacket_Main);
	}

	private void RecehvedCustomStatStringPacket_Main()
	{
		UpdateDataGrid();
		UpdateGraphWindows();
	}

	private void SessionDisconnected()
	{
		m_ControlTaskDispatcher.QueueTask(SessionDisconnected_Main);
	}

	private void SessionDisconnected_Main()
	{
		UpdateDataGrid();
		HandleCustomStatInfoChanged(force: true);
	}

	private void OnSessionIsReady()
	{
		m_ControlTaskDispatcher.QueueTask(OnSessionIsReady_Main);
	}

	private void OnSessionIsReady_Main()
	{
		m_SessionIsReady = true;
		UpdateDataGrid();
		m_DataGrid.RefreshDataGrid();
		if (!m_Settings.UserSetScopesViewGraphFlags)
		{
			SetDefaultGraphedFlags();
		}
		UpdateGraphWindows();
		RefreshGraphs();
	}

	private void InitialiseDataGrid()
	{
		m_NameColumn = new Column("Name");
		m_NameColumn.WidthMode = Column.EWidthMode.Fill;
		m_NameColumn.ReadOnly = true;
		m_DataGrid.Add(m_NameColumn);
		m_MinValueColumn = new Column("Min Value");
		m_MinValueColumn.Width = ScaleDPI(130);
		m_MinValueColumn.ReadOnly = true;
		m_DataGrid.Add(m_MinValueColumn);
		m_MaxValueColumn = new Column("Max Value");
		m_MaxValueColumn.Width = ScaleDPI(130);
		m_MaxValueColumn.ReadOnly = true;
		m_DataGrid.Add(m_MaxValueColumn);
		m_MinCountColumn = new Column("Min Count");
		m_MinCountColumn.Width = ScaleDPI(130);
		m_MinCountColumn.ReadOnly = true;
		m_DataGrid.Add(m_MinCountColumn);
		m_MaxCountColumn = new Column("Max Count");
		m_MaxCountColumn.Width = ScaleDPI(130);
		m_MaxCountColumn.ReadOnly = true;
		m_DataGrid.Add(m_MaxCountColumn);
		m_AverageValueColumn = new Column("Average Value");
		m_AverageValueColumn.Width = ScaleDPI(130);
		m_AverageValueColumn.ReadOnly = true;
		m_AverageValueColumn.SortMode = Column.ESortMode.Decreasing;
		m_DataGrid.Add(m_AverageValueColumn);
		m_AverageCountColumm = new Column("Average Count");
		m_AverageCountColumm.Width = ScaleDPI(130);
		m_AverageCountColumm.ReadOnly = true;
		m_DataGrid.Add(m_AverageCountColumm);
		m_UnitColumn = new Column("Unit");
		m_UnitColumn.Width = ScaleDPI(85);
		m_UnitColumn.ReadOnly = true;
		m_DataGrid.Add(m_UnitColumn);
		m_XAxisModeColumn = new Column();
		m_XAxisModeColumn.Width = ScaleDPI(85);
		m_DataGrid.Add(m_XAxisModeColumn);
		m_GraphNameColumn = new Column("Graph");
		m_GraphNameColumn.Width = ScaleDPI(70);
		m_GraphNameColumn.ReadOnly = true;
		m_DataGrid.Add(m_GraphNameColumn);
		m_ColourColumn = new Column("Colour");
		m_ColourColumn.Width = ScaleDPI(40);
		m_ColourColumn.ReadOnly = true;
		m_DataGrid.Add(m_ColourColumn);
		m_GraphColumn = new Column("Graph");
		m_GraphColumn.Width = ScaleDPI(70);
		m_GraphColumn.BoolTrueImage = Utils.To96Dpi(Resources.tick);
		m_DataGrid.Add(m_GraphColumn);
		Column column = null;
		foreach (Column column2 in m_DataGrid.Columns)
		{
			if (column2.Name == m_Settings.CustomStatsSortedColumn)
			{
				column = column2;
				break;
			}
		}
		if (column != null)
		{
			column.SortMode = (m_Settings.CustomStatsSortedColumnIncreasing ? Column.ESortMode.Increasing : Column.ESortMode.Decreasing);
			m_DataGrid.Sort(column);
		}
	}

	public override void UpdateView()
	{
		int tickCount = Environment.TickCount;
		if (m_Session.Connected && base.Active)
		{
			HandleCustomStatInfoChanged(force: false);
			if (tickCount - m_LastUpdateTime > 1000 && base.TrackEnd)
			{
				UpdateDataGrid();
				if (!m_Settings.UserSetScopesViewGraphFlags)
				{
					SetDefaultGraphedFlags();
				}
				m_LastUpdateTime = tickCount;
			}
			if (base.TrackEnd)
			{
				GraphsGotoEnd();
			}
			RefreshGraphs();
		}
		base.UpdateView();
	}

	private void HandleCustomStatInfoChanged(bool force)
	{
		if (force || (m_CustomStatInfoChanged && Environment.TickCount - m_CustomStatInfoChangedLastTime > 1000))
		{
			UpdateDataGrid();
			UpdateGraphWindows();
			m_CustomStatInfoChanged = false;
			m_CustomStatInfoChangedLastTime = Environment.TickCount;
		}
	}

	private void GraphsGotoEnd()
	{
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			graphWindow.GotoEnd();
		}
	}

	private void RefreshGraphs()
	{
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			if (!graphWindow.ChangedHeight)
			{
				UpdateGraphWindowYScale(graphWindow);
			}
			graphWindow.RefreshGraph();
		}
	}

	private void UpdateGraphWindowYScale(LineGraphWindow graph_window)
	{
		long timerFrequency = m_Session.TimerFrequency;
		double num = double.MinValue;
		foreach (Graph graph in graph_window.Graphs)
		{
			bool flag = IsAccumulated(graph.m_NameId);
			double min_value_per_sec;
			double max_value_per_sec;
			switch (graph.m_XAxisMode)
			{
			case XAxisMode.Frame:
				if (flag)
				{
					min_value_per_sec = m_Session.GetCustomStatMinAccPerFrame(graph.m_NameId);
					max_value_per_sec = m_Session.GetCustomStatMaxAccPerFrame(graph.m_NameId);
				}
				else
				{
					min_value_per_sec = m_Session.GetCustomStatMinPerFrame(graph.m_NameId);
					max_value_per_sec = m_Session.GetCustomStatMaxPerFrame(graph.m_NameId);
				}
				break;
			case XAxisMode.Time:
			{
				double min_count_per_sec;
				double max_count_per_sec;
				if (flag)
				{
					m_Session.GetCustomStatMinMaxAccValuesPerSec(graph.m_NameId, out min_value_per_sec, out max_value_per_sec, out min_count_per_sec, out max_count_per_sec);
				}
				else
				{
					m_Session.GetCustomStatMinMaxValuesPerSec(graph.m_NameId, out min_value_per_sec, out max_value_per_sec, out min_count_per_sec, out max_count_per_sec);
				}
				break;
			}
			default:
				min_value_per_sec = 0.0;
				max_value_per_sec = 1.0;
				break;
			}
			min_value_per_sec = Math.Abs(min_value_per_sec);
			max_value_per_sec = Math.Abs(max_value_per_sec);
			double num2 = Math.Max(min_value_per_sec, max_value_per_sec);
			if (graph_window.Unit.ToLower() == "cycles")
			{
				num2 = num2 * 1000.0 / (double)timerFrequency;
			}
			if (num2 > num)
			{
				num = num2;
			}
		}
		double num3 = num;
		double num4 = (double)graph_window.GraphHeight * 0.9 / num3;
		double num5 = graph_window.YScale / num4;
		if (num5 < 1.0)
		{
			num5 = 1.0 - num5;
		}
		num5 = Math.Abs(num5);
		if (num5 > 0.3)
		{
			graph_window.YScale = num4;
		}
	}

	private bool FilterIn(string name)
	{
		string value = m_FilterTextBox.Text.ToLower().Trim();
		if (string.IsNullOrEmpty(value))
		{
			return true;
		}
		return name.ToLower().Contains(value);
	}

	private CustomStatXAxisMode GetXAxisMode(string custom_stat_name)
	{
		return m_Settings.CoreSettings.GetCustomStatInfo(custom_stat_name).m_XAxisMode;
	}

	private Row GetOrCreateGraphRow(string graph_name)
	{
		foreach (Row row in m_DataGrid.Rows)
		{
			if ((string)row.Cells[0].Value == graph_name)
			{
				return row;
			}
		}
		Row result = new Row(graph_name);
		m_DataGrid.Rows.Add(graph_name, "", "", "", "", "", "", "", "", "", "", false);
		return result;
	}

	private void UpdateDataGrid()
	{
		List<CustomStatSessionData> customStats = m_Session.GetCustomStats();
		long timerFrequency = m_Session.TimerFrequency;
		_ = (double)(m_Session.LastFrameEndTime - m_Session.FirstFrameTime) / (double)timerFrequency;
		int frameCount = m_Session.FrameCount;
		double session_time_in_sec = (double)(m_Session.LastFrameEndTime - m_Session.FirstFrameTime) / (double)timerFrequency;
		int num = 0;
		foreach (CustomStatSessionData item in customStats)
		{
			string @string = m_Session.GetString(item.Name);
			string customStatGraph = m_Session.GetCustomStatGraph(item.Name);
			string unit = m_Session.GetCustomStatUnit(item.Name);
			if (!FilterIn(@string))
			{
				continue;
			}
			double average_value = 0.0;
			double average_count = 0.0;
			double min_value = 0.0;
			double max_value = 0.0;
			double min_count = 0.0;
			double max_count = 0.0;
			switch (m_Settings.CoreSettings.GetCustomStatInfo(@string).m_XAxisMode)
			{
			case CustomStatXAxisMode.Frame:
				GetDataGridValuesPerFrame(item, out average_value, out average_count, out min_value, out max_value, out min_count, out max_count, frameCount);
				break;
			case CustomStatXAxisMode.Time:
				GetDataGridValuesPerSecond(item, out average_value, out average_count, out min_value, out max_value, out min_count, out max_count, session_time_in_sec);
				break;
			case CustomStatXAxisMode.AccFrame:
				GetDataGridValuesAccPerFrame(item, out average_value, out average_count, out min_value, out max_value, out min_count, out max_count, frameCount);
				break;
			case CustomStatXAxisMode.AccTime:
				GetDataGridValuesAccPerSecond(item, out average_value, out average_count, out min_value, out max_value, out min_count, out max_count, session_time_in_sec);
				break;
			}
			if (unit.ToLower() == "cycles")
			{
				average_value = average_value * 1000.0 / (double)timerFrequency;
				min_value = min_value * 1000.0 / (double)timerFrequency;
				max_value = max_value * 1000.0 / (double)timerFrequency;
				unit = "ms";
			}
			else if (unit.ToLower() == "bytes")
			{
				PickBestMemoryUnit(ref average_value, ref min_value, ref max_value, ref unit);
			}
			average_value = Utils.TruncateDouble(average_value);
			average_count = Utils.TruncateDouble(average_count);
			min_value = Utils.TruncateDouble(min_value);
			max_value = Utils.TruncateDouble(max_value);
			min_count = Utils.TruncateDouble(min_count);
			max_count = Utils.TruncateDouble(max_count);
			Color customStatColour = m_Session.GetCustomStatColour(item.Name);
			Row row = FindRow(@string);
			if (row != null)
			{
				row.Cells[1].Value = min_value;
				row.Cells[2].Value = max_value;
				row.Cells[3].Value = min_count;
				row.Cells[4].Value = max_count;
				row.Cells[5].Value = average_value;
				row.Cells[6].Value = average_count;
				row.Cells[7].Value = unit;
				row.Cells[9].Value = customStatGraph;
				row.Cells[10].Value = customStatColour;
			}
			else
			{
				Row orCreateGraphRow = GetOrCreateGraphRow(customStatGraph);
				object obj = null;
				if (m_DataGridControlCount < 1000)
				{
					obj = new XAxisDropDownListControl
					{
						Value = m_Settings.CoreSettings.GetCustomStatInfo(@string).m_XAxisMode
					};
					m_DataGridControlCount++;
				}
				else
				{
					obj = "";
					if (!m_LoggedTooManyControlsWarning)
					{
						MainForm.Inst.LogLine("WARNING: too many custom scope types. Not creating x-axis controls in the datagrid");
						m_LoggedTooManyControlsWarning = true;
					}
				}
				orCreateGraphRow.ChildRows.Add(@string, min_value, max_value, min_count, max_count, average_value, average_count, unit, obj, customStatGraph, customStatColour, m_GraphedStats.Contains(@string));
			}
			num++;
		}
		List<Row> list = new List<Row>();
		foreach (Row row2 in m_DataGrid.Rows)
		{
			foreach (Row childRow in row2.ChildRows)
			{
				string text = (string)childRow.Cells[0].Value;
				bool flag = false;
				foreach (CustomStatSessionData item2 in customStats)
				{
					string string2 = m_Session.GetString(item2.Name);
					if (string2 == text && FilterIn(string2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(childRow);
				}
			}
		}
		foreach (Row item3 in list)
		{
			item3.Parent.ChildRows.Remove(item3);
		}
		list.Clear();
		foreach (Row row3 in m_DataGrid.Rows)
		{
			if (row3.ChildRows.Count == 0)
			{
				list.Add(row3);
			}
		}
		foreach (Row item4 in list)
		{
			m_DataGrid.Rows.Remove(item4);
		}
		UpdateParentRowGraphedFlags();
		m_DataGrid.RefreshDataGrid();
		m_DataGrid.ResortColumn();
		m_DataGrid.Refresh();
	}

	private Row FindRow(string name)
	{
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row childRow in row.ChildRows)
			{
				if ((string)childRow.Cells[0].Value == name)
				{
					return childRow;
				}
			}
		}
		return null;
	}

	private void GetDataGridValuesPerFrame(CustomStatSessionData custom_stat, out double average_value, out double average_count, out double min_value, out double max_value, out double min_count, out double max_count, int frame_count)
	{
		switch (custom_stat.ValueType)
		{
		case CustomStatValueType.Int64:
			average_value = (double)custom_stat.m_TotalValueInt64 / (double)frame_count;
			min_value = custom_stat.m_MinValuePerFrameInt64;
			max_value = custom_stat.m_MaxValuePerFrameInt64;
			break;
		case CustomStatValueType.Double:
			average_value = custom_stat.m_TotalValueDouble / (double)frame_count;
			min_value = custom_stat.m_MinValuePerFrameDouble;
			max_value = custom_stat.m_MaxValuePerFrameDouble;
			break;
		default:
			average_value = 0.0;
			min_value = 0.0;
			max_value = 0.0;
			break;
		}
		average_count = (double)custom_stat.m_TotalCount / (double)frame_count;
		min_count = custom_stat.m_MinCountPerFrame;
		max_count = custom_stat.m_MaxCountPerFrame;
	}

	private void GetDataGridValuesAccPerFrame(CustomStatSessionData custom_stat, out double average_value, out double average_count, out double min_value, out double max_value, out double min_count, out double max_count, int frame_count)
	{
		switch (custom_stat.ValueType)
		{
		case CustomStatValueType.Int64:
			average_value = (double)custom_stat.m_AccTotalValueInt64 / (double)frame_count;
			min_value = custom_stat.m_AccMinValuePerFrameInt64;
			max_value = custom_stat.m_AccMaxValuePerFrameInt64;
			break;
		case CustomStatValueType.Double:
			average_value = custom_stat.m_AccTotalValueDouble / (double)frame_count;
			min_value = custom_stat.m_AccMinValuePerFrameDouble;
			max_value = custom_stat.m_AccMaxValuePerFrameDouble;
			break;
		default:
			average_value = 0.0;
			min_value = 0.0;
			max_value = 0.0;
			break;
		}
		average_count = (double)custom_stat.m_TotalCount / (double)frame_count;
		min_count = custom_stat.m_MinCountPerFrame;
		max_count = custom_stat.m_MaxCountPerFrame;
	}

	private void GetDataGridValuesPerSecond(CustomStatSessionData custom_stat, out double average_value, out double average_count, out double min_value, out double max_value, out double min_count, out double max_count, double session_time_in_sec)
	{
		m_Session.GetCustomStatMinMaxValuesPerSec(custom_stat.Name, out min_value, out max_value, out min_count, out max_count);
		switch (custom_stat.ValueType)
		{
		case CustomStatValueType.Int64:
			average_value = (double)custom_stat.m_TotalValueInt64 / session_time_in_sec;
			break;
		case CustomStatValueType.Double:
			average_value = custom_stat.m_TotalValueDouble / session_time_in_sec;
			break;
		default:
			average_value = 0.0;
			break;
		}
		average_count = (double)custom_stat.m_TotalCount / session_time_in_sec;
	}

	private void GetDataGridValuesAccPerSecond(CustomStatSessionData custom_stat, out double average_value, out double average_count, out double min_value, out double max_value, out double min_count, out double max_count, double session_time_in_sec)
	{
		m_Session.GetCustomStatMinMaxAccValuesPerSec(custom_stat.Name, out min_value, out max_value, out min_count, out max_count);
		switch (custom_stat.ValueType)
		{
		case CustomStatValueType.Int64:
			average_value = (double)custom_stat.m_AccTotalValueInt64 / session_time_in_sec;
			break;
		case CustomStatValueType.Double:
			average_value = custom_stat.m_AccTotalValueDouble / session_time_in_sec;
			break;
		default:
			average_value = 0.0;
			break;
		}
		average_count = (double)custom_stat.m_TotalCount / session_time_in_sec;
	}

	private void PickBestMemoryUnit(ref double av_value_per_frame, ref double min_value_per_frame, ref double max_value_per_frame, ref string unit)
	{
		if (av_value_per_frame >= 1073741824.0)
		{
			av_value_per_frame /= 1073741824.0;
			min_value_per_frame /= 1073741824.0;
			max_value_per_frame /= 1073741824.0;
			unit = "GB";
		}
		else if (av_value_per_frame >= 1048576.0)
		{
			av_value_per_frame /= 1048576.0;
			min_value_per_frame /= 1048576.0;
			max_value_per_frame /= 1048576.0;
			unit = "MB";
		}
		else if (av_value_per_frame >= 1024.0)
		{
			av_value_per_frame /= 1024.0;
			min_value_per_frame /= 1024.0;
			max_value_per_frame /= 1024.0;
			unit = "KB";
		}
		else
		{
			unit = "Bytes";
		}
	}

	private void GraphAllCustomStats()
	{
		m_GraphedStats.Clear();
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row displayRow in row.ChildRows.DisplayRows)
			{
				string item = (string)displayRow.Cells[0].Value;
				m_GraphedStats.Add(item);
			}
		}
		UpdateDataGrid();
		UpdateGraphWindows();
	}

	private void SetDefaultGraphedFlags()
	{
		bool flag = false;
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row displayRow in row.ChildRows.DisplayRows)
			{
				if (m_GraphedStats.Count >= 10)
				{
					break;
				}
				if (!(bool)displayRow.Cells[GraphFlagCellIndex].Value)
				{
					displayRow.Cells[GraphFlagCellIndex].Value = true;
					m_GraphedStats.Add((string)displayRow.Cells[0].Value);
					flag = true;
				}
			}
			if (m_GraphedStats.Count >= 10)
			{
				break;
			}
		}
		if (flag)
		{
			UpdateGraphWindows();
		}
		UpdateParentRowGraphedFlags();
	}

	private void ClearGraph()
	{
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row childRow in row.ChildRows)
			{
				childRow.Cells[GraphFlagCellIndex].Value = false;
			}
		}
		UpdateGraphStatsFromDataGrid();
		UpdateDataGrid();
		m_DataGrid.RefreshDataGrid();
		UpdateGraphWindows();
	}

	private void FilterTextBoxTextChanged(object sender, EventArgs e)
	{
		UpdateDataGrid();
	}

	private void ClearFilterButtonClicked(object sender, EventArgs e)
	{
		m_FilterTextBox.Text = "";
	}

	private void DataGridCellChanged(ICollection<ColRow> sel_cells)
	{
		if (m_IgnoreDataGridCellChanged)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (ColRow sel_cell in sel_cells)
		{
			if (sel_cell.GetCell().Value is XAxisDropDownListControl)
			{
				string name = (string)sel_cell.Row.Cells[0].Value;
				XAxisDropDownListControl xAxisDropDownListControl = (XAxisDropDownListControl)sel_cell.GetCell().Value;
				m_Settings.CoreSettings.GetCustomStatInfo(name).m_XAxisMode = (CustomStatXAxisMode)xAxisDropDownListControl.Value;
				flag = true;
			}
			else
			{
				if (sel_cell.Col != GraphFlagCellIndex)
				{
					continue;
				}
				if (sel_cell.Row.ChildRows.Count != 0)
				{
					m_IgnoreDataGridCellChanged = true;
					bool flag3 = (bool)sel_cell.GetCellValue();
					foreach (Row childRow in sel_cell.Row.ChildRows)
					{
						childRow.Cells[GraphFlagCellIndex].Value = flag3;
					}
					m_IgnoreDataGridCellChanged = false;
				}
				flag2 = true;
			}
		}
		if (flag)
		{
			m_Settings.Write();
			UpdateDataGrid();
			UpdateGraphWindows();
			RefreshGraphs();
		}
		if (flag2)
		{
			m_Settings.UserSetScopesViewGraphFlags = true;
			UpdateGraphStatsFromDataGrid();
			UpdateGraphWindows();
			WriteGraphedStatsToSettings();
		}
	}

	private void UpdateGraphStatsFromDataGrid()
	{
		m_GraphedStats.Clear();
		foreach (Row row in m_DataGrid.Rows)
		{
			foreach (Row displayRow in row.ChildRows.DisplayRows)
			{
				if ((bool)displayRow.Cells[GraphFlagCellIndex].Value)
				{
					string item = (string)displayRow.Cells[0].Value;
					m_GraphedStats.Add(item);
				}
			}
		}
	}

	private static XAxisMode GetGraphXAxisMode(CustomStatXAxisMode x_axis_mode)
	{
		switch (x_axis_mode)
		{
		case CustomStatXAxisMode.Time:
		case CustomStatXAxisMode.AccTime:
			return XAxisMode.Time;
		case CustomStatXAxisMode.Frame:
		case CustomStatXAxisMode.AccFrame:
			return XAxisMode.Frame;
		default:
			return XAxisMode.Frame;
		}
	}

	private void UpdateGraphWindows()
	{
		if (!m_SessionIsReady)
		{
			return;
		}
		bool flag = false;
		long start_time = 0L;
		double x_scale = 0.0;
		if (m_GraphWindows.Count != 0)
		{
			flag = true;
			start_time = m_GraphWindows[0].XStart;
			x_scale = m_GraphWindows[0].XScale;
		}
		List<Graph> list = new List<Graph>();
		foreach (string graphedStat in m_GraphedStats)
		{
			long customStatNameId = m_Session.GetCustomStatNameId(graphedStat);
			if (customStatNameId != -1)
			{
				Color customStatColour = m_Session.GetCustomStatColour(customStatNameId);
				string customStatGraph = m_Session.GetCustomStatGraph(customStatNameId);
				string customStatUnit = m_Session.GetCustomStatUnit(customStatNameId);
				XAxisMode graphXAxisMode = GetGraphXAxisMode(m_Settings.CoreSettings.GetCustomStatInfo(graphedStat).m_XAxisMode);
				if (customStatGraph != null && customStatUnit != null)
				{
					list.Add(new Graph(graphedStat, customStatNameId, customStatColour, customStatGraph, customStatUnit, graphXAxisMode));
				}
			}
		}
		Dictionary<LineGraphWindow, List<Graph>> dictionary = new Dictionary<LineGraphWindow, List<Graph>>();
		List<LineGraphWindow> list2 = new List<LineGraphWindow>();
		foreach (Graph item in list)
		{
			LineGraphWindow lineGraphWindow = FindGraphWindow(item.m_GraphName);
			if (lineGraphWindow == null)
			{
				lineGraphWindow = CreateGraphWindow(item.m_GraphName, item.m_Unit);
				if (flag)
				{
					lineGraphWindow.SetTimeRange(start_time, x_scale);
				}
				list2.Add(lineGraphWindow);
			}
			if (!dictionary.TryGetValue(lineGraphWindow, out var value))
			{
				value = (dictionary[lineGraphWindow] = new List<Graph>());
			}
			value.Add(item);
		}
		List<string> list4 = new List<string>();
		foreach (Graph item2 in list)
		{
			if (!list4.Contains(item2.m_GraphName))
			{
				list4.Add(item2.m_GraphName);
			}
		}
		foreach (LineGraphWindow item3 in new List<LineGraphWindow>(m_GraphWindows))
		{
			if (!list4.Contains(item3.Graph))
			{
				RemoveGraphWindow(item3);
			}
		}
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			graphWindow.SetGraphs(dictionary[graphWindow]);
		}
		foreach (LineGraphWindow item4 in list2)
		{
			UpdateGraphWindowYScale(item4);
		}
	}

	private LineGraphWindow FindGraphWindow(string graph)
	{
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			if (graphWindow.Graph == graph)
			{
				return graphWindow;
			}
		}
		return null;
	}

	private void GraphAllButtonClicked(object sender, EventArgs e)
	{
		m_Settings.UserSetScopesViewGraphFlags = true;
		GraphAllCustomStats();
		WriteGraphedStatsToSettings();
	}

	private void ClearGraphButtonClicked(object sender, EventArgs e)
	{
		m_Settings.UserSetScopesViewGraphFlags = true;
		ClearGraph();
		WriteGraphedStatsToSettings();
	}

	public override void OnMouseWheel(int delta, Point location)
	{
		Point p = PointToScreen(location);
		Point pt = m_GraphsPanel.PointToClient(p);
		if (!m_GraphsPanel.ClientRectangle.Contains(pt))
		{
			return;
		}
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			if (new Rectangle(graphWindow.Location, graphWindow.Size).Contains(pt))
			{
				graphWindow.OnMouseWheel(delta, location);
			}
		}
	}

	private void XAxisFrameButtonCheckChanged()
	{
		m_XAxisMode = CustomStatXAxisMode.Frame;
		UpdateXAxisButtonStates();
		NotifyGraphWindowsXAxisChanged();
	}

	private void XAxisTimeButtonCheckChanged()
	{
		m_XAxisMode = CustomStatXAxisMode.Time;
		UpdateXAxisButtonStates();
		NotifyGraphWindowsXAxisChanged();
	}

	private void UpdateXAxisButtonStates()
	{
		m_XAxisFrameButton.Checked = m_XAxisMode == CustomStatXAxisMode.Frame;
		m_XAxisTimeButton.Checked = m_XAxisMode == CustomStatXAxisMode.Time;
	}

	private void NotifyGraphWindowsXAxisChanged()
	{
		XAxisMode graphXAxisMode = GetGraphXAxisMode(m_XAxisMode);
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			graphWindow.XAxisMode = graphXAxisMode;
		}
	}

	private void DataGridResize(object sender, EventArgs e)
	{
		if (m_Settings.CustomStatsDataGridHeight != m_DataGrid.Height)
		{
			m_Settings.CustomStatsDataGridHeight = m_DataGrid.Height;
			m_Settings.Write();
		}
	}

	private void ResetZoomButtonClicked(object sender, EventArgs e)
	{
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			graphWindow.ResetXAxisZoom();
			UpdateGraphWindowYScale(graphWindow);
			graphWindow.YOffset = 0.0;
		}
	}

	private void DataGridSelectionChanged(List<ColRow> selected_cells)
	{
		if (selected_cells.Count == 1 && selected_cells[0].Col == 10)
		{
			string name = selected_cells[0].Row.Cells[0].Value.ToString();
			ColorDialog colorDialog = new ColorDialog();
			if (colorDialog.ShowDialog(this) == DialogResult.OK)
			{
				m_Session.SetCustomStatColour(name, colorDialog.Color);
			}
		}
	}

	public override void OnCustomStatInfoChanged()
	{
		m_CustomStatInfoChanged = true;
	}

	public override void OnCustomStatColourChanged()
	{
		UpdateDataGrid();
		foreach (LineGraphWindow graphWindow in m_GraphWindows)
		{
			graphWindow.OnScopeColourChanged(m_Session.GetCustomStatColour);
		}
		Refresh();
	}

	private void SortedColumnChanged(Column column)
	{
		bool flag = column.SortMode == Column.ESortMode.Increasing;
		if (m_Settings.CustomStatsSortedColumn != column.Name || m_Settings.CustomStatsSortedColumnIncreasing != flag)
		{
			m_Settings.CustomStatsSortedColumn = column.Name;
			m_Settings.CustomStatsSortedColumnIncreasing = flag;
			m_Settings.Write();
		}
	}

	private void InitializeComponent()
	{
		SCL.RowCollection rows = new SCL.RowCollection();
		this.m_GraphsPanel = new FramePro.ScrollPanel();
		this.splitter2 = new System.Windows.Forms.Splitter();
		this.m_DataGrid = new SCL.HDataGrid();
		this.panel1 = new System.Windows.Forms.Panel();
		this.button4 = new System.Windows.Forms.Button();
		this.label2 = new System.Windows.Forms.Label();
		this.m_XAxisTimeButton = new FramePro.CheckButton();
		this.m_XAxisFrameButton = new FramePro.CheckButton();
		this.button3 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button1 = new Editor.Button();
		this.label1 = new System.Windows.Forms.Label();
		this.m_FilterTextBox = new System.Windows.Forms.TextBox();
		this.panel1.SuspendLayout();
		base.SuspendLayout();
		this.m_GraphsPanel.AutoScroll = true;
		this.m_GraphsPanel.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
		this.m_GraphsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_GraphsPanel.Location = new System.Drawing.Point(0, 338);
		this.m_GraphsPanel.Name = "m_GraphsPanel";
		this.m_GraphsPanel.Size = new System.Drawing.Size(1129, 406);
		this.m_GraphsPanel.TabIndex = 10;
		this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
		this.splitter2.Location = new System.Drawing.Point(0, 335);
		this.splitter2.Name = "splitter2";
		this.splitter2.Size = new System.Drawing.Size(1129, 3);
		this.splitter2.TabIndex = 9;
		this.splitter2.TabStop = false;
		this.m_DataGrid.AddEmptyRow = false;
		this.m_DataGrid.AlternateRowColours = true;
		this.m_DataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
		this.m_DataGrid.CanAddRemoveRows = false;
		this.m_DataGrid.CanRenameCell = false;
		this.m_DataGrid.CanResizeColumnTitleBar = false;
		this.m_DataGrid.CanResizeRows = false;
		this.m_DataGrid.CanResizeRowTitleBar = false;
		this.m_DataGrid.CanShowHideColumns = true;
		this.m_DataGrid.CanSortByColumn = true;
		this.m_DataGrid.ClearSelectionOnMouseLeave = false;
		this.m_DataGrid.ColumnTitlePanelVisible = true;
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_DataGrid.DrawColumnLines = true;
		this.m_DataGrid.DrawLastColumnLine = false;
		this.m_DataGrid.DrawRowLines = false;
		this.m_DataGrid.Font = new System.Drawing.Font("Monaco", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
		this.m_DataGrid.HighlightedRowBoxVisible = false;
		this.m_DataGrid.HighlightRow = true;
		this.m_DataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(225, 225, 255);
		this.m_DataGrid.HighlightSelectedRow = false;
		this.m_DataGrid.HorizontalTextOffset = 4;
		this.m_DataGrid.LightRowColour = System.Drawing.Color.FromArgb(235, 235, 235);
		this.m_DataGrid.Location = new System.Drawing.Point(0, 33);
		this.m_DataGrid.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
		this.m_DataGrid.MoveCellsEnabled = false;
		this.m_DataGrid.Name = "m_DataGrid";
		this.m_DataGrid.PadEmptyRows = false;
		this.m_DataGrid.ReadOnly = true;
		this.m_DataGrid.RowHeightPadding = 3;
		this.m_DataGrid.Rows = rows;
		this.m_DataGrid.RowTitelPanelVisible = true;
		this.m_DataGrid.ScrollColumnsHorz = false;
		this.m_DataGrid.SelectByRow = true;
		this.m_DataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(188, 180, 250);
		this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_DataGrid.SelectNextCellAfterEdit = true;
		this.m_DataGrid.ShowSelectBox = false;
		this.m_DataGrid.Size = new System.Drawing.Size(1129, 302);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 4;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.m_DataGrid.SelectionChanged += new SCL.SelectionChangedHandler(DataGridSelectionChanged);
		this.m_DataGrid.SortedColumnChanged += new SCL.SortedColumnChangedHandler(SortedColumnChanged);
		this.m_DataGrid.CellChanged += new SCL.CellChangedHandler(DataGridCellChanged);
		this.m_DataGrid.Resize += new System.EventHandler(DataGridResize);
		this.panel1.Controls.Add(this.button4);
		this.panel1.Controls.Add(this.label2);
		this.panel1.Controls.Add(this.m_XAxisTimeButton);
		this.panel1.Controls.Add(this.m_XAxisFrameButton);
		this.panel1.Controls.Add(this.button3);
		this.panel1.Controls.Add(this.button2);
		this.panel1.Controls.Add(this.button1);
		this.panel1.Controls.Add(this.label1);
		this.panel1.Controls.Add(this.m_FilterTextBox);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(1129, 33);
		this.panel1.TabIndex = 5;
		this.button4.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button4.Location = new System.Drawing.Point(567, 4);
		this.button4.Name = "button4";
		this.button4.Size = new System.Drawing.Size(75, 23);
		this.button4.TabIndex = 8;
		this.button4.Text = "Reset Zoom";
		this.button4.UseVisualStyleBackColor = true;
		this.button4.Click += new System.EventHandler(ResetZoomButtonClicked);
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(687, 9);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(71, 13);
		this.label2.TabIndex = 7;
		this.label2.Text = "Graph X Axis";
		this.m_XAxisTimeButton.BackColor = System.Drawing.SystemColors.Control;
		this.m_XAxisTimeButton.BorderColour = System.Drawing.Color.LightGray;
		this.m_XAxisTimeButton.ButtonText = "Time";
		this.m_XAxisTimeButton.Checked = false;
		this.m_XAxisTimeButton.CheckedBackgroundColour = System.Drawing.Color.LightBlue;
		this.m_XAxisTimeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_XAxisTimeButton.Location = new System.Drawing.Point(830, 4);
		this.m_XAxisTimeButton.Name = "m_XAxisTimeButton";
		this.m_XAxisTimeButton.Size = new System.Drawing.Size(69, 22);
		this.m_XAxisTimeButton.TabIndex = 6;
		this.m_XAxisTimeButton.CheckChange += new FramePro.CheckButtonCheckChangedHandler(XAxisTimeButtonCheckChanged);
		this.m_XAxisFrameButton.BackColor = System.Drawing.SystemColors.Control;
		this.m_XAxisFrameButton.BorderColour = System.Drawing.Color.LightGray;
		this.m_XAxisFrameButton.ButtonText = "Frame";
		this.m_XAxisFrameButton.Checked = true;
		this.m_XAxisFrameButton.CheckedBackgroundColour = System.Drawing.Color.LightBlue;
		this.m_XAxisFrameButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_XAxisFrameButton.Location = new System.Drawing.Point(761, 4);
		this.m_XAxisFrameButton.Name = "m_XAxisFrameButton";
		this.m_XAxisFrameButton.Size = new System.Drawing.Size(69, 22);
		this.m_XAxisFrameButton.TabIndex = 5;
		this.m_XAxisFrameButton.CheckChange += new FramePro.CheckButtonCheckChangedHandler(XAxisFrameButtonCheckChanged);
		this.button3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button3.Location = new System.Drawing.Point(437, 4);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(75, 23);
		this.button3.TabIndex = 4;
		this.button3.Text = "Graph None";
		this.button3.UseVisualStyleBackColor = true;
		this.button3.Click += new System.EventHandler(ClearGraphButtonClicked);
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(356, 4);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(75, 23);
		this.button2.TabIndex = 3;
		this.button2.Text = "Graph All";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(GraphAllButtonClicked);
		this.button1.ButtonText = "X";
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.HoverColour = System.Drawing.Color.FromArgb(245, 245, 245);
		this.button1.Image = null;
		this.button1.Location = new System.Drawing.Point(314, 6);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(20, 20);
		this.button1.TabIndex = 2;
		this.button1.Click += new System.EventHandler(ClearFilterButtonClicked);
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(20, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(33, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "Filter";
		this.m_FilterTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_FilterTextBox.Location = new System.Drawing.Point(59, 6);
		this.m_FilterTextBox.Name = "m_FilterTextBox";
		this.m_FilterTextBox.Size = new System.Drawing.Size(255, 22);
		this.m_FilterTextBox.TabIndex = 0;
		this.m_FilterTextBox.TextChanged += new System.EventHandler(FilterTextBoxTextChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_GraphsPanel);
		base.Controls.Add(this.splitter2);
		base.Controls.Add(this.m_DataGrid);
		base.Controls.Add(this.panel1);
		base.Name = "CustomStatsView";
		base.Size = new System.Drawing.Size(1129, 744);
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		base.ResumeLayout(false);
	}
}
