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

	private bool m_OutputWindowVisible = true;

	private List<string> m_OutputWindowLines = new List<string>();

	private const string m_ContextSwitchFileExt = ".profiler_context_switch";

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
		Text = "ProfilerStudy [Do not distribute]";
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
		////foreach (Control control2 in controls)
		////{
		////	if (control2 == m_StartupPage)
		////	{
		////		m_StartupPage = null;
		////	}
		////}
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
			////CloseStartupPage();
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
			MessageBox.Show("Please disconnect before saving", "ProfilerStudy", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return false;
		}
		if ((!Path.IsPathRooted(session.SessionFilename) || session.SessionFilename.ToLower().Trim().EndsWith("profiler_recording")) && !AskUserForSaveFilename())
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
			if (MessageBox.Show("Discard unprocessed packets?", "ProfilerStudy", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
		if (text != null && text.ToLower().Trim().EndsWith(".profiler_recording"))
		{
			text = text.Substring(0, text.Length - ".profiler_recording".Length);
		}
		saveFileDialog.FileName = text;
		saveFileDialog.Filter = "Files (*.profiler)|*.profiler|All files (*.*)|*.*";
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
		MessageBox.Show(error, "ProfilerStudy Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}

	private void SessionShowwarning(string warning)
	{
		MessageBox.Show(warning, "ProfilerStudy warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
			MessageBox.Show("Error reading file: " + filename + "\n" + ex.Message, "ProfilerStudy Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
			MessageBox.Show("Failed to read file.\n" + error, "ProfilerStudy Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
				MessageBox.Show("Error writing file " + ActiveSession.Filename + "\n" + writeThreadContext.m_Error, "ProfilerStudy Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
		openFileDialog.Filter = "Files (*.profiler;*.profiler_recording)|*.profiler;*.profiler_recording|All files (*.*)|*.*";
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
			MessageBox.Show(string.Concat(string.Concat("Incorrect FramePro.cpp version: " + ActiveSession.ReceivedFrameProLibVersion + "\n", "Expected version: ", Session.FrameProLibVersion.ToString(), "\n"), "Please update ProfilerStudy to connect to this app."), "FramePro Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
		if (PlatformTool.IsRunOnWine())
		{
			return;
		}
		
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
			MessageBox.Show("Unable to write to file " + saveFileDialog.FileName + "\n" + ex.Message, "ProfilerStudy Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
        MessageBox.Show("ѧϰ������о�, ��������!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
		ProcessStartInfo processStartInfo = new ProcessStartInfo("Profiler_GameSimulator.exe");
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
			MessageBox.Show("Failed to launch game simulator. " + ex2.Message, "ProfilerStudy Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
		openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
		openFileDialog.RestoreDirectory = true;
		openFileDialog.Filter = "Files (*.profiler_recording)|*.profiler_recording|All files (*.*)|*.*";
		openFileDialog.FilterIndex = 0;
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			LaunchRecordingPlayer(openFileDialog.FileName);
		}
	}

	private void LaunchRecordingPlayer(string playback_filename)
	{
		m_RecordingPlayerProcess = new Process();
		m_RecordingPlayerProcess.StartInfo.FileName = "Profiler_RecordingPlayer.exe";
		m_RecordingPlayerProcess.StartInfo.Arguments = playback_filename;
		m_RecordingPlayerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
		m_RecordingPlayerProcess.Start();
		m_RecordingPlayerProcess.WaitForInputIdle(5000);
		DisableCallstackRecording();
		Connect();
	}

	////public static void ShowHelp()
	////{
	////	ShowHelp(null);
	////}

	////public static void ShowHelp(string page)
	////{
	////	string text = ((!string.IsNullOrEmpty(page)) ? ("::/" + page) : "");
	////	string text2 = Path.Combine(Environment.CurrentDirectory, "FramePro.chm");
	////	Process.Start("hh.exe", text2 + text);
	////}

	////private void HelpMenuItemClicked(object sender, EventArgs e)
	////{
	////	ShowHelp();
	////}

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
			MessageBox.Show("Error cloning session: " + cloneThreadContext.m_Error, "ProfilerStudy ERROR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
			MessageBox.Show("Please select frames in the graph view from which to create a new session", "ProfilerStudy", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
		saveFileDialog.Filter = "Files (*.profiler_context_switch)|*.profiler_context_switch|All files (*.*)|*.*";
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
		openFileDialog.Filter = "Files (*.profiler_context_switch)|*.profiler_context_switch| All files (*.*)|*.*";
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
			MessageBox.Show("Unable to write to file " + saveFileDialog.FileName + "\n" + ex.Message, "ProfilerStudy Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
        panel1 = new Panel();
        m_GotoMaxFrameButton = new FrameProButton();
        m_CallstackButton = new FrameProButton();
        m_DataGridViewButton = new ViewButton();
        m_CustomStatsGraphButton = new ViewButton();
        m_ScopeColourModeButton = new FrameProButton();
        m_GotoNextSpikeButton = new FrameProButton();
        m_GotoPrevSpikeButton = new FrameProButton();
        m_InfoViewButton = new ViewButton();
        m_FindControl = new FindControl();
        m_ConnectButton = new FrameProButton();
        m_CoresViewButton = new ViewButton();
        m_ScopeViewButton = new ViewButton();
        m_FramesViewButton = new ViewButton();
        m_ConditionalScopeTimeSlider = new ConditionalScopeTimeSlider();
        m_GotoEndButton = new FrameProButton();
        m_TrackEndButton = new FrameProButton();
        m_GotoStartButton = new FrameProButton();
        m_ConnectSettingsButton = new FrameProButton();
        m_DisconnectButton = new FrameProButton();
        m_MainPanel = new Panel();
        fileToolStripMenuItem = new ToolStripMenuItem();
        openToolStripMenuItem = new ToolStripMenuItem();
        m_SaveMenuItem = new ToolStripMenuItem();
        m_SaveAsMenuItem = new ToolStripMenuItem();
        closeToolStripMenuItem = new ToolStripMenuItem();
        closeAllToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator9 = new ToolStripSeparator();
        m_ExportToCSVMenuItem = new ToolStripMenuItem();
        m_ExportFrameGraphToCSVMenuItem = new ToolStripMenuItem();
        toolStripSeparator2 = new ToolStripSeparator();
        m_RecentFilesMenuItem = new ToolStripMenuItem();
        toolStripSeparator4 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        viewToolStripMenuItem = new ToolStripMenuItem();
        m_ThreadsViewMenuItem = new ToolStripMenuItem();
        m_CoresViewMenuItem = new ToolStripMenuItem();
        m_ScopesViewMenuItem = new ToolStripMenuItem();
        toolStripSeparator6 = new ToolStripSeparator();
        m_ViewSettingsMenuItem = new ToolStripMenuItem();
        m_InfoMenuItem = new ToolStripMenuItem();
        m_FrameGraphMenuItem = new ToolStripMenuItem();
        m_ScopeGraphMenuItem = new ToolStripMenuItem();
        m_ThreadsViewCoreViewMenuItem = new ToolStripMenuItem();
        m_CustomStatsGraphMenuItem = new ToolStripMenuItem();
        colouringToolStripMenuItem = new ToolStripMenuItem();
        m_ColourByThreadMenuItem = new ToolStripMenuItem();
        m_ColourByScopeMenuItem = new ToolStripMenuItem();
        toolStripSeparator10 = new ToolStripSeparator();
        m_OutputWindowMenuItem = new ToolStripMenuItem();
        connectionToolStripMenuItem = new ToolStripMenuItem();
        m_NewConnectionMenuItem = new ToolStripMenuItem();
        m_ConnectMenuItem = new ToolStripMenuItem();
        m_DisconnectMenuItem = new ToolStripMenuItem();
        toolsToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        findToolStripMenuItem1 = new ToolStripMenuItem();
        toolStripSeparator5 = new ToolStripSeparator();
        m_CreateSessionFromSelectionMenuItem = new ToolStripMenuItem();
        toolStripSeparator7 = new ToolStripSeparator();
        androidToolStripMenuItem = new ToolStripMenuItem();
        recordContextSwitchesAndroidToolStripMenuItem = new ToolStripMenuItem();
        loadContextSwitchFileAndroidToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator11 = new ToolStripSeparator();
        settingsToolStripMenuItem1 = new ToolStripMenuItem();
        helpToolStripMenuItem = new ToolStripMenuItem();
        helpToolStripMenuItem1 = new ToolStripMenuItem();
        enterProductKeyToolStripMenuItem = new ToolStripMenuItem();
        checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator3 = new ToolStripSeparator();
        demoToolStripMenuItem = new ToolStripMenuItem();
        launchFrameProGameSimulatorToolStripMenuItem = new ToolStripMenuItem();
        playbackDumpFileInRealtimeToolStripMenuItem = new ToolStripMenuItem();
        showStartupPageToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator8 = new ToolStripSeparator();
        aboutToolStripMenuItem = new ToolStripMenuItem();
        menuStrip1 = new MenuStrip();
        m_OutputWindowPanel = new Panel();
        m_OutputTextBox = new TextBox();
        m_OutputWindowSplitter = new Splitter();
        panel1.SuspendLayout();
        menuStrip1.SuspendLayout();
        m_OutputWindowPanel.SuspendLayout();
        SuspendLayout();
        // 
        // panel1
        // 
        panel1.Controls.Add(m_GotoMaxFrameButton);
        panel1.Controls.Add(m_CallstackButton);
        panel1.Controls.Add(m_DataGridViewButton);
        panel1.Controls.Add(m_CustomStatsGraphButton);
        panel1.Controls.Add(m_ScopeColourModeButton);
        panel1.Controls.Add(m_GotoNextSpikeButton);
        panel1.Controls.Add(m_GotoPrevSpikeButton);
        panel1.Controls.Add(m_InfoViewButton);
        panel1.Controls.Add(m_FindControl);
        panel1.Controls.Add(m_ConnectButton);
        panel1.Controls.Add(m_CoresViewButton);
        panel1.Controls.Add(m_ScopeViewButton);
        panel1.Controls.Add(m_FramesViewButton);
        panel1.Controls.Add(m_ConditionalScopeTimeSlider);
        panel1.Controls.Add(m_GotoEndButton);
        panel1.Controls.Add(m_TrackEndButton);
        panel1.Controls.Add(m_GotoStartButton);
        panel1.Controls.Add(m_ConnectSettingsButton);
        panel1.Controls.Add(m_DisconnectButton);
        panel1.Dock = DockStyle.Top;
        panel1.Location = new Point(0, 36);
        panel1.Margin = new Padding(6);
        panel1.Name = "panel1";
        panel1.Size = new Size(3100, 120);
        panel1.TabIndex = 6;
        // 
        // m_GotoMaxFrameButton
        // 
        m_GotoMaxFrameButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_GotoMaxFrameButton.ButtonText = "Max";
        m_GotoMaxFrameButton.DisabledImage = (Image)resources.GetObject("m_GotoMaxFrameButton.DisabledImage");
        m_GotoMaxFrameButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_GotoMaxFrameButton.HighlightEnabled = true;
        m_GotoMaxFrameButton.Image = (Image)resources.GetObject("m_GotoMaxFrameButton.Image");
        m_GotoMaxFrameButton.Location = new Point(1511, 0);
        m_GotoMaxFrameButton.Margin = new Padding(6);
        m_GotoMaxFrameButton.Name = "m_GotoMaxFrameButton";
        m_GotoMaxFrameButton.Size = new Size(73, 120);
        m_GotoMaxFrameButton.TabIndex = 28;
        m_GotoMaxFrameButton.TabStop = false;
        m_GotoMaxFrameButton.UseImageAsText = false;
        m_GotoMaxFrameButton.Click += GotoMaxFrameButtonClicked;
        // 
        // m_CallstackButton
        // 
        m_CallstackButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_CallstackButton.ButtonText = "Callstacks";
        m_CallstackButton.DisabledImage = (Image)resources.GetObject("m_CallstackButton.DisabledImage");
        m_CallstackButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_CallstackButton.HighlightEnabled = true;
        m_CallstackButton.Image = (Image)resources.GetObject("m_CallstackButton.Image");
        m_CallstackButton.Location = new Point(2915, 0);
        m_CallstackButton.Margin = new Padding(6);
        m_CallstackButton.Name = "m_CallstackButton";
        m_CallstackButton.Size = new Size(119, 120);
        m_CallstackButton.TabIndex = 27;
        m_CallstackButton.TabStop = false;
        m_CallstackButton.UseImageAsText = false;
        m_CallstackButton.Click += OnCallstacksButtonClicked;
        // 
        // m_DataGridViewButton
        // 
        m_DataGridViewButton.ButtonText = "Details";
        m_DataGridViewButton.Checked = false;
        m_DataGridViewButton.DisabledImage = (Image)resources.GetObject("m_DataGridViewButton.DisabledImage");
        m_DataGridViewButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_DataGridViewButton.Image = (Image)resources.GetObject("m_DataGridViewButton.Image");
        m_DataGridViewButton.Location = new Point(1258, 0);
        m_DataGridViewButton.Margin = new Padding(6);
        m_DataGridViewButton.Name = "m_DataGridViewButton";
        m_DataGridViewButton.Size = new Size(119, 120);
        m_DataGridViewButton.TabIndex = 26;
        m_DataGridViewButton.CheckedChanged += DataGridViewButtonCheckedChanged;
        // 
        // m_CustomStatsGraphButton
        // 
        m_CustomStatsGraphButton.ButtonText = "Custom Stats";
        m_CustomStatsGraphButton.Checked = false;
        m_CustomStatsGraphButton.DisabledImage = (Image)resources.GetObject("m_CustomStatsGraphButton.DisabledImage");
        m_CustomStatsGraphButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_CustomStatsGraphButton.Image = (Image)resources.GetObject("m_CustomStatsGraphButton.Image");
        m_CustomStatsGraphButton.Location = new Point(1128, 0);
        m_CustomStatsGraphButton.Margin = new Padding(6);
        m_CustomStatsGraphButton.Name = "m_CustomStatsGraphButton";
        m_CustomStatsGraphButton.Size = new Size(119, 120);
        m_CustomStatsGraphButton.TabIndex = 25;
        m_CustomStatsGraphButton.CheckedChanged += CustomStatsGraphButtonCheckChanged;
        // 
        // m_ScopeColourModeButton
        // 
        m_ScopeColourModeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_ScopeColourModeButton.ButtonText = "Colour Mode";
        m_ScopeColourModeButton.DisabledImage = (Image)resources.GetObject("m_ScopeColourModeButton.DisabledImage");
        m_ScopeColourModeButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_ScopeColourModeButton.HighlightEnabled = true;
        m_ScopeColourModeButton.Image = (Image)resources.GetObject("m_ScopeColourModeButton.Image");
        m_ScopeColourModeButton.Location = new Point(2392, 0);
        m_ScopeColourModeButton.Margin = new Padding(6);
        m_ScopeColourModeButton.Name = "m_ScopeColourModeButton";
        m_ScopeColourModeButton.Size = new Size(119, 120);
        m_ScopeColourModeButton.TabIndex = 24;
        m_ScopeColourModeButton.TabStop = false;
        m_ScopeColourModeButton.UseImageAsText = false;
        m_ScopeColourModeButton.Click += ScopeColourModeButtonClicked;
        // 
        // m_GotoNextSpikeButton
        // 
        m_GotoNextSpikeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_GotoNextSpikeButton.ButtonText = "Next";
        m_GotoNextSpikeButton.DisabledImage = (Image)resources.GetObject("m_GotoNextSpikeButton.DisabledImage");
        m_GotoNextSpikeButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_GotoNextSpikeButton.HighlightEnabled = true;
        m_GotoNextSpikeButton.Image = (Image)resources.GetObject("m_GotoNextSpikeButton.Image");
        m_GotoNextSpikeButton.Location = new Point(1595, 0);
        m_GotoNextSpikeButton.Margin = new Padding(6);
        m_GotoNextSpikeButton.Name = "m_GotoNextSpikeButton";
        m_GotoNextSpikeButton.Size = new Size(73, 120);
        m_GotoNextSpikeButton.TabIndex = 23;
        m_GotoNextSpikeButton.TabStop = false;
        m_GotoNextSpikeButton.UseImageAsText = false;
        m_GotoNextSpikeButton.Click += GotoNextSpikeButtonClicked;
        // 
        // m_GotoPrevSpikeButton
        // 
        m_GotoPrevSpikeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_GotoPrevSpikeButton.ButtonText = "Prev";
        m_GotoPrevSpikeButton.DisabledImage = (Image)resources.GetObject("m_GotoPrevSpikeButton.DisabledImage");
        m_GotoPrevSpikeButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_GotoPrevSpikeButton.HighlightEnabled = true;
        m_GotoPrevSpikeButton.Image = (Image)resources.GetObject("m_GotoPrevSpikeButton.Image");
        m_GotoPrevSpikeButton.Location = new Point(1426, 0);
        m_GotoPrevSpikeButton.Margin = new Padding(6);
        m_GotoPrevSpikeButton.Name = "m_GotoPrevSpikeButton";
        m_GotoPrevSpikeButton.Size = new Size(73, 120);
        m_GotoPrevSpikeButton.TabIndex = 22;
        m_GotoPrevSpikeButton.TabStop = false;
        m_GotoPrevSpikeButton.UseImageAsText = false;
        m_GotoPrevSpikeButton.Click += GotoPrevSpikeButtonClicked;
        // 
        // m_InfoViewButton
        // 
        m_InfoViewButton.ButtonText = "Info";
        m_InfoViewButton.Checked = false;
        m_InfoViewButton.DisabledImage = (Image)resources.GetObject("m_InfoViewButton.DisabledImage");
        m_InfoViewButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_InfoViewButton.Image = (Image)resources.GetObject("m_InfoViewButton.Image");
        m_InfoViewButton.Location = new Point(607, 0);
        m_InfoViewButton.Margin = new Padding(6);
        m_InfoViewButton.Name = "m_InfoViewButton";
        m_InfoViewButton.Size = new Size(119, 120);
        m_InfoViewButton.TabIndex = 21;
        m_InfoViewButton.CheckedChanged += InfoViewCheckedChanged;
        // 
        // m_FindControl
        // 
        m_FindControl.Location = new Point(1716, 0);
        m_FindControl.Margin = new Padding(7);
        m_FindControl.Name = "m_FindControl";
        m_FindControl.Size = new Size(629, 120);
        m_FindControl.TabIndex = 12;
        m_FindControl.TabStop = false;
        m_FindControl.FindControlTextChanged += FindControlTextChanged;
        m_FindControl.FindControlGotoPrev += FindControlGotoPrev;
        m_FindControl.FindControlGotoNext += FindControlGotoNext;
        // 
        // m_ConnectButton
        // 
        m_ConnectButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_ConnectButton.ButtonText = "Connect";
        m_ConnectButton.DisabledImage = (Image)resources.GetObject("m_ConnectButton.DisabledImage");
        m_ConnectButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_ConnectButton.HighlightEnabled = true;
        m_ConnectButton.Image = (Image)resources.GetObject("m_ConnectButton.Image");
        m_ConnectButton.Location = new Point(0, 0);
        m_ConnectButton.Margin = new Padding(6);
        m_ConnectButton.Name = "m_ConnectButton";
        m_ConnectButton.Size = new Size(119, 120);
        m_ConnectButton.TabIndex = 0;
        m_ConnectButton.TabStop = false;
        m_ConnectButton.UseImageAsText = false;
        m_ConnectButton.Click += ConnectButtonClick;
        // 
        // m_CoresViewButton
        // 
        m_CoresViewButton.ButtonText = "Cores";
        m_CoresViewButton.Checked = false;
        m_CoresViewButton.DisabledImage = (Image)resources.GetObject("m_CoresViewButton.DisabledImage");
        m_CoresViewButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_CoresViewButton.Image = (Image)resources.GetObject("m_CoresViewButton.Image");
        m_CoresViewButton.Location = new Point(997, 0);
        m_CoresViewButton.Margin = new Padding(6);
        m_CoresViewButton.Name = "m_CoresViewButton";
        m_CoresViewButton.Size = new Size(119, 120);
        m_CoresViewButton.TabIndex = 20;
        m_CoresViewButton.CheckedChanged += CoresViewButtonCheckedChanged;
        // 
        // m_ScopeViewButton
        // 
        m_ScopeViewButton.ButtonText = "Scope";
        m_ScopeViewButton.Checked = false;
        m_ScopeViewButton.DisabledImage = (Image)resources.GetObject("m_ScopeViewButton.DisabledImage");
        m_ScopeViewButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_ScopeViewButton.Image = (Image)resources.GetObject("m_ScopeViewButton.Image");
        m_ScopeViewButton.Location = new Point(867, 0);
        m_ScopeViewButton.Margin = new Padding(6);
        m_ScopeViewButton.Name = "m_ScopeViewButton";
        m_ScopeViewButton.Size = new Size(119, 120);
        m_ScopeViewButton.TabIndex = 18;
        m_ScopeViewButton.CheckedChanged += ScopesViewButtonCheckedChanged;
        // 
        // m_FramesViewButton
        // 
        m_FramesViewButton.ButtonText = "Frames";
        m_FramesViewButton.Checked = false;
        m_FramesViewButton.DisabledImage = (Image)resources.GetObject("m_FramesViewButton.DisabledImage");
        m_FramesViewButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_FramesViewButton.Image = (Image)resources.GetObject("m_FramesViewButton.Image");
        m_FramesViewButton.Location = new Point(737, 0);
        m_FramesViewButton.Margin = new Padding(6);
        m_FramesViewButton.Name = "m_FramesViewButton";
        m_FramesViewButton.Size = new Size(119, 120);
        m_FramesViewButton.TabIndex = 17;
        m_FramesViewButton.CheckedChanged += FramesButtonCheckedChanged;
        // 
        // m_ConditionalScopeTimeSlider
        // 
        m_ConditionalScopeTimeSlider.Location = new Point(2545, 0);
        m_ConditionalScopeTimeSlider.Margin = new Padding(7);
        m_ConditionalScopeTimeSlider.Name = "m_ConditionalScopeTimeSlider";
        m_ConditionalScopeTimeSlider.Size = new Size(358, 120);
        m_ConditionalScopeTimeSlider.TabIndex = 10;
        m_ConditionalScopeTimeSlider.TabStop = false;
        m_ConditionalScopeTimeSlider.ValueChanged += ConditionalSliderValueChanged;
        // 
        // m_GotoEndButton
        // 
        m_GotoEndButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_GotoEndButton.ButtonText = "End";
        m_GotoEndButton.DisabledImage = (Image)resources.GetObject("m_GotoEndButton.DisabledImage");
        m_GotoEndButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_GotoEndButton.HighlightEnabled = true;
        m_GotoEndButton.Image = (Image)resources.GetObject("m_GotoEndButton.Image");
        m_GotoEndButton.Location = new Point(510, 0);
        m_GotoEndButton.Margin = new Padding(6);
        m_GotoEndButton.Name = "m_GotoEndButton";
        m_GotoEndButton.Size = new Size(73, 120);
        m_GotoEndButton.TabIndex = 9;
        m_GotoEndButton.TabStop = false;
        m_GotoEndButton.UseImageAsText = false;
        m_GotoEndButton.Click += GotoEndButtonClicked;
        // 
        // m_TrackEndButton
        // 
        m_TrackEndButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_TrackEndButton.ButtonText = "Track";
        m_TrackEndButton.DisabledImage = (Image)resources.GetObject("m_TrackEndButton.DisabledImage");
        m_TrackEndButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_TrackEndButton.HighlightEnabled = true;
        m_TrackEndButton.Image = (Image)resources.GetObject("m_TrackEndButton.Image");
        m_TrackEndButton.Location = new Point(425, 0);
        m_TrackEndButton.Margin = new Padding(6);
        m_TrackEndButton.Name = "m_TrackEndButton";
        m_TrackEndButton.Size = new Size(73, 120);
        m_TrackEndButton.TabIndex = 8;
        m_TrackEndButton.TabStop = false;
        m_TrackEndButton.UseImageAsText = false;
        m_TrackEndButton.Click += PlayPauseButtonClicked;
        // 
        // m_GotoStartButton
        // 
        m_GotoStartButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_GotoStartButton.ButtonText = "Start";
        m_GotoStartButton.DisabledImage = (Image)resources.GetObject("m_GotoStartButton.DisabledImage");
        m_GotoStartButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_GotoStartButton.HighlightEnabled = true;
        m_GotoStartButton.Image = (Image)resources.GetObject("m_GotoStartButton.Image");
        m_GotoStartButton.Location = new Point(341, 0);
        m_GotoStartButton.Margin = new Padding(6);
        m_GotoStartButton.Name = "m_GotoStartButton";
        m_GotoStartButton.Size = new Size(73, 120);
        m_GotoStartButton.TabIndex = 6;
        m_GotoStartButton.TabStop = false;
        m_GotoStartButton.UseImageAsText = false;
        m_GotoStartButton.Click += HomeButtonClicked;
        // 
        // m_ConnectSettingsButton
        // 
        m_ConnectSettingsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_ConnectSettingsButton.ButtonText = "";
        m_ConnectSettingsButton.DisabledImage = null;
        m_ConnectSettingsButton.HighlightEnabled = true;
        m_ConnectSettingsButton.Image = (Image)resources.GetObject("m_ConnectSettingsButton.Image");
        m_ConnectSettingsButton.Location = new Point(119, 0);
        m_ConnectSettingsButton.Margin = new Padding(6);
        m_ConnectSettingsButton.Name = "m_ConnectSettingsButton";
        m_ConnectSettingsButton.Size = new Size(31, 120);
        m_ConnectSettingsButton.TabIndex = 5;
        m_ConnectSettingsButton.TabStop = false;
        m_ConnectSettingsButton.UseImageAsText = true;
        m_ConnectSettingsButton.Click += ConnectSettingsButtonClicked;
        // 
        // m_DisconnectButton
        // 
        m_DisconnectButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        m_DisconnectButton.ButtonText = "Disconnect";
        m_DisconnectButton.DisabledImage = (Image)resources.GetObject("m_DisconnectButton.DisabledImage");
        m_DisconnectButton.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_DisconnectButton.HighlightEnabled = true;
        m_DisconnectButton.Image = (Image)resources.GetObject("m_DisconnectButton.Image");
        m_DisconnectButton.Location = new Point(161, 0);
        m_DisconnectButton.Margin = new Padding(6);
        m_DisconnectButton.Name = "m_DisconnectButton";
        m_DisconnectButton.Size = new Size(119, 120);
        m_DisconnectButton.TabIndex = 4;
        m_DisconnectButton.TabStop = false;
        m_DisconnectButton.UseImageAsText = false;
        m_DisconnectButton.Click += DisconnectButtonPressed;
        // 
        // m_MainPanel
        // 
        m_MainPanel.Dock = DockStyle.Fill;
        m_MainPanel.Location = new Point(0, 156);
        m_MainPanel.Margin = new Padding(6);
        m_MainPanel.Name = "m_MainPanel";
        m_MainPanel.Size = new Size(3100, 1060);
        m_MainPanel.TabIndex = 7;
        // 
        // fileToolStripMenuItem
        // 
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, m_SaveMenuItem, m_SaveAsMenuItem, closeToolStripMenuItem, closeAllToolStripMenuItem, toolStripSeparator9, m_ExportToCSVMenuItem, m_ExportFrameGraphToCSVMenuItem, toolStripSeparator2, m_RecentFilesMenuItem, toolStripSeparator4, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(56, 28);
        fileToolStripMenuItem.Text = "File";
        // 
        // openToolStripMenuItem
        // 
        openToolStripMenuItem.Name = "openToolStripMenuItem";
        openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        openToolStripMenuItem.Size = new Size(345, 34);
        openToolStripMenuItem.Text = "Open";
        openToolStripMenuItem.Click += OpenMenuItem;
        // 
        // m_SaveMenuItem
        // 
        m_SaveMenuItem.Name = "m_SaveMenuItem";
        m_SaveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        m_SaveMenuItem.Size = new Size(345, 34);
        m_SaveMenuItem.Text = "Save";
        m_SaveMenuItem.Click += SaveMenuItem;
        // 
        // m_SaveAsMenuItem
        // 
        m_SaveAsMenuItem.Name = "m_SaveAsMenuItem";
        m_SaveAsMenuItem.Size = new Size(345, 34);
        m_SaveAsMenuItem.Text = "Save As...";
        m_SaveAsMenuItem.Click += SaveAsMenuItemClicked;
        // 
        // closeToolStripMenuItem
        // 
        closeToolStripMenuItem.Name = "closeToolStripMenuItem";
        closeToolStripMenuItem.Size = new Size(345, 34);
        closeToolStripMenuItem.Text = "Close";
        closeToolStripMenuItem.Click += CloseMenuItem;
        // 
        // closeAllToolStripMenuItem
        // 
        closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
        closeAllToolStripMenuItem.Size = new Size(345, 34);
        closeAllToolStripMenuItem.Text = "Close All";
        closeAllToolStripMenuItem.Click += CloseAllMenuItemClicked;
        // 
        // toolStripSeparator9
        // 
        toolStripSeparator9.Name = "toolStripSeparator9";
        toolStripSeparator9.Size = new Size(342, 6);
        // 
        // m_ExportToCSVMenuItem
        // 
        m_ExportToCSVMenuItem.Name = "m_ExportToCSVMenuItem";
        m_ExportToCSVMenuItem.Size = new Size(345, 34);
        m_ExportToCSVMenuItem.Text = "Export to CSV";
        m_ExportToCSVMenuItem.Click += ExportToCSVMenuItem;
        // 
        // m_ExportFrameGraphToCSVMenuItem
        // 
        m_ExportFrameGraphToCSVMenuItem.Name = "m_ExportFrameGraphToCSVMenuItem";
        m_ExportFrameGraphToCSVMenuItem.Size = new Size(345, 34);
        m_ExportFrameGraphToCSVMenuItem.Text = "Export Frame Graph to CSV";
        m_ExportFrameGraphToCSVMenuItem.Click += ExportFrameGraphToCSVMenuItemClicked;
        // 
        // toolStripSeparator2
        // 
        toolStripSeparator2.Name = "toolStripSeparator2";
        toolStripSeparator2.Size = new Size(342, 6);
        // 
        // m_RecentFilesMenuItem
        // 
        m_RecentFilesMenuItem.Name = "m_RecentFilesMenuItem";
        m_RecentFilesMenuItem.Size = new Size(345, 34);
        m_RecentFilesMenuItem.Text = "Recent Files";
        // 
        // toolStripSeparator4
        // 
        toolStripSeparator4.Name = "toolStripSeparator4";
        toolStripSeparator4.Size = new Size(342, 6);
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(345, 34);
        exitToolStripMenuItem.Text = "Exit";
        exitToolStripMenuItem.Click += ExitMenuItem;
        // 
        // viewToolStripMenuItem
        // 
        viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { m_ThreadsViewMenuItem, m_CoresViewMenuItem, m_ScopesViewMenuItem, toolStripSeparator6, m_ViewSettingsMenuItem, colouringToolStripMenuItem, toolStripSeparator10, m_OutputWindowMenuItem });
        viewToolStripMenuItem.Name = "viewToolStripMenuItem";
        viewToolStripMenuItem.Size = new Size(67, 28);
        viewToolStripMenuItem.Text = "View";
        // 
        // m_ThreadsViewMenuItem
        // 
        m_ThreadsViewMenuItem.Name = "m_ThreadsViewMenuItem";
        m_ThreadsViewMenuItem.Size = new Size(252, 34);
        m_ThreadsViewMenuItem.Text = "Threads View";
        m_ThreadsViewMenuItem.Click += ThreadsViewMenuItemClicked;
        // 
        // m_CoresViewMenuItem
        // 
        m_CoresViewMenuItem.Name = "m_CoresViewMenuItem";
        m_CoresViewMenuItem.Size = new Size(252, 34);
        m_CoresViewMenuItem.Text = "Cores View";
        // 
        // m_ScopesViewMenuItem
        // 
        m_ScopesViewMenuItem.Name = "m_ScopesViewMenuItem";
        m_ScopesViewMenuItem.Size = new Size(252, 34);
        m_ScopesViewMenuItem.Text = "Scopes View";
        m_ScopesViewMenuItem.Click += ScopesViewMenuItemClicked;
        // 
        // toolStripSeparator6
        // 
        toolStripSeparator6.Name = "toolStripSeparator6";
        toolStripSeparator6.Size = new Size(249, 6);
        // 
        // m_ViewSettingsMenuItem
        // 
        m_ViewSettingsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { m_InfoMenuItem, m_FrameGraphMenuItem, m_ScopeGraphMenuItem, m_ThreadsViewCoreViewMenuItem, m_CustomStatsGraphMenuItem });
        m_ViewSettingsMenuItem.Name = "m_ViewSettingsMenuItem";
        m_ViewSettingsMenuItem.Size = new Size(252, 34);
        m_ViewSettingsMenuItem.Text = "Threads View";
        // 
        // m_InfoMenuItem
        // 
        m_InfoMenuItem.Checked = true;
        m_InfoMenuItem.CheckOnClick = true;
        m_InfoMenuItem.CheckState = CheckState.Checked;
        m_InfoMenuItem.Name = "m_InfoMenuItem";
        m_InfoMenuItem.Size = new Size(281, 34);
        m_InfoMenuItem.Text = "Info";
        m_InfoMenuItem.Click += InfoMenuItemClicked;
        // 
        // m_FrameGraphMenuItem
        // 
        m_FrameGraphMenuItem.Checked = true;
        m_FrameGraphMenuItem.CheckOnClick = true;
        m_FrameGraphMenuItem.CheckState = CheckState.Checked;
        m_FrameGraphMenuItem.Name = "m_FrameGraphMenuItem";
        m_FrameGraphMenuItem.Size = new Size(281, 34);
        m_FrameGraphMenuItem.Text = "Frame Graph";
        m_FrameGraphMenuItem.Click += FrameGraphMenuItemClicked;
        // 
        // m_ScopeGraphMenuItem
        // 
        m_ScopeGraphMenuItem.Checked = true;
        m_ScopeGraphMenuItem.CheckOnClick = true;
        m_ScopeGraphMenuItem.CheckState = CheckState.Checked;
        m_ScopeGraphMenuItem.Name = "m_ScopeGraphMenuItem";
        m_ScopeGraphMenuItem.Size = new Size(281, 34);
        m_ScopeGraphMenuItem.Text = "Scope Graph";
        m_ScopeGraphMenuItem.Click += ScopeGraphMenuItemClicked;
        // 
        // m_ThreadsViewCoreViewMenuItem
        // 
        m_ThreadsViewCoreViewMenuItem.Checked = true;
        m_ThreadsViewCoreViewMenuItem.CheckOnClick = true;
        m_ThreadsViewCoreViewMenuItem.CheckState = CheckState.Checked;
        m_ThreadsViewCoreViewMenuItem.Name = "m_ThreadsViewCoreViewMenuItem";
        m_ThreadsViewCoreViewMenuItem.Size = new Size(281, 34);
        m_ThreadsViewCoreViewMenuItem.Text = "CPU Graph";
        m_ThreadsViewCoreViewMenuItem.Click += CoreViewMenuItemClicked;
        // 
        // m_CustomStatsGraphMenuItem
        // 
        m_CustomStatsGraphMenuItem.Checked = true;
        m_CustomStatsGraphMenuItem.CheckOnClick = true;
        m_CustomStatsGraphMenuItem.CheckState = CheckState.Checked;
        m_CustomStatsGraphMenuItem.Name = "m_CustomStatsGraphMenuItem";
        m_CustomStatsGraphMenuItem.Size = new Size(281, 34);
        m_CustomStatsGraphMenuItem.Text = "Custom Stats Graph";
        m_CustomStatsGraphMenuItem.Click += CustomStatsGraphMenuItemClicked;
        // 
        // colouringToolStripMenuItem
        // 
        colouringToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { m_ColourByThreadMenuItem, m_ColourByScopeMenuItem });
        colouringToolStripMenuItem.Name = "colouringToolStripMenuItem";
        colouringToolStripMenuItem.Size = new Size(252, 34);
        colouringToolStripMenuItem.Text = "Scope Colouring";
        // 
        // m_ColourByThreadMenuItem
        // 
        m_ColourByThreadMenuItem.Name = "m_ColourByThreadMenuItem";
        m_ColourByThreadMenuItem.Size = new Size(259, 34);
        m_ColourByThreadMenuItem.Text = "Colour by Thread";
        // 
        // m_ColourByScopeMenuItem
        // 
        m_ColourByScopeMenuItem.Name = "m_ColourByScopeMenuItem";
        m_ColourByScopeMenuItem.Size = new Size(259, 34);
        m_ColourByScopeMenuItem.Text = "Colour by Scope";
        // 
        // toolStripSeparator10
        // 
        toolStripSeparator10.Name = "toolStripSeparator10";
        toolStripSeparator10.Size = new Size(249, 6);
        // 
        // m_OutputWindowMenuItem
        // 
        m_OutputWindowMenuItem.Checked = true;
        m_OutputWindowMenuItem.CheckState = CheckState.Checked;
        m_OutputWindowMenuItem.Name = "m_OutputWindowMenuItem";
        m_OutputWindowMenuItem.Size = new Size(252, 34);
        m_OutputWindowMenuItem.Text = "Output Window";
        m_OutputWindowMenuItem.Click += OutputWindowMenuItem;
        // 
        // connectionToolStripMenuItem
        // 
        connectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { m_NewConnectionMenuItem, m_ConnectMenuItem, m_DisconnectMenuItem });
        connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
        connectionToolStripMenuItem.Size = new Size(124, 28);
        connectionToolStripMenuItem.Text = "Connection";
        // 
        // m_NewConnectionMenuItem
        // 
        m_NewConnectionMenuItem.Name = "m_NewConnectionMenuItem";
        m_NewConnectionMenuItem.Size = new Size(264, 34);
        m_NewConnectionMenuItem.Text = "New Connection...";
        m_NewConnectionMenuItem.Click += NewConnectionMenuItem;
        // 
        // m_ConnectMenuItem
        // 
        m_ConnectMenuItem.Name = "m_ConnectMenuItem";
        m_ConnectMenuItem.Size = new Size(264, 34);
        m_ConnectMenuItem.Text = "Connect...";
        m_ConnectMenuItem.Click += ConnectMenuItemClicked;
        // 
        // m_DisconnectMenuItem
        // 
        m_DisconnectMenuItem.Name = "m_DisconnectMenuItem";
        m_DisconnectMenuItem.Size = new Size(264, 34);
        m_DisconnectMenuItem.Text = "Disconnect";
        m_DisconnectMenuItem.Click += DisconnectButtonClicked;
        // 
        // toolsToolStripMenuItem
        // 
        toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripSeparator1, findToolStripMenuItem1, toolStripSeparator5, m_CreateSessionFromSelectionMenuItem, toolStripSeparator7, androidToolStripMenuItem, toolStripSeparator11, settingsToolStripMenuItem1 });
        toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
        toolsToolStripMenuItem.Size = new Size(71, 28);
        toolsToolStripMenuItem.Text = "Tools";
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(360, 6);
        // 
        // findToolStripMenuItem1
        // 
        findToolStripMenuItem1.Name = "findToolStripMenuItem1";
        findToolStripMenuItem1.ShortcutKeys = Keys.Control | Keys.F;
        findToolStripMenuItem1.Size = new Size(363, 34);
        findToolStripMenuItem1.Text = "Find";
        findToolStripMenuItem1.Click += FindMenuItemClicked;
        // 
        // toolStripSeparator5
        // 
        toolStripSeparator5.Name = "toolStripSeparator5";
        toolStripSeparator5.Size = new Size(360, 6);
        // 
        // m_CreateSessionFromSelectionMenuItem
        // 
        m_CreateSessionFromSelectionMenuItem.Name = "m_CreateSessionFromSelectionMenuItem";
        m_CreateSessionFromSelectionMenuItem.Size = new Size(363, 34);
        m_CreateSessionFromSelectionMenuItem.Text = "Create Session from Selection";
        m_CreateSessionFromSelectionMenuItem.Click += CreateSessionFromSelectionMenuItemClicked;
        // 
        // toolStripSeparator7
        // 
        toolStripSeparator7.Name = "toolStripSeparator7";
        toolStripSeparator7.Size = new Size(360, 6);
        // 
        // androidToolStripMenuItem
        // 
        androidToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { recordContextSwitchesAndroidToolStripMenuItem, loadContextSwitchFileAndroidToolStripMenuItem });
        androidToolStripMenuItem.Name = "androidToolStripMenuItem";
        androidToolStripMenuItem.Size = new Size(363, 34);
        androidToolStripMenuItem.Text = "Android";
        // 
        // recordContextSwitchesAndroidToolStripMenuItem
        // 
        recordContextSwitchesAndroidToolStripMenuItem.Name = "recordContextSwitchesAndroidToolStripMenuItem";
        recordContextSwitchesAndroidToolStripMenuItem.Size = new Size(408, 34);
        recordContextSwitchesAndroidToolStripMenuItem.Text = "Start Recording Context Switches...";
        recordContextSwitchesAndroidToolStripMenuItem.Click += RecordContextSwitchesAndroidMenuItemClicked;
        // 
        // loadContextSwitchFileAndroidToolStripMenuItem
        // 
        loadContextSwitchFileAndroidToolStripMenuItem.Name = "loadContextSwitchFileAndroidToolStripMenuItem";
        loadContextSwitchFileAndroidToolStripMenuItem.Size = new Size(408, 34);
        loadContextSwitchFileAndroidToolStripMenuItem.Text = "Load Context Switch File";
        loadContextSwitchFileAndroidToolStripMenuItem.Click += LoadContextSwitchFileAndroid;
        // 
        // toolStripSeparator11
        // 
        toolStripSeparator11.Name = "toolStripSeparator11";
        toolStripSeparator11.Size = new Size(360, 6);
        // 
        // settingsToolStripMenuItem1
        // 
        settingsToolStripMenuItem1.Name = "settingsToolStripMenuItem1";
        settingsToolStripMenuItem1.Size = new Size(363, 34);
        settingsToolStripMenuItem1.Text = "Settings";
        settingsToolStripMenuItem1.Click += SettingsMenuItemClicked;
        // 
        // helpToolStripMenuItem
        // 
        helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripMenuItem1, enterProductKeyToolStripMenuItem, checkForUpdatesToolStripMenuItem, toolStripSeparator3, demoToolStripMenuItem, showStartupPageToolStripMenuItem, toolStripSeparator8, aboutToolStripMenuItem });
        helpToolStripMenuItem.Name = "helpToolStripMenuItem";
        helpToolStripMenuItem.Size = new Size(67, 28);
        helpToolStripMenuItem.Text = "Help";
        ////// 
        ////// helpToolStripMenuItem1
        ////// 
        ////helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
        ////helpToolStripMenuItem1.Size = new Size(268, 34);
        ////helpToolStripMenuItem1.Text = "View Help";
        ////helpToolStripMenuItem1.Click += HelpMenuItemClicked;
        // 
        // enterProductKeyToolStripMenuItem
        // 
        enterProductKeyToolStripMenuItem.Name = "enterProductKeyToolStripMenuItem";
        enterProductKeyToolStripMenuItem.Size = new Size(268, 34);
        enterProductKeyToolStripMenuItem.Text = "Registration...";
        enterProductKeyToolStripMenuItem.Click += RegistrationMenuItemClicked;
        // 
        // checkForUpdatesToolStripMenuItem
        // 
        checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
        checkForUpdatesToolStripMenuItem.Size = new Size(268, 34);
        checkForUpdatesToolStripMenuItem.Text = "Check for Updates";
        checkForUpdatesToolStripMenuItem.Click += CheckForUpdatesMenuItemClicked;
        // 
        // toolStripSeparator3
        // 
        toolStripSeparator3.Name = "toolStripSeparator3";
        toolStripSeparator3.Size = new Size(265, 6);
        // 
        // demoToolStripMenuItem
        // 
        demoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { launchFrameProGameSimulatorToolStripMenuItem, playbackDumpFileInRealtimeToolStripMenuItem });
        demoToolStripMenuItem.Name = "demoToolStripMenuItem";
        demoToolStripMenuItem.Size = new Size(268, 34);
        demoToolStripMenuItem.Text = "Demo";
        // 
        // launchFrameProGameSimulatorToolStripMenuItem
        // 
        launchFrameProGameSimulatorToolStripMenuItem.Name = "launchFrameProGameSimulatorToolStripMenuItem";
        launchFrameProGameSimulatorToolStripMenuItem.Size = new Size(402, 34);
        launchFrameProGameSimulatorToolStripMenuItem.Text = "Launch FramePro Game Simulator";
        launchFrameProGameSimulatorToolStripMenuItem.Click += LaunchGameSimulatorMenuItem;
        // 
        // playbackDumpFileInRealtimeToolStripMenuItem
        // 
        playbackDumpFileInRealtimeToolStripMenuItem.Name = "playbackDumpFileInRealtimeToolStripMenuItem";
        playbackDumpFileInRealtimeToolStripMenuItem.Size = new Size(402, 34);
        playbackDumpFileInRealtimeToolStripMenuItem.Text = "Playback Recording File...";
        playbackDumpFileInRealtimeToolStripMenuItem.Click += PlaybackRecordingFileInRealtime;
        // 
        // showStartupPageToolStripMenuItem
        // 
        showStartupPageToolStripMenuItem.Name = "showStartupPageToolStripMenuItem";
        showStartupPageToolStripMenuItem.Size = new Size(268, 34);
        // 
        // toolStripSeparator8
        // 
        toolStripSeparator8.Name = "toolStripSeparator8";
        toolStripSeparator8.Size = new Size(265, 6);
        // 
        // aboutToolStripMenuItem
        // 
        aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
        aboutToolStripMenuItem.Size = new Size(268, 34);
        aboutToolStripMenuItem.Text = "About";
        aboutToolStripMenuItem.Click += AboutButtonClicked;
        // 
        // menuStrip1
        // 
        menuStrip1.ImageScalingSize = new Size(24, 24);
        menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, connectionToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Padding = new Padding(11, 4, 0, 4);
        menuStrip1.Size = new Size(3100, 36);
        menuStrip1.TabIndex = 3;
        menuStrip1.Text = "menuStrip1";
        // 
        // m_OutputWindowPanel
        // 
        m_OutputWindowPanel.Controls.Add(m_OutputTextBox);
        m_OutputWindowPanel.Dock = DockStyle.Bottom;
        m_OutputWindowPanel.Location = new Point(0, 1222);
        m_OutputWindowPanel.Margin = new Padding(6);
        m_OutputWindowPanel.Name = "m_OutputWindowPanel";
        m_OutputWindowPanel.Size = new Size(3100, 369);
        m_OutputWindowPanel.TabIndex = 8;
        // 
        // m_OutputTextBox
        // 
        m_OutputTextBox.Dock = DockStyle.Fill;
        m_OutputTextBox.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        m_OutputTextBox.Location = new Point(0, 0);
        m_OutputTextBox.Margin = new Padding(6);
        m_OutputTextBox.Multiline = true;
        m_OutputTextBox.Name = "m_OutputTextBox";
        m_OutputTextBox.ScrollBars = ScrollBars.Both;
        m_OutputTextBox.Size = new Size(3100, 369);
        m_OutputTextBox.TabIndex = 0;
        m_OutputTextBox.Resize += OutputWindowResize;
        // 
        // m_OutputWindowSplitter
        // 
        m_OutputWindowSplitter.Dock = DockStyle.Bottom;
        m_OutputWindowSplitter.Location = new Point(0, 1216);
        m_OutputWindowSplitter.Margin = new Padding(6);
        m_OutputWindowSplitter.Name = "m_OutputWindowSplitter";
        m_OutputWindowSplitter.Size = new Size(3100, 6);
        m_OutputWindowSplitter.TabIndex = 9;
        m_OutputWindowSplitter.TabStop = false;
        // 
        // MainForm
        // 
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(11F, 24F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(3100, 1591);
        Controls.Add(m_MainPanel);
        Controls.Add(m_OutputWindowSplitter);
        Controls.Add(m_OutputWindowPanel);
        Controls.Add(panel1);
        Controls.Add(menuStrip1);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = menuStrip1;
        Margin = new Padding(6);
        Name = "MainForm";
        Text = "ProfilerStudy";
        WindowState = FormWindowState.Maximized;
        panel1.ResumeLayout(false);
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        m_OutputWindowPanel.ResumeLayout(false);
        m_OutputWindowPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
