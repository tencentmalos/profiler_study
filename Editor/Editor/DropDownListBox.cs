using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Editor;

public class DropDownListBox : Form
{
	private List<string> m_Items = new List<string>();

	private List<string> m_FilteredItems = new List<string>();

	private Brush m_TextBrush;

	private const int m_MaxHeight = 300;

	private string m_Filter;

	private int m_HighlightIndex;

	private Brush m_HighlightBrush = new SolidBrush(Color.LightBlue);

	private IContainer components;

	private VScrollBar m_VScrollBar;

	public List<string> Items => m_Items;

	public int HighlightIndex
	{
		get
		{
			return m_HighlightIndex;
		}
		set
		{
			m_HighlightIndex = value;
			ScrollIntoView(value);
			Refresh();
		}
	}

	public int VisibleItemCount => Math.Min(base.Height / Font.Height, m_FilteredItems.Count);

	public int FilteredItemCount => m_FilteredItems.Count;

	public string HighlightedItem
	{
		get
		{
			if (m_FilteredItems.Count == 0 || HighlightIndex == -1)
			{
				return null;
			}
			return m_FilteredItems[HighlightIndex];
		}
	}

	public event DropDownListBoxItemSelectedHandler ItemSelected;

	public DropDownListBox()
	{
		SetStyle(ControlStyles.Selectable, value: false);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		InitializeComponent();
		m_TextBrush = new SolidBrush(ForeColor);
	}

	protected override void OnShown(EventArgs e)
	{
		UpdateFilteredItems();
		base.OnShown(e);
	}

	public void SetFilter(string filter)
	{
		if (m_Filter != filter)
		{
			m_Filter = filter;
			UpdateFilteredItems();
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(BackColor);
		if (m_FilteredItems.Count != 0)
		{
			e.Graphics.FillRectangle(m_HighlightBrush, 0, (m_HighlightIndex - m_VScrollBar.Value) * Font.Height, base.Width, Font.Height);
		}
		int num = Math.Min((base.Height + Font.Height - 1) / Font.Height, m_FilteredItems.Count);
		for (int i = 0; i < num; i++)
		{
			int num2 = i + m_VScrollBar.Value;
			if (num2 >= 0 && num2 < m_FilteredItems.Count)
			{
				e.Graphics.DrawString(m_FilteredItems[num2], Font, m_TextBrush, 0f, i * Font.Height);
			}
		}
		base.OnPaint(e);
	}

	private void UpdateVScrollBar()
	{
		m_VScrollBar.Maximum = m_FilteredItems.Count;
		m_VScrollBar.LargeChange = base.Height / Font.Height;
		m_VScrollBar.Visible = m_FilteredItems.Count * Font.Height > base.Height;
	}

	private void VScrollBarScroll(object sender, ScrollEventArgs e)
	{
		Refresh();
	}

	private void UpdateFilteredItems()
	{
		if (string.IsNullOrEmpty(m_Filter) || m_Items.Contains(m_Filter))
		{
			m_FilteredItems = new List<string>(m_Items);
		}
		else
		{
			m_FilteredItems.Clear();
			foreach (string item in m_Items)
			{
				if (item.ToLower().Contains(m_Filter.ToLower()))
				{
					m_FilteredItems.Add(item);
				}
			}
		}
		int num = Math.Min(Font.Height * m_FilteredItems.Count, 300);
		base.Size = new Size(base.Width, num);
		UpdateVScrollBar();
		m_HighlightIndex = m_FilteredItems.IndexOf(m_Filter);
		if (m_HighlightIndex != -1)
		{
			int num2 = (base.Height + Font.Height - 1) / Font.Height;
			m_VScrollBar.Value = Misc.Clamp(m_HighlightIndex - num2 / 2, 0, m_FilteredItems.Count - 1);
		}
		else
		{
			m_VScrollBar.Value = 0;
			m_HighlightIndex = ((m_FilteredItems.Count != 0) ? Misc.Clamp(m_HighlightIndex, 0, m_FilteredItems.Count) : (-1));
		}
		Refresh();
	}

	private int GetItemIndex(int y)
	{
		return y / Font.Height + m_VScrollBar.Value;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		m_HighlightIndex = GetItemIndex(e.Y);
		Refresh();
		base.OnMouseMove(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			int itemIndex = GetItemIndex(e.Y);
			if (itemIndex >= 0 && itemIndex < m_FilteredItems.Count)
			{
				string selected_value = m_FilteredItems[itemIndex];
				if (this.ItemSelected != null)
				{
					this.ItemSelected(selected_value);
				}
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		m_HighlightIndex = -1;
		Refresh();
		base.OnMouseLeave(e);
	}

	private void ScrollIntoView(int index)
	{
		int value = m_VScrollBar.Value;
		int num = value + VisibleItemCount;
		if (index >= num)
		{
			m_VScrollBar.Value = index + 1 - VisibleItemCount;
		}
		else if (index < value)
		{
			m_VScrollBar.Value = index;
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
		this.m_VScrollBar = new System.Windows.Forms.VScrollBar();
		base.SuspendLayout();
		this.m_VScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
		this.m_VScrollBar.Location = new System.Drawing.Point(603, 0);
		this.m_VScrollBar.Name = "m_VScrollBar";
		this.m_VScrollBar.Size = new System.Drawing.Size(17, 190);
		this.m_VScrollBar.TabIndex = 0;
		this.m_VScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(VScrollBarScroll);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.SystemColors.ControlLightLight;
		base.ClientSize = new System.Drawing.Size(620, 190);
		base.Controls.Add(this.m_VScrollBar);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Name = "DropDownListBox";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "DropDownListBox";
		base.ResumeLayout(false);
	}
}
