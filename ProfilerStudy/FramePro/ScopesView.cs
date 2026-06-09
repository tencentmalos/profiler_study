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

internal class ScopesView : SessionView
{
	private const int m_ValueColumnWidth = 150;

	private const int m_MaxScopesToGraphByDefault = 10;

	private Session m_Session;

	private Settings m_Settings;

	private Column m_NameColumn;

	private Column m_MaxTimePerFrameColumn;

	private Column m_MaxCountPerFrameColumn;

	private Column m_AverageTimePerFrameColumn;

	private Column m_AverageCountPerFrameColumm;

	private Column m_GraphColumn;

	private Column m_ColourColumn;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private int m_LastUpdateTime;

	private const int m_UpdateFrequency = 1000;

	private List<string> m_GraphedScopes = new List<string>();

	private bool m_ScopeGraphNeedsUpdate;

	private bool m_DataGridNeedsUpdate;

	private bool m_CurrentlyUpdatingDataGrid;

	private int m_CurrentUpdateDataGridRevision;

	private float m_DPIScale;

	private IContainer components;

	private HDataGrid m_DataGrid;

	private Panel panel1;

	private Label label1;

	private TextBox m_FilterTextBox;

	private Editor.Button button1;

	private Splitter splitter1;

	private System.Windows.Forms.Button button3;

	private System.Windows.Forms.Button button2;

	private LineGraph m_ScopeGraph;

	private ContextMenuStrip m_ContextMenu;

	private ToolStripMenuItem goToSourceToolStripMenuItem;

	private ToolStripMenuItem copyToolStripMenuItem;

	public override string ViewName => "Scopes";

	public override Session Session => m_Session;

	private int GraphFlagCellIndex => m_DataGrid.IndexOf(m_GraphColumn);

