using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCL;
using SCLCoreCLR;

namespace FramePro;

internal class SettingsDialog : Form
{
	private Settings m_Settings;

	private bool m_SymbolPathsChanged;

	private IContainer components;

	private Button button1;

	private Button button2;

	private GroupBox groupBox1;

	private GroupBox groupBox2;

	private TextBox m_TargetFrameTimeMS;

	private Label label3;

	private HDataGrid m_ConnectionsDataGrid;

	private GroupBox groupBox3;

	private CheckBox m_SaveOnExitCheckBox;

	private CheckBox m_DisableInteractiveSessionsforLocalProfiles;

	private CheckBox m_ShowContextSwitchWarningDialogCheckBox;

	private HDataGrid m_SymbolPathsDataGrid;

	private GroupBox groupBox4;

	private Label label1;

	private TextBox m_MaxVisibleThreadsTextBox;

	private Label label2;

	private TextBox m_ThreadScopeHeightTextBox;

	private GroupBox groupBox5;

	private HDataGrid m_SourceRootPathsDataGrid;

	private Label label4;
    private Label label5;
    private ComboBox cbSourceViewer;
    private TextBox m_FrameProThreadAffinityTextBox;

	public bool SymbolPathsChanged => m_SymbolPathsChanged;

	public SettingsDialog(Settings settings)
	{
		InitializeComponent();
		m_Settings = settings;
		InitialiseConnectionsDataGrid();
		m_TargetFrameTimeMS.Text = settings.TargetFrameMS.ToString();
		m_SaveOnExitCheckBox.Checked = settings.SaveChangedQuery;
		m_DisableInteractiveSessionsforLocalProfiles.Checked = settings.DisableInteractiveSessionsForLocalProfiles;
		m_ShowContextSwitchWarningDialogCheckBox.Checked = settings.ShowContextSwitchWarningBox;
		m_MaxVisibleThreadsTextBox.Text = settings.CoreSettings.MaxVisibleThreads.ToString();
		m_ThreadScopeHeightTextBox.Text = settings.ThreadScopeHeight.ToString();
		m_FrameProThreadAffinityTextBox.Text = settings.FrameProThreadAffinity.ToString("X");
		InitialiseSymbolPathsDataGrid();
		UpdateSymbolsDataGrid();
		InitialiseSourceRootPathsDataGrid();
		UpdateSourceRootPathsDataGrid();

        if(PlatformTool.IsRunOnWine())
        {
            cbSourceViewer.Items.Add("VSCode");
            cbSourceViewer.Items.Add("Clion");
            cbSourceViewer.SelectedIndex = FindSourceViewerIndex(m_Settings.SourceViewerTool);
        }
        else
        {
            cbSourceViewer.Items.Add("VSCode");
            cbSourceViewer.Items.Add("Visual Studio");
            cbSourceViewer.Items.Add("Clion");
            cbSourceViewer.SelectedIndex = FindSourceViewerIndex(m_Settings.SourceViewerTool);
        }

    }

    private int FindSourceViewerIndex(string sourceViewer)
    {
        for(int i = 0; i < cbSourceViewer.Items.Count; i++)
        {
            if ((string)cbSourceViewer.Items[i] == sourceViewer)
            {
                return i;
            }
        }
        return 0;
    }

	private void InitialiseSymbolPathsDataGrid()
	{
		Column column = new Column();
		column.WidthMode = Column.EWidthMode.Fill;
		m_SymbolPathsDataGrid.Add(column);
	}

	private void InitialiseSourceRootPathsDataGrid()
	{
		Column column = new Column();
		column.WidthMode = Column.EWidthMode.Fill;
		m_SourceRootPathsDataGrid.Add(column);
	}

	private void UpdateSymbolsDataGrid()
	{
		m_SymbolPathsDataGrid.Clear();
		foreach (string symbolPath in m_Settings.CoreSettings.SymbolPaths)
		{
			m_SymbolPathsDataGrid.Rows.Add(symbolPath);
		}
		m_SymbolPathsDataGrid.RefreshDataGrid();
	}

	private void UpdateSourceRootPathsDataGrid()
	{
		m_SourceRootPathsDataGrid.Clear();
		foreach (string sourceRoot in m_Settings.SourceRoots)
		{
			m_SourceRootPathsDataGrid.Rows.Add(sourceRoot);
		}
		m_SourceRootPathsDataGrid.RefreshDataGrid();
	}

