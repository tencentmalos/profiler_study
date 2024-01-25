using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SCL;

internal class CellsPanel : UserControl
{
	private class CellDetails
	{
		public ColRow m_ColRow = ColRow.Invalid;

		public Rectangle m_Rect;
	}

	public enum SelectMode
	{
		Single,
		Multi,
		Range
	}

	private Row m_RootRow;

	private List<Column> m_Columns;

	private SolidBrush m_LightRowBrush = new SolidBrush(Color.FromArgb(235, 235, 235));

	private SolidBrush m_DarkRowBrush = new SolidBrush(Color.FromArgb(230, 230, 230));

	private SolidBrush m_SelectedCellBrush = new SolidBrush(Color.FromArgb(188, 180, 250));

	private SolidBrush m_SelectedRowBrush = new SolidBrush(Color.FromArgb(210, 210, 255));

	private SolidBrush m_HighlightRowBrush = new SolidBrush(Color.FromArgb(225, 225, 255));

	private Pen m_GridLinePen = SystemPens.ControlDark;

	private bool m_HasHeirachy;

	private int m_IndentOffset = 12;

	private int m_TextOffset = 4;

	private const int m_IndentedTextOffset = 14;

	private int m_ExpandBoxOffset = 4;

	private int m_ExpandBoxSize = 8;

	private bool m_MouseEnabled = true;

	private bool m_DraggingColumn;

	private int m_LastMouseX;

	private Column m_ColumnBeingResized;

	private StringControl m_StringControl = new StringControl();

	private BoolControl m_BoolControl = new BoolControl();

	private EnumControl m_EnumControl = new EnumControl();

	private bool m_ControlSelectedWithEnter;

	private Set<ColRow> m_SelectedCells = new Set<ColRow>();

	private ColRow m_FirstSelectedCell = ColRow.Invalid;

	private ColRow m_LastSelectedCell = ColRow.Invalid;

	private Set<ColRow> m_SelectedCellsBeforeRangeDrag;

	private bool m_StartedSelectDrag;

	private bool m_DraggingCells;

	private Point m_LastMousePos;

	private List<Row> m_RowsBeingDragged = new List<Row>();

	private DragNodesForm m_DragNodesForm;

	private EDropMode m_DropMode;

	private CellDetails m_DropNodeTargetCellDetails;

	private bool m_CtrlKeyHeld;

	private bool m_ShiftKeyHeld;

	private bool m_HighlightSelected;

	private bool m_RenamingCell;

	private bool m_CanRenameCell = true;

	private bool m_MoveCellsEnabled;

	private Point m_ScrollPosition = new Point(0, 0);

	private const int m_MaxSingleCellDrawCount = 8;

	private ColRow m_PrevLastSelectedCell = ColRow.Invalid;

	private bool m_PadEmptyRows;

	private bool m_ReadOnly;

	private bool m_AlternateRowColours = true;

	private bool m_DrawRowLines;

	private bool m_DrawColumnLines = true;

	private SolidBrush m_WindowBrush = new SolidBrush(SystemColors.Window);

	private bool m_SelectNextCellAfterEdit = true;

	private SolidBrush m_BackColourBrush = new SolidBrush(SystemColors.AppWorkspace);

	private ContextMenuStrip m_CellContextMenu = new ContextMenuStrip();

	private ToolStripMenuItem m_ExpandAllMenuItem = new ToolStripMenuItem("Expand All");

	private ToolStripMenuItem m_CollapseAllMenuItem = new ToolStripMenuItem("Collapse All");

	private ToolStripMenuItem m_CopyMenuItem = new ToolStripMenuItem("Copy");

	private bool m_HighlightSelectedRow;

	private bool m_SlideDrag;

	private bool m_SelectRowOnNextMouseMove;

	private Point m_LastMouseClickPt;

	private bool m_IgnoreRowEvents;

	private bool m_StartedSlidingRow;

	private int m_SlideDragOldIndex;

	private bool m_HasCellControls;

	private IDataGridEditControl m_CurCustomControl;

	private Dictionary<Type, Type> m_CustomEditControls = new Dictionary<Type, Type>();

	private bool m_ClearSelectionOnMouseLeave;

	private bool m_HighlightRow;

	private Row m_HighlightedRow;

	private bool m_CustomEditControlClosing;

	private bool m_DrawLastColumnLine;

	private SolidBrush m_CellTextBrush;

	private Dictionary<Color, Brush> m_Brushes = new Dictionary<Color, Brush>();

	private ColRow m_FirstSelectCell = ColRow.Invalid;

	private bool m_SelectByRow;

	private Set<Row> m_SelRows = new Set<Row>();

	private int m_StartEndUpdateCount;

	private float m_DPIScale;

	public bool EditControlVisible
	{
		get
		{
			if (!m_StringControl.Visible && !m_BoolControl.Visible && !m_EnumControl.Visible)
			{
				if (m_CurCustomControl != null)
				{
					return !m_CustomEditControlClosing;
				}
				return false;
			}
			return true;
		}
	}

	private HDataGrid HDataGrid
	{
		get
		{
			Control control = base.Parent;
			while (control != null && !(control is HDataGrid))
			{
				control = control.Parent;
			}
			return (HDataGrid)control;
		}
	}

	private bool IsSelectedCellBoolImage
	{
		get
		{
			if (IsSelectedCellBool())
			{
				return IsBoolImage(m_LastSelectedCell.Col);
			}
			return false;
		}
	}

	public Color WindowColour
	{
		get
		{
			return m_WindowBrush.Color;
		}
		set
		{
			m_WindowBrush = new SolidBrush(value);
			Refresh();
		}
	}

	private int VisibleColumnCount
	{
		get
		{
			int num = 0;
			foreach (Column column in m_Columns)
			{
				if (column.Visible)
				{
					num++;
				}
			}
			return num;
		}
	}

	private int LastVisibleColumnIndex
	{
		get
		{
			int num = m_Columns.Count - 1;
			while (num > 0 && !m_Columns[num].Visible)
			{
				num--;
			}
			return num;
		}
	}

