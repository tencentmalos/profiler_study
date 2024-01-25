using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SCL;

public class HDataGrid : UserControl
{
	private Row m_RootRow;

	private float m_DPIScale;

	private List<Column> m_Columns = new List<Column>();

	private ColumnTitlePanel m_ColumnTitlePanel;

	private RowTitlePanel m_RowTitlePanel;

	private ScrollPanel m_ScrollPanel;

	private CellsPanel m_CellsPanel;

	private int m_RowTitleWidth = 20;

	private int m_ColumnTitleHeight = 20;

	private bool m_ShowSelectBox = true;

	private SelectBox m_SelectBox = new SelectBox();

	private int m_DefaultHeight = 20;

	private Brush m_FillBrush = SystemBrushes.ButtonFace;

	private Pen m_HiPen = SystemPens.ButtonHighlight;

	private Pen m_LowPen = SystemPens.ButtonShadow;

	private ResizeTitleInfo m_ResizeTitleInfo = new ResizeTitleInfo();

	private DragNodeTargetMarker m_DragNodeTargetMarker = new DragNodeTargetMarker();

	private const int m_MinColumnWidth = 16;

	internal const int m_DragResizeExtent = 4;

	private int m_DefaultRowHeight = 20;

	private int m_RowHeightPadding = 3;

	private bool m_ScrollColumnsHorz;

	private const int m_HorzScrollPanelHeight = 12;

	private Panel m_HorzScrollPanel = new Panel();

	private List<HScrollBar> m_HorzScrollBars = new List<HScrollBar>();

	private bool m_SelectingMultipleCells;

	private bool m_CanSortByColumn = true;

	private bool m_AddEmptyRow;

	private bool m_CanAddRemoveRows = true;

	private bool m_CanResizeColumnTitleBar;

	private bool m_CanResizeRowTitleBar;

	private bool m_CanShowHideColumns = true;

	private SelectBox m_HighlightedRowBox = new SelectBox();

	private bool m_HighlightedRowBoxVisible;

	private int m_StartEndUpdateCount;

	private IContainer components;

	public RowCollection Rows
	{
		get
		{
			return m_RootRow.ChildRows;
		}
		set
		{
			m_RootRow.ChildRows = value;
			m_CellsPanel.OnRowsCollectionReset();
			AddEmptyRowIfNeeded();
			RefreshDataGrid();
		}
	}

	public ICollection<Row> DisplayRows => m_RootRow.ChildRows.DisplayRows;

	public bool ShowSelectBox
	{
		get
		{
			return m_ShowSelectBox;
		}
		set
		{
			if (m_ShowSelectBox != value)
			{
				m_ShowSelectBox = value;
				UpdateSelectBox();
			}
		}
	}

	public ICollection<ColRow> SelectedCells => m_CellsPanel.SelectedCells;

