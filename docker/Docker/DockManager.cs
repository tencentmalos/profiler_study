using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using SCLCoreCLR;

namespace Docker;

public class DockManager : Control
{
	private enum GetWindow_Cmd : uint
	{
		GW_HWNDFIRST,
		GW_HWNDLAST,
		GW_HWNDNEXT,
		GW_HWNDPREV,
		GW_OWNER,
		GW_CHILD,
		GW_ENABLEDPOPUP
	}

	private const int WH_MOUSE_LL = 14;

	private const int m_DockGap = 4;

	private const int m_MinDockPanelHeight = 10;

	private const int m_MinDockPanelWidth = 10;

	private Panel m_BasePanel = new Panel();

	private ToolStripContainer m_ToolStripContainer = new ToolStripContainer();

	private DockTargetPanel m_DockTargetPanel;

	private Form m_MainForm;

	private DockPanel m_RootPanel;

	private MDIPanel m_MDIPanel;

	private List<FloatingForm> m_FloatingForms = new List<FloatingForm>();

	private List<DockOptionControl> m_DockOptionControls = new List<DockOptionControl>();

	private List<ToolStrip> m_ToolStrips = new List<ToolStrip>();

	private List<Control> m_InternalControls = new List<Control>();

	private DockPanel m_HoverDockPanel;

	private Point m_NextMDIChildLocation = new Point(0, 0);

	private Color m_SplitterColour = SystemColors.Control;

	private Dictionary<Control, ControlDockOptions> m_ControlDockOptions = new Dictionary<Control, ControlDockOptions>();

	private Icon m_Icon;

	public bool IsMDI => m_MDIPanel != null;

	internal bool DockTargetPanelActive => m_DockTargetPanel != null;

	public bool MDIMaximised
	{
		get
		{
			if (m_MDIPanel != null)
			{
				return m_MDIPanel.Maximised;
			}
			return false;
		}
		set
		{
			MaximiseMDIForms();
		}
	}

	public Color SplitterColour
	{
		get
		{
			return m_SplitterColour;
		}
		set
		{
			m_SplitterColour = value;
			foreach (DockPanel allDockPanel in GetAllDockPanels())
			{
				allDockPanel.UpdateSplittedColour();
			}
		}
	}

	public Icon Icon
	{
		get
		{
			return m_Icon;
		}
		set
		{
			m_Icon = value;
		}
	}

	public event ActiveControlChangedHandler ActiveControlChanged;

	public event MDIFormClosingHandler MDIFormClosing;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

	public DockManager(Form main_form)
		: this(main_form, mdi: false)
	{
	}

