using System;
using System.Windows.Forms;
////using Registration;

namespace FramePro;

internal class Update
{
	private static bool m_Terminated;

	public static void Terminate()
	{
		m_Terminated = true;
	}

	private static int[] SplitVersion(string version)
	{
		string[] array = version.Split('.');
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = Convert.ToInt32(array[i]);
		}
		return array2;
	}

	////public static void CheckForUpdate(Settings settings)
	////{
	////	string error = null;
	////	string data = "";
	////	bool failed_to_conect = false;
	////	string text = Php.Post("https://www.puredevsoftware.com/framepro/update.php", data, ref failed_to_conect, ref error, ref m_Terminated);
	////	if (m_Terminated || error != null || text == null)
	////	{
	////		return;
	////	}
	////	string text2 = text.Trim();
	////	int[] array = SplitVersion(text2);
	////	int[] array2 = SplitVersion(FrameProCore.Version);
	////	bool flag = false;
	////	for (int i = 0; i < 3; i++)
	////	{
	////		if (array[i] < array2[i])
	////		{
	////			flag = false;
	////			break;
	////		}
	////		if (array[i] > array2[i])
	////		{
	////			flag = true;
	////			break;
	////		}
	////	}
	////	settings.NewVersionAvailable = flag;
	////	if (flag && text2 != settings.LastVersionChecked)
	////	{
	////		settings.NotifyNewVersion = true;
	////		settings.LastVersionChecked = text2;
	////	}
	////	settings.Write();
	////}

	////public static void OnCheckForUpdateComplete(bool succeeded, bool update_available, string new_version, Settings settings)
	////{
	////	if (succeeded)
	////	{
	////		bool flag = false;
	////		if (!update_available && (settings.NotifyNewVersion || settings.LastVersionChecked != ""))
	////		{
	////			settings.NotifyNewVersion = false;
	////			settings.LastVersionChecked = "";
	////			flag = true;
	////		}
	////		if (settings.NewVersionAvailable != update_available)
	////		{
	////			settings.NewVersionAvailable = update_available;
	////			flag = true;
	////		}
	////		if (update_available && new_version != settings.LastVersionChecked && (!settings.NotifyNewVersion || settings.LastVersionChecked != new_version))
	////		{
	////			settings.NotifyNewVersion = true;
	////			settings.LastVersionChecked = new_version;
	////			flag = true;
	////		}
	////		if (flag)
	////		{
	////			settings.Write();
	////		}
	////	}
	////}

	////public static bool NotifyNewVersion(Settings settings)
	////{
	////	AutoUpdate autoUpdate = new AutoUpdate();
	////	DialogResult dialogResult = autoUpdate.ShowDialog();
	////	if (autoUpdate.DontAskAgain)
	////	{
	////		settings.CheckForUpdates = false;
	////		settings.Write();
	////	}
	////	if (dialogResult == DialogResult.Yes)
	////	{
	////		return true;
	////	}
	////	settings.NotifyNewVersion = false;
	////	settings.Write();
	////	return false;
	////}
}
