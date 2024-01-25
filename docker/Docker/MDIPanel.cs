using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class MDIPanel : Panel
{
	private DockManager m_DockManager;

	private MDITabs m_MDITabs = new MDITabs();

	private Panel m_MainPanel = new Panel();

	private List<MDIForm> m_MDIForms = new List<MDIForm>();

	private bool m_Maximised;

	private DockOptionControl m_DockOptionMDI;

	private static Region m_DockOptionMDIRegion = Utils.BitmapToRegion(Resource.DockOptionMDI, Color.FromArgb(255, 255, 0, 255));

	private bool m_DockOptionsActive;

	private const float m_MinDockoptionControlOpacity = 0.7f;

	private DockStyle m_TargetDockStyle;

	public bool Maximised
	{
		get
		{
			return m_Maximised;
		}
		set
		{
			m_Maximised = value;
			UpdateTabsVisibility();
		}
	}

	public ICollection<MDIForm> MDIForms => m_MDIForms;

	public MDIForm ActiveForm
	{
		get
		{
			if (m_MainPanel.Controls.Count == 0)
			{
				return null;
			}
			return (MDIForm)m_MainPanel.Controls[0];
		}
	}

	public MDITabs MDITabs => m_MDITabs;

	public DockStyle TargetDockStyle => m_TargetDockStyle;

	public MDIPanel(DockManager dock_manager)
	{
		m_DockManager = dock_manager;
		m_MainPanel.Dock = DockStyle.Fill;
		m_MainPanel.AutoScroll = true;
		m_MainPanel.BackColor = SystemColors.AppWorkspace;
		base.Controls.Add(m_MainPanel);
		m_MDITabs.Dock = DockStyle.Top;
		m_MDITabs.Visible = false;
		base.Controls.Add(m_MDITabs);
	}

	public void Add(MDIForm form)
	{
		if (m_Maximised)
		{
			form.Dock = DockStyle.Fill;
		}
		m_MainPanel.Controls.Add(form);
		m_MDIForms.Insert(0, form);
		m_DockManager.BringMaximisedMDIFormToFront(form);
		SelectForm(form);
		m_MDITabs.UpdateTabs();
		UpdateTabsVisibility();
	}

	public void Remove(MDIForm form)
	{
		form.Dock = DockStyle.None;
		m_MainPanel.Controls.Remove(form);
		m_MDIForms.Remove(form);
		form.Dispose();
		m_MDITabs.UpdateTabs();
		UpdateTabsVisibility();
	}

	private void UpdateTabsVisibility()
	{
		m_MDITabs.Visible = m_Maximised && m_MDIForms.Count != 0;
	}

	public void Read(DockManager dock_manager, XmlReadStream read_stream, ICollection<Control> controls)
	{
		read_stream.Read("Maximised", ref m_Maximised);
		if (read_stream.StartElement("MDIForms"))
		{
			for (int i = 0; i < read_stream.Count; i++)
			{
				read_stream.StartElement(i);
				MDIForm mDIForm = new MDIForm(dock_manager);
				mDIForm.Read(read_stream, controls);
				read_stream.EndElement();
				Add(mDIForm);
			}
			read_stream.EndElement();
		}
		m_MDITabs.Read(read_stream);
		m_MDITabs.Visible = m_Maximised;
	}

	public void Write(XmlWriteStream write_stream)
	{
		write_stream.Write("Maximised", m_Maximised);
		write_stream.StartElement("MDIForms");
		foreach (MDIForm control in m_MainPanel.Controls)
		{
			write_stream.StartElement("MDIForm");
			control.Write(write_stream);
			write_stream.EndElement();
		}
		write_stream.EndElement();
		m_MDITabs.Write(write_stream);
	}

	public void FloatMaximisedForm(MDIForm form, int x)
	{
		Point screen_pos = PointToScreen(new Point(x, 0));
		m_DockManager.FloatAndDragPanel(form.DockPanel, screen_pos);
		m_MainPanel.Controls.Remove(form);
		m_MDIForms.Remove(form);
		m_MDITabs.UpdateTabs();
		if (m_MainPanel.Controls.Count == 0)
		{
			Maximised = false;
		}
	}

	private void SelectForm(MDIForm form)
	{
		MDIForm selectedMDIForm = GetSelectedMDIForm();
		if (form != selectedMDIForm)
		{
			selectedMDIForm?.DeactivateForm();
			form.ActivateForm();
		}
	}

	public MDIForm GetSelectedMDIForm()
	{
		foreach (Control control in m_MainPanel.Controls)
		{
			if (control is MDIForm { Activated: not false } mDIForm)
			{
				return mDIForm;
			}
		}
		return null;
	}

	public void SelectNextMDIForm()
	{
		for (int i = 0; i < m_MDIForms.Count; i++)
		{
			MDIForm mDIForm = m_MDIForms[i];
			if (mDIForm.Activated)
			{
				mDIForm.DeactivateForm();
				MDIForm mDIForm2 = m_MDIForms[(i + 1) % m_MDIForms.Count];
				mDIForm2.ActivateForm();
				if (m_MDITabs.Visible)
				{
					m_MDITabs.ActivateMDIForm(mDIForm2);
				}
				break;
			}
		}
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		if (!(m_MainPanel.GetChildAtPoint(m_MainPanel.PointToClient(pt)) is MDIForm mDIForm))
		{
			return null;
		}
		return mDIForm.GetControlAtScreenPoint(pt);
	}

	public Control GetActiveControl()
	{
		foreach (MDIForm mDIForm in m_MDIForms)
		{
			if (mDIForm.Activated)
			{
				return mDIForm.GetActiveControl();
			}
		}
		return null;
	}

	public void HandleDragWindowMouseMove(DockPanel moving_panel, Point screen_pt)
	{
		ShowDockOptions(b: true, moving_panel);
	}

	public void RemoveDockOptionControls()
	{
		ShowDockOptions(b: false, null);
		ShowTargetDockPanel(DockStyle.None, null);
	}

	public void ShowDockOptions(bool b, DockPanel moving_panel)
	{
		if (m_DockOptionsActive != b)
		{
			m_DockOptionsActive = b;
			if (b)
			{
				Point pos = default(Point);
				Point point = PointToScreen(new Point(0, 0));
				m_DockOptionMDI = new DockOptionControl(this, moving_panel, Resource.DockOptionMDI, m_DockOptionMDIRegion);
				m_DockOptionMDI.DockTargetActivated += DockOptionMDIDockTargetActivated;
				pos.X = point.X + (base.Width - m_DockOptionMDI.Width) / 2;
				pos.Y = point.Y + (base.Height - m_DockOptionMDI.Height) / 2;
				m_DockManager.AddDockOption(m_DockOptionMDI, pos);
			}
			else if (m_DockOptionMDI != null)
			{
				m_DockManager.RemoveDockOption(m_DockOptionMDI);
				m_DockOptionMDI = null;
			}
		}
	}

	private void DockOptionMDIDockTargetActivated(bool mouse_over, Point mouse_pos, DockPanel moving_panel)
	{
		ShowTargetDockPanel(mouse_over ? DockStyle.Fill : DockStyle.None, moving_panel);
	}

	private void ShowTargetDockPanel(DockStyle dock_style, DockPanel moving_panel)
	{
		if (m_TargetDockStyle != dock_style)
		{
			if (dock_style == DockStyle.None)
			{
				m_TargetDockStyle = DockStyle.None;
				m_DockManager.RemoveDockTargetPanel();
				return;
			}
			SuspendLayout();
			m_TargetDockStyle = dock_style;
			Point p = PointToClient(moving_panel.PointToScreen(new Point(0, 0)));
			Size size = moving_panel.Size;
			p = PointToScreen(p);
			m_DockManager.SetDockTargetPanel(p, size);
			ResumeLayout();
		}
	}

	public void SetDockOptionControlOpacity(double opacity)
	{
		opacity = Math.Min(opacity, 0.699999988079071);
		if (m_DockOptionMDI != null)
		{
			m_DockOptionMDI.TargetOpacity = opacity;
		}
	}
}
