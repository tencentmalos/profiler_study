using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FramePro.Properties;

namespace FramePro;

internal class NotShowingContextSwitchesMessageBox : UserControl
{
	private IContainer components;

	private LinkLabel m_LinkLabel;

	private PictureBox pictureBox1;

	public event ContextSwitchMessageBoxWantsToCloseHandler WantsToClose;

	public event ContextSwitchMessageBoxWantsToLoadFile WantsToLoadFile;

	public NotShowingContextSwitchesMessageBox()
	{
		InitializeComponent();
		string text = "Load context switch file";
		int start = m_LinkLabel.Text.IndexOf(text);
		m_LinkLabel.Links.Add(start, text.Length, "load");
		string text2 = "Find out more";
		int start2 = m_LinkLabel.Text.IndexOf(text2);
		m_LinkLabel.Links.Add(start2, text2.Length, "https://www.puredevsoftware.com/framepro/context_switches.htm");
		string text3 = "Dismiss";
		int start3 = m_LinkLabel.Text.IndexOf(text3);
		m_LinkLabel.Links.Add(start3, text3.Length, "dismiss");
	}

	private void LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		string text = e.Link.LinkData.ToString();
		if (text == "load")
		{
			if (this.WantsToLoadFile != null)
			{
				this.WantsToLoadFile();
			}
		}
		else if (text == "dismiss")
		{
			if (this.WantsToClose != null)
			{
				this.WantsToClose();
			}
		}
		else
		{
			Process.Start(text);
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
		this.m_LinkLabel = new System.Windows.Forms.LinkLabel();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.m_LinkLabel.ActiveLinkColor = System.Drawing.Color.SkyBlue;
		this.m_LinkLabel.AutoSize = true;
		this.m_LinkLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_LinkLabel.ForeColor = System.Drawing.Color.White;
		this.m_LinkLabel.LinkColor = System.Drawing.Color.FromArgb(192, 192, 255);
		this.m_LinkLabel.Location = new System.Drawing.Point(28, 3);
		this.m_LinkLabel.Name = "m_LinkLabel";
		this.m_LinkLabel.Size = new System.Drawing.Size(509, 13);
		this.m_LinkLabel.TabIndex = 0;
		this.m_LinkLabel.TabStop = true;
		this.m_LinkLabel.Text = "FramePro is not tracking context switches.    Load context switch file    Find out more          Dismiss\r\n";
		this.m_LinkLabel.VisitedLinkColor = System.Drawing.Color.Yellow;
		this.m_LinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(LinkClicked);
		this.pictureBox1.Image = FramePro.Properties.Resources.info;
		this.pictureBox1.Location = new System.Drawing.Point(0, 0);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(22, 22);
		this.pictureBox1.TabIndex = 4;
		this.pictureBox1.TabStop = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(35, 41, 86);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.m_LinkLabel);
		base.Name = "NotShowingContextSwitchesMessageBox";
		base.Size = new System.Drawing.Size(381, 20);
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
