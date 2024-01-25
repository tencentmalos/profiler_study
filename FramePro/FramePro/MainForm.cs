using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Docker;
using FramePro.Properties;
////using PureDev.PureDevReg;
////using PureDev.PureDevRegCLR;
////using Registration;
using SCLCoreCLR;

namespace FramePro;

internal class MainForm : Form
{
	////private class RegistrationResults
	////{
	////	public FullVerificationResult m_Result;

	////	public string m_Error;
	////}

	private class ReadThreadContext
	{
		public string m_Filename;

		public Session m_Session;

		public SessionViewSaveData m_SessionViewSaveData = new SessionViewSaveData();
	}

	private class WriteThreadContext
	{
		public Session m_Session;

		public SessionViewSaveData m_SessionViewSaveData;

		public string m_Error;
	}

	private class CloneThreadContext
	{
		public Session m_OriginalSession;

		public Session m_NewSession;

		public long m_StartTime;

		public long m_EndTime;

		public SessionViewSaveData m_SessionViewSaveData;

		public string m_Error;
	}

	private class RecordContextSwitchesContext
	{
		public string m_ADBPath;

		public int m_Duration;

		public bool m_Result;

		public string m_ResultText;
	}

	private static MainForm m_Inst;

	private DockManager m_DockManager;

	private HoverBox m_HoverBox = new HoverBox();

	private Settings m_Settings;

	private CallbackLog m_FrameProCoreLog = new CallbackLog();

	private ControlTaskDispatcher m_ControlTaskDispatcher = new ControlTaskDispatcher();

	private bool m_InitialisedSize;

	private string m_FileToOpenOnStartup;

	private string m_RecordingToPlaybackOnStartup;

	private List<Session> m_Sessions = new List<Session>();

	private List<MainSessionView> m_SessionViews = new List<MainSessionView>();

	private ThreadJob m_UpdateThreadJob;

	private MainSessionView m_ActiveView;

	private Process m_GameSimulatorProcess;

	private Process m_RecordingPlayerProcess;

	private StartupPage m_StartupPage;

	private bool m_OutputWindowVisible = true;

	private List<string> m_OutputWindowLines = new List<string>();

	private const string m_ContextSwitchFileExt = ".framepro_context_switch";

	private const int m_OnScopeColourChangedInterval = 500;

	private const int m_OnCustomStatColourChangedInterval = 500;

	private System.Windows.Forms.Timer m_OnScopeColourChangedTimer = new System.Windows.Forms.Timer();

	private System.Windows.Forms.Timer m_OnCustomStatColourChangedTimer = new System.Windows.Forms.Timer();

	private volatile bool m_VersionUpdateCheckSucceeded;

	private const string m_DownloadInstallerAddress = "https://www.puredevsoftware.com/8358782/FramePro_x64_setup.exe";

	private float m_DPIScale = 1f;

	private IContainer components;

	private FrameProButton m_ConnectButton;

	private FrameProButton m_DisconnectButton;

	private Panel panel1;

	private FrameProButton m_ConnectSettingsButton;

	private Panel m_MainPanel;

	private FrameProButton m_TrackEndButton;

	private FrameProButton m_GotoStartButton;

	private FrameProButton m_GotoEndButton;

	private ConditionalScopeTimeSlider m_ConditionalScopeTimeSlider;

	private FindControl m_FindControl;

	private ViewButton m_FramesViewButton;

	private ViewButton m_CoresViewButton;

	private ViewButton m_ScopeViewButton;

	private ViewButton m_InfoViewButton;

	private FrameProButton m_GotoPrevSpikeButton;

	private FrameProButton m_GotoNextSpikeButton;

	private ToolStripMenuItem fileToolStripMenuItem;

	private ToolStripMenuItem openToolStripMenuItem;

	private ToolStripMenuItem m_SaveMenuItem;

	private ToolStripMenuItem m_SaveAsMenuItem;

	private ToolStripMenuItem closeToolStripMenuItem;

	private ToolStripMenuItem m_ExportToCSVMenuItem;

	private ToolStripSeparator toolStripSeparator2;

	private ToolStripMenuItem m_RecentFilesMenuItem;

	private ToolStripSeparator toolStripSeparator4;

	private ToolStripMenuItem exitToolStripMenuItem;

	private ToolStripMenuItem viewToolStripMenuItem;

	private ToolStripMenuItem m_ThreadsViewMenuItem;

	private ToolStripMenuItem m_ScopesViewMenuItem;

	private ToolStripSeparator toolStripSeparator6;

	private ToolStripMenuItem m_ViewSettingsMenuItem;

	private ToolStripMenuItem m_InfoMenuItem;

	private ToolStripMenuItem m_FrameGraphMenuItem;

	private ToolStripMenuItem m_ThreadsViewCoreViewMenuItem;

	private ToolStripMenuItem connectionToolStripMenuItem;

	private ToolStripMenuItem m_NewConnectionMenuItem;

	private ToolStripMenuItem m_ConnectMenuItem;

	private ToolStripMenuItem m_DisconnectMenuItem;

	private ToolStripMenuItem toolsToolStripMenuItem;

	private ToolStripSeparator toolStripSeparator1;

	private ToolStripMenuItem findToolStripMenuItem1;

	private ToolStripSeparator toolStripSeparator7;

	private ToolStripMenuItem settingsToolStripMenuItem1;

	private ToolStripMenuItem helpToolStripMenuItem;

	private ToolStripMenuItem enterProductKeyToolStripMenuItem;

	private ToolStripMenuItem checkForUpdatesToolStripMenuItem;

	private ToolStripMenuItem aboutToolStripMenuItem;

	private MenuStrip menuStrip1;

	private ToolStripSeparator toolStripSeparator8;

	private ToolStripSeparator toolStripSeparator3;

	private ToolStripMenuItem demoToolStripMenuItem;

	private ToolStripMenuItem launchFrameProGameSimulatorToolStripMenuItem;

	private ToolStripMenuItem playbackDumpFileInRealtimeToolStripMenuItem;

	private ToolStripMenuItem helpToolStripMenuItem1;

	private ToolStripMenuItem showStartupPageToolStripMenuItem;

	private ToolStripSeparator toolStripSeparator9;

	private ToolStripSeparator toolStripSeparator5;

	private ToolStripMenuItem m_CreateSessionFromSelectionMenuItem;

	private ToolStripMenuItem m_CoresViewMenuItem;

	private Panel m_OutputWindowPanel;

	private TextBox m_OutputTextBox;

	private Splitter m_OutputWindowSplitter;

	private ToolStripSeparator toolStripSeparator10;

	private ToolStripMenuItem m_OutputWindowMenuItem;

	private ToolStripMenuItem colouringToolStripMenuItem;

	private ToolStripMenuItem m_ColourByThreadMenuItem;

	private ToolStripMenuItem m_ColourByScopeMenuItem;

	private FrameProButton m_ScopeColourModeButton;

	private ViewButton m_CustomStatsGraphButton;

	private ViewButton m_DataGridViewButton;

	private FrameProButton m_CallstackButton;

	private ToolStripMenuItem m_CustomStatsGraphMenuItem;

	private ToolStripMenuItem m_ScopeGraphMenuItem;

	private ToolStripSeparator toolStripSeparator11;

	private ToolStripMenuItem androidToolStripMenuItem;

	private ToolStripMenuItem recordContextSwitchesAndroidToolStripMenuItem;

	private ToolStripMenuItem loadContextSwitchFileAndroidToolStripMenuItem;

	private FrameProButton m_GotoMaxFrameButton;

	private ToolStripMenuItem m_ExportFrameGraphToCSVMenuItem;

	private ToolStripMenuItem closeAllToolStripMenuItem;

	public static MainForm Inst => m_Inst;

	public static float DPIScale
	{
		get
		{
			if (Inst == null)
			{
				return 1f;
			}
			return Inst.m_DPIScale;
		}
	}

	public SessionView ActiveSessionView
	{
		get
		{
			if (m_ActiveView == null)
			{
				return null;
			}
			return m_ActiveView.ActiveView;
		}
	}

	public HoverBox HoverBox => m_HoverBox;

	private bool HasUnsavedSessions
	{
		get
		{
			foreach (Session session in m_Sessions)
			{
				if (!session.Saved)
				{
					return true;
				}
			}
			return false;
		}
	}

	private Session ActiveSession
	{
		get
		{
			if (ActiveSessionView == null)
			{
				return null;
			}
			return ActiveSessionView.Session;
		}
	}

	public MainForm(Settings settings, string file_to_open)
	{
		m_Inst = this;
		Utils.SetProcessAffinity(settings.FrameProThreadAffinity);
		m_FrameProCoreLog.WriteEvent += FrameProCoreLog;
		m_FrameProCoreLog.DebugWriteEvent += FrameProCoreDebugLog;
		m_DPIScale = (float)base.DeviceDpi / 96f;
		Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
		bool num = !File.Exists(CoreSettings.Path);
		m_Settings = settings;
		if (num)
		{
			m_Settings.SetRegisterUsingPureDevReg(value: true, require_restart: false);
			m_Settings.RegisterUsingGUID = true;
		}
		InitializeComponent();
		SetupOutputWindow();
		InitialiseDockManager();
		UpdateRecentFilesMenuItems();
		UpdateButtonAndMenuItemStates();
		UpdateSaveMenuItemEnabledState();
		OnScopeColourModeChanged();
		m_ConditionalScopeTimeSlider.Initialise(settings.ConditionalScopeTimeSliderMinTime);
		base.Size = settings.MainWindowSize;
		base.WindowState = (settings.MainWindowMaximised ? FormWindowState.Maximized : FormWindowState.Normal);
		m_InitialisedSize = true;
		UpdateSessionViewButtons();
		UpdateMenuItemStates();
		////if (settings.RegisterUsingPureDevReg)
		////{
		////	FrameProCore.Registrar.VerifyInstallComplete += VerifyInstallComplete;
		////	FrameProCore.Registrar.VerifyRegistrationComplete += VerifyRegistrationComplete;
		////	FrameProCore.Registrar.CheckForUpdateComplete += CheckForUpdateComplete;
		////	FrameProCore.Registrar.UnregisterComplete += UnregisterComplete;
		////	FrameProCore.Registrar.Initialise();
		////	FrameProCore.Registrar.CheckForUpdates();
		////}
		UpdateTitle();
		m_OnScopeColourChangedTimer.Tick += OnScopeColourChangedTick;
		m_OnScopeColourChangedTimer.Interval = 500;
		m_OnCustomStatColourChangedTimer.Tick += OnCustomStatColourChangedTick;
		m_OnCustomStatColourChangedTimer.Interval = 500;
		m_FindControl.RecalculateAnchorstoGetAroundDPIBugs();
		if (m_Settings.ShowStartupPage)
		{
			ShowStartupPage();
		}
		if (file_to_open != null)
		{
			m_FileToOpenOnStartup = file_to_open;
		}
	}

	private int ScaleDPI(int value)
	{
		return (int)((float)value * m_DPIScale);
	}

	private void FrameProCoreLog(string text)
	{
		Inst.Log(text);
	}

	private void FrameProCoreDebugLog(string text)
	{
	}

	////private void CheckRegistration()
	////{
	////	if (FrameProCore.Registrar.TrialExpired && !FrameProCore.Registrar.Registered)
	////	{
	////		ShowRegistrationForm(show_in_task_bar: true);
	////	}
	////}

	////private void UnregisterComplete(UnregisterResult result, string p_result_text)
	////{
	////	m_ControlTaskDispatcher.QueueTask(delegate
	////	{
	////		UnregisterComplete_Main();
	////	});
	////}

	////private void UnregisterComplete_Main()
	////{
	////	FrameProCore.Registrar.SetRegisterUsingGUID(m_Settings.RegisterUsingGUID);
	////	UpdateTitle();
	////	CheckRegistration();
	////}

	////private void VerifyInstallComplete(VerifyInstallResult result, string result_text)
	////{
	////	m_ControlTaskDispatcher.QueueTask(delegate
	////	{
	////		UpdateRegistrationSettings();
	////	});
	////}

	////private void VerifyRegistrationComplete(VerifyRegistrationResult result, string result_Text)
	////{
	////	m_ControlTaskDispatcher.QueueTask(delegate
	////	{
	////		VerifyRegistrationComplete_Main(result);
	////	});
	////}

	////private void VerifyRegistrationComplete_Main(VerifyRegistrationResult result)
	////{
	////	VerifyResultHandler.Handle(result, FrameProCore.Registrar);
	////	UpdateRegistrationSettings();
	////}

	////private void CheckForUpdateComplete(bool succeeded, bool update_available, string new_version)
	////{
	////	m_ControlTaskDispatcher.QueueTask(delegate
	////	{
	////		UpdateAvailableEvent_Main(succeeded, update_available, new_version);
	////	});
	////}

	////private void UpdateAvailableEvent_Main(bool succeeded, bool update_available, string new_version)
	////{
	////	FramePro.Update.OnCheckForUpdateComplete(succeeded, update_available, new_version, m_Settings);
	////}

	////private void UpdateRegistrationSettings()
	////{
	////	UpdateTitle();
	////	CheckRegistration();
	////}

	private void SetupOutputWindow()
	{
		if (!m_Settings.OutputWindowVisible)
		{
			ToggleOutputWindow();
		}
		if (m_Settings.OutputWindowHeight != -1)
		{
			m_OutputWindowPanel.Size = new Size(m_OutputWindowPanel.Width, ScaleDPI(m_Settings.OutputWindowHeight));
		}
	}

