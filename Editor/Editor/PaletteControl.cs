using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Editor;

public class PaletteControl : UserControl
{
	public delegate void ColourChangedHandler(Color colour, ColourSelectMode mode);

	private Color m_Colour = Color.White;

	private Panel m_SelColourControl;

	private ColourSelectMode m_SelectMode;

	private List<Panel> m_Presets = new List<Panel>();

	private const int m_PresetsWidth = 30;

	private const int m_PresetsHeight = 2;

	private bool m_IgnoreTextBoxChangedEvents;

	private static Color[] m_DefaultPaletteColours;

	private static Color[] m_PaletteColours;

	private IContainer components;

	private Panel panel1;

	private SVColourPanel m_SVColourPanel;

	private Splitter splitter1;

	private HVColourPanel m_HVColourPanel;

	private Label label1;

	private TextBox m_RTextBox;

	private TextBox m_GTextBox;

	private Label label2;

	private TextBox m_BTextBox;

	private Label label3;

	private ColourSlider m_RSlider;

	private ColourSlider m_GSlider;

	private ColourSlider m_BSlider;

	private Panel m_PresetsPanel;

	private Panel m_ColourPanel;

	public Color Colour
	{
		get
		{
			return m_Colour;
		}
		set
		{
			ColourChangedEvent(value);
		}
	}

	public event ColourChangedHandler ColourChanged;

	static PaletteControl()
	{
		m_DefaultPaletteColours = new Color[15]
		{
			Color.Black,
			Color.White,
			Color.Brown,
			Color.Red,
			Color.Chocolate,
			Color.DarkOrange,
			Color.Yellow,
			Color.GreenYellow,
			Color.Lime,
			Color.Cyan,
			Color.LightSkyBlue,
			Color.Blue,
			Color.BlueViolet,
			Color.Purple,
			Color.Magenta
		};
		m_PaletteColours = new Color[60];
		for (int i = 0; i < m_DefaultPaletteColours.Length; i++)
		{
			m_PaletteColours[i] = m_DefaultPaletteColours[i];
		}
	}

	public PaletteControl()
		: this(Color.Black)
	{
	}

	public PaletteControl(Color colour)
	{
		InitializeComponent();
		ColourChangedEvent(colour);
		SetupPresetsPanel();
	}

