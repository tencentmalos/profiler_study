using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class ConditionalScopeTimeSlider : UserControl
{
	private IContainer components;

	private TrackBar m_Slider;

	private TextBox m_TextBox;

	private Label label1;

	private Label m_Label;

	public long Value => m_Slider.Value;

	public event ConditionalSliderChangedHandler ValueChanged;

	public ConditionalScopeTimeSlider()
	{
		InitializeComponent();
		m_Slider.MouseWheel += SliderMouseWheel;
		m_Label.ForeColor = Colours.ButtonText;
	}

	private void SliderMouseWheel(object sender, MouseEventArgs e)
	{
		((HandledMouseEventArgs)e).Handled = true;
	}

	public void Initialise(long min_time)
	{
		SetsliderValue(min_time);
	}

	private void SetsliderValue(long value)
	{
		if (m_Slider.Value != value)
		{
			m_Slider.Minimum = (int)Math.Max(0L, value - 50);
			m_Slider.Maximum = (int)Math.Max(100L, value + 50);
			m_Slider.Value = (int)value;
		}
	}

	private void SliderValueChanged(object sender, EventArgs e)
	{
		string text = m_Slider.Value.ToString();
		if (m_TextBox.Text != text)
		{
			m_TextBox.Text = text;
		}
		if (this.ValueChanged != null)
		{
			this.ValueChanged(Value);
		}
	}

	private void TextBoxTextChnged(object sender, EventArgs e)
	{
		long value = m_Slider.Value;
		try
		{
			value = Convert.ToInt64(m_TextBox.Text);
		}
		catch (Exception)
		{
		}
		SetsliderValue(value);
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
		this.m_Slider = new System.Windows.Forms.TrackBar();
		this.m_TextBox = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		this.m_Label = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)this.m_Slider).BeginInit();
		base.SuspendLayout();
		this.m_Slider.LargeChange = 10;
		this.m_Slider.Location = new System.Drawing.Point(9, 3);
		this.m_Slider.Maximum = 100;
		this.m_Slider.Name = "m_Slider";
		this.m_Slider.Size = new System.Drawing.Size(180, 45);
		this.m_Slider.TabIndex = 1;
		this.m_Slider.TabStop = false;
		this.m_Slider.TickFrequency = 10;
		this.m_Slider.ValueChanged += new System.EventHandler(SliderValueChanged);
		this.m_TextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TextBox.Location = new System.Drawing.Point(116, 38);
		this.m_TextBox.Name = "m_TextBox";
		this.m_TextBox.Size = new System.Drawing.Size(40, 22);
		this.m_TextBox.TabIndex = 2;
		this.m_TextBox.TabStop = false;
		this.m_TextBox.TextChanged += new System.EventHandler(TextBoxTextChnged);
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(162, 41);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(19, 13);
		this.label1.TabIndex = 3;
		this.label1.Text = "μs";
		this.m_Label.AutoSize = true;
		this.m_Label.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_Label.Location = new System.Drawing.Point(17, 41);
		this.m_Label.Name = "m_Label";
		this.m_Label.Size = new System.Drawing.Size(93, 13);
		this.m_Label.TabIndex = 4;
		this.m_Label.Text = "Scope Threshold";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_Label);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.m_TextBox);
		base.Controls.Add(this.m_Slider);
		base.Name = "ConditionalScopeTimeSlider";
		base.Size = new System.Drawing.Size(202, 65);
		((System.ComponentModel.ISupportInitialize)this.m_Slider).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
