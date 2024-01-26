using System;
using System.IO;
using SCLCoreCLR;
////using SymLibCLR;

namespace FramePro;

public class FrameProCore
{
	private static string m_Version = "1.10.17.0";

	private const int m_DemoLength = 30;

	private const int m_MaxVerifyFailDays = 3;

	private const int m_MaxLogSize = 1048576;

	private static string m_Manufacturer = "???";

	private static string m_ProductName = "ProfilerStudy";

	private static string m_WebsiteAddr = "???";

	private const string m_MachineIdDatabase = "framepro_machine_id";

	private const string m_RegistrationDatabase = "framepro_registration";

	private const string m_PurchaseWebsite = "???";

	private const string m_UpdateWebsite = "???";

	private const string m_RegisterWebsite = "???";

	private const string m_UnregisterWebsite = "???";

	private const string m_PureDevRegPhpScriptsWebsite = "???";

	private const string m_LocalServerPort = "8429";

	////private static string[] m_RegTypes = new string[1] { "Professional Floating License" };

	////private static Registrar m_Registrar;

	////private static CallbackLog m_RegistrarLog = new CallbackLog();

	private static ILog m_Log;

	public static string Version => m_Version;

	public static int DemoLength => 30;

	public static int MaxVerifyFailDays => 3;

	public static int MaxLogSize => 1048576;

	public static string Manufacturer => m_Manufacturer;

	public static string ProductName => m_ProductName;

	public static string WebsiteAddr => m_WebsiteAddr;

	public static string MachineIdDatabase => "framepro_machine_id";

	public static string RegistrationDatabase => "framepro_registration";

	public static string PurchaseWebsite => "https://www.puredevsoftware.com";

	////public static string[] RegTypes => m_RegTypes;

	////public static string RegisterWebsite => "https://www.puredevsoftware.com/Register_v2.htm";

	////public static Registrar Registrar => m_Registrar;

	////public static event RegistrarInitialiseCompleteHandler RegistrarInitialiseComplete;

	public static void Initialise(ISettings settings, bool register_using_guid, ILog log)
	{
		m_Log = log;
		if (!Directory.Exists(CoreSettings.UserLocalFolder))
		{
			log.Write("Creating directory " + CoreSettings.UserLocalFolder + "\n");
			Directory.CreateDirectory(CoreSettings.UserLocalFolder);
		}
		Log.OpenFile(Path.Combine(CoreSettings.UserLocalFolder, "FramePro.log"), 1048576);
		Log.WriteLine("-----------------------------------------------");
		Log.WriteLine("Starting FramePro");
		Log.WriteLine("Version: " + m_Version);
		settings.WriteToLog();
		////Demo.DemoLength = 30;
		////global::Registration.Registration.Initialise(m_Manufacturer, m_ProductName, m_WebsiteAddr, "framepro_machine_id", "framepro_registration", "https://www.puredevsoftware.com", m_RegTypes, StoreMode.Registry);
		////m_RegistrarLog.WriteEvent += RegistrarWriteLog;
		////m_RegistrarLog.DebugWriteEvent += RegistrarDebugWriteLog;
		////m_Registrar = new Registrar(ProductName, m_Version, "https://www.puredevsoftware.com", "https://www.puredevsoftware.com/Register_v2.htm", "https://www.puredevsoftware.com/Unregister.htm", "https://www.puredevsoftware.com/puredevreg/v2/", "https://www.puredevsoftware.com/framepro/update.php", 30, 3, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RegistrationMode.AllUsers, RegTypes, RegistryMode.WindowsRegistry, ElevationMode.RequiresElevation, register_using_guid, "8429", m_RegistrarLog);
		////m_Registrar.InitialiseComplete += OnRegistrarInitialiseComplete;
		////SymLib.LogCallback = SymLibWrite;
		////SymLib.DebugLogCallback = SymLibDebugWrite;
	}

	private static void SymLibWrite(string text)
	{
		m_Log.Write(text);
	}

	private static void SymLibDebugWrite(string text)
	{
	}

	////public static bool WaitForInitialiseToComplete(ref string result_text)
	////{
	////	return m_Registrar.WaitForInitialiseToComplete(ref result_text);
	////}

	////public static bool WaitForInstallVerificationComplete(ref string result_text)
	////{
	////	return m_Registrar.WaitForInstallVerification(ref result_text) != VerifyInstallResult.Failed;
	////}

	////public static bool WaitForRegistrationVerificationComplete(ref string result_text)
	////{
	////	return m_Registrar.WaitForRegistrationVerificationComplete(ref result_text);
	////}

	////private static void OnRegistrarInitialiseComplete(bool result, string result_Text)
	////{
	////	if (FrameProCore.RegistrarInitialiseComplete != null)
	////	{
	////		FrameProCore.RegistrarInitialiseComplete(result, result_Text);
	////	}
	////}

	private static void RegistrarDebugWriteLog(string text)
	{
	}

	private static void RegistrarWriteLog(string text)
	{
		Log.Write(text);
	}
}