	public ColRow SelectedCell
	{
		get
		{
			ColRow result = null;
			using (IEnumerator<ColRow> enumerator = SelectedCells.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					result = enumerator.Current;
				}
			}
			return result;
		}
	}

	public bool ColumnTitlePanelVisible
	{
		get
		{
			return m_ColumnTitlePanel.Visible;
		}
		set
		{
			m_ColumnTitlePanel.Visible = value;
		}
	}

	public bool RowTitelPanelVisible
	{
		get
		{
			return m_RowTitlePanel.Visible;
		}
		set
		{
			m_RowTitlePanel.Visible = value;
			m_ColumnTitlePanel.RowTitleWidth = (value ? m_RowTitlePanel.Width : 0);
		}
	}

	public bool CanRenameCell
	{
		get
		{
			return m_CellsPanel.CanRenameCell;
		}
		set
		{
			m_CellsPanel.CanRenameCell = value;
		}
	}

	public bool MoveCellsEnabled
	{
		get
		{
			return m_CellsPanel.MoveCellsEnabled;
		}
		set
		{
			m_CellsPanel.MoveCellsEnabled = value;
		}
	}

	public Color WindowColour
	{
		get
		{
			return m_CellsPanel.WindowColour;
		}
		set
		{
			m_CellsPanel.WindowColour = value;
		}
	}

	public bool PadEmptyRows
	{
		get
		{
			return m_CellsPanel.PadEmptyRows;
		}
		set
		{
			m_CellsPanel.PadEmptyRows = value;
			m_RowTitlePanel.PadEmptyRows = value;
		}
	}

	private bool DraggingColumn => m_ColumnTitlePanel.DraggingColumn;

	public int DefaultRowHeight => m_DefaultRowHeight;

	public int RowHeightPadding
	{
		get
		{
			return m_RowHeightPadding;
		}
		set
		{
			m_RowHeightPadding = value;
			SetDefaultRowHeight();
		}
	}

	public bool ReadOnly
	{
		get
		{
			return m_CellsPanel.ReadOnly;
		}
		set
		{
			m_CellsPanel.ReadOnly = value;
		}
	}

	public bool AlternateRowColours
	{
		get
		{
			return m_CellsPanel.AlternateRowColours;
		}
		set
		{
			m_CellsPanel.AlternateRowColours = value;
		}
	}

	public bool DrawRowLines
	{
		get
		{
			return m_CellsPanel.DrawRowLines;
		}
		set
		{
			m_CellsPanel.DrawRowLines = value;
		}
	}

	public bool DrawColumnLines
	{
		get
		{
			return m_CellsPanel.DrawColumnLines;
		}
		set
		{
			m_CellsPanel.DrawColumnLines = value;
		}
	}

	public Color SelectedCellColour
	{
		get
		{
			return m_CellsPanel.SelectedCellColour;
		}
		set
		{
			m_CellsPanel.SelectedCellColour = value;
		}
	}

	public bool SelectNextCellAfterEdit
	{
		get
		{
			return m_CellsPanel.SelectNextCellAfterEdit;
		}
		set
		{
			m_CellsPanel.SelectNextCellAfterEdit = value;
		}
	}

	public bool ScrollColumnsHorz
	{
		get
		{
			return m_ScrollColumnsHorz;
		}
		set
		{
			m_ScrollColumnsHorz = value;
			UpdateHorzScrollBars();
		}
	}

	public bool CanSortByColumn
	{
		get
		{
			return m_CanSortByColumn;
		}
		set
		{
			m_CanSortByColumn = value;
			m_ColumnTitlePanel.CanSortByColumn = m_CanSortByColumn;
		}
	}

	public ColRow LastSelectedCell => m_CellsPanel.LastSelectedCell;

	public bool HighlightSelectedRow
	{
		get
		{
			return m_CellsPanel.HighlightSelectedRow;
		}
		set
		{
			m_CellsPanel.HighlightSelectedRow = value;
		}
	}

	public Color SelectedRowColour
	{
		get
		{
			return m_CellsPanel.SelectedRowColour;
		}
		set
		{
			m_CellsPanel.SelectedRowColour = value;
		}
	}

	public bool AddEmptyRow
	{
		get
		{
			return m_AddEmptyRow;
		}
		set
		{
			m_AddEmptyRow = value;
			AddEmptyRowIfNeeded();
		}
	}

	private Row LastRow
	{
		get
		{
			if (m_RootRow.ChildRows.Count <= 0)
			{
				return null;
			}
			return m_RootRow.ChildRows.DisplayRows[m_RootRow.ChildRows.Count - 1];
		}
	}

	public bool SlideDrag
	{
		get
		{
			return m_CellsPanel.SlideDrag;
		}
		set
		{
			m_CellsPanel.SlideDrag = value;
		}
	}

	public bool EditControlVisible => m_CellsPanel.EditControlVisible;

	public bool CanResizeRows
	{
		get
		{
			return m_RowTitlePanel.CanResizeRows;
		}
		set
		{
			m_RowTitlePanel.CanResizeRows = value;
		}
	}

	public bool CanAddRemoveRows
	{
		get
		{
			return m_CanAddRemoveRows;
		}
		set
		{
			m_CanAddRemoveRows = value;
			m_RowTitlePanel.CanAddRemoveRows = value;
		}
	}

	public ICollection<Row> SelectedRows => m_CellsPanel.SelRows;

	public int SelectedRow
	{
		get
		{
			int num = int.MaxValue;
			foreach (ColRow selectedCell in SelectedCells)
			{
				if (selectedCell.Row.Index < num)
				{
					num = selectedCell.Row.Index;
				}
			}
			if (num != int.MaxValue)
			{
				return num;
			}
			return -1;
		}
	}

	public bool CanResizeColumnTitleBar
	{
		get
		{
			return m_CanResizeColumnTitleBar;
		}
		set
		{
			m_CanResizeColumnTitleBar = value;
		}
	}

	public bool CanResizeRowTitleBar
	{
		get
		{
			return m_CanResizeRowTitleBar;
		}
		set
		{
			m_CanResizeRowTitleBar = value;
		}
	}

	public Column CurrentSortedColumn
	{
		get
		{
			foreach (Column column in m_Columns)
			{
				if (column.SortMode != 0)
				{
					return column;
				}
			}
			return null;
		}
	}

	public ICollection<Column> Columns => m_Columns;

	public bool ClearSelectionOnMouseLeave
	{
		get
		{
			return m_CellsPanel.ClearSelectionOnMouseLeave;
		}
		set
		{
			m_CellsPanel.ClearSelectionOnMouseLeave = value;
		}
	}

	public bool HighlightRow
	{
		get
		{
			return m_CellsPanel.HighlightRow;
		}
		set
		{
			m_CellsPanel.HighlightRow = value;
		}
	}

	public int HorizontalTextOffset
	{
		get
		{
			return m_CellsPanel.HorizontalTextOffset;
		}
		set
		{
			m_CellsPanel.HorizontalTextOffset = value;
		}
	}

	public Color LightRowColour
	{
		get
		{
			return m_CellsPanel.LightRowColour;
		}
		set
		{
			m_CellsPanel.LightRowColour = value;
		}
	}

	public Color DarkRowColour
	{
		get
		{
			return m_CellsPanel.DarkRowColour;
		}
		set
		{
			m_CellsPanel.DarkRowColour = value;
		}
	}

	public bool DrawLastColumnLine
	{
		get
		{
			return m_CellsPanel.DrawLastColumnLine;
		}
		set
		{
			m_CellsPanel.DrawLastColumnLine = value;
		}
	}

	public Color HighlightRowColour
	{
		get
		{
			return m_CellsPanel.HighlightRowColour;
		}
		set
		{
			m_CellsPanel.HighlightRowColour = value;
		}
	}

	public bool CanShowHideColumns
	{
		get
		{
			return m_CanShowHideColumns;
		}
		set
		{
			if (m_CanShowHideColumns != value)
			{
				m_CanShowHideColumns = value;
				m_ColumnTitlePanel.CanShowHideColumns = value;
			}
		}
	}

	public bool HighlightedRowBoxVisible
	{
		get
		{
			return m_HighlightedRowBoxVisible;
		}
		set
		{
			m_HighlightedRowBoxVisible = value;
		}
	}

	public Color HighlightedRowBoxColour
	{
		get
		{
			return m_HighlightedRowBox.Colour;
		}
		set
		{
			m_HighlightedRowBox.Colour = value;
		}
	}

	public bool SelectByRow
	{
		get
		{
			return m_CellsPanel.SelectByRow;
		}
		set
		{
			m_CellsPanel.SelectByRow = value;
		}
	}

	public event SelectionChangedHandler SelectionChanged;

	public event RowMoveRequestHandler RowMoveRequest;

	public event ExpandedChangedHandler ExpandedChanged;

	public event RowMovedHandler RowMoved;

	public event CellDoubleClickedHandler CellDoubleClicked;

	public event RowRemovedHandler RowRemoved;

	public event RowInsertedHandler RowInserted;

	public event DeletingRowHandler DeletingRows;

	public event RowMovedHandler SlideDragFinished;

	public event SortedColumnChangedHandler SortedColumnChanged;

	public event CellChangedHandler CellChanged;

	static HDataGrid()
	{
		////if (Assembly.GetEntryAssembly() != null)
		////{
		////	byte[] publicKeyToken = typeof(HDataGrid).Assembly.GetName().GetPublicKeyToken();
		////	byte[] publicKeyToken2 = Assembly.GetEntryAssembly().GetName().GetPublicKeyToken();
		////	if (!Utils.Equal(publicKeyToken, publicKeyToken2))
		////	{
		////		MessageBox.Show("Unorthorised use of HDataGrid. Please visit www.puredevsoftware.com");
		////		Environment.Exit(1);
		////	}
		////}
	}

	public HDataGrid()
	{
		InitializeComponent();
		SuspendLayout();
		m_DPIScale = (float)base.DeviceDpi / 96f;
		SetDefaultRowHeight();
		BackColor = SystemColors.AppWorkspace;
		m_HorzScrollPanel.Size = new Size(100, ScaleDPI(12));
		m_HorzScrollPanel.Dock = DockStyle.Bottom;
		m_ColumnTitlePanel = new ColumnTitlePanel(m_Columns, m_DPIScale);
		m_ColumnTitlePanel.Size = new Size(base.ClientSize.Width, ScaleDPI(m_ColumnTitleHeight));
		m_ColumnTitlePanel.RowTitleWidth = ScaleDPI(m_RowTitleWidth);
		m_ColumnTitlePanel.Dock = DockStyle.Top;
		m_ColumnTitlePanel.CanSortByColumn = m_CanSortByColumn;
		m_ColumnTitlePanel.CanShowHideColumns = m_CanShowHideColumns;
		m_ColumnTitlePanel.ColumnVisibilityChanged += ColumnTitlePanelColumnVisibilityChanged;
		TrackMouse(m_ColumnTitlePanel);
		m_RootRow = new Row(this);
		m_RowTitlePanel = new RowTitlePanel(m_RootRow);
		m_RowTitlePanel.CanAddRemoveRows = m_CanAddRemoveRows;
		m_RowTitlePanel.Size = new Size(ScaleDPI(m_RowTitleWidth), base.ClientSize.Height);
		m_RowTitlePanel.Dock = DockStyle.Left;
		TrackMouse(m_RowTitlePanel);
		m_CellsPanel = new CellsPanel(m_RootRow, m_Columns, base.Controls, m_DPIScale);
		m_CellsPanel.Dock = DockStyle.Fill;
		m_CellsPanel.BackColor = BackColor;
		TrackMouse(m_CellsPanel);
		m_ScrollPanel = new ScrollPanel(m_CellsPanel);
		m_ScrollPanel.Dock = DockStyle.Fill;
		m_SelectBox.SelectBoxVisibilityChanged += SelectBoxSelectBoxVisibilityChanged;
		TrackMouse(m_SelectBox.SelLeftPanel);
		TrackMouse(m_SelectBox.SelRightPanel);
		TrackMouse(m_SelectBox.SelTopPanel);
		TrackMouse(m_SelectBox.SelBottomPanel);
		m_SelectBox.Visible = false;
		m_SelectBox.AddTo(this);
		m_HighlightedRowBox.SelPanelThickness = 1;
		m_HighlightedRowBox.Colour = Color.Blue;
		TrackMouse(m_HighlightedRowBox.SelLeftPanel);
		TrackMouse(m_HighlightedRowBox.SelRightPanel);
		TrackMouse(m_HighlightedRowBox.SelTopPanel);
		TrackMouse(m_HighlightedRowBox.SelBottomPanel);
		m_HighlightedRowBox.Visible = false;
		m_HighlightedRowBox.AddTo(this);
		UpdateSelectBox();
		base.Controls.Add(m_ScrollPanel);
		base.Controls.Add(m_RowTitlePanel);
		base.Controls.Add(m_ColumnTitlePanel);
		base.Controls.Add(m_HorzScrollPanel);
		m_DragNodeTargetMarker.AddTo(this);
		m_CellsPanel.Focus();
		ResumeLayout();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void ColumnTitlePanelColumnVisibilityChanged()
	{
		RefreshDataGrid();
	}

	private void SelectBoxSelectBoxVisibilityChanged(bool visible)
	{
		m_CellsPanel.HighlightSelected = !visible;
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		if (m_CellsPanel != null)
		{
			m_CellsPanel.BackColor = BackColor;
		}
		base.OnBackColorChanged(e);
	}

	protected override bool ProcessKeyPreview(ref Message m)
	{
		m_CellsPanel.MyProcessKeyPreview(ref m);
		return base.ProcessKeyPreview(ref m);
	}

	internal void ScrollPanelScrollEvent(ScrollOrientation scroll_orientation, int new_value)
	{
		switch (scroll_orientation)
		{
		case ScrollOrientation.VerticalScroll:
			m_RowTitlePanel.ScrollY = new_value;
			m_RowTitlePanel.Refresh();
			break;
		case ScrollOrientation.HorizontalScroll:
			m_ColumnTitlePanel.ScrollX = new_value;
			m_ColumnTitlePanel.Refresh();
			break;
		}
		UpdateSelectBox();
	}

	private void TrackMouse(Control control)
	{
		control.MouseMove += ChildControlMouseMove;
		control.MouseDown += ChildControlMouseDown;
		control.MouseUp += ChildControlMouseUp;
	}

	internal void UpdateSelectBox()
	{
		if (m_CellsPanel == null)
		{
			return;
		}
		if (!m_ShowSelectBox || m_CellsPanel.SelRows.Count != 0 || (m_CellsPanel.LastSelectedCell.Row != null && !Utils.RowExpandedRecursive(m_CellsPanel.LastSelectedCell.Row)))
		{
			m_SelectBox.Visible = false;
			m_HighlightedRowBox.Visible = false;
		}
		else
		{
			if (!m_CellsPanel.LastSelectedCell.Valid)
			{
				return;
			}
			Rectangle cellRect = m_CellsPanel.GetCellRect(m_CellsPanel.LastSelectedCell);
			cellRect = m_CellsPanel.RectangleToScreen(cellRect);
			cellRect = RectangleToClient(cellRect);
			Rectangle clientRectangle = m_CellsPanel.ClientRectangle;
			clientRectangle = m_CellsPanel.RectangleToScreen(clientRectangle);
			clientRectangle = RectangleToClient(clientRectangle);
			int num = Math.Max(cellRect.X, clientRectangle.X);
			int num2 = Math.Max(cellRect.Y, clientRectangle.Y);
			int num3 = Math.Min(cellRect.Right, clientRectangle.Right);
			int num4 = Math.Min(cellRect.Bottom, clientRectangle.Bottom);
			cellRect = new Rectangle(num, num2, num3 - num, num4 - num2);
			SuspendLayout();
			if (cellRect.Width > 0 && cellRect.Height > 0)
			{
				if (m_HighlightedRowBoxVisible)
				{
					m_HighlightedRowBox.SetRect(new Rectangle(0, cellRect.Y, m_CellsPanel.Width, cellRect.Height));
					m_HighlightedRowBox.Visible = true;
				}
				m_SelectBox.SetRect(cellRect);
				m_SelectBox.Visible = true;
				m_SelectBox.BringToFront();
			}
			else if (m_SelectBox.Visible)
			{
				m_SelectBox.Visible = false;
				m_HighlightedRowBox.Visible = false;
			}
			ResumeLayout();
		}
	}

	public void RefreshDataGrid()
	{
		UpdateColumnWidths();
		UpdateSelectBox();
		m_ScrollPanel.Reset();
		m_CellsPanel.Reset();
		ResortColumn();
		RefreshInternal();
		AddEmptyRowIfNeeded();
		m_ScrollPanel.UpdateScrollBars();
	}

	public void UpdateScrollBars()
	{
		m_ScrollPanel.UpdateScrollBars();
	}

	private Rectangle GetCellRect(ColRow colrow)
	{
		int num = m_CellsPanel.Location.X;
		int num2 = 0;
		for (int i = 0; i <= colrow.Col; i++)
		{
			if (m_Columns[i].Visible)
			{
				num += num2;
				num2 = ((i == m_Columns.Count - 1) ? (base.ClientSize.Width - num) : m_Columns[i].Width);
			}
		}
		int num3 = m_CellsPanel.Location.Y;
		int num4 = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.ChildRows);
		while (rowIterator.Current != colrow.Row)
		{
			num3 += num4;
			num4 = ((!rowIterator.MoveNext() || rowIterator.Current.Height == -1) ? ScaleDPI(m_DefaultHeight) : rowIterator.Current.Height);
		}
		return new Rectangle(num, num3, num2, num4);
	}

	public void Add(Column column)
	{
		m_Columns.Add(column);
		AddHScrollBar();
		UpdateColumnWidths();
		AddEmptyRowIfNeeded();
	}

	private void AddHScrollBar()
	{
		HScrollBar hScrollBar = new HScrollBar();
		hScrollBar.Scroll += ColumnHScrollBarScrolled;
		m_HorzScrollBars.Add(hScrollBar);
		m_HorzScrollPanel.Controls.Add(hScrollBar);
	}

	private void ColumnHScrollBarScrolled(object sender, ScrollEventArgs e)
	{
		HScrollBar hScrollBar = (HScrollBar)sender;
		int index = m_HorzScrollBars.IndexOf(hScrollBar);
		m_Columns[index].HScrollOffset = hScrollBar.Value;
		m_CellsPanel.Refresh();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		OnMouseMove(e.Location);
		base.OnMouseMove(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		OnMouseDown(e.Button, e.Location);
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		OnMouseUp(e.Button, e.Location);
		base.OnMouseUp(e);
	}

	private void ChildControlMouseMove(object sender, MouseEventArgs e)
	{
		OnMouseMove(ToClientSpace(sender, e.Location));
	}

	private void ChildControlMouseDown(object sender, MouseEventArgs e)
	{
		OnMouseDown(e.Button, ToClientSpace(sender, e.Location));
	}

	private void ChildControlMouseUp(object sender, MouseEventArgs e)
	{
		OnMouseUp(e.Button, ToClientSpace(sender, e.Location));
	}

	private Point ToClientSpace(object sender, Point pt)
	{
		Point p = ((Control)sender).PointToScreen(pt);
		return PointToClient(p);
	}

	private void OnMouseMove(Point pt)
	{
		if (DraggingColumn)
		{
			return;
		}
		if (m_ResizeTitleInfo.m_Active)
		{
			if (m_ResizeTitleInfo.m_Mode == ResizeTitleInfo.Mode.Column)
			{
				int num = pt.Y - m_ResizeTitleInfo.m_StartMousePos;
				int num2 = m_ResizeTitleInfo.m_StartValue + num;
				m_ColumnTitlePanel.Height = num2;
			}
			else
			{
				int num3 = pt.X - m_ResizeTitleInfo.m_StartMousePos;
				int rowTitleWidth = m_ResizeTitleInfo.m_StartValue + num3;
				m_RowTitlePanel.Width = rowTitleWidth;
				m_ColumnTitlePanel.RowTitleWidth = rowTitleWidth;
				m_ColumnTitlePanel.Refresh();
			}
		}
		else if (m_CanResizeColumnTitleBar && Math.Abs(pt.Y - m_ColumnTitlePanel.Bottom) < 4)
		{
			Cursor = Cursors.HSplit;
			m_ResizeTitleInfo.m_Mode = ResizeTitleInfo.Mode.Column;
			SetChildControlMouseEnabled(value: false);
		}
		else if (m_CanResizeRowTitleBar && Math.Abs(pt.X - m_RowTitlePanel.Right) < ScaleDPI(4))
		{
			Cursor = Cursors.VSplit;
			m_ResizeTitleInfo.m_Mode = ResizeTitleInfo.Mode.Row;
			SetChildControlMouseEnabled(value: false);
		}
		else if (m_ResizeTitleInfo.m_Mode != 0)
		{
			Cursor = Cursors.Default;
			m_ResizeTitleInfo.m_Mode = ResizeTitleInfo.Mode.None;
			SetChildControlMouseEnabled(value: true);
		}
	}

	private void OnMouseDown(MouseButtons button, Point pt)
	{
		if (button == MouseButtons.Left)
		{
			if (m_ResizeTitleInfo.m_Mode == ResizeTitleInfo.Mode.Column)
			{
				base.Capture = true;
				m_ResizeTitleInfo.m_Active = true;
				m_ResizeTitleInfo.m_StartValue = m_ColumnTitlePanel.Height;
				m_ResizeTitleInfo.m_StartMousePos = pt.Y;
			}
			else if (m_ResizeTitleInfo.m_Mode == ResizeTitleInfo.Mode.Row)
			{
				base.Capture = true;
				m_ResizeTitleInfo.m_Active = true;
				m_ResizeTitleInfo.m_StartValue = m_RowTitlePanel.Width;
				m_ResizeTitleInfo.m_StartMousePos = pt.X;
			}
		}
	}

	private void OnMouseUp(MouseButtons button, Point pt)
	{
		if (button == MouseButtons.Left && m_ResizeTitleInfo.m_Mode != 0)
		{
			m_ResizeTitleInfo.m_Active = false;
			base.Capture = false;
			SetChildControlMouseEnabled(value: true);
		}
	}

	private void SetChildControlMouseEnabled(bool value)
	{
		m_ColumnTitlePanel.MouseEnabled = value;
		m_RowTitlePanel.MouseEnabled = value;
		m_CellsPanel.MouseEnabled = value;
	}

	internal void StartSelectingMultipleCells()
	{
		m_SelectingMultipleCells = true;
	}

	internal void StopSelectingMultipleCells()
	{
		m_SelectingMultipleCells = false;
		UpdateSelectBox();
	}

	internal void OnSelectionChanged(ColRow colrow)
	{
		if (this.SelectionChanged != null)
		{
			this.SelectionChanged(new List<ColRow>(m_CellsPanel.SelectedCells));
		}
		if (!m_SelectingMultipleCells)
		{
			if (colrow.Valid)
			{
				ScrollIntoView(colrow.Row);
			}
			UpdateSelectBox();
		}
		if (m_CellsPanel.SelRows.Count != 0)
		{
			UpdateSelectBox();
			m_RowTitlePanel.Refresh();
		}
	}

	public void ScrollIntoView(Row row)
	{
		Rectangle cellRect = m_CellsPanel.GetCellRect(new ColRow(0, row));
		if (cellRect.Top < 0)
		{
			ScrollToRow(row);
		}
		else
		{
			if (cellRect.Bottom <= m_CellsPanel.ClientSize.Height + 1)
			{
				return;
			}
			int num = 0;
			ReverseRowIterator reverseRowIterator = new ReverseRowIterator(row);
			Row row2 = null;
			while (reverseRowIterator.MoveNext())
			{
				row = reverseRowIterator.Current;
				num += m_CellsPanel.GetCellRect(new ColRow(0, row)).Height;
				if (num >= m_CellsPanel.ClientSize.Height)
				{
					break;
				}
				row2 = row;
			}
			ScrollToRow(row2);
		}
	}

	private void ScrollToRow(Row row)
	{
		Rectangle cellRect = m_CellsPanel.GetCellRect(new ColRow(0, row));
		m_ScrollPanel.ScrollPosition = new Point(m_ScrollPanel.ScrollPosition.X, m_ScrollPanel.ScrollPosition.Y - cellRect.Y);
		m_CellsPanel.Focus();
	}

	internal void OnRowMoveRequest(List<Row> rows, Row target_row, EDropMode drop_mode, ref bool cancel)
	{
		if (this.RowMoveRequest != null)
		{
			this.RowMoveRequest(rows, target_row, drop_mode, ref cancel);
		}
	}

	internal void OnRowExpandedChanged(Row row)
	{
		UpdateSelectBox();
		m_ScrollPanel.UpdateScrollBars();
		if (this.ExpandedChanged != null)
		{
			this.ExpandedChanged(row);
		}
	}

	public void UnselectCell(Cell cell)
	{
		m_CellsPanel.UnselectCell(cell);
	}

	public void UnselectCell(object value)
	{
		m_CellsPanel.UnselectCell(value);
	}

	internal void ShowDragNodeTargetMarker(Rectangle rect)
	{
		rect.Offset(new Point(-m_ScrollPanel.ScrollPosition.X, -m_ScrollPanel.ScrollPosition.Y));
		m_DragNodeTargetMarker.SetPosition(rect.X, rect.Y, rect.Width, rect.Height);
		m_DragNodeTargetMarker.Show();
	}

	internal void HideDragNodeTargetMarker()
	{
		m_DragNodeTargetMarker.Hide();
	}

	public void AddSelectedCell(Cell cell)
	{
		m_CellsPanel.AddSelectedCell(cell);
	}

	public void AddSelectedCell(int col, int row)
	{
		RowIterator rowIterator = new RowIterator(m_RootRow);
		rowIterator.MoveNext();
		rowIterator.MoveNext();
		for (int i = 0; i < row; i++)
		{
			if (!rowIterator.MoveNext())
			{
				return;
			}
		}
		AddSelectedCell(new ColRow(col, rowIterator.Current));
	}

	public void AddSelectedCell(object value)
	{
		m_CellsPanel.AddSelectedCell(value);
	}

	public void AddSelectedCell(ColRow colrow)
	{
		m_CellsPanel.AddSelectedCell(colrow);
	}

	public void Select(Cell cell)
	{
		m_CellsPanel.SelectCell(cell);
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		UpdateSelectBox();
		UpdateColumnWidths();
		UpdateHorzScrollBars();
		UpdateScrollSize();
		if (m_CellsPanel != null)
		{
			m_CellsPanel.Refresh();
		}
		if (m_ColumnTitlePanel != null)
		{
			m_ColumnTitlePanel.Refresh();
		}
	}

	public void RenameCell(ColRow col_row)
	{
		m_CellsPanel.RenameCell(col_row);
	}

	public void ResortColumn()
	{
		Column sortedColumn = GetSortedColumn();
		if (sortedColumn != null)
		{
			Sort(sortedColumn);
		}
	}

	public Column GetSortedColumn()
	{
		foreach (Column column in m_Columns)
		{
			if (column.SortMode != 0)
			{
				return column;
			}
		}
		return null;
	}

	public void Sort(Column column)
	{
		foreach (Column column2 in m_Columns)
		{
			if (column2 != column)
			{
				column2.SortMode = Column.ESortMode.NotSorted;
			}
		}
		bool reverse = column.SortMode == Column.ESortMode.Decreasing;
		m_RootRow.ChildRows.Sort(m_Columns.IndexOf(column), reverse, column.Comparer);
		m_CellsPanel.UpdateCellControls();
		if (this.SortedColumnChanged != null)
		{
			this.SortedColumnChanged(column);
		}
	}

	internal void OnCellDoubleClicked(ColRow colrow)
	{
		if (this.CellDoubleClicked != null)
		{
			this.CellDoubleClicked(colrow);
		}
	}

	public void Clear()
	{
		m_RootRow.ChildRows.Clear();
	}

	internal void UpdateColumnWidths()
	{
		if (m_CellsPanel == null)
		{
			return;
		}
		if (m_Columns.Count == 1)
		{
			if (m_Columns[0].WidthMode == Column.EWidthMode.Fill)
			{
				m_Columns[0].Width = m_CellsPanel.Width;
			}
		}
		else
		{
			int num = m_CellsPanel.Width;
			int num2 = 0;
			foreach (Column column in m_Columns)
			{
				if (column.Visible)
				{
					if (column.WidthMode == Column.EWidthMode.Explicit)
					{
						num -= column.Width;
					}
					else
					{
						num2++;
					}
				}
			}
			if (num2 != 0)
			{
				int num3 = Math.Max(num / num2, ScaleDPI(16));
				bool flag = false;
				foreach (Column column2 in m_Columns)
				{
					if (column2.Visible && column2.WidthMode == Column.EWidthMode.Fill && column2.Width != num3)
					{
						column2.Width = num3;
						flag = true;
					}
				}
				if (flag)
				{
					RefreshInternal();
				}
			}
		}
		UpdateScrollBars();
		UpdateHorzScrollBars();
		m_CellsPanel.UpdateCellControls();
	}

	protected override void OnFontChanged(EventArgs e)
	{
		SetDefaultRowHeight();
		m_CellsPanel.Font = Font;
		m_ColumnTitlePanel.Font = Font;
		m_RowTitlePanel.Font = Font;
		base.OnFontChanged(e);
	}

	private Column FindNextVisibleColumn(int col)
	{
		for (int i = col + 1; i < m_Columns.Count; i++)
		{
			if (m_Columns[i].Visible)
			{
				return m_Columns[i];
			}
		}
		return null;
	}

	internal void ResizeColumn(Column column, int new_size)
	{
		int num = new_size - column.Width;
		int num2 = ScaleDPI(16);
		if (column.WidthMode == Column.EWidthMode.Fill)
		{
			int num3 = m_Columns.IndexOf(column);
			if (num3 != m_Columns.Count - 1)
			{
				Column column2 = FindNextVisibleColumn(num3);
				if (column2 != null && column2.WidthMode == Column.EWidthMode.Explicit)
				{
					column2.Width = Math.Max(column2.Width - num, num2);
				}
			}
		}
		else
		{
			int col = m_Columns.IndexOf(column);
			Column column3 = FindNextVisibleColumn(col);
			if (column3 != null && column3.Width - num < num2)
			{
				num = num2 - column3.Width;
			}
			_ = column.Width;
			column.Width = Math.Max(column.Width + num, num2);
			if (column3 != null)
			{
				column3.Width = Math.Max(column3.Width - num, num2);
			}
		}
		UpdateColumnWidths();
		m_CellsPanel.OnColumnResizing();
		m_ColumnTitlePanel.Refresh();
		m_CellsPanel.Refresh();
	}

	private void SetDefaultRowHeight()
	{
		m_DefaultRowHeight = Font.Height + 2 * ScaleDPI(m_RowHeightPadding);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if (m_CellsPanel.GetColRow(Utils.Negate(m_ScrollPanel.ScrollPosition)).Valid)
		{
			int num = base.Height / 6;
			if (e.Delta < 0)
			{
				num = -num;
			}
			DoScroll(num);
		}
		base.OnMouseWheel(e);
	}

	private void DoScroll(int scroll_dist)
	{
		ColRow colRow = m_CellsPanel.GetColRow(Utils.Negate(m_ScrollPanel.ScrollPosition));
		if (colRow.Valid)
		{
			int num = m_CellsPanel.GetRowIndex(colRow.Row, include_expanded: false);
			int rowCount = m_CellsPanel.RowCount;
			int num2 = Math.Abs(scroll_dist);
			int num3 = 0;
			while (num3 < num2 && num >= 0 && num < rowCount)
			{
				Row row = m_CellsPanel.GetRow(num);
				if (scroll_dist < 0)
				{
					num3 += m_CellsPanel.GetCellRect(new ColRow(0, row)).Height;
					num++;
				}
				else
				{
					num--;
					num3 += m_CellsPanel.GetCellRect(new ColRow(0, row)).Height;
				}
			}
			num = Utils.Clamp(num, 0, rowCount - 1);
			Row row2 = m_CellsPanel.GetRow(num);
			ScrollToRow(row2);
		}
		UpdateSelectBox();
	}

	internal void UpdateScrollSize()
	{
		if (m_ScrollPanel != null)
		{
			m_ScrollPanel.UpdateScrollBars();
		}
	}

	public ColRow GetCell(Point pt)
	{
		pt = PointToScreen(pt);
		pt = m_CellsPanel.PointToClient(pt);
		return m_CellsPanel.GetColRow(pt);
	}

	public void ClearSelection()
	{
		m_CellsPanel.ClearSelection();
	}

	private void UpdateHorzScrollBars()
	{
		if (!m_ScrollColumnsHorz)
		{
			m_HorzScrollPanel.Visible = false;
			return;
		}
		bool visible = false;
		int num = 0;
		for (int i = 0; i < m_Columns.Count; i++)
		{
			Column column = m_Columns[i];
			if (column.Visible)
			{
				HScrollBar hScrollBar = m_HorzScrollBars[i];
				hScrollBar.Location = new Point(num, 0);
				hScrollBar.Size = new Size(column.Width, m_HorzScrollPanel.Height);
				int maxCellWidth = m_CellsPanel.GetMaxCellWidth(i);
				hScrollBar.Maximum = maxCellWidth;
				hScrollBar.LargeChange = column.Width;
				if (hScrollBar.Visible = hScrollBar.Maximum > column.Width)
				{
					visible = true;
				}
				num += column.Width;
			}
		}
		m_HorzScrollPanel.Visible = visible;
	}

	protected override void OnContextMenuStripChanged(EventArgs e)
	{
		m_CellsPanel.ContextMenuStrip = ContextMenuStrip;
		base.OnContextMenuStripChanged(e);
	}

	internal void OnCellChanged()
	{
		AddEmptyRowIfNeeded();
		if (this.CellChanged != null)
		{
			this.CellChanged(SelectedCells);
		}
	}

	private void AddEmptyRowIfNeeded()
	{
		if (!m_AddEmptyRow)
		{
			return;
		}
		if (LastRow == null || !AllCellsAreDefault(LastRow))
		{
			m_RootRow.ChildRows.Add(new Row(GetDefaultValue(0)));
			for (int i = 1; i < m_Columns.Count; i++)
			{
				LastRow.Cells.Add(new Cell(GetDefaultValue(i)));
			}
		}
		RefreshInternal();
		m_ScrollPanel.UpdateScrollBars();
	}

	public void FinishedAddingRows()
	{
		RefreshInternal();
		m_ScrollPanel.UpdateScrollBars();
	}

	public Row GetDisplayedRow(int index)
	{
		return m_RootRow.ChildRows.DisplayRows[index];
	}

	private bool AllCellsAreDefault(Row row)
	{
		for (int i = 0; i < m_Columns.Count; i++)
		{
			if (row.Cells.Count > i && !row.Cells[i].Value.Equals(GetDefaultValue(i)))
			{
				return false;
			}
		}
		return true;
	}

	private object GetDefaultValue(int column)
	{
		if (m_Columns.Count == 0)
		{
			return "";
		}
		Type type = m_Columns[column].Type;
		if (type == null)
		{
			return "";
		}
		if (type.IsValueType)
		{
			return Activator.CreateInstance(type);
		}
		return type.GetConstructor(new Type[0]).Invoke(new object[0]);
	}

	protected override void OnLeave(EventArgs e)
	{
		m_SelectBox.Visible = false;
		base.OnLeave(e);
	}

	protected override void OnEnter(EventArgs e)
	{
		UpdateSelectBox();
		base.OnEnter(e);
	}

	internal void OnRowMoved(Row row_being_moved, int old_index, int new_index)
	{
		if (this.RowMoved != null)
		{
			this.RowMoved(row_being_moved, old_index, new_index);
		}
	}

	public void SelectRow(int row)
	{
		if (row != -1)
		{
			SelectRow(m_RootRow.ChildRows[row]);
		}
		else
		{
			ClearSelection();
		}
	}

	public void SelectRow(Row row)
	{
		if (!RowSelected(row))
		{
			StartUpdate();
			ClearSelection();
			m_CellsPanel.SelectRow(row);
			UpdateSelectBox();
			EndUpdate();
			RefreshInternal();
		}
	}

	internal void RefreshRowTitlePanel()
	{
		m_RowTitlePanel.Refresh();
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		switch (keyData)
		{
		case Keys.Delete:
			HandleDeleteKey();
			break;
		case Keys.Home:
			HandleHomeKey();
			break;
		case Keys.End:
			HandleEndKey();
			break;
		case Keys.Home | Keys.Control:
			HandleHomeCtrlKey();
			break;
		}
		return base.ProcessDialogKey(keyData);
	}

	private void HandleHomeKey()
	{
		if (Rows.Count != 0)
		{
			ClearSelection();
			AddSelectedCell(new ColRow(0, Rows[0]));
			RefreshInternal();
		}
	}

	private void HandleEndKey()
	{
		if (Rows.Count != 0)
		{
			Row row = Rows[Rows.Count - 1];
			ClearSelection();
			AddSelectedCell(new ColRow(0, row));
			ScrollIntoView(row);
			RefreshInternal();
		}
	}

	private void HandleHomeCtrlKey()
	{
		if (m_RootRow.FirstDisplayedChild != null)
		{
			ClearSelection();
			AddSelectedCell(new ColRow(0, m_RootRow.FirstDisplayedChild));
		}
		RefreshInternal();
	}

	private void HandleDeleteKey()
	{
		if (ReadOnly || m_CellsPanel.EditControlVisible)
		{
			return;
		}
		bool flag = false;
		if (m_CellsPanel.SelRows.Count != 0)
		{
			if (m_CanAddRemoveRows)
			{
				bool cancel = false;
				if (this.DeletingRows != null)
				{
					this.DeletingRows(m_CellsPanel.SelRows, ref cancel);
				}
				if (!cancel)
				{
					DeleteRows(m_CellsPanel.SelRows);
				}
			}
			flag = true;
		}
		else if (m_CanAddRemoveRows && m_CellsPanel.SelectedCells.Count != 0 && (m_Columns.Count == 1 || m_Columns[LastSelectedCell.Col].DeleteKeyDeletesRow))
		{
			Set<Row> set = new Set<Row>();
			bool flag2 = true;
			foreach (ColRow selectedCell in m_CellsPanel.SelectedCells)
			{
				if (m_Columns.Count != 1 && !m_Columns[selectedCell.Col].DeleteKeyDeletesRow)
				{
					flag2 = false;
					break;
				}
				set.Add(selectedCell.Row);
			}
			List<Row> sel_rows = new List<Row>(set);
			if (flag2)
			{
				bool cancel2 = false;
				if (this.DeletingRows != null)
				{
					this.DeletingRows(sel_rows, ref cancel2);
				}
				if (!cancel2)
				{
					DeleteRow(LastSelectedCell.Row);
				}
				flag = true;
			}
		}
		if (!flag && LastSelectedCell.Valid)
		{
			ClearCell(LastSelectedCell);
		}
	}

	public void DeleteRows(ICollection<Row> rows)
	{
		Utils.RemoveChildren(m_CellsPanel.SelRows);
		foreach (Row selRow in m_CellsPanel.SelRows)
		{
			DeleteRow(selRow);
		}
		ClearSelection();
		UpdateSelectBox();
	}

	public void DeleteRow(Row row)
	{
		if (m_AddEmptyRow && row == m_RootRow.LastDisplayedChild)
		{
			return;
		}
		int index = row.Index;
		if (index != -1)
		{
			row.Parent.ChildRows.RemoveAt(index);
			if (this.RowRemoved != null)
			{
				this.RowRemoved(row.Parent, index);
			}
			RefreshInternal();
		}
	}

	private void ClearCell(ColRow colrow)
	{
		Cell cell = colrow.GetCell();
		if (cell != null)
		{
			if (cell.Value is string)
			{
				cell.Value = "";
			}
			if (cell.Value is ITextObject)
			{
				((ITextObject)cell.Value).Text = "";
			}
			else if (cell.Value.GetType().IsPrimitive)
			{
				cell.Value = Activator.CreateInstance(cell.Value.GetType());
			}
			m_CellsPanel.RefreshCell(colrow);
			OnCellChanged();
		}
	}

	internal void InsertEmptyRowBefore(Row row)
	{
		Row row2 = new Row();
		for (int i = 0; i < m_Columns.Count; i++)
		{
			row2.Cells.Add(new Cell(""));
		}
		int index = row.Index;
		row.Parent.ChildRows.InsertDisplayRow(index, row2);
		if (this.RowInserted != null)
		{
			this.RowInserted(row.Parent, index);
		}
		RefreshInternal();
	}

	public bool RowSelected(Row row)
	{
		return m_CellsPanel.SelRows.Contains(row);
	}

	public void SelectRowRange(Row first, Row last)
	{
		StartUpdate();
		ClearSelection();
		m_CellsPanel.SelectRowRange(first, last);
		EndUpdate();
		RefreshInternal();
	}

	public void ExpandSelectedRowRecursive()
	{
		m_CellsPanel.ExpandSelectedRowRecursive();
	}

	public void CollapseSelectedRowRecursive()
	{
		m_CellsPanel.CollapseSelectedRowRecursive();
	}

	public void CopySelectedCellsToClipboard()
	{
		m_CellsPanel.CopySelectedCellsToClipboard();
	}

	public void RegisterEditControl(Type cell_type, Type control_type)
	{
		m_CellsPanel.RegisterEditControl(cell_type, control_type);
	}

	public void SelectAndEditCell(ColRow colrow)
	{
		m_CellsPanel.SelectAndEditCell(colrow);
	}

	internal void OnSlideDragFinished(Row row_bring_moved, int old_index, int new_index)
	{
		if (this.SlideDragFinished != null)
		{
			this.SlideDragFinished(row_bring_moved, old_index, new_index);
		}
	}

	public string GetColumnName(int col)
	{
		return m_Columns[col].Name;
	}

	public int IndexOf(Column column)
	{
		return m_Columns.IndexOf(column);
	}

	private void RefreshInternal()
	{
		if (m_StartEndUpdateCount == 0)
		{
			try
			{
                Refresh();
            }
			catch(Exception ex) 
			{

			}
			
		}
	}

	public void StartUpdate()
	{
		m_StartEndUpdateCount++;
		m_CellsPanel.StartUpdate();
	}

	public void EndUpdate()
	{
		m_StartEndUpdateCount--;
		if (m_StartEndUpdateCount == 0)
		{
			m_CellsPanel.EndUpdate();
			Refresh();
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
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.DoubleBuffered = true;
		base.Name = "HDataGrid";
		base.Size = new System.Drawing.Size(667, 411);
		base.ResumeLayout(false);
	}
}
