using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

public class ProgressBar : Form
{
	private System.Windows.Forms.Timer m_Timer = new System.Windows.Forms.Timer();

	private ThreadJob m_ThreadJob;

	private const int m_BarWidth = 20;

	private const int m_BarHighlightWidth = 100;

	private const int m_BarGap = 4;

	private const int m_BarBorder = 4;

	private const int m_Speed = 1000;

	private int m_StartTime;

	private IContainer components;

	private Button m_CancelButton;

	private System.Windows.Forms.ProgressBar m_ProgressBar;

	public static object Show(string message, ThreadJob thread_job, bool topmost, bool can_cancel)
	{
		ProgressBar progressBar = new ProgressBar(message, thread_job, topmost, can_cancel);
		progressBar.ShowDialog(MainForm.Inst);
		return progressBar.m_ThreadJob.Result;
	}

	public static object Show(string message, ThreadJobMain thread_job_main)
	{
		ThreadJob thread_job = new ThreadJob(thread_job_main, null);
		return Show(message, thread_job, topmost: false, can_cancel: true);
	}

	public static object Show(string message, ThreadJobMain thread_job_main, object arg)
	{
		ThreadJob thread_job = new ThreadJob(thread_job_main, arg);
		return Show(message, thread_job, topmost: false, can_cancel: true);
	}

	public static object Show(string message, ThreadJobMain thread_job_main, object arg, bool can_cancel)
	{
		ThreadJob thread_job = new ThreadJob(thread_job_main, arg);
		return Show(message, thread_job, topmost: false, can_cancel);
	}

	public static object ShowTopMost(string message, ThreadJobMain thread_job_main, object arg)
	{
		ThreadJob thread_job = new ThreadJob(thread_job_main, arg);
		return Show(message, thread_job, topmost: true, can_cancel: true);
	}

	private ProgressBar(string message, ThreadJob thread_job, bool topmost, bool can_cancel)
	{
		InitializeComponent();
		base.TopMost = topmost;
		Text = Text + " : " + message;
		m_ThreadJob = thread_job;
		m_StartTime = Environment.TickCount;
		m_Timer.Interval = 100;
		m_Timer.Tick += TimerTick;
		m_Timer.Start();
		m_CancelButton.Visible = can_cancel;
		thread_job.Run();
	}

	private void TimerTick(object sender, EventArgs e)
	{
		m_ProgressBar.Value = m_ThreadJob.PercentComplete;
		if (m_ThreadJob.Finished)
		{
			m_Timer.Stop();
			Close();
		}
	}

	private static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		m_Timer.Stop();
		m_Timer.Tick -= TimerTick;
		MainForm.Inst.Cursor = Cursors.Default;
		base.OnClosing(e);
	}

	private void CancelButtonClicked(object sender, EventArgs e)
	{
		m_ThreadJob.Cancel();
		m_CancelButton.Text = "Canceling...";
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerStudy.ProgressBar));
		this.m_CancelButton = new System.Windows.Forms.Button();
		this.m_ProgressBar = new System.Windows.Forms.ProgressBar();
		base.SuspendLayout();
		this.m_CancelButton.BackColor = System.Drawing.Color.WhiteSmoke;
		this.m_CancelButton.Cursor = System.Windows.Forms.Cursors.WaitCursor;
		this.m_CancelButton.FlatAppearance.BorderSize = 0;
		this.m_CancelButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(225, 225, 225);
		this.m_CancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.m_CancelButton.Font = new System.Drawing.Font("Monaco", 6.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_CancelButton.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
		this.m_CancelButton.Location = new System.Drawing.Point(165, 28);
		this.m_CancelButton.Name = "m_CancelButton";
		this.m_CancelButton.Size = new System.Drawing.Size(124, 20);
		this.m_CancelButton.TabIndex = 0;
		this.m_CancelButton.Text = "Cancel";
		this.m_CancelButton.UseVisualStyleBackColor = false;
		this.m_CancelButton.UseWaitCursor = true;
		this.m_CancelButton.Click += new System.EventHandler(CancelButtonClicked);
		this.m_ProgressBar.Location = new System.Drawing.Point(12, 6);
		this.m_ProgressBar.Name = "m_ProgressBar";
		this.m_ProgressBar.Size = new System.Drawing.Size(414, 23);
		this.m_ProgressBar.TabIndex = 1;
		this.m_ProgressBar.UseWaitCursor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.WhiteSmoke;
		base.ClientSize = new System.Drawing.Size(445, 48);
		base.ControlBox = false;
		base.Controls.Add(this.m_ProgressBar);
		base.Controls.Add(this.m_CancelButton);
		this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Name = "ProgressBar";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "ProfilerStudy";
		base.UseWaitCursor = true;
		base.ResumeLayout(false);
	}
}
