using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class HoverBox : Form
{
	private struct LinePair
	{
		public string m_Key;

		public string m_Value;

		public LinePair(string key, string value)
		{
			m_Key = key;
			m_Value = value;
		}
	}

	private const int SW_SHOWNOACTIVATE = 4;

	private const int HWND_TOPMOST = -1;

	private const uint SWP_NOACTIVATE = 16u;

	private object m_Target;

	private const int m_Padding = 4;

	private const int m_TabIndentOffset = 10;

	private int m_ValueTabIndent;

	private string m_Title;

	private List<LinePair> m_Lines = new List<LinePair>();

	private Font m_BoldFont;

	private int m_MinWidth;

	private IContainer components;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams obj = base.CreateParams;
			obj.ExStyle |= 128;
			obj.ExStyle |= 32;
			obj.ExStyle |= 8;
			obj.ExStyle |= 134217728;
			return obj;
		}
	}

	public object Target
	{
		get
		{
			return m_Target;
		}
		set
		{
			m_Target = value;
		}
	}

	public string Title
	{
		get
		{
			return m_Title;
		}
		set
		{
			m_Title = value;
		}
	}

	private int LineHeight => Font.Height / 1;

	public int MinWidth
	{
		get
		{
			return m_MinWidth;
		}
		set
		{
			m_MinWidth = value;
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(int hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	public HoverBox()
	{
		InitializeComponent();
		SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
		SetStyle(ControlStyles.Selectable, value: false);
		base.Visible = false;
		m_BoldFont = new Font(Font, FontStyle.Bold);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		m_BoldFont.Dispose();
		base.Dispose(disposing);
	}

	public void Clear()
	{
		m_Lines.Clear();
		m_Title = null;
	}

	public void AddLine(string key, string value)
	{
		m_Lines.Add(new LinePair(key, value));
	}

	public void SubmitLines()
	{
		Graphics graphics = CreateGraphics();
		int num = 0;
		foreach (LinePair line in m_Lines)
		{
			int num2 = (int)graphics.MeasureString(line.m_Key, m_BoldFont).Width;
			if (num2 > num)
			{
				num = num2;
			}
		}
		m_ValueTabIndent = num + 10;
		int val = (int)graphics.MeasureString(m_Title, m_BoldFont).Width;
		int num3 = Math.Max(m_MinWidth, val);
		foreach (LinePair line2 in m_Lines)
		{
			SizeF sizeF = graphics.MeasureString(line2.m_Value, Font);
			int num4 = m_ValueTabIndent + (int)sizeF.Width;
			if (num4 > num3)
			{
				num3 = num4;
			}
		}
		int num5 = m_Lines.Count * LineHeight;
		if (!string.IsNullOrEmpty(m_Title))
		{
			num5 += 3 * LineHeight / 2;
		}
		base.Size = new Size(num3 + 8, num5 + 8);
		graphics.Dispose();
		Refresh();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(SystemColors.Control);
		int num = 4;
		int num2 = 4;
		if (!string.IsNullOrEmpty(m_Title))
		{
			num2 -= 2;
			SizeF sizeF = e.Graphics.MeasureString(m_Title, m_BoldFont);
			int num3 = (base.Width - (int)sizeF.Width) / 2;
			e.Graphics.DrawString(m_Title, m_BoldFont, Brushes.Black, num3, num2);
			num2 += LineHeight;
			num2 += LineHeight / 4;
			e.Graphics.DrawLine(Pens.Gray, 4, num2, base.Width - 4, num2);
			num2 += LineHeight / 4;
			num2 += 2;
		}
		foreach (LinePair line in m_Lines)
		{
			e.Graphics.DrawString(line.m_Key, m_BoldFont, Brushes.Black, num, num2);
			e.Graphics.DrawString(line.m_Value, Font, Brushes.Black, num + m_ValueTabIndent, num2);
			num2 += LineHeight;
		}
		base.OnPaint(e);
	}

	public void SetLocation(Point screen_pt)
	{
		screen_pt += new Size(10, 10);
		int value = screen_pt.X;
		int value2 = screen_pt.Y;
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			if (screen.Bounds.Contains(screen_pt))
			{
				value = Misc.Clamp(value, screen.WorkingArea.X, screen.WorkingArea.Right - base.Width);
				value2 = Misc.Clamp(value2, screen.WorkingArea.Y, screen.WorkingArea.Bottom - base.Height);
				break;
			}
		}
		SetWindowPos(base.Handle.ToInt32(), -1, value, value2, base.Width, base.Height, 16u);
	}

	public void CopyToClipboard()
	{
		string text = "";
		foreach (LinePair line in m_Lines)
		{
			text = text + line.m_Key + "\t" + line.m_Value + "\r\n";
		}
		Clipboard.SetText(text);
	}

	private void InitializeComponent()
	{
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.White;
		base.ClientSize = new System.Drawing.Size(151, 80);
		base.Enabled = false;
		this.Font = new System.Drawing.Font("Monaco", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		base.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
		base.Name = "HoverBox";
		base.Opacity = 0.9;
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		this.Text = "TimerInfoBox";
		base.ResumeLayout(false);
	}
}
