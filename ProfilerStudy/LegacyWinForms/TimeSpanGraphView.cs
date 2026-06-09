using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Editor;
using ProfilerStudy.Properties;
using SCL;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class TimeSpanGraphView : UserControl
{
	private class UpdateTimeSpanDataGridValues
	{
		public string m_TimeLabelText;

		public double m_FrameCountAverage;

		public long m_AverageTime;

		public long m_MaxTime;

		public long m_MaxTimePerFrame;
	}

	private Session m_Session;

	private Settings m_Settings;

	private long m_TimeSpanName;

	private bool m_IgnoreTimeSpanTextBoxSelectionChanged;

	private int m_SelectedRangeStart = -1;

	private int m_SelectedRangeEnd = -1;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private UpdateTimeSpanDataGridValues m_UpdateTimeSpanDataGridValues = new UpdateTimeSpanDataGridValues();

	private IContainer components;

	private VerticalLabelPanel verticalLabelPanelScope;

	private LeftBackPanel leftBackPanel2;

	private HDataGrid m_DataGrid;

	private Panel m_TopPanel;

	private FrameGraphPanel m_GraphPanel;

	private DropDownTextBox m_TimeSpanTextBox;

	private FrameGraphYAxis m_FrameGraphYAxis;

	private Panel horz_line;

	private System.Windows.Forms.Button button3;

	private Panel top;

	private Panel panel1;

	private Label label1;

	private Label m_TimeLabel;

	private Panel panel2;

	private System.Windows.Forms.Button m_SelectedScopeDropDownButton;

	private Panel panel3;

	private Panel panel4;

	public bool NeedsUpdate => m_GraphPanel.NeedsUpdate;

	public double YScale
	{
		get
		{
			return m_GraphPanel.YScale;
		}
		set
		{
			m_GraphPanel.YScale = value;
			m_FrameGraphYAxis.YScale = value;
		}
	}

	public bool Active
	{
		get
		{
			return m_GraphPanel.Active;
		}
		set
		{
			m_GraphPanel.Active = value;
		}
	}

	public event VisibleRangeChangedHandler VisibleRangeChanged;

	public event RangeChangedHandler RangeChanged;

	public event TimeSpanGraphViewSelectionChangedHandler SelectionChanged;

	public event FrameGraphYAxisScaleChangedHandler FrameGraphYAxisScaleChanged;

	public event TimeSpanGraphViewTargetMSChangedHandler TimeSpanGraphViewTargetMSChanged;

	public event SelectedRangeChangedHandler SelectedRangeChanged;

	public TimeSpanGraphView()
	{
		InitializeComponent();
		InitialiseTimeSpanGraphDataGrid();
		m_TopPanel.BackColor = Colours.TimeSpanGraph;
	}

	public void SetSession(Session session)
	{
		m_Session = session;
		m_GraphPanel.SetSession(session);
	}

	public void SetSettings(Settings settings)
	{
		m_Settings = settings;
		m_GraphPanel.SetSettings(settings);
		m_FrameGraphYAxis.SetSettings(settings);
		m_GraphPanel.TargetFrameMS = settings.ScopeTargetTime;
		m_FrameGraphYAxis.TargetFrameMS = settings.ScopeTargetTime;
	}

	private void InitialiseTimeSpanGraphDataGrid()
	{
		m_DataGrid.LightRowColour = Colours.DataGridBackColour;
		m_DataGrid.DarkRowColour = Colours.DataGridBackColour;
		Column column = new Column();
		column.WidthMode = Column.EWidthMode.Fill;
		Column column2 = new Column();
		float dPIScale = MainForm.DPIScale;
		column2.Width = (int)((float)FrameInfoPanel.ValueColumnWidth * dPIScale);
		m_DataGrid.Add(column);
		m_DataGrid.Add(column2);
		m_DataGrid.Rows.Add("Average", "0 ms");
		m_DataGrid.Rows.Add("Average Count/Frame", "0 ms");
		m_DataGrid.Rows.Add("Max", "0 ms");
		m_DataGrid.Rows.Add("Frame Max", "0 ms");
		m_DataGrid.PerformLayout();
		m_DataGrid.RefreshDataGrid();
	}

	public void UpdateTimeSpanDataGrid()
	{
		Task.Run(delegate
		{
			UpdateTimeSpanDataGridTask(m_GraphPanel.TimeSpanName, m_SelectedRangeStart, m_SelectedRangeEnd);
		});
	}

	private void UpdateTimeSpanDataGridTask(long time_span_name, int selected_range_start, int selected_range_end)
	{
		string text = "";
		if (time_span_name != -1)
		{
			long num = 0L;
			num = ((selected_range_start == -1 || selected_range_end == -1) ? m_Session.GetTimeSpanFrameAverageTime(time_span_name) : m_Session.GetTimeSpanFrameAverageTime(time_span_name, selected_range_start, selected_range_end));
			text = Utils.GetTimeString(num, m_Session.TimerFrequency);
		}
		else
		{
			text = "--";
		}
		double num2 = 0.0;
		long num3 = 0L;
		long num4 = 0L;
		long num5 = 0L;
		if (selected_range_start != -1 && selected_range_end != -1)
		{
			num2 = ((time_span_name != -1) ? m_Session.GetTimeSpanFrameAverageCount(time_span_name, selected_range_start, selected_range_end) : 0.0);
			num5 = ((time_span_name != -1) ? m_Session.GetTimeSpanAverage(time_span_name, selected_range_start, selected_range_end) : 0);
			num3 = ((time_span_name != -1) ? m_Session.GetTimeSpanMax(time_span_name, selected_range_start, selected_range_end) : 0);
			num4 = ((time_span_name != -1) ? m_Session.GetTimeSpanFrameMaxTime(time_span_name, selected_range_start, selected_range_end) : 0);
		}
		else
		{
			num2 = ((time_span_name != -1) ? m_Session.GetTimeSpanFrameAverageCount(time_span_name) : 0.0);
			num5 = m_Session.GetTimeSpanAverage(time_span_name);
			long start_time = 0L;
			num3 = ((time_span_name != -1) ? m_Session.GetTimeSpanMax(time_span_name, out start_time) : 0);
			num4 = ((time_span_name != -1) ? m_Session.GetTimeSpanFrameMaxTime(time_span_name) : 0);
		}
		lock (m_UpdateTimeSpanDataGridValues)
		{
			m_UpdateTimeSpanDataGridValues.m_TimeLabelText = text;
			m_UpdateTimeSpanDataGridValues.m_FrameCountAverage = num2;
			m_UpdateTimeSpanDataGridValues.m_AverageTime = num5;
			m_UpdateTimeSpanDataGridValues.m_MaxTime = num3;
			m_UpdateTimeSpanDataGridValues.m_MaxTimePerFrame = num4;
		}
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			UpdateTimeSpanDataGridFinished_Main();
		});
	}

	private void UpdateTimeSpanDataGridFinished_Main()
	{
		string timeLabelText;
		double frameCountAverage;
		long averageTime;
		long maxTime;
		long maxTimePerFrame;
		lock (m_UpdateTimeSpanDataGridValues)
		{
			timeLabelText = m_UpdateTimeSpanDataGridValues.m_TimeLabelText;
			frameCountAverage = m_UpdateTimeSpanDataGridValues.m_FrameCountAverage;
			averageTime = m_UpdateTimeSpanDataGridValues.m_AverageTime;
			maxTime = m_UpdateTimeSpanDataGridValues.m_MaxTime;
			maxTimePerFrame = m_UpdateTimeSpanDataGridValues.m_MaxTimePerFrame;
		}
		m_TimeLabel.Text = timeLabelText;
		m_DataGrid.Rows[0].Cells[1].Value = Utils.GetTimeString(averageTime, m_Session.TimerFrequency);
		m_DataGrid.Rows[1].Cells[1].Value = frameCountAverage.ToString("0.##");
		m_DataGrid.Rows[2].Cells[1].Value = Utils.GetTimeString(maxTime, m_Session.TimerFrequency);
		m_DataGrid.Rows[3].Cells[1].Value = Utils.GetTimeString(maxTimePerFrame, m_Session.TimerFrequency);
		m_DataGrid.Refresh();
	}

	public void SetTimeSpan(long time_span_name)
	{
		if (m_TimeSpanName != time_span_name)
		{
			m_TimeSpanName = time_span_name;
			m_IgnoreTimeSpanTextBoxSelectionChanged = true;
			m_TimeSpanTextBox.Selection = m_Session.GetTimerName(m_TimeSpanName);
			m_IgnoreTimeSpanTextBoxSelectionChanged = false;
			m_GraphPanel.SetTimeSpan(time_span_name);
		}
	}

	private void SetTimeSpanInternal(long time_span_name)
	{
		if (m_TimeSpanName != time_span_name)
		{
			SetTimeSpan(time_span_name);
			if (this.SelectionChanged != null)
			{
				this.SelectionChanged(m_TimeSpanName, null);
			}
		}
	}

	public void RecalculateView(bool force)
	{
		m_GraphPanel.RecalculateView(force);
	}

	public void OnMouseWheel(int delta, Point mouse_pt)
	{
		Point point = PointToScreen(mouse_pt);
		if (m_GraphPanel.RectangleToScreen(m_GraphPanel.ClientRectangle).Contains(point))
		{
			m_GraphPanel.OnMouseWheel(delta, m_GraphPanel.PointToClient(point));
		}
	}

	public bool SetSelectedRange(long start_time, long end_time, bool scroll_into_view)
	{
		return m_GraphPanel.SetVisibleRange(start_time, end_time, scroll_into_view);
	}

	public void GotoStart()
	{
		m_GraphPanel.GotoStart();
	}

	public void GotoEnd()
	{
		m_GraphPanel.GotoEnd();
	}

	public void SetRange(long start_frame_x, double scale)
	{
		m_GraphPanel.SetRange(start_frame_x, scale);
	}

	public void SetTimeRange(long start_time, long end_time)
	{
		m_GraphPanel.SetTimeRange(start_time, end_time);
	}

	private void TimeSpanTextBoxDroppingDown()
	{
		Set<long> set = new Set<long>();
		if (m_TimeSpanTextBox.Text != "None")
		{
			set = m_Session.GetTimerNames(m_TimeSpanTextBox.Text);
		}
		if (set.Count == 1)
		{
			set = m_Session.GetTimerNames("");
		}
		List<string> list = new List<string>();
		foreach (long item in set)
		{
			list.Add(m_Session.GetTimerName(item));
		}
		list.Sort();
		m_TimeSpanTextBox.Items.Clear();
		foreach (string item2 in list)
		{
			m_TimeSpanTextBox.Items.Add(item2);
		}
	}

	private void TimeSpanTextBoxSelectionChanged(string value)
	{
		if (!m_IgnoreTimeSpanTextBoxSelectionChanged)
		{
			long timeSpanNameId = m_Session.GetTimeSpanNameId(value);
			SetTimeSpanInternal(timeSpanNameId);
		}
	}

	private void GraphSelectedRangeChanged(Control sender, long start_time, long end_time)
	{
		if (this.VisibleRangeChanged != null)
		{
			this.VisibleRangeChanged(this, start_time, end_time);
		}
	}

	private void GraphRangeChanged(Control sender, long start_frame_x, long end_frame_x, double scale)
	{
		if (this.RangeChanged != null)
		{
			this.RangeChanged(this, start_frame_x, end_frame_x, scale);
		}
	}

	public void CentreSelection()
	{
		m_GraphPanel.CentreSelection();
	}

	public void CentreFrame(int frame_index)
	{
		m_GraphPanel.CentreFrame(frame_index);
	}

	private void FrameGraphYAxisScaleCahnged(double y_scale)
	{
		m_GraphPanel.YScale = y_scale;
		if (this.FrameGraphYAxisScaleChanged != null)
		{
			this.FrameGraphYAxisScaleChanged(y_scale);
		}
	}

	private void FrameGraphYAxisTargetMsChanged(double target_ms)
	{
		m_GraphPanel.TargetFrameMS = target_ms;
		m_Settings.ScopeTargetTime = target_ms;
		if (this.TimeSpanGraphViewTargetMSChanged != null)
		{
			this.TimeSpanGraphViewTargetMSChanged(target_ms);
		}
	}

	public void OnTimeSpanTargetMSChanged()
	{
		m_GraphPanel.TargetFrameMS = m_Settings.ScopeTargetTime;
		m_FrameGraphYAxis.TargetFrameMS = m_Settings.ScopeTargetTime;
	}

	private void SelectedScopeButtonClick(object sender, EventArgs e)
	{
		m_TimeSpanTextBox.ShowDropDown();
	}

	private void SelectedScopeDropDownButtonClicked(object sender, EventArgs e)
	{
		m_TimeSpanTextBox.ShowDropDown();
	}

	public void SetSelectedRange(int start_frame_index, int end_frame_index)
	{
		m_GraphPanel.SetSelectedRange(start_frame_index, end_frame_index);
		if (m_SelectedRangeStart != start_frame_index || m_SelectedRangeEnd != end_frame_index)
		{
			m_SelectedRangeStart = start_frame_index;
			m_SelectedRangeEnd = end_frame_index;
			UpdateTimeSpanDataGrid();
		}
	}

	private void GraphSelectedRangeChanged(int start_frame_index, int end_frame_index)
	{
		if (this.SelectedRangeChanged != null)
		{
			this.SelectedRangeChanged(start_frame_index, end_frame_index);
		}
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		SCL.RowCollection rows = new SCL.RowCollection();
		this.m_TopPanel = new System.Windows.Forms.Panel();
		this.m_TimeSpanTextBox = new Editor.DropDownTextBox();
		this.horz_line = new System.Windows.Forms.Panel();
		this.m_GraphPanel = new ProfilerStudy.FrameGraphPanel();
		this.leftBackPanel2 = new ProfilerStudy.LeftBackPanel();
		this.panel2 = new System.Windows.Forms.Panel();
		this.panel4 = new System.Windows.Forms.Panel();
		this.m_DataGrid = new SCL.HDataGrid();
		this.panel3 = new System.Windows.Forms.Panel();
		this.label1 = new System.Windows.Forms.Label();
		this.m_TimeLabel = new System.Windows.Forms.Label();
		this.m_FrameGraphYAxis = new ProfilerStudy.FrameGraphYAxis();
		this.top = new System.Windows.Forms.Panel();
		this.m_SelectedScopeDropDownButton = new System.Windows.Forms.Button();
		this.panel1 = new System.Windows.Forms.Panel();
		this.button3 = new System.Windows.Forms.Button();
		this.verticalLabelPanelScope = new ProfilerStudy.VerticalLabelPanel();
		this.m_TopPanel.SuspendLayout();
		this.leftBackPanel2.SuspendLayout();
		this.panel2.SuspendLayout();
		this.panel4.SuspendLayout();
		this.panel3.SuspendLayout();
		this.top.SuspendLayout();
		base.SuspendLayout();
		this.m_TopPanel.Controls.Add(this.m_TimeSpanTextBox);
		this.m_TopPanel.Controls.Add(this.horz_line);
		this.m_TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TopPanel.Location = new System.Drawing.Point(173, 0);
		this.m_TopPanel.Name = "m_TopPanel";
		this.m_TopPanel.Size = new System.Drawing.Size(864, 23);
		this.m_TopPanel.TabIndex = 12;
		this.m_TimeSpanTextBox.BackColor = System.Drawing.Color.FromArgb(196, 196, 196);
		this.m_TimeSpanTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_TimeSpanTextBox.DropDownBoxWidth = 400;
		this.m_TimeSpanTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TimeSpanTextBox.ForeColor = System.Drawing.Color.Black;
		this.m_TimeSpanTextBox.Location = new System.Drawing.Point(0, 0);
		this.m_TimeSpanTextBox.Margin = new System.Windows.Forms.Padding(4);
		this.m_TimeSpanTextBox.Name = "m_TimeSpanTextBox";
		this.m_TimeSpanTextBox.Selection = "None";
		this.m_TimeSpanTextBox.ShowDropDownButton = false;
		this.m_TimeSpanTextBox.Size = new System.Drawing.Size(864, 22);
		this.m_TimeSpanTextBox.TabIndex = 1;
		this.m_TimeSpanTextBox.DroppedDown += new Editor.DropDownTextBoxDroppedDownHandler(TimeSpanTextBoxDroppingDown);
		this.m_TimeSpanTextBox.SelectionChanged += new Editor.DropDownTextBoxSelectionChangedHandler(TimeSpanTextBoxSelectionChanged);
		this.horz_line.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
		this.horz_line.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.horz_line.Location = new System.Drawing.Point(0, 22);
		this.horz_line.Name = "horz_line";
		this.horz_line.Size = new System.Drawing.Size(864, 1);
		this.horz_line.TabIndex = 2;
		this.m_GraphPanel.Active = false;
		this.m_GraphPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_GraphPanel.Location = new System.Drawing.Point(173, 23);
		this.m_GraphPanel.Margin = new System.Windows.Forms.Padding(4);
		this.m_GraphPanel.Name = "m_GraphPanel";
		this.m_GraphPanel.ShowEvents = false;
		this.m_GraphPanel.ShowingTimeSpans = true;
		this.m_GraphPanel.Size = new System.Drawing.Size(864, 134);
		this.m_GraphPanel.TabIndex = 13;
		this.m_GraphPanel.TargetFrameMS = 0.0;
		this.m_GraphPanel.YScale = 0.0;
		this.m_GraphPanel.VisibleRangeChanged += new ProfilerStudy.VisibleRangeChangedHandler(GraphSelectedRangeChanged);
		this.m_GraphPanel.RangeChanged += new ProfilerStudy.RangeChangedHandler(GraphRangeChanged);
		this.m_GraphPanel.SelectedRangeChanged += new ProfilerStudy.SelectedRangeChangedHandler(GraphSelectedRangeChanged);
		this.leftBackPanel2.Controls.Add(this.panel2);
		this.leftBackPanel2.Controls.Add(this.m_FrameGraphYAxis);
		this.leftBackPanel2.Controls.Add(this.top);
		this.leftBackPanel2.Dock = System.Windows.Forms.DockStyle.Left;
		this.leftBackPanel2.Location = new System.Drawing.Point(23, 0);
		this.leftBackPanel2.Name = "leftBackPanel2";
		this.leftBackPanel2.Size = new System.Drawing.Size(150, 157);
		this.leftBackPanel2.TabIndex = 11;
		this.panel2.Controls.Add(this.panel4);
		this.panel2.Controls.Add(this.panel3);
		this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel2.Location = new System.Drawing.Point(0, 22);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(129, 135);
		this.panel2.TabIndex = 6;
		this.panel4.Controls.Add(this.m_DataGrid);
		this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel4.Location = new System.Drawing.Point(0, 58);
		this.panel4.Name = "panel4";
		this.panel4.Size = new System.Drawing.Size(129, 77);
		this.panel4.TabIndex = 7;
		this.m_DataGrid.AddEmptyRow = false;
		this.m_DataGrid.AlternateRowColours = true;
		this.m_DataGrid.BackColor = System.Drawing.SystemColors.Control;
		this.m_DataGrid.CanAddRemoveRows = false;
		this.m_DataGrid.CanRenameCell = false;
		this.m_DataGrid.CanResizeColumnTitleBar = true;
		this.m_DataGrid.CanResizeRows = true;
		this.m_DataGrid.CanResizeRowTitleBar = true;
		this.m_DataGrid.CanShowHideColumns = true;
		this.m_DataGrid.CanSortByColumn = false;
		this.m_DataGrid.ClearSelectionOnMouseLeave = false;
		this.m_DataGrid.ColumnTitlePanelVisible = false;
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_DataGrid.DrawColumnLines = true;
		this.m_DataGrid.DrawLastColumnLine = false;
		this.m_DataGrid.DrawRowLines = true;
		this.m_DataGrid.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
		this.m_DataGrid.HighlightedRowBoxVisible = false;
		this.m_DataGrid.HighlightRow = false;
		this.m_DataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(225, 225, 255);
		this.m_DataGrid.HighlightSelectedRow = false;
		this.m_DataGrid.HorizontalTextOffset = 4;
		this.m_DataGrid.LightRowColour = System.Drawing.Color.FromArgb(235, 235, 235);
		this.m_DataGrid.Location = new System.Drawing.Point(0, 0);
		this.m_DataGrid.Margin = new System.Windows.Forms.Padding(4);
		this.m_DataGrid.MoveCellsEnabled = false;
		this.m_DataGrid.Name = "m_DataGrid";
		this.m_DataGrid.PadEmptyRows = false;
		this.m_DataGrid.ReadOnly = true;
		this.m_DataGrid.RowHeightPadding = 3;
		this.m_DataGrid.Rows = rows;
		this.m_DataGrid.RowTitelPanelVisible = false;
		this.m_DataGrid.ScrollColumnsHorz = false;
		this.m_DataGrid.SelectByRow = false;
		this.m_DataGrid.SelectedCellColour = System.Drawing.Color.Gainsboro;
		this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_DataGrid.SelectNextCellAfterEdit = true;
		this.m_DataGrid.ShowSelectBox = false;
		this.m_DataGrid.Size = new System.Drawing.Size(129, 123);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 1;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.panel3.Controls.Add(this.label1);
		this.panel3.Controls.Add(this.m_TimeLabel);
		this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel3.Location = new System.Drawing.Point(0, 0);
		this.panel3.Name = "panel3";
		this.panel3.Size = new System.Drawing.Size(129, 58);
		this.panel3.TabIndex = 6;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(8, 8);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(110, 13);
		this.label1.TabIndex = 4;
		this.label1.Text = "Average Time/Frame";
		this.m_TimeLabel.AutoSize = true;
		this.m_TimeLabel.Font = new System.Drawing.Font("Consolas", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TimeLabel.Location = new System.Drawing.Point(6, 23);
		this.m_TimeLabel.Name = "m_TimeLabel";
		this.m_TimeLabel.Size = new System.Drawing.Size(38, 28);
		this.m_TimeLabel.TabIndex = 5;
		this.m_TimeLabel.Text = "--";
		this.m_FrameGraphYAxis.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_FrameGraphYAxis.Location = new System.Drawing.Point(129, 22);
		this.m_FrameGraphYAxis.Margin = new System.Windows.Forms.Padding(4);
		this.m_FrameGraphYAxis.Name = "m_FrameGraphYAxis";
		this.m_FrameGraphYAxis.Size = new System.Drawing.Size(21, 135);
		this.m_FrameGraphYAxis.TabIndex = 2;
		this.m_FrameGraphYAxis.TargetFrameMS = 0.0;
		this.m_FrameGraphYAxis.YScale = 0.0;
		this.m_FrameGraphYAxis.FrameGraphYAxisScaleChanged += new ProfilerStudy.FrameGraphYAxisScaleChangedHandler(FrameGraphYAxisScaleCahnged);
		this.m_FrameGraphYAxis.FrameGraphYAxisTargetMSChanged += new ProfilerStudy.FrameGraphYAxisTargetMSChangedHandler(FrameGraphYAxisTargetMsChanged);
		this.top.BackColor = System.Drawing.Color.FromArgb(196, 196, 196);
		this.top.Controls.Add(this.m_SelectedScopeDropDownButton);
		this.top.Controls.Add(this.panel1);
		this.top.Controls.Add(this.button3);
		this.top.Dock = System.Windows.Forms.DockStyle.Top;
		this.top.Location = new System.Drawing.Point(0, 0);
		this.top.Name = "top";
		this.top.Size = new System.Drawing.Size(150, 22);
		this.top.TabIndex = 3;
		this.m_SelectedScopeDropDownButton.FlatAppearance.BorderSize = 0;
		this.m_SelectedScopeDropDownButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.m_SelectedScopeDropDownButton.Image = ProfilerStudy.Properties.Resources.dropdown;
		this.m_SelectedScopeDropDownButton.Location = new System.Drawing.Point(128, 0);
		this.m_SelectedScopeDropDownButton.Name = "m_SelectedScopeDropDownButton";
		this.m_SelectedScopeDropDownButton.Size = new System.Drawing.Size(22, 23);
		this.m_SelectedScopeDropDownButton.TabIndex = 4;
		this.m_SelectedScopeDropDownButton.UseVisualStyleBackColor = true;
		this.m_SelectedScopeDropDownButton.Click += new System.EventHandler(SelectedScopeDropDownButtonClicked);
		this.panel1.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
		this.panel1.Location = new System.Drawing.Point(149, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(1, 22);
		this.panel1.TabIndex = 3;
		this.button3.BackColor = System.Drawing.Color.FromArgb(225, 225, 225);
		this.button3.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.button3.FlatAppearance.BorderSize = 0;
		this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button3.Location = new System.Drawing.Point(0, 0);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(129, 22);
		this.button3.TabIndex = 2;
		this.button3.Text = "Selected Scope";
		this.button3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.button3.UseVisualStyleBackColor = false;
		this.button3.Click += new System.EventHandler(SelectedScopeButtonClick);
		this.verticalLabelPanelScope.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanelScope.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanelScope.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanelScope.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanelScope.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		this.verticalLabelPanelScope.Name = "verticalLabelPanelScope";
		this.verticalLabelPanelScope.PanelText = "Scope";
		this.verticalLabelPanelScope.Size = new System.Drawing.Size(23, 157);
		this.verticalLabelPanelScope.TabIndex = 10;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_GraphPanel);
		base.Controls.Add(this.m_TopPanel);
		base.Controls.Add(this.leftBackPanel2);
		base.Controls.Add(this.verticalLabelPanelScope);
		base.Name = "TimeSpanGraphView";
		base.Size = new System.Drawing.Size(1037, 157);
		this.m_TopPanel.ResumeLayout(false);
		this.leftBackPanel2.ResumeLayout(false);
		this.panel2.ResumeLayout(false);
		this.panel4.ResumeLayout(false);
		this.panel3.ResumeLayout(false);
		this.panel3.PerformLayout();
		this.top.ResumeLayout(false);
		base.ResumeLayout(false);

		if (PlatformTool.IsRunOnWine())
		{
			this.verticalLabelPanelScope.PanelText = "S";
		}
	}
}
