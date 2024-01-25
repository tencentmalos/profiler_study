using System;
using System.Drawing;
using System.Windows.Forms;

namespace Docker;

internal class DockOptionControl : Form
{
	public delegate void DockTargetActivatedHandler(bool mouse_over, Point mouse_pos, DockPanel moving_panel);

	private Control m_TargetPanel;

	private DockPanel m_MovingPanel;

	private Bitmap m_Bitmap;

	private Timer m_FadeInTimer;

	private double m_TargetOpacity = 1.0;

	private bool m_DockTargetActive;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ExStyle |= 128;
			return obj;
		}
	}

	public Control TargetPanel => m_TargetPanel;

	public double TargetOpacity
	{
		get
		{
			return m_TargetOpacity;
		}
		set
		{
			m_TargetOpacity = value;
			m_FadeInTimer.Enabled = true;
		}
	}

	public DockPanel MovingPanel => m_MovingPanel;

	public event DockTargetActivatedHandler DockTargetActivated;

	public DockOptionControl(DockPanel dock_panel, DockPanel moving_panel, Bitmap bitmap)
		: this(dock_panel, moving_panel, bitmap, null)
	{
	}

	public DockOptionControl(Control dock_panel, DockPanel moving_panel, Bitmap bitmap, Region region)
	{
		m_TargetPanel = dock_panel;
		m_MovingPanel = moving_panel;
		m_Bitmap = bitmap;
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		SetStyle(ControlStyles.Selectable, value: false);
		base.Enabled = false;
		base.Size = new Size(bitmap.Width, bitmap.Height);
		if (region != null)
		{
			base.Region = region;
		}
		base.Opacity = 0.0;
		m_FadeInTimer = new Timer();
		m_FadeInTimer.Tick += FadeInTimer;
		m_FadeInTimer.Interval = 10;
		m_FadeInTimer.Enabled = true;
	}

	protected override void Dispose(bool disposing)
	{
		m_FadeInTimer.Enabled = false;
		base.Dispose(disposing);
	}

	private void FadeInTimer(object sender, EventArgs e)
	{
		if (base.Opacity < m_TargetOpacity)
		{
			base.Opacity = Math.Min(base.Opacity + 0.08, m_TargetOpacity);
		}
		else if (base.Opacity > m_TargetOpacity)
		{
			base.Opacity = Math.Max(base.Opacity - 0.08, m_TargetOpacity);
		}
		else
		{
			m_FadeInTimer.Enabled = false;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.DrawImage(m_Bitmap, new Point(0, 0));
	}

	public void SetDockTargetActive(bool mouse_over, Point mouse_pos)
	{
		if ((mouse_over || m_DockTargetActive != mouse_over) && m_TargetOpacity != 0.0)
		{
			m_DockTargetActive = mouse_over;
			if (this.DockTargetActivated != null)
			{
				this.DockTargetActivated(mouse_over, mouse_pos, m_MovingPanel);
			}
		}
	}
}
