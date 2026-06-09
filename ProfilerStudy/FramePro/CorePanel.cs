using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProfilerStudy;

internal class CorePanel : UserControl
{
	private Session m_Session;

	private CoreGraph m_CoreGraph;

	private const int m_TextGapX = 8;

	private int m_CoreYGap = 20;

	private const int m_CoreRectHeight = 6;

	private Brush m_TextBrush = Brushes.White;

	private int m_ScrollY;

	private IContainer components;

	public int CoreYGap
	{
		get
		{
			return m_CoreYGap;
		}
		set
		{
			m_CoreYGap = value;
			Refresh();
		}
	}

	public static int CoreRectHeight => 6;

	public int ScrollY => m_ScrollY;

	public CorePanel()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		InitializeComponent();
	}

	public void SetSession(Session session)
	{
		m_Session = session;
	}

	public void SetCoreGraph(CoreGraph core_graph)
	{
		m_CoreGraph = core_graph;
		core_graph.CoreHeightsChanged += CoreHeightsChanged;
	}

	private void CoreHeightsChanged()
	{
		Refresh();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.Clear(BackColor);
		if (m_Session == null || !m_Session.IsReady)
		{
			return;
		}
		for (int i = 0; i < m_Session.CoreCount; i++)
		{
			Rectangle coreRect = m_CoreGraph.GetCoreRect(i);
			string s = "CORE " + i;
			if (m_CoreGraph.ShowHeirachy)
			{
				SizeF sizeF = e.Graphics.MeasureString(s, Font);
				int num = base.ClientSize.Width - (int)sizeF.Width - 8;
				int coreHeight = m_CoreGraph.GetCoreHeight(i);
				coreRect = new Rectangle(coreRect.X, coreRect.Y, coreRect.Width, coreHeight);
				int num2 = coreRect.Y + (coreRect.Height - Font.Height) / 2;
				e.Graphics.SetClip(coreRect);
				e.Graphics.DrawString(s, Font, m_TextBrush, num, num2);
				e.Graphics.ResetClip();
				e.Graphics.DrawLine(Pens.Black, 0, coreRect.Bottom, base.Width, coreRect.Bottom);
			}
			else
			{
				SizeF sizeF2 = e.Graphics.MeasureString(s, Font);
				int num3 = base.ClientSize.Width - (int)sizeF2.Width - 8;
				int num4 = coreRect.Y + (coreRect.Height - Font.Height) / 2;
				e.Graphics.DrawString(s, Font, m_TextBrush, num3, num4);
			}
		}
	}

	public void SetScrollY(int scroll_y)
	{
		if (m_ScrollY != scroll_y)
		{
			m_ScrollY = scroll_y;
			Refresh();
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
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.DimGray;
		this.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.ForeColor = System.Drawing.Color.White;
		base.Name = "CorePanel";
		base.ResumeLayout(false);
	}
}