	public DockManager(Form main_form, bool mdi)
	{
		Application.AddMessageFilter(new MessageFilterClass(this));
		SuspendLayout();
		m_ToolStripContainer.Dock = DockStyle.Fill;
		base.Controls.Add(m_ToolStripContainer);
		m_BasePanel.Dock = DockStyle.Fill;
		m_ToolStripContainer.ContentPanel.Controls.Add(m_BasePanel);
		m_MainForm = main_form;
		ProfessionalColorTable professionalColorTable = new ProfessionalColorTable();
		Color toolStripPanelGradientBegin = professionalColorTable.ToolStripPanelGradientBegin;
		Color toolStripPanelGradientEnd = professionalColorTable.ToolStripPanelGradientEnd;
		Bitmap bitmap = new Bitmap(2048, 1);
		int num = bitmap.Width - 1;
		for (int i = 0; i < bitmap.Width; i++)
		{
			int red = ((num - i) * toolStripPanelGradientBegin.R + i * toolStripPanelGradientEnd.R) / num;
			int green = ((num - i) * toolStripPanelGradientBegin.G + i * toolStripPanelGradientEnd.G) / num;
			int blue = ((num - i) * toolStripPanelGradientBegin.B + i * toolStripPanelGradientEnd.B) / num;
			Color color = Color.FromArgb(red, green, blue);
			bitmap.SetPixel(i, 0, color);
		}
		BackgroundImage = bitmap;
		BackgroundImageLayout = ImageLayout.Stretch;
		Initialise(mdi);
		ResumeLayout();
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == (Keys.Tab | Keys.Control) && m_MDIPanel != null && m_MDIPanel.GetSelectedMDIForm() != null)
		{
			m_MDIPanel.SelectNextMDIForm();
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	public void Clear()
	{
		foreach (FloatingForm item in new List<FloatingForm>(m_FloatingForms))
		{
			item.ForceClose();
		}
		Initialise(IsMDI);
	}

	private void Initialise(bool mdi)
	{
		CreateRootPanel();
		SetupRootPanel(mdi);
	}

	private void SetupRootPanel(bool mdi)
	{
		if (mdi)
		{
			MDIPanel mDIPanel = CreateMDIPanel();
			m_InternalControls.Add(mDIPanel);
			m_RootPanel.AddControl(mDIPanel);
		}
		else
		{
			AddFillerPanel();
		}
	}

	private void CreateRootPanel()
	{
		if (m_RootPanel != null)
		{
			m_RootPanel.Dispose();
		}
		m_RootPanel = new DockPanel(this);
		m_RootPanel.Dock = DockStyle.Fill;
		m_BasePanel.Controls.Add(m_RootPanel);
	}

	private void AddFillerPanel()
	{
		FillerPanel fillerPanel = CreateFillerPanel();
		m_RootPanel.AddControl(fillerPanel);
		m_InternalControls.Add(fillerPanel);
	}

	private FillerPanel CreateFillerPanel()
	{
		FillerPanel fillerPanel = new FillerPanel();
		fillerPanel.Name = "Root filler panel";
		fillerPanel.Disposed += FillerPanelDisposed;
		return fillerPanel;
	}

	private void FillerPanelDisposed(object sender, EventArgs e)
	{
		m_InternalControls.Remove((Control)sender);
	}

	protected override void OnResize(EventArgs eventargs)
	{
		m_RootPanel.Size = base.Size;
		base.OnResize(eventargs);
	}

	private DockPanel WrapControlWithDockPanel(Control control)
	{
		DockPanel dockPanel = new DockPanel(this);
		dockPanel.Size = control.Size;
		dockPanel.AddControl(control);
		return dockPanel;
	}

	public void AddFloating(Control control)
	{
		Size size = control.Size;
		DockPanel dockPanel = control as DockPanel;
		if (dockPanel == null)
		{
			dockPanel = WrapControlWithDockPanel(control);
		}
		FloatingForm floatingForm = AddFloatingPanel(dockPanel);
		floatingForm.Owner = m_MainForm;
		floatingForm.ClientSize = size;
	}

	public void AddFloating(Control control, Point pos)
	{
		DockPanel dockPanel = control as DockPanel;
		if (dockPanel == null)
		{
			dockPanel = WrapControlWithDockPanel(control);
		}
		AddFloatingPanel(dockPanel, pos);
	}

	private FloatingForm AddFloatingPanel(DockPanel dock_panel)
	{
		FloatingForm floatingForm = new FloatingForm(dock_panel, this);
		floatingForm.Owner = m_MainForm;
		if (dock_panel.FloatingLocation != Point.Empty)
		{
			floatingForm.Location = dock_panel.FloatingLocation;
		}
		floatingForm.ClientSize = dock_panel.FloatingSize;
		floatingForm.Show();
		AddFloatingForm(floatingForm);
		return floatingForm;
	}

	private void FloatingFormActivated(object sender, EventArgs e)
	{
		FloatingForm item = (FloatingForm)sender;
		m_FloatingForms.Remove(item);
		m_FloatingForms.Add(item);
	}

	private FloatingForm AddFloatingPanel(DockPanel dock_panel, Point pos)
	{
		FloatingForm floatingForm = AddFloatingPanel(dock_panel);
		if (pos != Point.Empty)
		{
			floatingForm.Location = pos;
		}
		return floatingForm;
	}

	private void AddDocked(DockPanel existing_dock_panel, Control control, DockStyle dock_style)
	{
		DockPanel new_panel = WrapControlWithDockPanel(control);
		existing_dock_panel.AddDockedPanel(new_panel, dock_style);
	}

	public void AddDocked(Control docked_control, Control control, DockStyle dock_style)
	{
		DockPanel panel = GetPanel(docked_control);
		AddDocked(panel, control, dock_style);
	}

	public void AddDocked(Control control, DockStyle dock_style)
	{
		AddDocked(m_RootPanel, control, dock_style);
	}

	private static DockPanel GetPanel(Control control)
	{
		while (control != null && !(control is DockPanel))
		{
			control = control.Parent;
		}
		return control as DockPanel;
	}

	internal void DockFloating(FloatingForm floating_form)
	{
		DockPanel dockPanel = floating_form.DockPanel;
		int dockedParentUniqueID = dockPanel.DockedParentUniqueID;
		DockPanel dockPanel2 = FindPanel(dockedParentUniqueID);
		if (dockPanel2 != null)
		{
			dockPanel2.AddDockedPanel(dockPanel, dockPanel.DockStyle);
			floating_form.Close();
		}
	}

	private DockPanel FindPanel(int panel_id)
	{
		DockPanel dockPanel = m_RootPanel.FindPanel(panel_id);
		if (dockPanel != null)
		{
			return dockPanel;
		}
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			dockPanel = floatingForm.DockPanel.FindPanel(panel_id);
			if (dockPanel != null)
			{
				return dockPanel;
			}
		}
		return null;
	}

	private List<FloatingForm> GetFloatingFormList()
	{
		List<FloatingForm> list = new List<FloatingForm>(m_FloatingForms);
		list.Reverse();
		return list;
	}

	private FloatingForm GetTopMostForm(Point screen_pt, DockPanel exclude_panel)
	{
		foreach (FloatingForm floatingForm in GetFloatingFormList())
		{
			if (new Rectangle(floatingForm.Location, floatingForm.Size).Contains(screen_pt) && floatingForm.DockPanel != exclude_panel)
			{
				return floatingForm;
			}
		}
		return null;
	}