	private void InitialiseConnectionsDataGrid()
	{
		Column column = new Column("Name");
		column.WidthMode = Column.EWidthMode.Fill;
		m_ConnectionsDataGrid.Add(column);
		m_ConnectionsDataGrid.Add(new Column("IP"));
		m_ConnectionsDataGrid.Add(new Column("Port"));
		RowCollection rowCollection = new RowCollection();
		foreach (Connection connection in m_Settings.Connections)
		{
			rowCollection.Add(connection.m_Name, connection.m_IP, connection.m_Port);
		}
		m_ConnectionsDataGrid.Rows = rowCollection;
	}

	private void OKButtonClicked(object sender, EventArgs e)
	{
		m_Settings.TargetFrameMS = Utils.ConvertToDouble(m_TargetFrameTimeMS.Text, m_Settings.TargetFrameMS);
		List<Connection> list = new List<Connection>();
		foreach (Row row in m_ConnectionsDataGrid.Rows)
		{
			Connection connection = new Connection();
			connection.m_Name = (string)row.Cells[0].Value;
			connection.m_IP = (string)row.Cells[1].Value;
			connection.m_Port = (string)row.Cells[2].Value;
			if (!string.IsNullOrEmpty(connection.m_Name) && !string.IsNullOrEmpty(connection.m_IP) && !string.IsNullOrEmpty(connection.m_Port))
			{
				list.Add(connection);
			}
		}
		m_Settings.Connections = list;
		m_Settings.SaveChangedQuery = m_SaveOnExitCheckBox.Checked;
		m_Settings.DisableInteractiveSessionsForLocalProfiles = m_DisableInteractiveSessionsforLocalProfiles.Checked;
		m_Settings.ShowContextSwitchWarningBox = m_ShowContextSwitchWarningDialogCheckBox.Checked;


		try
		{
			int maxVisibleThreads = Convert.ToInt32(m_MaxVisibleThreadsTextBox.Text);
			m_Settings.CoreSettings.MaxVisibleThreads = maxVisibleThreads;
		}
		catch (Exception)
		{
		}
		try
		{
			int threadScopeHeight = Convert.ToInt32(m_ThreadScopeHeightTextBox.Text);
			m_Settings.ThreadScopeHeight = threadScopeHeight;
		}
		catch (Exception)
		{
		}
		List<string> list2 = new List<string>();
		foreach (Row row2 in m_SymbolPathsDataGrid.Rows)
		{
			string text = row2.Cells[0].ToString().Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list2.Add(text);
			}
		}
		if (!Utils.ListsEquals(m_Settings.CoreSettings.SymbolPaths, list2))
		{
			m_Settings.CoreSettings.SymbolPaths = list2;
			m_SymbolPathsChanged = true;
		}
		List<string> list3 = new List<string>();
		foreach (Row row3 in m_SourceRootPathsDataGrid.Rows)
		{
			string text2 = row3.Cells[0].ToString().Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				list3.Add(text2);
			}
		}
		if (!Utils.ListsEquals(m_Settings.SourceRoots, list3))
		{
			m_Settings.SourceRoots = list3;
		}
		ulong value = 0uL;
		if (Misc.HexStringToULong(m_FrameProThreadAffinityTextBox.Text, ref value) && value != 0L && m_Settings.FrameProThreadAffinity != value)
		{
			m_Settings.FrameProThreadAffinity = value;
			Utils.SetProcessAffinity(m_Settings.FrameProThreadAffinity);
		}
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
            SCL.RowCollection rowCollection1 = new SCL.RowCollection();
            SCL.RowCollection rowCollection2 = new SCL.RowCollection();
            SCL.RowCollection rowCollection3 = new SCL.RowCollection();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.m_ConnectionsDataGrid = new SCL.HDataGrid();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.m_TargetFrameTimeMS = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cbSourceViewer = new System.Windows.Forms.ComboBox();
            this.m_DisableInteractiveSessionsforLocalProfiles = new System.Windows.Forms.CheckBox();
            this.m_ShowContextSwitchWarningDialogCheckBox = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.m_FrameProThreadAffinityTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.m_ThreadScopeHeightTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.m_MaxVisibleThreadsTextBox = new System.Windows.Forms.TextBox();
            this.m_SaveOnExitCheckBox = new System.Windows.Forms.CheckBox();
            this.m_SymbolPathsDataGrid = new SCL.HDataGrid();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.m_SourceRootPathsDataGrid = new SCL.HDataGrid();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(710, 997);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 32);
            this.button1.TabIndex = 4;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button2.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(588, 997);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(112, 32);
            this.button2.TabIndex = 5;
            this.button2.Text = "Ok";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OKButtonClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.m_ConnectionsDataGrid);
            this.groupBox1.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(18, 40);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(804, 180);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connections";
            // 
            // m_ConnectionsDataGrid
            // 
            this.m_ConnectionsDataGrid.AddEmptyRow = true;
            this.m_ConnectionsDataGrid.AlternateRowColours = true;
            this.m_ConnectionsDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_ConnectionsDataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_ConnectionsDataGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_ConnectionsDataGrid.CanAddRemoveRows = true;
            this.m_ConnectionsDataGrid.CanRenameCell = true;
            this.m_ConnectionsDataGrid.CanResizeColumnTitleBar = false;
            this.m_ConnectionsDataGrid.CanResizeRows = false;
            this.m_ConnectionsDataGrid.CanResizeRowTitleBar = false;
            this.m_ConnectionsDataGrid.CanShowHideColumns = true;
            this.m_ConnectionsDataGrid.CanSortByColumn = false;
            this.m_ConnectionsDataGrid.ClearSelectionOnMouseLeave = false;
            this.m_ConnectionsDataGrid.ColumnTitlePanelVisible = true;
            this.m_ConnectionsDataGrid.DarkRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.m_ConnectionsDataGrid.DrawColumnLines = true;
            this.m_ConnectionsDataGrid.DrawLastColumnLine = false;
            this.m_ConnectionsDataGrid.DrawRowLines = false;
            this.m_ConnectionsDataGrid.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_ConnectionsDataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
            this.m_ConnectionsDataGrid.HighlightedRowBoxVisible = false;
            this.m_ConnectionsDataGrid.HighlightRow = false;
            this.m_ConnectionsDataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(255)))));
            this.m_ConnectionsDataGrid.HighlightSelectedRow = false;
            this.m_ConnectionsDataGrid.HorizontalTextOffset = 4;
            this.m_ConnectionsDataGrid.LightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_ConnectionsDataGrid.Location = new System.Drawing.Point(9, 26);
            this.m_ConnectionsDataGrid.Margin = new System.Windows.Forms.Padding(6);
            this.m_ConnectionsDataGrid.MoveCellsEnabled = false;
            this.m_ConnectionsDataGrid.Name = "m_ConnectionsDataGrid";
            this.m_ConnectionsDataGrid.PadEmptyRows = false;
            this.m_ConnectionsDataGrid.ReadOnly = false;
            this.m_ConnectionsDataGrid.RowHeightPadding = 3;
            this.m_ConnectionsDataGrid.Rows = rowCollection1;
            this.m_ConnectionsDataGrid.RowTitelPanelVisible = false;
            this.m_ConnectionsDataGrid.ScrollColumnsHorz = false;
            this.m_ConnectionsDataGrid.SelectByRow = false;
            this.m_ConnectionsDataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(180)))), ((int)(((byte)(250)))));
            this.m_ConnectionsDataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(255)))));
            this.m_ConnectionsDataGrid.SelectNextCellAfterEdit = true;
            this.m_ConnectionsDataGrid.ShowSelectBox = true;
            this.m_ConnectionsDataGrid.Size = new System.Drawing.Size(785, 145);
            this.m_ConnectionsDataGrid.SlideDrag = true;
            this.m_ConnectionsDataGrid.TabIndex = 4;
            this.m_ConnectionsDataGrid.WindowColour = System.Drawing.SystemColors.Window;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.m_TargetFrameTimeMS);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(18, 228);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(804, 109);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Frames";
            // 
            // m_TargetFrameTimeMS
            // 
            this.m_TargetFrameTimeMS.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_TargetFrameTimeMS.Location = new System.Drawing.Point(246, 48);
            this.m_TargetFrameTimeMS.Margin = new System.Windows.Forms.Padding(4);
            this.m_TargetFrameTimeMS.Name = "m_TargetFrameTimeMS";
            this.m_TargetFrameTimeMS.Size = new System.Drawing.Size(190, 26);
            this.m_TargetFrameTimeMS.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(60, 53);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(192, 20);
            this.label3.TabIndex = 1;
            this.label3.Text = "Target Frame Time (ms)";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.cbSourceViewer);
            this.groupBox3.Controls.Add(this.m_DisableInteractiveSessionsforLocalProfiles);
            this.groupBox3.Controls.Add(this.m_ShowContextSwitchWarningDialogCheckBox);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.m_FrameProThreadAffinityTextBox);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.m_ThreadScopeHeightTextBox);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.m_MaxVisibleThreadsTextBox);
            this.groupBox3.Controls.Add(this.m_SaveOnExitCheckBox);
            this.groupBox3.Location = new System.Drawing.Point(21, 738);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(804, 251);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Misc";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(394, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(134, 18);
            this.label5.TabIndex = 13;
            this.label5.Text = "Source Viewer:";
            // 
            // cbSourceViewer
            // 
            this.cbSourceViewer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSourceViewer.FormattingEnabled = true;
            this.cbSourceViewer.Location = new System.Drawing.Point(534, 38);
            this.cbSourceViewer.Name = "cbSourceViewer";
            this.cbSourceViewer.Size = new System.Drawing.Size(256, 26);
            this.cbSourceViewer.TabIndex = 12;
            this.cbSourceViewer.SelectedValueChanged += new System.EventHandler(this.cbSourceViewer_SelectedValueChanged);
            // 
            // m_DisableInteractiveSessionsforLocalProfiles
            // 
            this.m_DisableInteractiveSessionsforLocalProfiles.AutoSize = true;
            this.m_DisableInteractiveSessionsforLocalProfiles.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_DisableInteractiveSessionsforLocalProfiles.Location = new System.Drawing.Point(64, 104);
            this.m_DisableInteractiveSessionsforLocalProfiles.Margin = new System.Windows.Forms.Padding(4);
            this.m_DisableInteractiveSessionsforLocalProfiles.Name = "m_DisableInteractiveSessionsforLocalProfiles";
            this.m_DisableInteractiveSessionsforLocalProfiles.Size = new System.Drawing.Size(372, 24);
            this.m_DisableInteractiveSessionsforLocalProfiles.TabIndex = 1;
            this.m_DisableInteractiveSessionsforLocalProfiles.Text = "Disable Interactive Session for Local Profiles";
            this.m_DisableInteractiveSessionsforLocalProfiles.UseVisualStyleBackColor = true;
            this.m_DisableInteractiveSessionsforLocalProfiles.Visible = false;
            // 
            // m_ShowContextSwitchWarningDialogCheckBox
            // 
            this.m_ShowContextSwitchWarningDialogCheckBox.AutoSize = true;
            this.m_ShowContextSwitchWarningDialogCheckBox.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_ShowContextSwitchWarningDialogCheckBox.Location = new System.Drawing.Point(64, 72);
            this.m_ShowContextSwitchWarningDialogCheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.m_ShowContextSwitchWarningDialogCheckBox.Name = "m_ShowContextSwitchWarningDialogCheckBox";
            this.m_ShowContextSwitchWarningDialogCheckBox.Size = new System.Drawing.Size(313, 24);
            this.m_ShowContextSwitchWarningDialogCheckBox.TabIndex = 3;
            this.m_ShowContextSwitchWarningDialogCheckBox.Text = "Show Context Switch Warning Dialog";
            this.m_ShowContextSwitchWarningDialogCheckBox.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(424, 180);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(196, 20);
            this.label4.TabIndex = 11;
            this.label4.Text = "FramePro Thread Affinity";
            // 
            // m_FrameProThreadAffinityTextBox
            // 
            this.m_FrameProThreadAffinityTextBox.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_FrameProThreadAffinityTextBox.Location = new System.Drawing.Point(632, 176);
            this.m_FrameProThreadAffinityTextBox.Name = "m_FrameProThreadAffinityTextBox";
            this.m_FrameProThreadAffinityTextBox.Size = new System.Drawing.Size(158, 26);
            this.m_FrameProThreadAffinityTextBox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(51, 212);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 20);
            this.label2.TabIndex = 9;
            this.label2.Text = "Thread Scope Height";
            // 
            // m_ThreadScopeHeightTextBox
            // 
            this.m_ThreadScopeHeightTextBox.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_ThreadScopeHeightTextBox.Location = new System.Drawing.Point(230, 208);
            this.m_ThreadScopeHeightTextBox.Name = "m_ThreadScopeHeightTextBox";
            this.m_ThreadScopeHeightTextBox.Size = new System.Drawing.Size(112, 26);
            this.m_ThreadScopeHeightTextBox.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(60, 180);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(161, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Max Visible Threads";
            // 
            // m_MaxVisibleThreadsTextBox
            // 
            this.m_MaxVisibleThreadsTextBox.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_MaxVisibleThreadsTextBox.Location = new System.Drawing.Point(230, 176);
            this.m_MaxVisibleThreadsTextBox.Name = "m_MaxVisibleThreadsTextBox";
            this.m_MaxVisibleThreadsTextBox.Size = new System.Drawing.Size(112, 26);
            this.m_MaxVisibleThreadsTextBox.TabIndex = 6;
            // 
            // m_SaveOnExitCheckBox
            // 
            this.m_SaveOnExitCheckBox.AutoSize = true;
            this.m_SaveOnExitCheckBox.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_SaveOnExitCheckBox.Location = new System.Drawing.Point(64, 40);
            this.m_SaveOnExitCheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.m_SaveOnExitCheckBox.Name = "m_SaveOnExitCheckBox";
            this.m_SaveOnExitCheckBox.Size = new System.Drawing.Size(202, 24);
            this.m_SaveOnExitCheckBox.TabIndex = 0;
            this.m_SaveOnExitCheckBox.Text = "Prompt to save on exit";
            this.m_SaveOnExitCheckBox.UseVisualStyleBackColor = true;
            // 
            // m_SymbolPathsDataGrid
            // 
            this.m_SymbolPathsDataGrid.AddEmptyRow = true;
            this.m_SymbolPathsDataGrid.AlternateRowColours = true;
            this.m_SymbolPathsDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_SymbolPathsDataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_SymbolPathsDataGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_SymbolPathsDataGrid.CanAddRemoveRows = true;
            this.m_SymbolPathsDataGrid.CanRenameCell = true;
            this.m_SymbolPathsDataGrid.CanResizeColumnTitleBar = false;
            this.m_SymbolPathsDataGrid.CanResizeRows = false;
            this.m_SymbolPathsDataGrid.CanResizeRowTitleBar = false;
            this.m_SymbolPathsDataGrid.CanShowHideColumns = true;
            this.m_SymbolPathsDataGrid.CanSortByColumn = true;
            this.m_SymbolPathsDataGrid.ClearSelectionOnMouseLeave = false;
            this.m_SymbolPathsDataGrid.ColumnTitlePanelVisible = false;
            this.m_SymbolPathsDataGrid.DarkRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.m_SymbolPathsDataGrid.DrawColumnLines = true;
            this.m_SymbolPathsDataGrid.DrawLastColumnLine = false;
            this.m_SymbolPathsDataGrid.DrawRowLines = false;
            this.m_SymbolPathsDataGrid.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_SymbolPathsDataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
            this.m_SymbolPathsDataGrid.HighlightedRowBoxVisible = false;
            this.m_SymbolPathsDataGrid.HighlightRow = false;
            this.m_SymbolPathsDataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(255)))));
            this.m_SymbolPathsDataGrid.HighlightSelectedRow = false;
            this.m_SymbolPathsDataGrid.HorizontalTextOffset = 4;
            this.m_SymbolPathsDataGrid.LightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_SymbolPathsDataGrid.Location = new System.Drawing.Point(9, 32);
            this.m_SymbolPathsDataGrid.Margin = new System.Windows.Forms.Padding(6);
            this.m_SymbolPathsDataGrid.MoveCellsEnabled = false;
            this.m_SymbolPathsDataGrid.Name = "m_SymbolPathsDataGrid";
            this.m_SymbolPathsDataGrid.PadEmptyRows = false;
            this.m_SymbolPathsDataGrid.ReadOnly = false;
            this.m_SymbolPathsDataGrid.RowHeightPadding = 3;
            this.m_SymbolPathsDataGrid.Rows = rowCollection2;
            this.m_SymbolPathsDataGrid.RowTitelPanelVisible = false;
            this.m_SymbolPathsDataGrid.ScrollColumnsHorz = false;
            this.m_SymbolPathsDataGrid.SelectByRow = false;
            this.m_SymbolPathsDataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(180)))), ((int)(((byte)(250)))));
            this.m_SymbolPathsDataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(255)))));
            this.m_SymbolPathsDataGrid.SelectNextCellAfterEdit = true;
            this.m_SymbolPathsDataGrid.ShowSelectBox = true;
            this.m_SymbolPathsDataGrid.Size = new System.Drawing.Size(785, 146);
            this.m_SymbolPathsDataGrid.SlideDrag = false;
            this.m_SymbolPathsDataGrid.TabIndex = 9;
            this.m_SymbolPathsDataGrid.WindowColour = System.Drawing.SystemColors.Window;
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.m_SymbolPathsDataGrid);
            this.groupBox4.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox4.Location = new System.Drawing.Point(18, 346);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(804, 187);
            this.groupBox4.TabIndex = 10;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Symbol Paths";
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.m_SourceRootPathsDataGrid);
            this.groupBox5.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox5.Location = new System.Drawing.Point(18, 543);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox5.Size = new System.Drawing.Size(804, 187);
            this.groupBox5.TabIndex = 11;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Source Root Paths";
            // 
            // m_SourceRootPathsDataGrid
            // 
            this.m_SourceRootPathsDataGrid.AddEmptyRow = true;
            this.m_SourceRootPathsDataGrid.AlternateRowColours = true;
            this.m_SourceRootPathsDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.m_SourceRootPathsDataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.m_SourceRootPathsDataGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.m_SourceRootPathsDataGrid.CanAddRemoveRows = true;
            this.m_SourceRootPathsDataGrid.CanRenameCell = true;
            this.m_SourceRootPathsDataGrid.CanResizeColumnTitleBar = false;
            this.m_SourceRootPathsDataGrid.CanResizeRows = false;
            this.m_SourceRootPathsDataGrid.CanResizeRowTitleBar = false;
            this.m_SourceRootPathsDataGrid.CanShowHideColumns = true;
            this.m_SourceRootPathsDataGrid.CanSortByColumn = true;
            this.m_SourceRootPathsDataGrid.ClearSelectionOnMouseLeave = false;
            this.m_SourceRootPathsDataGrid.ColumnTitlePanelVisible = false;
            this.m_SourceRootPathsDataGrid.DarkRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this.m_SourceRootPathsDataGrid.DrawColumnLines = true;
            this.m_SourceRootPathsDataGrid.DrawLastColumnLine = false;
            this.m_SourceRootPathsDataGrid.DrawRowLines = false;
            this.m_SourceRootPathsDataGrid.Font = new System.Drawing.Font("Monaco", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.m_SourceRootPathsDataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
            this.m_SourceRootPathsDataGrid.HighlightedRowBoxVisible = false;
            this.m_SourceRootPathsDataGrid.HighlightRow = false;
            this.m_SourceRootPathsDataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(225)))), ((int)(((byte)(255)))));
            this.m_SourceRootPathsDataGrid.HighlightSelectedRow = false;
            this.m_SourceRootPathsDataGrid.HorizontalTextOffset = 4;
            this.m_SourceRootPathsDataGrid.LightRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.m_SourceRootPathsDataGrid.Location = new System.Drawing.Point(9, 32);
            this.m_SourceRootPathsDataGrid.Margin = new System.Windows.Forms.Padding(6);
            this.m_SourceRootPathsDataGrid.MoveCellsEnabled = false;
            this.m_SourceRootPathsDataGrid.Name = "m_SourceRootPathsDataGrid";
            this.m_SourceRootPathsDataGrid.PadEmptyRows = false;
            this.m_SourceRootPathsDataGrid.ReadOnly = false;
            this.m_SourceRootPathsDataGrid.RowHeightPadding = 3;
            this.m_SourceRootPathsDataGrid.Rows = rowCollection3;
            this.m_SourceRootPathsDataGrid.RowTitelPanelVisible = false;
            this.m_SourceRootPathsDataGrid.ScrollColumnsHorz = false;
            this.m_SourceRootPathsDataGrid.SelectByRow = false;
            this.m_SourceRootPathsDataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(((int)(((byte)(188)))), ((int)(((byte)(180)))), ((int)(((byte)(250)))));
            this.m_SourceRootPathsDataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(210)))), ((int)(((byte)(255)))));
            this.m_SourceRootPathsDataGrid.SelectNextCellAfterEdit = true;
            this.m_SourceRootPathsDataGrid.ShowSelectBox = true;
            this.m_SourceRootPathsDataGrid.Size = new System.Drawing.Size(785, 146);
            this.m_SourceRootPathsDataGrid.SlideDrag = false;
            this.m_SourceRootPathsDataGrid.TabIndex = 9;
            this.m_SourceRootPathsDataGrid.WindowColour = System.Drawing.SystemColors.Window;
            // 
            // SettingsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(840, 1047);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FramePro Settings";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.ResumeLayout(false);

	}

    private void cbSourceViewer_SelectedValueChanged(object sender, EventArgs e)
    {
        var viewer = (string)cbSourceViewer.SelectedItem;
        if (viewer != m_Settings.SourceViewerTool)
        {
            m_Settings.SourceViewerTool = viewer;
            m_Settings.Write();
        }
    }
}
