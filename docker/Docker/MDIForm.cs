using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class MDIForm : Control
{
	public delegate void MaximiseButtonClickedHandler();

	public delegate void CloseButtonClickedHandler(MDIForm form);

	public enum MDIWindowState
	{
		Invalid,
		Normal,
		Maximised,
		Minimised
	}

	private DockManager m_DockManager;

	private DockPanel m_RootPanel;

	private const int m_BorderWidth = 6;

	private const int m_TitleBarHeight = 24;

	private const int m_MinWidth = 100;

	private MDIFormTitleText m_MDIFormTitleText = new MDIFormTitleText();

	private bool m_ResizeUp;

	private bool m_ResizeDown;

	private bool m_ResizeLeft;

	private bool m_ResizeRight;

	private bool m_Resizing;

	private bool m_Activated;

	private const int m_ResizeCornerSize = 12;

	private CloseButton m_CloseButton = new CloseButton();

	private MaximiseButton m_MaximiseButton = new MaximiseButton();

	private MinimiseButton m_MinimiseButton = new MinimiseButton();

	private RestoreButton m_RestoreButton = new RestoreButton();

	private const int m_GapBetweenWindowButtons = 2;

	private Rectangle m_NormalRect;

	private MDIWindowState m_PreMaximiseState;

	private Rectangle m_PreMaximiseRect;

	private Point m_LastMousePt;

	private MDIWindowState m_WindowState;

	private bool m_Dragging;

	private const int m_ChildAtEdgeBreakAwayTime = 500;

	private bool m_MDIChildAtEdge;

	private int m_MDIChildAtEdgeStartTime;

	private static Color m_ActiveTextColour = Color.Black;

	private static Color m_InactiveTextColour = Color.FromArgb(90, 90, 90);

	private static Color m_ActiveBorderColour = Color.FromArgb(220, 220, 255);

	private static Color m_InactiveBorderColour = Color.FromArgb(195, 195, 195);

	public bool Activated
	{
		get
		{
			return m_Activated;
		}
		set
		{
			m_Activated = value;
			BackColor = (value ? m_ActiveBorderColour : m_InactiveBorderColour);
		}
	}

	private bool HoverResize
	{
		get
		{
			if (!m_ResizeUp && !m_ResizeDown && !m_ResizeLeft)
			{
				return m_ResizeRight;
			}
			return true;
		}
	}

	public MDIWindowState DockerWindowState => m_WindowState;

	public DockPanel DockPanel => m_RootPanel;

	public static int TitleBarHeight => 30;

	public MDIPanel MDIPanel
	{
		get
		{
			Control control = base.Parent;
			while (!(control is MDIPanel))
			{
				control = control.Parent;
			}
			return (MDIPanel)control;
		}
	}

	public event MaximiseButtonClickedHandler MaximiseButtonClicked;

	public event CloseButtonClickedHandler CloseButtonClicked;

	private MDIForm()
	{
		SetStyle(ControlStyles.UserPaint, value: true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetupWindowButtons();
		SetupTitleText();
		base.Size = new Size(100, 100);
		m_NormalRect = new Rectangle(base.Location, base.Size);
	}

	public MDIForm(DockManager dock_manager)
		: this()
	{
		m_DockManager = dock_manager;
	}

	public MDIForm(DockPanel dock_panel, DockManager dock_manager)
		: this()
	{
		m_DockManager = dock_manager;
		m_RootPanel = dock_panel;
		base.Controls.Add(m_RootPanel);
		Text = m_RootPanel.ActivePanelName;
		SetWindowState(MDIWindowState.Normal);
	}

	private void SetupWindowButtons()
	{
		int num = base.ClientSize.Width - 6 - m_CloseButton.Width;
		int num2 = (30 - m_CloseButton.Height) / 2;
		m_CloseButton.Location = new Point(num, num2);
		m_CloseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		num -= 2 + m_CloseButton.Width;
		m_MaximiseButton.Location = new Point(num, num2);
		m_MaximiseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		num -= 2 + m_MaximiseButton.Width;
		m_MinimiseButton.Location = new Point(num, num2);
		m_MinimiseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		m_RestoreButton.Location = m_MinimiseButton.Location;
		m_RestoreButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		m_CloseButton.Clicked += CloseButtonClickedEvent;
		m_MaximiseButton.Click += MaximiseButtonClick;
		m_MinimiseButton.Click += MinimiseButtonClick;
		m_RestoreButton.Click += RestoreButtonClick;
		base.Controls.Add(m_CloseButton);
		base.Controls.Add(m_MaximiseButton);
		base.Controls.Add(m_MinimiseButton);
		base.Controls.Add(m_RestoreButton);
	}

	private void SetupTitleText()
	{
		LayoutTitleText();
		m_MDIFormTitleText.ActiveColour = m_ActiveTextColour;
		m_MDIFormTitleText.InactiveColour = m_InactiveTextColour;
		m_MDIFormTitleText.BackColor = BackColor;
		m_MDIFormTitleText.MouseDown += TitleTextMouseDown;
		m_MDIFormTitleText.MouseMove += TitleTextMouseMove;
		m_MDIFormTitleText.MouseUp += TitleTextMouseUp;
		base.Controls.Add(m_MDIFormTitleText);
	}

	private void LayoutTitleText()
	{
		m_MDIFormTitleText.Size = new Size(m_MinimiseButton.Left - 12, 23);
		m_MDIFormTitleText.Location = new Point(6, 6);
		m_MDIFormTitleText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
	}

	private void TitleTextMouseDown(object sender, MouseEventArgs e)
	{
		OnMouseDown(Utils.ConvertMouseEventArgs(sender, this, e));
	}

	private void TitleTextMouseUp(object sender, MouseEventArgs e)
	{
		OnMouseUp(Utils.ConvertMouseEventArgs(sender, this, e));
	}

	private void TitleTextMouseMove(object sender, MouseEventArgs e)
	{
		OnMouseMove(Utils.ConvertMouseEventArgs(sender, this, e));
	}

	protected override void OnTextChanged(EventArgs e)
	{
		m_MDIFormTitleText.TitleText = Text;
		m_MDIFormTitleText.Refresh();
		base.OnTextChanged(e);
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		m_MDIFormTitleText.BackColor = BackColor;
		m_MDIFormTitleText.Selected = Activated;
		base.OnBackColorChanged(e);
	}

	private void MaximiseButtonClick(object sender, EventArgs e)
	{
		if (this.MaximiseButtonClicked != null)
		{
			this.MaximiseButtonClicked();
		}
	}

	private void MinimiseButtonClick(object sender, EventArgs e)
	{
		SetWindowState(MDIWindowState.Minimised);
	}

	private void RestoreButtonClick(object sender, EventArgs e)
	{
		SetWindowState(MDIWindowState.Normal);
	}

	private void CloseButtonClickedEvent()
	{
		if (m_RootPanel.TabCount > 1)
		{
			m_RootPanel.CloseCurrentTab();
		}
		else if (this.CloseButtonClicked != null)
		{
			this.CloseButtonClicked(this);
		}
	}

	public override string ToString()
	{
		return "MDIForm: " + m_RootPanel;
	}

	public void SetAsMaximised()
	{
		m_PreMaximiseState = m_WindowState;
		m_PreMaximiseRect = new Rectangle(base.Location, base.Size);
		SetWindowState(MDIWindowState.Maximised);
	}

	public void SetAsRestored()
	{
		SuspendLayout();
		SetWindowState(m_PreMaximiseState);
		base.Location = m_PreMaximiseRect.Location;
		base.Size = m_PreMaximiseRect.Size;
		ResumeLayout();
	}

	private void SetWindowState(MDIWindowState state)
	{
		if (m_WindowState != state)
		{
			m_WindowState = state;
			switch (state)
			{
			case MDIWindowState.Normal:
				Restore();
				break;
			case MDIWindowState.Maximised:
				Maximise();
				break;
			case MDIWindowState.Minimised:
				Minimise();
				break;
			}
		}
	}

	private void LayoutRootPanel()
	{
		m_RootPanel.Dock = DockStyle.None;
		m_RootPanel.Location = new Point(6, 30);
		m_RootPanel.Size = new Size(base.ClientSize.Width - 12, base.ClientSize.Height - 12 - 24);
		m_RootPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
	}

	private void Restore()
	{
		SuspendLayout();
		Dock = DockStyle.None;
		Rectangle normalRect = m_NormalRect;
		base.Location = normalRect.Location;
		base.Size = normalRect.Size;
		ShowWindowButtons(value: true);
		base.Visible = true;
		LayoutRootPanel();
		m_MDIFormTitleText.Visible = true;
		ResumeLayout();
	}

	private void Maximise()
	{
		SuspendLayout();
		Dock = DockStyle.Fill;
		ShowWindowButtons(value: false);
		base.Visible = base.Parent == null || this == base.Parent.Controls[0];
		m_RootPanel.Anchor = AnchorStyles.None;
		m_RootPanel.Dock = DockStyle.Fill;
		m_MDIFormTitleText.Visible = false;
		ResumeLayout();
	}

	private void Minimise()
	{
		SuspendLayout();
		ShowWindowButtons(value: true);
		Dock = DockStyle.None;
		base.Visible = true;
		base.Size = new Size(m_MDIFormTitleText.TextWidth + 10 + (m_CloseButton.Right - m_MinimiseButton.Left), TitleBarHeight);
		base.Location = new Point(GetNextMinimiseX(), base.Parent.Height - base.Size.Height - 1);
		m_MinimiseButton.Visible = false;
		m_RestoreButton.Visible = true;
		LayoutRootPanel();
		m_MDIFormTitleText.Visible = true;
		ResumeLayout();
	}

	private int GetNextMinimiseX()
	{
		int num = 0;
		foreach (MDIForm control in base.Parent.Controls)
		{
			if (control != this && control.m_WindowState == MDIWindowState.Minimised)
			{
				num = Math.Max(num, control.Right + 1);
			}
		}
		return num;
	}

	private void ShowWindowButtons(bool value)
	{
		m_CloseButton.Visible = value;
		m_MaximiseButton.Visible = value;
		m_MinimiseButton.Visible = value;
		m_RestoreButton.Visible = false;
	}

	public void Read(XmlReadStream read_stream, ICollection<Control> controls)
	{
		SuspendLayout();
		base.Location = Utils.ReadPoint(read_stream, "Location", base.Location);
		base.Size = Utils.ReadSize(read_stream, "Size", base.Size);
		SetupWindowButtons();
		LayoutTitleText();
		m_NormalRect = Utils.ReadRect(read_stream, "NormalRect", new Rectangle(base.Location, base.Size));
		MDIWindowState value = MDIWindowState.Normal;
		read_stream.ReadEnum("WindowState", ref value);
		read_stream.ReadEnum("PreMaximiseState", ref m_PreMaximiseState);
		m_PreMaximiseRect = Utils.ReadRect(read_stream, "PreMaximiseRect", m_NormalRect);
		m_RootPanel = new DockPanel(m_DockManager);
		base.Controls.Add(m_RootPanel);
		if (read_stream.StartElement("RootPanel"))
		{
			m_RootPanel.Read(read_stream, controls);
			read_stream.EndElement();
		}
		SetWindowState(value);
		Text = m_RootPanel.ActivePanelName;
		ResumeLayout();
	}

	public void Write(XmlWriteStream write_stream)
	{
		Utils.Write(write_stream, "Location", base.Location);
		Utils.Write(write_stream, "Size", base.Size);
		Utils.Write(write_stream, "NormalRect", m_NormalRect);
		write_stream.Write("PreMaximiseState", m_PreMaximiseState);
		Utils.Write(write_stream, "PreMaximiseRect", m_PreMaximiseRect);
		write_stream.Write("WindowState", m_WindowState);
		m_RootPanel.Write("RootPanel", write_stream);
	}

	public bool ActivateForm()
	{
		if (!Activated)
		{
			Activated = true;
			m_DockManager.OnActiveMDIFormChanged(this);
			Refresh();
		}
		return m_RootPanel.SelectPanel(Cursor.Position);
	}

	public void DeactivateForm()
	{
		m_RootPanel.DeselectPanel();
		Activated = false;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			ActivateForm();
			if (HoverResize)
			{
				m_Resizing = true;
				base.Capture = true;
				m_LastMousePt = PointToScreen(e.Location);
			}
			else if (e.Y < 24)
			{
				base.Capture = true;
				m_Dragging = true;
				m_LastMousePt = PointToScreen(e.Location);
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		Point pt = e.Location;
		if (m_Resizing || m_Dragging)
		{
			Rectangle r = base.Parent.RectangleToScreen(base.Parent.ClientRectangle);
			r = RectangleToClient(r);
			pt = new Point(Misc.Clamp(pt.X, r.Left, r.Right), Misc.Clamp(pt.Y, r.Top, r.Bottom));
		}
		if (m_Resizing)
		{
			HandleMDIResizeMouseMove(pt);
		}
		else if (m_Dragging)
		{
			HandleMDIDragMouseMove(pt, e.Location);
		}
		else if (m_WindowState == MDIWindowState.Normal)
		{
			UpdateResizeCursor(e.Location);
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			StopDragging();
		}
		base.OnMouseUp(e);
	}

	private void HandleMDIResizeMouseMove(Point pt)
	{
		pt = PointToScreen(pt);
		int num = pt.X - m_LastMousePt.X;
		int num2 = pt.Y - m_LastMousePt.Y;
		m_LastMousePt = pt;
		int num3 = base.Location.X;
		_ = base.Location.Y;
		int val = base.Width;
		int val2 = base.Height;
		int num4 = base.Location.X;
		int num5 = base.Location.Y;
		int num6 = base.Width;
		int num7 = base.Height;
		if (m_ResizeRight)
		{
			num6 += num;
		}
		if (m_ResizeDown)
		{
			num7 += num2;
		}
		if (m_ResizeLeft)
		{
			num4 += num;
			num6 -= num;
		}
		if (m_ResizeUp)
		{
			num5 += num2;
			num7 -= num2;
		}
		num6 = Math.Max(num6, 100);
		num7 = Math.Max(num7, TitleBarHeight);
		Invalidate(new Rectangle(Math.Min(val, num6) - 6, 0, 6, base.Height));
		Invalidate(new Rectangle(0, Math.Min(val2, num7) - 6, base.Width, 6));
		if (num4 != num3)
		{
			Invalidate(new Rectangle(m_MinimiseButton.Left - 1, m_MinimiseButton.Top, m_CloseButton.Right - (m_MinimiseButton.Left - 1), m_MinimiseButton.Height));
		}
		base.Bounds = new Rectangle(num4, num5, num6, num7);
		Update();
	}

	private void HandleMDIDragMouseMove(Point pt, Point unclamped_pt)
	{
		pt = PointToScreen(pt);
		int num = pt.X - m_LastMousePt.X;
		int num2 = pt.Y - m_LastMousePt.Y;
		m_LastMousePt = pt;
		base.Location = new Point(base.Location.X + num, base.Location.Y + num2);
		base.Parent.Update();
		m_DockManager.HandleDragWindowMouseMove(m_RootPanel, Cursor.Position);
		if (CursorAtEdgeOfMDIClient(unclamped_pt))
		{
			if (!m_MDIChildAtEdge)
			{
				m_MDIChildAtEdge = true;
				m_MDIChildAtEdgeStartTime = Environment.TickCount;
			}
			else if (Environment.TickCount - m_MDIChildAtEdgeStartTime > 500)
			{
				Point point = base.Parent.PointToScreen(base.Location);
				Point point2 = new Point(pt.X - point.X, pt.Y - point.Y);
				FloatingForm floatingForm = m_DockManager.FloatMDIForm(this);
				floatingForm.ClientSize = m_NormalRect.Size;
				floatingForm.Location = new Point(Cursor.Position.X - point2.X, Cursor.Position.Y - point2.Y);
				floatingForm.SetDragging();
				m_MDIChildAtEdge = false;
			}
		}
		else
		{
			m_MDIChildAtEdge = false;
		}
	}

	private bool CursorAtEdgeOfMDIClient(Point pt)
	{
		pt = PointToScreen(pt);
		return !base.Parent.RectangleToScreen(base.Parent.ClientRectangle).Contains(pt);
	}

	private void UpdateResizeCursor(Point pt)
	{
		if (pt.X < 6 || pt.X > base.ClientSize.Width - 6 || pt.Y < 6 || pt.Y > base.ClientSize.Height - 6)
		{
			int num = 12;
			m_ResizeUp = pt.Y < num;
			m_ResizeDown = pt.Y > base.ClientSize.Height - num;
			m_ResizeLeft = pt.X < num;
			m_ResizeRight = pt.X > base.ClientSize.Width - num;
		}
		else
		{
			m_ResizeUp = false;
			m_ResizeDown = false;
			m_ResizeLeft = false;
			m_ResizeRight = false;
		}
		SetResizeCursor();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		StopDragging();
		base.OnLostFocus(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		StopDragging();
		base.OnMouseLeave(e);
	}

	private void SetResizeCursor()
	{
		if ((m_ResizeUp && m_ResizeLeft) || (m_ResizeDown && m_ResizeRight))
		{
			Cursor = Cursors.SizeNWSE;
		}
		else if ((m_ResizeUp && m_ResizeRight) || (m_ResizeDown && m_ResizeLeft))
		{
			Cursor = Cursors.SizeNESW;
		}
		else if (m_ResizeUp || m_ResizeDown)
		{
			Cursor = Cursors.SizeNS;
		}
		else if (m_ResizeLeft || m_ResizeRight)
		{
			Cursor = Cursors.SizeWE;
		}
		else
		{
			Cursor = Cursors.Default;
		}
	}

	private void StopDragging()
	{
		if (m_Resizing)
		{
			m_Resizing = false;
			base.Capture = false;
		}
		else if (m_Dragging)
		{
			m_Dragging = false;
			base.Capture = false;
			m_MDIChildAtEdge = false;
			OnDraggingStopped();
		}
		Cursor = Cursors.Default;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (m_WindowState == MDIWindowState.Normal || m_WindowState == MDIWindowState.Minimised)
		{
			PaintMDIFloating(e.Graphics);
		}
		base.OnPaint(e);
	}

	private void PaintBackground(Graphics graphics)
	{
		Brush brush = new SolidBrush(BackColor);
		Rectangle rectangle = new Rectangle(m_RootPanel.Location, m_RootPanel.Size);
		Rectangle rect = new Rectangle(1, 1, base.Width - 2, rectangle.Y - 2);
		graphics.FillRectangle(brush, rect);
		Rectangle rect2 = new Rectangle(1, rectangle.Y - 1, 4, rectangle.Height + 2);
		graphics.FillRectangle(brush, rect2);
		Rectangle rect3 = new Rectangle(rectangle.Right + 1, rectangle.Y - 1, 4, rectangle.Height + 2);
		graphics.FillRectangle(brush, rect3);
		Rectangle rect4 = new Rectangle(1, rectangle.Bottom + 1, base.Width - 2, base.Height - rectangle.Bottom - 2);
		graphics.FillRectangle(brush, rect4);
	}

	private void PaintMDIFloating(Graphics graphics)
	{
		if (!Activated)
		{
			_ = SystemBrushes.InactiveCaptionText;
		}
		else
		{
			_ = SystemBrushes.ActiveCaptionText;
		}
		graphics.DrawRectangle(SystemPens.ControlDarkDark, 0, 0, base.ClientSize.Width - 1, base.ClientSize.Height - 1);
		Rectangle rect = new Rectangle(5, 29, base.ClientSize.Width - 12 + 1, base.ClientSize.Height - 12 - 24 + 1);
		graphics.DrawRectangle(SystemPens.ControlDarkDark, rect);
		PaintBackground(graphics);
	}

	private void OnDraggingStopped()
	{
		m_DockManager.DockToDockTarget(this);
		m_DockManager.RemoveDockOptionControls();
	}

	private void HandleFloatingDrag(Point pt)
	{
		pt = PointToScreen(pt);
		int num = pt.X - m_LastMousePt.X;
		int num2 = pt.Y - m_LastMousePt.Y;
		base.Location = new Point(base.Location.X + num, base.Location.Y + num2);
		m_LastMousePt = pt;
	}

	public void SetClientSize(Size client_size)
	{
		Size size = new Size(client_size.Width + 12, client_size.Height + 12 + 24);
		if (m_WindowState == MDIWindowState.Normal)
		{
			base.Size = size;
		}
	}

	protected override void OnResize(EventArgs e)
	{
		UpdateNormalRect();
		base.OnResize(e);
	}

	protected override void OnMove(EventArgs e)
	{
		UpdateNormalRect();
		base.OnMove(e);
	}

	private void UpdateNormalRect()
	{
		if (m_WindowState == MDIWindowState.Normal)
		{
			m_NormalRect = new Rectangle(base.Location, base.Size);
		}
	}

	public Control GetControlAtScreenPoint(Point pt)
	{
		return m_RootPanel.GetControlAtScreenPoint(pt);
	}

	public Control GetActiveControl()
	{
		return m_RootPanel.GetActiveControl();
	}

	public List<Control> GetAllControls()
	{
		List<Control> list = new List<Control>();
		m_RootPanel.GetAllControls(list);
		return list;
	}

	public void OnControlTextChanged(Control control)
	{
		Text = m_RootPanel.ActivePanelName;
	}

	public void SetActiveControl(Control control)
	{
		m_RootPanel.SetActiveControl(control);
	}
}
