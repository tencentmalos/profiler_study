using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace ProfilerStudy;

public class FrameInfoPanel : UserControl
{
	private int m_SelectedRangeStart = -1;

	private int m_SelectedRangeEnd = -1;

	private int m_UpdateStatsSelectedRangeStart = -1;

	private int m_UpdateStatsSelectedRangeEnd = -1;

	private const int m_ValueColumnWidth = 50;

	private IContainer components;

	private Label m_TimeLabel;

	private Label label1;

	private HDataGrid m_FramesDataGrid;

	private Panel panel1;

	private Panel panel2;

	public static int ValueColumnWidth => 50;

	public FrameInfoPanel()
	{
		InitializeComponent();
		InitialiseDataGrid();
	}

	private void InitialiseDataGrid()
	{
		m_FramesDataGrid.LightRowColour = Colours.DataGridBackColour;
		m_FramesDataGrid.DarkRowColour = Colours.DataGridBackColour;
		Column column = new Column();
		column.WidthMode = Column.EWidthMode.Fill;
		Column column2 = new Column();
		float dPIScale = MainForm.DPIScale;
		column2.Width = (int)(50f * dPIScale);
		m_FramesDataGrid.Add(column);
		m_FramesDataGrid.Add(column2);
		m_FramesDataGrid.Rows.Add("FPS", "-");
		m_FramesDataGrid.Rows.Add("Max", "-");
		m_FramesDataGrid.Rows.Add("In Budget", "0 %");
	}

	public void UpdateStats(Session session)
	{
		long timerFrequency = session.TimerFrequency;
		bool flag = false;
		long num = 0L;
		long num2 = 0L;
		int num3 = 0;
		if (m_SelectedRangeStart != -1 && m_SelectedRangeEnd != -1)
		{
			if (m_UpdateStatsSelectedRangeStart != m_SelectedRangeStart || m_UpdateStatsSelectedRangeEnd != m_SelectedRangeEnd)
			{
				m_UpdateStatsSelectedRangeStart = m_SelectedRangeStart;
				m_UpdateStatsSelectedRangeEnd = m_SelectedRangeEnd;
				flag = true;
				num = session.GetAverageFrameTime(m_SelectedRangeStart, m_SelectedRangeEnd);
				num2 = session.GetMaxFrameTime(m_SelectedRangeStart, m_SelectedRangeEnd);
				int framesInBudget = session.GetFramesInBudget(m_SelectedRangeStart, m_SelectedRangeEnd);
				int num4 = m_SelectedRangeEnd + 1 - m_SelectedRangeStart;
				num3 = ((num4 != 0) ? (framesInBudget * 100 / num4) : 100);
			}
		}
		else
		{
			flag = true;
			num = session.AverageFrameTime;
			num2 = session.MaxFrameTime;
			int frameCount = session.FrameCount;
			num3 = ((frameCount != 0) ? (session.FramesInBudget * 100 / frameCount) : 100);
		}
		if (flag)
		{
			float num5 = ((timerFrequency != 0L) ? ((float)((double)(num * 1000) / (double)timerFrequency)) : 0f);
			m_TimeLabel.Text = num5.ToString("0.##") + " ms";
			m_FramesDataGrid.Rows[0].Cells[1].Value = (1000.0 / (double)num5).ToString("0.##");
			m_FramesDataGrid.Rows[1].Cells[1].Value = ((double)(num2 * 1000) / (double)timerFrequency).ToString("0.##");
			m_FramesDataGrid.Rows[2].Cells[1].Value = num3.ToString("0.#") + " %";
			m_FramesDataGrid.Refresh();
		}
	}

