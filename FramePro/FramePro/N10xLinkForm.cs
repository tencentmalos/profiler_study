using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FramePro.Properties;

namespace FramePro;

public class N10xLinkForm : Form
{
	private IContainer components;

	private Label label1;

	private LinkLabel linkLabel1;

	private Button button1;

	private PictureBox pictureBox1;

	public N10xLinkForm()
	{
		InitializeComponent();
	}

	private void ShowWebsite()
	{
		Process.Start("https://10xeditor.com/");
	}

	private void FindOutMoreClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		ShowWebsite();
	}

	private void CloseButtonClicked(object sender, EventArgs e)
	{
		Close();
	}

	private void TextClick(object sender, EventArgs e)
	{
		ShowWebsite();
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
		this.label1 = new System.Windows.Forms.Label();
		this.linkLabel1 = new System.Windows.Forms.LinkLabel();
		this.button1 = new System.Windows.Forms.Button();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.BackColor = System.Drawing.Color.Transparent;
		this.label1.Cursor = System.Windows.Forms.Cursors.Hand;
		this.label1.Font = new System.Drawing.Font("Monaco", 14.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.ForeColor = System.Drawing.Color.White;
		this.label1.Location = new System.Drawing.Point(179, 221);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(442, 125);
		this.label1.TabIndex = 0;
		this.label1.Text = "Try out the new code editor from PureDev Software\r\n\r\n\"A blazingly fast editor\"\r\n\r\nExperience the difference";
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label1.Click += new System.EventHandler(TextClick);
		this.linkLabel1.AutoSize = true;
		this.linkLabel1.BackColor = System.Drawing.Color.Transparent;
		this.linkLabel1.Font = new System.Drawing.Font("Monaco", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.linkLabel1.ForeColor = System.Drawing.Color.White;
		this.linkLabel1.LinkColor = System.Drawing.Color.Cyan;
		this.linkLabel1.Location = new System.Drawing.Point(320, 370);
		this.linkLabel1.Name = "linkLabel1";
		this.linkLabel1.Size = new System.Drawing.Size(163, 32);
		this.linkLabel1.TabIndex = 1;
		this.linkLabel1.TabStop = true;
		this.linkLabel1.Text = "find out more";
		this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(FindOutMoreClicked);
		this.button1.Location = new System.Drawing.Point(364, 435);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 3;
		this.button1.Text = "Close";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(CloseButtonClicked);
		this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
		this.pictureBox1.BackgroundImage = FramePro.Properties.Resources._10x_editor_title;
		this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
		this.pictureBox1.Location = new System.Drawing.Point(280, 38);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(244, 180);
		this.pictureBox1.TabIndex = 4;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.Click += new System.EventHandler(TextClick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackgroundImage = FramePro.Properties.Resources.N10x_editor_background;
		this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
		base.ClientSize = new System.Drawing.Size(800, 470);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.linkLabel1);
		base.Controls.Add(this.label1);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "N10xLinkForm";
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "10x Editor";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
