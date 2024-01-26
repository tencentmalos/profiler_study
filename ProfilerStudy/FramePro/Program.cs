using System;
using System.IO;
using System.Windows.Forms;
using SCL;
////using PureDev.PureDevRegCLR;
////using Registration;
using SCLCoreCLR;
////using SymLibCLR;
////using SCL;

namespace FramePro;

internal static class Program
{
	private static string m_PreMainFormLog = "";
	private static SCL.Cell mTestCell = new Cell();

	[STAThread]
	private static void Main()
	{
		//"en-US"
		// Application.CurrentCulture = new System.Globalization.CultureInfo("zh-CN", false);
		
		System.Windows.Forms.Clipboard.SetText("Can not paste from host mac!");

		Settings settings = new Settings();
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		////if (Registrar.IsCommandLineLaunch(commandLineArgs))
		////{
		////	Environment.Exit(Registrar.RunCommandLine(commandLineArgs));
		////}
		////else if (commandLineArgs.Length > 1 && commandLineArgs[1] == "set_reg_key")
		////{
		////	if (!Directory.Exists(CoreSettings.UserLocalFolder))
		////	{
		////		Directory.CreateDirectory(CoreSettings.UserLocalFolder);
		////	}
		////	Log.OpenFile(CoreSettings.UserLocalFolder + "FrameProReg.log", FrameProCore.MaxLogSize);
		////	Log.WriteLine("\n-------------------------------------------------------");
		////	Log.WriteLine("FramePro: FramePro setting registry key");
		////	Log.WriteLine("FramePro: Registration.Initialise");
		////	Demo.DemoLength = FrameProCore.DemoLength;
		////	global::Registration.Registration.Initialise(FrameProCore.Manufacturer, FrameProCore.ProductName, FrameProCore.WebsiteAddr, FrameProCore.MachineIdDatabase, FrameProCore.RegistrationDatabase, FrameProCore.PurchaseWebsite, FrameProCore.RegTypes, StoreMode.Registry);
		////	Log.WriteLine("FramePro: SetRegistryRegKey");
		////	SetRegistryRegKey(commandLineArgs);
		////	return;
		////}
		////FrameProCore.RegistrarInitialiseComplete += OnRegistrarInitialiseComplete;
		bool num = !File.Exists(CoreSettings.Path);
		bool register_using_guid = settings.RegisterUsingGUID;
		if (num)
		{
			register_using_guid = true;
		}
		CallbackLog callbackLog = new CallbackLog();
		callbackLog.WriteEvent += FrameProCoreLog;
		callbackLog.DebugWriteEvent += FrameProCoreDebugLog;
		FrameProCore.Initialise(settings, register_using_guid, callbackLog);
		string text = ((commandLineArgs.Length > 1) ? commandLineArgs[1] : null);
		if (text != null)
		{
			Log.WriteLine("file: " + text);
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);


		var mainForm = new MainForm(settings, text);
		// bool isMainFormClosed = false;
		// mainForm.FormClosed += (object sender, FormClosedEventArgs e) => {
		// 	isMainFormClosed = true;
		// };
		// mainForm.Show();
		// while (!isMainFormClosed)
		// {
		// 	Application.DoEvents();
		// }

        Application.Run(mainForm);
	}


    private static void FrameProCoreLog(string text)
	{
		if (MainForm.Inst != null)
		{
			lock (m_PreMainFormLog)
			{
				if (m_PreMainFormLog.Length != 0)
				{
					MainForm.Inst.Log(m_PreMainFormLog);
					m_PreMainFormLog = "";
				}
			}
			MainForm.Inst.Log(text);
			return;
		}
		lock (m_PreMainFormLog)
		{
			m_PreMainFormLog += text;
		}
	}

	private static void FrameProCoreDebugLog(string text)
	{
	}

	////private static void OnRegistrarInitialiseComplete(bool result, string result_text)
	////{
	////	if (!FrameProCore.Registrar.Installed)
	////	{
	////		MessageBox.Show(string.Concat(string.Concat("" + "ERROR: There is a problem with your installation.\n", "\n"), "Please re-install FramePro\n"), "FramePro ERROR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	////		Environment.Exit(1);
	////	}
	////	if (!result)
	////	{
	////		MessageBox.Show("ERROR: failed to intiialise registration library: " + result_text, "FramePro ERROR", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	////		Environment.Exit(2);
	////	}
	////}

	////private static void SetRegistryRegKey(string[] args)
	////{
	////	Log.WriteLine("SetRegistryRegKey");
	////	try
	////	{
	////		string text = args[2];
	////		string text2 = args[3];
	////		string error = null;
	////		Log.WriteLine("FramePro: email: " + text);
	////		Log.WriteLine("FramePro: reg_key: " + text2);
	////		if (!global::Registration.Registration.SetRegistryRegKey(text, text2, ref error) || error != null)
	////		{
	////			Log.WriteLine("FramePro: error: " + ((error != null) ? error : "null"));
	////			Environment.Exit(1);
	////		}
	////	}
	////	catch (Exception ex)
	////	{
	////		Log.WriteLine(ex.Message);
	////		Environment.Exit(1);
	////	}
	////}

	////private static void PureDevRegWrite(string text)
	////{
	////	Log.Write(text);
	////}

	////private static void PureDevRegdebugWrite(string text)
	////{
	////}
}
