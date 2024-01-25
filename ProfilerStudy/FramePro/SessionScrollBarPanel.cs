using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class SessionScrollBarPanel : UserControl
{
	private IContainer components;

	private SessionScrollBar m_SessionScrollBar;

	private VerticalLabelPanel verticalLabelPanel3;

	private Panel panel1;

	private Panel panel2;

	private FrameProButton m_TimeModeButton;

	private FrameProButton m_FrameModeButton;

	private Panel panel3;

	public double TargetFrameMS
	{
		get
		{
			return m_SessionScrollBar.TargetFrameMS;
		}
		set
		{
			m_SessionScrollBar.TargetFrameMS = value;
		}
	}

	public event SessionScrollBarChangedHandler ScrollBarChanged;

	public SessionScrollBarPanel()
	{
		InitializeComponent();
		UpdateButtonStates();
	}

	public void SetSession(Session session)
	{
		m_SessionScrollBar.SetSession(session);
	}

	public void SetSettings(Settings settings)
	{
		m_SessionScrollBar.SetSettings(settings);
		UpdateButtonStates();
	}

	public void SetSelectedRange(long start_frame_x, long end_frame_x)
	{
		m_SessionScrollBar.SetSelectedRange(start_frame_x, end_frame_x);
	}

	private void ScrollBarChangedEvent(long start_frame_x, long end_frame_x)
	{
		if (this.ScrollBarChanged != null)
		{
			this.ScrollBarChanged(start_frame_x, end_frame_x);
		}
	}

	public void OnSessionChanged()
	{
		m_SessionScrollBar.OnSessionChanged();
	}

	private void FrameModeButtonClick(object sender, EventArgs e)
	{
		m_SessionScrollBar.Mode = SessionScrollBar.EMode.Frame;
		UpdateButtonStates();
	}

	private void TimeModeButtonClick(object sender, EventArgs e)
	{
		m_SessionScrollBar.Mode = SessionScrollBar.EMode.Time;
		UpdateButtonStates();
	}

	private void UpdateButtonStates()
	{
		switch (m_SessionScrollBar.Mode)
		{
		case SessionScrollBar.EMode.Frame:
			m_FrameModeButton.BackColor = Colours.SessionScrollBarButtonCheckedColour;
			m_FrameModeButton.HighlightEnabled = false;
			m_TimeModeButton.BackColor = BackColor;
			m_TimeModeButton.HighlightEnabled = true;
			break;
		case SessionScrollBar.EMode.Time:
			m_FrameModeButton.BackColor = BackColor;
			m_FrameModeButton.HighlightEnabled = true;
			m_TimeModeButton.BackColor = Colours.SessionScrollBarButtonCheckedColour;
			m_TimeModeButton.HighlightEnabled = false;
			break;
		}
	}

	public void UpdateSessionScrollBarMode()
	{
		SessionScrollBar.EMode mode = m_SessionScrollBar.Mode;
		m_SessionScrollBar.UpdateSessionScrollBarMode();
		if (m_SessionScrollBar.Mode != mode)
		{
			UpdateButtonStates();
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
		this.panel1 = new System.Windows.Forms.Panel();
		this.panel2 = new System.Windows.Forms.Panel();
		this.panel3 = new System.Windows.Forms.Panel();
		this.verticalLabelPanel3 = new FramePro.VerticalLabelPanel();
		this.m_TimeModeButton = new FramePro.FrameProButton();
		this.m_FrameModeButton = new FramePro.FrameProButton();
		this.m_SessionScrollBar = new FramePro.SessionScrollBar();
		this.panel1.SuspendLayout();
		this.panel2.SuspendLayout();
		base.SuspendLayout();
		this.panel1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
		this.panel1.Controls.Add(this.panel2);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(173, 61);
		this.panel1.TabIndex = 10;
		this.panel2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.panel2.BackColor = System.Drawing.SystemColors.Control;
		this.panel2.Controls.Add(this.panel3);
		this.panel2.Controls.Add(this.verticalLabelPanel3);
		this.panel2.Controls.Add(this.m_TimeModeButton);
		this.panel2.Controls.Add(this.m_FrameModeButton);
		this.panel2.Location = new System.Drawing.Point(0, 2);
		this.panel2.Name = "panel2";
		this.panel2.Size = new System.Drawing.Size(172, 59);
		this.panel2.TabIndex = 0;
		this.panel3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.panel3.BackColor = System.Drawing.Color.FromArgb(99, 99, 99);
		this.panel3.Location = new System.Drawing.Point(97, 0);
		this.panel3.Name = "panel3";
		this.panel3.Size = new System.Drawing.Size(1, 59);
		this.panel3.TabIndex = 12;
		this.verticalLabelPanel3.BackColor = System.Drawing.Color.FromArgb(222, 222, 222);
		this.verticalLabelPanel3.Dock = System.Windows.Forms.DockStyle.Left;
		this.verticalLabelPanel3.Font = new System.Drawing.Font("Monaco", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.verticalLabelPanel3.Location = new System.Drawing.Point(0, 0);
		this.verticalLabelPanel3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
		this.verticalLabelPanel3.Name = "verticalLabelPanel3";
		this.verticalLabelPanel3.PanelText = "";
		this.verticalLabelPanel3.Size = new System.Drawing.Size(23, 59);
		this.verticalLabelPanel3.TabIndex = 9;
		this.m_TimeModeButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_TimeModeButton.ButtonText = "Time";
		this.m_TimeModeButton.DisabledImage = null;
		this.m_TimeModeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TimeModeButton.HighlightEnabled = true;
		this.m_TimeModeButton.Image = null;
		this.m_TimeModeButton.Location = new System.Drawing.Point(98, 0);
		this.m_TimeModeButton.Name = "m_TimeModeButton";
		this.m_TimeModeButton.Size = new System.Drawing.Size(75, 59);
		this.m_TimeModeButton.TabIndex = 11;
		this.m_TimeModeButton.UseImageAsText = false;
		this.m_TimeModeButton.Click += new System.EventHandler(TimeModeButtonClick);
		this.m_FrameModeButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_FrameModeButton.ButtonText = "Frame";
		this.m_FrameModeButton.DisabledImage = null;
		this.m_FrameModeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_FrameModeButton.HighlightEnabled = true;
		this.m_FrameModeButton.Image = null;
		this.m_FrameModeButton.Location = new System.Drawing.Point(22, 0);
		this.m_FrameModeButton.Name = "m_FrameModeButton";
		this.m_FrameModeButton.Size = new System.Drawing.Size(75, 59);
		this.m_FrameModeButton.TabIndex = 10;
		this.m_FrameModeButton.UseImageAsText = false;
		this.m_FrameModeButton.Click += new System.EventHandler(FrameModeButtonClick);
		this.m_SessionScrollBar.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_SessionScrollBar.Location = new System.Drawing.Point(173, 0);
		this.m_SessionScrollBar.Mode = FramePro.SessionScrollBar.EMode.Frame;
		this.m_SessionScrollBar.Name = "m_SessionScrollBar";
		this.m_SessionScrollBar.Size = new System.Drawing.Size(904, 61);
		this.m_SessionScrollBar.TabIndex = 1;
		this.m_SessionScrollBar.TargetFrameMS = 0.0;
		this.m_SessionScrollBar.SessionScrollBarChanged += new FramePro.SessionScrollBarChangedHandler(ScrollBarChangedEvent);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_SessionScrollBar);
		base.Controls.Add(this.panel1);
		base.Name = "SessionScrollBarPanel";
		base.Size = new System.Drawing.Size(1077, 61);
		this.panel1.ResumeLayout(false);
		this.panel2.ResumeLayout(false);
		base.ResumeLayout(false);
	}
}
