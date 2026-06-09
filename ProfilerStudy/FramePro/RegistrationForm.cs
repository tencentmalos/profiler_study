using System;
using System.Diagnostics;
using System.Windows.Forms;
////using PureDev.PureDevReg;
////using Registration;
using SCLCoreCLR;

namespace ProfilerStudy;

internal class RegistrationForm
{
	private static bool m_Visible;

	public static bool ShowingForm => m_Visible;

	public static void ShowForm(Settings settings, bool show_in_taskbar)
	{
		if (m_Visible)
		{
			return;
		}
		m_Visible = true;
		////if (settings.RegisterUsingPureDevReg)
		////{
		////	PureDev.PureDevReg.RegistrationForm registrationForm = new PureDev.PureDevReg.RegistrationForm(ProfilerStudyCore.Registrar, settings.EMail, settings.RegKey, show_in_taskbar, Resource1.Icon);
		////	registrationForm.ShowDialog();
		////	if (ProfilerStudyCore.Registrar.IsValidRegistrationNameKeyPair(registrationForm.EMail, registrationForm.RegKey))
		////	{
		////		settings.EMail = registrationForm.EMail;
		////		settings.RegKey = registrationForm.RegKey;
		////		settings.Write();
		////	}
		////}
		////else
		////{
		////	Registration.RegistrationForm registrationForm2 = new Registration.RegistrationForm(settings.EMail, settings.RegKey, show_in_taskbar, Resource1.Icon, RegisterCallback);
		////	registrationForm2.ShowDialog();
		////	if (global::Registration.Registration.LooksLikeValidRegKey(registrationForm2.EMail, registrationForm2.RegKey, quiet: true))
		////	{
		////		settings.EMail = registrationForm2.EMail;
		////		settings.RegKey = registrationForm2.RegKey;
		////		settings.Write();
		////	}
		////}
		m_Visible = false;
		////bool flag;
		////bool registered = true;
		////if (settings.RegisterUsingPureDevReg)
		////{
		////	flag = ProfilerStudyCore.Registrar.TrialExpired;
		////	registered = ProfilerStudyCore.Registrar.Registered;
		////}
		////else
		////{
		////	flag = Demo.Expired;
		////	registered = global::Registration.Registration.Registered;
		////}
		////string text = null;
		////if (flag && !registered)
		////{
		////	if (text != null)
		////	{
		////		MessageBox.Show("ProfilerStudy registered check error:\n" + text);
		////	}
		////	Environment.Exit(0);
		////}
	}

	////private static RegVerifyResult RegisterCallback(string email, string reg_key, ref string error)
	////{
	////	try
	////	{
	////		Process process = new Process();
	////		process.StartInfo.FileName = "ProfilerStudy.exe";
	////		process.StartInfo.Arguments = "set_reg_key " + email + " " + reg_key + " " + ProfilerStudyCore.WebsiteAddr;
	////		process.StartInfo.Verb = "runas";
	////		process.StartInfo.UseShellExecute = true;
	////		Log.WriteLine("Starting process: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments);
	////		process.Start();
	////		process.WaitForExit();
	////		if (process.ExitCode != 0)
	////		{
	////			Log.WriteLine("Process exit code: " + process.ExitCode);
	////			return RegVerifyResult.ProblemWritingToRegistry;
	////		}
	////	}
	////	catch (Exception ex)
	////	{
	////		MessageBox.Show("Failed to set registration key.\n" + ex.Message);
	////		return RegVerifyResult.Failed;
	////	}
	////	return RegVerifyResult.Passed;
	////}
}
