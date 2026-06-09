using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace ProfilerStudy;

public class ThreadRowPanel : UserControl
{
	private float m_DPIScale;

	private int m_TitlePanelOriginalHeight;

	private IContainer components;

	private Label m_ThreadNameLabel;

	private HDataGrid m_DataGrid;

	private Panel panel1;

	private Panel panel2;

	private Panel panel3;

	private Panel m_TitlePanel;

	public string ThreadName
	{
		get
		{
			return m_ThreadNameLabel.Text;
		}
		set
		{
			m_ThreadNameLabel.Text = value;
			UpdateThreadNameLabelPos();
		}
	}

	public event ThreadRowPanelCollapseExpandToggleHandler CollapseExpandToggle;

	public event ThreadRowPanelMouseMoveHandler ThreadRowPanelMouseMove;

	public event ThreadRowPanelMouseDownHandler ThreadRowPanelMouseDown;

	public ThreadRowPanel()
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
		SuspendLayout();
		Column column = new Column();
		column.Width = ScaleDPI(80);
		m_DataGrid.Add(column);
		Column column2 = new Column();
		column2.WidthMode = Column.EWidthMode.Fill;
		m_DataGrid.Add(column2);
		m_DataGrid.Rows.Add("Scope", "");
		m_DataGrid.Rows.Add("Time/Frame", "");
		m_DataGrid.Rows.Add("Count/Frame", "");
		m_DataGrid.Rows.Add("Max", "");
		UpdateDataGridSize();
		m_DataGrid.FontChanged += DataGridFontChanged;
		UpdateDataGridColours();
		ResumeLayout();
	}

	private void DataGridFontChanged(object sender, EventArgs e)
	{
		UpdateDataGridSize();
	}

	private void UpdateDataGridSize()
	{
		m_DataGrid.Size = new Size(m_DataGrid.Width, m_DataGrid.Rows.Count * m_DataGrid.DefaultRowHeight);
	}

	protected override void OnMouseClick(MouseEventArgs e)
	{
		base.OnMouseClick(e);
		if (e.Button == MouseButtons.Left && e.Location.Y <= m_ThreadNameLabel.Bottom)
		{
			FireCollapseExpandToggle();
		}
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		UpdateDataGridColours();
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		UpdateDataGridColours();
	}

	private void UpdateDataGridColours()
	{
		m_DataGrid.WindowColour = BackColor;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateTitlePanelHeight();
		UpdateThreadNameLabelPos();
		UpdateDataGridVisibility();
	}

	private void UpdateTitlePanelHeight()
	{
		if (m_TitlePanelOriginalHeight == 0)
		{
			m_TitlePanelOriginalHeight = m_TitlePanel.Height;
		}
		int num = Math.Min(base.Height, m_TitlePanelOriginalHeight);
		if (num != m_TitlePanel.Height)
		{
			m_TitlePanel.Size = new Size(m_TitlePanel.Width, num);
		}
	}

	private void UpdateDataGridVisibility()
	{
		int num = m_DataGrid.DefaultRowHeight + m_DataGrid.RowHeightPadding;
		m_DataGrid.Visible = num <= base.Height;
	}

	private void UpdateThreadNameLabelPos()
	{
		int num = m_ThreadNameLabel.Width;
		int num2 = m_ThreadNameLabel.Height;
		m_ThreadNameLabel.Location = new Point(Math.Max(0, (m_TitlePanel.Width - num) / 2), Math.Max(0, (m_TitlePanel.Height - num2) / 2));
	}

	private void ThreadNameLabelMouseClick(object sender, EventArgs e)
	{
		FireCollapseExpandToggle();
	}

	private void FireCollapseExpandToggle()
	{
		if (this.CollapseExpandToggle != null)
		{
			this.CollapseExpandToggle();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		FireMouseMoveEvent(e.Location);
	}

	private void ThreadNameLabelMouseMove(object sender, MouseEventArgs e)
	{
		Point p = m_ThreadNameLabel.PointToScreen(e.Location);
		p = PointToClient(p);
		FireMouseMoveEvent(p);
	}

	private void FireMouseMoveEvent(Point location)
	{
		if (this.ThreadRowPanelMouseMove != null)
		{
			this.ThreadRowPanelMouseMove(location);
		}
	}

	public void SetSelectedTimeSpan(long time_span_name, string thread_name, Session session)
	{
		if (time_span_name != -1)
		{
			m_DataGrid.Rows[0].Cells[1].Value = session.GetTimerName(time_span_name);
			long num = 0L;
			double num2 = 0.0;
			long num3 = 0L;
			List<int> threadIds = session.GetThreadIds(thread_name);
			foreach (int item in threadIds)
			{
				num += session.GetTimeSpanFrameAverageTimeForThread(time_span_name, item);
				num2 = session.GetTimeSpanFrameAverageCountForThread(time_span_name, item);
				long start_time;
				long timeSpanMax = session.GetTimeSpanMax(time_span_name, item, out start_time);
				if (timeSpanMax > num3)
				{
					num3 = timeSpanMax;
				}
			}
			if (threadIds.Count != 0)
			{
				num /= threadIds.Count;
				num2 /= (double)threadIds.Count;
			}
			m_DataGrid.Rows[1].Cells[1].Value = Utils.GetTimeString(num, session.TimerFrequency);
			m_DataGrid.Rows[2].Cells[1].Value = num2.ToString("0.##");
			m_DataGrid.Rows[3].Cells[1].Value = Utils.GetTimeString(num3, session.TimerFrequency);
		}
		else
		{
			foreach (Row row in m_DataGrid.Rows)
			{
				row.Cells[1].Value = "";
			}
		}
		m_DataGrid.Refresh();
	}

	private void DataGridMouseDown(object sender, MouseEventArgs e)
	{
		FireMouseDown(e.Button);
	}

	private void PanelMouseDown(object sender, MouseEventArgs e)
	{
		FireMouseDown(e.Button);
	}

	private void LabelMouseDown(object sender, MouseEventArgs e)
	{
		FireMouseDown(e.Button);
	}

	private void FireMouseDown(MouseButtons button)
	{
		if (this.ThreadRowPanelMouseDown != null)
		{
			this.ThreadRowPanelMouseDown(button);
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
		this.m_ThreadNameLabel = new System.Windows.Forms.Label();
		this.m_DataGrid = new SCL.HDataGrid();
		this.panel1 = new System.Windows.Forms.Panel();
		this.panel2 = new System.Windows.Forms.Panel();
		this.panel3 = new System.Windows.Forms.Panel();
		this.m_TitlePanel = new System.Windows.Forms.Panel();
		this.panel3.SuspendLayout();
		this.m_TitlePanel.SuspendLayout();
		base.SuspendLayout();
		this.m_ThreadNameLabel.AutoSize = true;
		this.m_ThreadNameLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ThreadNameLabel.Location = new System.Drawing.Point(81, 20);
		this.m_ThreadNameLabel.Name = "m_ThreadNameLabel";
		this.m_ThreadNameLabel.Size = new System.Drawing.Size(114, 23);
		this.m_ThreadNameLabel.TabIndex = 0;
		this.m_ThreadNameLabel.Text = "Thread Name";
		this.m_ThreadNameLabel.Click += new System.EventHandler(ThreadNameLabelMouseClick);
		this.m_ThreadNameLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(LabelMouseDown);
		this.m_ThreadNameLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(ThreadNameLabelMouseMove);
		this.m_DataGrid.AddEmptyRow = false;
		this.m_DataGrid.AlternateRowColours = false;
		this.m_DataGrid.BackColor = System.Drawing.SystemColors.AppWorkspace;
		this.m_DataGrid.CanAddRemoveRows = true;
		this.m_DataGrid.CanRenameCell = true;
		this.m_DataGrid.CanResizeColumnTitleBar = false;
		this.m_DataGrid.CanResizeRows = false;
		this.m_DataGrid.CanResizeRowTitleBar = false;
		this.m_DataGrid.CanShowHideColumns = true;
		this.m_DataGrid.CanSortByColumn = true;
		this.m_DataGrid.ClearSelectionOnMouseLeave = false;
		this.m_DataGrid.ColumnTitlePanelVisible = false;
		this.m_DataGrid.DarkRowColour = System.Drawing.Color.FromArgb(230, 230, 230);
		this.m_DataGrid.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_DataGrid.DrawColumnLines = false;
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
		this.m_DataGrid.Margin = new System.Windows.Forms.Padding(6, 9, 6, 9);
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
		this.m_DataGrid.Size = new System.Drawing.Size(274, 131);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 1;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.m_DataGrid.MouseDown += new System.Windows.Forms.MouseEventHandler(DataGridMouseDown);
		this.panel1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
		this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.panel1.Location = new System.Drawing.Point(0, 283);
		this.panel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(276, 5);
		this.panel1.TabIndex = 2;
		this.panel2.BackColor = System.Drawing.SystemColors.ControlDarkDark;
		this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
		this.panel2.Location = new System.Drawing.Point(274, 0);
		this.panel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(2, 283);
		this.panel2.TabIndex = 3;
		this.panel3.Controls.Add(this.m_DataGrid);
		this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel3.Location = new System.Drawing.Point(0, 62);
		this.panel3.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.panel3.Name = "panel3";
		this.panel3.Size = new System.Drawing.Size(274, 221);
		this.panel3.TabIndex = 4;
		this.panel3.MouseDown += new System.Windows.Forms.MouseEventHandler(PanelMouseDown);
		this.m_TitlePanel.Controls.Add(this.m_ThreadNameLabel);
		this.m_TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TitlePanel.Location = new System.Drawing.Point(0, 0);
		this.m_TitlePanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
		this.m_TitlePanel.Name = "m_TitlePanel";
		this.m_TitlePanel.Size = new System.Drawing.Size(274, 62);
		this.m_TitlePanel.TabIndex = 5;
		this.m_TitlePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(PanelMouseDown);
		base.AutoScaleDimensions = new System.Drawing.SizeF(9f, 20f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.panel3);
		base.Controls.Add(this.m_TitlePanel);
		base.Controls.Add(this.panel2);
		base.Controls.Add(this.panel1);
		base.Name = "ThreadRowPanel";
		base.Size = new System.Drawing.Size(276, 288);
		this.panel3.ResumeLayout(false);
		this.m_TitlePanel.ResumeLayout(false);
		this.m_TitlePanel.PerformLayout();
		base.ResumeLayout(false);
	}
}
