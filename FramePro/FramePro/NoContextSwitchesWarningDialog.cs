using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

public class NoContextSwitchesWarningDialog : Form
{
	private IContainer components;

	private Button button1;

	private LinkLabel m_LinkLabel;

	public NoContextSwitchesWarningDialog()
	{
		InitializeComponent();
		string text = "this page";
		int start = m_LinkLabel.Text.IndexOf(text);
		m_LinkLabel.Links.Add(start, text.Length, "https://www.puredevsoftware.com/framepro/context_switches.htm");
	}

	private void LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		Process.Start(e.Link.LinkData.ToString());
		Close();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.NoContextSwitchesWarningDialog));
		this.button1 = new System.Windows.Forms.Button();
		this.m_LinkLabel = new System.Windows.Forms.LinkLabel();
		base.SuspendLayout();
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(140, 80);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 0;
		this.button1.Text = "OK";
		this.button1.UseVisualStyleBackColor = true;
		this.m_LinkLabel.AutoSize = true;
		this.m_LinkLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_LinkLabel.Location = new System.Drawing.Point(45, 23);
		this.m_LinkLabel.Name = "m_LinkLabel";
		this.m_LinkLabel.Size = new System.Drawing.Size(287, 39);
		this.m_LinkLabel.TabIndex = 1;
		this.m_LinkLabel.TabStop = true;
		this.m_LinkLabel.Text = "This session does not contain any context switch data.\r\n\r\nPlease see this page for more information: ";
		this.m_LinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.m_LinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(LinkClicked);
		base.AcceptButton = this.button1;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(355, 115);
		base.Controls.Add(this.m_LinkLabel);
		base.Controls.Add(this.button1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "NoContextSwitchesWarningDialog";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "FramePro";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
