using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class TabbedPanel : Panel
{
	private TabbedPanelTitleBar m_TitlePanel = new TabbedPanelTitleBar();

	private Panel m_MainPanel = new Panel();

	private TabbedPanelTabs m_TabsPanel = new TabbedPanelTabs();

	private List<Control> m_Controls = new List<Control>();

	private Control m_ActiveControl;

	private DockManager DockManager => ((DockPanel)base.Parent).DockManager;

	public bool MaintainSplitRatios
	{
		get
		{
			foreach (Control control in m_Controls)
			{
				if (DockManager.GetControlDockOptions(control).MaintainSplitRatioOnResize)
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool ShouldDisplayTitleBar
	{
		get
		{
			bool result = (m_ActiveControl == null || !(m_ActiveControl is MDIPanel)) && base.Parent != null && base.Parent.Parent is DockPanel;
			if (m_ActiveControl != null)
			{
				DockerAttribute dockerAttribute = Utils.FindAttribute<DockerAttribute>(m_ActiveControl);
				if (dockerAttribute != null && !dockerAttribute.ShowTitleBar)
				{
					result = false;
				}
			}
			return result;
		}
	}

	public Control ActiveControl
	{
		get
		{
			if (m_ActiveControl == null)
			{
				return null;
			}
			return m_ActiveControl;
		}
	}

	public string ActiveControlName
	{
		get
		{
			if (m_ActiveControl == null)
			{
				return "";
			}
			return m_ActiveControl.Text;
		}
	}

	private bool ShowAsTabbed => m_Controls.Count > 1;

	private int ActivePanelIndex => m_Controls.IndexOf(m_ActiveControl);

	public bool CanResize
	{
		get
		{
			foreach (Control control in m_Controls)
			{
				DockerAttribute dockerAttribute = Utils.FindAttribute<DockerAttribute>(control);
				if (dockerAttribute != null && !dockerAttribute.CanResize)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool Selected => m_TitlePanel.BackColor == SystemColors.ActiveCaption;

	public int TabHeight => m_TabsPanel.Height;

	public int TabCount => m_Controls.Count;

	public List<Control> TabbedControls => m_Controls;

	public event ControlTextChangedHandler ControlTextChanged;

	public TabbedPanel()
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		BackColor = SystemColors.Control;
		m_MainPanel.Dock = DockStyle.Fill;
		base.Controls.Add(m_MainPanel);
		m_TitlePanel.Size = new Size(base.Width, Font.Height + 4);
		m_TitlePanel.Dock = DockStyle.Top;
		m_TitlePanel.Visible = false;
		m_TitlePanel.MouseDown += TabbedPanelTitleBarMouseDown;
		m_TitlePanel.CloseButtonClicked += TitlePanelCloseButtonClicked;
		m_TitlePanel.TitleDoubleClick += TitlePanelDoubleClick;
		m_TitlePanel.CaptionDragged += TitlePanelCaptionDragged;
		base.Controls.Add(m_TitlePanel);
		m_TabsPanel.Dock = DockStyle.Bottom;
		m_TabsPanel.Visible = false;
		m_TabsPanel.TabSelected += TabbedPanelTabsTabSelected;
		m_TabsPanel.FloatTab += TabbedPanelTabsFloatTab;
		m_TabsPanel.CloseTab += TabsPanelCloseTab;
		base.Controls.Add(m_TabsPanel);
	}

	private void TabsPanelCloseTab(Control control)
	{
		CloseControl(control);
	}

	private void TitlePanelCaptionDragged()
	{
		((DockPanel)base.Parent).CaptionDragged();
	}

	private void TitlePanelDoubleClick()
	{
		DockPanel dockPanel = (DockPanel)base.Parent;
		Control activeControl = m_ActiveControl;
		CloseCurrentTab();
		DockPanel dockPanel2 = new DockPanel(DockManager);
		activeControl.Size = dockPanel.FloatingSize;
		dockPanel2.AddControl(activeControl);
		DockManager.FloatPanel(dockPanel2, dockPanel.FloatingLocation);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	private void TitlePanelCloseButtonClicked()
	{
		CloseCurrentTab();
	}

	private void TabbedPanelTabsFloatTab(Control control)
	{
		_ = (DockPanel)base.Parent;
		DockPanel dockPanel = new DockPanel(DockManager);
		dockPanel.AddControl(control);
		CloseCurrentTab();
		Point screen_pos = base.Parent.PointToScreen(base.Location);
		DockManager.FloatAndDragPanel(dockPanel, screen_pos);
	}

	private void TabbedPanelTabsTabSelected(Control control)
	{
		SetActiveControl(control);
	}

	private void TabbedPanelTitleBarMouseDown(object sender, MouseEventArgs e)
	{
		Point p = ((Control)sender).PointToScreen(e.Location);
		p = PointToClient(p);
		MouseEventArgs e2 = new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta);
		OnMouseDown(e2);
	}

	public void AddControl(Control control)
	{
		SuspendLayout();
		if (m_Controls.Count == 1 && m_Controls[0] is FillerPanel)
		{
			Control control2 = m_Controls[0];
			base.Controls.Remove(control2);
			control2.Dispose();
			m_Controls.Clear();
		}
		HookControl(control);
		m_Controls.Add(control);
		m_TabsPanel.AddControl(control);
		UpdateTabsPanelVisibility();
		if (control is Form form)
		{
			form.TopLevel = false;
			form.FormBorderStyle = FormBorderStyle.None;
		}
		control.Focus();
		control.Dock = DockStyle.Fill;
		m_MainPanel.Controls.Add(control);
		m_TitlePanel.Text = control.Text;
		SetActiveControl(control);
		Refresh();
		control.Focus();
		ResumeLayout();
	}

	private void HookControl(Control control)
	{
		control.TextChanged += ControlTextChangedEvent;
	}

	private void UnhookControl(Control control)
	{
		control.TextChanged -= ControlTextChangedEvent;
	}

	private void ControlTextChangedEvent(object sender, EventArgs e)
	{
		ControlTextChangedEvent((Control)sender);
	}

	private void ControlTextChangedEvent(Control control)
	{
		m_TabsPanel.Refresh();
		if (this.ControlTextChanged != null)
		{
			this.ControlTextChanged(control);
		}
	}

	private void UpdateTabsPanelVisibility()
	{
		m_TabsPanel.Visible = m_Controls.Count > 1;
	}

	protected override void OnEnter(EventArgs e)
	{
		Refresh();
		base.OnEnter(e);
	}

	protected override void OnLeave(EventArgs e)
	{
		Refresh();
		base.OnLeave(e);
	}

	protected override void OnParentChanged(EventArgs e)
	{
		UpdateTitleBarVisibility();
		base.OnParentChanged(e);
	}

	public void UpdateTitleBarVisibility()
	{
		m_TitlePanel.Visible = ShouldDisplayTitleBar;
	}

	public void SetActiveControl(Control control)
	{
		if (m_ActiveControl == control)
		{
			return;
		}
		SuspendLayout();
		m_ActiveControl = control;
		m_TabsPanel.SetActiveTab(control);
		m_TitlePanel.Text = control.Text;
		if (base.Parent != null && (base.Parent.Parent is FloatingForm || base.Parent.Parent is MDIForm))
		{
			base.Parent.Parent.Text = control.Text;
		}
		UpdateTitleBarVisibility();
		m_TitlePanel.Text = control.Text;
		foreach (Control control2 in m_Controls)
		{
			control2.Visible = control2 == m_ActiveControl;
		}
		DockManager.OnActiveControlChanged(control);
		ResumeLayout();
	}

	private static string GetControlText(Control control)
	{
		if (control is Form)
		{
			return ((Form)control).Text;
		}
		return control.Text;
	}

	public override string ToString()
	{
		string text = "TabbedPanel showing ";
		Control activeControl = ActiveControl;
		if (activeControl != null)
		{
			return text + GetControlText(activeControl);
		}
		return "Empty tabbed panel";
	}

	protected override void OnGotFocus(EventArgs e)
	{
		SelectPanel();
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		UnselectPanel();
		base.OnLostFocus(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		ActiveControl.Focus();
		base.OnMouseDown(e);
	}

	public void CloseControl(Control control)
	{
		int num = m_Controls.IndexOf(control);
		UnhookControl(control);
		m_Controls.Remove(control);
		m_TabsPanel.RemoveControl(control);
		if (m_MainPanel.Controls.Contains(control))
		{
			m_MainPanel.Controls.Remove(control);
			control.Dispose();
		}
		if (m_Controls.Count != 0)
		{
			UpdateTabsPanelVisibility();
			if (control == m_ActiveControl)
			{
				int index = Math.Max(0, num - 1);
				SetActiveControl(m_Controls[index]);
				Refresh();
				m_ActiveControl.Show();
				m_ActiveControl.Focus();
			}
		}
		else
		{
			((DockPanel)base.Parent).OnLastTabClosed();
		}
	}

	public void CloseCurrentTab()
	{
		CloseControl(m_ActiveControl);
	}

	public void Read(XmlReadStream read_stream, ICollection<Control> controls)
	{
		if (read_stream.StartElement("TabbedPanel"))
		{
			int value = -1;
			read_stream.Read("ActivePanelIndex", ref value);
			ReadControlList(read_stream, controls);
			if (value >= 0 && value < m_Controls.Count)
			{
				SetActiveControl(m_Controls[value]);
			}
			else if (m_Controls.Count != 0)
			{
				SetActiveControl(m_Controls[0]);
			}
			m_TabsPanel.Read(read_stream);
			read_stream.EndElement();
		}
	}

	public void Write(XmlWriteStream write_stream)
	{
		write_stream.StartElement("TabbedPanel");
		write_stream.Write("ActivePanelIndex", ActivePanelIndex);
		WriteControlList(write_stream);
		m_TabsPanel.Write(write_stream);
		write_stream.EndElement();
	}

	private void ReadControlList(XmlReadStream read_stream, ICollection<Control> controls)
	{
		if (!read_stream.StartElement("Controls"))
		{
			return;
		}
		for (int i = 0; i < read_stream.Count; i++)
		{
			string value = "error";
			read_stream.Read(i, ref value);
			Control control = FindControl(value, controls);
			if (control != null)
			{
				AddControl(control);
			}
		}
		read_stream.EndElement();
	}

	private static Control FindControl(string control_name, ICollection<Control> controls)
	{
		foreach (Control control in controls)
		{
			if (control.Text == control_name)
			{
				return control;
			}
		}
		return null;
	}

	private void WriteControlList(XmlWriteStream write_stream)
	{
		write_stream.StartElement("Controls");
		foreach (Control control in m_Controls)
		{
			write_stream.Write("Control", control.Text);
		}
		write_stream.EndElement();
	}

	public bool ContainsControl(Control control)
	{
		foreach (Control control2 in m_Controls)
		{
			if (control2 == control)
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsControl(Type type)
	{
		foreach (Control control in m_Controls)
		{
			if (control.GetType() == type)
			{
				return true;
			}
		}
		return false;
	}

	public bool SelectPanel(Point screen_pt)
	{
		Point pt = PointToClient(screen_pt);
		if (base.ClientRectangle.Contains(pt))
		{
			SelectPanel();
			return true;
		}
		UnselectPanel();
		return false;
	}

	public void DeselectPanel()
	{
		UnselectPanel();
	}

	private void SelectPanel()
	{
		m_TitlePanel.BackColor = SystemColors.ActiveCaption;
	}

	private void UnselectPanel()
	{
		m_TitlePanel.BackColor = SystemColors.InactiveCaption;
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		if (m_ActiveControl != null && m_ActiveControl.ClientRectangle.Contains(m_ActiveControl.PointToClient(pt)))
		{
			return m_ActiveControl;
		}
		return null;
	}

	public void GetAllControls(List<Control> controls)
	{
		controls.AddRange(m_Controls);
	}
}