	public void Log(string message)
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			Log_Main(message);
		});
	}

	public void LogLine(string message)
	{
		Log(message + "\n");
	}

	private void Log_Main(string message)
	{
		SCLCoreCLR.Log.Write(message);
		message = message.Replace("\n", "\r\n");
		if (m_OutputTextBox.Visible)
		{
			bool num = m_OutputTextBox.SelectionStart == m_OutputTextBox.Text.Length;
			m_OutputTextBox.AppendText(message);
			if (num)
			{
				m_OutputTextBox.SelectionStart = m_OutputTextBox.Text.Length;
			}
		}
		else
		{
			m_OutputWindowLines.Add(message);
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (m_GameSimulatorProcess != null && !m_GameSimulatorProcess.HasExited)
			{
				m_GameSimulatorProcess.Kill();
			}
		}
		catch (Exception ex)
		{
			LogLine(ex.Message);
		}
		try
		{
			if (m_RecordingPlayerProcess != null && !m_RecordingPlayerProcess.HasExited)
			{
				m_RecordingPlayerProcess.Kill();
			}
		}
		catch (Exception ex2)
		{
			LogLine(ex2.Message);
		}
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		////if (!m_Settings.RegisterUsingPureDevReg)
		////{
		////	global::Registration.Registration.VerificationCompleteCallback += RegistrationVerificationComplete;
		////	global::Registration.Registration.StartVerification();
		////}
		if (m_FileToOpenOnStartup != null)
		{
			Read(m_FileToOpenOnStartup);
		}
		if (m_RecordingToPlaybackOnStartup != null)
		{
			LaunchRecordingPlayer(m_RecordingToPlaybackOnStartup);
		}
		////if (m_Settings.CheckForUpdates)
		////{
		////	if (m_Settings.NotifyNewVersion)
		////	{
		////		NotifyNewVersion();
		////	}
		////	else if (!m_Settings.RegisterUsingPureDevReg)
		////	{
		////		m_UpdateThreadJob = new ThreadJob(CheckForUpdate);
		////		m_UpdateThreadJob.Run();
		////	}
		////}
		if (DateTime.Now >= new DateTime(2023, 3, 25) && !m_Settings.Shown10xLinkForm)
		{
			new N10xLinkForm().ShowDialog(this);
			m_Settings.Shown10xLinkForm = true;
			m_Settings.Write();
		}
	}

	////private void NotifyNewVersion()
	////{
	////	LogLine("Update available");
	////	if (FramePro.Update.NotifyNewVersion(m_Settings))
	////	{
	////		string error = "";
	////		if (new UpdateInstaller().InstallUpdate(this, FrameProCore.Registrar, "https://www.puredevsoftware.com/8358782/FramePro_x64_setup.exe", ref error))
	////		{
	////			m_Settings.NewVersionAvailable = false;
	////			m_Settings.NotifyNewVersion = false;
	////			m_Settings.LastVersionChecked = "";
	////			m_Settings.Write();
	////		}
	////		else
	////		{
	////			MessageBox.Show("Error: failed to download installer: " + error, "Error downloading");
	////		}
	////	}
	////}

	////private object CheckForUpdate_PureDevReg(object arg, ThreadJobContext context)
	////{
	////	FrameProCore.Registrar.CheckForUpdates();
	////	m_VersionUpdateCheckSucceeded = FrameProCore.Registrar.WaitForCheckForUpdatesCompleted(10000);
	////	return null;
	////}

	////private object CheckForUpdate(object arg, ThreadJobContext context)
	////{
	////	LogLine("Checking for update");
	////	FramePro.Update.CheckForUpdate(m_Settings);
	////	return null;
	////}

	////private void RegistrationVerificationComplete(FullVerificationResult result, string error)
	////{
	////	RegistrationResults results = new RegistrationResults();
	////	results.m_Result = result;
	////	results.m_Error = error;
	////	m_ControlTaskDispatcher.QueueTask(delegate
	////	{
	////		RegistrationVerificationComplete_MainThread(results);
	////	});
	////}

	////private void RegistrationVerificationComplete_MainThread(RegistrationResults results)
	////{
	////	FullVerificationResult result = results.m_Result;
	////	string error = results.m_Error;
	////	SCLCoreCLR.Log.WriteLine("Registration result: " + result);
	////	switch (result)
	////	{
	////	case FullVerificationResult.DemoCheckFailed:
	////	{
	////		string text = "There is a problem with the FramePro installation. Please re-install FramePro.";
	////		if (error != null)
	////		{
	////			text = text + "\n\nDetails:\n" + error;
	////		}
	////		SCLCoreCLR.Log.WriteLine(text);
	////		MessageBox.Show(text);
	////		Environment.Exit(1);
	////		break;
	////	}
	////	case FullVerificationResult.RegistrationCheckFailed:
	////	{
	////		string message = ((error == "invalidated") ? "Your registration has been invalidated because it has moved to another machine. Please register this software again" : ((!(error == "terminated")) ? "Your registration key is not valid. Please register FramePro again." : "Your registration has been terminated. Please contact slynch@puredevsoftware.com"));
	////		MessageBox.Show(message);
	////		SCLCoreCLR.Log.WriteLine(message);
	////		if (error != null)
	////		{
	////			SCLCoreCLR.Log.WriteLine(error);
	////		}
	////		ShowRegistrationForm();
	////		break;
	////	}
	////	case FullVerificationResult.Expired:
	////		ShowRegistrationForm(show_in_task_bar: true);
	////		break;
	////	}
	////	UpdateTitle();
	////}

	private void UpdateTitle()
	{
		Text = "FramePro - 1.10.17 - 学习版(勿传播)";
	}

	private void InitialiseDockManager()
	{
		m_DockManager = new DockManager(this, mdi: true);
		m_DockManager.Dock = DockStyle.Fill;
		m_DockManager.MaximiseMDIForms();
		m_DockManager.ActiveControlChanged += ActiveControlChanged;
		m_DockManager.MDIFormClosing += MDIFormClosing;
		m_MainPanel.Controls.Add(m_DockManager);
	}

	private void MDIFormClosing(ICollection<Control> controls, ref bool cancel)
	{
		if (m_Settings.SaveChangedQuery)
		{
			foreach (Control control in controls)
			{
				if (control is MainSessionView mainSessionView && !mainSessionView.Session.Saved)
				{
					SaveChangesDialog saveChangesDialog = new SaveChangesDialog();
					switch (saveChangesDialog.ShowDialog())
					{
					case DialogResult.Yes:
						SaveSession(mainSessionView.Session);
						break;
					case DialogResult.Cancel:
						cancel = true;
						break;
					}
					if (saveChangesDialog.DontAskAgain)
					{
						m_Settings.SaveChangedQuery = false;
						m_Settings.Write();
					}
				}
				if (cancel || !m_Settings.SaveChangedQuery)
				{
					break;
				}
			}
		}
		foreach (Control control2 in controls)
		{
			if (control2 == m_StartupPage)
			{
				m_StartupPage = null;
			}
		}
	}

	private void ActiveControlChanged(Control control)
	{
		bool flag = false;
		if (control == null)
		{
			if (m_ActiveView != null)
			{
				m_ActiveView = null;
				flag = true;
			}
		}
		else
		{
			MainSessionView mainSessionView = control as MainSessionView;
			if (m_ActiveView != mainSessionView)
			{
				m_ActiveView = mainSessionView;
				flag = true;
			}
		}
		if (flag)
		{
			ActiveViewChanged(ActiveSessionView);
		}
	}

	private void ActiveViewChanged(SessionView new_view)
	{
		UpdateButtonAndMenuItemStates();
		UpdateSessionViewButtons();
		UpdateMenuItemStates();
	}

	private void CreateSessionView(Session session)
	{
		MainSessionView mainSessionView = new MainSessionView(session, m_Settings);
		m_SessionViews.Add(mainSessionView);
		mainSessionView.Disposed += MainSessionViewDisposed;
		mainSessionView.ActiveViewChanged += ActiveViewChanged;
		mainSessionView.TrackEndChanged += SessionViewTrackEndChanged;
		mainSessionView.HighlightedTimeSpanCountChanged += HighlightedTimeSpanCountChanged;
		HookThreadsView(mainSessionView.ThreadsView);
		HookCoresView(mainSessionView.CoresView);
		mainSessionView.HighlightTimeSpans(m_FindControl.FindText);
		m_DockManager.AddMdiChild(mainSessionView);
	}

	private void MainSessionViewDisposed(object sender, EventArgs e)
	{
		m_SessionViews.Remove((MainSessionView)sender);
	}

	private void CloseSessionView(Session session)
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			if (sessionView.Session == session)
			{
				m_DockManager.CloseControl(sessionView);
				break;
			}
		}
	}

	private void HookThreadsView(ThreadsView threads_view)
	{
		threads_view.SectionVisibilityChanged += ViewSectionVisibilityChanged;
	}

	private void HookCoresView(CoresView cores_view)
	{
		cores_view.ViewVisibilityChanged += ViewSectionVisibilityChanged;
	}

	private void HighlightedTimeSpanCountChanged(int count)
	{
		m_FindControl.SetMatchCount(count);
	}

	private void ViewSectionVisibilityChanged()
	{
		UpdateSessionViewButtons();
	}

	private void SessionViewTrackEndChanged()
	{
		UpdatePlayPauseButtonState();
	}

	private void SessionDisconnected()
	{
		m_ControlTaskDispatcher.QueueTask(OnDisconnected);
	}

	protected override void OnDeactivate(EventArgs e)
	{
		m_HoverBox.Visible = false;
		base.OnDeactivate(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		m_HoverBox.Visible = false;
		base.OnLostFocus(e);
	}

	private void ConnectButtonClick(object sender, EventArgs e)
	{
		if (m_Settings.m_FirstConnect)
		{
			ShowConnectionSettingsDialog();
			return;
		}
		DisableCallstackRecording();
		Connect();
	}

	private void UpdateButtonAndMenuItemStates()
	{
		bool flag = ActiveSession != null && ActiveSession.Connected;
		m_ConnectButton.Enabled = !flag;
		m_ConnectSettingsButton.Enabled = !flag;
		m_DisconnectButton.Enabled = flag;
		UpdateSaveMenuItemEnabledState();
		UpdatePlayPauseButtonState();
		bool flag2 = ActiveSession != null;
		m_GotoStartButton.Enabled = flag2;
		m_TrackEndButton.Enabled = flag;
		m_GotoEndButton.Enabled = flag2;
		bool flag3 = ActiveSessionView is ThreadsView || ActiveSessionView is CoresView;
		m_GotoPrevSpikeButton.Enabled = flag2 && flag3;
		m_GotoNextSpikeButton.Enabled = flag2 && flag3;
		m_GotoMaxFrameButton.Enabled = flag2 && flag3;
		m_CreateSessionFromSelectionMenuItem.Enabled = flag2;
		m_ScopeColourModeButton.Enabled = flag2;
		UpdateCallstackButton();
	}

	private bool TextBoxHasFocus()
	{
		if (!(FindFocusedControl(this) is TextBox))
		{
			return FindFocusedControl(this) is TrackBar;
		}
		return true;
	}

	private static Control FindFocusedControl(Control control)
	{
		for (IContainerControl containerControl = control as IContainerControl; containerControl != null; containerControl = control as IContainerControl)
		{
			control = containerControl.ActiveControl;
		}
		return control;
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		switch (keyData)
		{
		case Keys.Home:
			if (!TextBoxHasFocus())
			{
				GotoStart();
			}
			break;
		case Keys.End:
			if (!TextBoxHasFocus())
			{
				GotoEnd();
			}
			break;
		case Keys.Space:
			if (m_ActiveView != null && ActiveSession != null && ActiveSession.Connected)
			{
				m_ActiveView.TrackEnd = !m_ActiveView.TrackEnd;
			}
			break;
		case Keys.Left:
			if (!TextBoxHasFocus())
			{
				bool zoom_to_frame4 = true;
				if (ActiveSessionView is ThreadsView)
				{
					((ThreadsView)ActiveSessionView).MoveToPrevFrame(zoom_to_frame4);
				}
				if (ActiveSessionView is CoresView)
				{
					((CoresView)ActiveSessionView).MoveToPrevFrame(zoom_to_frame4);
				}
			}
			break;
		case Keys.Left | Keys.Shift:
			if (!TextBoxHasFocus())
			{
				bool zoom_to_frame2 = false;
				if (ActiveSessionView is ThreadsView)
				{
					((ThreadsView)ActiveSessionView).MoveToPrevFrame(zoom_to_frame2);
				}
				if (ActiveSessionView is CoresView)
				{
					((CoresView)ActiveSessionView).MoveToPrevFrame(zoom_to_frame2);
				}
			}
			break;
		case Keys.Right:
			if (!TextBoxHasFocus())
			{
				bool zoom_to_frame3 = true;
				if (ActiveSessionView is ThreadsView)
				{
					((ThreadsView)ActiveSessionView).MoveToNextFrame(zoom_to_frame3);
				}
				if (ActiveSessionView is CoresView)
				{
					((CoresView)ActiveSessionView).MoveToNextFrame(zoom_to_frame3);
				}
			}
			break;
		case Keys.Right | Keys.Shift:
			if (!TextBoxHasFocus())
			{
				bool zoom_to_frame = false;
				if (ActiveSessionView is ThreadsView)
				{
					((ThreadsView)ActiveSessionView).MoveToNextFrame(zoom_to_frame);
				}
				if (ActiveSessionView is CoresView)
				{
					((CoresView)ActiveSessionView).MoveToNextFrame(zoom_to_frame);
				}
			}
			break;
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	private void GotoStart()
	{
		if (m_ActiveView != null)
		{
			m_ActiveView.GotoStart();
			m_ActiveView.TrackEnd = false;
		}
	}

	private void GotoEnd()
	{
		if (m_ActiveView != null)
		{
			m_ActiveView.GotoEnd();
			m_ActiveView.TrackEnd = false;
		}
	}

	private void Connect()
	{
		if (ActiveSession == null || !ActiveSession.ProcessingPackets || new WaitForProcessingCompleteForm(ActiveSession, can_ignore: false).ShowDialog(this) != DialogResult.Cancel)
		{
			CloseStartupPage();
			Connect(cap_receive_speed: false);
		}
	}

	private void Connect(bool cap_receive_speed)
	{
		LogLine("Connect: CreateSession");
		Session session = CreateSession();
		session.ShouldCapReceiveSpeed = cap_receive_speed;
		LogLine("Connect: session.Connect...");
		if (session.Connect())
		{
			LogLine("Connect: Session Connected!");
			CreateSessionView(session);
			if (!session.Interactive)
			{
				new RecordingDataForm(session).ShowDialog(this);
				session.RequestRecordedData();
			}
			session.StartReceiving();
		}
		else
		{
			session.Close();
			MessageBox.Show("Failed to connect.\nPlease check that your application is running, and check your connection settings");
			ShowConnectionSettingsDialog();
		}
	}

	private void ExitMenuItem(object sender, EventArgs e)
	{
		Close();
	}

	private void SaveMenuItem(object sender, EventArgs e)
	{
		if (ActiveSession != null)
		{
			SaveSession(ActiveSession);
		}
	}

	private bool SaveSession(Session session)
	{
		if (session.Connected)
		{
			MessageBox.Show("Please disconnect before saving", "FramePro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return false;
		}
		if ((!Path.IsPathRooted(session.SessionFilename) || session.SessionFilename.ToLower().Trim().EndsWith("framepro_recording")) && !AskUserForSaveFilename())
		{
			return false;
		}
		if (session.ProcessingPackets && !(bool)ProgressBar.Show("Still processing received packets. Please wait... ", WaitForProcessingPacketsToComplete, session))
		{
			return false;
		}
		Write();
		return true;
	}

	private object WaitForProcessingPacketsToComplete(object arg, ThreadJobContext context)
	{
		Session session = (Session)arg;
		long num = session.PacketsToProcessCount;
		while (session.ProcessingPackets && !context.Cancel)
		{
			session.WaitforProcessingToFinish(100);
			long packetsToProcessCount = session.PacketsToProcessCount;
			if (packetsToProcessCount > num)
			{
				num = packetsToProcessCount;
			}
			int percentComplete = (int)((num - packetsToProcessCount) * 100 / num);
			context.Progress.PercentComplete = percentComplete;
		}
		if (context.Cancel)
		{
			if (MessageBox.Show("Discard unprocessed packets?", "FramePro", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				session.ForceStopProcessingPackets();
				session.WaitforProcessingToFinish(-1);
				return true;
			}
			return false;
		}
		return true;
	}

	private bool AskUserForSaveFilename()
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		string text = ActiveSession.SessionFilename;
		if (text != null && text.ToLower().Trim().EndsWith(".framepro_recording"))
		{
			text = text.Substring(0, text.Length - ".framepro_recording".Length);
		}
		saveFileDialog.FileName = text;
		saveFileDialog.Filter = "Files (*.framepro)|*.framepro|All files (*.*)|*.*";
		saveFileDialog.FilterIndex = 0;
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return false;
		}
		ActiveSession.SessionFilename = Path.GetFullPath(saveFileDialog.FileName);
		return true;
	}

	private void SaveAsMenuItemClicked(object sender, EventArgs e)
	{
		if (AskUserForSaveFilename())
		{
			SaveSession(ActiveSession);
		}
	}

	private Session CreateSession()
	{
		Session session = new Session(m_Settings.CoreSettings, m_FrameProCoreLog);
		HookSession(session);
		m_Sessions.Add(session);
		return session;
	}

	private void HookSession(Session session)
	{
		session.Disconnected += SessionDisconnected;
		session.SessionClosed += SessionClosed;
		session.ShowError += SessionShowError;
		session.ShowWarning += SessionShowwarning;
		session.RecordCallstacksChanged += SessionRecordCallstacksChangedHandler;
		session.ScopeColourChanged += OnScopeColourChanged;
		session.CustomStatInfoChanged += OnCustomStatInfoChanged;
		session.CustomStatColourChanged += OnCustomStatColourChanged;
	}

	private void UnhookSession(Session session)
	{
		session.Disconnected -= SessionDisconnected;
		session.SessionClosed -= SessionClosed;
		session.ShowError -= SessionShowError;
		session.ShowWarning -= SessionShowwarning;
		session.RecordCallstacksChanged -= SessionRecordCallstacksChangedHandler;
		session.ScopeColourChanged -= OnScopeColourChanged;
		session.CustomStatInfoChanged -= OnCustomStatInfoChanged;
		session.CustomStatColourChanged -= OnCustomStatColourChanged;
	}

	private void OnScopeColourChanged()
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			OnScopeColourChanged_Main();
		});
	}

	private void OnScopeColourChanged_Main()
	{
		m_OnScopeColourChangedTimer.Stop();
		m_OnScopeColourChangedTimer.Start();
	}

	private void OnScopeColourChangedTick(object sender, EventArgs e)
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			sessionView.OnScopeColourChanged();
		}
		m_OnScopeColourChangedTimer.Stop();
	}

	private void OnCustomStatInfoChanged()
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			OnCustomStatInfoChanged_Main();
		});
	}

	private void OnCustomStatInfoChanged_Main()
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			sessionView.OnCustomStatInfoChanged();
		}
	}

	private void OnCustomStatColourChanged()
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			OnCustomStatColourChanged_Main();
		});
	}

	private void OnCustomStatColourChanged_Main()
	{
		m_OnCustomStatColourChangedTimer.Stop();
		m_OnCustomStatColourChangedTimer.Start();
	}

	private void OnCustomStatColourChangedTick(object sender, EventArgs e)
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			sessionView.OnCustomStatColourChanged();
		}
		m_OnCustomStatColourChangedTimer.Stop();
	}

	private void SessionRecordCallstacksChangedHandler()
	{
		m_ControlTaskDispatcher.QueueTask(delegate
		{
			SessionRecordCallstacksChangedHandler_Main();
		});
	}

	private void SessionRecordCallstacksChangedHandler_Main()
	{
		UpdateCallstackButton();
	}

	private void SessionShowError(string error)
	{
		MessageBox.Show(error, "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}

	private void SessionShowwarning(string warning)
	{
		MessageBox.Show(warning, "FramePro warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}

	private void SessionClosed(Session session)
	{
		m_Sessions.Remove(session);
		UnhookSession(session);
		UpdateSaveMenuItemEnabledState();
	}

	private bool Read(string filename)
	{
		if (ActiveSession != null && ActiveSession.ProcessingPackets && new WaitForProcessingCompleteForm(ActiveSession, can_ignore: false).ShowDialog(this) == DialogResult.Cancel)
		{
			return false;
		}
		CloseStartupPage();
		try
		{
			if (File.Exists(filename))
			{
				Session session = CreateSession();
				session.SessionFilename = Path.GetFullPath(filename);
				CreateSessionView(session);
				ReadThreadContext readThreadContext = new ReadThreadContext();
				readThreadContext.m_Filename = filename;
				readThreadContext.m_Session = session;
				bool num = (bool)ProgressBar.Show("Reading " + Path.GetFileName(filename), ReadJob, readThreadContext);
				if (num)
				{
					AddRecentFile(filename);
					ApplySessionViewSaveData(readThreadContext.m_SessionViewSaveData);
				}
				else
				{
					CloseSessionView(session);
				}
				return num;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error reading file: " + filename + "\n" + ex.Message, "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		return false;
	}

	private object ReadJob(object arg, ThreadJobContext context)
	{
		ReadThreadContext readThreadContext = (ReadThreadContext)arg;
		string error = null;
		bool num = readThreadContext.m_Session.Read(readThreadContext.m_Filename, readThreadContext.m_SessionViewSaveData, context, ref error);
		if (num)
		{
			m_ControlTaskDispatcher.QueueTask(delegate
			{
				UpdateFindControl();
			});
		}
		else if (!context.Cancel)
		{
			if (string.IsNullOrEmpty(error))
			{
				error = "unknown error";
			}
			MessageBox.Show("Failed to read file.\n" + error, "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		return num;
	}

	private void UpdateFindControl()
	{
		if (!string.IsNullOrEmpty(m_FindControl.FindText))
		{
			FindControlTextChanged(m_FindControl.FindText);
		}
	}

	private void ApplySessionViewSaveData(SessionViewSaveData session_view_save_data)
	{
		if (m_ActiveView != null)
		{
			m_ActiveView.ApplySessionViewSaveData(session_view_save_data);
		}
	}

	private SessionViewSaveData GetSessionViewSaveData()
	{
		SessionViewSaveData sessionViewSaveData = new SessionViewSaveData();
		if (m_ActiveView != null)
		{
			m_ActiveView.GetSessionViewSaveData(sessionViewSaveData);
		}
		return sessionViewSaveData;
	}

	private void Write()
	{
		if (ActiveSession != null)
		{
			WriteThreadContext writeThreadContext = new WriteThreadContext();
			writeThreadContext.m_Session = ActiveSession;
			writeThreadContext.m_SessionViewSaveData = GetSessionViewSaveData();
			if ((bool)ProgressBar.Show("Writing " + ActiveSession.SessionFilename, WriteJob, writeThreadContext))
			{
				AddRecentFile(ActiveSession.SessionFilename);
			}
			else
			{
				MessageBox.Show("Error writing file " + ActiveSession.Filename + "\n" + writeThreadContext.m_Error, "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
	}

	private object WriteJob(object arg, ThreadJobContext context)
	{
		WriteThreadContext writeThreadContext = (WriteThreadContext)arg;
		string error = "";
		bool num = writeThreadContext.m_Session.Write(writeThreadContext.m_SessionViewSaveData, context, ref error);
		writeThreadContext.m_Error = error;
		return num;
	}

	private void OpenMenuItem(object sender, EventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Files (*.framepro;*.framepro_recording)|*.framepro;*.framepro_recording|All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			Read(openFileDialog.FileName);
		}
	}

	private void DisconnectButtonPressed(object sender, EventArgs e)
	{
		Disconnect();
	}

	public void Disconnect()
	{
		if (ActiveSession != null)
		{
			ActiveSession.Disconnect(DisconnectReason.Requested);
		}
	}

	private void OnDisconnected()
	{
		DisableCallstackRecording();
		UpdateButtonAndMenuItemStates();
		if (ActiveSession != null && !ActiveSession.Recorded && ActiveSession.DisconnectReason == DisconnectReason.BadVersion)
		{
			MessageBox.Show(string.Concat(string.Concat("Incorrect FramePro.cpp version: " + ActiveSession.ReceivedFrameProLibVersion + "\n", "Expected version: ", Session.FrameProLibVersion.ToString(), "\n"), "Please update FramePro to connect to this app."), "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void CloseMenuItem(object sender, EventArgs e)
	{
		CloseSession();
	}

	private void CloseSession()
	{
		Disconnect();
		if (ActiveSession != null)
		{
			CloseSessionView(ActiveSession);
		}
	}

	private void UpdateSaveMenuItemEnabledState()
	{
		bool enabled = ActiveSession != null;
		m_SaveMenuItem.Enabled = enabled;
		m_SaveAsMenuItem.Enabled = enabled;
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		Point point = PointToScreen(e.Location);
		Control controlAtScreenPoint = m_DockManager.GetControlAtScreenPoint(point);
		if (controlAtScreenPoint is MainSessionView mainSessionView)
		{
			mainSessionView.OnMouseWheel(e.Delta, controlAtScreenPoint.PointToClient(point));
		}
		base.OnMouseWheel(e);
	}

	private void ConnectSettingsButtonClicked(object sender, EventArgs e)
	{
		ShowConnectionSettingsDialog();
	}

	private void ShowConnectionSettingsDialog()
	{
		ShowConnectionSettingsDialog(new_connection: false);
	}

	private void DisableCallstackRecording()
	{
		if (m_Settings.GetCurrentConnection().RecordCallstacks)
		{
			m_Settings.GetCurrentConnection().RecordCallstacks = false;
			m_Settings.Write();
		}
	}

	private void ShowConnectionSettingsDialog(bool new_connection)
	{
		DisableCallstackRecording();
		if (new ConnectSettingsDialog(m_Settings, new_connection).ShowDialog(this) == DialogResult.OK)
		{
			if (m_Settings.m_FirstConnect)
			{
				m_Settings.m_FirstConnect = false;
			}
			m_Settings.Write();
			Connect();
		}
	}

	private void SettingsMenuItemClicked(object sender, EventArgs e)
	{
		double targetFrameMS = m_Settings.TargetFrameMS;
		int threadScopeHeight = m_Settings.ThreadScopeHeight;
		SettingsDialog settingsDialog = new SettingsDialog(m_Settings);
		if (settingsDialog.ShowDialog(this) == DialogResult.OK)
		{
			m_Settings.Write();
			if (settingsDialog.SymbolPathsChanged && ActiveSession != null)
			{
				ActiveSession.ReloadSymbols();
			}
		}
		if (m_Settings.TargetFrameMS != targetFrameMS)
		{
			foreach (SessionView view in GetViews<SessionView>())
			{
				view.OnTargetFrameTimeChanged();
			}
		}
		if (m_Settings.ThreadScopeHeight == threadScopeHeight)
		{
			return;
		}
		foreach (SessionView view2 in GetViews<SessionView>())
		{
			view2.OnThreadScopeHeightChanged();
		}
	}

	private void AboutButtonClicked(object sender, EventArgs e)
	{
		new AboutDialog(m_Settings).ShowDialog(this);
	}

	private void ConnectMenuItemClicked(object sender, EventArgs e)
	{
		ShowConnectionSettingsDialog();
	}

	private void NewConnectionMenuItem(object sender, EventArgs e)
	{
		ShowConnectionSettingsDialog(new_connection: true);
	}

	private void DisconnectButtonClicked(object sender, EventArgs e)
	{
		Disconnect();
	}

	private void HomeButtonClicked(object sender, EventArgs e)
	{
		GotoStart();
	}

	private void PlayPauseButtonClicked(object sender, EventArgs e)
	{
		if (m_ActiveView != null)
		{
			m_ActiveView.TrackEnd = !m_ActiveView.TrackEnd;
		}
	}

	private void UpdatePlayPauseButtonState()
	{
		m_TrackEndButton.Image = ((m_ActiveView != null && m_ActiveView.TrackEnd) ? Resources.PauseButton : Resources.PlayButton);
	}

	private void GotoEndButtonClicked(object sender, EventArgs e)
	{
		GotoEnd();
	}

	protected override void WndProc(ref Message m)
	{
		m_ControlTaskDispatcher.ProcessMessage(base.Handle, ref m);
		base.WndProc(ref m);
	}

	private void RegistrationMenuItemClicked(object sender, EventArgs e)
	{
		ShowRegistrationForm();
	}

	private void ShowRegistrationForm()
	{
		ShowRegistrationForm(show_in_task_bar: false);
	}

	private void ShowRegistrationForm(bool show_in_task_bar)
	{
		SCLCoreCLR.Log.WriteLine("ShowRegistrationForm");
		RegistrationForm.ShowForm(m_Settings, show_in_task_bar);
		UpdateTitle();
	}

	private void ConditionalSliderValueChanged(long min_time)
	{
		if (m_Settings.ConditionalScopeTimeSliderMinTime != min_time)
		{
			m_Settings.ConditionalScopeTimeSliderMinTime = min_time;
			m_Settings.Write();
		}
		if (ActiveSession != null && ActiveSession.Connected)
		{
			ActiveSession.SendConditionalScopeMinTime();
		}
	}

	protected override void OnResize(EventArgs e)
	{
		if (m_InitialisedSize)
		{
			if (m_Settings.MainWindowSize != base.Size && base.WindowState != FormWindowState.Maximized && base.WindowState != FormWindowState.Minimized)
			{
				m_Settings.MainWindowSize = base.Size;
				m_Settings.Write();
			}
			switch (base.WindowState)
			{
			case FormWindowState.Normal:
				if (m_Settings.MainWindowMaximised)
				{
					m_Settings.MainWindowMaximised = false;
					m_Settings.Write();
				}
				break;
			case FormWindowState.Maximized:
				if (!m_Settings.MainWindowMaximised)
				{
					m_Settings.MainWindowMaximised = true;
					m_Settings.Write();
				}
				break;
			}
		}
		base.OnResize(e);
	}

	private void UpdateSessionViewButtons()
	{
		ThreadsView threadsView = ActiveSessionView as ThreadsView;
		bool flag = threadsView != null;
		CoresView coresView = ActiveSessionView as CoresView;
		bool flag2 = coresView != null;
		bool enabled = flag || flag2;
		m_InfoMenuItem.Enabled = enabled;
		m_FrameGraphMenuItem.Enabled = enabled;
		m_ThreadsViewCoreViewMenuItem.Enabled = flag;
		m_InfoViewButton.Enabled = enabled;
		m_FramesViewButton.Enabled = enabled;
		m_ScopeViewButton.Enabled = enabled;
		m_CoresViewButton.Enabled = flag;
		m_CustomStatsGraphButton.Enabled = flag;
		m_DataGridViewButton.Enabled = flag;
		m_InfoMenuItem.Checked = (flag && threadsView.InfoPanelVisible) || (flag2 && coresView.InfoPanelVisible);
		m_FrameGraphMenuItem.Checked = (flag && threadsView.FrameGraphVisible) || (flag2 && coresView.FrameGraphVisible);
		m_ThreadsViewCoreViewMenuItem.Checked = flag && threadsView.CoreViewVisible;
		m_InfoViewButton.Checked = (flag && threadsView.InfoPanelVisible) || (flag2 && coresView.InfoPanelVisible);
		m_FramesViewButton.Checked = (flag && threadsView.FrameGraphVisible) || (flag2 && coresView.FrameGraphVisible);
		m_ScopeViewButton.Checked = (flag && threadsView.TimeSpanGraphVisible) || (flag2 && coresView.TimeSpanGraphVisible);
		m_CoresViewButton.Checked = flag && threadsView.CoreViewVisible;
		m_CustomStatsGraphButton.Checked = flag && threadsView.CustomStatsVisible;
		m_DataGridViewButton.Checked = flag && threadsView.DataGridVisible;
	}

	private void UpdateMenuItemStates()
	{
		bool enabled = ActiveSession != null;
		m_ExportToCSVMenuItem.Enabled = enabled;
		m_ExportFrameGraphToCSVMenuItem.Enabled = enabled;
		m_ThreadsViewMenuItem.Enabled = enabled;
		m_CoresViewMenuItem.Enabled = enabled;
		m_ScopesViewMenuItem.Enabled = enabled;
	}

	private void ShowInfoPanel(bool visible)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowInfoPanel(visible);
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).ShowInfoPanel(visible);
		}
		UpdateSessionViewButtons();
	}

	private void ShowFrameGraph(bool visible)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowFrameGraph(visible);
		}
		if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).ShowFrameGraph(visible);
		}
		UpdateSessionViewButtons();
	}

	private void ShowTimeSpanGraph(bool visible)
	{
		if (ActiveSessionView is ThreadsView threadsView)
		{
			threadsView.ShowTimeSpanGraph(visible);
		}
		UpdateSessionViewButtons();
	}

	private void ShowCoreView(bool visible)
	{
		if (ActiveSessionView is ThreadsView threadsView)
		{
			threadsView.ShowCoreView(visible);
		}
		UpdateSessionViewButtons();
	}

	private void ShowCustomStatsView(bool visible)
	{
		if (ActiveSessionView is ThreadsView threadsView)
		{
			threadsView.ShowCustomStatsGraph(visible);
		}
		UpdateSessionViewButtons();
	}

	public void ShowThreadsViewDataGrid(bool visible)
	{
		if (ActiveSessionView is ThreadsView threadsView)
		{
			threadsView.ShowDataGrid(visible);
		}
		UpdateSessionViewButtons();
	}

	public void ShowThreadsViewDataGrid(TimeSpan time_span)
	{
		if (time_span.TimeSpanInfoId != TimeSpanInfo.InvalidInfoId)
		{
			if (ActiveSessionView is ThreadsView threadsView)
			{
				threadsView.DataGridTimeSpan = time_span;
			}
			ShowThreadsViewDataGrid(visible: true);
		}
	}

	private void FrameGraphMenuItemClicked(object sender, EventArgs e)
	{
		ShowFrameGraph(m_FrameGraphMenuItem.Checked);
	}

	private void CoreViewMenuItemClicked(object sender, EventArgs e)
	{
		ShowCoreView(m_ThreadsViewCoreViewMenuItem.Checked);
	}

	private void CustomStatsGraphButtonCheckChanged(ViewButton sender)
	{
		ShowCustomStatsView(m_CustomStatsGraphButton.Checked);
	}

	private void DataGridViewButtonCheckedChanged(ViewButton sender)
	{
		ShowThreadsViewDataGrid(m_DataGridViewButton.Checked);
	}

	private void InfoMenuItemClicked(object sender, EventArgs e)
	{
		ShowInfoPanel(m_InfoMenuItem.Checked);
		UpdateSessionViewButtons();
	}

	private void UpdateRecentFilesMenuItems()
	{
		foreach (ToolStripItem dropDownItem in m_RecentFilesMenuItem.DropDownItems)
		{
			dropDownItem.Click -= RecentFileMenuItem;
		}
		m_RecentFilesMenuItem.DropDownItems.Clear();
		foreach (string recentFile in m_Settings.RecentFiles)
		{
			m_RecentFilesMenuItem.DropDownItems.Add(recentFile);
		}
		foreach (ToolStripItem dropDownItem2 in m_RecentFilesMenuItem.DropDownItems)
		{
			dropDownItem2.Click += RecentFileMenuItem;
		}
	}

	private void RecentFileMenuItem(object sender, EventArgs e)
	{
		string filename = ((ToolStripMenuItem)sender).Text;
		OpenRecentFile(filename);
	}

	private void OpenRecentFile(string filename)
	{
		if (File.Exists(filename))
		{
			Read(filename);
			return;
		}
		MessageBox.Show("Failed opening file. File does not exist\n" + filename);
		m_Settings.RemoveRecentFile(filename);
		UpdateRecentFilesMenuItems();
	}

	private void AddRecentFile(string file)
	{
		m_Settings.AddRecentFile(file);
		UpdateRecentFilesMenuItems();
	}

	private void FindControlTextChanged(string text)
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			sessionView.HighlightTimeSpans(text);
		}
	}

	private void FindControlGotoPrev(string text)
	{
		if (ActiveSessionView != null)
		{
			ActiveSessionView.GotoPrev(text);
		}
	}

	private void FindControlGotoNext(string text)
	{
		if (ActiveSessionView != null)
		{
			ActiveSessionView.GotoNext(text);
		}
	}

	private void InfoViewCheckedChanged(ViewButton sender)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowInfoPanel(sender.Checked);
		}
		if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).ShowInfoPanel(sender.Checked);
		}
		UpdateSessionViewButtons();
	}

	private void FramesButtonCheckedChanged(ViewButton sender)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowFrameGraph(sender.Checked);
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).ShowFrameGraph(sender.Checked);
		}
		UpdateSessionViewButtons();
	}

	private void ScopesViewButtonCheckedChanged(ViewButton sender)
	{
		ShowScopeGraph(sender.Checked);
	}

	private void ShowScopeGraph(bool visible)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowTimeSpanGraph(visible);
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).ShowTimeSpanGraph(visible);
		}
		UpdateSessionViewButtons();
	}

	private void CoresViewButtonCheckedChanged(ViewButton sender)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).ShowCoreView(sender.Checked);
		}
		UpdateSessionViewButtons();
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		if (keyData == (Keys.C | Keys.Control) && m_HoverBox.Visible)
		{
			m_HoverBox.CopyToClipboard();
		}
		return base.ProcessDialogKey(keyData);
	}

	protected override void OnDragEnter(DragEventArgs drgevent)
	{
		if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
		{
			drgevent.Effect = DragDropEffects.Copy;
		}
		base.OnDragEnter(drgevent);
	}

	protected override void OnDragDrop(DragEventArgs drgevent)
	{
		base.OnDragDrop(drgevent);
		string[] array = (string[])drgevent.Data.GetData(DataFormats.FileDrop);
		foreach (string filename in array)
		{
			Read(filename);
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		bool cancel = false;
		NotifyUnsavedSessions(ref cancel);
		e.Cancel = cancel;
		base.OnClosing(e);
	}

	private void NotifyUnsavedSessions(ref bool cancel)
	{
		if (!HasUnsavedSessions || !m_Settings.SaveChangedQuery)
		{
			return;
		}
		switch (new SaveOnExitDialog(m_Settings).ShowDialog(this))
		{
		case DialogResult.Yes:
		{
			foreach (Session session in m_Sessions)
			{
				if (!session.Saved && !SaveSession(session))
				{
					cancel = true;
					break;
				}
			}
			break;
		}
		case DialogResult.Cancel:
			cancel = true;
			break;
		}
	}

	protected override void OnClosed(EventArgs e)
	{
		if (m_UpdateThreadJob != null && !m_UpdateThreadJob.Finished)
		{
			FramePro.Update.Terminate();
			m_UpdateThreadJob.Abort();
		}
		base.OnClosed(e);
	}

	private void ExportToCSVMenuItem(object sender, EventArgs e)
	{
		if (ActiveSession == null)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Files (*.csv)|*.csv|All files (*.*)|*.*";
		saveFileDialog.FilterIndex = 0;
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK || (ActiveSession != null && ActiveSession.ProcessingPackets && new WaitForProcessingCompleteForm(ActiveSession, can_ignore: true).ShowDialog(this) == DialogResult.Cancel))
		{
			return;
		}
		try
		{
			ActiveSession.WriteToCSV(saveFileDialog.FileName);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Unable to write to file " + saveFileDialog.FileName + "\n" + ex.Message, "FramePro Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void GotoPrevSpikeButtonClicked(object sender, EventArgs e)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).GotoPrevSpike();
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).GotoPrevSpike();
		}
	}

	private void GotoNextSpikeButtonClicked(object sender, EventArgs e)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).GotoNextSpike();
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).GotoNextSpike();
		}
	}

	private void CheckForUpdatesMenuItemClicked(object sender, EventArgs e)
	{
        ////if (m_Settings.RegisterUsingPureDevReg)
        ////{
        ////	Registration.BusyForm.Show("Checking for updates...", "FramePro Update", CheckForUpdate_PureDevReg);
        ////}
        ////else
        ////{
        ////	Registration.BusyForm.Show("Checking for updates...", "FramePro Update", CheckForUpdate);
        ////}
        ////if (m_VersionUpdateCheckSucceeded)
        ////{
        ////	if (m_Settings.NewVersionAvailable)
        ////	{
        ////		NotifyNewVersion();
        ////	}
        ////	else
        ////	{
        ////		MessageBox.Show("You already have the latest version of FramePro", "FramePro Update", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        ////	}
        ////}
        ////else
        ////{
        ////	MessageBox.Show("Error requesting current version number\n\nPlease visit www.puredevsoftware.com", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        ////}
        MessageBox.Show("学习版仅供研究, 不能升级!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }

	private ViewT GetView<ViewT>(Session session) where ViewT : SessionView
	{
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			if (sessionView.Session == session)
			{
				return sessionView.GetView<ViewT>();
			}
		}
		return null;
	}

	private List<ViewT> GetViews<ViewT>() where ViewT : SessionView
	{
		List<ViewT> list = new List<ViewT>();
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			ViewT view = sessionView.GetView<ViewT>();
			if (view != null)
			{
				list.Add(view);
			}
		}
		return list;
	}

	private void ThreadsViewMenuItemClicked(object sender, EventArgs e)
	{
		if (ActiveSession != null && m_ActiveView != null)
		{
			m_ActiveView.ActivateTab("Threads");
		}
	}

	private void ScopesViewMenuItemClicked(object sender, EventArgs e)
	{
		if (ActiveSession != null && m_ActiveView != null)
		{
			m_ActiveView.ActivateTab("Scopes");
		}
	}

	private void FindMenuItemClicked(object sender, EventArgs e)
	{
		m_FindControl.SelectAll();
		m_FindControl.Focus();
	}

	private void LaunchGameSimulatorMenuItem(object sender, EventArgs e)
	{
		LaunchSimulator();
	}

	private bool LaunchSimulator()
	{
		return LaunchSimulator(minimised: false);
	}

	private bool LaunchSimulator(bool minimised)
	{
		try
		{
			if (m_GameSimulatorProcess != null && !m_GameSimulatorProcess.HasExited)
			{
				MessageBox.Show("Game Simulator is already running.");
				return false;
			}
		}
		catch (Exception ex)
		{
			LogLine(ex.Message);
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo("FramePro_GameSimulator.exe");
		processStartInfo.Verb = "runas";
		processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;
		m_GameSimulatorProcess = new Process();
		m_GameSimulatorProcess.StartInfo = processStartInfo;
		try
		{
			m_GameSimulatorProcess.Start();
		}
		catch (Exception ex2)
		{
			MessageBox.Show("Failed to launch game simulator. " + ex2.Message, "FramePro Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			LogLine(ex2.Message);
			return false;
		}
		return true;
	}

	private void PlaybackRecordingFileInRealtime(object sender, EventArgs e)
	{
		if (m_RecordingPlayerProcess != null && !m_RecordingPlayerProcess.HasExited)
		{
			MessageBox.Show("Recording Player is already running.");
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Files (*.framepro_recording)|*.framepro_recording|All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			LaunchRecordingPlayer(openFileDialog.FileName);
		}
	}

	private void LaunchRecordingPlayer(string playback_filename)
	{
		m_RecordingPlayerProcess = new Process();
		m_RecordingPlayerProcess.StartInfo.FileName = "FramePro_RecordingPlayer.exe";
		m_RecordingPlayerProcess.StartInfo.Arguments = playback_filename;
		m_RecordingPlayerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
		m_RecordingPlayerProcess.Start();
		m_RecordingPlayerProcess.WaitForInputIdle(5000);
		DisableCallstackRecording();
		Connect();
	}

	public static void ShowHelp()
	{
		ShowHelp(null);
	}

	public static void ShowHelp(string page)
	{
		string text = ((!string.IsNullOrEmpty(page)) ? ("::/" + page) : "");
		string text2 = Path.Combine(Environment.CurrentDirectory, "FramePro.chm");
		Process.Start("hh.exe", text2 + text);
	}

	private void HelpMenuItemClicked(object sender, EventArgs e)
	{
		ShowHelp();
	}

	private void ShowStartupPage()
	{
		if (m_StartupPage != null)
		{
			m_DockManager.SetActiveControl(m_StartupPage);
			return;
		}
		m_StartupPage = new StartupPage(m_Settings.RecentFiles, m_Settings.ShowStartupPage);
		m_StartupPage.LaunchDemo += LaunchDemo;
		m_StartupPage.StartupPageOpenFile += OpenRecentFile;
		m_StartupPage.ShowStartupPageToggled += ShowStartupPageCheckBoxToggled;
		m_DockManager.AddMdiChild(m_StartupPage);
	}

	private void CloseStartupPage()
	{
		if (m_StartupPage != null)
		{
			m_DockManager.CloseControl(m_StartupPage);
			m_StartupPage = null;
		}
	}

	private void ShowStartupPageCheckBoxToggled(bool show_startup_page)
	{
		m_Settings.ShowStartupPage = show_startup_page;
		m_Settings.Write();
	}

	private void LaunchDemo()
	{
		if (LaunchSimulator(minimised: true))
		{
			List<Connection> connections = m_Settings.Connections;
			m_Settings.Connections = new List<Connection>();
			Connection connection = new Connection();
			connection.IP = "127.0.0.1";
			connection.Port = Settings.DefaultPort;
			m_Settings.Connections.Add(connection);
			DisableCallstackRecording();
			Connect();
			m_Settings.Connections = connections;
			m_Settings.Write();
		}
	}

	private void ShowStartupPageMenuItemClicked(object sender, EventArgs e)
	{
		ShowStartupPage();
	}

	public void CloneSession(long start_time, long end_time)
	{
		if (ActiveSession == null)
		{
			return;
		}
		CloneThreadContext cloneThreadContext = new CloneThreadContext();
		cloneThreadContext.m_SessionViewSaveData = GetSessionViewSaveData();
		cloneThreadContext.m_OriginalSession = ActiveSession;
		cloneThreadContext.m_StartTime = start_time;
		cloneThreadContext.m_EndTime = end_time;
		cloneThreadContext.m_NewSession = CreateSession();
		CreateSessionView(cloneThreadContext.m_NewSession);
		if ((bool)ProgressBar.Show("Cloning session... ", CloneJob, cloneThreadContext))
		{
			ApplySessionViewSaveData(cloneThreadContext.m_SessionViewSaveData);
			if (!string.IsNullOrEmpty(m_FindControl.FindText))
			{
				FindControlTextChanged(m_FindControl.FindText);
			}
		}
		else
		{
			MessageBox.Show("Error cloning session: " + cloneThreadContext.m_Error, "FramePro ERROR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			CloseSessionView(cloneThreadContext.m_NewSession);
		}
	}

	private object CloneJob(object arg, ThreadJobContext context)
	{
		CloneThreadContext cloneThreadContext = (CloneThreadContext)arg;
		return cloneThreadContext.m_OriginalSession.CopyTo(cloneThreadContext.m_NewSession, cloneThreadContext.m_StartTime, cloneThreadContext.m_EndTime, context, ref cloneThreadContext.m_Error);
	}

	private void CreateSessionFromSelectionMenuItemClicked(object sender, EventArgs e)
	{
		ThreadsView view = GetView<ThreadsView>(ActiveSession);
		if (view != null)
		{
			CloneSession(view.SelectionStartTime, view.SelectionEndTime);
		}
		else
		{
			MessageBox.Show("Please select frames in the graph view from which to create a new session", "FramePro", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	private void OutputWindowMenuItem(object sender, EventArgs e)
	{
		ToggleOutputWindow();
	}

	private void ToggleOutputWindow()
	{
		m_OutputWindowVisible = !m_OutputWindowVisible;
		m_OutputWindowPanel.Visible = m_OutputWindowVisible;
		m_OutputWindowSplitter.Visible = m_OutputWindowVisible;
		m_OutputWindowMenuItem.Checked = m_OutputWindowVisible;
		if (m_Settings.OutputWindowVisible != m_OutputWindowVisible)
		{
			m_Settings.OutputWindowVisible = m_OutputWindowVisible;
			m_Settings.Write();
		}
		if (!m_OutputWindowVisible)
		{
			return;
		}
		foreach (string outputWindowLine in m_OutputWindowLines)
		{
			m_OutputTextBox.AppendText(outputWindowLine);
		}
		m_OutputWindowLines.Clear();
	}

	private void OutputWindowResize(object sender, EventArgs e)
	{
		int num = (int)((float)m_OutputWindowPanel.Height / m_DPIScale);
		if (m_Settings.OutputWindowHeight != num)
		{
			m_Settings.OutputWindowHeight = num;
			m_Settings.Write();
		}
	}

	private void ColourByThreadMenuItemClicked(object sender, EventArgs e)
	{
		m_Settings.ScopeColourMode = ScopeColourMode.Thread;
		m_Settings.Write();
		OnScopeColourModeChanged();
	}

	private void ColourByScopeMenuItemClicked(object sender, EventArgs e)
	{
		m_Settings.ScopeColourMode = ScopeColourMode.Scope;
		m_Settings.Write();
		OnScopeColourModeChanged();
	}

	private void ScopeColourModeButtonClicked(object sender, EventArgs e)
	{
		m_Settings.ScopeColourMode = ((m_Settings.ScopeColourMode == ScopeColourMode.Thread) ? ScopeColourMode.Scope : ScopeColourMode.Thread);
		m_Settings.Write();
		OnScopeColourModeChanged();
	}

	private void OnScopeColourModeChanged()
	{
		m_ColourByThreadMenuItem.Checked = m_Settings.ScopeColourMode == ScopeColourMode.Thread;
		m_ColourByScopeMenuItem.Checked = m_Settings.ScopeColourMode == ScopeColourMode.Scope;
		foreach (MainSessionView sessionView in m_SessionViews)
		{
			sessionView.OnScopeColourModeChanged();
		}
		m_ScopeColourModeButton.Image = ((m_Settings.ScopeColourMode == ScopeColourMode.Thread) ? Resources.ThreadColourModeButton : Resources.ScopeColourModeButton);
	}

	private void OnCallstacksButtonClicked(object sender, EventArgs e)
	{
		ActiveSession.RecordCallstacks = !ActiveSession.RecordCallstacks;
	}

	private void UpdateCallstackButton()
	{
		m_CallstackButton.Enabled = ActiveSession != null && ActiveSession.Connected;
		m_CallstackButton.Image = ((ActiveSession != null && ActiveSession.RecordCallstacks) ? Resources.CallstackButtonActive : Resources.CallstackButton);
	}

	private void CustomStatsGraphMenuItemClicked(object sender, EventArgs e)
	{
		ShowCustomStatsView(m_CustomStatsGraphMenuItem.Checked);
	}

	private void ScopeGraphMenuItemClicked(object sender, EventArgs e)
	{
		ShowScopeGraph(m_ScopeGraphMenuItem.Checked);
	}

	private void RecordContextSwitchesAndroidMenuItemClicked(object sender, EventArgs e)
	{
		AndroidContextSwitchRecordingDialog androidContextSwitchRecordingDialog = new AndroidContextSwitchRecordingDialog(m_Settings.AndroidContextSwitchRecordingDuration);
		if (androidContextSwitchRecordingDialog.ShowDialog(this) == DialogResult.Cancel)
		{
			return;
		}
		int duration = androidContextSwitchRecordingDialog.Duration;
		if (duration != m_Settings.AndroidContextSwitchRecordingDuration)
		{
			m_Settings.AndroidContextSwitchRecordingDuration = duration;
			m_Settings.Write();
		}
		string text = m_Settings.AndroidADBExePath;
		if (text == "" || !File.Exists(text))
		{
			text = Utils.FindAndroidADBExe();
		}
		if (text == null)
		{
			GetAndroidSDKPathForm getAndroidSDKPathForm = new GetAndroidSDKPathForm();
			if (getAndroidSDKPathForm.ShowDialog(this) == DialogResult.OK)
			{
				text = getAndroidSDKPathForm.ADBPath;
			}
		}
		if (!File.Exists(text))
		{
			MessageBox.Show("Bad ADB path: " + text);
			return;
		}
		if (text != m_Settings.AndroidADBExePath)
		{
			m_Settings.AndroidADBExePath = text;
			m_Settings.Write();
		}
		if (!Utils.ExecuteProcess(text + " shell \"echo 1 > /d/tracing/tracing_on\"", out var result))
		{
			MessageBox.Show("Error starting context switch recording:\n" + result, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		if (!Utils.ExecuteProcess(text + " shell \"echo echo userlandclock\\(\\) > /d/tracing/trace_marker\"", out result))
		{
			MessageBox.Show("Error starting context switch recording:\n" + result, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		RecordContextSwitchesContext recordContextSwitchesContext = new RecordContextSwitchesContext();
		recordContextSwitchesContext.m_Duration = duration;
		recordContextSwitchesContext.m_ADBPath = text;
		ProgressBar.Show("Recording Context Switches...", RecordContextSwitchesJob, recordContextSwitchesContext, can_cancel: false);
		if (!recordContextSwitchesContext.m_Result)
		{
			MessageBox.Show("Error starting context switch recording:\n" + recordContextSwitchesContext.m_ResultText, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			return;
		}
		Utils.ExecuteProcess(text + " shell \"echo 0 > /d/tracing/tracing_on\"", out result);
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Files (*.framepro_context_switch)|*.framepro_context_switch|All files (*.*)|*.*";
		saveFileDialog.FilterIndex = 0;
		if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			File.WriteAllText(saveFileDialog.FileName, recordContextSwitchesContext.m_ResultText);
		}
	}

	private object RecordContextSwitchesJob(object arg, ThreadJobContext context)
	{
		RecordContextSwitchesContext recordContextSwitchesContext = (RecordContextSwitchesContext)arg;
		string aDBPath = recordContextSwitchesContext.m_ADBPath;
		int duration = recordContextSwitchesContext.m_Duration;
		new Thread((ThreadStart)delegate
		{
			RecordContextSwitchesJobProgress(duration, context.Progress);
		}).Start();
		recordContextSwitchesContext.m_Result = Utils.ExecuteProcess(aDBPath + " shell atrace -t " + duration + " sched", out var result, log_result: false);
		recordContextSwitchesContext.m_ResultText = result;
		return null;
	}

	private void RecordContextSwitchesJobProgress(int duration, Progress progress)
	{
		int num = duration * 1000;
		for (int i = 0; i < num; i += 100)
		{
			progress.Set(i * 100 / num, 100L);
			Thread.Sleep(100);
		}
	}

	private void LoadContextSwitchFileAndroid(object sender, EventArgs e)
	{
		LoadContextSwitchFile();
	}

	public bool LoadContextSwitchFile()
	{
		Session activeSession = ActiveSession;
		if (activeSession == null)
		{
			return false;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Files (*.framepro_context_switch)|*.framepro_context_switch| All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(this) == DialogResult.OK && File.Exists(openFileDialog.FileName))
		{
			if (activeSession.LoadContextSwitchFile(openFileDialog.FileName))
			{
				return true;
			}
			MessageBox.Show("Error reading context switch file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		return false;
	}

	private void GotoMaxFrameButtonClicked(object sender, EventArgs e)
	{
		if (ActiveSessionView is ThreadsView)
		{
			((ThreadsView)ActiveSessionView).GotoMaxFrame();
		}
		else if (ActiveSessionView is CoresView)
		{
			((CoresView)ActiveSessionView).GotoMaxFrame();
		}
	}

	private void ExportFrameGraphToCSVMenuItemClicked(object sender, EventArgs e)
	{
		if (ActiveSession == null)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "Files (*.csv)|*.csv|All files (*.*)|*.*";
		saveFileDialog.FilterIndex = 0;
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK || (ActiveSession != null && ActiveSession.ProcessingPackets && new WaitForProcessingCompleteForm(ActiveSession, can_ignore: true).ShowDialog(this) == DialogResult.Cancel))
		{
			return;
		}
		try
		{
			ActiveSession.WriteFrameGraphToCSV(saveFileDialog.FileName);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Unable to write to file " + saveFileDialog.FileName + "\n" + ex.Message, "FramePro Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void CloseAllMenuItemClicked(object sender, EventArgs e)
	{
		bool cancel = false;
		NotifyUnsavedSessions(ref cancel);
		if (cancel)
		{
			return;
		}
		while (ActiveSession != null)
		{
			CloseSessionView(ActiveSession);
			if (m_SessionViews.Count != 0)
			{
				m_DockManager.SetActiveControl(m_SessionViews[m_SessionViews.Count - 1]);
			}
		}
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FramePro.MainForm));
		this.panel1 = new System.Windows.Forms.Panel();
		this.m_GotoMaxFrameButton = new FramePro.FrameProButton();
		this.m_CallstackButton = new FramePro.FrameProButton();
		this.m_DataGridViewButton = new FramePro.ViewButton();
		this.m_CustomStatsGraphButton = new FramePro.ViewButton();
		this.m_ScopeColourModeButton = new FramePro.FrameProButton();
		this.m_GotoNextSpikeButton = new FramePro.FrameProButton();
		this.m_GotoPrevSpikeButton = new FramePro.FrameProButton();
		this.m_InfoViewButton = new FramePro.ViewButton();
		this.m_FindControl = new FramePro.FindControl();
		this.m_ConnectButton = new FramePro.FrameProButton();
		this.m_CoresViewButton = new FramePro.ViewButton();
		this.m_ScopeViewButton = new FramePro.ViewButton();
		this.m_FramesViewButton = new FramePro.ViewButton();
		this.m_ConditionalScopeTimeSlider = new FramePro.ConditionalScopeTimeSlider();
		this.m_GotoEndButton = new FramePro.FrameProButton();
		this.m_TrackEndButton = new FramePro.FrameProButton();
		this.m_GotoStartButton = new FramePro.FrameProButton();
		this.m_ConnectSettingsButton = new FramePro.FrameProButton();
		this.m_DisconnectButton = new FramePro.FrameProButton();
		this.m_MainPanel = new System.Windows.Forms.Panel();
		this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_SaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_SaveAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.closeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
		this.m_ExportToCSVMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ExportFrameGraphToCSVMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
		this.m_RecentFilesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
		this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ThreadsViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_CoresViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ScopesViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
		this.m_ViewSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_InfoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_FrameGraphMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ScopeGraphMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ThreadsViewCoreViewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_CustomStatsGraphMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.colouringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ColourByThreadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ColourByScopeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
		this.m_OutputWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.connectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_NewConnectionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_ConnectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.m_DisconnectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
		this.findToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
		this.m_CreateSessionFromSelectionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
		this.androidToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.recordContextSwitchesAndroidToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.loadContextSwitchFileAndroidToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
		this.settingsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.helpToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
		this.enterProductKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.checkForUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
		this.demoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.launchFrameProGameSimulatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.playbackDumpFileInRealtimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.showStartupPageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
		this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.m_OutputWindowPanel = new System.Windows.Forms.Panel();
		this.m_OutputTextBox = new System.Windows.Forms.TextBox();
		this.m_OutputWindowSplitter = new System.Windows.Forms.Splitter();
		this.panel1.SuspendLayout();
		this.menuStrip1.SuspendLayout();
		this.m_OutputWindowPanel.SuspendLayout();
		base.SuspendLayout();
		this.panel1.Controls.Add(this.m_GotoMaxFrameButton);
		this.panel1.Controls.Add(this.m_CallstackButton);
		this.panel1.Controls.Add(this.m_DataGridViewButton);
		this.panel1.Controls.Add(this.m_CustomStatsGraphButton);
		this.panel1.Controls.Add(this.m_ScopeColourModeButton);
		this.panel1.Controls.Add(this.m_GotoNextSpikeButton);
		this.panel1.Controls.Add(this.m_GotoPrevSpikeButton);
		this.panel1.Controls.Add(this.m_InfoViewButton);
		this.panel1.Controls.Add(this.m_FindControl);
		this.panel1.Controls.Add(this.m_ConnectButton);
		this.panel1.Controls.Add(this.m_CoresViewButton);
		this.panel1.Controls.Add(this.m_ScopeViewButton);
		this.panel1.Controls.Add(this.m_FramesViewButton);
		this.panel1.Controls.Add(this.m_ConditionalScopeTimeSlider);
		this.panel1.Controls.Add(this.m_GotoEndButton);
		this.panel1.Controls.Add(this.m_TrackEndButton);
		this.panel1.Controls.Add(this.m_GotoStartButton);
		this.panel1.Controls.Add(this.m_ConnectSettingsButton);
		this.panel1.Controls.Add(this.m_DisconnectButton);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 24);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(1691, 65);
		this.panel1.TabIndex = 6;
		this.m_GotoMaxFrameButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GotoMaxFrameButton.ButtonText = "Max";
		this.m_GotoMaxFrameButton.DisabledImage = FramePro.Properties.Resources.GotoMaxFrame_disabled;
		this.m_GotoMaxFrameButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_GotoMaxFrameButton.HighlightEnabled = true;
		this.m_GotoMaxFrameButton.Image = FramePro.Properties.Resources.GotoMaxFrame;
		this.m_GotoMaxFrameButton.Location = new System.Drawing.Point(824, 0);
		this.m_GotoMaxFrameButton.Name = "m_GotoMaxFrameButton";
		this.m_GotoMaxFrameButton.Size = new System.Drawing.Size(40, 65);
		this.m_GotoMaxFrameButton.TabIndex = 28;
		this.m_GotoMaxFrameButton.TabStop = false;
		this.m_GotoMaxFrameButton.UseImageAsText = false;
		this.m_GotoMaxFrameButton.Click += new System.EventHandler(GotoMaxFrameButtonClicked);
		this.m_CallstackButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_CallstackButton.ButtonText = "Callstacks";
		this.m_CallstackButton.DisabledImage = FramePro.Properties.Resources.CallstackButton_disabled;
		this.m_CallstackButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_CallstackButton.HighlightEnabled = true;
		this.m_CallstackButton.Image = FramePro.Properties.Resources.CallstackButton;
		this.m_CallstackButton.Location = new System.Drawing.Point(1590, 0);
		this.m_CallstackButton.Name = "m_CallstackButton";
		this.m_CallstackButton.Size = new System.Drawing.Size(65, 65);
		this.m_CallstackButton.TabIndex = 27;
		this.m_CallstackButton.TabStop = false;
		this.m_CallstackButton.UseImageAsText = false;
		this.m_CallstackButton.Click += new System.EventHandler(OnCallstacksButtonClicked);
		this.m_DataGridViewButton.ButtonText = "Details";
		this.m_DataGridViewButton.Checked = false;
		this.m_DataGridViewButton.DisabledImage = FramePro.Properties.Resources.DataGridViewButton_disabled;
		this.m_DataGridViewButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DataGridViewButton.Image = FramePro.Properties.Resources.DataGridViewButton;
		this.m_DataGridViewButton.Location = new System.Drawing.Point(686, 0);
		this.m_DataGridViewButton.Name = "m_DataGridViewButton";
		this.m_DataGridViewButton.Size = new System.Drawing.Size(65, 65);
		this.m_DataGridViewButton.TabIndex = 26;
		this.m_DataGridViewButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(DataGridViewButtonCheckedChanged);
		this.m_CustomStatsGraphButton.ButtonText = "Custom Stats";
		this.m_CustomStatsGraphButton.Checked = false;
		this.m_CustomStatsGraphButton.DisabledImage = FramePro.Properties.Resources.CustomStatGraphDisabled;
		this.m_CustomStatsGraphButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_CustomStatsGraphButton.Image = FramePro.Properties.Resources.CustomStatGraph;
		this.m_CustomStatsGraphButton.Location = new System.Drawing.Point(615, 0);
		this.m_CustomStatsGraphButton.Name = "m_CustomStatsGraphButton";
		this.m_CustomStatsGraphButton.Size = new System.Drawing.Size(65, 65);
		this.m_CustomStatsGraphButton.TabIndex = 25;
		this.m_CustomStatsGraphButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(CustomStatsGraphButtonCheckChanged);
		this.m_ScopeColourModeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_ScopeColourModeButton.ButtonText = "Colour Mode";
		this.m_ScopeColourModeButton.DisabledImage = FramePro.Properties.Resources.DisabledColourModeButton;
		this.m_ScopeColourModeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ScopeColourModeButton.HighlightEnabled = true;
		this.m_ScopeColourModeButton.Image = FramePro.Properties.Resources.ThreadColourModeButton;
		this.m_ScopeColourModeButton.Location = new System.Drawing.Point(1305, 0);
		this.m_ScopeColourModeButton.Name = "m_ScopeColourModeButton";
		this.m_ScopeColourModeButton.Size = new System.Drawing.Size(65, 65);
		this.m_ScopeColourModeButton.TabIndex = 24;
		this.m_ScopeColourModeButton.TabStop = false;
		this.m_ScopeColourModeButton.UseImageAsText = false;
		this.m_ScopeColourModeButton.Click += new System.EventHandler(ScopeColourModeButtonClicked);
		this.m_GotoNextSpikeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GotoNextSpikeButton.ButtonText = "Next";
		this.m_GotoNextSpikeButton.DisabledImage = FramePro.Properties.Resources.GotoNextSpikeButton_disabled;
		this.m_GotoNextSpikeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_GotoNextSpikeButton.HighlightEnabled = true;
		this.m_GotoNextSpikeButton.Image = FramePro.Properties.Resources.GotoNextSpikeButton;
		this.m_GotoNextSpikeButton.Location = new System.Drawing.Point(870, 0);
		this.m_GotoNextSpikeButton.Name = "m_GotoNextSpikeButton";
		this.m_GotoNextSpikeButton.Size = new System.Drawing.Size(40, 65);
		this.m_GotoNextSpikeButton.TabIndex = 23;
		this.m_GotoNextSpikeButton.TabStop = false;
		this.m_GotoNextSpikeButton.UseImageAsText = false;
		this.m_GotoNextSpikeButton.Click += new System.EventHandler(GotoNextSpikeButtonClicked);
		this.m_GotoPrevSpikeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GotoPrevSpikeButton.ButtonText = "Prev";
		this.m_GotoPrevSpikeButton.DisabledImage = FramePro.Properties.Resources.GotoPrevSpikeButton_disabled;
		this.m_GotoPrevSpikeButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_GotoPrevSpikeButton.HighlightEnabled = true;
		this.m_GotoPrevSpikeButton.Image = FramePro.Properties.Resources.GotoPrevSpikeButton;
		this.m_GotoPrevSpikeButton.Location = new System.Drawing.Point(778, 0);
		this.m_GotoPrevSpikeButton.Name = "m_GotoPrevSpikeButton";
		this.m_GotoPrevSpikeButton.Size = new System.Drawing.Size(40, 65);
		this.m_GotoPrevSpikeButton.TabIndex = 22;
		this.m_GotoPrevSpikeButton.TabStop = false;
		this.m_GotoPrevSpikeButton.UseImageAsText = false;
		this.m_GotoPrevSpikeButton.Click += new System.EventHandler(GotoPrevSpikeButtonClicked);
		this.m_InfoViewButton.ButtonText = "Info";
		this.m_InfoViewButton.Checked = false;
		this.m_InfoViewButton.DisabledImage = FramePro.Properties.Resources.InfoViewButton_disabled1;
		this.m_InfoViewButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_InfoViewButton.Image = FramePro.Properties.Resources.InfoViewButton1;
		this.m_InfoViewButton.Location = new System.Drawing.Point(331, 0);
		this.m_InfoViewButton.Name = "m_InfoViewButton";
		this.m_InfoViewButton.Size = new System.Drawing.Size(65, 65);
		this.m_InfoViewButton.TabIndex = 21;
		this.m_InfoViewButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(InfoViewCheckedChanged);
		this.m_FindControl.Location = new System.Drawing.Point(936, 0);
		this.m_FindControl.Margin = new System.Windows.Forms.Padding(4);
		this.m_FindControl.Name = "m_FindControl";
		this.m_FindControl.Size = new System.Drawing.Size(343, 65);
		this.m_FindControl.TabIndex = 12;
		this.m_FindControl.TabStop = false;
		this.m_FindControl.FindControlTextChanged += new FramePro.FindControlTextChangedHandler(FindControlTextChanged);
		this.m_FindControl.FindControlGotoPrev += new FramePro.FindControlGotoPrevHandler(FindControlGotoPrev);
		this.m_FindControl.FindControlGotoNext += new FramePro.FindControlGotoNextHandler(FindControlGotoNext);
		this.m_ConnectButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_ConnectButton.ButtonText = "Connect";
		this.m_ConnectButton.DisabledImage = FramePro.Properties.Resources.ConnectButton_disabled;
		this.m_ConnectButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ConnectButton.HighlightEnabled = true;
		this.m_ConnectButton.Image = FramePro.Properties.Resources.ConnectButton;
		this.m_ConnectButton.Location = new System.Drawing.Point(0, 0);
		this.m_ConnectButton.Name = "m_ConnectButton";
		this.m_ConnectButton.Size = new System.Drawing.Size(65, 65);
		this.m_ConnectButton.TabIndex = 0;
		this.m_ConnectButton.TabStop = false;
		this.m_ConnectButton.UseImageAsText = false;
		this.m_ConnectButton.Click += new System.EventHandler(ConnectButtonClick);
		this.m_CoresViewButton.ButtonText = "Cores";
		this.m_CoresViewButton.Checked = false;
		this.m_CoresViewButton.DisabledImage = FramePro.Properties.Resources.CoreViewButton_disabled;
		this.m_CoresViewButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_CoresViewButton.Image = FramePro.Properties.Resources.CoreViewButton1;
		this.m_CoresViewButton.Location = new System.Drawing.Point(544, 0);
		this.m_CoresViewButton.Name = "m_CoresViewButton";
		this.m_CoresViewButton.Size = new System.Drawing.Size(65, 65);
		this.m_CoresViewButton.TabIndex = 20;
		this.m_CoresViewButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(CoresViewButtonCheckedChanged);
		this.m_ScopeViewButton.ButtonText = "Scope";
		this.m_ScopeViewButton.Checked = false;
		this.m_ScopeViewButton.DisabledImage = FramePro.Properties.Resources.TimeSpanGraphViewButton_disabled;
		this.m_ScopeViewButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_ScopeViewButton.Image = FramePro.Properties.Resources.TimeSpanGraphViewButton1;
		this.m_ScopeViewButton.Location = new System.Drawing.Point(473, 0);
		this.m_ScopeViewButton.Name = "m_ScopeViewButton";
		this.m_ScopeViewButton.Size = new System.Drawing.Size(65, 65);
		this.m_ScopeViewButton.TabIndex = 18;
		this.m_ScopeViewButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(ScopesViewButtonCheckedChanged);
		this.m_FramesViewButton.ButtonText = "Frames";
		this.m_FramesViewButton.Checked = false;
		this.m_FramesViewButton.DisabledImage = FramePro.Properties.Resources.FrameGraphViewButton_disabled;
		this.m_FramesViewButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_FramesViewButton.Image = FramePro.Properties.Resources.FrameGraphViewButton;
		this.m_FramesViewButton.Location = new System.Drawing.Point(402, 0);
		this.m_FramesViewButton.Name = "m_FramesViewButton";
		this.m_FramesViewButton.Size = new System.Drawing.Size(65, 65);
		this.m_FramesViewButton.TabIndex = 17;
		this.m_FramesViewButton.CheckedChanged += new FramePro.ViewButtonCheckedChangedHandler(FramesButtonCheckedChanged);
		this.m_ConditionalScopeTimeSlider.Location = new System.Drawing.Point(1388, 0);
		this.m_ConditionalScopeTimeSlider.Margin = new System.Windows.Forms.Padding(4);
		this.m_ConditionalScopeTimeSlider.Name = "m_ConditionalScopeTimeSlider";
		this.m_ConditionalScopeTimeSlider.Size = new System.Drawing.Size(195, 65);
		this.m_ConditionalScopeTimeSlider.TabIndex = 10;
		this.m_ConditionalScopeTimeSlider.TabStop = false;
		this.m_ConditionalScopeTimeSlider.ValueChanged += new FramePro.ConditionalSliderChangedHandler(ConditionalSliderValueChanged);
		this.m_GotoEndButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GotoEndButton.ButtonText = "End";
		this.m_GotoEndButton.DisabledImage = FramePro.Properties.Resources.GotoEndButton_disabled;
		this.m_GotoEndButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_GotoEndButton.HighlightEnabled = true;
		this.m_GotoEndButton.Image = (System.Drawing.Image)resources.GetObject("m_GotoEndButton.Image");
		this.m_GotoEndButton.Location = new System.Drawing.Point(278, 0);
		this.m_GotoEndButton.Name = "m_GotoEndButton";
		this.m_GotoEndButton.Size = new System.Drawing.Size(40, 65);
		this.m_GotoEndButton.TabIndex = 9;
		this.m_GotoEndButton.TabStop = false;
		this.m_GotoEndButton.UseImageAsText = false;
		this.m_GotoEndButton.Click += new System.EventHandler(GotoEndButtonClicked);
		this.m_TrackEndButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_TrackEndButton.ButtonText = "Track";
		this.m_TrackEndButton.DisabledImage = FramePro.Properties.Resources.PlayButton_disabled;
		this.m_TrackEndButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_TrackEndButton.HighlightEnabled = true;
		this.m_TrackEndButton.Image = (System.Drawing.Image)resources.GetObject("m_TrackEndButton.Image");
		this.m_TrackEndButton.Location = new System.Drawing.Point(232, 0);
		this.m_TrackEndButton.Name = "m_TrackEndButton";
		this.m_TrackEndButton.Size = new System.Drawing.Size(40, 65);
		this.m_TrackEndButton.TabIndex = 8;
		this.m_TrackEndButton.TabStop = false;
		this.m_TrackEndButton.UseImageAsText = false;
		this.m_TrackEndButton.Click += new System.EventHandler(PlayPauseButtonClicked);
		this.m_GotoStartButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_GotoStartButton.ButtonText = "Start";
		this.m_GotoStartButton.DisabledImage = FramePro.Properties.Resources.GotoStartButton_disabled;
		this.m_GotoStartButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_GotoStartButton.HighlightEnabled = true;
		this.m_GotoStartButton.Image = (System.Drawing.Image)resources.GetObject("m_GotoStartButton.Image");
		this.m_GotoStartButton.Location = new System.Drawing.Point(186, 0);
		this.m_GotoStartButton.Name = "m_GotoStartButton";
		this.m_GotoStartButton.Size = new System.Drawing.Size(40, 65);
		this.m_GotoStartButton.TabIndex = 6;
		this.m_GotoStartButton.TabStop = false;
		this.m_GotoStartButton.UseImageAsText = false;
		this.m_GotoStartButton.Click += new System.EventHandler(HomeButtonClicked);
		this.m_ConnectSettingsButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_ConnectSettingsButton.ButtonText = "";
		this.m_ConnectSettingsButton.DisabledImage = null;
		this.m_ConnectSettingsButton.HighlightEnabled = true;
		this.m_ConnectSettingsButton.Image = FramePro.Properties.Resources.ConnectSettingsButton;
		this.m_ConnectSettingsButton.Location = new System.Drawing.Point(65, 0);
		this.m_ConnectSettingsButton.Name = "m_ConnectSettingsButton";
		this.m_ConnectSettingsButton.Size = new System.Drawing.Size(17, 65);
		this.m_ConnectSettingsButton.TabIndex = 5;
		this.m_ConnectSettingsButton.TabStop = false;
		this.m_ConnectSettingsButton.UseImageAsText = true;
		this.m_ConnectSettingsButton.Click += new System.EventHandler(ConnectSettingsButtonClicked);
		this.m_DisconnectButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this.m_DisconnectButton.ButtonText = "Disconnect";
		this.m_DisconnectButton.DisabledImage = FramePro.Properties.Resources.DisconnectButton_disabled;
		this.m_DisconnectButton.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_DisconnectButton.HighlightEnabled = true;
		this.m_DisconnectButton.Image = FramePro.Properties.Resources.DisconnectButton1;
		this.m_DisconnectButton.Location = new System.Drawing.Point(88, 0);
		this.m_DisconnectButton.Name = "m_DisconnectButton";
		this.m_DisconnectButton.Size = new System.Drawing.Size(65, 65);
		this.m_DisconnectButton.TabIndex = 4;
		this.m_DisconnectButton.TabStop = false;
		this.m_DisconnectButton.UseImageAsText = false;
		this.m_DisconnectButton.Click += new System.EventHandler(DisconnectButtonPressed);
		this.m_MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_MainPanel.Location = new System.Drawing.Point(0, 89);
		this.m_MainPanel.Name = "m_MainPanel";
		this.m_MainPanel.Size = new System.Drawing.Size(1691, 663);
		this.m_MainPanel.TabIndex = 7;
		this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[12]
		{
			this.openToolStripMenuItem, this.m_SaveMenuItem, this.m_SaveAsMenuItem, this.closeToolStripMenuItem, this.closeAllToolStripMenuItem, this.toolStripSeparator9, this.m_ExportToCSVMenuItem, this.m_ExportFrameGraphToCSVMenuItem, this.toolStripSeparator2, this.m_RecentFilesMenuItem,
			this.toolStripSeparator4, this.exitToolStripMenuItem
		});
		this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
		this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
		this.fileToolStripMenuItem.Text = "File";
		this.openToolStripMenuItem.Name = "openToolStripMenuItem";
		this.openToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.O | System.Windows.Forms.Keys.Control;
		this.openToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
		this.openToolStripMenuItem.Text = "Open";
		this.openToolStripMenuItem.Click += new System.EventHandler(OpenMenuItem);
		this.m_SaveMenuItem.Name = "m_SaveMenuItem";
		this.m_SaveMenuItem.ShortcutKeys = System.Windows.Forms.Keys.S | System.Windows.Forms.Keys.Control;
		this.m_SaveMenuItem.Size = new System.Drawing.Size(217, 22);
		this.m_SaveMenuItem.Text = "Save";
		this.m_SaveMenuItem.Click += new System.EventHandler(SaveMenuItem);
		this.m_SaveAsMenuItem.Name = "m_SaveAsMenuItem";
		this.m_SaveAsMenuItem.Size = new System.Drawing.Size(217, 22);
		this.m_SaveAsMenuItem.Text = "Save As...";
		this.m_SaveAsMenuItem.Click += new System.EventHandler(SaveAsMenuItemClicked);
		this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
		this.closeToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
		this.closeToolStripMenuItem.Text = "Close";
		this.closeToolStripMenuItem.Click += new System.EventHandler(CloseMenuItem);
		this.closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
		this.closeAllToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
		this.closeAllToolStripMenuItem.Text = "Close All";
		this.closeAllToolStripMenuItem.Click += new System.EventHandler(CloseAllMenuItemClicked);
		this.toolStripSeparator9.Name = "toolStripSeparator9";
		this.toolStripSeparator9.Size = new System.Drawing.Size(214, 6);
		this.m_ExportToCSVMenuItem.Name = "m_ExportToCSVMenuItem";
		this.m_ExportToCSVMenuItem.Size = new System.Drawing.Size(217, 22);
		this.m_ExportToCSVMenuItem.Text = "Export to CSV";
		this.m_ExportToCSVMenuItem.Click += new System.EventHandler(ExportToCSVMenuItem);
		this.m_ExportFrameGraphToCSVMenuItem.Name = "m_ExportFrameGraphToCSVMenuItem";
		this.m_ExportFrameGraphToCSVMenuItem.Size = new System.Drawing.Size(217, 22);
		this.m_ExportFrameGraphToCSVMenuItem.Text = "Export Frame Graph to CSV";
		this.m_ExportFrameGraphToCSVMenuItem.Click += new System.EventHandler(ExportFrameGraphToCSVMenuItemClicked);
		this.toolStripSeparator2.Name = "toolStripSeparator2";
		this.toolStripSeparator2.Size = new System.Drawing.Size(214, 6);
		this.m_RecentFilesMenuItem.Name = "m_RecentFilesMenuItem";
		this.m_RecentFilesMenuItem.Size = new System.Drawing.Size(217, 22);
		this.m_RecentFilesMenuItem.Text = "Recent Files";
		this.toolStripSeparator4.Name = "toolStripSeparator4";
		this.toolStripSeparator4.Size = new System.Drawing.Size(214, 6);
		this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
		this.exitToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
		this.exitToolStripMenuItem.Text = "Exit";
		this.exitToolStripMenuItem.Click += new System.EventHandler(ExitMenuItem);
		this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[8] { this.m_ThreadsViewMenuItem, this.m_CoresViewMenuItem, this.m_ScopesViewMenuItem, this.toolStripSeparator6, this.m_ViewSettingsMenuItem, this.colouringToolStripMenuItem, this.toolStripSeparator10, this.m_OutputWindowMenuItem });
		this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
		this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
		this.viewToolStripMenuItem.Text = "View";
		this.m_ThreadsViewMenuItem.Name = "m_ThreadsViewMenuItem";
		this.m_ThreadsViewMenuItem.Size = new System.Drawing.Size(162, 22);
		this.m_ThreadsViewMenuItem.Text = "Threads View";
		this.m_ThreadsViewMenuItem.Click += new System.EventHandler(ThreadsViewMenuItemClicked);
		this.m_CoresViewMenuItem.Name = "m_CoresViewMenuItem";
		this.m_CoresViewMenuItem.Size = new System.Drawing.Size(162, 22);
		this.m_CoresViewMenuItem.Text = "Cores View";
		this.m_ScopesViewMenuItem.Name = "m_ScopesViewMenuItem";
		this.m_ScopesViewMenuItem.Size = new System.Drawing.Size(162, 22);
		this.m_ScopesViewMenuItem.Text = "Scopes View";
		this.m_ScopesViewMenuItem.Click += new System.EventHandler(ScopesViewMenuItemClicked);
		this.toolStripSeparator6.Name = "toolStripSeparator6";
		this.toolStripSeparator6.Size = new System.Drawing.Size(159, 6);
		this.m_ViewSettingsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[5] { this.m_InfoMenuItem, this.m_FrameGraphMenuItem, this.m_ScopeGraphMenuItem, this.m_ThreadsViewCoreViewMenuItem, this.m_CustomStatsGraphMenuItem });
		this.m_ViewSettingsMenuItem.Name = "m_ViewSettingsMenuItem";
		this.m_ViewSettingsMenuItem.Size = new System.Drawing.Size(162, 22);
		this.m_ViewSettingsMenuItem.Text = "Threads View";
		this.m_InfoMenuItem.Checked = true;
		this.m_InfoMenuItem.CheckOnClick = true;
		this.m_InfoMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_InfoMenuItem.Name = "m_InfoMenuItem";
		this.m_InfoMenuItem.Size = new System.Drawing.Size(179, 22);
		this.m_InfoMenuItem.Text = "Info";
		this.m_InfoMenuItem.Click += new System.EventHandler(InfoMenuItemClicked);
		this.m_FrameGraphMenuItem.Checked = true;
		this.m_FrameGraphMenuItem.CheckOnClick = true;
		this.m_FrameGraphMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_FrameGraphMenuItem.Name = "m_FrameGraphMenuItem";
		this.m_FrameGraphMenuItem.Size = new System.Drawing.Size(179, 22);
		this.m_FrameGraphMenuItem.Text = "Frame Graph";
		this.m_FrameGraphMenuItem.Click += new System.EventHandler(FrameGraphMenuItemClicked);
		this.m_ScopeGraphMenuItem.Checked = true;
		this.m_ScopeGraphMenuItem.CheckOnClick = true;
		this.m_ScopeGraphMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_ScopeGraphMenuItem.Name = "m_ScopeGraphMenuItem";
		this.m_ScopeGraphMenuItem.Size = new System.Drawing.Size(179, 22);
		this.m_ScopeGraphMenuItem.Text = "Scope Graph";
		this.m_ScopeGraphMenuItem.Click += new System.EventHandler(ScopeGraphMenuItemClicked);
		this.m_ThreadsViewCoreViewMenuItem.Checked = true;
		this.m_ThreadsViewCoreViewMenuItem.CheckOnClick = true;
		this.m_ThreadsViewCoreViewMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_ThreadsViewCoreViewMenuItem.Name = "m_ThreadsViewCoreViewMenuItem";
		this.m_ThreadsViewCoreViewMenuItem.Size = new System.Drawing.Size(179, 22);
		this.m_ThreadsViewCoreViewMenuItem.Text = "CPU Graph";
		this.m_ThreadsViewCoreViewMenuItem.Click += new System.EventHandler(CoreViewMenuItemClicked);
		this.m_CustomStatsGraphMenuItem.Checked = true;
		this.m_CustomStatsGraphMenuItem.CheckOnClick = true;
		this.m_CustomStatsGraphMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_CustomStatsGraphMenuItem.Name = "m_CustomStatsGraphMenuItem";
		this.m_CustomStatsGraphMenuItem.Size = new System.Drawing.Size(179, 22);
		this.m_CustomStatsGraphMenuItem.Text = "Custom Stats Graph";
		this.m_CustomStatsGraphMenuItem.Click += new System.EventHandler(CustomStatsGraphMenuItemClicked);
		this.colouringToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.m_ColourByThreadMenuItem, this.m_ColourByScopeMenuItem });
		this.colouringToolStripMenuItem.Name = "colouringToolStripMenuItem";
		this.colouringToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
		this.colouringToolStripMenuItem.Text = "Scope Colouring";
		this.m_ColourByThreadMenuItem.Name = "m_ColourByThreadMenuItem";
		this.m_ColourByThreadMenuItem.Size = new System.Drawing.Size(165, 22);
		this.m_ColourByThreadMenuItem.Text = "Colour by Thread";
		this.m_ColourByScopeMenuItem.Name = "m_ColourByScopeMenuItem";
		this.m_ColourByScopeMenuItem.Size = new System.Drawing.Size(165, 22);
		this.m_ColourByScopeMenuItem.Text = "Colour by Scope";
		this.toolStripSeparator10.Name = "toolStripSeparator10";
		this.toolStripSeparator10.Size = new System.Drawing.Size(159, 6);
		this.m_OutputWindowMenuItem.Checked = true;
		this.m_OutputWindowMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
		this.m_OutputWindowMenuItem.Name = "m_OutputWindowMenuItem";
		this.m_OutputWindowMenuItem.Size = new System.Drawing.Size(162, 22);
		this.m_OutputWindowMenuItem.Text = "Output Window";
		this.m_OutputWindowMenuItem.Click += new System.EventHandler(OutputWindowMenuItem);
		this.connectionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.m_NewConnectionMenuItem, this.m_ConnectMenuItem, this.m_DisconnectMenuItem });
		this.connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
		this.connectionToolStripMenuItem.Size = new System.Drawing.Size(81, 20);
		this.connectionToolStripMenuItem.Text = "Connection";
		this.m_NewConnectionMenuItem.Name = "m_NewConnectionMenuItem";
		this.m_NewConnectionMenuItem.Size = new System.Drawing.Size(172, 22);
		this.m_NewConnectionMenuItem.Text = "New Connection...";
		this.m_NewConnectionMenuItem.Click += new System.EventHandler(NewConnectionMenuItem);
		this.m_ConnectMenuItem.Name = "m_ConnectMenuItem";
		this.m_ConnectMenuItem.Size = new System.Drawing.Size(172, 22);
		this.m_ConnectMenuItem.Text = "Connect...";
		this.m_ConnectMenuItem.Click += new System.EventHandler(ConnectMenuItemClicked);
		this.m_DisconnectMenuItem.Name = "m_DisconnectMenuItem";
		this.m_DisconnectMenuItem.Size = new System.Drawing.Size(172, 22);
		this.m_DisconnectMenuItem.Text = "Disconnect";
		this.m_DisconnectMenuItem.Click += new System.EventHandler(DisconnectButtonClicked);
		this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[8] { this.toolStripSeparator1, this.findToolStripMenuItem1, this.toolStripSeparator5, this.m_CreateSessionFromSelectionMenuItem, this.toolStripSeparator7, this.androidToolStripMenuItem, this.toolStripSeparator11, this.settingsToolStripMenuItem1 });
		this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
		this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
		this.toolsToolStripMenuItem.Text = "Tools";
		this.toolStripSeparator1.Name = "toolStripSeparator1";
		this.toolStripSeparator1.Size = new System.Drawing.Size(227, 6);
		this.findToolStripMenuItem1.Name = "findToolStripMenuItem1";
		this.findToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.F | System.Windows.Forms.Keys.Control;
		this.findToolStripMenuItem1.Size = new System.Drawing.Size(230, 22);
		this.findToolStripMenuItem1.Text = "Find";
		this.findToolStripMenuItem1.Click += new System.EventHandler(FindMenuItemClicked);
		this.toolStripSeparator5.Name = "toolStripSeparator5";
		this.toolStripSeparator5.Size = new System.Drawing.Size(227, 6);
		this.m_CreateSessionFromSelectionMenuItem.Name = "m_CreateSessionFromSelectionMenuItem";
		this.m_CreateSessionFromSelectionMenuItem.Size = new System.Drawing.Size(230, 22);
		this.m_CreateSessionFromSelectionMenuItem.Text = "Create Session from Selection";
		this.m_CreateSessionFromSelectionMenuItem.Click += new System.EventHandler(CreateSessionFromSelectionMenuItemClicked);
		this.toolStripSeparator7.Name = "toolStripSeparator7";
		this.toolStripSeparator7.Size = new System.Drawing.Size(227, 6);
		this.androidToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.recordContextSwitchesAndroidToolStripMenuItem, this.loadContextSwitchFileAndroidToolStripMenuItem });
		this.androidToolStripMenuItem.Name = "androidToolStripMenuItem";
		this.androidToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
		this.androidToolStripMenuItem.Text = "Android";
		this.recordContextSwitchesAndroidToolStripMenuItem.Name = "recordContextSwitchesAndroidToolStripMenuItem";
		this.recordContextSwitchesAndroidToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
		this.recordContextSwitchesAndroidToolStripMenuItem.Text = "Start Recording Context Switches...";
		this.recordContextSwitchesAndroidToolStripMenuItem.Click += new System.EventHandler(RecordContextSwitchesAndroidMenuItemClicked);
		this.loadContextSwitchFileAndroidToolStripMenuItem.Name = "loadContextSwitchFileAndroidToolStripMenuItem";
		this.loadContextSwitchFileAndroidToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
		this.loadContextSwitchFileAndroidToolStripMenuItem.Text = "Load Context Switch File";
		this.loadContextSwitchFileAndroidToolStripMenuItem.Click += new System.EventHandler(LoadContextSwitchFileAndroid);
		this.toolStripSeparator11.Name = "toolStripSeparator11";
		this.toolStripSeparator11.Size = new System.Drawing.Size(227, 6);
		this.settingsToolStripMenuItem1.Name = "settingsToolStripMenuItem1";
		this.settingsToolStripMenuItem1.Size = new System.Drawing.Size(230, 22);
		this.settingsToolStripMenuItem1.Text = "Settings";
		this.settingsToolStripMenuItem1.Click += new System.EventHandler(SettingsMenuItemClicked);
		this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[8] { this.helpToolStripMenuItem1, this.enterProductKeyToolStripMenuItem, this.checkForUpdatesToolStripMenuItem, this.toolStripSeparator3, this.demoToolStripMenuItem, this.showStartupPageToolStripMenuItem, this.toolStripSeparator8, this.aboutToolStripMenuItem });
		this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
		this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
		this.helpToolStripMenuItem.Text = "Help";
		this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
		this.helpToolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
		this.helpToolStripMenuItem1.Text = "View Help";
		this.helpToolStripMenuItem1.Click += new System.EventHandler(HelpMenuItemClicked);
		this.enterProductKeyToolStripMenuItem.Name = "enterProductKeyToolStripMenuItem";
		this.enterProductKeyToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
		this.enterProductKeyToolStripMenuItem.Text = "Registration...";
		this.enterProductKeyToolStripMenuItem.Click += new System.EventHandler(RegistrationMenuItemClicked);
		this.checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
		this.checkForUpdatesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
		this.checkForUpdatesToolStripMenuItem.Text = "Check for Updates";
		this.checkForUpdatesToolStripMenuItem.Click += new System.EventHandler(CheckForUpdatesMenuItemClicked);
		this.toolStripSeparator3.Name = "toolStripSeparator3";
		this.toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
		this.demoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.launchFrameProGameSimulatorToolStripMenuItem, this.playbackDumpFileInRealtimeToolStripMenuItem });
		this.demoToolStripMenuItem.Name = "demoToolStripMenuItem";
		this.demoToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
		this.demoToolStripMenuItem.Text = "Demo";
		this.launchFrameProGameSimulatorToolStripMenuItem.Name = "launchFrameProGameSimulatorToolStripMenuItem";
		this.launchFrameProGameSimulatorToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
		this.launchFrameProGameSimulatorToolStripMenuItem.Text = "Launch FramePro Game Simulator";
		this.launchFrameProGameSimulatorToolStripMenuItem.Click += new System.EventHandler(LaunchGameSimulatorMenuItem);
		this.playbackDumpFileInRealtimeToolStripMenuItem.Name = "playbackDumpFileInRealtimeToolStripMenuItem";
		this.playbackDumpFileInRealtimeToolStripMenuItem.Size = new System.Drawing.Size(255, 22);
		this.playbackDumpFileInRealtimeToolStripMenuItem.Text = "Playback Recording File...";
		this.playbackDumpFileInRealtimeToolStripMenuItem.Click += new System.EventHandler(PlaybackRecordingFileInRealtime);
		this.showStartupPageToolStripMenuItem.Name = "showStartupPageToolStripMenuItem";
		this.showStartupPageToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
		this.showStartupPageToolStripMenuItem.Text = "Show Startup Page";
		this.showStartupPageToolStripMenuItem.Click += new System.EventHandler(ShowStartupPageMenuItemClicked);
		this.toolStripSeparator8.Name = "toolStripSeparator8";
		this.toolStripSeparator8.Size = new System.Drawing.Size(177, 6);
		this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
		this.aboutToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
		this.aboutToolStripMenuItem.Text = "About";
		this.aboutToolStripMenuItem.Click += new System.EventHandler(AboutButtonClicked);
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[5] { this.fileToolStripMenuItem, this.viewToolStripMenuItem, this.connectionToolStripMenuItem, this.toolsToolStripMenuItem, this.helpToolStripMenuItem });
		this.menuStrip1.Location = new System.Drawing.Point(0, 0);
		this.menuStrip1.Name = "menuStrip1";
		this.menuStrip1.Size = new System.Drawing.Size(1691, 24);
		this.menuStrip1.TabIndex = 3;
		this.menuStrip1.Text = "menuStrip1";
		this.m_OutputWindowPanel.Controls.Add(this.m_OutputTextBox);
		this.m_OutputWindowPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_OutputWindowPanel.Location = new System.Drawing.Point(0, 755);
		this.m_OutputWindowPanel.Name = "m_OutputWindowPanel";
		this.m_OutputWindowPanel.Size = new System.Drawing.Size(1691, 200);
		this.m_OutputWindowPanel.TabIndex = 8;
		this.m_OutputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
		this.m_OutputTextBox.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.m_OutputTextBox.Location = new System.Drawing.Point(0, 0);
		this.m_OutputTextBox.Multiline = true;
		this.m_OutputTextBox.Name = "m_OutputTextBox";
		this.m_OutputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		this.m_OutputTextBox.Size = new System.Drawing.Size(1691, 107);
		this.m_OutputTextBox.TabIndex = 0;
		this.m_OutputTextBox.Resize += new System.EventHandler(OutputWindowResize);
		this.m_OutputWindowSplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.m_OutputWindowSplitter.Location = new System.Drawing.Point(0, 752);
		this.m_OutputWindowSplitter.Name = "m_OutputWindowSplitter";
		this.m_OutputWindowSplitter.Size = new System.Drawing.Size(1691, 3);
		this.m_OutputWindowSplitter.TabIndex = 9;
		this.m_OutputWindowSplitter.TabStop = false;
		this.AllowDrop = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1691, 862);
		base.Controls.Add(this.m_MainPanel);
		base.Controls.Add(this.m_OutputWindowSplitter);
		base.Controls.Add(this.m_OutputWindowPanel);
		base.Controls.Add(this.panel1);
		base.Controls.Add(this.menuStrip1);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MainMenuStrip = this.menuStrip1;
		base.Name = "MainForm";
		this.Text = "FramePro";
		base.WindowState = System.Windows.Forms.FormWindowState.Maximized;
		this.panel1.ResumeLayout(false);
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		this.m_OutputWindowPanel.ResumeLayout(false);
		this.m_OutputWindowPanel.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
