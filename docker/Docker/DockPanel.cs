using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class DockPanel : Control
{
	private DockManager m_DockManager;

	private TabbedPanel m_TabbedPanel;

	private DockPanel m_DockPanel1;

	private DockPanel m_DockPanel2;

	private SplitMode m_SplitMode;

	private bool m_DockOptionsActive;

	private DockOptionControl m_DockOptionCentre;

	private DockOptionControl m_DockOptionTop;

	private DockOptionControl m_DockOptionBottom;

	private DockOptionControl m_DockOptionLeft;

	private DockOptionControl m_DockOptionRight;

	private static Region m_DockOptionCentreRegion = Utils.BitmapToRegion(Resource.DockOptionCentre, Color.FromArgb(255, 255, 0, 255));

	private DockStyle m_TargetDockStyle;

	private Splitter m_Splitter;

	private int m_UniqueID;

	private static int m_NextUniqueID = 1;

	private static Set<int> m_UniqueIDSet = new Set<int>();

	private const int m_MinSplitSize = 20;

	private const int m_MDITargetPanelSize = 150;

	private const float m_MinDockoptionControlOpacity = 0.7f;

	private int m_DockedParentUniqueID;

	private DockStyle m_DockedStyle;

	private Point m_FloatingLocation = Point.Empty;

	private Size m_FloatingSize;

	private const int m_MDITargetPanelExpand = 10;

	private Size m_CurSize;

	private bool m_DisableChildDocking;

	private bool MaintainSplitRatios
	{
		get
		{
			if (m_TabbedPanel != null)
			{
				return m_TabbedPanel.MaintainSplitRatios;
			}
			if (m_DockPanel1.MaintainSplitRatios)
			{
				return true;
			}
			if (m_DockPanel2.MaintainSplitRatios)
			{
				return true;
			}
			return false;
		}
	}

	public DockStyle TargetDockStyle => m_TargetDockStyle;

	private bool CanResize
	{
		get
		{
			if (m_TabbedPanel == null)
			{
				return true;
			}
			return m_TabbedPanel.CanResize;
		}
	}

	public string ActivePanelName
	{
		get
		{
			if (m_TabbedPanel != null && m_TabbedPanel.ActiveControlName != null)
			{
				return m_TabbedPanel.TabbedControls[0].Text;
			}
			return m_DockPanel1.ActivePanelName;
		}
	}

	public Control Control
	{
		get
		{
			if (m_TabbedPanel == null)
			{
				return null;
			}
			return m_TabbedPanel.ActiveControl;
		}
	}

	public DockManager DockManager => m_DockManager;

	public int DockedParentUniqueID => m_DockedParentUniqueID;

	public DockStyle DockStyle => m_DockedStyle;

	public SplitMode SplitMode => m_SplitMode;

	public DockPanel DockPanel1 => m_DockPanel1;

	public DockPanel DockPanel2 => m_DockPanel2;

	public int TabCount
	{
		get
		{
			if (m_TabbedPanel == null)
			{
				return 0;
			}
			return m_TabbedPanel.TabCount;
		}
	}

	public int UniqueID => m_UniqueID;

	public Point FloatingLocation => m_FloatingLocation;

	public Size FloatingSize => m_FloatingSize;

	public bool DisableChildDocking
	{
		get
		{
			return m_DisableChildDocking;
		}
		set
		{
			m_DisableChildDocking = value;
		}
	}

	public DockPanel(DockManager dock_manager)
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		m_DockManager = dock_manager;
		m_UniqueID = GenerateUniqueID();
		m_CurSize = base.Size;
	}

	protected override void OnResize(EventArgs eventargs)
	{
		if (m_SplitMode != 0 && MaintainSplitRatios)
		{
			if (m_SplitMode == SplitMode.Horz)
			{
				SetSplitSize(base.Height * m_DockPanel1.Height / m_CurSize.Height);
			}
			else
			{
				SetSplitSize(base.Width * m_DockPanel1.Width / m_CurSize.Width);
			}
		}
		m_CurSize = base.Size;
		base.OnResize(eventargs);
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		if (base.Parent != null)
		{
			_ = base.Handle;
			BeginInvoke((MethodInvoker)delegate
			{
				base.OnSizeChanged(e);
			});
		}
		else
		{
			base.OnSizeChanged(e);
		}
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	public void AddControl(Control control)
	{
		m_FloatingSize = control.Size;
		if (m_TabbedPanel == null)
		{
			SetTabbedPanel(new TabbedPanel());
		}
		m_TabbedPanel.AddControl(control);
	}

	private void SetTabbedPanel(TabbedPanel tabbed_panel)
	{
		if (tabbed_panel != null)
		{
			tabbed_panel.Dock = DockStyle.Fill;
			tabbed_panel.ControlTextChanged += ControlTextChangedEvent;
			base.Controls.Add(tabbed_panel);
		}
		else
		{
			m_TabbedPanel.ControlTextChanged -= ControlTextChangedEvent;
			base.Controls.Remove(m_TabbedPanel);
		}
		m_TabbedPanel = tabbed_panel;
		if (tabbed_panel != null)
		{
			UpdateParentTitleText();
		}
	}

	private void ControlTextChangedEvent(Control control)
	{
		m_DockManager.OnControlTextChanged(control);
	}

	protected override void OnParentChanged(EventArgs e)
	{
		if (m_TabbedPanel != null)
		{
			m_TabbedPanel.UpdateTitleBarVisibility();
		}
		base.OnParentChanged(e);
	}

	public void OnLastTabClosed()
	{
		if (base.Parent is DockPanel)
		{
			((DockPanel)base.Parent).Remove(this);
		}
		else if (base.Parent is MDIForm mdi_form)
		{
			DockManager.TryCloseMDIForm(mdi_form);
		}
		else
		{
			(base.Parent as FloatingForm).Close();
		}
	}

	public void CaptionDragged()
	{
		m_DockManager.FloatAndDragPanel(this);
	}

	public void ShowDockOptions(bool b, DockPanel moving_panel)
	{
		if (m_DockOptionsActive == b)
		{
			return;
		}
		m_DockOptionsActive = b;
		if (b)
		{
			Point pos = default(Point);
			Point point = PointToScreen(new Point(0, 0));
			bool flag = m_TabbedPanel != null && (moving_panel.Parent is FloatingForm || !(Control is MDIPanel));
			if (m_TabbedPanel != null && m_TabbedPanel.ContainsControl(typeof(MDIPanel)))
			{
				flag = false;
			}
			if (flag)
			{
				m_DockOptionCentre = new DockOptionControl(this, moving_panel, Resource.DockOptionCentre, m_DockOptionCentreRegion);
				m_DockOptionCentre.DockTargetActivated += CentreDockOptionActivatEvent;
				pos.X = point.X + (base.Width - m_DockOptionCentre.Width) / 2;
				pos.Y = point.Y + (base.Height - m_DockOptionCentre.Height) / 2;
				m_DockManager.AddDockOption(m_DockOptionCentre, pos);
				return;
			}
			m_DockOptionTop = new DockOptionControl(this, moving_panel, Resource.DockOptionTop);
			m_DockOptionTop.DockTargetActivated += DockOptionTopEvent;
			pos.X = point.X + (base.Width - m_DockOptionTop.Width) / 2;
			pos.Y = point.Y + m_DockOptionTop.Height / 2;
			m_DockManager.AddDockOption(m_DockOptionTop, pos);
			m_DockOptionBottom = new DockOptionControl(this, moving_panel, Resource.DockOptionBottom);
			m_DockOptionBottom.DockTargetActivated += DockOptionBottomEvent;
			pos.X = point.X + (base.Width - m_DockOptionBottom.Width) / 2;
			pos.Y = point.Y + base.Height - 3 * m_DockOptionBottom.Height / 2;
			m_DockManager.AddDockOption(m_DockOptionBottom, pos);
			m_DockOptionLeft = new DockOptionControl(this, moving_panel, Resource.DockOptionLeft);
			m_DockOptionLeft.DockTargetActivated += DockOptionLeftEvent;
			pos.X = point.X + m_DockOptionLeft.Width / 2;
			pos.Y = point.Y + (base.Height - m_DockOptionLeft.Height) / 2;
			m_DockManager.AddDockOption(m_DockOptionLeft, pos);
			m_DockOptionRight = new DockOptionControl(this, moving_panel, Resource.DockOptionRight);
			m_DockOptionRight.DockTargetActivated += DockOptionRightEvent;
			pos.X = point.X + base.Width - 3 * m_DockOptionLeft.Width / 2;
			pos.Y = point.Y + (base.Height - m_DockOptionLeft.Height) / 2;
			m_DockManager.AddDockOption(m_DockOptionRight, pos);
		}
		else
		{
			if (m_DockOptionTop != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionTop);
				m_DockOptionTop = null;
			}
			if (m_DockOptionBottom != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionBottom);
				m_DockOptionBottom = null;
			}
			if (m_DockOptionLeft != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionLeft);
				m_DockOptionLeft = null;
			}
			if (m_DockOptionRight != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionRight);
				m_DockOptionRight = null;
			}
			if (m_DockOptionCentre != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionCentre);
				m_DockOptionCentre = null;
			}
		}
	}

	public void SetDockOptionControlOpacity(double opacity)
	{
		opacity = Math.Min(opacity, 0.699999988079071);
		if (m_DockOptionCentre != null)
		{
			m_DockOptionCentre.TargetOpacity = opacity;
		}
		if (m_DockOptionTop != null)
		{
			m_DockOptionTop.TargetOpacity = opacity;
			m_DockOptionBottom.TargetOpacity = opacity;
			m_DockOptionLeft.TargetOpacity = opacity;
			m_DockOptionRight.TargetOpacity = opacity;
		}
	}

	private void CentreDockOptionActivatEvent(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		if (mouse_over)
		{
			bool flag = mouse_pos.X >= 30 && mouse_pos.X < 59;
			bool flag2 = mouse_pos.Y >= 29 && mouse_pos.Y < 58;
			DockStyle dock_style = DockStyle.None;
			if (flag2 && flag)
			{
				dock_style = DockStyle.Fill;
			}
			else if (flag)
			{
				dock_style = ((mouse_pos.Y < 29) ? DockStyle.Top : DockStyle.Bottom);
			}
			else if (flag2)
			{
				dock_style = ((mouse_pos.X < 30) ? DockStyle.Left : DockStyle.Right);
			}
			ShowTargetDockPanel(dock_style, moving_panel);
		}
		else
		{
			ShowTargetDockPanel(DockStyle.None, moving_panel);
		}
	}

	private void ShowTargetDockPanel(DockStyle dock_style, DockPanel moving_panel)
	{
		if (m_TargetDockStyle == dock_style)
		{
			return;
		}
		if (dock_style == DockStyle.None)
		{
			m_TargetDockStyle = DockStyle.None;
			m_DockManager.RemoveDockTargetPanel();
			return;
		}
		SuspendLayout();
		m_TargetDockStyle = dock_style;
		Point p = new Point(0, 0);
		Size size = new Size(0, 0);
		switch (dock_style)
		{
		case DockStyle.Fill:
			if (Control is MDIPanel)
			{
				p = PointToClient(moving_panel.PointToScreen(new Point(0, 0)));
				size = moving_panel.Size;
				p = new Point(p.X - 10, p.Y - 10);
				size = new Size(size.Width + 20, size.Height + 20);
			}
			else
			{
				p = new Point(0, 0);
				size = base.Size;
			}
			break;
		case DockStyle.Top:
		{
			int num4 = Math.Min(moving_panel.Height, base.Height / 2);
			p = new Point(0, 0);
			size = new Size(base.Width, num4);
			break;
		}
		case DockStyle.Bottom:
		{
			int num3 = Math.Min(moving_panel.Height, base.Height / 2);
			p = new Point(0, base.Height - num3);
			size = new Size(base.Width, num3);
			break;
		}
		case DockStyle.Left:
		{
			int num2 = Math.Min(moving_panel.Width, base.Width / 2);
			p = new Point(0, 0);
			size = new Size(num2, base.Height);
			break;
		}
		case DockStyle.Right:
		{
			int num = Math.Min(moving_panel.Width, base.Width / 2);
			p = new Point(base.Width - num, 0);
			size = new Size(num, base.Height);
			break;
		}
		}
		p = PointToScreen(p);
		if (dock_style == DockStyle.Fill && m_TabbedPanel.TabCount > 1)
		{
			size = new Size(size.Width, size.Height - m_TabbedPanel.TabHeight);
		}
		m_DockManager.SetDockTargetPanel(p, size);
		ResumeLayout();
	}

	private void DockOptionTopEvent(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		ShowTargetDockPanel(mouse_over ? DockStyle.Top : DockStyle.None, moving_panel);
	}

	private void DockOptionBottomEvent(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		ShowTargetDockPanel(mouse_over ? DockStyle.Bottom : DockStyle.None, moving_panel);
	}

	private void DockOptionLeftEvent(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		ShowTargetDockPanel(mouse_over ? DockStyle.Left : DockStyle.None, moving_panel);
	}

	private void DockOptionRightEvent(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		ShowTargetDockPanel(mouse_over ? DockStyle.Right : DockStyle.None, moving_panel);
	}

	public void HandleDragWindowMouseMove(DockPanel moving_panel, Point screen_pt)
	{
		if (m_TabbedPanel != null || !(base.Parent is DockPanel) || m_DisableChildDocking)
		{
			Point location = PointToScreen(new Point(0, 0));
			bool b = new Rectangle(location, base.Size).Contains(screen_pt);
			ShowDockOptions(b, moving_panel);
		}
		if (m_TabbedPanel == null)
		{
			m_DockPanel1.HandleDragWindowMouseMove(moving_panel, screen_pt);
			m_DockPanel2.HandleDragWindowMouseMove(moving_panel, screen_pt);
		}
	}

	private List<Control> GetAllControls()
	{
		List<Control> list = new List<Control>();
		GetAllControls(list);
		return list;
	}

	private void UpdateParentTitleText()
	{
		if (base.Parent is FloatingForm || base.Parent is MDIForm)
		{
			base.Parent.Text = ActivePanelName;
		}
	}

	public void AddDockedPanel(DockPanel new_panel, DockStyle dock_style)
	{
		SuspendLayout();
		if (dock_style == DockStyle.Fill)
		{
			if (m_TabbedPanel == null)
			{
				SetTabbedPanel(new TabbedPanel());
			}
			foreach (Control allControl in new_panel.GetAllControls())
			{
				m_TabbedPanel.AddControl(allControl);
			}
		}
		else
		{
			new_panel.m_DockedParentUniqueID = m_UniqueID;
			new_panel.m_DockedStyle = dock_style;
			DockPanel dockPanel = new DockPanel(m_DockManager);
			dockPanel.Size = base.Size;
			TransferTo(dockPanel);
			m_DockPanel1 = dockPanel;
			m_DockPanel2 = new_panel;
			int splitSize = 0;
			DockStyle dock = DockStyle.None;
			switch (dock_style)
			{
			case DockStyle.Top:
				m_SplitMode = SplitMode.Horz;
				Misc.Swap(ref m_DockPanel1, ref m_DockPanel2);
				splitSize = Math.Min(new_panel.Height, base.Height / 2);
				dock = DockStyle.Top;
				break;
			case DockStyle.Bottom:
				m_SplitMode = SplitMode.Horz;
				splitSize = base.Height - Math.Min(new_panel.Height, base.Height / 2);
				dock = DockStyle.Top;
				break;
			case DockStyle.Left:
				m_SplitMode = SplitMode.Vert;
				Misc.Swap(ref m_DockPanel1, ref m_DockPanel2);
				splitSize = Math.Min(new_panel.Width, base.Width / 2);
				dock = DockStyle.Left;
				break;
			case DockStyle.Right:
				m_SplitMode = SplitMode.Vert;
				splitSize = base.Width - Math.Min(new_panel.Width, base.Width / 2);
				dock = DockStyle.Left;
				break;
			}
			m_DockPanel2.Dock = DockStyle.Fill;
			base.Controls.Add(m_DockPanel2);
			m_Splitter = new Splitter(m_SplitMode, m_DockManager.SplitterColour);
			m_Splitter.Dock = dock;
			base.Controls.Add(m_Splitter);
			m_DockPanel1.Dock = dock;
			base.Controls.Add(m_DockPanel1);
			SetSplitSize(splitSize);
			UpdateParentTitleText();
		}
		ResumeLayout();
	}

	private void Clear()
	{
		RemoveDockOptionControls();
		if (m_TabbedPanel != null)
		{
			base.Controls.Remove(m_TabbedPanel);
			m_TabbedPanel = null;
		}
		else
		{
			base.Controls.Remove(m_DockPanel1);
			base.Controls.Remove(m_DockPanel2);
			base.Controls.Remove(m_Splitter);
			m_DockPanel1 = null;
			m_DockPanel2 = null;
			m_Splitter = null;
		}
		base.Controls.Clear();
	}

	private void TransferTo(DockPanel target_panel)
	{
		if (m_TabbedPanel != null)
		{
			target_panel.SetTabbedPanel(m_TabbedPanel);
			SetTabbedPanel(null);
		}
		else
		{
			target_panel.m_DockPanel2 = m_DockPanel2;
			target_panel.Controls.Add(m_DockPanel2);
			m_DockPanel2 = null;
			target_panel.m_SplitMode = m_SplitMode;
			target_panel.Controls.Add(m_Splitter);
			m_Splitter = null;
			target_panel.m_DockPanel1 = m_DockPanel1;
			target_panel.Controls.Add(m_DockPanel1);
			m_DockPanel1 = null;
			m_SplitMode = SplitMode.NoSplit;
		}
		target_panel.m_FloatingSize = m_FloatingSize;
	}

	public void RemoveDockOptionControls()
	{
		ShowTargetDockPanel(DockStyle.None, null);
		ShowDockOptions(b: false, null);
		if (m_DockPanel1 != null)
		{
			m_DockPanel1.RemoveDockOptionControls();
		}
		if (m_DockPanel2 != null)
		{
			m_DockPanel2.RemoveDockOptionControls();
		}
	}

	public void Remove(DockPanel dock_panel)
	{
		SuspendLayout();
		DockPanel obj = ((dock_panel == m_DockPanel1) ? m_DockPanel2 : m_DockPanel1);
		Clear();
		obj.TransferTo(this);
		m_SplitMode = SplitMode.NoSplit;
		ResumeLayout();
	}

	public override string ToString()
	{
		string text = "DockPanel containing ";
		if (m_TabbedPanel != null)
		{
			return text + m_TabbedPanel.ToString();
		}
		if (m_SplitMode != 0)
		{
			return text + m_SplitMode;
		}
		if (base.Name != null && base.Name != "")
		{
			return base.Name;
		}
		return text + "Empty";
	}

	internal void SplitterMoved(Point pos)
	{
		Point point = PointToClient(pos);
		if (m_SplitMode == SplitMode.Horz)
		{
			SetSplitSize(point.Y);
		}
		else
		{
			SetSplitSize(point.X);
		}
	}

	public void Read(XmlReadStream read_stream, ICollection<Control> controls)
	{
		Clear();
		read_stream.Read("UniqueID", ref m_UniqueID);
		read_stream.ReadEnum("SplitMode", ref m_SplitMode);
		if (m_SplitMode == SplitMode.NoSplit)
		{
			TabbedPanel tabbedPanel = new TabbedPanel();
			SetTabbedPanel(tabbedPanel);
			tabbedPanel.Read(read_stream, controls);
		}
		else
		{
			m_DockPanel1 = new DockPanel(m_DockManager);
			m_DockPanel2 = new DockPanel(m_DockManager);
			m_DockPanel2.Dock = DockStyle.Fill;
			base.Controls.Add(m_DockPanel2);
			DockStyle dock = ((m_SplitMode == SplitMode.Horz) ? DockStyle.Top : DockStyle.Left);
			m_Splitter = new Splitter(m_SplitMode, m_DockManager.SplitterColour);
			m_Splitter.Dock = dock;
			base.Controls.Add(m_Splitter);
			m_DockPanel1.Dock = dock;
			base.Controls.Add(m_DockPanel1);
			int value = 0;
			read_stream.Read("SplitSize", ref value);
			SetSplitSize(value);
			m_Splitter.Enabled = m_DockPanel1.CanResize && m_DockPanel2.CanResize;
			if (read_stream.StartElement("Panel1"))
			{
				m_DockPanel1.Read(read_stream, controls);
				read_stream.EndElement();
			}
			if (read_stream.StartElement("Panel2"))
			{
				m_DockPanel2.Read(read_stream, controls);
				read_stream.EndElement();
			}
		}
		read_stream.Read("DockedParentUniqueID", ref m_DockedParentUniqueID);
		read_stream.ReadEnum("DockedStyle", ref m_DockedStyle);
		m_FloatingSize = Utils.ReadSize(read_stream, "FloatingSize", base.Size);
		m_FloatingLocation = Utils.ReadPoint(read_stream, "FloatingLocation", base.Location);
	}

	public void Write(string name, XmlWriteStream write_stream)
	{
		write_stream.StartElement(name);
		Write(write_stream);
		write_stream.EndElement();
	}

	public void Write(XmlWriteStream write_stream)
	{
		write_stream.Write("UniqueID", m_UniqueID);
		write_stream.Write("SplitMode", m_SplitMode);
		write_stream.Write("Description", ToString());
		if (m_SplitMode == SplitMode.NoSplit)
		{
			m_TabbedPanel.Write(write_stream);
		}
		else
		{
			m_DockPanel1.Write("Panel1", write_stream);
			m_DockPanel2.Write("Panel2", write_stream);
			write_stream.Write("SplitSize", (m_SplitMode == SplitMode.Horz) ? m_DockPanel1.Height : m_DockPanel1.Width);
		}
		write_stream.Write("DockedParentUniqueID", m_DockedParentUniqueID);
		write_stream.Write("DockedStyle", m_DockedStyle);
		Utils.Write(write_stream, "FloatingLocation", m_FloatingLocation);
		Utils.Write(write_stream, "FloatingSize", m_FloatingSize);
	}

	private static int GenerateUniqueID()
	{
		int i;
		for (i = m_NextUniqueID; m_UniqueIDSet.Contains(i); i++)
		{
		}
		m_UniqueIDSet.Add(i);
		m_NextUniqueID = i + 1;
		return i;
	}

	protected override void Dispose(bool disposing)
	{
		m_UniqueIDSet.Remove(m_UniqueID);
		base.Dispose(disposing);
	}

	public DockPanel FindPanel(int unique_id)
	{
		if (m_UniqueID == unique_id)
		{
			return this;
		}
		if (m_DockPanel1 != null)
		{
			DockPanel dockPanel = m_DockPanel1.FindPanel(unique_id);
			if (dockPanel != null)
			{
				return dockPanel;
			}
		}
		if (m_DockPanel2 != null)
		{
			DockPanel dockPanel2 = m_DockPanel2.FindPanel(unique_id);
			if (dockPanel2 != null)
			{
				return dockPanel2;
			}
		}
		return null;
	}

	public bool ContainsControl(Control control)
	{
		if (m_TabbedPanel != null)
		{
			return m_TabbedPanel.ContainsControl(control);
		}
		return false;
	}

	public DockPanel FindDockPanelRecursive(Control control)
	{
		if (m_TabbedPanel != null && m_TabbedPanel.ContainsControl(control))
		{
			return this;
		}
		if (m_DockPanel1 != null)
		{
			DockPanel dockPanel = m_DockPanel1.FindDockPanelRecursive(control);
			if (dockPanel != null)
			{
				return dockPanel;
			}
		}
		if (m_DockPanel2 != null)
		{
			DockPanel dockPanel2 = m_DockPanel2.FindDockPanelRecursive(control);
			if (dockPanel2 != null)
			{
				return dockPanel2;
			}
		}
		return null;
	}

	public void SetSplitSize(int size)
	{
		if (m_SplitMode == SplitMode.Horz)
		{
			int num = Misc.Clamp(size, 20, base.Height - 20);
			m_DockPanel1.Size = new Size(m_DockPanel1.Width, num);
		}
		else
		{
			int num2 = Misc.Clamp(size, 20, base.Width - 20);
			m_DockPanel1.Size = new Size(num2, m_DockPanel1.Height);
		}
		m_CurSize = base.Size;
	}

	public void CloseControl(Control control)
	{
		m_TabbedPanel.CloseControl(control);
	}

	public DockPanel GetSelectedPanel()
	{
		if (m_TabbedPanel != null)
		{
			if (m_TabbedPanel.Selected)
			{
				return this;
			}
		}
		else
		{
			DockPanel selectedPanel = m_DockPanel1.GetSelectedPanel();
			if (selectedPanel != null)
			{
				return selectedPanel;
			}
			selectedPanel = m_DockPanel2.GetSelectedPanel();
			if (selectedPanel != null)
			{
				return selectedPanel;
			}
		}
		return null;
	}

	public bool SelectPanel(Point screen_pt)
	{
		if (m_TabbedPanel != null)
		{
			return m_TabbedPanel.SelectPanel(screen_pt);
		}
		m_DockPanel1.SelectPanel(screen_pt);
		m_DockPanel2.SelectPanel(screen_pt);
		return false;
	}

	public void DeselectPanel()
	{
		if (m_TabbedPanel != null)
		{
			m_TabbedPanel.DeselectPanel();
			return;
		}
		m_DockPanel1.DeselectPanel();
		m_DockPanel2.DeselectPanel();
	}

	public void CloseCurrentTab()
	{
		m_TabbedPanel.CloseCurrentTab();
	}

	public void UpdateFloatingBounds()
	{
		m_FloatingLocation = PointToScreen(new Point(0, 0));
		m_FloatingSize = base.Size;
	}

	public void UpdateSplittedColour()
	{
		if (m_Splitter != null)
		{
			m_Splitter.BackColor = m_DockManager.SplitterColour;
		}
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		if (m_TabbedPanel != null)
		{
			return m_TabbedPanel.GetControlAtScreenPoint(pt);
		}
		Control controlAtScreenPoint = m_DockPanel1.GetControlAtScreenPoint(pt);
		if (controlAtScreenPoint != null)
		{
			return controlAtScreenPoint;
		}
		Control controlAtScreenPoint2 = m_DockPanel2.GetControlAtScreenPoint(pt);
		if (controlAtScreenPoint2 != null)
		{
			return controlAtScreenPoint2;
		}
		return null;
	}

	public Control GetActiveControl()
	{
		if (m_TabbedPanel != null)
		{
			return m_TabbedPanel.ActiveControl;
		}
		if (m_DockPanel1 != null)
		{
			Control activeControl = m_DockPanel1.GetActiveControl();
			if (activeControl != null)
			{
				return activeControl;
			}
		}
		if (m_DockPanel2 != null)
		{
			Control activeControl2 = m_DockPanel2.GetActiveControl();
			if (activeControl2 != null)
			{
				return activeControl2;
			}
		}
		return null;
	}

	public void SetActiveControl(Control control)
	{
		if (m_TabbedPanel != null)
		{
			m_TabbedPanel.SetActiveControl(control);
			return;
		}
		if (m_DockPanel1 != null)
		{
			m_DockPanel1.SetActiveControl(control);
		}
		if (m_DockPanel2 != null)
		{
			m_DockPanel2.SetActiveControl(control);
		}
	}

	public void GetAllControls(List<Control> controls)
	{
		if (m_TabbedPanel != null)
		{
			m_TabbedPanel.GetAllControls(controls);
			return;
		}
		if (m_DockPanel1 != null)
		{
			m_DockPanel1.GetAllControls(controls);
		}
		if (m_DockPanel2 != null)
		{
			m_DockPanel2.GetAllControls(controls);
		}
	}
}