	private bool HasFillColumn
	{
		get
		{
			foreach (Column column in m_Columns)
			{
				if (column.Visible && column.WidthMode == Column.EWidthMode.Fill)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool MouseEnabled
	{
		get
		{
			return m_MouseEnabled;
		}
		set
		{
			m_MouseEnabled = value;
		}
	}

	public ColRow LastSelectedCell => m_LastSelectedCell;

	public ICollection<ColRow> SelectedCells => m_SelectedCells;

	private ScrollPanel ScrollPanel => base.Parent as ScrollPanel;

	public bool HighlightSelected
	{
		get
		{
			return m_HighlightSelected;
		}
		set
		{
			if (m_HighlightSelected != value)
			{
				m_HighlightSelected = value;
				UpdateCellControls();
				Invalidate();
				UpdateInternal();
			}
		}
	}

	public bool CanRenameCell
	{
		get
		{
			return m_CanRenameCell;
		}
		set
		{
			m_CanRenameCell = value;
		}
	}

	public bool MoveCellsEnabled
	{
		get
		{
			return m_MoveCellsEnabled;
		}
		set
		{
			m_MoveCellsEnabled = value;
		}
	}

	public Point ScrollPosition
	{
		get
		{
			return m_ScrollPosition;
		}
		set
		{
			m_ScrollPosition = value;
		}
	}

	public bool PadEmptyRows
	{
		get
		{
			return m_PadEmptyRows;
		}
		set
		{
			m_PadEmptyRows = value;
		}
	}

	private int DefaultRowHeight
	{
		get
		{
			if (HDataGrid == null)
			{
				return 15;
			}
			return HDataGrid.DefaultRowHeight;
		}
	}

	public int RowCount
	{
		get
		{
			int num = 0;
			RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
			while (rowIterator.MoveNext())
			{
				num++;
			}
			return num;
		}
	}

	public bool ReadOnly
	{
		get
		{
			return m_ReadOnly;
		}
		set
		{
			m_ReadOnly = value;
		}
	}

	public bool AlternateRowColours
	{
		get
		{
			return m_AlternateRowColours;
		}
		set
		{
			m_AlternateRowColours = value;
		}
	}

	public bool DrawRowLines
	{
		get
		{
			return m_DrawRowLines;
		}
		set
		{
			m_DrawRowLines = value;
		}
	}

	public bool DrawColumnLines
	{
		get
		{
			return m_DrawColumnLines;
		}
		set
		{
			m_DrawColumnLines = value;
		}
	}

	public Color SelectedCellColour
	{
		get
		{
			return m_SelectedCellBrush.Color;
		}
		set
		{
			m_SelectedCellBrush = new SolidBrush(value);
		}
	}

	public Color SelectedRowColour
	{
		get
		{
			return m_SelectedRowBrush.Color;
		}
		set
		{
			m_SelectedRowBrush = new SolidBrush(value);
		}
	}

	public bool SelectNextCellAfterEdit
	{
		get
		{
			return m_SelectNextCellAfterEdit;
		}
		set
		{
			m_SelectNextCellAfterEdit = value;
		}
	}

	public bool HighlightSelectedRow
	{
		get
		{
			return m_HighlightSelectedRow;
		}
		set
		{
			m_HighlightSelectedRow = value;
		}
	}

	public bool HighlightRow
	{
		get
		{
			return m_HighlightRow;
		}
		set
		{
			m_HighlightRow = value;
		}
	}

	public bool SlideDrag
	{
		get
		{
			return m_SlideDrag;
		}
		set
		{
			m_SlideDrag = value;
		}
	}

	public bool CtrlKeyHeld => m_CtrlKeyHeld;

	public bool ClearSelectionOnMouseLeave
	{
		get
		{
			return m_ClearSelectionOnMouseLeave;
		}
		set
		{
			m_ClearSelectionOnMouseLeave = value;
		}
	}

	public Color HighlightRowColour
	{
		get
		{
			return m_HighlightRowBrush.Color;
		}
		set
		{
			m_HighlightRowBrush = new SolidBrush(value);
			RefreshInternal();
		}
	}

	public int HorizontalTextOffset
	{
		get
		{
			return m_TextOffset;
		}
		set
		{
			m_TextOffset = value;
		}
	}

	public Color LightRowColour
	{
		get
		{
			return m_LightRowBrush.Color;
		}
		set
		{
			m_LightRowBrush = new SolidBrush(value);
			RefreshInternal();
		}
	}

	public Color DarkRowColour
	{
		get
		{
			return m_DarkRowBrush.Color;
		}
		set
		{
			m_DarkRowBrush = new SolidBrush(value);
			RefreshInternal();
		}
	}

	public bool DrawLastColumnLine
	{
		get
		{
			return m_DrawLastColumnLine;
		}
		set
		{
			m_DrawLastColumnLine = value;
		}
	}

	public bool SelectByRow
	{
		get
		{
			return m_SelectByRow;
		}
		set
		{
			m_SelectByRow = value;
		}
	}

	public Set<Row> SelRows => m_SelRows;

	public CellsPanel(Row root_row, List<Column> columns, ControlCollection parent_controls, float dpi_scale)
	{
		m_RootRow = root_row;
		m_Columns = columns;
		InitialiseCellContextMenu();
		m_DPIScale = dpi_scale;
		m_RootRow.RowRemoved += RowRemovedEvent;
		DoubleBuffered = true;
		m_StringControl.KeyDown += StringControlKeyDown;
		m_StringControl.LostFocus += StringControlLostFocus;
		parent_controls.Add(m_StringControl);
		m_BoolControl.ValueChanged += BoolControlValueChanged;
		m_BoolControl.KeyDown += BoolControlKeyDown;
		m_BoolControl.DropDownClosed += BoolControlDropDownClosed;
		parent_controls.Add(m_BoolControl);
		m_EnumControl.ValueChanged += EnumControlValueChanged;
		m_EnumControl.KeyDown += EnumControlKeyDown;
		parent_controls.Add(m_EnumControl);
		SetCellBackPointers();
		AddCellControls();
		UpdateCellControls();
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void InitialiseCellContextMenu()
	{
		m_CellContextMenu.SuspendLayout();
		m_CellContextMenu.Items.Add(m_ExpandAllMenuItem);
		m_CellContextMenu.Items.Add(m_CollapseAllMenuItem);
		m_CellContextMenu.Items.Add(m_CopyMenuItem);
		m_CellContextMenu.ResumeLayout();
		m_ExpandAllMenuItem.Click += ExpandAllMenuItemClick;
		m_CollapseAllMenuItem.Click += CollapseAllMenuItemClick;
		m_CopyMenuItem.Click += CopyMenuItemClick;
		ContextMenuStrip = m_CellContextMenu;
	}

	private int ColRowComparer(ColRow colrow1, ColRow colrow2)
	{
		int rowIndex = GetRowIndex(colrow1.Row, include_expanded: false);
		int rowIndex2 = GetRowIndex(colrow2.Row, include_expanded: false);
		if (rowIndex < rowIndex2)
		{
			return -1;
		}
		if (rowIndex > rowIndex2)
		{
			return 1;
		}
		if (colrow1.Col < colrow2.Col)
		{
			return -1;
		}
		if (colrow1.Col > colrow2.Col)
		{
			return 1;
		}
		return 0;
	}

	private void CopyMenuItemClick(object sender, EventArgs e)
	{
		CopySelectedCellsToClipboard();
	}

	public void CopySelectedCellsToClipboard()
	{
		if (m_SelectedCells.Count == 0)
		{
			return;
		}
		List<ColRow> list = new List<ColRow>(m_SelectedCells);
		list.Sort(ColRowComparer);
		bool flag = true;
		int num = -1;
		foreach (ColRow item in list)
		{
			if (num != -1 && item.Col != num)
			{
				flag = false;
				break;
			}
			num = item.Col;
		}
		string text = "";
		Row row = list[0].Row;
		foreach (ColRow item2 in list)
		{
			if (item2.Row != row)
			{
				text += "\r\n";
				row = item2.Row;
			}
			string text2 = GetCellValueAsString(item2.GetCellValue());
			if (text2 == null)
			{
				text2 = "";
			}
			text += text2;
			if (!flag)
			{
				text += ",";
			}
		}
		Clipboard.SetText(text);
	}

	private void ExpandAllMenuItemClick(object sender, EventArgs e)
	{
		ExpandSelectedRowRecursive();
	}

	public void ExpandSelectedRowRecursive()
	{
		if (m_FirstSelectedCell.Valid)
		{
			ExpandRow(m_FirstSelectedCell.Row, expanded: true, recurse: true);
			RefreshInternal();
		}
	}

	private void CollapseAllMenuItemClick(object sender, EventArgs e)
	{
		CollapseSelectedRowRecursive();
	}

	public void CollapseSelectedRowRecursive()
	{
		if (m_FirstSelectedCell.Valid)
		{
			ExpandRow(m_FirstSelectedCell.Row, expanded: false, recurse: true);
			RefreshInternal();
		}
	}

	private void StringControlLostFocus(object sender, EventArgs e)
	{
		if (m_StringControl.Enabled && m_StringControl.Visible)
		{
			SubmitCellChange();
			CloseStringControl();
		}
	}

	private void RowRemovedEvent()
	{
		if (!m_IgnoreRowEvents)
		{
			ClearSelection();
		}
	}

	private void BoolControlDropDownClosed(object sender, EventArgs e)
	{
		Focus();
	}

	private void BoolControlKeyDown(object sender, KeyEventArgs e)
	{
		ColRow lastSelectedCell = m_LastSelectedCell;
		if (!lastSelectedCell.Valid)
		{
			return;
		}
		if (e.KeyCode == Keys.Return)
		{
			SubmitCellChange();
			CloseBoolControl();
			Row nextRow = Utils.GetNextRow(lastSelectedCell.Row);
			if (nextRow != null)
			{
				SelectCell(new ColRow(lastSelectedCell.Col, nextRow));
			}
			else
			{
				SelectCell(ColRow.Invalid);
			}
		}
		else if (e.KeyCode == Keys.Escape)
		{
			object cellValue = lastSelectedCell.GetCellValue();
			m_BoolControl.Value = (bool)cellValue;
			CloseBoolControl();
		}
	}

	private void SubmitCellChange()
	{
		object new_value = null;
		if (m_StringControl.Visible)
		{
			new_value = m_StringControl.Value;
		}
		else if (m_BoolControl.Visible)
		{
			new_value = m_BoolControl.Value;
		}
		else if (m_EnumControl.Visible)
		{
			new_value = m_EnumControl.Value;
		}
		else if (m_CurCustomControl != null)
		{
			new_value = m_CurCustomControl.Value;
		}
		SubmitCellChanged(new_value);
	}

	private void SubmitCellChanged(object new_value)
	{
		bool flag = false;
		foreach (ColRow item in new Set<ColRow>(m_SelectedCells))
		{
			if (item.GetCellValue() is ITextObject textObject && new_value is string)
			{
				if (textObject.Text != (string)new_value)
				{
					textObject.Text = (string)new_value;
					flag = true;
				}
			}
			else if (new_value != null)
			{
				if (item.GetCellValue() is IDataGridControl dataGridControl)
				{
					dataGridControl.Value = new_value;
					flag = true;
				}
				else if (GetCellValueType(item) == new_value.GetType() && !object.Equals(item.GetCellValue(), new_value))
				{
					SetStringCellValue(item, new_value);
					flag = true;
				}
			}
		}
		if (flag)
		{
			HDataGrid.OnCellChanged();
			RefreshInternal();
		}
	}

	private static Type GetCellValueType(ColRow colrow)
	{
		if (colrow.GetCellType() == null && colrow.GetCellValue() == null)
		{
			return typeof(string);
		}
		return colrow.GetCellType();
	}

	private void SetStringCellValue(ColRow colrow, object new_value)
	{
		Type cellValueType = GetCellValueType(colrow);
		if (cellValueType == typeof(int))
		{
			int num = 0;
			try
			{
				num = Convert.ToInt32(new_value);
			}
			catch (Exception)
			{
				num = (int)colrow.GetCellValue();
			}
			colrow.SetCellValue(num);
		}
		else if (cellValueType == typeof(float))
		{
			float num2 = 0f;
			try
			{
				num2 = Convert.ToSingle(new_value);
			}
			catch (Exception)
			{
				num2 = (float)colrow.GetCellValue();
			}
			colrow.SetCellValue(num2);
		}
		else
		{
			colrow.SetCellValue(new_value);
		}
	}

	protected override void OnLostFocus(EventArgs e)
	{
		m_CtrlKeyHeld = false;
		m_ShiftKeyHeld = false;
		base.OnLostFocus(e);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		m_CtrlKeyHeld = false;
		m_ShiftKeyHeld = false;
		base.OnGotFocus(e);
	}

	public void MyProcessKeyPreview(ref Message m)
	{
		switch ((int)m.WParam)
		{
		case 16:
			m_ShiftKeyHeld = m.Msg == 256;
			break;
		case 17:
			m_CtrlKeyHeld = m.Msg == 256;
			break;
		}
		if (m.Msg == 256)
		{
			switch ((int)m.WParam)
			{
			case 33:
				PageUp();
				break;
			case 34:
				PageDown();
				break;
			}
		}
	}

	private SelectMode GetSelectMode()
	{
		if (m_CtrlKeyHeld)
		{
			return SelectMode.Multi;
		}
		if (m_ShiftKeyHeld)
		{
			return SelectMode.Range;
		}
		return SelectMode.Single;
	}

	private void PageUp()
	{
		if (m_LastSelectedCell != null)
		{
			int num = 0;
			ReverseRowIterator reverseRowIterator = new ReverseRowIterator(m_LastSelectedCell.Row);
			while (reverseRowIterator.MoveNext() && num < base.ClientSize.Height)
			{
				num += GetCellRect(new ColRow(0, reverseRowIterator.Current)).Height;
			}
			Row row = ((reverseRowIterator.Current != null) ? reverseRowIterator.Current : m_RootRow.FirstDisplayedChild);
			AddSelectedCell(new ColRow(m_LastSelectedCell.Col, row), GetSelectMode());
		}
	}

	private void PageDown()
	{
		if (m_LastSelectedCell == null)
		{
			return;
		}
		RowIterator rowIterator = new RowIterator(m_LastSelectedCell.Row);
		Row row = null;
		int num = 0;
		while (rowIterator.MoveNext())
		{
			num += GetCellRect(new ColRow(0, rowIterator.Current)).Height;
			if (num >= base.ClientSize.Height)
			{
				break;
			}
			row = rowIterator.Current;
		}
		Row row2 = ((rowIterator.Current != null) ? rowIterator.Current : row);
		if (row2 != null)
		{
			AddSelectedCell(new ColRow(m_LastSelectedCell.Col, row2), GetSelectMode());
		}
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		Keys keys = keyData;
		if ((keys & Keys.Shift) == Keys.Shift)
		{
			keys ^= Keys.Shift;
		}
		ColRow lastSelectedCell = m_LastSelectedCell;
		if (lastSelectedCell.Valid && m_CurCustomControl == null)
		{
			switch (keys)
			{
			case Keys.Left:
				if (lastSelectedCell.Col == 0)
				{
					if (lastSelectedCell.Row.ChildRows.Count != 0 && lastSelectedCell.Row.Expanded)
					{
						ExpandRow(lastSelectedCell.Row, expanded: false, recurse: false);
						HDataGrid.Refresh();
					}
					else if (lastSelectedCell.Row.Parent.Parent != null)
					{
						SelectCell(new ColRow(0, lastSelectedCell.Row.Parent));
					}
				}
				else
				{
					int num = lastSelectedCell.Col - 1;
					while (num > 0 && !m_Columns[num].Visible)
					{
						num--;
					}
					if (num >= 0)
					{
						AddSelectedCell(new ColRow(num, lastSelectedCell.Row), GetSelectMode());
					}
				}
				return true;
			case Keys.Right:
				if (lastSelectedCell.Col == 0 && lastSelectedCell.Row.ChildRows.Count != 0)
				{
					if (!lastSelectedCell.Row.Expanded)
					{
						ExpandRow(lastSelectedCell.Row, expanded: true, recurse: false);
						HDataGrid.Refresh();
					}
				}
				else if (lastSelectedCell.Col < m_Columns.Count - 1)
				{
					int i;
					for (i = lastSelectedCell.Col + 1; i < m_Columns.Count - 1 && !m_Columns[i].Visible; i++)
					{
					}
					if (i < m_Columns.Count)
					{
						AddSelectedCell(new ColRow(i, lastSelectedCell.Row), GetSelectMode());
					}
				}
				return true;
			case Keys.Up:
			{
				Row prevRow = Utils.GetPrevRow(lastSelectedCell.Row);
				if (prevRow != null)
				{
					AddSelectedCell(new ColRow(lastSelectedCell.Col, prevRow), GetSelectMode());
				}
				return true;
			}
			case Keys.Down:
			{
				Row nextRow = Utils.GetNextRow(lastSelectedCell.Row);
				if (nextRow != null)
				{
					AddSelectedCell(new ColRow(lastSelectedCell.Col, nextRow), GetSelectMode());
				}
				return true;
			}
			case Keys.F2:
				if (m_CanRenameCell)
				{
					RenameSelected();
				}
				return true;
			}
		}
		return base.ProcessDialogKey(keyData);
	}

	private void ShowBoolControl()
	{
		if (!m_BoolControl.Visible)
		{
			object cellValue = m_LastSelectedCell.GetCellValue();
			if (cellValue != null)
			{
				m_BoolControl.Value = (bool)cellValue;
				Rectangle cellRect = GetCellRect(m_LastSelectedCell);
				cellRect = RectangleToScreen(cellRect);
				cellRect = HDataGrid.RectangleToClient(cellRect);
				m_BoolControl.Location = cellRect.Location;
				m_BoolControl.Size = cellRect.Size;
				m_BoolControl.Show();
				m_BoolControl.BringToFront();
				m_BoolControl.Focus();
				m_BoolControl.DroppedDown = true;
			}
		}
	}

	private void ShowEnumControl()
	{
		if (!m_EnumControl.Visible)
		{
			object cellValue = m_LastSelectedCell.GetCellValue();
			if (cellValue != null)
			{
				m_EnumControl.Value = cellValue;
				Rectangle cellRect = GetCellRect(m_LastSelectedCell);
				cellRect = RectangleToScreen(cellRect);
				cellRect = HDataGrid.RectangleToClient(cellRect);
				m_EnumControl.Location = cellRect.Location;
				m_EnumControl.Size = cellRect.Size;
				m_EnumControl.Show();
				m_EnumControl.BringToFront();
				m_EnumControl.Focus();
				m_EnumControl.DroppedDown = true;
			}
		}
	}

	private void ShowCustomControl()
	{
		if (m_CurCustomControl != null)
		{
			return;
		}
		object cellValue = m_LastSelectedCell.GetCellValue();
		if (cellValue != null)
		{
			Type key = GetCellType(m_LastSelectedCell);
			if (!m_CustomEditControls.ContainsKey(key))
			{
				key = m_LastSelectedCell.GetCellValue().GetType();
			}
			Control control = (Control)m_CustomEditControls[key].GetConstructor(new Type[0]).Invoke(new object[0]);
			HDataGrid.Controls.Add(control);
			Rectangle cellRect = GetCellRect(m_LastSelectedCell);
			cellRect = RectangleToScreen(cellRect);
			cellRect = HDataGrid.RectangleToClient(cellRect);
			control.Location = cellRect.Location;
			control.Size = cellRect.Size;
			m_CurCustomControl = (IDataGridEditControl)control;
			object obj = cellValue;
			if (obj is IDataGridControl)
			{
				obj = ((IDataGridControl)obj).Value;
			}
			m_CurCustomControl.Value = obj;
			m_CurCustomControl.EditControlValueChanged += CustomControlValueChanged;
			control.BringToFront();
			control.Focus();
		}
	}

	private void CustomControlValueChanged(bool close)
	{
		if (close)
		{
			m_CustomEditControlClosing = true;
			SubmitCellChange();
			m_CustomEditControlClosing = false;
			CloseCustomControl();
		}
		else
		{
			SubmitCellChange();
		}
	}

	private bool IsSelectedCellBool()
	{
		if (m_LastSelectedCell.Valid)
		{
			return m_LastSelectedCell.GetCellValue() is bool;
		}
		return false;
	}

	private bool IsColRowEnum()
	{
		return m_LastSelectedCell.GetCellValue()?.GetType().IsEnum ?? false;
	}

	private bool IsCellMappedToCustomControl(ColRow colrow)
	{
		Type cellType = GetCellType(colrow);
		object cellValue = colrow.GetCellValue();
		if (cellValue == null)
		{
			return false;
		}
		Type key = cellValue?.GetType();
		if (cellType != null)
		{
			if (!m_CustomEditControls.ContainsKey(cellType))
			{
				return m_CustomEditControls.ContainsKey(key);
			}
			return true;
		}
		return false;
	}

	private Type GetCellType(ColRow colrow)
	{
		if (!m_LastSelectedCell.Valid)
		{
			return null;
		}
		return m_LastSelectedCell.GetCell()?.GetType();
	}

	private bool ColumnReadOnly(int col)
	{
		return m_Columns[col].ReadOnly;
	}

	protected override void OnKeyPress(KeyPressEventArgs e)
	{
		if (m_LastSelectedCell.Valid && !m_CtrlKeyHeld && e.KeyChar >= ' ' && m_LastSelectedCell.GetCell() != null && !m_LastSelectedCell.GetCell().ReadOnly && !ColumnReadOnly(m_LastSelectedCell.Col))
		{
			object cellValue = m_LastSelectedCell.GetCellValue();
			Type cellType = m_LastSelectedCell.GetCellType();
			if (Utils.IsValidCellChar(e.KeyChar, cellValue, cellType))
			{
				ShowStringControl(m_LastSelectedCell, e.KeyChar.ToString());
			}
		}
		base.OnKeyPress(e);
	}

	private void ShowStringControl(ColRow colrow, string new_char)
	{
		if (colrow.GetCell() != null && !colrow.GetCell().ReadOnly && !ColumnReadOnly(colrow.Col))
		{
			object obj = colrow.GetCellValue();
			Type cellType = colrow.GetCellType();
			if (obj == null && cellType == null)
			{
				obj = "";
				cellType = typeof(string);
			}
			if (obj != null && !m_StringControl.Visible && !(obj is bool) && !obj.GetType().IsEnum && (!(obj is Control) || obj is ITextObject) && !IsCellMappedToCustomControl(colrow))
			{
				string text = null;
				text = ((!(obj is ITextObject)) ? obj.ToString() : ((ITextObject)obj).Text);
				ShowStringControl(text, colrow.GetCellType(), new_char);
			}
		}
	}

	private bool ShowStringControl(string value, Type type, string text)
	{
		return ShowStringControl(value, type, text, select_all: false);
	}

	private bool ShowStringControl(string value, Type type, string text, bool select_all)
	{
		if (m_ReadOnly || !m_LastSelectedCell.Valid)
		{
			return false;
		}
		Rectangle cellRect = GetCellRect(m_LastSelectedCell);
		cellRect = RectangleToScreen(cellRect);
		cellRect = HDataGrid.RectangleToClient(cellRect);
		m_StringControl.Enabled = true;
		m_StringControl.Value = value;
		m_StringControl.Type = type;
		m_StringControl.Location = cellRect.Location;
		m_StringControl.Size = cellRect.Size;
		m_StringControl.Text = text;
		m_StringControl.BackColor = m_LightRowBrush.Color;
		m_StringControl.Show();
		m_StringControl.Focus();
		m_StringControl.SelectionLength = (select_all ? text.Length : 0);
		m_StringControl.SelectionStart = ((!select_all) ? 1 : 0);
		m_StringControl.BringToFront();
		return true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		switch (e.KeyCode)
		{
		case Keys.Return:
			e.Handled = EditCell();
			break;
		case Keys.C:
			if (m_CtrlKeyHeld)
			{
				CopySelectedCellsToClipboard();
			}
			break;
		case Keys.A:
			if (m_CtrlKeyHeld)
			{
				SelectAllVisibleCells();
			}
			break;
		case Keys.Space:
			if (IsSelectedCellBoolImage)
			{
				ToggleSelectedBoolValue();
			}
			else
			{
				e.Handled = EditCell();
			}
			break;
		}
		base.OnKeyDown(e);
	}

	private void ToggleSelectedBoolValue()
	{
		bool flag = !(bool)m_LastSelectedCell.GetCellValue();
		SubmitCellChanged(flag);
	}

	private bool EditCell()
	{
		if (ShowControlForCell())
		{
			m_ControlSelectedWithEnter = true;
			return true;
		}
		if (m_LastSelectedCell.Col == 0 && m_LastSelectedCell.Row.ChildRows.Count != 0)
		{
			ExpandRow(m_LastSelectedCell.Row, !m_LastSelectedCell.Row.Expanded, m_ShiftKeyHeld);
			HDataGrid.Refresh();
		}
		return false;
	}

	private void SelectAllVisibleCells()
	{
		HDataGrid.StartSelectingMultipleCells();
		ClearSelection();
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			for (int i = 0; i < m_Columns.Count; i++)
			{
				if (m_Columns[i].Visible)
				{
					AddSelectedCell(new ColRow(i, rowIterator.Current));
				}
			}
		}
		HDataGrid.StopSelectingMultipleCells();
		RefreshInternal();
	}

	private void ExpandRow(Row row, bool expanded, bool recurse)
	{
		ExpandRowRecursive(row, expanded, recurse);
		UpdateCellControls();
		HDataGrid.OnRowExpandedChanged(row);
	}

	private void ExpandRowRecursive(Row row, bool expanded, bool recurse)
	{
		if (row.Expanded != expanded)
		{
			row.Expanded = expanded;
		}
		if (!recurse)
		{
			return;
		}
		foreach (Row childRow in row.ChildRows)
		{
			ExpandRowRecursive(childRow, expanded, recurse);
		}
	}

	private bool ShowControlForCell()
	{
		if (m_ReadOnly || !m_LastSelectedCell.Valid || ColumnReadOnly(m_LastSelectedCell.Col))
		{
			return false;
		}
		if (IsSelectedCellBool())
		{
			if (IsBoolImage(m_LastSelectedCell.Col))
			{
				return false;
			}
			ShowBoolControl();
			m_StartedSelectDrag = false;
			return true;
		}
		if (IsColRowEnum())
		{
			ShowEnumControl();
			m_StartedSelectDrag = false;
			return true;
		}
		if (IsCellMappedToCustomControl(m_LastSelectedCell))
		{
			ShowCustomControl();
			m_StartedSelectDrag = false;
			return true;
		}
		return false;
	}

	private void CloseBoolControl()
	{
		if (m_BoolControl.Visible)
		{
			Focus();
			m_BoolControl.Hide();
		}
	}

	private void CloseEnumControl()
	{
		if (m_EnumControl.Visible)
		{
			Focus();
			m_EnumControl.Hide();
		}
	}

	private void CloseStringControl()
	{
		if (m_StringControl.Visible)
		{
			m_StringControl.Hide();
			m_RenamingCell = false;
		}
	}

	private void CloseCustomControl()
	{
		if (m_CurCustomControl != null)
		{
			m_CurCustomControl.EditControlValueChanged -= CustomControlValueChanged;
			Control value = (Control)m_CurCustomControl;
			HDataGrid.Controls.Remove(value);
			m_CurCustomControl = null;
		}
	}

	private void EnumControlKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			SubmitCellChange();
			CloseEnumControl();
			if (m_LastSelectedCell.Valid)
			{
				Row nextRow = Utils.GetNextRow(m_LastSelectedCell.Row);
				if (nextRow != null)
				{
					SelectCell(new ColRow(m_LastSelectedCell.Col, nextRow));
				}
				else
				{
					SelectCell(ColRow.Invalid);
				}
			}
		}
		else if (e.KeyCode == Keys.Escape)
		{
			object cellValue = m_LastSelectedCell.GetCellValue();
			if (cellValue != null)
			{
				m_EnumControl.Value = cellValue;
			}
			CloseEnumControl();
		}
	}

	private void BoolControlValueChanged()
	{
		if (m_BoolControl.Visible && !m_ControlSelectedWithEnter)
		{
			SubmitCellChange();
			CloseBoolControl();
		}
	}

	private void EnumControlValueChanged()
	{
		if (m_EnumControl.Visible && !m_ControlSelectedWithEnter)
		{
			SubmitCellChange();
			CloseEnumControl();
		}
	}

	public void Reset()
	{
		ClearSelection();
		SetCellBackPointers();
		ReAddCellControls();
	}

	public void ReAddCellControls()
	{
		base.Controls.Clear();
		AddCellControls();
		UpdateCellControls();
		RefreshHasHeirachyFlag();
	}

	private int GetColumnWidth(int index)
	{
		if (m_Columns.Count == 0)
		{
			return 0;
		}
		return m_Columns[index].Width;
	}

	private int GetRowHeight(Row row)
	{
		if (row.Height != -1)
		{
			return row.Height;
		}
		return DefaultRowHeight;
	}

	private int GetTextOffset(int col_index)
	{
		if (col_index != 0 || !m_HasHeirachy)
		{
			return ScaleDPI(m_TextOffset);
		}
		return ScaleDPI(14);
	}

	private SolidBrush GetCellBrush(bool row_alt, ColRow colrow)
	{
		if (!colrow.Valid || !(colrow.GetCellValue() is ComboBox))
		{
			if (CellSelected(colrow) && (m_HighlightSelected || !colrow.Equals(m_LastSelectedCell)))
			{
				return m_SelectedCellBrush;
			}
			if (m_HighlightSelectedRow && RowSelected(colrow))
			{
				return m_SelectedRowBrush;
			}
			if (m_HighlightRow && m_HighlightedRow != null && colrow.Row == m_HighlightedRow)
			{
				return m_HighlightRowBrush;
			}
		}
		if (m_AlternateRowColours)
		{
			if (!row_alt)
			{
				return m_DarkRowBrush;
			}
			return m_LightRowBrush;
		}
		return m_WindowBrush;
	}

	public void OnColumnResizing()
	{
		UpdateCellControls();
	}

	public void UpdateCellControls()
	{
		if (!m_HasCellControls)
		{
			return;
		}
		int num = m_ScrollPosition.Y;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		rowIterator.IncludeExpanded = true;
		bool flag = false;
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			bool flag2 = Utils.RowExpandedRecursive(current);
			int rowHeight = GetRowHeight(current);
			bool flag3 = RowBeingDragged(current);
			int num2 = m_ScrollPosition.X;
			int num3 = 0;
			foreach (Column column in m_Columns)
			{
				if (column.Visible)
				{
					int columnWidth = GetColumnWidth(num3);
					if (num3 < current.Cells.Count)
					{
						Cell cell = current.Cells[num3];
						Control control = ((cell != null) ? (cell.Value as Control) : null);
						if (control != null)
						{
							Panel panel = (Panel)control.Parent;
							if (panel != null)
							{
								if (flag3 || !flag2)
								{
									panel.Hide();
								}
								else
								{
									panel.Show();
									int indentOffset = GetIndentOffset(new ColRow(num3, current));
									int num4 = (m_HasHeirachy ? GetTextOffset(num3) : 0);
									int num5 = indentOffset + num4;
									SolidBrush cellBrush = GetCellBrush(flag, new ColRow(num3, current));
									panel.Location = new Point(num2 + num5, num + 1);
									Size size = new Size(columnWidth - num5, rowHeight - 2);
									panel.Size = size;
									panel.BackColor = cellBrush.Color;
									control.BackColor = cellBrush.Color;
								}
							}
						}
					}
					num2 += columnWidth;
				}
				num3++;
			}
			if (flag2)
			{
				flag = !flag;
				num += rowHeight;
			}
		}
	}

