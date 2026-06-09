using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

internal class ConnectSettingsDialog : Form
{
	private Settings m_Settings;

	private bool m_IgnoreNameChange;

	private IContainer components;

	private Label label1;

	private Label label2;

	private Button button1;

	private Button button2;

	private Label label3;

	private TextBox m_ConnectionNameTextBox;

	private ComboBox m_ConnectionDropDownBox;

	private CheckBox m_InteractiveCheckBox;

	private ComboBox m_PortTextBox;

	private ComboBox m_IPTextBox;

	private CheckBox m_RecordContextSwitchesTextBox;

	private CheckBox m_CollectCallstacksCheckBox;

	private TextBox textBox1;

	public ConnectSettingsDialog(Settings settings, bool new_connection)
	{
		InitializeComponent();
		m_Settings = settings;
		m_ConnectionDropDownBox.Items.Add("New Connection");
		foreach (Connection connection in m_Settings.Connections)
		{
			string item = connection.m_Name + "    " + connection.m_IP + "    " + connection.m_Port;
			m_ConnectionDropDownBox.Items.Add(item);
		}
		m_ConnectionDropDownBox.SelectedIndex = ((!new_connection && m_ConnectionDropDownBox.Items.Count > 1) ? 1 : 0);
		Connection currentConnection = m_Settings.GetCurrentConnection();
		m_IPTextBox.Text = currentConnection.m_IP;
		m_PortTextBox.Text = currentConnection.m_Port;
		m_InteractiveCheckBox.Checked = currentConnection.Interactive;
		m_RecordContextSwitchesTextBox.Checked = currentConnection.RecordConectSwitches;
	}

	private void ConnectButtonClicked(object sender, EventArgs e)
	{
		ConnectButtonClicked();
	}

	private void ConnectButtonClicked()
	{
		RememberConneection();
	}