	private void SetupPresetsPanel()
	{
		int num = 0;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 30; j++)
			{
				Panel panel = new Panel();
				panel.Location = new Point(4 + j * 14, i * 14);
				panel.Size = new Size(12, 12);
				panel.BorderStyle = BorderStyle.FixedSingle;
				panel.BackColor = m_PaletteColours[num++];
				panel.MouseDown += PresetBoxClick;
				m_Presets.Add(panel);
				m_PresetsPanel.Controls.Add(panel);
			}
		}
	}

	public void ColourChangedEvent(Color colour)
	{
		ColourChangedEvent(colour, m_SelectMode);
	}

	public void ColourChangedEvent(Color colour, ColourSelectMode sel_mode)
	{
		if (m_Colour != colour)
		{
			m_Colour = colour;
			m_RSlider.Colour = colour;
			m_GSlider.Colour = colour;
			m_BSlider.Colour = colour;
			m_HVColourPanel.Colour = colour;
			m_SVColourPanel.Colour = m_Colour;
			m_IgnoreTextBoxChangedEvents = true;
			m_RTextBox.Text = m_Colour.R.ToString();
			m_GTextBox.Text = m_Colour.G.ToString();
			m_BTextBox.Text = m_Colour.B.ToString();
			m_IgnoreTextBoxChangedEvents = false;
			if (m_SelColourControl != null && m_SelColourControl.BackColor != m_Colour)
			{
				m_SelColourControl.BackColor = m_Colour;
				int num = m_Presets.IndexOf(m_SelColourControl);
				m_PaletteColours[num] = m_Colour;
			}
			m_SelectMode = sel_mode;
			if (this.ColourChanged != null)
			{
				this.ColourChanged(colour, m_SelectMode);
			}
			m_ColourPanel.BackColor = colour;
		}
	}

	private void PresetBoxClick(object sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
		{
			m_SelectMode = ((e.Button != MouseButtons.Left) ? ColourSelectMode.Back : ColourSelectMode.Fore);
			if (m_SelColourControl != null)
			{
				m_SelColourControl.BorderStyle = BorderStyle.FixedSingle;
			}
			Panel panel = (Panel)sender;
			panel.BorderStyle = BorderStyle.Fixed3D;
			m_SelColourControl = panel;
			ColourChangedEvent(panel.BackColor);
		}
	}

	public void UnselectColourBox()
	{
		if (m_SelColourControl != null)
		{
			m_SelColourControl.BorderStyle = BorderStyle.FixedSingle;
		}
		m_SelColourControl = null;
	}

	public static ICollection<Color> GetPaletteColours()
	{
		return m_PaletteColours;
	}

	public static void SetPaletteColours(ICollection<Color> colours)
	{
		int num = 0;
		foreach (Color colour in colours)
		{
			m_PaletteColours[num++] = colour;
		}
	}

	private void UpdateColourFromTextBoxes(object sender, EventArgs e)
	{
		if (m_IgnoreTextBoxChangedEvents)
		{
			return;
		}
		try
		{
			int red = Convert.ToInt32(m_RTextBox.Text);
			int green = Convert.ToInt32(m_GTextBox.Text);
			int blue = Convert.ToInt32(m_BTextBox.Text);
			Colour = Color.FromArgb(red, green, blue);
		}
		catch (Exception)
		{
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
		this.panel1 = new System.Windows.Forms.Panel();
		this.m_SVColourPanel = new Editor.SVColourPanel();
		this.splitter1 = new System.Windows.Forms.Splitter();
		this.m_HVColourPanel = new Editor.HVColourPanel();
		this.m_PresetsPanel = new System.Windows.Forms.Panel();
		this.label1 = new System.Windows.Forms.Label();
		this.m_RTextBox = new System.Windows.Forms.TextBox();
		this.m_GTextBox = new System.Windows.Forms.TextBox();
		this.label2 = new System.Windows.Forms.Label();
		this.m_BTextBox = new System.Windows.Forms.TextBox();
		this.label3 = new System.Windows.Forms.Label();
		this.m_ColourPanel = new System.Windows.Forms.Panel();
		this.m_BSlider = new Editor.ColourSlider();
		this.m_GSlider = new Editor.ColourSlider();
		this.m_RSlider = new Editor.ColourSlider();
		this.panel1.SuspendLayout();
		base.SuspendLayout();
		this.panel1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.panel1.Controls.Add(this.m_SVColourPanel);
		this.panel1.Controls.Add(this.splitter1);
		this.panel1.Controls.Add(this.m_HVColourPanel);
		this.panel1.Location = new System.Drawing.Point(2, 2);
		this.panel1.Margin = new System.Windows.Forms.Padding(2);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(159, 253);
		this.panel1.TabIndex = 0;
		this.m_SVColourPanel.Colour = System.Drawing.Color.Empty;
		this.m_SVColourPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_SVColourPanel.Location = new System.Drawing.Point(0, 156);
		this.m_SVColourPanel.Margin = new System.Windows.Forms.Padding(2);
		this.m_SVColourPanel.Name = "m_SVColourPanel";
		this.m_SVColourPanel.Size = new System.Drawing.Size(159, 97);
		this.m_SVColourPanel.TabIndex = 2;
		this.m_SVColourPanel.Text = "svColourPanel1";
		this.m_SVColourPanel.ColourChanged += new Editor.SVColourPanel.ColourChangedHandler(ColourChangedEvent);
		this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
		this.splitter1.Location = new System.Drawing.Point(0, 154);
		this.splitter1.Margin = new System.Windows.Forms.Padding(2);
		this.splitter1.Name = "splitter1";
		this.splitter1.Size = new System.Drawing.Size(159, 2);
		this.splitter1.TabIndex = 1;
		this.splitter1.TabStop = false;
		this.m_HVColourPanel.Colour = System.Drawing.Color.Empty;
		this.m_HVColourPanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_HVColourPanel.Location = new System.Drawing.Point(0, 0);
		this.m_HVColourPanel.Margin = new System.Windows.Forms.Padding(2);
		this.m_HVColourPanel.Name = "m_HVColourPanel";
		this.m_HVColourPanel.Size = new System.Drawing.Size(159, 154);
		this.m_HVColourPanel.TabIndex = 0;
		this.m_HVColourPanel.ColourChanged += new Editor.HVColourPanel.ColourChangedHandler(ColourChangedEvent);
		this.m_PresetsPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_PresetsPanel.Location = new System.Drawing.Point(2, 259);
		this.m_PresetsPanel.Margin = new System.Windows.Forms.Padding(2);
		this.m_PresetsPanel.Name = "m_PresetsPanel";
		this.m_PresetsPanel.Size = new System.Drawing.Size(159, 28);
		this.m_PresetsPanel.TabIndex = 42;
		this.label1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 294);
		this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(15, 13);
		this.label1.TabIndex = 17;
		this.label1.Text = "R";
		this.m_RTextBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_RTextBox.Location = new System.Drawing.Point(30, 292);
		this.m_RTextBox.Margin = new System.Windows.Forms.Padding(2);
		this.m_RTextBox.Name = "m_RTextBox";
		this.m_RTextBox.Size = new System.Drawing.Size(36, 20);
		this.m_RTextBox.TabIndex = 18;
		this.m_RTextBox.TextChanged += new System.EventHandler(UpdateColourFromTextBoxes);
		this.m_GTextBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GTextBox.Location = new System.Drawing.Point(30, 315);
		this.m_GTextBox.Margin = new System.Windows.Forms.Padding(2);
		this.m_GTextBox.Name = "m_GTextBox";
		this.m_GTextBox.Size = new System.Drawing.Size(36, 20);
		this.m_GTextBox.TabIndex = 20;
		this.m_GTextBox.TextChanged += new System.EventHandler(UpdateColourFromTextBoxes);
		this.label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(11, 317);
		this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(15, 13);
		this.label2.TabIndex = 19;
		this.label2.Text = "G";
		this.m_BTextBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_BTextBox.Location = new System.Drawing.Point(30, 337);
		this.m_BTextBox.Margin = new System.Windows.Forms.Padding(2);
		this.m_BTextBox.Name = "m_BTextBox";
		this.m_BTextBox.Size = new System.Drawing.Size(36, 20);
		this.m_BTextBox.TabIndex = 22;
		this.m_BTextBox.TextChanged += new System.EventHandler(UpdateColourFromTextBoxes);
		this.label3.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(12, 340);
		this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(14, 13);
		this.label3.TabIndex = 21;
		this.label3.Text = "B";
		this.m_ColourPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ColourPanel.Location = new System.Drawing.Point(0, 362);
		this.m_ColourPanel.Name = "m_ColourPanel";
		this.m_ColourPanel.Size = new System.Drawing.Size(164, 52);
		this.m_ColourPanel.TabIndex = 43;
		this.m_BSlider.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_BSlider.Colour = System.Drawing.Color.Empty;
		this.m_BSlider.Location = new System.Drawing.Point(70, 337);
		this.m_BSlider.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
		this.m_BSlider.Mode = Editor.ColourSlider.EMode.B;
		this.m_BSlider.Name = "m_BSlider";
		this.m_BSlider.Size = new System.Drawing.Size(92, 19);
		this.m_BSlider.TabIndex = 25;
		this.m_BSlider.ColourChanged += new Editor.ColourSlider.ColourChangedHandler(ColourChangedEvent);
		this.m_GSlider.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_GSlider.Colour = System.Drawing.Color.Empty;
		this.m_GSlider.Location = new System.Drawing.Point(69, 315);
		this.m_GSlider.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
		this.m_GSlider.Mode = Editor.ColourSlider.EMode.G;
		this.m_GSlider.Name = "m_GSlider";
		this.m_GSlider.Size = new System.Drawing.Size(92, 19);
		this.m_GSlider.TabIndex = 24;
		this.m_GSlider.ColourChanged += new Editor.ColourSlider.ColourChangedHandler(ColourChangedEvent);
		this.m_RSlider.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_RSlider.Colour = System.Drawing.Color.Empty;
		this.m_RSlider.Location = new System.Drawing.Point(69, 291);
		this.m_RSlider.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
		this.m_RSlider.Mode = Editor.ColourSlider.EMode.R;
		this.m_RSlider.Name = "m_RSlider";
		this.m_RSlider.Size = new System.Drawing.Size(92, 19);
		this.m_RSlider.TabIndex = 23;
		this.m_RSlider.ColourChanged += new Editor.ColourSlider.ColourChangedHandler(ColourChangedEvent);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_ColourPanel);
		base.Controls.Add(this.m_PresetsPanel);
		base.Controls.Add(this.m_BSlider);
		base.Controls.Add(this.m_GSlider);
		base.Controls.Add(this.m_RSlider);
		base.Controls.Add(this.m_BTextBox);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.m_GTextBox);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.m_RTextBox);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.panel1);
		base.Margin = new System.Windows.Forms.Padding(2);
		base.Name = "PaletteControl";
		base.Size = new System.Drawing.Size(164, 414);
		this.panel1.ResumeLayout(false);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
