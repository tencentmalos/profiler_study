using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCL;

namespace FramePro;

internal class LogView : SessionView
{
	private Session m_Session;

	private List<LogMessage> m_LogMessages = new List<LogMessage>();

	private float m_DPIScale;

	private IContainer components;

	private HDataGrid m_DataGrid;

	private ContextMenuStrip m_DataGridContextMenu;

	private ToolStripMenuItem viewFrameToolStripMenuItem;

	public override Session Session => m_Session;

	public override string ViewName => "Log";

	private int SelectedRowIndex
	{
		get
		{
			if (!m_DataGrid.SelectedCell.Valid)
			{
				return 0;
			}
			return m_DataGrid.SelectedCell.Row.Index;
		}
	}

	public event ShowFrameHandler ShowFrame;

	public LogView(Session session)
	{
		InitializeComponent();
		m_DPIScale = MainForm.DPIScale;
		m_Session = session;
		Column column = new Column("Time")
		{
			Width = ScaleDPI(160)
		};
		m_DataGrid.Add(column);
		Column column2 = new Column("Message")
		{
			WidthMode = Column.EWidthMode.Fill
		};
		m_DataGrid.Add(column2);
		UpdateDataGrid();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	public override void UpdateView()
	{
		UpdateDataGrid();
	}

	private void UpdateDataGrid()
	{
		int count = m_LogMessages.Count;
		m_Session.GetLogMessages(count, m_LogMessages);
		if (m_LogMessages.Count != count)
		{
			bool flag = m_DataGrid.SelectedCell == null || m_DataGrid.SelectedCell.Row.Index == m_DataGrid.Rows.Count - 1;
			long firstFrameTime = m_Session.FirstFrameTime;
			long timerFrequency = m_Session.TimerFrequency;
			for (int i = count; i < m_LogMessages.Count; i++)
			{
				m_DataGrid.Rows.Add(GetTimeString(m_LogMessages[i], firstFrameTime, timerFrequency), m_LogMessages[i].Message);
			}
			m_DataGrid.FinishedAddingRows();
			if (flag && m_DataGrid.Rows.Count != 0)
			{
				Row row = m_DataGrid.Rows[m_DataGrid.Rows.Count - 1];
				m_DataGrid.ClearSelection();
				m_DataGrid.AddSelectedCell(new ColRow(0, row));
				m_DataGrid.ScrollIntoView(row);
			}
			m_DataGrid.Refresh();
		}
	}

	private string GetTimeString(LogMessage log_message, long first_frame_time, long timer_frequency)
	{
		long num = (log_message.Time - first_frame_time) * 1000000 / timer_frequency;
		long num2 = num / 1000;
		num -= num2 * 1000;
		long num3 = num2 / 1000;
		num2 -= num3 * 1000;
		long num4 = num3 / 60;
		num3 -= num4 * 60;
		long num5 = num4 / 60;
		num4 -= num5 * 60;
		return num5 + ":" + num4.ToString("00") + ":" + num3.ToString("00") + ":" + num2.ToString("0000") + ":" + num.ToString("0000");
	}

	private long GetTimeFromTimeString(string time_string)
	{
		string[] array = time_string.Split(':');
		long num = Convert.ToInt32(array[0]);
		long num2 = Convert.ToInt32(array[1]);
		long num3 = Convert.ToInt32(array[2]);
		long num4 = Convert.ToInt32(array[3]);
		long num5 = Convert.ToInt32(array[4]);
		long num6 = num * 60 * 60 * 1000 * 1000 + num2 * 60 * 1000 * 1000 + num3 * 1000 * 1000 + num4 * 1000 + num5;
		return m_Session.FirstFrameTime + num6 * m_Session.TimerFrequency / 1000000;
	}

	private void ViewFrameMenuItemClick(object sender, EventArgs e)
	{
		if (m_DataGrid.SelectedCell.Valid)
		{
			string time_string = m_DataGrid.SelectedCell.Row.Cells[0].Value as string;
			long timeFromTimeString = GetTimeFromTimeString(time_string);
			if (this.ShowFrame != null)
			{
				this.ShowFrame(timeFromTimeString);
			}
		}
	}

	public override void GotoPrev(string filter)
	{
		string value = filter.ToLower().Trim();
		int selectedRowIndex = SelectedRowIndex;
		_ = m_DataGrid.Rows.Count;
		for (int num = selectedRowIndex; num >= 0; num--)
		{
			if ((m_DataGrid.Rows[num].Cells[1].Value as string).ToLower().Contains(value))
			{
				m_DataGrid.ClearSelection();
				m_DataGrid.AddSelectedCell(new ColRow(1, m_DataGrid.Rows[num]));
				break;
			}
		}
	}

	public override void GotoNext(string filter)
	{
		string value = filter.ToLower().Trim();
		int selectedRowIndex = SelectedRowIndex;
		int count = m_DataGrid.Rows.Count;
		for (int i = selectedRowIndex; i < count; i++)
		{
			if ((m_DataGrid.Rows[i].Cells[1].Value as string).ToLower().Contains(value))
			{
				m_DataGrid.ClearSelection();
				m_DataGrid.AddSelectedCell(new ColRow(1, m_DataGrid.Rows[i]));
				break;
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
		this.components = new System.ComponentModel.Container();
		SCL.RowCollection rows = new SCL.RowCollection();
		this.m_DataGrid = new SCL.HDataGrid();
		this.m_DataGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
		this.viewFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_DataGridContextMenu.SuspendLayout();
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
		this.m_DataGrid.ContextMenuStrip = this.m_DataGridContextMenu;
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
		this.m_DataGrid.ReadOnly = true;
		this.m_DataGrid.RowHeightPadding = 3;
		this.m_DataGrid.Rows = rows;
		this.m_DataGrid.RowTitelPanelVisible = true;
		this.m_DataGrid.ScrollColumnsHorz = false;
		this.m_DataGrid.SelectByRow = false;
		this.m_DataGrid.SelectedCellColour = System.Drawing.Color.FromArgb(188, 180, 250);
		this.m_DataGrid.SelectedRowColour = System.Drawing.Color.FromArgb(210, 210, 255);
		this.m_DataGrid.SelectNextCellAfterEdit = true;
		this.m_DataGrid.ShowSelectBox = true;
		this.m_DataGrid.Size = new System.Drawing.Size(1191, 1005);
		this.m_DataGrid.SlideDrag = false;
		this.m_DataGrid.TabIndex = 0;
		this.m_DataGrid.WindowColour = System.Drawing.SystemColors.Window;
		this.m_DataGridContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.viewFrameToolStripMenuItem });
		this.m_DataGridContextMenu.Name = "m_DataGridContextMenu";
		this.m_DataGridContextMenu.Size = new System.Drawing.Size(136, 26);
		this.viewFrameToolStripMenuItem.Name = "viewFrameToolStripMenuItem";
		this.viewFrameToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
		this.viewFrameToolStripMenuItem.Text = "View Frame";
		this.viewFrameToolStripMenuItem.Click += new System.EventHandler(ViewFrameMenuItemClick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_DataGrid);
		base.Name = "LogView";
		base.Size = new System.Drawing.Size(1191, 1005);
		this.m_DataGridContextMenu.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
