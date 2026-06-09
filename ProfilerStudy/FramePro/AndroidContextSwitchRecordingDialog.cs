using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

public class AndroidContextSwitchRecordingDialog : Form
{
	private IContainer components;

	private Button button1;

	private Button button2;

	private TextBox m_DurationTextBox;

	private Label label1;

	public int Duration
	{
		get
		{
			try
			{
				return Convert.ToInt32(m_DurationTextBox.Text);
			}
			catch (Exception)
			{
				return 5;
			}
		}
	}

	public AndroidContextSwitchRecordingDialog(int duration)
	{
		InitializeComponent();
		m_DurationTextBox.Text = duration.ToString();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerStudy.AndroidContextSwitchRecordingDialog));
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.m_DurationTextBox = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(366, 72);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 1;
		this.button1.Text = "Cancel";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(285, 72);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(75, 23);
		this.button2.TabIndex = 0;
		this.button2.Text = "Start";
		this.button2.UseVisualStyleBackColor = true;
		this.m_DurationTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DurationTextBox.Location = new System.Drawing.Point(154, 27);
		this.m_DurationTextBox.Name = "m_DurationTextBox";
		this.m_DurationTextBox.Size = new System.Drawing.Size(100, 22);
		this.m_DurationTextBox.TabIndex = 2;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(44, 30);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(104, 13);
		this.label1.TabIndex = 3;
		this.label1.Text = "Duration (seconds)";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(453, 107);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.m_DurationTextBox);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "AndroidContextSwitchRecordingDialog";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Start Recording Context Switches";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
