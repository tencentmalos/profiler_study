using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
////using Registration;

namespace FramePro;

internal class AboutDialog : Form
{
	private IContainer components;

	private Label label8;

	private Label label9;

	private Label label7;

	private Label label6;

	private Label label5;

	private Label label4;

	private Label m_VersionLabel;

	private Label label3;

	private Label label2;

	private Label label1;

	private PictureBox pictureBox1;

	private Button button1;

	private PictureBox pictureBox2;

	private Label label10;

	private Label m_LicenseLabel;

	public AboutDialog(Settings settings)
	{
		InitializeComponent();
		m_VersionLabel.Text = Utils.TrimVersionString(FrameProCore.Version);
		bool registered = true;
		////if (settings.RegisterUsingPureDevReg)
		////{
		////	registered = FrameProCore.Registrar.Registered;
		////	flag = true;
		////	num = FrameProCore.Registrar.TrialDaysLeft;
		////}
		////else
		////{
		////	registered = global::Registration.Registration.Registered;
		////	flag = Demo.DaysLeftValid;
		////	num = Demo.DaysLeft;
		////}
		if (registered)
		{
			m_LicenseLabel.Text = "Ñ§Ï°°æ-½ûÖ¹´«²¥";
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.AboutDialog));
		this.label8 = new System.Windows.Forms.Label();
		this.label9 = new System.Windows.Forms.Label();
		this.label7 = new System.Windows.Forms.Label();
		this.label6 = new System.Windows.Forms.Label();
		this.label5 = new System.Windows.Forms.Label();
		this.label4 = new System.Windows.Forms.Label();
		this.m_VersionLabel = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.pictureBox2 = new System.Windows.Forms.PictureBox();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.label10 = new System.Windows.Forms.Label();
		this.m_LicenseLabel = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.label8.AutoSize = true;
		this.label8.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label8.Location = new System.Drawing.Point(162, 321);
		this.label8.Name = "label8";
		this.label8.Size = new System.Drawing.Size(161, 13);
		this.label8.TabIndex = 23;
		this.label8.Text = "slynch@puredevsoftware.com";
		this.label9.AutoSize = true;
		this.label9.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label9.Location = new System.Drawing.Point(34, 321);
		this.label9.Name = "label9";
		this.label9.Size = new System.Drawing.Size(41, 13);
		this.label9.TabIndex = 22;
		this.label9.Text = "e-mail:";
		this.label7.AutoSize = true;
		this.label7.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label7.Location = new System.Drawing.Point(162, 293);
		this.label7.Name = "label7";
		this.label7.Size = new System.Drawing.Size(148, 13);
		this.label7.TabIndex = 21;
		this.label7.Text = "www.puredevsoftware.com";
		this.label6.AutoSize = true;
		this.label6.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label6.Location = new System.Drawing.Point(34, 293);
		this.label6.Name = "label6";
		this.label6.Size = new System.Drawing.Size(52, 13);
		this.label6.TabIndex = 20;
		this.label6.Text = "Website:";
		this.label5.AutoSize = true;
		this.label5.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label5.Location = new System.Drawing.Point(162, 265);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(138, 13);
		this.label5.TabIndex = 19;
		this.label5.Text = "PureDev Software Limited";
		this.label4.AutoSize = true;
		this.label4.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label4.Location = new System.Drawing.Point(162, 237);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(138, 13);
		this.label4.TabIndex = 18;
		this.label4.Text = "PureDev Software Limited";
		this.m_VersionLabel.AutoSize = true;
		this.m_VersionLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_VersionLabel.Location = new System.Drawing.Point(162, 209);
		this.m_VersionLabel.Name = "m_VersionLabel";
		this.m_VersionLabel.Size = new System.Drawing.Size(0, 13);
		this.m_VersionLabel.TabIndex = 17;
		this.label3.AutoSize = true;
		this.label3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.Location = new System.Drawing.Point(34, 265);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(90, 13);
		this.label3.TabIndex = 16;
		this.label3.Text = "Company Name:";
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(34, 237);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(61, 13);
		this.label2.TabIndex = 15;
		this.label2.Text = "Copyright:";
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(34, 209);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(48, 13);
		this.label1.TabIndex = 14;
		this.label1.Text = "Version:";
		this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(352, 378);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 26;
		this.button1.Text = "Close";
		this.button1.UseVisualStyleBackColor = true;
		this.pictureBox2.Image = (System.Drawing.Image)resources.GetObject("pictureBox2.Image");
		this.pictureBox2.Location = new System.Drawing.Point(37, 12);
		this.pictureBox2.Name = "pictureBox2";
		this.pictureBox2.Size = new System.Drawing.Size(123, 145);
		this.pictureBox2.TabIndex = 27;
		this.pictureBox2.TabStop = false;
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(315, 12);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(112, 55);
		this.pictureBox1.TabIndex = 25;
		this.pictureBox1.TabStop = false;
		this.label10.AutoSize = true;
		this.label10.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label10.Location = new System.Drawing.Point(34, 349);
		this.label10.Name = "label10";
		this.label10.Size = new System.Drawing.Size(44, 13);
		this.label10.TabIndex = 28;
		this.label10.Text = "License";
		this.m_LicenseLabel.AutoSize = true;
		this.m_LicenseLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_LicenseLabel.Location = new System.Drawing.Point(162, 349);
		this.m_LicenseLabel.Name = "m_LicenseLabel";
		this.m_LicenseLabel.Size = new System.Drawing.Size(188, 13);
		this.m_LicenseLabel.TabIndex = 29;
		this.m_LicenseLabel.Text = "This product has not been licensed";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(439, 413);
		base.Controls.Add(this.m_LicenseLabel);
		base.Controls.Add(this.label10);
		base.Controls.Add(this.pictureBox2);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.label8);
		base.Controls.Add(this.label9);
		base.Controls.Add(this.label7);
		base.Controls.Add(this.label6);
		base.Controls.Add(this.label5);
		base.Controls.Add(this.label4);
		base.Controls.Add(this.m_VersionLabel);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "AboutDialog";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "About";
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).EndInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
