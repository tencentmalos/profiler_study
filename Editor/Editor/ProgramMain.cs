using System;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Editor;

public class ProgramMain
{
	private static RenderableMainform m_MainForm;

	private const float m_MinStep = 1f / 60f;

	private const float m_MaxStep = 0.1f;

	private static long m_LastUpdateTime;

	internal static bool IReallyDoNeedAnUpdate => Environment.TickCount - m_LastUpdateTime > 100;

	internal static RenderableMainform MainForm => m_MainForm;

	public static void Initialise()
	{
		Log.WriteLine("----------------------------------------------------");
		Log.WriteLine("START");
		Log.WriteLine("ProgramMain.Initialise()");
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.AddMessageFilter(new MessageFilter());
	}

	public static void MainLoop(RenderableMainform main_form)
	{
		m_MainForm = main_form;
		main_form.Show();
		while (m_MainForm.Created)
		{
			UpdateAndRender();
			Application.DoEvents();
		}
	}

	internal static void UpdateAndRender()
	{
		long num = Time.Now_HiRes();
		long num2 = num - m_LastUpdateTime;
		m_LastUpdateTime = num;
		float value = (float)((double)num2 / (double)Time.GetTicksPerSec());
		value = Misc.Clamp(value, 1f / 60f, 0.1f);
		m_MainForm.Update(num, value);
		m_MainForm.Render();
	}
}
