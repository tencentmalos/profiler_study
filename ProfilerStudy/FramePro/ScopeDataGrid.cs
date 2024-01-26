using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SCL;
using SCLCoreCLR;

namespace FramePro;

internal class ScopeDataGrid : UserControl
{
	private class PercentCell : IComparable
	{
		private float m_Value;

		private string m_StringValue;

		public float Value => m_Value;

		public PercentCell(float value)
		{
			m_Value = value;
			m_StringValue = value.ToString("0.##") + " %";
		}

		public override string ToString()
		{
			return m_StringValue;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return -1;
			}
			PercentCell percentCell = (PercentCell)obj;
			return m_Value.CompareTo(percentCell.m_Value);
		}
	}

	private struct DataGridRow
	{
		public string m_TimeSpanName;

		public long m_Duration;

		public long m_Count;

		public float m_Percent;
	}

	private class TimeSpanTotal
	{
		public long m_Duration;

		public long m_Count;
	}

	private Session m_Session;

	private Settings m_Settings;

	private TimeSpan m_TimeSpan;

	private Column m_CallstackColumn = new Column();

	private Column m_CallstackFilenameColumn = new Column();

	private Column m_TimeColumn = new Column();

	private Column m_CountColumn = new Column();

	private Column m_PercentColumn = new Column();

	private volatile bool m_ExitCallstackThread;

	private object m_CallstackThreadIdLock = new object();

	private List<int> m_NewCallstackThreadCallstackIds = new List<int>();

	private int m_CallstackThreadCallstackId = -1;

	private AutoResetEvent m_NewCallstackTimeSpanEvent = new AutoResetEvent(initialState: false);

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private float m_DPIScale;

	private string[] m_CallstackExcludeFilters = new string[9] { "FramePro::Platform::GetStackTrace", "FramePro::StackTrace::Capture", "FramePro::FrameProTLS::GetCallstack", "FramePro::FrameProSession::SendScopeCallstack", "FramePro::SendScopeCallstack", "FramePro::AddTimeSpan", "FWindowsPlatformStackWalk::CaptureStackBackTrace", "FFrameProProfiler::PopEvent", "FramePro::TimerScope::~TimerScope" };

	private IContainer components;

	private HDataGrid m_DataGrid;

	private Label label3;

	private TextBox m_ScopeNameTextBox;

	private Button m_TimeUnitsButton;

	private Label label1;

	private HDataGrid m_CallstackDataGrid;

	private Splitter splitter1;

	private Panel m_CallstackPanel;

	private Panel m_TopPanel;

	private Panel m_DataGridPanel;

	private Button button1;

	private Button button2;

	public TimeSpan TimeSpan
	{
		get
		{
			return m_TimeSpan;
		}
		set
		{
			if (m_TimeSpan != value)
			{
				m_TimeSpan = value;
				UpdateCallstackDataGrid();
				UpdateDataGrid();
				m_ScopeNameTextBox.Text = ((m_TimeSpan != null) ? m_Session.GetTimerName(m_Session.GetTimeSpanInfo(m_TimeSpan.TimeSpanInfoId).Name) : "");
			}
		}
	}

	public ScopeDataGrid()
	{
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		InitialiseCallstackDataGrid();
		InitialiseDataGrid();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void InitialiseCallstackDataGrid()
	{
		m_CallstackColumn = new Column("Callstack");
		m_CallstackColumn.WidthMode = Column.EWidthMode.Fill;
		m_CallstackDataGrid.Add(m_CallstackColumn);
		m_CallstackFilenameColumn = new Column();
		m_CallstackFilenameColumn.Width = ScaleDPI(100);
		m_CallstackDataGrid.Add(m_CallstackFilenameColumn);
		Thread thread = new Thread(CallstackThread);
		thread.Name = "Callstack Thread";
		thread.Start();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		if (disposing)
		{
			m_ExitCallstackThread = true;
			m_NewCallstackTimeSpanEvent.Set();
		}
		base.Dispose(disposing);
	}

	private void CallstackThread()
	{
		while (!m_ExitCallstackThread)
		{
			bool flag = false;
			lock (m_CallstackThreadIdLock)
			{
				if (m_NewCallstackThreadCallstackIds.Count != 0)
				{
					m_CallstackThreadCallstackId = m_NewCallstackThreadCallstackIds[0];
					m_NewCallstackThreadCallstackIds.RemoveAt(0);
					flag = true;
				}
			}
			if (flag)
			{
				////string[] callstack = m_Session.GetCallstack(m_CallstackThreadCallstackId);
				////m_ControlTaskDispatcher.QueueTask(delegate
				////{
				////	CallstackThreadFinished(callstack);
				////});
			}
			else
			{
				m_NewCallstackTimeSpanEvent.WaitOne();
			}
		}
	}

	private bool ExcludeCallstackLine(string line)
	{
		string[] callstackExcludeFilters = m_CallstackExcludeFilters;
		foreach (string value in callstackExcludeFilters)
		{
			if (line.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	private void CallstackThreadFinished(string[] thraed_callstack)
	{
		string[] array = null;
		lock (m_CallstackThreadIdLock)
		{
			if (m_TimeSpan != null && m_CallstackThreadCallstackId == m_Session.GetTimeSpanInfo(m_TimeSpan.TimeSpanInfoId).CallstackId)
			{
				array = thraed_callstack;
			}
		}
		if (array != null)
		{
			m_CallstackDataGrid.Clear();
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!ExcludeCallstackLine(text))
				{
					int num = text.IndexOf('-');
					if (num == -1)
					{
						num = text.Length;
					}
					string cell = text.Substring(0, num - 1).Trim();
					string cell2 = ((num < text.Length) ? text.Substring(num + 1, text.Length - (num + 1)).Trim() : "");
					m_CallstackDataGrid.Rows.Add(cell, cell2);
				}
			}
			m_CallstackDataGrid.RefreshDataGrid();
		}
		else
		{
			UpdateCallstack();
		}
	}

	private void InitialiseDataGrid()
	{
		Column column = new Column();
		column.Name = "Scope";
		column.WidthMode = Column.EWidthMode.Fill;
		m_DataGrid.Add(column);
		m_TimeColumn.Name = "Time";
		m_TimeColumn.Width = ScaleDPI(68);
		m_TimeColumn.SortMode = Column.ESortMode.Decreasing;
		m_DataGrid.Add(m_TimeColumn);
		m_CountColumn.Name = "Count";
		m_CountColumn.Width = ScaleDPI(68);
		m_CountColumn.SortMode = Column.ESortMode.Decreasing;
		m_DataGrid.Add(m_CountColumn);
		m_PercentColumn.Name = "Percent";
		m_PercentColumn.Width = ScaleDPI(58);
		m_PercentColumn.SortMode = Column.ESortMode.Decreasing;
		m_DataGrid.Add(m_PercentColumn);
	}

	public void Initialise(Session session, Settings settings)
	{
		m_Session = session;
		m_Settings = settings;
		UpdateTimeUnitsButtonText();
		base.Size = new Size(ScaleDPI(settings.ScopeDataGridWidth), base.Height);
	}

	private void UpdateCallstackDataGrid()
	{
		m_CallstackDataGrid.Clear();
		if (m_TimeSpan != null)
		{
			if (UpdateCallstack())
			{
				m_CallstackDataGrid.Rows.Add("Resolving Callstack...");
			}
			else
			{
				m_CallstackDataGrid.Rows.Add("No Callstack Available");
				lock (m_CallstackThreadIdLock)
				{
					m_CallstackThreadCallstackId = -1;
				}
			}
		}
		m_CallstackDataGrid.Refresh();
	}

	private bool UpdateCallstack()
	{
		if (m_TimeSpan != null)
		{
			int callstackId = m_Session.GetTimeSpanInfo(m_TimeSpan.TimeSpanInfoId).CallstackId;
			if (callstackId != -1)
			{
				lock (m_CallstackThreadIdLock)
				{
					m_NewCallstackThreadCallstackIds.Add(callstackId);
				}
				m_NewCallstackTimeSpanEvent.Set();
				return true;
			}
		}
		return false;
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		int num = (int)((float)base.Width / m_DPIScale);
		if (m_Settings != null && m_Settings.ScopeDataGridWidth != num)
		{
			m_Settings.ScopeDataGridWidth = num;
			m_Settings.Write();
		}
		base.OnSizeChanged(e);
	}

	private void UpdateDataGrid()
	{
		m_DataGrid.Clear();
		if (m_TimeSpan != null && m_Settings != null)
		{
			m_TimeColumn.Name = "Time (" + Utils.GetTimeUnitsString(m_Settings.ScopeDataGridTimeUnits) + ")";
			Dictionary<string, TimeSpanTotal> dictionary = new Dictionary<string, TimeSpanTotal>();
			TimeSpan timeSpan = ((m_TimeSpan.Next != null) ? m_TimeSpan.Next : m_TimeSpan.Parent);
			RootFirstTimeSpanIterator rootFirstTimeSpanIterator = new RootFirstTimeSpanIterator(m_TimeSpan, is_root: true);
			while (rootFirstTimeSpanIterator.MoveNext() && rootFirstTimeSpanIterator.Current != timeSpan)
			{
				if (rootFirstTimeSpanIterator.Current.IsTimeSpanEx)
				{
					TimeSpanEx timeSpanEx = rootFirstTimeSpanIterator.Current as TimeSpanEx;
					if (timeSpanEx.HiResTimers == null)
					{
						continue;
					}
					foreach (HiResTimer hiResTimer in timeSpanEx.HiResTimers)
					{
						string timerName = m_Session.GetTimerName(hiResTimer.m_Name);
						if (!dictionary.ContainsKey(timerName))
						{
							dictionary[timerName] = new TimeSpanTotal();
						}
						dictionary[timerName].m_Duration += hiResTimer.m_Duration;
						dictionary[timerName].m_Count += hiResTimer.m_Count;
					}
				}
				else
				{
					int timeSpanInfoId = rootFirstTimeSpanIterator.Current.TimeSpanInfoId;
					TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(timeSpanInfoId);
					string timerName2 = m_Session.GetTimerName(timeSpanInfo.Name);
					if (!dictionary.ContainsKey(timerName2))
					{
						dictionary[timerName2] = new TimeSpanTotal();
					}
					if (!AlreadyCountedInParent(rootFirstTimeSpanIterator.Current, m_TimeSpan))
					{
						dictionary[timerName2].m_Duration += rootFirstTimeSpanIterator.Current.Duration;
					}
					dictionary[timerName2].m_Count++;
				}
			}
			foreach (string key in dictionary.Keys)
			{
				TimeSpanTotal timeSpanTotal = dictionary[key];
				DataGridRow dataGridRow = default(DataGridRow);
				dataGridRow.m_TimeSpanName = key;
				dataGridRow.m_Duration = timeSpanTotal.m_Duration;
				dataGridRow.m_Count = timeSpanTotal.m_Count;
				dataGridRow.m_Percent = (float)timeSpanTotal.m_Duration * 100f / (float)m_TimeSpan.Duration;
				float num = Convert.ToSingle(Utils.GetTimeString(dataGridRow.m_Duration, m_Session.TimerFrequency, m_Settings.ScopeDataGridTimeUnits));
				PercentCell cell = new PercentCell(dataGridRow.m_Percent);
				m_DataGrid.Rows.Add(dataGridRow.m_TimeSpanName, num, dataGridRow.m_Count, cell);
			}
		}
		Column column = m_DataGrid.GetSortedColumn();
		if (column == null)
		{
			column = m_TimeColumn;
		}
		m_DataGrid.Sort(column);
		m_DataGrid.RefreshDataGrid();
	}

	private bool AlreadyCountedInParent(TimeSpan time_span, TimeSpan root_scope)
	{
		TimeSpanInfo timeSpanInfo = m_Session.GetTimeSpanInfo(time_span.TimeSpanInfoId);
		string timerName = m_Session.GetTimerName(timeSpanInfo.Name);
		TimeSpan timeSpan = time_span.Parent;
		while (timeSpan != null && timeSpan != root_scope.Parent)
		{
			TimeSpanInfo timeSpanInfo2 = m_Session.GetTimeSpanInfo(timeSpan.TimeSpanInfoId);
			if (m_Session.GetTimerName(timeSpanInfo2.Name) == timerName)
			{
				return true;
			}
			timeSpan = timeSpan.Parent;
		}
		return false;
	}

	private void TimeUnitsButtonPressed(object sender, EventArgs e)
	{
		m_Settings.ScopeDataGridTimeUnits = (TimeUnitsNEW)((int)(m_Settings.ScopeDataGridTimeUnits + 1) % 4);
		m_Settings.Write();
		UpdateTimeUnitsButtonText();
		UpdateDataGrid();
	}

	private void UpdateTimeUnitsButtonText()
	{
		m_TimeUnitsButton.Text = m_Settings.ScopeDataGridTimeUnits.ToString();
	}

	private void JumpToSourceButtonClicked(object sender, EventArgs e)
	{
		if (m_TimeSpan != null)
		{
			Utils.JumpToSourceCode(m_TimeSpan, m_Session, m_Settings);
		}
	}

	private void CloseButtonClicked(object sender, EventArgs e)
	{
		MainForm.Inst.ShowThreadsViewDataGrid(visible: false);
	}

	private void InitializeComponent()
	{
            SCL.RowCollection rowCollection3 = new SCL.RowCollection();
            SCL.RowCollection rowCollection4 = new SCL.RowCollection();
            this.m_DataGrid = new SCL.HDataGrid();
            this.label3 = new System.Windows.Forms.Label();
            this.m_ScopeNameTextBox = new System.Windows.Forms.TextBox();
            this.m_TimeUnitsButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.m_CallstackDataGrid = new SCL.HDataGrid();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.m_CallstackPanel = new System.Windows.Forms.Panel();
            this.m_TopPanel = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.m_DataGridPanel = new System.Windows.Forms.Panel();
            this.m_CallstackPanel.SuspendLayout();
            this.m_TopPanel.SuspendLayout();
            this.m_DataGridPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_DataGrid
            // 
            this.m_DataGrid.AddEmptyRow = false;
            this.m_DataGrid.AlternateRowColours = true;
            this.m_DataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_DataGrid.CanAddRemoveRows = true;
            this.m_DataGrid.CanRenameCell = true;
            this.m_DataGrid.CanResizeColumnTitleBar = false;
            this.m_DataGrid.CanResizeRows = false;
            this.m_DataGrid.CanResizeRowTitleBar = false;
            this.m_DataGrid.CanShowHideColumns = true;
            this.m_DataGrid.CanSortByColumn = true;
            this.m_DataGrid.ClearSelectionOnMouseLeave = false;
            this.m_DataGrid.ColumnTitlePanelVisible = true;
            this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_DataGrid.DrawColumnLines = true;
            this.m_DataGrid.DrawLastColumnLine = false;
            this.m_DataGrid.DrawRowLines = false;
            this.m_DataGrid.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_DataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
            this.m_DataGrid.HighlightedRowBoxVisible = false;
            this.m_DataGrid.HighlightRow = false;
            this.m_DataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(255)))));
            this.m_DataGrid.HighlightSelectedRow = false;
            this.m_DataGrid.HorizontalTextOffset = 4;
            this.m_DataGrid.LightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_DataGrid.Location = new System.Drawing.Point(0, 0);
            this.m_DataGrid.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.m_DataGrid.MoveCellsEnabled = false;
            this.m_DataGrid.Name = "m_DataGrid";
            this.m_DataGrid.PadEmptyRows = true;
            this.m_DataGrid.ReadOnly = true;
            this.m_DataGrid.RowHeightPadding = 3;
            this.m_DataGrid.Rows = rowCollection3;
            this.m_DataGrid.RowTitelPanelVisible = true;
            this.m_DataGrid.ScrollColumnsHorz = false;
            this.m_DataGrid.SelectByRow = false;
            this.m_DataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(180)))), ((int)(((byte)(250)))));
            this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(255)))));
            this.m_DataGrid.SelectNextCellAfterEdit = true;
            this.m_DataGrid.ShowSelectBox = true;
            this.m_DataGrid.Size = new System.Drawing.Size(452, 772);
            this.m_DataGrid.SlideDrag = false;
            this.m_DataGrid.TabIndex = 0;
            this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 29);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 18);
            this.label3.TabIndex = 5;
            this.label3.Text = "Scope";
            // 
            // m_ScopeNameTextBox
            // 
            this.m_ScopeNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_ScopeNameTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_ScopeNameTextBox.Location = new System.Drawing.Point(84, 25);
            this.m_ScopeNameTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.m_ScopeNameTextBox.Name = "m_ScopeNameTextBox";
            this.m_ScopeNameTextBox.ReadOnly = true;
            this.m_ScopeNameTextBox.Size = new System.Drawing.Size(308, 26);
            this.m_ScopeNameTextBox.TabIndex = 6;
            // 
            // m_TimeUnitsButton
            // 
            this.m_TimeUnitsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.m_TimeUnitsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.m_TimeUnitsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_TimeUnitsButton.Location = new System.Drawing.Point(307, 62);
            this.m_TimeUnitsButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.m_TimeUnitsButton.Name = "m_TimeUnitsButton";
            this.m_TimeUnitsButton.Size = new System.Drawing.Size(141, 32);
            this.m_TimeUnitsButton.TabIndex = 7;
            this.m_TimeUnitsButton.Text = "Microseconds";
            this.m_TimeUnitsButton.UseVisualStyleBackColor = true;
            this.m_TimeUnitsButton.Click += new System.EventHandler(this.TimeUnitsButtonPressed);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(209, 68);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 20);
            this.label1.TabIndex = 8;
            this.label1.Text = "Time Units";
            // 
            // m_CallstackDataGrid
            // 
            this.m_CallstackDataGrid.AddEmptyRow = false;
            this.m_CallstackDataGrid.AlternateRowColours = true;
            this.m_CallstackDataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_CallstackDataGrid.CanAddRemoveRows = true;
            this.m_CallstackDataGrid.CanRenameCell = true;
            this.m_CallstackDataGrid.CanResizeColumnTitleBar = false;
            this.m_CallstackDataGrid.CanResizeRows = false;
            this.m_CallstackDataGrid.CanResizeRowTitleBar = false;
            this.m_CallstackDataGrid.CanShowHideColumns = true;
            this.m_CallstackDataGrid.CanSortByColumn = true;
            this.m_CallstackDataGrid.ClearSelectionOnMouseLeave = false;
            this.m_CallstackDataGrid.ColumnTitlePanelVisible = true;
            this.m_CallstackDataGrid.DarkRowColour = System.Drawing.Color.WhiteSmoke;
            this.m_CallstackDataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_CallstackDataGrid.DrawColumnLines = true;
            this.m_CallstackDataGrid.DrawLastColumnLine = false;
            this.m_CallstackDataGrid.DrawRowLines = false;
            this.m_CallstackDataGrid.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_CallstackDataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
            this.m_CallstackDataGrid.HighlightedRowBoxVisible = false;
            this.m_CallstackDataGrid.HighlightRow = true;
            this.m_CallstackDataGrid.HighlightRowColour = System.Drawing.Color.White;
            this.m_CallstackDataGrid.HighlightSelectedRow = true;
            this.m_CallstackDataGrid.HorizontalTextOffset = 4;
            this.m_CallstackDataGrid.LightRowColour = System.Drawing.Color.WhiteSmoke;
            this.m_CallstackDataGrid.Location = new System.Drawing.Point(0, 0);
            this.m_CallstackDataGrid.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.m_CallstackDataGrid.MoveCellsEnabled = false;
            this.m_CallstackDataGrid.Name = "m_CallstackDataGrid";
            this.m_CallstackDataGrid.PadEmptyRows = true;
            this.m_CallstackDataGrid.ReadOnly = true;
            this.m_CallstackDataGrid.RowHeightPadding = 3;
            this.m_CallstackDataGrid.Rows = rowCollection4;
            this.m_CallstackDataGrid.RowTitelPanelVisible = false;
            this.m_CallstackDataGrid.ScrollColumnsHorz = false;
            this.m_CallstackDataGrid.SelectByRow = false;
            this.m_CallstackDataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_CallstackDataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.m_CallstackDataGrid.SelectNextCellAfterEdit = true;
            this.m_CallstackDataGrid.ShowSelectBox = false;
            this.m_CallstackDataGrid.Size = new System.Drawing.Size(452, 310);
            this.m_CallstackDataGrid.SlideDrag = false;
            this.m_CallstackDataGrid.TabIndex = 9;
            this.m_CallstackDataGrid.WindowColour = System.Drawing.SystemColors.Window;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Location = new System.Drawing.Point(0, 881);
            this.splitter1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(452, 4);
            this.splitter1.TabIndex = 10;
            this.splitter1.TabStop = false;
            // 
            // m_CallstackPanel
            // 
            this.m_CallstackPanel.Controls.Add(this.m_CallstackDataGrid);
            this.m_CallstackPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.m_CallstackPanel.Location = new System.Drawing.Point(0, 885);
            this.m_CallstackPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.m_CallstackPanel.Name = "m_CallstackPanel";
            this.m_CallstackPanel.Size = new System.Drawing.Size(452, 310);
            this.m_CallstackPanel.TabIndex = 11;
            // 
            // m_TopPanel
            // 
            this.m_TopPanel.Controls.Add(this.button2);
            this.m_TopPanel.Controls.Add(this.button1);
            this.m_TopPanel.Controls.Add(this.m_ScopeNameTextBox);
            this.m_TopPanel.Controls.Add(this.label3);
            this.m_TopPanel.Controls.Add(this.m_TimeUnitsButton);
            this.m_TopPanel.Controls.Add(this.label1);
            this.m_TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.m_TopPanel.Location = new System.Drawing.Point(0, 0);
            this.m_TopPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.m_TopPanel.Name = "m_TopPanel";
            this.m_TopPanel.Size = new System.Drawing.Size(452, 109);
            this.m_TopPanel.TabIndex = 12;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Location = new System.Drawing.Point(410, 4);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(38, 35);
            this.button2.TabIndex = 10;
            this.button2.Text = "X";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.CloseButtonClicked);
            // 
            // button1
            // 
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(4, 62);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(160, 32);
            this.button1.TabIndex = 9;
            this.button1.Text = "Go to Source";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.JumpToSourceButtonClicked);
            // 
            // m_DataGridPanel
            // 
            this.m_DataGridPanel.Controls.Add(this.m_DataGrid);
            this.m_DataGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_DataGridPanel.Location = new System.Drawing.Point(0, 109);
            this.m_DataGridPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.m_DataGridPanel.Name = "m_DataGridPanel";
            this.m_DataGridPanel.Size = new System.Drawing.Size(452, 772);
            this.m_DataGridPanel.TabIndex = 13;
            // 
            // ScopeDataGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.m_DataGridPanel);
            this.Controls.Add(this.m_TopPanel);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.m_CallstackPanel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "ScopeDataGrid";
            this.Size = new System.Drawing.Size(452, 1195);
            this.m_CallstackPanel.ResumeLayout(false);
            this.m_TopPanel.ResumeLayout(false);
            this.m_TopPanel.PerformLayout();
            this.m_DataGridPanel.ResumeLayout(false);
            this.ResumeLayout(false);

	}
}