	private void TextBoxKeyDown(object sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Return)
		{
			ConnectButtonClicked();
			base.DialogResult = DialogResult.OK;
			Close();
		}
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		if (keyData == Keys.Escape)
		{
			Close();
		}
		return base.ProcessDialogKey(keyData);
	}

	private void RememberConneection()
	{
		string text = m_ConnectionNameTextBox.Text.Trim();
		if (string.IsNullOrEmpty(text))
		{
			text = GenerateUniqueName();
		}
		Connection connection = FindConnection(text);
		if (connection == null)
		{
			connection = new Connection();
			connection.m_Name = text;
		}
		else
		{
			m_Settings.Connections.Remove(connection);
		}
		connection.m_IP = m_IPTextBox.Text;
		connection.m_Port = GetPortStringFromTextBox();
		connection.Interactive = m_InteractiveCheckBox.Checked;
		connection.RecordCallstacks = m_CollectCallstacksCheckBox.Checked;
		connection.RecordConectSwitches = m_RecordContextSwitchesTextBox.Checked;
		m_Settings.Connections.Insert(0, connection);
		m_Settings.Write();
	}

	private string GetPortStringFromTextBox()
	{
		string text = m_PortTextBox.Text;
		int num = text.IndexOf('(');
		if (num != -1)
		{
			text = text.Substring(0, num);
		}
		return text.Trim();
	}

	private string GenerateUniqueName()
	{
		string text = "Unnamed";
		int num = 1;
		while (FindConnection(text) != null)
		{
			text = "Unnamed " + num;
			num++;
		}
		return text;
	}

	private Connection FindConnection(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (Connection connection in m_Settings.Connections)
		{
			if (connection.m_Name == name)
			{
				return connection;
			}
		}
		return null;
	}

	private void ConnectionNameTextBoxTextChanged(object sender, EventArgs e)
	{
		if (!m_IgnoreNameChange)
		{
			m_ConnectionDropDownBox.SelectedIndex = 0;
		}
	}

	private void ConnectionDropDownSelectionChanged(object sender, EventArgs e)
	{
		if (m_ConnectionDropDownBox.SelectedIndex == 0)
		{
			m_ConnectionNameTextBox.Text = "";
		}
		else if (m_ConnectionDropDownBox.SelectedIndex > 0)
		{
			Connection connection = m_Settings.Connections[m_ConnectionDropDownBox.SelectedIndex - 1];
			m_IgnoreNameChange = true;
			m_ConnectionNameTextBox.Text = connection.m_Name;
			m_IgnoreNameChange = false;
			m_IPTextBox.Text = connection.m_IP.ToString();
			m_PortTextBox.Text = connection.m_Port.ToString();
			m_InteractiveCheckBox.Checked = connection.Interactive;
			m_CollectCallstacksCheckBox.Checked = connection.RecordCallstacks;
			m_RecordContextSwitchesTextBox.Checked = connection.RecordConectSwitches;
		}
	}

	private void IPTextBoxTextChanged(object sender, EventArgs e)
	{
		string text = m_IPTextBox.Text.ToLower().Trim();
		m_InteractiveCheckBox.Checked = !(text == "localhost") && (!(text == "127.0.0.1") || !m_Settings.DisableInteractiveSessionsForLocalProfiles);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerStudy.ConnectSettingsDialog));
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.button2 = new System.Windows.Forms.Button();
		this.label3 = new System.Windows.Forms.Label();
		this.m_ConnectionNameTextBox = new System.Windows.Forms.TextBox();
		this.m_ConnectionDropDownBox = new System.Windows.Forms.ComboBox();
		this.m_InteractiveCheckBox = new System.Windows.Forms.CheckBox();
		this.m_PortTextBox = new System.Windows.Forms.ComboBox();
		this.m_IPTextBox = new System.Windows.Forms.ComboBox();
		this.m_RecordContextSwitchesTextBox = new System.Windows.Forms.CheckBox();
		this.m_CollectCallstacksCheckBox = new System.Windows.Forms.CheckBox();
		this.textBox1 = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(88, 78);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(16, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "IP";
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(76, 103);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(28, 13);
		this.label2.TabIndex = 3;
		this.label2.Text = "Port";
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.button1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(306, 210);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(75, 23);
		this.button1.TabIndex = 4;
		this.button1.Text = "Cancel";
		this.button1.UseVisualStyleBackColor = true;
		this.button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.button2.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button2.Location = new System.Drawing.Point(198, 210);
		this.button2.Name = "button2";
		this.button2.Size = new System.Drawing.Size(102, 23);
		this.button2.TabIndex = 5;
		this.button2.Text = "Connect";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(ConnectButtonClicked);
		this.label3.AutoSize = true;
		this.label3.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label3.Location = new System.Drawing.Point(5, 52);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(99, 13);
		this.label3.TabIndex = 7;
		this.label3.Text = "Connection Name";
		this.m_ConnectionNameTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ConnectionNameTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ConnectionNameTextBox.Location = new System.Drawing.Point(110, 49);
		this.m_ConnectionNameTextBox.Name = "m_ConnectionNameTextBox";
		this.m_ConnectionNameTextBox.Size = new System.Drawing.Size(271, 22);
		this.m_ConnectionNameTextBox.TabIndex = 6;
		this.m_ConnectionNameTextBox.TextChanged += new System.EventHandler(ConnectionNameTextBoxTextChanged);
		this.m_ConnectionDropDownBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_ConnectionDropDownBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.m_ConnectionDropDownBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ConnectionDropDownBox.FormattingEnabled = true;
		this.m_ConnectionDropDownBox.Location = new System.Drawing.Point(12, 12);
		this.m_ConnectionDropDownBox.Name = "m_ConnectionDropDownBox";
		this.m_ConnectionDropDownBox.Size = new System.Drawing.Size(369, 21);
		this.m_ConnectionDropDownBox.TabIndex = 8;
		this.m_ConnectionDropDownBox.SelectedIndexChanged += new System.EventHandler(ConnectionDropDownSelectionChanged);
		this.m_InteractiveCheckBox.AutoSize = true;
		this.m_InteractiveCheckBox.Enabled = false;
		this.m_InteractiveCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_InteractiveCheckBox.Location = new System.Drawing.Point(190, 145);
		this.m_InteractiveCheckBox.Name = "m_InteractiveCheckBox";
		this.m_InteractiveCheckBox.Size = new System.Drawing.Size(79, 17);
		this.m_InteractiveCheckBox.TabIndex = 9;
		this.m_InteractiveCheckBox.Text = "Interactive";
		this.m_InteractiveCheckBox.UseVisualStyleBackColor = true;
		this.m_PortTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_PortTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_PortTextBox.FormattingEnabled = true;
		this.m_PortTextBox.Items.AddRange(new object[2] { "8428 (PC)", "4420 (XBox)" });
		this.m_PortTextBox.Location = new System.Drawing.Point(110, 100);
		this.m_PortTextBox.Name = "m_PortTextBox";
		this.m_PortTextBox.Size = new System.Drawing.Size(271, 21);
		this.m_PortTextBox.TabIndex = 10;
		this.m_IPTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.m_IPTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_IPTextBox.FormattingEnabled = true;
		this.m_IPTextBox.Items.AddRange(new object[1] { "localhost" });
		this.m_IPTextBox.Location = new System.Drawing.Point(110, 75);
		this.m_IPTextBox.Name = "m_IPTextBox";
		this.m_IPTextBox.Size = new System.Drawing.Size(271, 21);
		this.m_IPTextBox.TabIndex = 11;
		this.m_RecordContextSwitchesTextBox.AutoSize = true;
		this.m_RecordContextSwitchesTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_RecordContextSwitchesTextBox.Location = new System.Drawing.Point(190, 168);
		this.m_RecordContextSwitchesTextBox.Name = "m_RecordContextSwitchesTextBox";
		this.m_RecordContextSwitchesTextBox.Size = new System.Drawing.Size(143, 17);
		this.m_RecordContextSwitchesTextBox.TabIndex = 12;
		this.m_RecordContextSwitchesTextBox.Text = "Track Context Switches";
		this.m_RecordContextSwitchesTextBox.UseVisualStyleBackColor = true;
		this.m_CollectCallstacksCheckBox.AutoSize = true;
		this.m_CollectCallstacksCheckBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_CollectCallstacksCheckBox.Location = new System.Drawing.Point(12, 145);
		this.m_CollectCallstacksCheckBox.Name = "m_CollectCallstacksCheckBox";
		this.m_CollectCallstacksCheckBox.Size = new System.Drawing.Size(114, 17);
		this.m_CollectCallstacksCheckBox.TabIndex = 13;
		this.m_CollectCallstacksCheckBox.Text = "Collect Callstacks";
		this.m_CollectCallstacksCheckBox.UseVisualStyleBackColor = true;
		this.textBox1.BackColor = System.Drawing.SystemColors.Control;
		this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBox1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.textBox1.ForeColor = System.Drawing.Color.DarkGoldenrod;
		this.textBox1.Location = new System.Drawing.Point(12, 162);
		this.textBox1.Multiline = true;
		this.textBox1.Name = "textBox1";
		this.textBox1.ReadOnly = true;
		this.textBox1.Size = new System.Drawing.Size(142, 48);
		this.textBox1.TabIndex = 14;
		this.textBox1.Text = "Note: This can negatively affect performance in the app you are profiling";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(393, 245);
		base.Controls.Add(this.textBox1);
		base.Controls.Add(this.m_CollectCallstacksCheckBox);
		base.Controls.Add(this.m_RecordContextSwitchesTextBox);
		base.Controls.Add(this.m_InteractiveCheckBox);
		base.Controls.Add(this.m_IPTextBox);
		base.Controls.Add(this.m_PortTextBox);
		base.Controls.Add(this.m_ConnectionDropDownBox);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.m_ConnectionNameTextBox);
		base.Controls.Add(this.button2);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "ConnectSettingsDialog";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "Connect";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
