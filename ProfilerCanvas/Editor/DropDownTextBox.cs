using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Editor;

public class DropDownTextBox : UserControl
{
	private DropDownListBox m_DropDownListBox = new DropDownListBox();

	public string m_SelectedValue = "";

	private string m_OldSelectedValue;

	private bool m_IgnoreTextChanged;

	private int m_DropDownBoxWidth = -1;

	private bool m_ShowingDropDown;

	private IContainer components;

	private TextBox m_TextBox;

	private Button m_DropDownButton;

	public List<string> Items => m_DropDownListBox.Items;

	public string Selection
	{
		get
		{
			return m_SelectedValue;
		}
		set
		{
			if (m_SelectedValue != value)
			{
				m_SelectedValue = value;
				m_IgnoreTextChanged = true;
				m_TextBox.Text = m_SelectedValue;
				m_IgnoreTextChanged = false;
				m_DropDownListBox.Hide();
				if (this.SelectionChanged != null)
				{
					this.SelectionChanged(m_SelectedValue);
				}
			}
		}
	}

	public bool ShowDropDownButton
	{
		get
		{
			return m_DropDownButton.Visible;
		}
		set
		{
			m_DropDownButton.Visible = value;
		}
	}

	public int DropDownBoxWidth
	{
		get
		{
			return m_DropDownBoxWidth;
		}
		set
		{
			m_DropDownBoxWidth = value;
		}
	}

	public event DropDownTextBoxDroppedDownHandler DroppedDown;

	public event DropDownTextBoxSelectionChangedHandler SelectionChanged;

	public DropDownTextBox()
	{
		InitializeComponent();
		m_DropDownListBox.ItemSelected += DropDownItemSelected;
		m_TextBox.LostFocus += TextBoxLostFocus;
	}

	private void TextBoxLostFocus(object sender, EventArgs e)
	{
		if (m_ShowingDropDown && !m_DropDownListBox.Focused)
		{
			CancelDropdown();
		}
	}

	private void DropDownItemSelected(string selected_value)
	{
		Selection = selected_value;
		HideDropDown();
	}

	private void DropDownButtonClicked(Button button)
	{
		if (m_DropDownListBox.Visible)
		{
			m_DropDownListBox.Hide();
		}
		else
		{
			ShowDropDown();
		}
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		m_TextBox.ForeColor = ForeColor;
		base.OnForeColorChanged(e);
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		m_TextBox.BackColor = BackColor;
		base.OnBackColorChanged(e);
	}

	public void ShowDropDown()
	{
		if (!m_DropDownListBox.Visible)
		{
			if (this.DroppedDown != null)
			{
				this.DroppedDown();
			}
			m_OldSelectedValue = m_TextBox.Text;
			m_DropDownListBox.Location = PointToScreen(new Point(0, base.Height));
			m_DropDownListBox.Width = ((m_DropDownBoxWidth != -1) ? m_DropDownBoxWidth : base.Width);
			m_DropDownListBox.Show(this);
			m_TextBox.Clear();
			m_TextBox.Focus();
			m_ShowingDropDown = true;
		}
	}

	private void CancelDropdown()
	{
		if (m_DropDownListBox.Visible)
		{
			m_SelectedValue = m_OldSelectedValue;
			m_TextBox.Text = m_OldSelectedValue;
			HideDropDown();
		}
	}

	private void HideDropDown()
	{
		m_DropDownListBox.Hide();
		m_ShowingDropDown = false;
	}

	private void TextBoxTextChanged(object sender, EventArgs e)
	{
		if (!m_IgnoreTextChanged)
		{
			m_DropDownListBox.SetFilter(m_TextBox.Text);
			if (!m_DropDownListBox.Visible)
			{
				ShowDropDown();
			}
		}
	}