	public void SetSelectedRange(int start_frame_index, int end_frame_index, Session session)
	{
		if (m_SelectedRangeStart != start_frame_index || m_SelectedRangeEnd != end_frame_index)
		{
			m_SelectedRangeStart = start_frame_index;
			m_SelectedRangeEnd = end_frame_index;
			UpdateStats(session);
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
		this.m_TimeLabel = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.m_FramesDataGrid = new SCL.HDataGrid();
		this.panel1 = new System.Windows.Forms.Panel();
		this.panel2 = new System.Windows.Forms.Panel();
		this.panel1.SuspendLayout();
		this.panel2.SuspendLayout();
		base.SuspendLayout();
		this.m_TimeLabel.AutoSize = true;
		this.m_TimeLabel.Font = new System.Drawing.Font("Consolas", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TimeLabel.Location = new System.Drawing.Point(6, 23);
		this.m_TimeLabel.Name = "m_TimeLabel";
		this.m_TimeLabel.Size = new System.Drawing.Size(38, 28);
		this.m_TimeLabel.TabIndex = 5;
		this.m_TimeLabel.Text = "--";
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(8, 8);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(82, 13);
		this.label1.TabIndex = 4;
		this.label1.Text = "Average Frame";
		this.m_FramesDataGrid.AddEmptyRow = false;
		this.m_FramesDataGrid.AlternateRowColours = true;
		this.m_FramesDataGrid.BackColor = System.Drawing.SystemColors.Control;
		this.m_FramesDataGrid.CanAddRemoveRows = false;
		this.m_FramesDataGrid.CanRenameCell = false;
		this.m_FramesDataGrid.CanResizeColumnTitleBar = true;
		this.m_FramesDataGrid.CanResizeRows = true;
		this.m_FramesDataGrid.CanResizeRowTitleBar = true;
		this.m_FramesDataGrid.CanShowHideColumns = true;
		this.m_FramesDataGrid.CanSortByColumn = false;
		this.m_FramesDataGrid.ClearSelectionOnMouseLeave = false;
		this.m_FramesDataGrid.ColumnTitlePanelVisible = false;
		this.m_FramesDataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_FramesDataGrid.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_FramesDataGrid.DrawColumnLines = true;
		this.m_FramesDataGrid.DrawLastColumnLine = false;
		this.m_FramesDataGrid.DrawRowLines = true;
		this.m_FramesDataGrid.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_FramesDataGrid.HighlightedRowBoxColour = System.Drawing.Color.Blue;
		this.m_FramesDataGrid.HighlightedRowBoxVisible = false;
		this.m_FramesDataGrid.HighlightRow = false;
		this.m_FramesDataGrid.HighlightRowColour = System.Drawing.Color.FromArgb(225, 225, 255);
		this.m_FramesDataGrid.HighlightSelectedRow = false;
		this.m_FramesDataGrid.HorizontalTextOffset = 4;
		this.m_FramesDataGrid.LightRowColour = System.Drawing.Color.FromArgb(235, 235, 235);
		this.m_FramesDataGrid.Location = new System.Drawing.Point(0, 0);
		this.m_FramesDataGrid.Margin = new System.Windows.Forms.Padding(4);
		this.m_FramesDataGrid.MoveCellsEnabled = false;
		this.m_FramesDataGrid.Name = "m_FramesDataGrid";
		this.m_FramesDataGrid.PadEmptyRows = false;
		this.m_FramesDataGrid.ReadOnly = true;
		this.m_FramesDataGrid.RowHeightPadding = 3;
		this.m_FramesDataGrid.Rows = rows;
		this.m_FramesDataGrid.RowTitelPanelVisible = false;
		this.m_FramesDataGrid.ScrollColumnsHorz = false;
		this.m_FramesDataGrid.SelectByRow = false;
		this.m_FramesDataGrid.SelectedCellColour = System.Drawing.Color.Gainsboro;
		this.m_FramesDataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_FramesDataGrid.SelectNextCellAfterEdit = true;
		this.m_FramesDataGrid.ShowSelectBox = false;
		this.m_FramesDataGrid.Size = new System.Drawing.Size(111, 118);
		this.m_FramesDataGrid.SlideDrag = false;
		this.m_FramesDataGrid.TabIndex = 3;
		this.m_FramesDataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.panel1.Controls.Add(this.label1);
		this.panel1.Controls.Add(this.m_TimeLabel);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(111, 60);
		this.panel1.TabIndex = 6;
		this.panel2.Controls.Add(this.m_FramesDataGrid);
		this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel2.Location = new System.Drawing.Point(0, 60);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(111, 44);
		this.panel2.TabIndex = 7;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.panel2);
		base.Controls.Add(this.panel1);
		base.Name = "FrameInfoPanel";
		base.Size = new System.Drawing.Size(111, 104);
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		this.panel2.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
