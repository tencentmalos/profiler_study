using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class CustomStatsGraph : UserControl
{
	private Session m_Session;

	private Settings m_Settings;

	private List<Graph> m_Graphs = new List<Graph>();

	private IContainer components;

	private LineGraph m_LineGraph;

	public double YScale
	{
		get
		{
			return m_LineGraph.YScale;
		}
		set
		{
			m_LineGraph.YScale = value;
		}
	}

	public bool NeedsUpdate => m_LineGraph.NeedsUpdate;

	public event ValueGraphTimeRangeChangedHandler TimeRangeChanged;

	public CustomStatsGraph()
	{
		InitializeComponent();
		m_LineGraph.GetGraphValuesPerFrameFunction = GetGraphValuesPerFrameFunction;
		m_LineGraph.DrawAxis = false;
		m_LineGraph.AlignScale = true;
		m_LineGraph.SetFillBackgroundColour(Colours.CustomStatGraphBackground);
		m_LineGraph.TimeRangeChanged += LineGraphTimeRangeChanged;
	}

	public void Initialise(Session session, Settings settings)
	{
		m_Session = session;
		m_Settings = settings;
		m_LineGraph.YScale = settings.ScopeGraphYScale;
		m_LineGraph.SetSession(session);
		m_LineGraph.SetSettings(settings);
	}

	public void RecalculateView()
	{
		m_LineGraph.RecalculateView();
	}

	public void SetCustomStats(List<long> names)
	{
		m_Graphs = new List<Graph>();
		foreach (long name in names)
		{
			Graph item = new Graph(m_Session.GetString(name), name, m_Session.GetCustomStatColour(name), m_Session.GetCustomStatGraph(name), m_Session.GetCustomStatUnit(name), XAxisMode.Frame);
			m_Graphs.Add(item);
		}
		m_LineGraph.SetGraphs(m_Graphs);
		foreach (Graph graph in m_Graphs)
		{
			if (graph.m_Unit != "cycles")
			{
				m_LineGraph.SetUnitScaleToHeight(graph.m_Unit, value: true);
			}
		}
	}

	public void SetSessionIsReady()
	{
		m_LineGraph.SetSessionIsReady();
	}

	public void SetRange(long start_frame_x, double frame_x_scale)
	{
		m_LineGraph.SetTimeRange(start_frame_x, frame_x_scale);
	}

	public void SetTimeRange(long start_time, long end_time)
	{
		long start = m_Session.TimeToFrameX(start_time);
		long end = m_Session.TimeToFrameX(end_time);
		m_LineGraph.SetTimeRange(start, end);
	}

	private void GetGraphValuesPerFrameFunction(int start_frame_index, int end_frame_index, long graph_name, List<FrameValue> values)
	{
		string @string = m_Session.GetString(graph_name);
		CustomStatXAxisMode xAxisMode = m_Settings.CoreSettings.GetCustomStatInfo(@string).m_XAxisMode;
		bool acc = xAxisMode == CustomStatXAxisMode.AccFrame || xAxisMode == CustomStatXAxisMode.AccTime;
		m_Session.GetCustomStats(start_frame_index, end_frame_index, graph_name, acc, values);
	}

	private void LineGraphTimeRangeChanged(Control sender, long start_frame_x, long end_frame_x, double frame_x_scale)
	{
		if (this.TimeRangeChanged != null)
		{
			this.TimeRangeChanged(sender, start_frame_x, end_frame_x, frame_x_scale);
		}
	}

	public void OnMouseWheel(int delta, Point location)
	{
		m_LineGraph.OnMouseWheel(delta, location);
	}

	public void SetGraphColour(string name, Color colour)
	{
		foreach (Graph graph in m_Graphs)
		{
			if (graph.m_StatName == name)
			{
				graph.m_Colour = colour;
				break;
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
		this.m_LineGraph = new FramePro.LineGraph();
		base.SuspendLayout();
		this.m_LineGraph.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_LineGraph.GetGraphValuesPerFrameFunction = null;
		this.m_LineGraph.GetGraphValuesPerSecFunction = null;
		this.m_LineGraph.Location = new System.Drawing.Point(0, 0);
		this.m_LineGraph.Name = "m_LineGraph";
		this.m_LineGraph.Size = new System.Drawing.Size(1057, 84);
		this.m_LineGraph.TabIndex = 0;
		this.m_LineGraph.Unit = "";
		this.m_LineGraph.XAxisMode = FramePro.XAxisMode.Frame;
		this.m_LineGraph.YOffset = 0.0;
		this.m_LineGraph.YScale = 1.0;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.m_LineGraph);
		base.Name = "CustomStatsGraph";
		base.Size = new System.Drawing.Size(1057, 84);
		base.ResumeLayout(false);
	}
}
