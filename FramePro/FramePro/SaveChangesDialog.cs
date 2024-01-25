using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

public class SaveChangesDialog : Form
{
	private IContainer components;

	private Button button1;

	private Button button2;

	private Button button3;

	private CheckBox m_DontAskAgainCheckBox;

	private Label label1;

	public bool DontAskAgain => m_DontAskAgainCheckBox.Checked;

	public SaveChangesDialog()
	{
		InitializeComponent();
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.SaveChangesDialog));
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.button3 = new System.Windows.Forms.Button();
		this.m_DontAskAgainCheckBox = new System.Windows.Forms.CheckBox();
		this.label1 = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(297, 80);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 3;
		this.button1.Text = "Cancel";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.Yes;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(135, 80);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(75, 23);
		this.button2.TabIndex = 1;
		this.button2.Text = "Yes";
		this.button2.UseVisualStyleBackColor = true;
		this.button3.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button3.DialogResult = System.Windows.Forms.DialogResult.No;
		this.button3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button3.Location = new System.Drawing.Point(216, 80);
		this.button3.Name = "button3";
		this.button3.Size = new System.Drawing.Size(75, 23);
		this.button3.TabIndex = 2;
		this.button3.Text = "No";
		this.button3.UseVisualStyleBackColor = true;
		this.m_DontAskAgainCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_DontAskAgainCheckBox.AutoSize = true;
		this.m_DontAskAgainCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DontAskAgainCheckBox.Location = new System.Drawing.Point(12, 86);
		this.m_DontAskAgainCheckBox.Name = "m_DontAskAgainCheckBox";
		this.m_DontAskAgainCheckBox.Size = new System.Drawing.Size(107, 17);
		this.m_DontAskAgainCheckBox.TabIndex = 4;
		this.m_DontAskAgainCheckBox.Text = "Don't ask again";
		this.m_DontAskAgainCheckBox.UseVisualStyleBackColor = true;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(150, 34);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(83, 13);
		this.label1.TabIndex = 4;
		this.label1.Text = "Save Changes?";
		base.AcceptButton = this.button2;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(384, 115);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.m_DontAskAgainCheckBox);
		base.Controls.Add(this.button3);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "SaveChangesDialog";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "FramePro";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
