using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FramePro.Properties;
using SCL;

namespace FramePro;

public class CustomStatSelector : Form
{
	public struct CustomStat
	{
		public string m_Name;

		public bool m_Visible;
	}

	private Column m_NameColumn;

	private Column m_VisibleColumn;

	private float m_DPIScale;

	private IContainer components;

	private HDataGrid m_DataGrid;

	public event CustomStatVisibilityChangedHandler CustomStatVisibilityChanged;

	public CustomStatSelector()
	{
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		InitialiseDataGrid();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void InitialiseDataGrid()
	{
		m_NameColumn = new Column();
		m_NameColumn.Name = "Name";
		m_NameColumn.WidthMode = Column.EWidthMode.Fill;
		m_NameColumn.ReadOnly = true;
		m_DataGrid.Columns.Add(m_NameColumn);
		m_VisibleColumn = new Column();
		m_VisibleColumn.Name = "Visible";
		m_VisibleColumn.Width = ScaleDPI(60);
		m_VisibleColumn.BoolTrueImage = Utils.To96Dpi(Resources.tick);
		m_DataGrid.Columns.Add(m_VisibleColumn);
	}

	public void Setup(List<CustomStat> stats)
	{
		m_DataGrid.Clear();
		foreach (CustomStat stat in stats)
		{
			m_DataGrid.Rows.Add(stat.m_Name, stat.m_Visible);
		}
		m_DataGrid.RefreshDataGrid();
	}

	private void CellChanged(ICollection<ColRow> sel_cells)
	{
		foreach (ColRow sel_cell in sel_cells)
		{
			string name = (string)sel_cell.Row.Cells[0].Value;
			bool visible = (bool)sel_cell.Row.Cells[1].Value;
			if (this.CustomStatVisibilityChanged != null)
			{
				this.CustomStatVisibilityChanged(name, visible);
			}
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
		SCL.RowCollection rows = new SCL.RowCollection();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.CustomStatSelector));
		this.m_DataGrid = new SCL.HDataGrid();
		base.SuspendLayout();
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
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_DataGrid.DrawColumnLines = true;
		this.m_DataGrid.DrawLastColumnLine = false;
		this.m_DataGrid.DrawRowLines = false;
		this.m_DataGrid.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
		this.m_DataGrid.HighlightedRowBoxVisible = false;
		this.m_DataGrid.HighlightRow = false;
		this.m_DataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(225, 225, 255);
		this.m_DataGrid.HighlightSelectedRow = false;
		this.m_DataGrid.HorizontalTextOffset = 4;
		this.m_DataGrid.LightRowColour = System.Drawing.Color.FromArgb(235, 235, 235);
		this.m_DataGrid.Location = new System.Drawing.Point(0, 0);
		this.m_DataGrid.MoveCellsEnabled = false;
		this.m_DataGrid.Name = "m_DataGrid";
		this.m_DataGrid.PadEmptyRows = false;
		this.m_DataGrid.ReadOnly = false;
		this.m_DataGrid.RowHeightPadding = 3;
		this.m_DataGrid.Rows = rows;
		this.m_DataGrid.RowTitelPanelVisible = true;
		this.m_DataGrid.ScrollColumnsHorz = false;
		this.m_DataGrid.SelectByRow = false;
		this.m_DataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(188, 180, 250);
		this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_DataGrid.SelectNextCellAfterEdit = true;
		this.m_DataGrid.ShowSelectBox = true;
		this.m_DataGrid.Size = new System.Drawing.Size(284, 261);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 0;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.m_DataGrid.CellChanged += new SCL.CellChangedHandler(CellChanged);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(284, 261);
		base.Controls.Add(this.m_DataGrid);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "CustomStatSelector";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Custom Stats";
		base.ResumeLayout(false);
	}
}
