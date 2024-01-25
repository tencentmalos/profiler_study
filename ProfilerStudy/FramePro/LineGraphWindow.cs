using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class LineGraphWindow : UserControl
{
	private string m_GraphName;

	private string m_Unit;

	private List<Label> m_KeyLabels = new List<Label>();

	private List<Panel> m_KeyPanels = new List<Panel>();

	private const int m_KeyLabelGap = 20;

	private const int m_KeyPanelGap = 8;

	private Size m_KeyPanelSize = new Size(10, 10);

	private IContainer components;

	private Panel m_TitlePanel;

	private Label m_TitleLabel;

	private Button button1;

	private LineGraph m_Graph;

	public XAxisMode XAxisMode
	{
		get
		{
			return m_Graph.XAxisMode;
		}
		set
		{
			m_Graph.XAxisMode = value;
			UpdateTitleLabel();
		}
	}

	public long XStart => m_Graph.XStart;

	public double XScale => m_Graph.XScale;

	public string Unit => m_Unit;

	public string Graph => m_GraphName;

	public ICollection<Graph> Graphs => m_Graph.Graphs;

	public double YScale
	{
		get
		{
			return m_Graph.YScale;
		}
		set
		{
			m_Graph.YScale = value;
		}
	}

	public bool ChangedHeight => m_Graph.ChangedHeight;

	public double YOffset
	{
		get
		{
			return m_Graph.YOffset;
		}
		set
		{
			m_Graph.YOffset = value;
		}
	}

	public int GraphHeight => m_Graph.GraphHeight;

	public event StopTrackingEndHandler StopTrackingEnd;

	public event ValueGraphTimeRangeChangedHandler TimeRangeChanged;

	public event ValuePerSecGraphWindowCloseHandler FrameGraphWindowClose;

	public LineGraphWindow(string graph_name, string unit, Session session, Settings settings, GetGraphValuesPerFrameFunction get_values_per_frame_callback, GetGraphValuesPerSecFunction get_values_per_sec_callback)
	{
		InitializeComponent();
		m_GraphName = graph_name;
		m_Unit = unit;
		m_Graph.SetSession(session);
		m_Graph.SetSettings(settings);
		m_Graph.GetGraphValuesPerFrameFunction = get_values_per_frame_callback;
		m_Graph.GetGraphValuesPerSecFunction = get_values_per_sec_callback;
		m_Graph.Unit = unit;
		UpdateTitleLabel();
		m_Graph.StopTrackingEnd += StopTrackingEndEvent;
		m_Graph.TimeRangeChanged += OnTimeRangeChangedEvent;
		m_Graph.DisplayUnitChanged += UnitToDisplayChanged;
		m_Graph.SetSessionIsReady();
	}

	private void UpdateTitleLabel()
	{
		m_TitleLabel.Text = m_GraphName + "    Unit: " + m_Graph.UnitToDisplay;
		UpdateKeyLabels();
	}

	private void UnitToDisplayChanged()
	{
		UpdateTitleLabel();
	}

	private void OnTimeRangeChangedEvent(Control sender, long start_time, long end_time, double scale)
	{
		if (this.TimeRangeChanged != null)
		{
			this.TimeRangeChanged(this, start_time, end_time, scale);
		}
	}

	public void SetTimeRange(long start_time, double x_scale)
	{
		m_Graph.SetTimeRange(start_time, x_scale);
	}

	private void StopTrackingEndEvent()
	{
		this.StopTrackingEnd();
	}

	public void GotoEnd()
	{
		m_Graph.GotoEnd();
	}

	public void RefreshGraph()
	{
		m_Graph.RefreshGraph();
	}

	public void SetGraphs(List<Graph> graphs)
	{
		if (m_Graph.SetGraphs(graphs))
		{
			UpdateKeyLabels();
		}
	}

	public void OnMouseWheel(int delta, Point location)
	{
		m_Graph.OnMouseWheel(delta, location);
	}

	private void CloseButtonClicked(object sender, EventArgs e)
	{
		if (this.FrameGraphWindowClose != null)
		{
			this.FrameGraphWindowClose(this);
		}
	}

	private void UpdateKeyLabels()
	{
		foreach (Label keyLabel in m_KeyLabels)
		{
			m_TitlePanel.Controls.Remove(keyLabel);
		}
		foreach (Panel keyPanel in m_KeyPanels)
		{
			m_TitlePanel.Controls.Remove(keyPanel);
		}
		int num = m_TitleLabel.Location.X + m_TitleLabel.Width + 20;
		int num2 = m_TitleLabel.Location.Y;
		foreach (Graph graph in m_Graph.Graphs)
		{
			Label label = new Label();
			string text = "";
			switch (graph.m_XAxisMode)
			{
			case XAxisMode.Frame:
				text = "Frame";
				break;
			case XAxisMode.Time:
				text = "Sec";
				break;
			}
			Panel panel = new Panel();
			panel.BorderStyle = BorderStyle.FixedSingle;
			panel.BackColor = graph.m_Colour;
			panel.Location = new Point(num, num2 + (m_TitleLabel.Height - m_KeyPanelSize.Height) / 2);
			panel.Size = m_KeyPanelSize;
			m_TitlePanel.Controls.Add(panel);
			m_KeyPanels.Add(panel);
			num += m_KeyPanelSize.Width + 8;
			label.Text = graph.m_StatName + "   (" + m_Graph.UnitToDisplay + "/" + text + ")";
			label.Location = new Point(num, num2);
			label.AutoSize = true;
			m_TitlePanel.Controls.Add(label);
			m_KeyLabels.Add(label);
			num += label.Width + 20;
		}
	}

	public void ResetXAxisZoom()
	{
		m_Graph.ResetXAxisZoom();
	}

	public void OnScopeColourChanged(GetLineColourDelegate get_line_colour_delegate)
	{
		m_Graph.OnScopeColourChanged(get_line_colour_delegate);
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
		this.m_TitlePanel = new System.Windows.Forms.Panel();
		this.m_TitleLabel = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.m_Graph = new FramePro.LineGraph();
		this.m_TitlePanel.SuspendLayout();
		base.SuspendLayout();
		this.m_TitlePanel.BackColor = System.Drawing.Color.Gainsboro;
		this.m_TitlePanel.Controls.Add(this.m_TitleLabel);
		this.m_TitlePanel.Controls.Add(this.button1);
		this.m_TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
		this.m_TitlePanel.Location = new System.Drawing.Point(0, 0);
		this.m_TitlePanel.Name = "m_TitlePanel";
		this.m_TitlePanel.Size = new System.Drawing.Size(1100, 25);
		this.m_TitlePanel.TabIndex = 1;
		this.m_TitleLabel.AutoSize = true;
		this.m_TitleLabel.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TitleLabel.Location = new System.Drawing.Point(7, 7);
		this.m_TitleLabel.Name = "m_TitleLabel";
		this.m_TitleLabel.Size = new System.Drawing.Size(29, 13);
		this.m_TitleLabel.TabIndex = 1;
		this.m_TitleLabel.Text = "Title";
		this.button1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
		this.button1.Location = new System.Drawing.Point(1073, 0);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(24, 23);
		this.button1.TabIndex = 0;
		this.button1.Text = "X";
		this.button1.UseVisualStyleBackColor = true;
		this.m_Graph.AlignScale = false;
		this.m_Graph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_Graph.DrawAxis = true;
		this.m_Graph.GetGraphValuesPerFrameFunction = null;
		this.m_Graph.GetGraphValuesPerSecFunction = null;
		this.m_Graph.Location = new System.Drawing.Point(0, 25);
		this.m_Graph.Name = "m_Graph";
		this.m_Graph.Size = new System.Drawing.Size(1100, 238);
		this.m_Graph.TabIndex = 2;
		this.m_Graph.Unit = null;
		this.m_Graph.XAxisMode = FramePro.XAxisMode.Frame;
		this.m_Graph.YOffset = 0.0;
		this.m_Graph.YScale = 1.0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_Graph);
		base.Controls.Add(this.m_TitlePanel);
		base.Name = "LineGraphWindow";
		base.Size = new System.Drawing.Size(1100, 263);
		this.m_TitlePanel.ResumeLayout(false);
		this.m_TitlePanel.PerformLayout();
		base.ResumeLayout(false);
	}
}
