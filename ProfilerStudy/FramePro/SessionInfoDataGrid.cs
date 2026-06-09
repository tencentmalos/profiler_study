using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace ProfilerStudy;

public class SessionInfoDataGrid : UserControl
{
	private float m_DPIScale;

	private IContainer components;

	private HDataGrid m_DataGrid;

	private Panel panel1;

	private Label m_TitleLabel;

	private Panel panel2;

	private Panel panel3;

	public string Title
	{
		get
		{
			return m_TitleLabel.Text;
		}
		set
		{
			m_TitleLabel.Text = value;
			CantreTitleLabel();
			Refresh();
		}
	}

	public HDataGrid DataGrid => m_DataGrid;

	public int PreferredHeight => m_DataGrid.Bottom;

	public SessionInfoDataGrid()
	{
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		Column column = new Column("Name")
		{
			WidthMode = Column.EWidthMode.Fill
		};
		m_DataGrid.Add(column);
		Column column2 = new Column("Value")
		{
			Width = ScaleDPI(120)
		};
		m_DataGrid.Add(column2);
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void CantreTitleLabel()
	{
		int num = (base.ClientSize.Width - m_TitleLabel.Width) / 2;
		int num2 = (panel1.ClientSize.Height - m_TitleLabel.Height) / 2;
		m_TitleLabel.Location = new Point(num, num2);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		CantreTitleLabel();
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
		this.panel1 = new System.Windows.Forms.Panel();
		this.m_TitleLabel = new System.Windows.Forms.Label();
		this.panel2 = new System.Windows.Forms.Panel();
		this.m_DataGrid = new SCL.HDataGrid();
		this.panel3 = new System.Windows.Forms.Panel();
		this.panel1.SuspendLayout();
		this.panel3.SuspendLayout();
		base.SuspendLayout();
		this.panel1.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.panel1.Controls.Add(this.m_TitleLabel);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(282, 20);
		this.panel1.TabIndex = 6;
		this.m_TitleLabel.AutoSize = true;
		this.m_TitleLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TitleLabel.Location = new System.Drawing.Point(129, 4);
		this.m_TitleLabel.Name = "m_TitleLabel";
		this.m_TitleLabel.Size = new System.Drawing.Size(29, 13);
		this.m_TitleLabel.TabIndex = 0;
		this.m_TitleLabel.Text = "Title";
		this.panel2.BackColor = System.Drawing.SystemColors.ControlDark;
		this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
		this.panel2.Location = new System.Drawing.Point(282, 0);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(2, 129);
		this.panel2.TabIndex = 8;
		this.m_DataGrid.AddEmptyRow = false;
		this.m_DataGrid.AlternateRowColours = true;
		this.m_DataGrid.BackColor = System.Drawing.Color.FromArgb(219, 219, 219);
		this.m_DataGrid.CanAddRemoveRows = false;
		this.m_DataGrid.CanRenameCell = false;
		this.m_DataGrid.CanResizeColumnTitleBar = false;
		this.m_DataGrid.CanResizeRows = false;
		this.m_DataGrid.CanResizeRowTitleBar = false;
		this.m_DataGrid.CanShowHideColumns = true;
		this.m_DataGrid.CanSortByColumn = false;
		this.m_DataGrid.ClearSelectionOnMouseLeave = false;
		this.m_DataGrid.ColumnTitlePanelVisible = false;
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.WhiteSmoke;
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
		this.m_DataGrid.LightRowColour = System.Drawing.Color.WhiteSmoke;
		this.m_DataGrid.Location = new System.Drawing.Point(0, 0);
		this.m_DataGrid.MoveCellsEnabled = false;
		this.m_DataGrid.Name = "m_DataGrid";
		this.m_DataGrid.PadEmptyRows = true;
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
		this.m_DataGrid.Size = new System.Drawing.Size(282, 105);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 5;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.panel3.Controls.Add(this.m_DataGrid);
		this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel3.Location = new System.Drawing.Point(0, 20);
		this.panel3.Name = "panel3";
		this.panel3.Size = new System.Drawing.Size(282, 109);
		this.panel3.TabIndex = 9;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.panel3);
		base.Controls.Add(this.panel1);
		base.Controls.Add(this.panel2);
		base.Name = "SessionInfoDataGrid";
		base.Size = new System.Drawing.Size(284, 129);
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		this.panel3.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
