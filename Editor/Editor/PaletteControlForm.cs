using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Editor;

public class PaletteControlForm : Form
{
	private IContainer components;

	private PaletteControl m_PaletteControl;

	public Color Colour
	{
		get
		{
			return m_PaletteControl.Colour;
		}
		set
		{
			m_PaletteControl.Colour = value;
		}
	}

	public event ColourChangedHandler ColourChanged;

	public PaletteControlForm()
	{
		InitializeComponent();
	}

	protected override void OnDeactivate(EventArgs e)
	{
		base.OnDeactivate(e);
		Close();
	}

	private void ColourChangedEvent(Color colour, ColourSelectMode mode)
	{
		if (this.ColourChanged != null)
		{
			this.ColourChanged(colour);
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
		this.m_PaletteControl = new Editor.PaletteControl();
		base.SuspendLayout();
		this.m_PaletteControl.Colour = System.Drawing.Color.Black;
		this.m_PaletteControl.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_PaletteControl.Location = new System.Drawing.Point(0, 0);
		this.m_PaletteControl.Margin = new System.Windows.Forms.Padding(2);
		this.m_PaletteControl.Name = "m_PaletteControl";
		this.m_PaletteControl.Size = new System.Drawing.Size(218, 431);
		this.m_PaletteControl.TabIndex = 0;
		this.m_PaletteControl.ColourChanged += new Editor.PaletteControl.ColourChangedHandler(ColourChangedEvent);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(218, 431);
		base.Controls.Add(this.m_PaletteControl);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
		base.Name = "PaletteControlForm";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		this.Text = "Colour Palette";
		base.TopMost = true;
		base.ResumeLayout(false);
	}
}