	public ScopesView(Session session, Settings settings)
	{
		m_DPIScale = MainForm.DPIScale;
		m_Session = session;
		m_Settings = settings;
		InitializeComponent();
		m_ScopeGraph.StopTrackingEnd += base.StopTrackingEnd;
		if (m_Settings.ScopesGraphHeight != -1)
		{
			m_ScopeGraph.Size = new Size(m_ScopeGraph.Width, m_Settings.ScopesGraphHeight);
		}
		m_ScopeGraph.SetSession(session);
		m_ScopeGraph.SetSettings(settings);
		m_ScopeGraph.GetGraphValuesPerFrameFunction = GetGraphValues;
		m_ScopeGraph.YScale = settings.ScopeGraphYScale;
		InitialiseDataGrid();
		if (m_Settings.UserSetScopesViewGraphFlags)
		{
			ReadGraphedScopesFromSettings();
		}
		HookSession();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void ContextMenuGoToSourceMenuItemClicked(object sender, EventArgs e)
	{
		GoToSourceForSelectedCell();
	}

	private void ContextMenuCopyMenuItemClicked(object sender, EventArgs e)
	{
		m_DataGrid.CopySelectedCellsToClipboard();
	}

	private void GoToSourceForSelectedCell()
	{
		if (m_DataGrid.SelectedCell.Valid)
		{
			string text = m_DataGrid.SelectedCell.GetCellValue() as string;
			if (!string.IsNullOrEmpty(text))
			{
				Utils.JumpToSourceCode(m_Session.GetTimeSpanInfoIdFromName(text), m_Session, m_Settings);
			}
		}
	}

	private void ReadGraphedScopesFromSettings()
	{
		m_GraphedScopes = new List<string>(m_Settings.ScopesViewGraphedScopes);
	}

	private void WriteGraphedScopesToSettings()
	{
		m_Settings.ScopesViewGraphedScopes = new List<string>(m_GraphedScopes);
		m_Settings.Write();
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	public override void UpdateView()
	{
		int tickCount = Environment.TickCount;
		if (m_ScopeGraphNeedsUpdate)
		{
			UpdateScopeGraph();
			m_ScopeGraphNeedsUpdate = false;
		}
		if (m_DataGridNeedsUpdate)
		{
			UpdateDataGrid();
			m_DataGridNeedsUpdate = false;
		}
		if (m_Session.Connected && base.Active)
		{
			if (tickCount - m_LastUpdateTime > 1000 && base.TrackEnd)
			{
				UpdateDataGrid();
				if (!m_Settings.UserSetScopesViewGraphFlags)
				{
					SetDefaultGraphedScopes();
				}
				m_LastUpdateTime = tickCount;
			}
			if (base.TrackEnd)
			{
				m_ScopeGraph.GotoEnd();
			}
			m_ScopeGraph.RefreshGraph();
		}
		base.UpdateView();
	}

	private void GetGraphValues(int start_frame_index, int end_frame_index, long name, List<FrameValue> values)
	{
		m_Session.GetTimeSpanFrameTimes(start_frame_index, end_frame_index, name, values);
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
		m_Session.TimerNameAdded += SessionTimerNameAdded;
		m_Session.FinishedProcessingPackets += SessionFinishedProcessingPackets;
	}

	private void SessionFinishedProcessingPackets()
	{
		m_ControlTaskDispatcher.QueueTask(SessionFinishedProcessingPackets_Main);
	}

	private void SessionFinishedProcessingPackets_Main()
	{
		RefreshDataGridAndGraphs();
	}

	private void SessionTimerNameAdded(long name_id, string name)
	{
		m_ControlTaskDispatcher.QueueTask(SessionTimerNameAdded_Main);
	}

	private void SessionTimerNameAdded_Main()
	{
		RefreshDataGridAndGraphs();
	}

	private void RefreshDataGridAndGraphs()
	{
		m_ScopeGraphNeedsUpdate = true;
		m_DataGridNeedsUpdate = true;
	}

	private void OnSessionIsReady()
	{
		m_ControlTaskDispatcher.QueueTask(OnSessionIsready_Main);
	}

	private void OnSessionIsready_Main()
	{
		SetDefaultGraphedScopes();
		m_ScopeGraph.SetSessionIsReady();
		UpdateScopeGraph();
		UpdateDataGrid();
	}

	private void InitialiseDataGrid()
	{
		m_DataGrid.ContextMenuStrip = m_ContextMenu;
		m_NameColumn = new Column("Name");
		m_NameColumn.WidthMode = Column.EWidthMode.Fill;
		m_NameColumn.ReadOnly = true;
		m_DataGrid.Add(m_NameColumn);
		m_MaxTimePerFrameColumn = new Column("Max Time / Frame");
		m_MaxTimePerFrameColumn.Width = ScaleDPI(150);
		m_MaxTimePerFrameColumn.ReadOnly = true;
		m_DataGrid.Add(m_MaxTimePerFrameColumn);
		m_MaxCountPerFrameColumn = new Column("Max Count / Frame");
		m_MaxCountPerFrameColumn.Width = ScaleDPI(150);
		m_MaxCountPerFrameColumn.ReadOnly = true;
		m_DataGrid.Add(m_MaxCountPerFrameColumn);
		m_AverageTimePerFrameColumn = new Column("Average Time / Frame");
		m_AverageTimePerFrameColumn.Width = ScaleDPI(150);
		m_AverageTimePerFrameColumn.ReadOnly = true;
		m_DataGrid.Add(m_AverageTimePerFrameColumn);
		m_AverageCountPerFrameColumm = new Column("Average Count / Frame");
		m_AverageCountPerFrameColumm.Width = ScaleDPI(150);
		m_AverageCountPerFrameColumm.ReadOnly = true;
		m_DataGrid.Add(m_AverageCountPerFrameColumm);
		m_ColourColumn = new Column("Colour");
		m_ColourColumn.Width = ScaleDPI(40);
		m_DataGrid.Add(m_ColourColumn);
		m_GraphColumn = new Column("Graph");
		m_GraphColumn.Width = ScaleDPI(70);
		m_GraphColumn.BoolTrueImage = Resources.tick;
		m_DataGrid.Add(m_GraphColumn);
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

	private void UpdateDataGrid()
	{
		m_CurrentUpdateDataGridRevision++;
		if (!m_CurrentlyUpdatingDataGrid)
		{
			m_CurrentlyUpdatingDataGrid = true;
			Task.Run(delegate
			{
				UpdateDataGrid_Thread(m_CurrentUpdateDataGridRevision);
			});
		}
	}

	private void UpdateDataGrid_Thread(int revision)
	{
		List<ScopeSessionStats> scope_session_stats_list = m_Session.GetTimeSpanStats();
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			UpdateDataGrid_Main(scope_session_stats_list, revision);
		});
	}

	private void UpdateDataGrid_Main(List<ScopeSessionStats> scope_session_stats_list, int revision)
	{
		long timerFrequency = m_Session.TimerFrequency;
		m_DataGrid.Rows.Clear();
		int frameCount = m_Session.FrameCount;
		_ = (double)(m_Session.LastFrameEndTime - m_Session.FirstFrameTime) / (double)timerFrequency;
		int num = 0;
		foreach (ScopeSessionStats item in scope_session_stats_list)
		{
			if (FilterIn(item.m_Name))
			{
				double cycles = (double)item.m_TotalTime / (double)frameCount;
				cycles = Utils.CyclesToMs(cycles, timerFrequency);
				cycles = Utils.TruncateDouble(cycles);
				double value = (double)item.m_TotalCount / (double)frameCount;
				value = Utils.TruncateDouble(value);
				double value2 = Utils.CyclesToMs(item.m_MaxTimePerFrame, timerFrequency);
				value2 = Utils.TruncateDouble(value2);
				long maxCountPerFrame = item.m_MaxCountPerFrame;
				long stringId = m_Session.GetStringId(item.m_Name);
				Color scopeColour = m_Session.GetScopeColour(stringId);
				m_DataGrid.Rows.Add(item.m_Name, value2, maxCountPerFrame, cycles, value, scopeColour, m_GraphedScopes.Contains(item.m_Name));
				num++;
			}
		}
		m_AverageTimePerFrameColumn.SortMode = Column.ESortMode.Decreasing;
		m_DataGrid.Sort(m_AverageTimePerFrameColumn);
		m_DataGrid.RefreshDataGrid();
		m_CurrentlyUpdatingDataGrid = false;
		if (m_CurrentUpdateDataGridRevision != revision)
		{
			m_CurrentlyUpdatingDataGrid = true;
			Task.Run(delegate
			{
				UpdateDataGrid_Thread(m_CurrentUpdateDataGridRevision);
			});
		}
	}

	private void GraphAllScopes()
	{
		for (int i = 0; i < m_DataGrid.Rows.Count; i++)
		{
			string item = (string)m_DataGrid.GetDisplayedRow(i).Cells[0].Value;
			m_GraphedScopes.Add(item);
		}
		UpdateDataGrid();
		UpdateScopeGraph();
	}

	private void SetDefaultGraphedScopes()
	{
		bool flag = false;
		for (int i = 0; i < m_DataGrid.Rows.Count; i++)
		{
			if (m_GraphedScopes.Count >= 10)
			{
				break;
			}
			m_DataGrid.GetDisplayedRow(i).Cells[GraphFlagCellIndex].Value = true;
			m_GraphedScopes.Add((string)m_DataGrid.GetDisplayedRow(i).Cells[0].Value);
			flag = true;
		}
		if (flag)
		{
			UpdateScopeGraph();
		}
	}

	private void ClearGraph()
	{
		m_GraphedScopes.Clear();
		foreach (Row row in m_DataGrid.Rows)
		{
			row.Cells[GraphFlagCellIndex].Value = false;
		}
		UpdateScopeGraph();
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
		m_Settings.UserSetScopesViewGraphFlags = true;
		UpdateGraphjedScopesFromDataGrid();
		UpdateScopeGraph();
		WriteGraphedScopesToSettings();
	}

	private void UpdateGraphjedScopesFromDataGrid()
	{
		m_GraphedScopes.Clear();
		for (int i = 0; i < m_DataGrid.Rows.Count; i++)
		{
			if ((bool)m_DataGrid.GetDisplayedRow(i).Cells[GraphFlagCellIndex].Value)
			{
				m_GraphedScopes.Add((string)m_DataGrid.GetDisplayedRow(i).Cells[0].Value);
			}
		}
	}

	private void UpdateScopeGraph()
	{
		List<Graph> list = new List<Graph>();
		foreach (string graphedScope in m_GraphedScopes)
		{
			long timeSpanNameId = m_Session.GetTimeSpanNameId(graphedScope);
			Color scopeColour = m_Session.GetScopeColour(timeSpanNameId);
			list.Add(new Graph(graphedScope, timeSpanNameId, scopeColour, "scope", "ms", XAxisMode.Frame));
		}
		m_ScopeGraph.SetGraphs(list);
	}

	private void GraphAllButtonClicked(object sender, EventArgs e)
	{
		m_Settings.UserSetScopesViewGraphFlags = true;
		GraphAllScopes();
		WriteGraphedScopesToSettings();
	}

	private void ClearGraph(object sender, EventArgs e)
	{
		m_Settings.UserSetScopesViewGraphFlags = true;
		ClearGraph();
		WriteGraphedScopesToSettings();
	}

	public override void OnMouseWheel(int delta, Point location)
	{
		if (new Rectangle(m_ScopeGraph.Location, m_ScopeGraph.Size).Contains(location))
		{
			m_ScopeGraph.OnMouseWheel(delta, location);
		}
	}

	private void GraphResize(object sender, EventArgs e)
	{
		if (m_Settings.ScopesGraphHeight != m_ScopeGraph.Height)
		{
			m_Settings.ScopesGraphHeight = m_ScopeGraph.Height;
			m_Settings.Write();
		}
	}

	private void DataGridSelectionChanged(List<ColRow> selected_cells)
	{
		if (selected_cells.Count == 1 && selected_cells[0].Col == 5)
		{
			string name = selected_cells[0].Row.Cells[0].Value.ToString();
			ColorDialog colorDialog = new ColorDialog();
			if (colorDialog.ShowDialog(this) == DialogResult.OK)
			{
				m_Session.SetScopeColour(name, colorDialog.Color);
			}
		}
	}

	public override void OnScopeColourChanged()
	{
		UpdateDataGrid();
		m_ScopeGraph.OnScopeColourChanged(m_Session.GetScopeColour);
	}

	private void DataGridCellDoubleClicked(ColRow colrow)
	{
		GoToSourceForSelectedCell();
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		SCL.RowCollection rows = new SCL.RowCollection();
		this.m_DataGrid = new SCL.HDataGrid();
		this.panel1 = new System.Windows.Forms.Panel();
		this.button3 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button1 = new Editor.Button();
		this.label1 = new System.Windows.Forms.Label();
		this.m_FilterTextBox = new System.Windows.Forms.TextBox();
		this.splitter1 = new System.Windows.Forms.Splitter();
		this.m_ScopeGraph = new ProfilerStudy.LineGraph();
		this.m_ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.goToSourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.panel1.SuspendLayout();
		this.m_ContextMenu.SuspendLayout();
		base.SuspendLayout();
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
		this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
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
		this.m_DataGrid.Size = new System.Drawing.Size(820, 404);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 0;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.m_DataGrid.SelectionChanged += new SCL.SelectionChangedHandler(DataGridSelectionChanged);
		this.m_DataGrid.CellDoubleClicked += new SCL.CellDoubleClickedHandler(DataGridCellDoubleClicked);
		this.m_DataGrid.CellChanged += new SCL.CellChangedHandler(DataGridCellChanged);
		this.panel1.Controls.Add(this.button3);
		this.panel1.Controls.Add(this.button2);
		this.panel1.Controls.Add(this.button1);
		this.panel1.Controls.Add(this.label1);
		this.panel1.Controls.Add(this.m_FilterTextBox);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(820, 33);
		this.panel1.TabIndex = 1;
		this.button3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button3.Location = new System.Drawing.Point(457, 4);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(95, 23);
		this.button3.TabIndex = 4;
		this.button3.Text = "Clear Graph";
		this.button3.UseVisualStyleBackColor = true;
		this.button3.Click += new System.EventHandler(ClearGraph);
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(356, 4);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(95, 23);
		this.button2.TabIndex = 3;
		this.button2.Text = "Graph All";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(GraphAllButtonClicked);
		this.button1.ButtonText = "X";
		this.button1.HoverColour = System.Drawing.Color.FromArgb(245, 245, 245);
		this.button1.Image = null;
		this.button1.Location = new System.Drawing.Point(314, 6);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(20, 20);
		this.button1.TabIndex = 2;
		this.button1.Click += new System.EventHandler(ClearFilterButtonClicked);
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(24, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(33, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "Filter";
		this.m_FilterTextBox.Location = new System.Drawing.Point(59, 6);
		this.m_FilterTextBox.Name = "m_FilterTextBox";
		this.m_FilterTextBox.Size = new System.Drawing.Size(255, 20);
		this.m_FilterTextBox.TabIndex = 0;
		this.m_FilterTextBox.TextChanged += new System.EventHandler(FilterTextBoxTextChanged);
		this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.splitter1.Location = new System.Drawing.Point(0, 437);
		this.splitter1.Name = "splitter1";
		this.splitter1.Size = new System.Drawing.Size(820, 3);
		this.splitter1.TabIndex = 3;
		this.splitter1.TabStop = false;
		this.m_ScopeGraph.AlignScale = false;
		this.m_ScopeGraph.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_ScopeGraph.DrawAxis = true;
		this.m_ScopeGraph.GetGraphValuesPerFrameFunction = null;
		this.m_ScopeGraph.GetGraphValuesPerSecFunction = null;
		this.m_ScopeGraph.Location = new System.Drawing.Point(0, 440);
		this.m_ScopeGraph.Name = "m_ScopeGraph";
		this.m_ScopeGraph.Size = new System.Drawing.Size(820, 196);
		this.m_ScopeGraph.TabIndex = 4;
		this.m_ScopeGraph.Unit = "cycles";
		this.m_ScopeGraph.XAxisMode = ProfilerStudy.XAxisMode.Frame;
		this.m_ScopeGraph.YOffset = 0.0;
		this.m_ScopeGraph.YScale = 1.0;
		this.m_ScopeGraph.Resize += new System.EventHandler(GraphResize);
		this.m_ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.goToSourceToolStripMenuItem, this.copyToolStripMenuItem });
		this.m_ContextMenu.Name = "m_ContextMenu";
		this.m_ContextMenu.Size = new System.Drawing.Size(143, 48);
		this.goToSourceToolStripMenuItem.Name = "goToSourceToolStripMenuItem";
		this.goToSourceToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
		this.goToSourceToolStripMenuItem.Text = "Go to Source";
		this.goToSourceToolStripMenuItem.Click += new System.EventHandler(ContextMenuGoToSourceMenuItemClicked);
		this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
		this.copyToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
		this.copyToolStripMenuItem.Text = "Copy";
		this.copyToolStripMenuItem.Click += new System.EventHandler(ContextMenuCopyMenuItemClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_DataGrid);
		base.Controls.Add(this.splitter1);
		base.Controls.Add(this.panel1);
		base.Controls.Add(this.m_ScopeGraph);
		base.Name = "ScopesView";
		base.Size = new System.Drawing.Size(820, 636);
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		this.m_ContextMenu.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
