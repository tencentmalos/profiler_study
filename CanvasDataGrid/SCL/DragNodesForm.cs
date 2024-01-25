using System.Collections.Generic;
using System.Windows.Forms;

namespace SCL;

internal class DragNodesForm : Form
{
	private Panel m_Panel = new Panel();

	private CellsPanel m_CellsPanel;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ExStyle |= 128;
			return obj;
		}
	}

	public DragNodesForm(RowCollection rows, List<Column> columns, float dpi_scale)
	{
		SetStyle(ControlStyles.Selectable, value: false);
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.Enabled = false;
		base.Opacity = 0.4;
		Row row = new Row();
		foreach (Row row2 in rows)
		{
			row.ChildRows.Add(row2);
		}
		m_Panel.BorderStyle = BorderStyle.FixedSingle;
		m_Panel.Dock = DockStyle.Fill;
		base.Controls.Add(m_Panel);
		m_CellsPanel = new CellsPanel(row, columns, base.Controls, dpi_scale);
		m_CellsPanel.AutoScroll = false;
		m_CellsPanel.Dock = DockStyle.Fill;
		m_Panel.Controls.Add(m_CellsPanel);
		base.Size = m_CellsPanel.GetScrollSize();
	}
}
