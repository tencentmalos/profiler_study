using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class SaveOnExitDialog : Form
{
	private Settings m_Settings;

	private IContainer components;

	private Label label1;

	private Button button1;

	private Button button2;

	private CheckBox m_DontAskAgainCheckBox;

	public SaveOnExitDialog(Settings settings)
	{
		InitializeComponent();
		m_Settings = settings;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (m_DontAskAgainCheckBox.Checked)
		{
			m_Settings.SaveChangedQuery = false;
			m_Settings.Write();
		}
		base.OnClosing(e);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.SaveOnExitDialog));
		this.label1 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.m_DontAskAgainCheckBox = new System.Windows.Forms.CheckBox();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 14.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(29, 18);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(237, 25);
		this.label1.TabIndex = 0;
		this.label1.Text = "You have unsaved sessions";
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.No;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(157, 51);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(139, 29);
		this.button1.TabIndex = 2;
		this.button1.Text = "Exit Without Saving";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.Yes;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(12, 51);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(139, 29);
		this.button2.TabIndex = 3;
		this.button2.Text = "Save Session";
		this.button2.UseVisualStyleBackColor = true;
		this.m_DontAskAgainCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_DontAskAgainCheckBox.AutoSize = true;
		this.m_DontAskAgainCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DontAskAgainCheckBox.Location = new System.Drawing.Point(12, 86);
		this.m_DontAskAgainCheckBox.Name = "m_DontAskAgainCheckBox";
		this.m_DontAskAgainCheckBox.Size = new System.Drawing.Size(107, 17);
		this.m_DontAskAgainCheckBox.TabIndex = 4;
		this.m_DontAskAgainCheckBox.Text = "Don't ask again";
		this.m_DontAskAgainCheckBox.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(308, 106);
		base.Controls.Add(this.m_DontAskAgainCheckBox);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "SaveOnExitDialog";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Save Session?";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
