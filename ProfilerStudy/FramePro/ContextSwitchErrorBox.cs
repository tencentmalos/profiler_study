using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using FramePro.Properties;

namespace FramePro;

public class ContextSwitchErrorBox : Form
{
	private IContainer components;

	private PictureBox pictureBox1;

	private TextBox m_TextBox;

	private Button button1;

	private CheckBox m_DontShowAgainCheckBox;

	private LinkLabel linkLabel1;

	public bool DontShowAgain => m_DontShowAgainCheckBox.Checked;

	public ContextSwitchErrorBox(string message)
	{
		InitializeComponent();
		m_TextBox.Text += message;
		m_TextBox.ReadOnly = true;
		m_TextBox.Select(0, 0);
	}

	private void MoreInfoLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		Process.Start("https://www.puredevsoftware.com/framepro/context_switches.htm");
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.ContextSwitchErrorBox));
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.m_TextBox = new System.Windows.Forms.TextBox();
		this.button1 = new System.Windows.Forms.Button();
		this.m_DontShowAgainCheckBox = new System.Windows.Forms.CheckBox();
		this.linkLabel1 = new System.Windows.Forms.LinkLabel();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.pictureBox1.Image = FramePro.Properties.Resources.warning;
		this.pictureBox1.Location = new System.Drawing.Point(14, 14);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(35, 31);
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.m_TextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_TextBox.BackColor = System.Drawing.SystemColors.Control;
		this.m_TextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.m_TextBox.Location = new System.Drawing.Point(56, 14);
		this.m_TextBox.Multiline = true;
		this.m_TextBox.Name = "m_TextBox";
		this.m_TextBox.Size = new System.Drawing.Size(510, 122);
		this.m_TextBox.TabIndex = 1;
		this.m_TextBox.Text = resources.GetString("m_TextBox.Text");
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button1.Location = new System.Drawing.Point(478, 177);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(87, 27);
		this.button1.TabIndex = 2;
		this.button1.Text = "OK";
		this.button1.UseVisualStyleBackColor = true;
		this.m_DontShowAgainCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_DontShowAgainCheckBox.AutoSize = true;
		this.m_DontShowAgainCheckBox.Location = new System.Drawing.Point(56, 185);
		this.m_DontShowAgainCheckBox.Name = "m_DontShowAgainCheckBox";
		this.m_DontShowAgainCheckBox.Size = new System.Drawing.Size(140, 19);
		this.m_DontShowAgainCheckBox.TabIndex = 3;
		this.m_DontShowAgainCheckBox.Text = "Don't show this again";
		this.m_DontShowAgainCheckBox.UseVisualStyleBackColor = true;
		this.linkLabel1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.linkLabel1.AutoSize = true;
		this.linkLabel1.Location = new System.Drawing.Point(52, 151);
		this.linkLabel1.Name = "linkLabel1";
		this.linkLabel1.Size = new System.Drawing.Size(317, 15);
		this.linkLabel1.TabIndex = 4;
		this.linkLabel1.TabStop = true;
		this.linkLabel1.Text = "Click here to find out more about context switch recording";
		this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(MoreInfoLinkClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(580, 218);
		base.Controls.Add(this.linkLabel1);
		base.Controls.Add(this.m_DontShowAgainCheckBox);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.m_TextBox);
		base.Controls.Add(this.pictureBox1);
		this.Font = new System.Drawing.Font("Monaco", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "ContextSwitchErrorBox";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "ProfilerStudy - Warning";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
