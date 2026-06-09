using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProfilerStudy.Properties;
using SCL;

namespace ProfilerStudy;

internal class ThreadFilterForm : Form
{
	private Column m_NameColumn = new Column("Name");

	private Column m_CollapsedColumn = new Column("Collapsed");

	private Column m_VisibleColumn = new Column("Visible");

	private Settings m_Settings;

	private Session m_Session;

	private float m_DPIScale;

	private IContainer components;

	private HDataGrid m_DataGrid;

	private Button button1;

	private Button button2;

	private Button button3;

	private Label label1;

	public ThreadFilterForm(Settings settings, Session session)
	{
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		m_Settings = settings;
		m_Session = session;
		InitialiseDataGrid();
		SetupDataGrid(reset: false);
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void InitialiseDataGrid()
	{
		m_NameColumn.WidthMode = Column.EWidthMode.Fill;
		m_NameColumn.ReadOnly = true;
		m_DataGrid.Add(m_NameColumn);
		m_CollapsedColumn.Width = ScaleDPI(70);
		m_CollapsedColumn.BoolTrueImage = Resources.tick;
		m_DataGrid.Add(m_CollapsedColumn);
		m_VisibleColumn.Width = ScaleDPI(70);
		m_VisibleColumn.BoolTrueImage = Resources.tick;
		m_DataGrid.Add(m_VisibleColumn);
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void SetupDataGrid(bool reset)
	{
		string name = m_Session.SessionDetails.m_Name;
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(name);
		if (threadFilter == null || reset)
		{
			threadFilter = Utils.CreateNewThreadFilter(m_Session);
		}
		m_DataGrid.Clear();
		foreach (ThreadFilterRow thread in threadFilter.m_Threads)
		{
			m_DataGrid.Rows.Add(thread.m_Name, thread.m_Collapsed, thread.m_Visible);
		}
		m_DataGrid.RefreshDataGrid();
		m_DataGrid.Refresh();
	}

	private void ResetButtonClicked(object sender, EventArgs e)
	{
		SetupDataGrid(reset: true);
	}

	public List<ThreadFilterRow> GetThreadFilters()
	{
		List<ThreadFilterRow> list = new List<ThreadFilterRow>();
		ThreadFilter threadFilter = m_Settings.GetThreadFilter(m_Session.SessionDetails.m_Name);
		foreach (Row row in m_DataGrid.Rows)
		{
			string thread_name = (string)row.Cells[0].Value;
			bool collapsed = (bool)row.Cells[1].Value;
			bool visible = (bool)row.Cells[2].Value;
			ThreadFilterRow threadFilterRow = new ThreadFilterRow(threadFilter.GetThreadFilterRow(thread_name));
			threadFilterRow.m_Visible = visible;
			threadFilterRow.m_Collapsed = collapsed;
			list.Add(threadFilterRow);
		}
		return list;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerStudy.ThreadFilterForm));
		this.m_DataGrid = new SCL.HDataGrid();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button3 = new System.Windows.Forms.Button();
		this.label1 = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.m_DataGrid.AddEmptyRow = false;
		this.m_DataGrid.AlternateRowColours = true;
		this.m_DataGrid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_DataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
		this.m_DataGrid.CanAddRemoveRows = false;
		this.m_DataGrid.CanRenameCell = false;
		this.m_DataGrid.CanResizeColumnTitleBar = false;
		this.m_DataGrid.CanResizeRows = false;
		this.m_DataGrid.CanResizeRowTitleBar = false;
		this.m_DataGrid.CanShowHideColumns = true;
		this.m_DataGrid.CanSortByColumn = false;
		this.m_DataGrid.ClearSelectionOnMouseLeave = false;
		this.m_DataGrid.ColumnTitlePanelVisible = true;
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_DataGrid.DrawColumnLines = true;
		this.m_DataGrid.DrawLastColumnLine = false;
		this.m_DataGrid.DrawRowLines = false;
		this.m_DataGrid.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
		this.m_DataGrid.HighlightedRowBoxVisible = false;
		this.m_DataGrid.HighlightRow = true;
		this.m_DataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(225, 225, 255);
		this.m_DataGrid.HighlightSelectedRow = false;
		this.m_DataGrid.HorizontalTextOffset = 4;
		this.m_DataGrid.LightRowColour = System.Drawing.Color.FromArgb(235, 235, 235);
		this.m_DataGrid.Location = new System.Drawing.Point(1, 34);
		this.m_DataGrid.MoveCellsEnabled = false;
		this.m_DataGrid.Name = "m_DataGrid";
		this.m_DataGrid.PadEmptyRows = false;
		this.m_DataGrid.ReadOnly = false;
		this.m_DataGrid.RowHeightPadding = 3;
		this.m_DataGrid.Rows = rows;
		this.m_DataGrid.RowTitelPanelVisible = false;
		this.m_DataGrid.ScrollColumnsHorz = false;
		this.m_DataGrid.SelectByRow = false;
		this.m_DataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(188, 180, 250);
		this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_DataGrid.SelectNextCellAfterEdit = true;
		this.m_DataGrid.ShowSelectBox = false;
		this.m_DataGrid.Size = new System.Drawing.Size(503, 290);
		this.m_DataGrid.SlideDrag = true;
		this.m_DataGrid.TabIndex = 0;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(377, 330);
		this.button1.Margin = new System.Windows.Forms.Padding(2);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(56, 19);
		this.button1.TabIndex = 0;
		this.button1.Text = "OK";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(438, 330);
		this.button2.Margin = new System.Windows.Forms.Padding(2);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(56, 19);
		this.button2.TabIndex = 1;
		this.button2.Text = "Cancel";
		this.button2.UseVisualStyleBackColor = true;
		this.button3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.button3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button3.Location = new System.Drawing.Point(438, 9);
		this.button3.Margin = new System.Windows.Forms.Padding(2);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(56, 19);
		this.button3.TabIndex = 3;
		this.button3.Text = "Reset";
		this.button3.UseVisualStyleBackColor = true;
		this.button3.Click += new System.EventHandler(ResetButtonClicked);
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(9, 12);
		this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(206, 13);
		this.label1.TabIndex = 4;
		this.label1.Text = "Click and drag rows to reorder threads";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(503, 359);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.button3);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.m_DataGrid);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(2);
		base.Name = "ThreadFilterForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Threads";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
