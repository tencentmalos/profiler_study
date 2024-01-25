using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class RecordingDataForm : Form
{
	private Session m_Session;

	private Timer m_TickTimer = new Timer();

	private IContainer components;

	private Label label1;

	private Label m_ProfileSizeLabel;

	private Button button1;

	public RecordingDataForm(Session session)
	{
		InitializeComponent();
		m_Session = session;
		m_TickTimer.Interval = 1000;
		m_TickTimer.Tick += m_TickTimer_Tick;
		m_TickTimer.Start();
	}

	private void m_TickTimer_Tick(object sender, EventArgs e)
	{
		string text = ((float)m_Session.RecordingFileSize / 1024f / 1024f).ToString("0.0");
		m_ProfileSizeLabel.Text = "Profile Size: " + text + "MB";
		if (!m_Session.Connected)
		{
			Close();
		}
	}

	private void FinishButtonClicked(object sender, EventArgs e)
	{
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.RecordingDataForm));
		this.label1 = new System.Windows.Forms.Label();
		this.m_ProfileSizeLabel = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(12, 20);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(126, 21);
		this.label1.TabIndex = 0;
		this.label1.Text = "Recording Data...";
		this.m_ProfileSizeLabel.AutoSize = true;
		this.m_ProfileSizeLabel.Font = new System.Drawing.Font("Monaco", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ProfileSizeLabel.Location = new System.Drawing.Point(12, 50);
		this.m_ProfileSizeLabel.Name = "m_ProfileSizeLabel";
		this.m_ProfileSizeLabel.Size = new System.Drawing.Size(124, 21);
		this.m_ProfileSizeLabel.TabIndex = 1;
		this.m_ProfileSizeLabel.Text = "Profile size: 0MB";
		this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(199, 39);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(122, 32);
		this.button1.TabIndex = 2;
		this.button1.Text = "Finish";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(FinishButtonClicked);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(333, 86);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.m_ProfileSizeLabel);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "RecordingDataForm";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "FramePro Recording";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
