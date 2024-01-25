using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FramePro;

internal class StartupPage : UserControl
{
	private List<string> m_RecentFilesList;

	private List<LinkLabel> m_RecentFileLinkLabels = new List<LinkLabel>();

	private IContainer components;

	private CheckBox m_ShowStartupPageCheckBox;

	private Panel panel1;

	private Label label1;

	private PictureBox pictureBox1;

	private Panel panel2;

	private Label label2;

	private PictureBox pictureBox2;

	private Panel panel3;

	private Label label3;

	private PictureBox pictureBox3;

	private TextBox textBox1;

	private Panel panel4;

	private LinkLabel m_RecentSessionLabel6;

	private LinkLabel m_RecentSessionLabel5;

	private LinkLabel m_RecentSessionLabel4;

	private LinkLabel m_RecentSessionLabel3;

	private LinkLabel m_RecentSessionLabel2;

	private LinkLabel m_RecentSessionLabel1;

	private Label label4;

	private Panel panel5;

	private Panel panel6;

	private TextBox textBox2;

	private Panel panel7;

	private Panel panel8;

	private Label label5;

	private Panel panel10;

	private Panel panel11;

	private Panel panel9;

	public event LaunchDemoHandler LaunchDemo;

	public event StartupPageOpenFileHandler StartupPageOpenFile;

	public event ShowStartupPageToggledHandler ShowStartupPageToggled;