	private void TextBoxKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.KeyCode)
		{
		case Keys.Return:
			if (m_DropDownListBox.Visible)
			{
				string highlightedItem = m_DropDownListBox.HighlightedItem;
				if (highlightedItem != null)
				{
					Selection = highlightedItem;
				}
				m_DropDownListBox.Hide();
			}
			break;
		case Keys.Down:
			if (!m_DropDownListBox.Visible)
			{
				ShowDropDown();
			}
			else if (m_DropDownListBox.HighlightIndex < m_DropDownListBox.FilteredItemCount - 1)
			{
				DropDownListBox dropDownListBox = m_DropDownListBox;
				int highlightIndex = dropDownListBox.HighlightIndex + 1;
				dropDownListBox.HighlightIndex = highlightIndex;
			}
			e.Handled = true;
			break;
		case Keys.Up:
			if (!m_DropDownListBox.Visible)
			{
				ShowDropDown();
			}
			else if (m_DropDownListBox.HighlightIndex > 0)
			{
				DropDownListBox dropDownListBox2 = m_DropDownListBox;
				int highlightIndex = dropDownListBox2.HighlightIndex - 1;
				dropDownListBox2.HighlightIndex = highlightIndex;
			}
			e.Handled = true;
			break;
		case Keys.Next:
			if (!m_DropDownListBox.Visible)
			{
				ShowDropDown();
			}
			else
			{
				m_DropDownListBox.HighlightIndex = Math.Min(m_DropDownListBox.HighlightIndex + m_DropDownListBox.VisibleItemCount - 1, m_DropDownListBox.FilteredItemCount - 1);
			}
			e.Handled = true;
			break;
		case Keys.Prior:
			if (!m_DropDownListBox.Visible)
			{
				ShowDropDown();
			}
			else
			{
				m_DropDownListBox.HighlightIndex = Math.Max(m_DropDownListBox.HighlightIndex - m_DropDownListBox.VisibleItemCount + 1, 0);
			}
			e.Handled = true;
			break;
		case Keys.Escape:
			CancelDropdown();
			break;
		}
	}

	private void TextBoxPreviewKeyDoown(object sender, PreviewKeyDownEventArgs e)
	{
		base.OnPreviewKeyDown(e);
	}

	private void TextBoxMouseDown(object sender, MouseEventArgs e)
	{
		if ((m_DropDownBoxWidth == -1 || e.X < m_DropDownBoxWidth) && !m_DropDownListBox.Visible)
		{
			ShowDropDown();
		}
		else
		{
			HideDropDown();
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
		this.m_TextBox = new System.Windows.Forms.TextBox();
		this.m_DropDownButton = new Editor.Button();
		base.SuspendLayout();
		this.m_TextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_TextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.m_TextBox.Location = new System.Drawing.Point(4, 4);
		this.m_TextBox.Name = "m_TextBox";
		this.m_TextBox.Size = new System.Drawing.Size(746, 13);
		this.m_TextBox.TabIndex = 0;
		this.m_TextBox.TextChanged += new System.EventHandler(TextBoxTextChanged);
		this.m_TextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(TextBoxKeyDown);
		this.m_TextBox.MouseDown += new System.Windows.Forms.MouseEventHandler(TextBoxMouseDown);
		this.m_TextBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(TextBoxPreviewKeyDoown);
		this.m_DropDownButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.m_DropDownButton.BackColor = System.Drawing.SystemColors.Window;
		this.m_DropDownButton.ButtonText = "";
		this.m_DropDownButton.HoverColour = System.Drawing.SystemColors.ControlLight;
		this.m_DropDownButton.Image = Editor.Resource1.drop_down_arrow;
		this.m_DropDownButton.Location = new System.Drawing.Point(750, 0);
		this.m_DropDownButton.Name = "m_DropDownButton";
		this.m_DropDownButton.Size = new System.Drawing.Size(22, 20);
		this.m_DropDownButton.TabIndex = 1;
		this.m_DropDownButton.Clicked += new Editor.ButtonClickedHandler(DropDownButtonClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.SystemColors.Window;
		base.Controls.Add(this.m_DropDownButton);
		base.Controls.Add(this.m_TextBox);
		base.Name = "DropDownTextBox";
		base.Size = new System.Drawing.Size(772, 20);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
