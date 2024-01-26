using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

public class AutoUpdate : Form
{
	private IContainer components;

	private Button button1;

	private Button button2;

	private CheckBox m_DontAskAgainCheckBox;

	private WebBrowser webBrowser1;

	public bool DontAskAgain => m_DontAskAgainCheckBox.Checked;

	public AutoUpdate()
	{
		InitializeComponent();
		webBrowser1.Refresh(WebBrowserRefreshOption.Completely);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.AutoUpdate));
		this.m_DontAskAgainCheckBox = new System.Windows.Forms.CheckBox();
		this.webBrowser1 = new System.Windows.Forms.WebBrowser();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.m_DontAskAgainCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_DontAskAgainCheckBox.AutoSize = true;
		this.m_DontAskAgainCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DontAskAgainCheckBox.ForeColor = System.Drawing.Color.Gray;
		this.m_DontAskAgainCheckBox.Location = new System.Drawing.Point(5, 328);
		this.m_DontAskAgainCheckBox.Name = "m_DontAskAgainCheckBox";
		this.m_DontAskAgainCheckBox.Size = new System.Drawing.Size(107, 17);
		this.m_DontAskAgainCheckBox.TabIndex = 3;
		this.m_DontAskAgainCheckBox.Text = "Never ask again";
		this.m_DontAskAgainCheckBox.UseVisualStyleBackColor = true;
		this.webBrowser1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.webBrowser1.Location = new System.Drawing.Point(0, 0);
		this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
		this.webBrowser1.Name = "webBrowser1";
		this.webBrowser1.ScrollBarsEnabled = false;
		this.webBrowser1.Size = new System.Drawing.Size(457, 303);
		this.webBrowser1.TabIndex = 7;
		this.webBrowser1.Url = new System.Uri("https://www.puredevsoftware.com/framepro/update_info.htm", System.UriKind.Absolute);
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.BackColor = System.Drawing.Color.White;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.No;
		this.button1.FlatAppearance.BorderSize = 0;
		this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
		this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
		this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button1.Font = new System.Drawing.Font("Monaco", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.ForeColor = System.Drawing.Color.DarkGray;
		this.button1.Location = new System.Drawing.Point(154, 309);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(94, 36);
		this.button1.TabIndex = 1;
		this.button1.Text = "Ignore";
		this.button1.UseVisualStyleBackColor = false;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.BackColor = System.Drawing.Color.FromArgb(255, 128, 0);
		this.button2.DialogResult = System.Windows.Forms.DialogResult.Yes;
		this.button2.FlatAppearance.BorderSize = 0;
		this.button2.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(192, 64, 0);
		this.button2.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(255, 192, 128);
		this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button2.Font = new System.Drawing.Font("Monaco", 15.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.ForeColor = System.Drawing.Color.White;
		this.button2.Location = new System.Drawing.Point(254, 309);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(203, 36);
		this.button2.TabIndex = 2;
		this.button2.Text = "Update Now...";
		this.button2.UseVisualStyleBackColor = false;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(464, 350);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.webBrowser1);
		base.Controls.Add(this.m_DontAskAgainCheckBox);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "AutoUpdate";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "ProfilerStudy Update";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