	public StartupPage(ICollection<string> recent_file_list, bool show_startup_page)
	{
		InitializeComponent();
		Text = "Startup";
		m_ShowStartupPageCheckBox.Checked = show_startup_page;
		m_RecentFilesList = new List<string>(recent_file_list);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel1);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel2);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel3);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel4);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel5);
		m_RecentFileLinkLabels.Add(m_RecentSessionLabel6);
		int num = 0;
		foreach (LinkLabel recentFileLinkLabel in m_RecentFileLinkLabels)
		{
			recentFileLinkLabel.LinkClicked += RecentFileLinkLabelClicked;
			if (num < recent_file_list.Count)
			{
				recentFileLinkLabel.Text = Path.GetFileName(m_RecentFilesList[num]);
			}
			else
			{
				recentFileLinkLabel.Visible = false;
			}
			num++;
		}
	}

	private void RecentFileLinkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		int index = m_RecentFileLinkLabels.IndexOf((LinkLabel)sender);
		string filename = m_RecentFilesList[index];
		if (this.StartupPageOpenFile != null)
		{
			this.StartupPageOpenFile(filename);
		}
	}

	private void SetupVideoClick(object sender, EventArgs e)
	{
		Process.Start("https://www.puredevsoftware.com/framepro/SetupVideo.htm");
	}

	private void PromotionalVideoClick(object sender, EventArgs e)
	{
		Process.Start("https://www.puredevsoftware.com/framepro/PromoVideo.htm");
	}

	private void LaunchDemoClick(object sender, EventArgs e)
	{
		if (this.LaunchDemo != null)
		{
			this.LaunchDemo();
		}
	}

	private void ImageMouseEnter(object sender, EventArgs e)
	{
		Cursor = Cursors.Hand;
	}

	private void ImageMouseLeave(object sender, EventArgs e)
	{
		Cursor = Cursors.Default;
	}

	private void ShowStartupPageCheckChanged(object sender, EventArgs e)
	{
		if (this.ShowStartupPageToggled != null)
		{
			this.ShowStartupPageToggled(m_ShowStartupPageCheckBox.Checked);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.StartupPage));
		this.m_ShowStartupPageCheckBox = new System.Windows.Forms.CheckBox();
		this.panel1 = new System.Windows.Forms.Panel();
		this.label1 = new System.Windows.Forms.Label();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.panel2 = new System.Windows.Forms.Panel();
		this.label2 = new System.Windows.Forms.Label();
		this.pictureBox2 = new System.Windows.Forms.PictureBox();
		this.panel3 = new System.Windows.Forms.Panel();
		this.label3 = new System.Windows.Forms.Label();
		this.pictureBox3 = new System.Windows.Forms.PictureBox();
		this.textBox1 = new System.Windows.Forms.TextBox();
		this.panel4 = new System.Windows.Forms.Panel();
		this.panel8 = new System.Windows.Forms.Panel();
		this.m_RecentSessionLabel1 = new System.Windows.Forms.LinkLabel();
		this.m_RecentSessionLabel6 = new System.Windows.Forms.LinkLabel();
		this.m_RecentSessionLabel2 = new System.Windows.Forms.LinkLabel();
		this.m_RecentSessionLabel5 = new System.Windows.Forms.LinkLabel();
		this.m_RecentSessionLabel3 = new System.Windows.Forms.LinkLabel();
		this.m_RecentSessionLabel4 = new System.Windows.Forms.LinkLabel();
		this.label4 = new System.Windows.Forms.Label();
		this.panel5 = new System.Windows.Forms.Panel();
		this.panel6 = new System.Windows.Forms.Panel();
		this.textBox2 = new System.Windows.Forms.TextBox();
		this.panel7 = new System.Windows.Forms.Panel();
		this.label5 = new System.Windows.Forms.Label();
		this.panel9 = new System.Windows.Forms.Panel();
		this.panel10 = new System.Windows.Forms.Panel();
		this.panel11 = new System.Windows.Forms.Panel();
		this.panel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		this.panel2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).BeginInit();
		this.panel3.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox3).BeginInit();
		this.panel4.SuspendLayout();
		this.panel8.SuspendLayout();
		this.panel5.SuspendLayout();
		this.panel6.SuspendLayout();
		this.panel7.SuspendLayout();
		this.panel9.SuspendLayout();
		this.panel10.SuspendLayout();
		this.panel11.SuspendLayout();
		base.SuspendLayout();
		this.m_ShowStartupPageCheckBox.AutoSize = true;
		this.m_ShowStartupPageCheckBox.Checked = true;
		this.m_ShowStartupPageCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_ShowStartupPageCheckBox.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ShowStartupPageCheckBox.Location = new System.Drawing.Point(28, 627);
		this.m_ShowStartupPageCheckBox.Name = "m_ShowStartupPageCheckBox";
		this.m_ShowStartupPageCheckBox.Size = new System.Drawing.Size(180, 21);
		this.m_ShowStartupPageCheckBox.TabIndex = 0;
		this.m_ShowStartupPageCheckBox.Text = "Show this page on startup";
		this.m_ShowStartupPageCheckBox.UseVisualStyleBackColor = true;
		this.m_ShowStartupPageCheckBox.CheckedChanged += new System.EventHandler(ShowStartupPageCheckChanged);
		this.panel1.BackColor = System.Drawing.Color.Silver;
		this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel1.Controls.Add(this.pictureBox1);
		this.panel1.Controls.Add(this.panel10);
		this.panel1.Location = new System.Drawing.Point(540, 157);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(320, 205);
		this.panel1.TabIndex = 1;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.ForeColor = System.Drawing.Color.White;
		this.label1.Location = new System.Drawing.Point(3, 1);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(187, 25);
		this.label1.TabIndex = 1;
		this.label1.Text = "Promotional Video";
		this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(0, 28);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(318, 175);
		this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.Click += new System.EventHandler(PromotionalVideoClick);
		this.pictureBox1.MouseEnter += new System.EventHandler(ImageMouseEnter);
		this.pictureBox1.MouseLeave += new System.EventHandler(ImageMouseLeave);
		this.panel2.BackColor = System.Drawing.Color.Silver;
		this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel2.Controls.Add(this.pictureBox2);
		this.panel2.Controls.Add(this.panel11);
		this.panel2.Location = new System.Drawing.Point(193, 157);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(320, 205);
		this.panel2.TabIndex = 2;
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.ForeColor = System.Drawing.Color.White;
		this.label2.Location = new System.Drawing.Point(3, 1);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(129, 25);
		this.label2.TabIndex = 1;
		this.label2.Text = "Setup Video";
		this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
		this.pictureBox2.Image = (System.Drawing.Image)resources.GetObject("pictureBox2.Image");
		this.pictureBox2.Location = new System.Drawing.Point(0, 28);
		this.pictureBox2.Name = "pictureBox2";
		this.pictureBox2.Size = new System.Drawing.Size(318, 175);
		this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.pictureBox2.TabIndex = 0;
		this.pictureBox2.TabStop = false;
		this.pictureBox2.Click += new System.EventHandler(SetupVideoClick);
		this.pictureBox2.MouseEnter += new System.EventHandler(ImageMouseEnter);
		this.pictureBox2.MouseLeave += new System.EventHandler(ImageMouseLeave);
		this.panel3.BackColor = System.Drawing.Color.SteelBlue;
		this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel3.Controls.Add(this.pictureBox3);
		this.panel3.Controls.Add(this.panel9);
		this.panel3.Location = new System.Drawing.Point(193, 393);
		this.panel3.Name = "panel3";
		this.panel3.Size = new System.Drawing.Size(320, 205);
		this.panel3.TabIndex = 3;
		this.label3.AutoSize = true;
		this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.ForeColor = System.Drawing.Color.White;
		this.label3.Location = new System.Drawing.Point(3, 1);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(182, 25);
		this.label3.TabIndex = 1;
		this.label3.Text = "Start Demo Game";
		this.pictureBox3.Dock = System.Windows.Forms.DockStyle.Fill;
		this.pictureBox3.Image = (System.Drawing.Image)resources.GetObject("pictureBox3.Image");
		this.pictureBox3.Location = new System.Drawing.Point(0, 28);
		this.pictureBox3.Name = "pictureBox3";
		this.pictureBox3.Size = new System.Drawing.Size(318, 175);
		this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
		this.pictureBox3.TabIndex = 0;
		this.pictureBox3.TabStop = false;
		this.pictureBox3.Click += new System.EventHandler(LaunchDemoClick);
		this.pictureBox3.MouseEnter += new System.EventHandler(ImageMouseEnter);
		this.pictureBox3.MouseLeave += new System.EventHandler(ImageMouseLeave);
		this.textBox1.BackColor = System.Drawing.Color.FromArgb(159, 159, 159);
		this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBox1.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBox1.ForeColor = System.Drawing.Color.White;
		this.textBox1.Location = new System.Drawing.Point(8, 8);
		this.textBox1.Margin = new System.Windows.Forms.Padding(8);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.ReadOnly = true;
		this.textBox1.Size = new System.Drawing.Size(145, 186);
		this.textBox1.TabIndex = 5;
		this.textBox1.Text = "Getting Started\r\n\r\nWatch the FramePro Setup Guide video to see how to integrate FramePro into your codebase and start profiling your application.\r\n\r\n\r\n";
		this.panel4.BackColor = System.Drawing.Color.Teal;
		this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel4.Controls.Add(this.panel8);
		this.panel4.Controls.Add(this.label4);
		this.panel4.Location = new System.Drawing.Point(541, 394);
		this.panel4.Name = "panel4";
		this.panel4.Size = new System.Drawing.Size(320, 205);
		this.panel4.TabIndex = 7;
		this.panel8.BackColor = System.Drawing.Color.Gainsboro;
		this.panel8.Controls.Add(this.m_RecentSessionLabel1);
		this.panel8.Controls.Add(this.m_RecentSessionLabel6);
		this.panel8.Controls.Add(this.m_RecentSessionLabel2);
		this.panel8.Controls.Add(this.m_RecentSessionLabel5);
		this.panel8.Controls.Add(this.m_RecentSessionLabel3);
		this.panel8.Controls.Add(this.m_RecentSessionLabel4);
		this.panel8.Location = new System.Drawing.Point(0, 28);
		this.panel8.Name = "panel8";
		this.panel8.Size = new System.Drawing.Size(320, 176);
		this.panel8.TabIndex = 8;
		this.m_RecentSessionLabel1.AutoSize = true;
		this.m_RecentSessionLabel1.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel1.Location = new System.Drawing.Point(18, 17);
		this.m_RecentSessionLabel1.Name = "m_RecentSessionLabel1";
		this.m_RecentSessionLabel1.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel1.TabIndex = 2;
		this.m_RecentSessionLabel1.TabStop = true;
		this.m_RecentSessionLabel1.Text = "Session1.framepro";
		this.m_RecentSessionLabel6.AutoSize = true;
		this.m_RecentSessionLabel6.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel6.Location = new System.Drawing.Point(18, 137);
		this.m_RecentSessionLabel6.Name = "m_RecentSessionLabel6";
		this.m_RecentSessionLabel6.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel6.TabIndex = 7;
		this.m_RecentSessionLabel6.TabStop = true;
		this.m_RecentSessionLabel6.Text = "Session6.framepro";
		this.m_RecentSessionLabel2.AutoSize = true;
		this.m_RecentSessionLabel2.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel2.Location = new System.Drawing.Point(18, 41);
		this.m_RecentSessionLabel2.Name = "m_RecentSessionLabel2";
		this.m_RecentSessionLabel2.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel2.TabIndex = 3;
		this.m_RecentSessionLabel2.TabStop = true;
		this.m_RecentSessionLabel2.Text = "Session2.framepro";
		this.m_RecentSessionLabel5.AutoSize = true;
		this.m_RecentSessionLabel5.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel5.Location = new System.Drawing.Point(18, 113);
		this.m_RecentSessionLabel5.Name = "m_RecentSessionLabel5";
		this.m_RecentSessionLabel5.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel5.TabIndex = 6;
		this.m_RecentSessionLabel5.TabStop = true;
		this.m_RecentSessionLabel5.Text = "Session5.framepro";
		this.m_RecentSessionLabel3.AutoSize = true;
		this.m_RecentSessionLabel3.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel3.Location = new System.Drawing.Point(18, 65);
		this.m_RecentSessionLabel3.Name = "m_RecentSessionLabel3";
		this.m_RecentSessionLabel3.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel3.TabIndex = 4;
		this.m_RecentSessionLabel3.TabStop = true;
		this.m_RecentSessionLabel3.Text = "Session3.framepro";
		this.m_RecentSessionLabel4.AutoSize = true;
		this.m_RecentSessionLabel4.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecentSessionLabel4.Location = new System.Drawing.Point(18, 89);
		this.m_RecentSessionLabel4.Name = "m_RecentSessionLabel4";
		this.m_RecentSessionLabel4.Size = new System.Drawing.Size(117, 17);
		this.m_RecentSessionLabel4.TabIndex = 5;
		this.m_RecentSessionLabel4.TabStop = true;
		this.m_RecentSessionLabel4.Text = "Session4.framepro";
		this.label4.AutoSize = true;
		this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label4.ForeColor = System.Drawing.Color.White;
		this.label4.Location = new System.Drawing.Point(3, 0);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(132, 25);
		this.label4.TabIndex = 1;
		this.label4.Text = "Recent Files";
		this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel5.Controls.Add(this.textBox1);
		this.panel5.Location = new System.Drawing.Point(28, 158);
		this.panel5.Name = "panel5";
		this.panel5.Size = new System.Drawing.Size(163, 204);
		this.panel5.TabIndex = 8;
		this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.panel6.Controls.Add(this.textBox2);
		this.panel6.Location = new System.Drawing.Point(28, 393);
		this.panel6.Name = "panel6";
		this.panel6.Size = new System.Drawing.Size(163, 204);
		this.panel6.TabIndex = 9;
		this.textBox2.BackColor = System.Drawing.Color.FromArgb(159, 159, 159);
		this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBox2.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBox2.ForeColor = System.Drawing.Color.White;
		this.textBox2.Location = new System.Drawing.Point(8, 8);
		this.textBox2.Margin = new System.Windows.Forms.Padding(8);
		this.textBox2.Multiline = true;
		this.textBox2.Name = "textBox2";
		this.textBox2.ReadOnly = true;
		this.textBox2.Size = new System.Drawing.Size(145, 186);
		this.textBox2.TabIndex = 5;
		this.textBox2.Text = "Demo\r\n\r\nLaunch the game simulator demo. This will send scopes to FramePro as if from a real game, allowing you to connect and profile and try out FramePro.\r\n";
		this.panel7.AutoScroll = true;
		this.panel7.Controls.Add(this.label5);
		this.panel7.Controls.Add(this.panel5);
		this.panel7.Controls.Add(this.panel6);
		this.panel7.Controls.Add(this.m_ShowStartupPageCheckBox);
		this.panel7.Controls.Add(this.panel1);
		this.panel7.Controls.Add(this.panel4);
		this.panel7.Controls.Add(this.panel2);
		this.panel7.Controls.Add(this.panel3);
		this.panel7.Dock = System.Windows.Forms.DockStyle.Fill;
		this.panel7.Location = new System.Drawing.Point(0, 0);
		this.panel7.Name = "panel7";
		this.panel7.Size = new System.Drawing.Size(1048, 784);
		this.panel7.TabIndex = 10;
		this.label5.AutoSize = true;
		this.label5.Font = new System.Drawing.Font("Monaco", 72f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label5.ForeColor = System.Drawing.Color.DimGray;
		this.label5.Location = new System.Drawing.Point(23, 16);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(491, 128);
		this.label5.TabIndex = 10;
		this.label5.Text = "FramePro";
		this.panel9.Controls.Add(this.label3);
		this.panel9.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel9.Location = new System.Drawing.Point(0, 0);
		this.panel9.Name = "panel9";
		this.panel9.Size = new System.Drawing.Size(318, 28);
		this.panel9.TabIndex = 2;
		this.panel10.Controls.Add(this.label1);
		this.panel10.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel10.Location = new System.Drawing.Point(0, 0);
		this.panel10.Name = "panel10";
		this.panel10.Size = new System.Drawing.Size(318, 28);
		this.panel10.TabIndex = 2;
		this.panel11.Controls.Add(this.label2);
		this.panel11.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel11.Location = new System.Drawing.Point(0, 0);
		this.panel11.Name = "panel11";
		this.panel11.Size = new System.Drawing.Size(318, 28);
		this.panel11.TabIndex = 2;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.FromArgb(159, 159, 159);
		base.Controls.Add(this.panel7);
		base.Margin = new System.Windows.Forms.Padding(8);
		base.Name = "StartupPage";
		base.Size = new System.Drawing.Size(1048, 784);
		this.panel1.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		this.panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).EndInit();
		this.panel3.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.pictureBox3).EndInit();
		this.panel4.ResumeLayout(false);
		this.panel4.PerformLayout();
		this.panel8.ResumeLayout(false);
		this.panel8.PerformLayout();
		this.panel5.ResumeLayout(false);
		this.panel5.PerformLayout();
		this.panel6.ResumeLayout(false);
		this.panel6.PerformLayout();
		this.panel7.ResumeLayout(false);
		this.panel7.PerformLayout();
		this.panel9.ResumeLayout(false);
		this.panel9.PerformLayout();
		this.panel10.ResumeLayout(false);
		this.panel10.PerformLayout();
		this.panel11.ResumeLayout(false);
		this.panel11.PerformLayout();
		base.ResumeLayout(false);
	}
}
