using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

public class WaitForProcessingCompleteForm : Form
{
	private Session m_Session;

	private Thread m_Thread;

	private volatile bool m_Closing;

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private IContainer components;

	private TextBox textBox1;

	private Button button1;

	private Button m_DontWaitButton;

	private System.Windows.Forms.ProgressBar m_ProgressBar;

	public WaitForProcessingCompleteForm(Session session, bool can_ignore)
	{
		InitializeComponent();
		m_Session = session;
		m_DontWaitButton.Visible = can_ignore;
		m_Thread = new Thread(CheckDoneThread);
		m_Thread.Name = "WaitForProcessingCompleteForm_WaitThread";
		m_Thread.Start();
	}

	private void CheckDoneThread()
	{
		while (!m_Closing && m_Session.ProcessingPackets)
		{
			m_Session.WaitforProcessingToFinish(200);
			m_ControlTaskDispatcher.QueueTask(delegate
			{
				m_ProgressBar.Value = m_Session.ProcessingCompletePercent;
			});
		}
		if (!m_Closing)
		{
			if (!m_Session.ProcessingPackets)
			{
				base.DialogResult = DialogResult.OK;
			}
			m_ControlTaskDispatcher.QueueTask(delegate
			{
				Close();
			});
		}
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
		m_Closing = true;
		m_Thread.Join();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerStudy.WaitForProcessingCompleteForm));
		this.textBox1 = new System.Windows.Forms.TextBox();
		this.button1 = new System.Windows.Forms.Button();
		this.m_DontWaitButton = new System.Windows.Forms.Button();
		this.m_ProgressBar = new System.Windows.Forms.ProgressBar();
		base.SuspendLayout();
		this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBox1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBox1.Location = new System.Drawing.Point(12, 23);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.ReadOnly = true;
		this.textBox1.Size = new System.Drawing.Size(387, 35);
		this.textBox1.TabIndex = 0;
		this.textBox1.TabStop = false;
		this.textBox1.Text = "Waiting for processing to complete...";
		this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(324, 81);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 1;
		this.button1.Text = "Cancel";
		this.button1.UseVisualStyleBackColor = true;
		this.m_DontWaitButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.m_DontWaitButton.DialogResult = System.Windows.Forms.DialogResult.Ignore;
		this.m_DontWaitButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DontWaitButton.Location = new System.Drawing.Point(243, 81);
		this.m_DontWaitButton.Name = "m_DontWaitButton";
		this.m_DontWaitButton.Size = new System.Drawing.Size(75, 23);
		this.m_DontWaitButton.TabIndex = 2;
		this.m_DontWaitButton.Text = "Don't Wait";
		this.m_DontWaitButton.UseVisualStyleBackColor = true;
		this.m_ProgressBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ProgressBar.Location = new System.Drawing.Point(12, 45);
		this.m_ProgressBar.Name = "m_ProgressBar";
		this.m_ProgressBar.Size = new System.Drawing.Size(387, 23);
		this.m_ProgressBar.TabIndex = 3;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(411, 117);
		base.Controls.Add(this.m_ProgressBar);
		base.Controls.Add(this.m_DontWaitButton);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.textBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "WaitForProcessingCompleteForm";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Waiting for data processing to complete...";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
