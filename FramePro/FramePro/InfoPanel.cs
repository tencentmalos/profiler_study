using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class InfoPanel : UserControl
{
	private Session m_Session;

	private string m_IP;

	private bool m_Connected;

	private string m_SessionName;

	private string m_SessionBuildId;

	private string m_SessionDate;

	private string m_Platform;

	private string m_SendBufferSize;

	private string m_StringSize;

	private string m_MiscSize;

	private IContainer components;

	private SessionInfoDataGrid m_ConnectionDataGrid;

	private SessionInfoDataGrid m_MemoryDataGrid;

	private SessionInfoDataGrid m_SessionStatsDataGrid;

	private SessionInfoDataGrid m_FrameProMemoryDataGrid;

	private SessionInfoDataGrid m_SessionDetailsDataGrid;

	private SessionInfoDataGrid m_SessionInfoDataGrid;

	public int PreferredHeight => m_ConnectionDataGrid.PreferredHeight;

	public InfoPanel()
	{
		InitializeComponent();
		InitialiseConnectionDataGrid();
		InitialiseSessionDetailsDataGrid();
		InitialiseSessionInfoDataGrid();
		InitialiseMemoryDataGrid();
		InitialiseSessionDataGrid();
		InitialiseFrameProMemoryDataGrid();
	}

	public void SetSession(Session session)
	{
		m_Session = session;
	}

	private void InitialiseConnectionDataGrid()
	{
		m_ConnectionDataGrid.DataGrid.Rows.Add("IP", "----");
		m_ConnectionDataGrid.DataGrid.Rows.Add("Status", "Disconnected");
		m_ConnectionDataGrid.DataGrid.Rows.Add("Time", "00:00");
		m_ConnectionDataGrid.DataGrid.Rows.Add("Buffered", new PacketBufferTimeCell());
		m_ConnectionDataGrid.DataGrid.RefreshDataGrid();
	}

	private void InitialiseSessionDetailsDataGrid()
	{
		m_SessionDetailsDataGrid.DataGrid.Rows.Add("Name", "not set");
		m_SessionDetailsDataGrid.DataGrid.Rows.Add("Build Id", "not set");
		m_SessionDetailsDataGrid.DataGrid.Rows.Add("Date", "not set");
		m_SessionDetailsDataGrid.DataGrid.Rows.Add("Platform", "not set");
		m_SessionDetailsDataGrid.DataGrid.RefreshDataGrid();
	}

	private void InitialiseSessionInfoDataGrid()
	{
		m_SessionInfoDataGrid.DataGrid.Rows.Add("-", "-");
		m_SessionInfoDataGrid.DataGrid.Rows.Add("-", "-");
		m_SessionInfoDataGrid.DataGrid.Rows.Add("-", "-");
		m_SessionInfoDataGrid.DataGrid.Rows.Add("-", "-");
		m_SessionInfoDataGrid.DataGrid.RefreshDataGrid();
	}

	private void InitialiseMemoryDataGrid()
	{
		m_MemoryDataGrid.DataGrid.Rows.Add("Send Buffer", "0 MB");
		m_MemoryDataGrid.DataGrid.Rows.Add("Strings", "0 MB");
		m_MemoryDataGrid.DataGrid.Rows.Add("Misc", "0 MB");
		m_MemoryDataGrid.DataGrid.Rows.Add("Total", "0 MB");
	}

	private void InitialiseSessionDataGrid()
	{
		m_SessionStatsDataGrid.DataGrid.Rows.Add("Frames", "0");
		m_SessionStatsDataGrid.DataGrid.Rows.Add("Threads", "0");
		m_SessionStatsDataGrid.DataGrid.Rows.Add("Scopes", "0");
		m_SessionStatsDataGrid.DataGrid.Rows.Add("Scopes / frame", "0");
	}

	private void InitialiseFrameProMemoryDataGrid()
	{
		m_FrameProMemoryDataGrid.DataGrid.Rows.Add("Memory (working set)", "0");
	}

	protected override void OnVisibleChanged(EventArgs e)
	{
		if (base.Visible)
		{
			Update();
		}
		base.OnVisibleChanged(e);
	}

	public void UpdateInfo()
	{
		UpdateConnectionDataGrid();
		UpdateSessionDetailsDataGrid();
		UpdateSessionInfoDataGrid();
		UpdateMemoryDartaGrid();
		UpdateSessionDataGrid();
		UpdateFrameProMemoryDataGrid();
	}

	private void UpdateConnectionDataGrid()
	{
		if (m_Session.IsReady)
		{
			string text = m_Session.IP;
			if (string.IsNullOrEmpty(text))
			{
				text = "----";
			}
			if (text != m_IP)
			{
				m_ConnectionDataGrid.DataGrid.Rows[0].Cells[1].Value = text;
				m_IP = text;
			}
			if (m_Connected != m_Session.Connected)
			{
				m_ConnectionDataGrid.DataGrid.Rows[1].Cells[1].Value = (m_Session.Connected ? "Connected" : "Disconnected");
				m_Connected = m_Session.Connected;
			}
			string timeAsMinSecString = Utils.GetTimeAsMinSecString(m_Session.TotalConnectTime, m_Session.TimerFrequency);
			m_ConnectionDataGrid.DataGrid.Rows[2].Cells[1].Value = timeAsMinSecString;
			PacketBufferTimeCell packetBufferTimeCell = (PacketBufferTimeCell)m_ConnectionDataGrid.DataGrid.Rows[3].Cells[1].Value;
			if (m_Session.Recorded)
			{
				packetBufferTimeCell.PercentComplete = m_Session.ProcessingCompletePercent;
			}
			else
			{
				packetBufferTimeCell.Time = m_Session.BufferTime;
			}
			m_ConnectionDataGrid.DataGrid.Refresh();
		}
	}

	private void UpdateSessionDetailsDataGrid()
	{
		string text = m_Session.SessionDetails.m_Name;
		string text2 = m_Session.SessionDetails.m_BuildId;
		string date = m_Session.SessionDetails.m_Date;
		string text3 = m_Session.Platform.ToString();
		if (string.IsNullOrEmpty(text))
		{
			text = "Not Set";
		}
		if (string.IsNullOrEmpty(text2))
		{
			text2 = "Not Set";
		}
		if (text != m_SessionName || text2 != m_SessionBuildId || date != m_SessionDate || text3 != m_Platform)
		{
			m_SessionName = text;
			m_SessionBuildId = text2;
			m_SessionDate = date;
			m_Platform = text3;
			m_SessionDetailsDataGrid.DataGrid.Rows[0].Cells[1].Value = text;
			m_SessionDetailsDataGrid.DataGrid.Rows[1].Cells[1].Value = text2;
			m_SessionDetailsDataGrid.DataGrid.Rows[2].Cells[1].Value = date;
			m_SessionDetailsDataGrid.DataGrid.Rows[3].Cells[1].Value = text3;
			m_SessionDetailsDataGrid.DataGrid.Refresh();
		}
	}

	private void UpdateSessionInfoDataGrid()
	{
		List<SessionInfoPair> sessionInfoValues = m_Session.GetSessionInfoValues();
		Math.Min(sessionInfoValues.Count, 4);
		int num = 0;
		foreach (SessionInfoPair item in sessionInfoValues)
		{
			string @string = m_Session.GetString(item.m_Name);
			string text = @string.ToLower();
			if (text.ToLower() != "name" && text != "build id" && text != "buildid" && text != "date")
			{
				string string2 = m_Session.GetString(item.m_Value);
				m_SessionInfoDataGrid.DataGrid.Rows[num].Cells[0].Value = @string;
				m_SessionInfoDataGrid.DataGrid.Rows[num].Cells[1].Value = string2;
				num++;
				if (num == m_SessionInfoDataGrid.DataGrid.Rows.Count)
				{
					break;
				}
			}
		}
		m_SessionInfoDataGrid.DataGrid.RefreshDataGrid();
	}

	private void UpdateMemoryDartaGrid()
	{
		long sendBufferSize = m_Session.SendBufferSize;
		long stringMemorySize = m_Session.StringMemorySize;
		long miscMemorySize = m_Session.MiscMemorySize;
		string memoryString = Utils.GetMemoryString(sendBufferSize);
		string memoryString2 = Utils.GetMemoryString(stringMemorySize);
		string memoryString3 = Utils.GetMemoryString(miscMemorySize);
		if (memoryString != m_SendBufferSize || memoryString2 != m_StringSize || memoryString3 != m_MiscSize)
		{
			m_SendBufferSize = memoryString;
			m_StringSize = memoryString2;
			m_MiscSize = memoryString3;
			string memoryString4 = Utils.GetMemoryString(sendBufferSize + stringMemorySize);
			int num = 0;
			m_MemoryDataGrid.DataGrid.Rows[num++].Cells[1].Value = memoryString;
			m_MemoryDataGrid.DataGrid.Rows[num++].Cells[1].Value = memoryString2;
			m_MemoryDataGrid.DataGrid.Rows[num++].Cells[1].Value = memoryString3;
			m_MemoryDataGrid.DataGrid.Rows[num++].Cells[1].Value = memoryString4;
			m_MemoryDataGrid.DataGrid.Refresh();
		}
	}

	private void UpdateSessionDataGrid()
	{
		long num = m_Session.FrameCount;
		long timeSpanCount = m_Session.TimeSpanCount;
		int num2 = 0;
		m_SessionStatsDataGrid.DataGrid.Rows[num2++].Cells[1].Value = num;
		m_SessionStatsDataGrid.DataGrid.Rows[num2++].Cells[1].Value = m_Session.ThreadCount.ToString();
		m_SessionStatsDataGrid.DataGrid.Rows[num2++].Cells[1].Value = timeSpanCount;
		m_SessionStatsDataGrid.DataGrid.Rows[num2++].Cells[1].Value = ((num != 0L) ? (timeSpanCount / num) : 0);
		m_SessionStatsDataGrid.DataGrid.Refresh();
	}

	private void UpdateFrameProMemoryDataGrid()
	{
		long workingSet = Process.GetCurrentProcess().WorkingSet64;
		int num = 0;
		m_FrameProMemoryDataGrid.DataGrid.Rows[num++].Cells[1].Value = workingSet / 1024 / 1024 + " MB";
		m_FrameProMemoryDataGrid.DataGrid.Refresh();
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
		this.m_FrameProMemoryDataGrid = new FramePro.SessionInfoDataGrid();
		this.m_SessionStatsDataGrid = new FramePro.SessionInfoDataGrid();
		this.m_MemoryDataGrid = new FramePro.SessionInfoDataGrid();
		this.m_SessionDetailsDataGrid = new FramePro.SessionInfoDataGrid();
		this.m_ConnectionDataGrid = new FramePro.SessionInfoDataGrid();
		this.m_SessionInfoDataGrid = new FramePro.SessionInfoDataGrid();
		base.SuspendLayout();
		this.m_FrameProMemoryDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_FrameProMemoryDataGrid.Location = new System.Drawing.Point(1150, 0);
		this.m_FrameProMemoryDataGrid.Name = "m_FrameProMemoryDataGrid";
		this.m_FrameProMemoryDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_FrameProMemoryDataGrid.TabIndex = 5;
		this.m_FrameProMemoryDataGrid.Title = "FramePro Memory";
		this.m_SessionStatsDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_SessionStatsDataGrid.Location = new System.Drawing.Point(920, 0);
		this.m_SessionStatsDataGrid.Name = "m_SessionStatsDataGrid";
		this.m_SessionStatsDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_SessionStatsDataGrid.TabIndex = 4;
		this.m_SessionStatsDataGrid.Title = "Session Stats";
		this.m_MemoryDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_MemoryDataGrid.Location = new System.Drawing.Point(690, 0);
		this.m_MemoryDataGrid.Name = "m_MemoryDataGrid";
		this.m_MemoryDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_MemoryDataGrid.TabIndex = 2;
		this.m_MemoryDataGrid.Title = "App Memory Overhead";
		this.m_SessionDetailsDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_SessionDetailsDataGrid.Location = new System.Drawing.Point(230, 0);
		this.m_SessionDetailsDataGrid.Name = "m_SessionDetailsDataGrid";
		this.m_SessionDetailsDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_SessionDetailsDataGrid.TabIndex = 6;
		this.m_SessionDetailsDataGrid.Title = "Session";
		this.m_ConnectionDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_ConnectionDataGrid.Location = new System.Drawing.Point(0, 0);
		this.m_ConnectionDataGrid.Name = "m_ConnectionDataGrid";
		this.m_ConnectionDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_ConnectionDataGrid.TabIndex = 1;
		this.m_ConnectionDataGrid.Title = "Connection";
		this.m_SessionInfoDataGrid.Dock = System.Windows.Forms.DockStyle.Left;
		this.m_SessionInfoDataGrid.Location = new System.Drawing.Point(460, 0);
		this.m_SessionInfoDataGrid.Name = "m_CustomSessionInfoDataGrid";
		this.m_SessionInfoDataGrid.Size = new System.Drawing.Size(230, 78);
		this.m_SessionInfoDataGrid.TabIndex = 7;
		this.m_SessionInfoDataGrid.Title = "Custom Session Info";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.WhiteSmoke;
		base.Controls.Add(this.m_FrameProMemoryDataGrid);
		base.Controls.Add(this.m_SessionStatsDataGrid);
		base.Controls.Add(this.m_MemoryDataGrid);
		base.Controls.Add(this.m_SessionInfoDataGrid);
		base.Controls.Add(this.m_SessionDetailsDataGrid);
		base.Controls.Add(this.m_ConnectionDataGrid);
		base.Name = "InfoPanel";
		base.Size = new System.Drawing.Size(1342, 78);
		base.ResumeLayout(false);
	}
}
