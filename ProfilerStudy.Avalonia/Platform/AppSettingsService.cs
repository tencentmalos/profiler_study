using System;
using System.IO;
using System.Text.Json;

namespace ProfilerStudy.Avalonia;

internal sealed class AppSettingsService : IAppSettingsService
{
	private const string kAppDirectoryName = "ProfilerStudy.Avalonia";
	private const string kSettingsFileName = "settings.json";

	public AppSettings Load()
	{
		try
		{
			string path = GetSettingsPath();
			if (!File.Exists(path))
			{
				return new AppSettings();
			}

			string json = File.ReadAllText(path);
			return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
		}
		catch
		{
			return new AppSettings();
		}
	}

	public void Save(AppSettings settings)
	{
		if (settings == null)
		{
			return;
		}

		string path = GetSettingsPath();
		string directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(path, json);
	}

	private static string GetSettingsPath()
	{
		string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		if (string.IsNullOrWhiteSpace(appDataPath))
		{
			appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		if (string.IsNullOrWhiteSpace(appDataPath))
		{
			appDataPath = AppContext.BaseDirectory;
		}

		return Path.Combine(appDataPath, kAppDirectoryName, kSettingsFileName);
	}
}
