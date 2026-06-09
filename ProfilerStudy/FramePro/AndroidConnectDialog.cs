using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

internal class AndroidConnectDialog : Form
{
	private ComboBox m_EndpointComboBox;
	private Button m_ConnectButton;
	private Button m_CancelButton;
	private Label m_EndpointLabel;
	private TextBox m_EndpointTextBox;
	private IContainer components;

	private class AndroidEndpointOption
	{
		public readonly string Name;
		public readonly string Endpoint;

		public AndroidEndpointOption(string name, string endpoint)
		{
			Name = name;
			Endpoint = endpoint;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public string SelectedEndpoint
	{
		get
		{
			AndroidEndpointOption option = m_EndpointComboBox.SelectedItem as AndroidEndpointOption;
			return option != null ? option.Endpoint : string.Empty;
		}
	}

	public AndroidConnectDialog()
	{
		InitializeComponent();
		m_EndpointComboBox.Items.Add(new AndroidEndpointOption("Debug", AdbSocketDiscovery.DebugProfilerStudyEndpoint));
		m_EndpointComboBox.Items.Add(new AndroidEndpointOption("Release", AdbSocketDiscovery.ReleaseProfilerStudyEndpoint));
		m_EndpointComboBox.SelectedIndex = 0;
	}

	private void EndpointSelectionChanged(object sender, EventArgs e)
	{
		m_EndpointTextBox.Text = SelectedEndpoint;
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
		components = new Container();
		m_EndpointLabel = new Label();
		m_EndpointComboBox = new ComboBox();
		m_EndpointTextBox = new TextBox();
		m_ConnectButton = new Button();
		m_CancelButton = new Button();
		SuspendLayout();

		m_EndpointLabel.AutoSize = true;
		m_EndpointLabel.Location = new Point(12, 16);
		m_EndpointLabel.Name = "m_EndpointLabel";
		m_EndpointLabel.Size = new Size(38, 13);
		m_EndpointLabel.Text = "Target";

		m_EndpointComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		m_EndpointComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		m_EndpointComboBox.FormattingEnabled = true;
		m_EndpointComboBox.Location = new Point(112, 12);
		m_EndpointComboBox.Name = "m_EndpointComboBox";
		m_EndpointComboBox.Size = new Size(560, 21);
		m_EndpointComboBox.TabIndex = 0;
		m_EndpointComboBox.SelectedIndexChanged += EndpointSelectionChanged;

		m_EndpointTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		m_EndpointTextBox.Location = new Point(112, 46);
		m_EndpointTextBox.Name = "m_EndpointTextBox";
		m_EndpointTextBox.ReadOnly = true;
		m_EndpointTextBox.Size = new Size(560, 20);
		m_EndpointTextBox.TabIndex = 1;

		m_ConnectButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		m_ConnectButton.DialogResult = DialogResult.OK;
		m_ConnectButton.Enabled = true;
		m_ConnectButton.Location = new Point(490, 92);
		m_ConnectButton.Name = "m_ConnectButton";
		m_ConnectButton.Size = new Size(90, 26);
		m_ConnectButton.TabIndex = 2;
		m_ConnectButton.Text = "Connect";
		m_ConnectButton.UseVisualStyleBackColor = true;

		m_CancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		m_CancelButton.DialogResult = DialogResult.Cancel;
		m_CancelButton.Location = new Point(586, 92);
		m_CancelButton.Name = "m_CancelButton";
		m_CancelButton.Size = new Size(86, 26);
		m_CancelButton.TabIndex = 3;
		m_CancelButton.Text = "Cancel";
		m_CancelButton.UseVisualStyleBackColor = true;

		AcceptButton = m_ConnectButton;
		CancelButton = m_CancelButton;
		AutoScaleDimensions = new SizeF(6F, 13F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(684, 130);
		Controls.Add(m_EndpointLabel);
		Controls.Add(m_EndpointComboBox);
		Controls.Add(m_EndpointTextBox);
		Controls.Add(m_ConnectButton);
		Controls.Add(m_CancelButton);
		MinimizeBox = false;
		Name = "AndroidConnectDialog";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Connect To Android";
		ResumeLayout(false);
		PerformLayout();
	}
}
