using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class FindControl : UserControl
{
	private bool m_TimerStarted;

	private Timer m_Timer = new Timer();

	private const int m_ShortTextUpdateDelay = 500;

	private const int m_ShortStringLength = 2;

	private int m_NextUpdateTime;

	private int m_MatchCount = -1;

	private IContainer components;

	private TextBox m_TextBox;

	private Button m_ClearButton;

	private Button m_NextButton;

	private Label m_MatchCountLabel;

	private Button m_PrevButton;

	private Label m_FindLabel;

	public string FindText => m_TextBox.Text;

	public event FindControlTextChangedHandler FindControlTextChanged;

	public event FindControlGotoPrevHandler FindControlGotoPrev;

	public event FindControlGotoNextHandler FindControlGotoNext;

	public FindControl()
	{
		InitializeComponent();
		m_Timer.Interval = 500;
		m_Timer.Tick += TimerTick;
		m_MatchCountLabel.Visible = false;
		m_FindLabel.ForeColor = Colours.ButtonText;
	}

	public void RecalculateAnchorstoGetAroundDPIBugs()
	{
		int num = m_TextBox.Height / 3;
		int num2 = base.ClientSize.Width - (m_ClearButton.Width + m_PrevButton.Width + m_NextButton.Width + 4 * num);
		m_TextBox.Location = new Point(num, m_TextBox.Location.Y);
		m_TextBox.Size = new Size(num2, m_TextBox.Height);
		m_ClearButton.Location = new Point(m_TextBox.Right, m_ClearButton.Location.Y);
		m_PrevButton.Location = new Point(m_ClearButton.Right + num, m_PrevButton.Location.Y);
		m_NextButton.Location = new Point(m_PrevButton.Right + num, m_PrevButton.Location.Y);
		m_FindLabel.Location = new Point(num, m_FindLabel.Location.Y);
		m_MatchCountLabel.Location = new Point(num, m_FindLabel.Location.Y);
	}

	private void TimerTick(object sender, EventArgs e)
	{
		if (m_NextUpdateTime != 0 && Environment.TickCount >= m_NextUpdateTime)
		{
			if (this.FindControlTextChanged != null)
			{
				this.FindControlTextChanged(m_TextBox.Text);
			}
			m_NextUpdateTime = 0;
			m_TimerStarted = false;
			m_Timer.Stop();
		}
	}

	public void SelectAll()
	{
		m_TextBox.SelectAll();
	}

	private void ClearBoxClicked(object sender, EventArgs e)
	{
		m_TextBox.Clear();
	}

	private void TextBoxTextChanged(object sender, EventArgs e)
	{
		if (!m_TimerStarted)
		{
			m_TimerStarted = true;
			m_Timer.Start();
		}
		m_NextUpdateTime = Environment.TickCount + GetUpdateDelay();
		ShowMatchCountLabel(m_TextBox.Text.Length != 0);
	}

	private int GetUpdateDelay()
	{
		int length = m_TextBox.Text.Length;
		if (length > 2)
		{
			return 0;
		}
		return length * 500 / 2;
	}

	private void FindPrevButtonClicked(object sender, EventArgs e)
	{
		FindPrev();
	}

	private void FindNextButtonClicked(object sender, EventArgs e)
	{
		FindNext();
	}

	private void FindPrev()
	{
		if (this.FindControlGotoPrev != null)
		{
			this.FindControlGotoPrev(m_TextBox.Text);
		}
	}

	private void FindNext()
	{
		if (this.FindControlGotoNext != null)
		{
			this.FindControlGotoNext(m_TextBox.Text);
		}
	}

	private void TextBoxKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			FindNext();
		}
	}

	public void SetMatchCount(int match_count)
	{
		if (match_count != m_MatchCount)
		{
			m_MatchCount = match_count;
			m_MatchCountLabel.Text = "Matches: " + match_count;
		}
	}

	private void ShowMatchCountLabel(bool show)
	{
		if (m_MatchCountLabel.Visible != show)
		{
			m_MatchCountLabel.Visible = show;
			m_FindLabel.Visible = !show;
			if (show)
			{
				m_MatchCountLabel.Text = "Matches: 0";
			}
			else
			{
				m_MatchCount = -1;
			}
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
		this.m_TextBox = new System.Windows.Forms.TextBox();
		this.m_ClearButton = new System.Windows.Forms.Button();
		this.m_NextButton = new System.Windows.Forms.Button();
		this.m_MatchCountLabel = new System.Windows.Forms.Label();
		this.m_PrevButton = new System.Windows.Forms.Button();
		this.m_FindLabel = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.m_TextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_TextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TextBox.Location = new System.Drawing.Point(11, 21);
		this.m_TextBox.Name = "m_TextBox";
		this.m_TextBox.Size = new System.Drawing.Size(195, 22);
		this.m_TextBox.TabIndex = 0;
		this.m_TextBox.TextChanged += new System.EventHandler(TextBoxTextChanged);
		this.m_TextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(TextBoxKeyDown);
		this.m_ClearButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.m_ClearButton.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
		this.m_ClearButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
		this.m_ClearButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.m_ClearButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ClearButton.Location = new System.Drawing.Point(203, 20);
		this.m_ClearButton.Name = "m_ClearButton";
		this.m_ClearButton.Size = new System.Drawing.Size(20, 23);
		this.m_ClearButton.TabIndex = 1;
		this.m_ClearButton.Text = "X";
		this.m_ClearButton.UseVisualStyleBackColor = false;
		this.m_ClearButton.Click += new System.EventHandler(ClearBoxClicked);
		this.m_NextButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.m_NextButton.Location = new System.Drawing.Point(265, 20);
		this.m_NextButton.Name = "m_NextButton";
		this.m_NextButton.Size = new System.Drawing.Size(36, 23);
		this.m_NextButton.TabIndex = 3;
		this.m_NextButton.Text = ">";
		this.m_NextButton.UseVisualStyleBackColor = true;
		this.m_NextButton.Click += new System.EventHandler(FindNextButtonClicked);
		this.m_MatchCountLabel.AutoSize = true;
		this.m_MatchCountLabel.ForeColor = System.Drawing.Color.Gray;
		this.m_MatchCountLabel.Location = new System.Drawing.Point(8, 46);
		this.m_MatchCountLabel.Name = "m_MatchCountLabel";
		this.m_MatchCountLabel.Size = new System.Drawing.Size(0, 13);
		this.m_MatchCountLabel.TabIndex = 5;
		this.m_PrevButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.m_PrevButton.Location = new System.Drawing.Point(226, 20);
		this.m_PrevButton.Name = "m_PrevButton";
		this.m_PrevButton.Size = new System.Drawing.Size(36, 23);
		this.m_PrevButton.TabIndex = 6;
		this.m_PrevButton.Text = "<";
		this.m_PrevButton.UseVisualStyleBackColor = true;
		this.m_PrevButton.Click += new System.EventHandler(FindPrevButtonClicked);
		this.m_FindLabel.AutoSize = true;
		this.m_FindLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_FindLabel.Location = new System.Drawing.Point(8, 46);
		this.m_FindLabel.Name = "m_FindLabel";
		this.m_FindLabel.Size = new System.Drawing.Size(30, 13);
		this.m_FindLabel.TabIndex = 7;
		this.m_FindLabel.Text = "Find";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_FindLabel);
		base.Controls.Add(this.m_PrevButton);
		base.Controls.Add(this.m_MatchCountLabel);
		base.Controls.Add(this.m_ClearButton);
		base.Controls.Add(this.m_NextButton);
		base.Controls.Add(this.m_TextBox);
		base.Name = "FindControl";
		base.Size = new System.Drawing.Size(307, 65);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
