using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FramePro;

public class GetAndroidSDKPathForm : Form
{
	private IContainer components;

	private TextBox textBox1;

	private TextBox m_ADBPathTextBox;

	private Button button1;

	private Button button2;

	private Button m_OKButton;

	public string ADBPath => m_ADBPathTextBox.Text;

	public GetAndroidSDKPathForm()
	{
		InitializeComponent();
	}

	private void BrowseButtonClicked(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Files (*.exe)|*.exe|All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(this) == DialogResult.OK && File.Exists(openFileDialog.FileName))
		{
			m_ADBPathTextBox.Text = openFileDialog.FileName;
		}
	}

	private void PathTextBoxTextChanged(object sender, EventArgs e)
	{
		m_OKButton.Enabled = File.Exists(m_ADBPathTextBox.Text);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.GetAndroidSDKPathForm));
		this.textBox1 = new System.Windows.Forms.TextBox();
		this.m_ADBPathTextBox = new System.Windows.Forms.TextBox();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.m_OKButton = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBox1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBox1.Location = new System.Drawing.Point(12, 29);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.ReadOnly = true;
		this.textBox1.Size = new System.Drawing.Size(366, 59);
		this.textBox1.TabIndex = 0;
		this.textBox1.TabStop = false;
		this.textBox1.Text = "Please specify the path to the android ADB exe.\r\n\r\nThis can be found in the Android SDK folder in Platform-Tools\r\n";
		this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
		this.m_ADBPathTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ADBPathTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ADBPathTextBox.Location = new System.Drawing.Point(12, 94);
		this.m_ADBPathTextBox.Name = "m_ADBPathTextBox";
		this.m_ADBPathTextBox.Size = new System.Drawing.Size(320, 22);
		this.m_ADBPathTextBox.TabIndex = 2;
		this.m_ADBPathTextBox.TextChanged += new System.EventHandler(PathTextBoxTextChanged);
		this.button1.Location = new System.Drawing.Point(338, 94);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(40, 20);
		this.button1.TabIndex = 3;
		this.button1.Text = "...";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(BrowseButtonClicked);
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(303, 135);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(75, 23);
		this.button2.TabIndex = 1;
		this.button2.Text = "Cancel";
		this.button2.UseVisualStyleBackColor = true;
		this.m_OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.m_OKButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_OKButton.Location = new System.Drawing.Point(222, 135);
		this.m_OKButton.Name = "m_OKButton";
		this.m_OKButton.Size = new System.Drawing.Size(75, 23);
		this.m_OKButton.TabIndex = 0;
		this.m_OKButton.Text = "OK";
		this.m_OKButton.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(390, 170);
		base.Controls.Add(this.m_OKButton);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.m_ADBPathTextBox);
		base.Controls.Add(this.textBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "GetAndroidSDKPathForm";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Android SDK path needed";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