	private MDIForm GetMDIForm(Point screen_pt, DockPanel exclude_panel)
	{
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			if (mDIForm.RectangleToScreen(mDIForm.ClientRectangle).Contains(screen_pt) && mDIForm.DockPanel != exclude_panel)
			{
				return mDIForm;
			}
		}
		return null;
	}

	private DockPanel GetDockPanel(Point screen_pt, DockPanel exclude_panel, bool mdi_only)
	{
		if (!mdi_only)
		{
			FloatingForm topMostForm = GetTopMostForm(screen_pt, exclude_panel);
			if (topMostForm != null)
			{
				return topMostForm.DockPanel;
			}
		}
		if (m_MDIPanel.RectangleToScreen(m_MDIPanel.ClientRectangle).Contains(screen_pt))
		{
			MDIForm mDIForm = GetMDIForm(screen_pt, exclude_panel);
			if (mDIForm != null)
			{
				return mDIForm.DockPanel;
			}
		}
		if (!mdi_only && m_RootPanel.RectangleToScreen(m_RootPanel.ClientRectangle).Contains(screen_pt))
		{
			return m_RootPanel;
		}
		return null;
	}

	internal void HandleDragWindowMouseMove(DockPanel moving_panel, Point screen_pt)
	{
		bool flag = moving_panel.Parent is MDIForm;
		DockPanel dockPanel = GetDockPanel(screen_pt, moving_panel, flag);
		if (moving_panel.Parent is FloatingForm && !m_MDIPanel.Maximised)
		{
			m_MDIPanel.HandleDragWindowMouseMove(moving_panel, screen_pt);
			m_MDIPanel.SetDockOptionControlOpacity(1.0);
		}
		if (m_HoverDockPanel != null && m_HoverDockPanel != dockPanel)
		{
			m_HoverDockPanel.RemoveDockOptionControls();
		}
		if (dockPanel != null)
		{
			dockPanel.SetDockOptionControlOpacity(1.0);
			dockPanel.HandleDragWindowMouseMove(moving_panel, screen_pt);
			if (!flag && dockPanel.Parent is MDIForm)
			{
				m_RootPanel.SetDockOptionControlOpacity(1.0);
				m_RootPanel.HandleDragWindowMouseMove(moving_panel, screen_pt);
			}
		}
		else
		{
			m_RootPanel.SetDockOptionControlOpacity(0.0);
			m_RootPanel.RemoveDockOptionControls();
		}
		m_HoverDockPanel = dockPanel;
		foreach (DockOptionControl dockOptionControl in m_DockOptionControls)
		{
			bool flag2 = false;
			Point point = dockOptionControl.PointToClient(screen_pt);
			flag2 = ((dockOptionControl.Region == null) ? dockOptionControl.ClientRectangle.Contains(point) : dockOptionControl.Region.IsVisible(point));
			dockOptionControl.SetDockTargetActive(flag2, point);
		}
	}

	internal FloatingForm FloatMDIForm(MDIForm mdi_form)
	{
		Point location = mdi_form.Parent.PointToScreen(mdi_form.Location);
		FloatingForm floatingForm = AddFloatingPanel(mdi_form.DockPanel);
		floatingForm.Location = location;
		floatingForm.Size = mdi_form.Size;
		CloseMDIForm(mdi_form);
		RemoveDockOptionControls();
		RemoveDockTargetPanel();
		return floatingForm;
	}

	public void RemoveDockOptionControls()
	{
		m_RootPanel.RemoveDockOptionControls();
		m_MDIPanel.RemoveDockOptionControls();
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			mDIForm.DockPanel.RemoveDockOptionControls();
		}
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			floatingForm.DockPanel.RemoveDockOptionControls();
		}
	}

	internal void AddDockOption(DockOptionControl control, Point pos)
	{
		control.Owner = m_MainForm;
		control.Show();
		control.Location = pos;
		m_DockOptionControls.Add(control);
	}

	internal void RemoveDockOption(DockOptionControl control)
	{
		control.Owner = null;
		control.Close();
		m_DockOptionControls.Remove(control);
	}

	internal void DockToDockTarget(MDIForm mdi_form)
	{
		if (m_HoverDockPanel != null)
		{
			m_HoverDockPanel.SetDockOptionControlOpacity(0.0);
			m_HoverDockPanel = null;
		}
		m_MDIPanel.SetDockOptionControlOpacity(0.0);
		if (DockToDockTarget(mdi_form.DockPanel))
		{
			CloseMDIForm(mdi_form);
		}
		RemoveDockOptionControls();
		RemoveDockTargetPanel();
	}

	internal bool DockToDockTarget(FloatingForm floating_form)
	{
		bool num = DockToDockTarget(floating_form.DockPanel);
		if (num)
		{
			RemoveFloatingForm(floating_form);
			floating_form.Close();
		}
		RemoveDockOptionControls();
		RemoveDockTargetPanel();
		return num;
	}

	private void MoveFormToMDI(FloatingForm floating_form)
	{
		Point location = floating_form.Location;
		location = m_MDIPanel.PointToClient(location);
		DockPanel dockPanel = floating_form.DockPanel;
		AddMdiChild(dockPanel);
		((MDIForm)dockPanel.Parent).Location = location;
		RemoveFloatingForm(floating_form);
		floating_form.Close();
	}

	private void AddFloatingForm(FloatingForm floating_form)
	{
		floating_form.Activated += FloatingFormActivated;
		floating_form.Disposed += FloatingFormDisposed;
		m_FloatingForms.Add(floating_form);
	}

	private void RemoveFloatingForm(FloatingForm floating_form)
	{
		floating_form.Activated -= FloatingFormActivated;
		floating_form.Disposed -= FloatingFormDisposed;
		m_FloatingForms.Remove(floating_form);
	}

	private void MDIFormMaximiseButtonClicked()
	{
		MaximiseMDIForms();
	}

	public void MaximiseMDIForms()
	{
		SuspendLayout();
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			mDIForm.SetAsMaximised();
		}
		m_MDIPanel.Maximised = true;
		ResumeLayout();
	}

	internal void BringMaximisedMDIFormToFront(MDIForm form)
	{
		if (!m_MDIPanel.Maximised)
		{
			return;
		}
		SuspendLayout();
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			if (mDIForm == form)
			{
				bool activated = mDIForm.Activated;
				mDIForm.Activated = true;
				mDIForm.Show();
				mDIForm.BringToFront();
				if (!activated)
				{
					OnActiveMDIFormChanged(form);
				}
			}
			else
			{
				mDIForm.Activated = false;
				mDIForm.Hide();
			}
		}
		m_MDIPanel.Refresh();
		ResumeLayout();
	}

	private MDIForm GetActiveMDIForm()
	{
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			if (mDIForm.Activated)
			{
				return mDIForm;
			}
		}
		return null;
	}

	internal void RestoreWindows()
	{
		SuspendLayout();
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			mDIForm.SetAsRestored();
		}
		m_MDIPanel.Maximised = false;
		ResumeLayout();
	}

	private void FloatingFormDisposed(object sender, EventArgs e)
	{
		RemoveFloatingForm((FloatingForm)sender);
	}

	internal bool DockToDockTarget(DockPanel dock_panel)
	{
		foreach (DockOptionControl dockOptionControl in m_DockOptionControls)
		{
			if (dockOptionControl.TargetPanel is DockPanel)
			{
				DockPanel dockPanel = (DockPanel)dockOptionControl.TargetPanel;
				DockStyle targetDockStyle = dockPanel.TargetDockStyle;
				if (targetDockStyle == DockStyle.None)
				{
					continue;
				}
				if (dock_panel.Parent is FloatingForm && targetDockStyle == DockStyle.Fill && dockPanel.Control is MDIPanel)
				{
					FloatingForm floating_form = (FloatingForm)dock_panel.Parent;
					MoveFormToMDI(floating_form);
					continue;
				}
				if (!(dock_panel.Parent is FloatingForm) || targetDockStyle != DockStyle.Fill || !(dockPanel.Parent is MDIForm) || !m_MDIPanel.Maximised)
				{
					dockPanel.AddDockedPanel(dock_panel, targetDockStyle);
					return true;
				}
				FloatingForm floating_form2 = (FloatingForm)dock_panel.Parent;
				MoveFormToMDI(floating_form2);
			}
			else if (dockOptionControl.TargetPanel is MDIPanel && ((MDIPanel)dockOptionControl.TargetPanel).TargetDockStyle != 0 && dock_panel.Parent is FloatingForm)
			{
				FloatingForm floating_form3 = (FloatingForm)dock_panel.Parent;
				MoveFormToMDI(floating_form3);
			}
		}
		return false;
	}

	internal void RemoveDockTargetPanel()
	{
		if (m_DockTargetPanel != null)
		{
			m_DockTargetPanel.Close();
			m_DockTargetPanel.Owner = null;
			m_DockTargetPanel = null;
		}
	}

	internal void SetDockTargetPanel(Point pos, Size size)
	{
		SuspendLayout();
		if (m_DockTargetPanel == null)
		{
			m_DockTargetPanel = new DockTargetPanel();
			m_DockTargetPanel.Owner = m_MainForm;
			m_DockTargetPanel.Show();
		}
		m_DockTargetPanel.SuspendLayout();
		m_DockTargetPanel.Location = pos;
		m_DockTargetPanel.Size = size;
		foreach (DockOptionControl dockOptionControl in m_DockOptionControls)
		{
			Utils.BringToFrontNoActivate(dockOptionControl);
		}
		m_DockTargetPanel.Refresh();
		m_DockTargetPanel.ResumeLayout();
		ResumeLayout();
	}

	internal FloatingForm FloatPanel(DockPanel dock_panel, Point pos)
	{
		if (dock_panel == m_RootPanel)
		{
			CreateRootPanel();
		}
		else if (dock_panel.Parent is DockPanel)
		{
			((DockPanel)dock_panel.Parent).Remove(dock_panel);
		}
		return AddFloatingPanel(dock_panel, pos);
	}

	internal void FloatAndDragPanel(DockPanel dock_panel)
	{
		Point screen_pos = dock_panel.PointToScreen(new Point(0, 0));
		FloatAndDragPanel(dock_panel, screen_pos);
	}

	internal void FloatAndDragPanel(DockPanel dock_panel, Point screen_pos)
	{
		FloatingForm floatingForm = FloatPanel(dock_panel, screen_pos);
		int num = Font.Height + 4;
		if (Cursor.Position.Y > num)
		{
			floatingForm.Location = new Point(floatingForm.Location.X, Cursor.Position.Y - num / 2);
		}
		floatingForm.SetDragging();
	}

	public void SetWindowState(Control control, FormWindowState window_state)
	{
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			if (floatingForm.DockPanel.FindDockPanelRecursive(control) != null || floatingForm == control)
			{
				floatingForm.WindowState = window_state;
				m_MDIPanel.Maximised = floatingForm.WindowState == FormWindowState.Maximized;
				break;
			}
		}
	}

	public void Read(XmlDocument xml_document, XmlElement current_element, ICollection<Control> controls, List<ToolStrip> tool_strips)
	{
		m_MainForm.SuspendLayout();
		CreateRootPanel();
		XmlReadStream xmlReadStream = new XmlReadStream(xml_document, current_element);
		if (xmlReadStream.StartElement("DockManager"))
		{
			List<Control> list = new List<Control>(controls);
			if (xmlReadStream.StartElement("InternalControls"))
			{
				ReadInternalControls(xmlReadStream);
				list.AddRange(m_InternalControls);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("RootPanel"))
			{
				m_RootPanel.Read(xmlReadStream, list);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("FloatingForms"))
			{
				ReadFloatingForms(xmlReadStream, list);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("MDIForms"))
			{
				m_MDIPanel.Read(this, xmlReadStream, list);
				xmlReadStream.EndElement();
			}
			if (xmlReadStream.StartElement("ToolStrips"))
			{
				ReadToolStrips(xmlReadStream, tool_strips);
				xmlReadStream.EndElement();
			}
		}
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			HookMDIForm(mDIForm);
		}
		m_MainForm.ResumeLayout();
	}

	public void Write(XmlDocument xml_document, XmlElement current_element)
	{
		XmlWriteStream xmlWriteStream = new XmlWriteStream(xml_document, current_element);
		xmlWriteStream.StartElement("DockManager");
		xmlWriteStream.StartElement("InternalControls");
		WriteInternalControls(xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("RootPanel");
		m_RootPanel.Write(xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("FloatingForms");
		WriteFloatingForms(xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("MDIForms");
		m_MDIPanel.Write(xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.StartElement("ToolStrips");
		WriteToolStrips(xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.EndElement();
	}

	private void ReadInternalControls(XmlReadStream stream)
	{
		for (int i = 0; i < stream.Count; i++)
		{
			stream.StartElement(i);
			string currentValue = stream.CurrentValue;
			Control item = null;
			if (currentValue == typeof(FillerPanel).ToString())
			{
				item = CreateFillerPanel();
			}
			else if (currentValue == typeof(MDIPanel).ToString())
			{
				item = CreateMDIPanel();
			}
			m_InternalControls.Add(item);
			stream.EndElement();
		}
	}

	private void WriteInternalControls(XmlWriteStream stream)
	{
		foreach (Control internalControl in m_InternalControls)
		{
			stream.Write("Control", internalControl.GetType().ToString());
		}
	}

	private void ReadFloatingForms(XmlReadStream read_stream, ICollection<Control> controls)
	{
		for (int i = 0; i < read_stream.Count; i++)
		{
			read_stream.StartElement(i);
			FloatingForm floatingForm = new FloatingForm(this);
			floatingForm.FormBorderStyle = FormBorderStyle.SizableToolWindow;
			floatingForm.Owner = m_MainForm;
			floatingForm.Show();
			floatingForm.Read(read_stream, controls);
			read_stream.EndElement();
			AddFloatingForm(floatingForm);
		}
	}

	private void WriteFloatingForms(XmlWriteStream write_stream)
	{
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			write_stream.StartElement("FloatingForm");
			floatingForm.Write(write_stream);
			write_stream.EndElement();
		}
	}

	private ToolStrip FindToolStrip(string name, List<ToolStrip> tool_strips)
	{
		foreach (ToolStrip tool_strip in tool_strips)
		{
			if (tool_strip.Name == name)
			{
				return tool_strip;
			}
		}
		return null;
	}

	private void ReadToolStrips(XmlReadStream read_stream, List<ToolStrip> tool_strips)
	{
		for (int i = 0; i < read_stream.Count; i++)
		{
			read_stream.StartElement(i);
			string value = "error";
			read_stream.Read("Name", ref value);
			ToolStrip toolStrip = FindToolStrip(value, tool_strips);
			if (toolStrip != null)
			{
				bool value2 = true;
				read_stream.Read("Floating", ref value2);
				if (value2)
				{
					int value3 = 0;
					read_stream.Read("X", ref value3);
					int value4 = 0;
					read_stream.Read("Y", ref value4);
					Point pos = new Point(value3, value4);
					AddFloating(toolStrip, pos);
				}
				else
				{
					int value5 = 0;
					read_stream.Read("X", ref value5);
					int value6 = 0;
					read_stream.Read("Y", ref value6);
					DockStyle value7 = DockStyle.Top;
					read_stream.ReadEnum("Dock", ref value7);
					ToolStripPanel panel = null;
					switch (value7)
					{
					case DockStyle.Top:
						panel = m_ToolStripContainer.TopToolStripPanel;
						break;
					case DockStyle.Bottom:
						panel = m_ToolStripContainer.BottomToolStripPanel;
						break;
					case DockStyle.Left:
						panel = m_ToolStripContainer.LeftToolStripPanel;
						break;
					case DockStyle.Right:
						panel = m_ToolStripContainer.RightToolStripPanel;
						break;
					}
					Point location = new Point(value5, value6);
					m_ToolStrips.Add(toolStrip);
					DockToolStrip(panel, toolStrip, location);
				}
			}
			read_stream.EndElement();
		}
	}

	private void WriteToolStrips(XmlWriteStream write_stream)
	{
		foreach (ToolStrip toolStrip in m_ToolStrips)
		{
			write_stream.StartElement("ToolStrip");
			write_stream.Write("Name", toolStrip.Name);
			Control control = toolStrip.Parent;
			bool flag = control is FloatingToolStripForm;
			write_stream.Write("Floating", flag);
			if (flag)
			{
				write_stream.Write("X", control.Location.X);
				write_stream.Write("Y", control.Location.Y);
			}
			else
			{
				write_stream.Write("X", toolStrip.Location.X);
				write_stream.Write("Y", toolStrip.Location.Y);
				DockStyle value = DockStyle.None;
				if (control == m_ToolStripContainer.TopToolStripPanel)
				{
					value = DockStyle.Top;
				}
				else if (control == m_ToolStripContainer.BottomToolStripPanel)
				{
					value = DockStyle.Bottom;
				}
				else if (control == m_ToolStripContainer.LeftToolStripPanel)
				{
					value = DockStyle.Left;
				}
				else if (control == m_ToolStripContainer.RightToolStripPanel)
				{
					value = DockStyle.Right;
				}
				write_stream.Write("Dock", value);
			}
			write_stream.EndElement();
		}
	}

	public void AddFloating(ToolStrip tool_strip)
	{
		AddFloating(tool_strip, Point.Empty);
	}

	private void AddFloating(ToolStrip tool_strip, Point pos)
	{
		m_ToolStrips.Add(tool_strip);
		tool_strip.Disposed += ToolStripDisposed;
		FloatingToolStripForm floatingToolStripForm = new FloatingToolStripForm(this, tool_strip);
		floatingToolStripForm.Owner = m_MainForm;
		floatingToolStripForm.Show();
		if (pos != Point.Empty)
		{
			floatingToolStripForm.Location = pos;
		}
	}

	internal void OnFloatingToolStripFormMoved(FloatingToolStripForm sender)
	{
		ToolStripPanel panel = null;
		Rectangle toolStripRect = GetToolStripRect(Cursor.Position, ref panel, sender);
		if (toolStripRect != Rectangle.Empty)
		{
			SetDockTargetPanel(toolStripRect.Location, toolStripRect.Size);
		}
		else
		{
			RemoveDockTargetPanel();
		}
	}

	private Rectangle GetToolStripRect(Point pt, ref ToolStripPanel panel, FloatingToolStripForm sender)
	{
		Rectangle toolStripRect = GetToolStripRect(DockStyle.Top, ref panel, sender);
		if (toolStripRect.Contains(pt))
		{
			return toolStripRect;
		}
		toolStripRect = GetToolStripRect(DockStyle.Bottom, ref panel, sender);
		if (toolStripRect.Contains(pt))
		{
			return toolStripRect;
		}
		toolStripRect = GetToolStripRect(DockStyle.Left, ref panel, sender);
		if (toolStripRect.Contains(pt))
		{
			return toolStripRect;
		}
		toolStripRect = GetToolStripRect(DockStyle.Right, ref panel, sender);
		if (toolStripRect.Contains(pt))
		{
			return toolStripRect;
		}
		return Rectangle.Empty;
	}

	internal void OnFloatingToolStripDragged(FloatingToolStripForm sender)
	{
		RemoveDockTargetPanel();
		ToolStripPanel panel = null;
		GetToolStripRect(Cursor.Position, ref panel, sender);
		ToolStrip toolStrip = sender.ToolStrip;
		Point location = panel.PointToClient(sender.Location);
		DockToolStrip(panel, toolStrip, location);
		sender.Capture = false;
		sender.Close();
		toolStrip.Capture = true;
	}

	public void DockToolStrip(DockStyle dock_style, ToolStrip tool_strip)
	{
		ToolStripPanel panel = null;
		switch (dock_style)
		{
		case DockStyle.Top:
			panel = m_ToolStripContainer.TopToolStripPanel;
			break;
		case DockStyle.Bottom:
			panel = m_ToolStripContainer.BottomToolStripPanel;
			break;
		case DockStyle.Left:
			panel = m_ToolStripContainer.LeftToolStripPanel;
			break;
		case DockStyle.Right:
			panel = m_ToolStripContainer.RightToolStripPanel;
			break;
		}
		DockToolStrip(panel, tool_strip, new Point(0, 0));
	}

	private Rectangle GetToolStripRect(DockStyle dock_style, ref ToolStripPanel panel, FloatingToolStripForm sender)
	{
		Size size = new Size(sender.ToolStrip.Height, sender.ToolStrip.Height);
		Rectangle r = Rectangle.Empty;
		switch (dock_style)
		{
		case DockStyle.Top:
			panel = m_ToolStripContainer.TopToolStripPanel;
			r = new Rectangle(panel.Location, panel.Size);
			r.Height = Math.Max(r.Height, size.Height);
			break;
		case DockStyle.Bottom:
		{
			panel = m_ToolStripContainer.BottomToolStripPanel;
			r = new Rectangle(panel.Location, panel.Size);
			int num2 = size.Height - r.Height;
			if (num2 > 0)
			{
				r.Y -= num2;
				r.Height += num2;
			}
			break;
		}
		case DockStyle.Left:
			panel = m_ToolStripContainer.LeftToolStripPanel;
			r = new Rectangle(panel.Location, panel.Size);
			r.Width = Math.Max(r.Width, size.Width);
			break;
		case DockStyle.Right:
		{
			panel = m_ToolStripContainer.RightToolStripPanel;
			r = new Rectangle(panel.Location, panel.Size);
			int num = size.Width - r.Width;
			if (num > 0)
			{
				r.X -= num;
				r.Width += num;
			}
			break;
		}
		}
		return RectangleToScreen(r);
	}

	private void DockToolStrip(ToolStripPanel panel, ToolStrip tool_strip, Point location)
	{
		tool_strip.GripStyle = ToolStripGripStyle.Visible;
		tool_strip.EndDrag += ToolStripEndDrag;
		panel.Controls.Add(tool_strip);
		tool_strip.Location = location;
	}

	private static bool MouseIn(Control control)
	{
		Rectangle r = new Rectangle(control.Location, control.Size);
		if (control.Parent != null)
		{
			r = control.Parent.RectangleToScreen(r);
		}
		return r.Contains(Control.MousePosition);
	}

	private void ToolStripEndDrag(object sender, EventArgs e)
	{
		if (!MouseIn(m_ToolStripContainer.TopToolStripPanel) && !MouseIn(m_ToolStripContainer.BottomToolStripPanel) && !MouseIn(m_ToolStripContainer.LeftToolStripPanel) && !MouseIn(m_ToolStripContainer.RightToolStripPanel))
		{
			ToolStrip toolStrip = (ToolStrip)sender;
			toolStrip.EndDrag -= ToolStripEndDrag;
			AddFloating(toolStrip, Control.MousePosition);
		}
	}

	private void ToolStripDisposed(object sender, EventArgs e)
	{
		ToolStrip toolStrip = (ToolStrip)sender;
		if (toolStrip.Parent is FloatingToolStripForm)
		{
			((FloatingToolStripForm)toolStrip.Parent).Close();
		}
		m_ToolStrips.Remove(toolStrip);
	}

	private Point GetNextMDIChildLocation()
	{
		Point nextMDIChildLocation = m_NextMDIChildLocation;
		int titleBarHeight = MDIForm.TitleBarHeight;
		m_NextMDIChildLocation = new Point(m_NextMDIChildLocation.X + titleBarHeight, m_NextMDIChildLocation.Y + titleBarHeight);
		if (m_NextMDIChildLocation.X > m_MDIPanel.Width - 20 || m_NextMDIChildLocation.Y > m_MDIPanel.Height - 20)
		{
			m_NextMDIChildLocation = new Point(0, 0);
		}
		return nextMDIChildLocation;
	}

	public void AddMdiChild(Control control)
	{
		Size size = control.Size;
		if (control is Form)
		{
			_ = ((Form)control).WindowState;
		}
		DockPanel dockPanel = control as DockPanel;
		if (dockPanel == null)
		{
			dockPanel = WrapControlWithDockPanel(control);
		}
		MDIForm mDIForm = new MDIForm(dockPanel, this);
		HookMDIForm(mDIForm);
		mDIForm.SuspendLayout();
		mDIForm.SetClientSize(size);
		mDIForm.Location = GetNextMDIChildLocation();
		m_MDIPanel.Add(mDIForm);
		if (m_MDIPanel.Maximised)
		{
			mDIForm.SetAsMaximised();
		}
		mDIForm.BringToFront();
		mDIForm.ResumeLayout();
	}

	private void MdiFormCloseButtonClicked(MDIForm mdi_form)
	{
		TryCloseMDIForm(mdi_form);
	}

	internal bool TryCloseMDIForm(MDIForm mdi_form)
	{
		bool cancel = false;
		if (this.MDIFormClosing != null)
		{
			List<Control> allControls = mdi_form.GetAllControls();
			this.MDIFormClosing(allControls, ref cancel);
		}
		if (!cancel)
		{
			CloseMDIForm(mdi_form);
		}
		return !cancel;
	}

	private Control GetActivatedMDIControl()
	{
		return GetActiveMDIForm()?.GetActiveControl();
	}

	private void CloseMDIForm(MDIForm mdi_form)
	{
		bool activated = mdi_form.Activated;
		UnhookMDIForm(mdi_form);
		m_MDIPanel.Remove(mdi_form);
		mdi_form.Dispose();
		if (activated)
		{
			Control activatedMDIControl = GetActivatedMDIControl();
			if (this.ActiveControlChanged != null)
			{
				this.ActiveControlChanged(activatedMDIControl);
			}
		}
	}

	private void HookMDIForm(MDIForm mdi_form)
	{
		mdi_form.MaximiseButtonClicked += MDIFormMaximiseButtonClicked;
		mdi_form.CloseButtonClicked += MdiFormCloseButtonClicked;
	}

	private void UnhookMDIForm(MDIForm mdi_form)
	{
		mdi_form.MaximiseButtonClicked -= MDIFormMaximiseButtonClicked;
		mdi_form.CloseButtonClicked -= MdiFormCloseButtonClicked;
	}

	private MDIPanel CreateMDIPanel()
	{
		m_MDIPanel = new MDIPanel(this);
		m_MDIPanel.Disposed += MDIPanelDisposed;
		m_MDIPanel.BackColor = SystemColors.AppWorkspace;
		m_MDIPanel.Name = "MDIPanel";
		return m_MDIPanel;
	}

	private void MDIPanelDisposed(object sender, EventArgs e)
	{
		m_InternalControls.Remove(m_MDIPanel);
		m_MDIPanel.Disposed -= MDIPanelDisposed;
		m_MDIPanel = null;
	}

	private DockPanel FindDockPanel(Control control)
	{
		foreach (DockPanel allDockPanel in GetAllDockPanels())
		{
			if (allDockPanel.ContainsControl(control))
			{
				return allDockPanel;
			}
		}
		return null;
	}

	public void SetSize(Control control, Size size)
	{
		DockPanel dockPanel = FindDockPanel(control);
		if (dockPanel != null)
		{
			DockPanel dockPanel2 = (DockPanel)dockPanel.Parent;
			while (dockPanel2 != null && dockPanel2.SplitMode != SplitMode.Horz)
			{
				dockPanel2 = dockPanel2.Parent as DockPanel;
			}
			SetSize(control, size, dockPanel2);
			DockPanel dockPanel3 = (DockPanel)dockPanel.Parent;
			while (dockPanel3 != null && dockPanel3.SplitMode != SplitMode.Vert)
			{
				dockPanel3 = dockPanel3.Parent as DockPanel;
			}
			SetSize(control, size, dockPanel3);
		}
	}

	private void SetSize(Control control, Size size, DockPanel dock_panel)
	{
		if (dock_panel != null)
		{
			bool flag = dock_panel.DockPanel1.FindDockPanelRecursive(control) != null;
			switch (dock_panel.SplitMode)
			{
			case SplitMode.Vert:
			{
				int splitSize2 = (flag ? size.Width : (dock_panel.ClientSize.Width - size.Width));
				dock_panel.SetSplitSize(splitSize2);
				break;
			}
			case SplitMode.Horz:
			{
				int splitSize = (flag ? size.Height : (dock_panel.ClientSize.Height - size.Height));
				dock_panel.SetSplitSize(splitSize);
				break;
			}
			}
		}
	}

	public void FloatAndDragControl(Control control)
	{
		DockPanel dockPanel = FindDockPanel(control);
		if (dockPanel != null)
		{
			FloatAndDragPanel(dockPanel);
		}
	}

	public void CloseControl(Control control)
	{
		FindDockPanel(control)?.CloseControl(control);
	}

	internal void OnLeftMouseButtonDownGlobal()
	{
		if (m_MDIPanel == null || m_MDIPanel.Maximised)
		{
			return;
		}
		bool flag = false;
		Control control = null;
		FloatingForm topMostForm = GetTopMostForm(Cursor.Position, null);
		if (topMostForm != null)
		{
			topMostForm.SelectPanel(Cursor.Position);
			control = topMostForm;
		}
		else if (m_MDIPanel != null)
		{
			foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
			{
				Point pt = mDIForm.PointToClient(Cursor.Position);
				if (mDIForm.ClientRectangle.Contains(pt))
				{
					control = mDIForm;
					mDIForm.ActivateForm();
					if (m_MDIPanel.Maximised)
					{
						BringMaximisedMDIFormToFront(mDIForm);
					}
					else
					{
						mDIForm.BringToFront();
					}
					break;
				}
			}
			if (control == null)
			{
				flag = true;
				m_RootPanel.SelectPanel(Cursor.Position);
			}
		}
		foreach (FloatingForm floatingForm2 in m_FloatingForms)
		{
			if (floatingForm2 is FloatingForm floatingForm && floatingForm != control)
			{
				floatingForm.DeselectPanel();
			}
		}
		if (m_MDIPanel != null)
		{
			foreach (MDIForm mDIForm2 in m_MDIPanel.MDIForms)
			{
				if (mDIForm2 != control)
				{
					mDIForm2.DeactivateForm();
				}
			}
		}
		if (!flag)
		{
			m_RootPanel.DeselectPanel();
		}
	}

	private List<DockPanel> GetAllDockPanels()
	{
		List<DockPanel> list = new List<DockPanel>();
		GetAllDockPanels(m_RootPanel, list);
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			GetAllDockPanels(floatingForm.DockPanel, list);
		}
		foreach (MDIForm mDIForm in m_MDIPanel.MDIForms)
		{
			GetAllDockPanels(mDIForm.DockPanel, list);
		}
		return list;
	}

	private void GetAllDockPanels(DockPanel dock_panel, List<DockPanel> panels)
	{
		panels.Add(dock_panel);
		if (dock_panel.DockPanel1 != null)
		{
			GetAllDockPanels(dock_panel.DockPanel1, panels);
		}
		if (dock_panel.DockPanel2 != null)
		{
			GetAllDockPanels(dock_panel.DockPanel2, panels);
		}
	}

	public void SetControlDockOptions(Control control, ControlDockOptions options)
	{
		m_ControlDockOptions[control] = options;
	}

	public ControlDockOptions GetControlDockOptions(Control control)
	{
		ControlDockOptions value = default(ControlDockOptions);
		m_ControlDockOptions.TryGetValue(control, out value);
		return value;
	}

	private static MDIForm GetMDIForm(Control control)
	{
		for (Control control2 = control; control2 != null; control2 = control2.Parent)
		{
			if (control2 is MDIForm result)
			{
				return result;
			}
		}
		return null;
	}

	public void SetActiveControl(Control control)
	{
		MDIForm mDIForm = GetMDIForm(control);
		if (mDIForm != null)
		{
			if (MDIMaximised)
			{
				BringMaximisedMDIFormToFront(mDIForm);
			}
			else
			{
				mDIForm.BringToFront();
			}
			mDIForm.SetActiveControl(control);
		}
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			if (floatingForm.ClientRectangle.Contains(floatingForm.PointToClient(pt)))
			{
				return floatingForm.GetControlAtScreenPoint(pt);
			}
		}
		if (m_MDIPanel.ClientRectangle.Contains(m_MDIPanel.PointToClient(pt)))
		{
			return m_MDIPanel.GetControlAtScreenPoint(pt);
		}
		return m_RootPanel.GetControlAtScreenPoint(pt);
	}

	public Control GetActiveControl()
	{
		foreach (FloatingForm floatingForm in m_FloatingForms)
		{
			if (floatingForm.ContainsFocus)
			{
				Control activeControl = floatingForm.GetActiveControl();
				if (activeControl != null)
				{
					return activeControl;
				}
			}
		}
		if (m_MDIPanel != null)
		{
			return m_MDIPanel.GetActiveControl();
		}
		m_RootPanel.GetActiveControl();
		return null;
	}

	internal void OnActiveControlChanged(Control control)
	{
		if (this.ActiveControlChanged != null)
		{
			this.ActiveControlChanged(control);
		}
	}

	internal void OnActiveMDIFormChanged(MDIForm mdi_form)
	{
		OnActiveControlChanged(mdi_form.GetActiveControl());
	}

	internal void OnControlTextChanged(Control control)
	{
		GetMDIForm(control)?.OnControlTextChanged(control);
	}
}