	public void OnRowsCollectionReset()
	{
		ClearSelection();
	}

	public void ClearSelection()
	{
		m_LastSelectedCell = ColRow.Invalid;
		if (m_SelectedCells.Count != 0)
		{
			m_SelectedCells.Clear();
			m_SelRows.Clear();
			OnSelectionChanged(ColRow.Invalid);
		}
	}

	private void SetCellBackPointers()
	{
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			foreach (Cell cell in rowIterator.Current.Cells)
			{
				cell.m_CellsPanel = this;
			}
		}
	}

	private void AddCellControls()
	{
		m_HasCellControls = false;
		bool focused = Focused;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int num = Math.Min(current.Cells.Count, m_Columns.Count);
			for (int i = 0; i < num; i++)
			{
				if (!m_Columns[i].Visible)
				{
					continue;
				}
				Control control = ((current.Cells[i] != null) ? (current.Cells[i].Value as Control) : null);
				if (control != null)
				{
					m_HasCellControls = true;
					HookControl(control);
					Panel panel = new Panel();
					panel.Size = new Size(m_Columns[i].Width, current.Height);
					if (current.Expanded)
					{
						panel.Show();
					}
					else
					{
						panel.Hide();
					}
					control.Dock = DockStyle.Fill;
					panel.Controls.Add(control);
					base.Controls.Add(panel);
				}
			}
		}
		if (focused)
		{
			Focus();
		}
	}

	private void HookControl(Control control)
	{
		control.MouseDown += ControlMouseDown;
		control.MouseMove += ControlMouseMove;
		control.MouseUp += ControlMouseUp;
		control.MouseDoubleClick += ControlMouseDoubleClick;
		control.KeyPress += ControlKeyPress;
		if (control is IHDataGridCellControl iHDataGridCellControl)
		{
			iHDataGridCellControl.CellChanged += CellControlCahnged;
		}
		foreach (Control control2 in control.Controls)
		{
			HookControl(control2);
		}
	}

	private void CellControlCahnged(Control sender)
	{
		HDataGrid.OnCellChanged();
	}

	private void ControlKeyPress(object sender, KeyPressEventArgs e)
	{
		OnKeyPress(e);
	}

	private MouseEventArgs ConvertToClient(object sender, MouseEventArgs e)
	{
		Point p = ((Control)sender).PointToScreen(e.Location);
		p = PointToClient(p);
		return new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta);
	}

	private void ControlMouseDown(object sender, MouseEventArgs e)
	{
		OnMouseDown(ConvertToClient(sender, e));
	}

	private void ControlMouseMove(object sender, MouseEventArgs e)
	{
		OnMouseMove(ConvertToClient(sender, e));
	}

	private void ControlMouseUp(object sender, MouseEventArgs e)
	{
		OnMouseUp(ConvertToClient(sender, e));
	}

	private void ControlMouseDoubleClick(object sender, MouseEventArgs e)
	{
		OnMouseDoubleClick(ConvertToClient(sender, e));
	}

	private void StringControlKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			m_StringControl.UpdateValue();
			bool renamingCell = m_RenamingCell;
			m_StringControl.Enabled = false;
			SubmitCellChange();
			CloseStringControl();
			if (m_LastSelectedCell.Valid && m_SelectNextCellAfterEdit && !renamingCell)
			{
				Row nextRow = Utils.GetNextRow(m_LastSelectedCell.Row);
				if (nextRow != null)
				{
					SelectCell(new ColRow(m_LastSelectedCell.Col, nextRow));
				}
			}
		}
		else if (e.KeyCode == Keys.Escape)
		{
			m_StringControl.Value = null;
			CloseStringControl();
		}
	}

	private bool RowBeingDragged(Row row)
	{
		if (m_RowsBeingDragged.Contains(row))
		{
			return true;
		}
		foreach (Row item in m_RowsBeingDragged)
		{
			if (item.IsParentOf(row))
			{
				return true;
			}
		}
		return false;
	}

	private int GetIndentOffset(ColRow colrow)
	{
		if (colrow.Col != 0)
		{
			return 0;
		}
		return (colrow.Row.Depth - 1) * ScaleDPI(m_IndentOffset);
	}

	private List<ColRow> GetCellsToPaint(Rectangle rect, int max_cell_count)
	{
		rect.Offset(-m_ScrollPosition.X, -m_ScrollPosition.Y);
		List<ColRow> list = new List<ColRow>();
		List<int> list2 = new List<int>();
		int num = 0;
		for (int i = 0; i < m_Columns.Count; i++)
		{
			if (m_Columns[i].Visible)
			{
				int columnWidth = GetColumnWidth(i);
				if (num < rect.Right && num + columnWidth > rect.Left)
				{
					list2.Add(i);
				}
				num += columnWidth;
				if (num > rect.Right)
				{
					break;
				}
			}
		}
		int num2 = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int rowHeight = GetRowHeight(current);
			if (num2 < rect.Bottom && num2 + rowHeight > rect.Top)
			{
				foreach (int item in list2)
				{
					list.Add(new ColRow(item, current));
				}
				if (list.Count > max_cell_count)
				{
					break;
				}
			}
			num2 += rowHeight;
			if (num2 > rect.Bottom)
			{
				break;
			}
		}
		return list;
	}

	private int GetTotalWidth()
	{
		int num = 0;
		for (int i = 0; i < m_Columns.Count; i++)
		{
			if (m_Columns[i].Visible)
			{
				num += GetColumnWidth(i);
			}
		}
		return num;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		List<ColRow> cellsToPaint = GetCellsToPaint(e.ClipRectangle, 8);
		if (cellsToPaint.Count <= 8 && e.ClipRectangle.Width < base.ClientSize.Width / 2 && e.ClipRectangle.Height < base.ClientSize.Height / 2)
		{
			foreach (ColRow item in cellsToPaint)
			{
				e.Graphics.SetClip(GetCellRect(item));
				PaintCell(e.Graphics, item);
				e.Graphics.ResetClip();
			}
			int num = GetHeight();
			if (!m_PadEmptyRows && e.ClipRectangle.Bottom > num)
			{
				e.Graphics.FillRectangle(SystemBrushes.AppWorkspace, e.ClipRectangle.X, num, e.ClipRectangle.Width, e.ClipRectangle.Bottom - num);
			}
			int totalWidth = GetTotalWidth();
			if (e.ClipRectangle.Right > totalWidth)
			{
				Rectangle rect = new Rectangle(totalWidth, e.ClipRectangle.Y, e.ClipRectangle.Right, e.ClipRectangle.Height);
				e.Graphics.FillRectangle(SystemBrushes.AppWorkspace, rect);
			}
		}
		else
		{
			if (m_BackColourBrush.Color != BackColor)
			{
				m_BackColourBrush = new SolidBrush(BackColor);
			}
			e.Graphics.FillRectangle(m_BackColourBrush, base.ClientRectangle);
			int num2 = m_ScrollPosition.X;
			int num3 = 0;
			bool flag = false;
			foreach (Column column in m_Columns)
			{
				if (column.Visible)
				{
					int columnWidth = GetColumnWidth(num3);
					e.Graphics.SetClip(new Rectangle(num2, 0, columnWidth, base.Height));
					RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
					bool flag2 = false;
					int num4 = m_ScrollPosition.Y;
					while (rowIterator.MoveNext())
					{
						Row current2 = rowIterator.Current;
						int rowHeight = GetRowHeight(current2);
						Rectangle rect2 = new Rectangle(num2, num4, columnWidth, rowHeight);
						if (rect2.IntersectsWith(base.ClientRectangle))
						{
							SolidBrush cellBrush = GetCellBrush(flag2, new ColRow(num3, current2));
							e.Graphics.FillRectangle(cellBrush, rect2);
							PaintCell(e.Graphics, new ColRow(num3, current2), rect2, cellBrush);
						}
						flag2 = !flag2;
						num4 += rowHeight;
						if (num4 > base.ClientSize.Height)
						{
							break;
						}
						flag = flag2;
					}
					num2 += columnWidth;
				}
				num3++;
			}
			e.Graphics.ResetClip();
			int totalHeight = GetTotalHeight();
			if (m_PadEmptyRows)
			{
				int num5 = m_ScrollPosition.Y + totalHeight;
				bool flag3 = flag;
				while (num5 < base.ClientSize.Height)
				{
					Rectangle rect3 = new Rectangle(0, num5, base.ClientSize.Width, DefaultRowHeight);
					SolidBrush cellBrush2 = GetCellBrush(flag3, ColRow.Invalid);
					e.Graphics.FillRectangle(cellBrush2, rect3);
					num5 += DefaultRowHeight;
					flag3 = !flag3;
				}
			}
			if (m_DrawColumnLines)
			{
				PaintColumnLines(e.Graphics, m_PadEmptyRows ? base.ClientSize.Height : totalHeight);
			}
			if (m_DrawRowLines)
			{
				PaintRowLines(e.Graphics);
			}
		}
		base.OnPaint(e);
	}

	private bool GetRowAlt(Row row)
	{
		bool flag = false;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext() && rowIterator.Current != row)
		{
			flag = !flag;
		}
		return flag;
	}

	private void PaintCell(Graphics graphics, ColRow col_row)
	{
		Rectangle cellRect = GetCellRect(col_row);
		bool rowAlt = GetRowAlt(col_row.Row);
		SolidBrush cellBrush = GetCellBrush(rowAlt, new ColRow(col_row.Col, col_row.Row));
		graphics.FillRectangle(cellBrush, cellRect);
		PaintCell(graphics, col_row, cellRect, cellBrush);
		if (col_row.Col < VisibleColumnCount)
		{
			graphics.DrawLine(m_GridLinePen, cellRect.Right - 1, cellRect.Top, cellRect.Right - 1, cellRect.Bottom);
		}
	}

	private Point GetCellTextPos(ColRow colrow, Rectangle rect)
	{
		int indentOffset = GetIndentOffset(colrow);
		int textOffset = GetTextOffset(colrow.Col);
		return new Point(rect.Left + indentOffset + textOffset, rect.Top + (rect.Height - Font.Height) / 2);
	}

	private bool IsBoolImage(int col)
	{
		Column column = m_Columns[col];
		if (column.BoolTrueImage == null)
		{
			return column.BoolFalseImage != null;
		}
		return true;
	}

	private void PaintCell(Graphics graphics, ColRow col_row, Rectangle rect, Brush cell_brush)
	{
		rect.Offset(-m_Columns[col_row.Col].HScrollOffset, 0);
		int col = col_row.Col;
		Row row = col_row.Row;
		bool flag = RowBeingDragged(row);
		if (col < row.Cells.Count)
		{
			Cell cell = row.Cells[col];
			if (cell != null && !(cell.Value is Control) && !flag)
			{
				PaintCell(graphics, col_row, rect);
			}
			DrawHeirachyLinesForcell(graphics, col_row, rect);
			if (row.ChildRows.Count != 0 && !flag && col == 0)
			{
				DrawCollapseExpandRect(graphics, rect, row, cell_brush);
			}
		}
	}

	private void PaintCell(Graphics graphics, ColRow colrow, Rectangle rect)
	{
		Cell cell = colrow.GetCell();
		if (cell.Value is bool && IsBoolImage(colrow.Col))
		{
			PaintBoolCell(graphics, colrow, rect);
		}
		else if (cell.Value is Color)
		{
			PaintColourCell(graphics, colrow, rect);
		}
		else
		{
			PaintStringCell(graphics, colrow, rect);
		}
	}

	private void PaintStringCell(Graphics graphics, ColRow colrow, Rectangle rect)
	{
		Cell cell = colrow.GetCell();
		Point cellTextPos = GetCellTextPos(colrow, rect);
		if (m_CellTextBrush == null || m_CellTextBrush.Color != ForeColor)
		{
			m_CellTextBrush = new SolidBrush(ForeColor);
		}
		string cellValueAsString = GetCellValueAsString(cell.Value);
		if (cellValueAsString != null)
		{
			graphics.DrawString(cellValueAsString, Font, m_CellTextBrush, cellTextPos);
		}
	}

	private void PaintBoolCell(Graphics graphics, ColRow colrow, Rectangle rect)
	{
		bool num = (bool)colrow.GetCell().Value;
		Column column = m_Columns[colrow.Col];
		if (num)
		{
			if (column.BoolTrueImage != null)
			{
				int num2 = rect.X + (rect.Width - column.BoolTrueImage.Width) / 2;
				int num3 = rect.Y + (rect.Height - column.BoolTrueImage.Height) / 2;
				graphics.DrawImage(column.BoolTrueImage, num2, num3);
			}
		}
		else if (column.BoolFalseImage != null)
		{
			int num4 = rect.X + (rect.Width - column.BoolTrueImage.Width) / 2;
			int num5 = rect.Y + (rect.Height - column.BoolTrueImage.Height) / 2;
			graphics.DrawImageUnscaled(column.BoolFalseImage, num4, num5);
		}
	}

	private void PaintColourCell(Graphics graphics, ColRow colrow, Rectangle rect)
	{
		Rectangle rect2 = rect;
		rect2.Inflate(-4, -4);
		Color colour = (Color)colrow.GetCell().Value;
		Brush brush = GetBrush(colour);
		graphics.FillRectangle(brush, rect2);
	}

	private Brush GetBrush(Color colour)
	{
		if (!m_Brushes.ContainsKey(colour))
		{
			m_Brushes[colour] = new SolidBrush(colour);
		}
		return m_Brushes[colour];
	}

	private void DrawHeirachyLinesForcell(Graphics graphics, ColRow col_row, Rectangle rect)
	{
		int col = col_row.Col;
		Row row = col_row.Row;
		bool flag = RowBeingDragged(row);
		if (col != 0 || row.Parent.Parent == null || flag)
		{
			return;
		}
		int indentOffset = GetIndentOffset(col_row);
		Point cellTextPos = GetCellTextPos(col_row, rect);
		int num = rect.X + ScaleDPI(m_ExpandBoxOffset) + ScaleDPI(m_ExpandBoxSize) / 2 + indentOffset;
		int num2 = rect.Y + rect.Height / 2;
		graphics.DrawLine(m_GridLinePen, num, rect.Y, num, num2);
		graphics.DrawLine(m_GridLinePen, num, num2, cellTextPos.X - 2, num2);
		if (row.Parent.LastDisplayedChild != row)
		{
			graphics.DrawLine(m_GridLinePen, num, num2, num, rect.Bottom);
		}
		Row row2 = row.Parent;
		num -= ScaleDPI(m_IndentOffset);
		while (row2 != null)
		{
			if (row2.Parent != null && row2.Parent.LastDisplayedChild != row2)
			{
				graphics.DrawLine(m_GridLinePen, num, rect.Y, num, num2);
				graphics.DrawLine(m_GridLinePen, num, rect.Y, num, rect.Bottom);
			}
			num -= ScaleDPI(m_IndentOffset);
			row2 = row2.Parent;
		}
	}

	private Rectangle GetCollapseExpandRect(Row row, Rectangle cell_rect)
	{
		int num = ScaleDPI(m_ExpandBoxSize);
		int num2 = (row.Depth - 1) * ScaleDPI(m_IndentOffset);
		return new Rectangle(cell_rect.X + num2 + ScaleDPI(m_ExpandBoxOffset), cell_rect.Y + (cell_rect.Height - num) / 2, num, num);
	}

	private void DrawCollapseExpandRect(Graphics graphics, Rectangle cell_rect, Row row, Brush fill_brush)
	{
		Rectangle collapseExpandRect = GetCollapseExpandRect(row, cell_rect);
		graphics.FillRectangle(fill_brush, collapseExpandRect);
		graphics.DrawRectangle(m_GridLinePen, collapseExpandRect);
		int num = collapseExpandRect.Y + collapseExpandRect.Height / 2;
		graphics.DrawLine(m_GridLinePen, collapseExpandRect.X + collapseExpandRect.Width / 3, num, collapseExpandRect.X + 2 * collapseExpandRect.Width / 3, num);
		if (!row.Expanded)
		{
			int num2 = collapseExpandRect.X + collapseExpandRect.Height / 2;
			graphics.DrawLine(m_GridLinePen, num2, collapseExpandRect.Y + collapseExpandRect.Height / 3, num2, collapseExpandRect.Y + 2 * collapseExpandRect.Height / 3);
		}
	}

	private void PaintColumnLines(Graphics graphics, int height)
	{
		int num = m_ScrollPosition.X - 1;
		int num2 = (m_DrawLastColumnLine ? m_Columns.Count : (m_Columns.Count - 1));
		for (int i = 0; i < num2; i++)
		{
			Column column = m_Columns[i];
			if (column.Visible)
			{
				num += column.Width;
				graphics.DrawLine(m_GridLinePen, num, 0, num, height);
			}
		}
	}

	private void PaintRowLines(Graphics graphics)
	{
		int x = GetTotalWidth() - m_ScrollPosition.X;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		int i = m_ScrollPosition.Y;
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int rowHeight = GetRowHeight(current);
			if (i >= 0 && i < base.ClientSize.Height)
			{
				graphics.DrawLine(m_GridLinePen, 0, i, x, i);
			}
			i += rowHeight;
			if (i > base.ClientSize.Height)
			{
				break;
			}
		}
		if (m_PadEmptyRows)
		{
			for (; i < base.ClientSize.Height; i += DefaultRowHeight)
			{
				if (i >= 0 && i < base.ClientSize.Height)
				{
					graphics.DrawLine(m_GridLinePen, 0, i, x, i);
				}
			}
		}
		graphics.DrawLine(m_GridLinePen, 0, i, x, i);
	}

	private int GetCellX(ColRow colrow)
	{
		int num = m_ScrollPosition.X;
		for (int i = 0; i < colrow.Col; i++)
		{
			if (m_Columns[i].Visible)
			{
				num += GetColumnWidth(i);
			}
		}
		return num;
	}

	public Rectangle GetCellRect(ColRow col_row)
	{
		int cellX = GetCellX(col_row);
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		bool flag = false;
		int num = m_ScrollPosition.Y;
		int columnWidth = GetColumnWidth(col_row.Col);
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int rowHeight = GetRowHeight(current);
			if (current == col_row.Row)
			{
				return new Rectangle(cellX, num, columnWidth, rowHeight);
			}
			flag = !flag;
			num += rowHeight;
		}
		return Rectangle.Empty;
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			Point pt = new Point(e.X - m_ScrollPosition.X, e.Y - m_ScrollPosition.Y);
			CellDetails cellDetails = GetCellDetails(pt);
			if (cellDetails != null)
			{
				Cell cell = cellDetails.m_ColRow.GetCell();
				if (cell != null && !cell.ReadOnly)
				{
					RenameCell(cellDetails.m_ColRow);
					HDataGrid.OnCellDoubleClicked(cellDetails.m_ColRow);
				}
			}
		}
		base.OnMouseDoubleClick(e);
	}

	public void RenameCell(ColRow col_row)
	{
		Cell cell = col_row.GetCell();
		string text = null;
		if (cell.Value is string)
		{
			text = (string)cell.Value;
		}
		else if (cell.Value is ITextObject)
		{
			text = ((ITextObject)cell.Value).Text;
		}
		if (text != null)
		{
			ShowStringControl(text, typeof(string), text, select_all: true);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		bool eat_event = false;
		if (m_MouseEnabled)
		{
			switch (e.Button)
			{
			case MouseButtons.Left:
				HandleLeftMouseButtonDown(e, ref eat_event);
				break;
			case MouseButtons.Right:
				HandleRightMouseButtonDown(e);
				break;
			}
		}
		if (!eat_event)
		{
			base.OnMouseDown(e);
		}
	}

	private void HandleLeftMouseButtonDown(MouseEventArgs e, ref bool eat_event)
	{
		if (m_ColumnBeingResized != null)
		{
			base.Capture = true;
			m_DraggingColumn = true;
			m_LastMouseX = e.X;
			return;
		}
		Point pt = new Point(e.X - m_ScrollPosition.X, e.Y - m_ScrollPosition.Y);
		CellDetails cellDetails = GetCellDetails(pt);
		if (cellDetails == null || !cellDetails.m_ColRow.Valid)
		{
			return;
		}
		bool flag = false;
		if (cellDetails.m_ColRow.Col == 0)
		{
			Rectangle collapseExpandRect = GetCollapseExpandRect(cellDetails.m_ColRow.Row, cellDetails.m_Rect);
			collapseExpandRect.Offset(-m_Columns[0].HScrollOffset, 0);
			if (collapseExpandRect.Contains(pt))
			{
				ExpandRow(cellDetails.m_ColRow.Row, !cellDetails.m_ColRow.Row.Expanded, m_ShiftKeyHeld);
				ScrollPanel.UpdateScrollBars();
				AddSelectedCell(cellDetails.m_ColRow, SelectMode.Single);
				HDataGrid.Refresh();
				flag = true;
			}
		}
		if (flag)
		{
			return;
		}
		if (EditControlVisible)
		{
			SubmitCellChange();
			CloseEditControls();
		}
		if (CellSelected(cellDetails.m_ColRow) && m_MoveCellsEnabled)
		{
			m_DraggingCells = true;
		}
		else
		{
			m_FirstSelectedCell = cellDetails.m_ColRow;
			m_StartedSelectDrag = true;
			AddSelectedCell(cellDetails.m_ColRow, GetSelectMode());
			if (m_SlideDrag && !m_ReadOnly && m_SelectedCells.Contains(cellDetails.m_ColRow))
			{
				m_StartedSlidingRow = true;
				m_SlideDragOldIndex = cellDetails.m_ColRow.Row.Index;
				m_SelectRowOnNextMouseMove = m_SlideDrag && m_SelectedCells.Contains(cellDetails.m_ColRow);
				m_LastMouseClickPt = e.Location;
			}
			m_SelectedCellsBeforeRangeDrag = new Set<ColRow>(m_SelectedCells);
		}
		if (IsSelectedCellBoolImage)
		{
			ToggleSelectedBoolValue();
		}
		else
		{
			eat_event = ShowControlForCell();
		}
	}

	public void SelectAndEditCell(ColRow colrow)
	{
		AddSelectedCell(colrow, SelectMode.Single);
		if (!ShowControlForCell())
		{
			ShowStringControl(colrow, "");
		}
	}

	private void HandleRightMouseButtonDown(MouseEventArgs e)
	{
		if (m_DraggingCells)
		{
			StopDraggingCells();
			return;
		}
		Point pt = new Point(e.X - m_ScrollPosition.X, e.Y - m_ScrollPosition.Y);
		ColRow colRow = GetColRow(pt);
		if (colRow.Valid)
		{
			if (!m_SelectedCells.Contains(colRow))
			{
				m_FirstSelectedCell = colRow;
				AddSelectedCell(colRow, SelectMode.Single);
			}
			m_ExpandAllMenuItem.Enabled = colRow.Row.ChildRows.Count != 0;
			m_CollapseAllMenuItem.Enabled = colRow.Row.ChildRows.Count != 0;
		}
	}

	private void OnSelectionChanged(ColRow colrow)
	{
		HDataGrid.OnSelectionChanged(colrow);
	}

	public ColRow GetColRow(Point pt)
	{
		CellDetails cellDetails = GetCellDetails(pt);
		if (cellDetails == null)
		{
			return ColRow.Invalid;
		}
		return cellDetails.m_ColRow;
	}

	private CellDetails GetCellDetails(Point pt)
	{
		if (pt.X < 0 || pt.Y < 0)
		{
			return new CellDetails();
		}
		CellDetails cellDetails = new CellDetails();
		int num = 0;
		int num2 = 0;
		int col = LastVisibleColumnIndex;
		for (int i = 0; i < m_Columns.Count; i++)
		{
			if (m_Columns[i].Visible)
			{
				num += num2;
				num2 = m_Columns[i].Width;
				if (pt.X < num + num2)
				{
					col = i;
					break;
				}
			}
		}
		int num3 = 0;
		int num4 = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		Row row = null;
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			num3 += num4;
			num4 = GetRowHeight(current);
			if (pt.Y < num3 + num4)
			{
				row = current;
				break;
			}
		}
		if (row == null)
		{
			return null;
		}
		cellDetails.m_ColRow = new ColRow(col, row);
		cellDetails.m_Rect = new Rectangle(num, num3, num2, num4);
		return cellDetails;
	}

	public void AddSelectedCell(ColRow colrow)
	{
		AddSelectedCell(colrow, SelectMode.Multi);
	}

	public void AddSelectedCell(ColRow colrow, SelectMode select_mode)
	{
		if (m_SelectByRow)
		{
			StartUpdate();
			for (int i = 0; i < colrow.Row.Cells.Count; i++)
			{
				ColRow colrow2 = new ColRow(i, colrow.Row);
				AddSelectedCellInternal(colrow2, (i != 0) ? SelectMode.Multi : select_mode);
			}
			m_SelRows.Add(colrow.Row);
			EndUpdate();
		}
		else
		{
			AddSelectedCellInternal(colrow, select_mode);
		}
	}

	private void AddSelectedCellInternal(ColRow colrow, SelectMode select_mode)
	{
		Set<ColRow> s = new Set<ColRow>(m_SelectedCells);
		if (select_mode != SelectMode.Multi)
		{
			m_SelectedCells.Clear();
			m_SelRows.Clear();
		}
		if (select_mode == SelectMode.Range && m_FirstSelectCell.Valid)
		{
			int num = Math.Min(m_FirstSelectCell.Col, colrow.Col);
			int num2 = Math.Max(m_FirstSelectCell.Col, colrow.Col);
			int num3 = Math.Min(m_FirstSelectCell.Row.Index, colrow.Row.Index);
			int num4 = Math.Max(m_FirstSelectCell.Row.Index, colrow.Row.Index);
			int num5 = num;
			int num6 = num3;
			while (num5 <= num2 && num6 <= num4)
			{
				m_SelectedCells.Add(new ColRow(num5, m_RootRow.ChildRows[num6]));
				num5++;
				if (num5 == m_Columns.Count)
				{
					num5 = 0;
					num6++;
				}
			}
		}
		else
		{
			m_FirstSelectCell = colrow;
			if (!m_SelectedCells.Contains(colrow))
			{
				m_SelectedCells.Add(colrow);
			}
		}
		if (m_LastSelectedCell != colrow)
		{
			m_LastSelectedCell = colrow;
			OnSelectionChanged(colrow);
		}
		UpdateCellControls();
		Set<ColRow> s2 = new Set<ColRow>(m_SelectedCells);
		Set<ColRow> set = Set<ColRow>.InverseUnion(s, s2);
		if (m_PrevLastSelectedCell.Valid)
		{
			set.Add(m_PrevLastSelectedCell);
		}
		RefreshCells(new List<ColRow>(set));
	}

	private void UpdateSelectedRange(ColRow start, ColRow end)
	{
		Set<ColRow> s = new Set<ColRow>(m_SelectedCells);
		m_SelectedCells.Clear();
		m_SelRows.Clear();
		m_SelectedCells.AddRange(m_SelectedCellsBeforeRangeDrag);
		int num = Math.Min(start.Col, end.Col);
		int num2 = Math.Max(start.Col, end.Col);
		Row row = start.Row;
		Row row2 = end.Row;
		if (row2 == Utils.GetMinRow(row, row2))
		{
			Row row3 = row;
			row = row2;
			row2 = row3;
		}
		Set<Row> set = new Set<Row>();
		RowIterator rowIterator = new RowIterator(row);
		while (rowIterator.MoveNext())
		{
			if (!set.Contains(rowIterator.Current))
			{
				set.Add(rowIterator.Current);
			}
			for (int i = num; i <= num2; i++)
			{
				m_SelectedCells.Add(new ColRow(i, rowIterator.Current));
			}
			if (rowIterator.Current == row2)
			{
				break;
			}
		}
		if (m_SelectByRow)
		{
			foreach (Row item in set)
			{
				for (int j = 0; j < item.Cells.Count; j++)
				{
					m_SelectedCells.Add(new ColRow(j, item));
				}
			}
			m_SelRows.AddRange(set);
		}
		UpdateCellControls();
		OnSelectionChanged(end);
		Set<ColRow> s2 = new Set<ColRow>(m_SelectedCells);
		Set<ColRow> set2 = Set<ColRow>.InverseUnion(s, s2);
		if (m_PrevLastSelectedCell.Valid)
		{
			set2.Add(m_PrevLastSelectedCell);
		}
		set2.Add(end);
		RefreshCells(new List<ColRow>(set2));
	}

	public void RefreshCell(ColRow colrow)
	{
		List<ColRow> list = new List<ColRow>();
		list.Add(colrow);
		RefreshCells(list);
	}

	private void RefreshCells(List<ColRow> cells)
	{
		if (cells.Count > 8)
		{
			RefreshInternal();
			return;
		}
		foreach (ColRow cell in cells)
		{
			Rectangle rc = GetCellRect(cell);
			if (m_HighlightSelectedRow && m_HighlightRow)
			{
				rc = new Rectangle(0, rc.Y, base.Width, rc.Height);
			}
			Invalidate(rc);
		}
		UpdateInternal();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		Point point = new Point(e.X - m_ScrollPosition.X, e.Y - m_ScrollPosition.Y);
		if (m_MouseEnabled)
		{
			if (m_DraggingColumn)
			{
				int num = e.X - m_LastMouseX;
				m_LastMouseX = e.X;
				HDataGrid.ResizeColumn(m_ColumnBeingResized, m_ColumnBeingResized.Width + num);
			}
			else if (m_StartedSelectDrag)
			{
				if (m_SelectRowOnNextMouseMove && e.Location != m_LastMouseClickPt)
				{
					HDataGrid.SelectRow(m_LastSelectedCell.Row);
					m_SelectRowOnNextMouseMove = false;
				}
				CellDetails cellDetails = GetCellDetails(point);
				if (cellDetails != null && cellDetails.m_ColRow.Valid && m_LastSelectedCell.Valid && !m_LastSelectedCell.Equals(cellDetails.m_ColRow))
				{
					if (m_StartedSlidingRow)
					{
						HandleSlideDrag(cellDetails);
					}
					else
					{
						m_PrevLastSelectedCell = m_LastSelectedCell;
						m_LastSelectedCell = cellDetails.m_ColRow;
						UpdateSelectedRange(m_FirstSelectedCell, m_LastSelectedCell);
					}
				}
			}
			else if (m_DraggingCells)
			{
				base.Capture = true;
				HandleDraggingCellsMouseMove(e);
			}
			else
			{
				int num2 = 0;
				bool flag = false;
				int num3 = 0;
				int num4 = VisibleColumnCount;
				if (HasFillColumn)
				{
					num4--;
				}
				for (int i = 0; i < num4; i++)
				{
					Column column = m_Columns[i];
					if (column.Visible)
					{
						num2 += GetColumnWidth(num3);
						if (Math.Abs(e.X - num2) < ScaleDPI(4))
						{
							m_ColumnBeingResized = column;
							Cursor = Cursors.VSplit;
							flag = true;
							break;
						}
						num3++;
					}
				}
				if (!flag)
				{
					Cursor = Cursors.Default;
					m_ColumnBeingResized = null;
				}
			}
		}
		if (m_HighlightRow)
		{
			Row row = GetCellDetails(point)?.m_ColRow.Row;
			if (m_HighlightedRow != row)
			{
				Row highlightedRow = m_HighlightedRow;
				m_HighlightedRow = row;
				for (int j = 0; j < m_Columns.Count; j++)
				{
					if (m_Columns[j].Visible)
					{
						if (highlightedRow != null)
						{
							RefreshCell(new ColRow(j, highlightedRow));
						}
						RefreshCell(new ColRow(j, m_HighlightedRow));
					}
				}
			}
		}
		m_LastMousePos = point;
		base.OnMouseMove(e);
	}

	private void HandleSlideDrag(CellDetails cell_details)
	{
		_ = m_LastSelectedCell.Col;
		Row row = m_LastSelectedCell.Row;
		Row row2 = cell_details.m_ColRow.Row;
		if (IsLastEmptyEditRow(row2) || row2.Parent != row.Parent || IsLastEmptyEditRow(row))
		{
			return;
		}
		List<Row> list = new List<Row>();
		list.Add(row);
		bool cancel = false;
		HDataGrid.OnRowMoveRequest(list, row2, EDropMode.Before, ref cancel);
		if (!cancel)
		{
			int index = row.Index;
			int index2 = row2.Index;
			if (index2 != index)
			{
				Row row3 = row.Parent;
				m_IgnoreRowEvents = true;
				row3.ChildRows.Remove(row);
				row3.ChildRows.Insert(index2, row);
				HDataGrid.OnRowMoved(row, index, index2);
				HDataGrid.SelectRow(row);
				HDataGrid.RefreshRowTitlePanel();
				m_IgnoreRowEvents = false;
				RefreshInternal();
			}
		}
	}

	private bool IsLastEmptyEditRow(Row row)
	{
		if (HDataGrid.AddEmptyRow)
		{
			return row == m_RootRow.LastDisplayedChild;
		}
		return false;
	}

	private void HandleDraggingCellsMouseMove(MouseEventArgs e)
	{
		CloseEditControls();
		Point point = new Point(e.X - m_ScrollPosition.X, e.Y - m_ScrollPosition.Y);
		if (!(point != m_LastMousePos))
		{
			return;
		}
		if (m_DragNodesForm == null)
		{
			m_RowsBeingDragged = Utils.GetSelectedRows(m_SelectedCells);
			RowCollection rowCollection = new RowCollection();
			rowCollection = Utils.Clone(m_RowsBeingDragged);
			m_DragNodesForm = new DragNodesForm(rowCollection, m_Columns, m_DPIScale);
			UpdateCellControls();
			Invalidate();
			Update();
		}
		m_DragNodesForm.Location = PointToScreen(e.Location);
		m_DragNodesForm.Show();
		CellDetails cellDetails = (m_DropNodeTargetCellDetails = GetCellDetails(point));
		if (cellDetails != null && cellDetails.m_ColRow.Row != null && !RowBeingDragged(cellDetails.m_ColRow.Row))
		{
			Rectangle rectangle = cellDetails.m_Rect;
			if (cellDetails.m_ColRow.Col != 0)
			{
				rectangle = GetCellRect(new ColRow(0, cellDetails.m_ColRow.Row));
			}
			m_DropMode = ((point.Y >= rectangle.Top + rectangle.Height / 2) ? EDropMode.After : EDropMode.Before);
			int num = cellDetails.m_ColRow.Row.Depth;
			if (m_DropMode == EDropMode.After && cellDetails.m_ColRow.Row.ChildRows.Count != 0 && !RowBeingDragged(cellDetails.m_ColRow.Row))
			{
				num++;
			}
			int num2 = num * ScaleDPI(m_IndentOffset);
			int num3 = rectangle.Left + num2;
			int num4 = ((m_DropMode == EDropMode.Before) ? rectangle.Top : rectangle.Bottom);
			int num5 = rectangle.Width;
			int num6 = ((cellDetails.m_ColRow.Row.Height != -1) ? Math.Min(DefaultRowHeight, cellDetails.m_ColRow.Row.Height) : DefaultRowHeight);
			if (e.X > num3 + 2 * ScaleDPI(m_IndentOffset) && (m_DropMode != 0 || !cellDetails.m_ColRow.Row.IsFirstDisplayedChild()) && cellDetails.m_ColRow.Row.ChildRows.Count == 0 && !RowBeingDragged(cellDetails.m_ColRow.Row))
			{
				m_DropMode = EDropMode.Child;
				num3 += ScaleDPI(m_IndentOffset);
			}
			ShowDragNodeTargetMarker(num3, num4, num5, num6);
		}
		else
		{
			HideDragNodeTargetMarker();
		}
	}

	private void ShowDragNodeTargetMarker(int x, int y, int width, int height)
	{
		Rectangle r = new Rectangle(x, y, width, height);
		r = RectangleToScreen(r);
		r = HDataGrid.RectangleToClient(r);
		HDataGrid.ShowDragNodeTargetMarker(r);
	}

	private void HideDragNodeTargetMarker()
	{
		HDataGrid.HideDragNodeTargetMarker();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			if (m_DraggingColumn)
			{
				m_DraggingColumn = false;
				base.Capture = false;
			}
			if (m_StartedSlidingRow)
			{
				int slideDragOldIndex = m_SlideDragOldIndex;
				int index = m_LastSelectedCell.Row.Index;
				if (slideDragOldIndex != index)
				{
					HDataGrid.OnSlideDragFinished(m_LastSelectedCell.Row, slideDragOldIndex, index);
				}
				m_StartedSlidingRow = false;
			}
			m_StartedSelectDrag = false;
			if (m_RowsBeingDragged.Count != 0)
			{
				DropDraggedNodes();
			}
			StopDraggingCells();
		}
		base.OnMouseUp(e);
	}

	private void StopDraggingCells()
	{
		if (m_DragNodesForm != null)
		{
			m_DragNodesForm.Close();
			m_DragNodesForm = null;
			HideDragNodeTargetMarker();
		}
		m_DraggingCells = false;
		base.Capture = false;
		if (m_RowsBeingDragged.Count != 0)
		{
			m_RowsBeingDragged.Clear();
			UpdateCellControls();
			HDataGrid.Refresh();
		}
	}

	private void DropDraggedNodes()
	{
		if (m_DropNodeTargetCellDetails == null || !m_DropNodeTargetCellDetails.m_ColRow.Valid || RowBeingDragged(m_DropNodeTargetCellDetails.m_ColRow.Row))
		{
			return;
		}
		Row row = m_DropNodeTargetCellDetails.m_ColRow.Row;
		bool cancel = false;
		HDataGrid.OnRowMoveRequest(m_RowsBeingDragged, row, m_DropMode, ref cancel);
		if (cancel)
		{
			return;
		}
		foreach (Row item in m_RowsBeingDragged)
		{
			item.Parent.ChildRows.Remove(item);
		}
		if (m_DropMode == EDropMode.Child)
		{
			if (m_RowsBeingDragged.Count != 0)
			{
				row.InsertChildrenAtHead(m_RowsBeingDragged);
			}
			return;
		}
		foreach (Row item2 in m_RowsBeingDragged)
		{
			Row row2 = row.Parent;
			if (m_DropMode == EDropMode.Before)
			{
				row2.InsertBefore(row, item2);
			}
			else
			{
				row2.InsertAfter(row, item2);
			}
		}
	}

	private void SelectCell(ColRow colrow)
	{
		m_StartedSelectDrag = false;
		m_FirstSelectedCell = ColRow.Invalid;
		m_LastSelectedCell = ColRow.Invalid;
		CloseEditControls();
		if (colrow.Valid)
		{
			AddSelectedCell(colrow, SelectMode.Single);
		}
		if (colrow.Row != null)
		{
			HDataGrid.ScrollIntoView(colrow.Row);
		}
	}

	private void CloseEditControls()
	{
		CloseBoolControl();
		CloseEnumControl();
		CloseStringControl();
		CloseCustomControl();
	}

	public int GetHeight()
	{
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		int num = 0;
		while (rowIterator.MoveNext())
		{
			Row current = rowIterator.Current;
			int rowHeight = GetRowHeight(current);
			num += rowHeight;
		}
		return num;
	}

	private bool CellSelected(ColRow colrow)
	{
		if (!colrow.Valid)
		{
			return false;
		}
		return m_SelectedCells.Contains(colrow);
	}

	private bool RowSelected(ColRow colrow)
	{
		if (!colrow.Valid)
		{
			return false;
		}
		foreach (ColRow selectedCell in m_SelectedCells)
		{
			if (selectedCell.Row == colrow.Row)
			{
				return true;
			}
		}
		return false;
	}

	private void RemoveSelectedRow(ColRow colrow)
	{
		for (int i = 0; i < colrow.Row.Cells.Count; i++)
		{
			if (CellSelected(new ColRow(i, colrow.Row)))
			{
				return;
			}
		}
		m_SelRows.Remove(colrow.Row);
	}

	public void UnselectCell(Cell cell)
	{
		foreach (ColRow selectedCell in m_SelectedCells)
		{
			if (selectedCell.GetCell() == cell)
			{
				m_SelectedCells.Remove(selectedCell);
				RemoveSelectedRow(selectedCell);
				OnSelectionChanged(ColRow.Invalid);
				break;
			}
		}
	}

	public void UnselectCell(object value)
	{
		foreach (ColRow selectedCell in m_SelectedCells)
		{
			Cell cell = selectedCell.GetCell();
			if (cell != null && cell.Value == value)
			{
				m_SelectedCells.Remove(selectedCell);
				RemoveSelectedRow(selectedCell);
				OnSelectionChanged(ColRow.Invalid);
				break;
			}
		}
	}

	public void AddSelectedCell(Cell cell)
	{
		ColRow colRow = Utils.GetColRow(cell, m_RootRow.ChildRows);
		AddSelectedCell(colRow, SelectMode.Multi);
	}

	public void AddSelectedCell(object value)
	{
		ColRow colRow = Utils.GetColRow(value, m_RootRow.ChildRows);
		if (colRow.Valid)
		{
			AddSelectedCell(colRow, SelectMode.Multi);
		}
	}

	public void SelectCell(Cell cell)
	{
		ClearSelection();
		AddSelectedCell(cell);
	}

	private int GetTotalHeight()
	{
		int num = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			num += GetRowHeight(rowIterator.Current);
		}
		return num;
	}

	public Size GetScrollSize()
	{
		return new Size(GetTotalWidth(), GetTotalHeight());
	}

	public void OnScrollPositionChanged()
	{
		UpdateCellControls();
		Invalidate();
		UpdateInternal();
	}

	private void RenameSelected()
	{
		if (m_SelectedCells.Count == 0 || !m_LastSelectedCell.Valid || m_ReadOnly || ColumnReadOnly(m_LastSelectedCell.Col) || m_LastSelectedCell.GetCell().ReadOnly)
		{
			return;
		}
		object cellValue = m_LastSelectedCell.GetCellValue();
		string text = null;
		if (cellValue is string)
		{
			text = (string)cellValue;
		}
		else
		{
			if (!(cellValue is ITextObject))
			{
				return;
			}
			text = ((ITextObject)cellValue).Text;
		}
		m_RenamingCell = true;
		if (ShowStringControl(text, typeof(string), text))
		{
			m_StringControl.SelectAll();
		}
	}

	public int GetRowIndex(Row row, bool include_expanded)
	{
		int num = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		rowIterator.IncludeExpanded = include_expanded;
		while (rowIterator.MoveNext())
		{
			if (rowIterator.Current == row)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	public Row GetRow(int index)
	{
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		rowIterator.MoveNext();
		for (int i = 0; i < index; i++)
		{
			rowIterator.MoveNext();
		}
		return rowIterator.Current;
	}

	private static string GetCellValueAsString(object value)
	{
		if (value == null)
		{
			return "";
		}
		if (value is ITextObject)
		{
			return ((ITextObject)value).Text;
		}
		return value.ToString();
	}

	public int GetMaxCellWidth(int col)
	{
		Graphics graphics = CreateGraphics();
		int num = 0;
		RowIterator rowIterator = new RowIterator(m_RootRow.FirstDisplayedChild);
		while (rowIterator.MoveNext())
		{
			if (col >= rowIterator.Current.Cells.Count)
			{
				continue;
			}
			string cellValueAsString = GetCellValueAsString(rowIterator.Current.Cells[col].Value);
			if (cellValueAsString != null)
			{
				ColRow colRow = new ColRow(col, rowIterator.Current);
				int indentOffset = GetIndentOffset(colRow);
				int textOffset = GetTextOffset(colRow.Col);
				int num2 = indentOffset + textOffset + (int)graphics.MeasureString(cellValueAsString, Font).Width;
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		graphics.Dispose();
		return num;
	}

	public void RegisterEditControl(Type cell_type, Type control_type)
	{
		m_CustomEditControls[cell_type] = control_type;
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (m_ClearSelectionOnMouseLeave && !base.ClientRectangle.Contains(PointToClient(Cursor.Position)))
		{
			ClearSelection();
			RefreshInternal();
		}
		base.OnMouseLeave(e);
	}

	public void RefreshHasHeirachyFlag()
	{
		m_HasHeirachy = false;
		foreach (Row childRow in m_RootRow.ChildRows)
		{
			if (childRow.ChildRows.Count != 0)
			{
				m_HasHeirachy = true;
				break;
			}
		}
	}

	private void RefreshInternal()
	{
		if (m_StartEndUpdateCount == 0)
		{
			Refresh();
		}
	}

	private void UpdateInternal()
	{
		if (m_StartEndUpdateCount == 0)
		{
			Update();
		}
	}

	public void StartUpdate()
	{
		m_StartEndUpdateCount++;
	}

	public void EndUpdate()
	{
		m_StartEndUpdateCount--;
		if (m_StartEndUpdateCount == 0)
		{
			Update();
			Refresh();
		}
	}

	public void SelectRow(Row row)
	{
		StartUpdate();
		for (int i = 0; i < m_Columns.Count; i++)
		{
			if (m_Columns[i].Visible)
			{
				AddSelectedCell(new ColRow(i, row));
			}
		}
		EndUpdate();
	}

	public void SelectRowRange(Row first, Row last)
	{
		StartUpdate();
		m_SelRows.Clear();
		if (first == last)
		{
			m_SelRows.Add(first);
		}
		else
		{
			bool flag = false;
			RowIterator rowIterator = new RowIterator(m_RootRow);
			while (rowIterator.MoveNext())
			{
				if (flag)
				{
					m_SelRows.Add(rowIterator.Current);
				}
				if (rowIterator.Current == first || rowIterator.Current == last)
				{
					flag = !flag;
					if (!flag)
					{
						break;
					}
					m_SelRows.Add(rowIterator.Current);
				}
			}
		}
		foreach (Row selRow in m_SelRows)
		{
			for (int i = 0; i < m_Columns.Count; i++)
			{
				if (m_Columns[i].Visible)
				{
					AddSelectedCell(new ColRow(i, selRow));
				}
			}
		}
		EndUpdate();
	}
}
