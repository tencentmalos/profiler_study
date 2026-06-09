using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Styling;
using SukiUI;
using SukiUI.Models;

namespace ProfilerStudy.Avalonia;

internal sealed class AppThemeService
{
	private const string kLightThemeName = "Light";
	private const string kDarkThemeName = "Dark";
	private const string kDefaultColorThemeName = "Blue";

	private readonly AppSettings m_Settings;
	private readonly IThemeHost m_ThemeHost;
	private readonly Action<AppSettings> m_SaveSettings;
	private string m_ActiveBaseThemeName;
	private string m_ActiveColorThemeName;

	public event EventHandler Changed;

	public AppThemeService(AppSettings settings, IThemeHost themeHost, Action<AppSettings> saveSettings)
	{
		m_Settings = settings ?? new AppSettings();
		m_ThemeHost = themeHost ?? new NullThemeHost();
		m_SaveSettings = saveSettings ?? (_ => { });
		ColorThemes = m_ThemeHost.ColorThemes.ToArray();
		ApplySavedTheme();
	}

	public IReadOnlyList<AppThemeColorOption> ColorThemes { get; }

	public string ActiveBaseThemeName => m_ActiveBaseThemeName;

	public string ActiveColorThemeName => m_ActiveColorThemeName;

	public bool IsLightThemeActive => string.Equals(m_ActiveBaseThemeName, kLightThemeName, StringComparison.Ordinal);

	public bool IsDarkThemeActive => string.Equals(m_ActiveBaseThemeName, kDarkThemeName, StringComparison.Ordinal);

	public static AppThemeService Create(AppSettings settings, Action<AppSettings> saveSettings)
	{
		return new AppThemeService(settings, new SukiThemeHost(), saveSettings);
	}

	public void ChangeBaseTheme(string baseThemeName)
	{
		string normalizedTheme = NormalizeBaseTheme(baseThemeName);
		m_ThemeHost.ChangeBaseTheme(normalizedTheme);
		m_Settings.BaseTheme = normalizedTheme;
		m_ActiveBaseThemeName = normalizedTheme;
		SaveAndNotify();
	}

	public void ChangeColorTheme(string colorThemeName)
	{
		string normalizedTheme = ResolveColorThemeName(colorThemeName);
		m_ThemeHost.ChangeColorTheme(normalizedTheme);
		m_Settings.ColorTheme = normalizedTheme;
		m_ActiveColorThemeName = normalizedTheme;
		SaveAndNotify();
	}

	private void ApplySavedTheme()
	{
		string baseThemeName = NormalizeBaseTheme(m_Settings.BaseTheme);
		string colorThemeName = ResolveColorThemeName(m_Settings.ColorTheme);
		m_ThemeHost.ChangeBaseTheme(baseThemeName);
		m_ThemeHost.ChangeColorTheme(colorThemeName);
		m_Settings.BaseTheme = baseThemeName;
		m_Settings.ColorTheme = colorThemeName;
		m_ActiveBaseThemeName = baseThemeName;
		m_ActiveColorThemeName = colorThemeName;
	}

	private string ResolveColorThemeName(string colorThemeName)
	{
		if (!string.IsNullOrWhiteSpace(colorThemeName))
		{
			foreach (var theme in ColorThemes)
			{
				if (string.Equals(theme.Name, colorThemeName, StringComparison.OrdinalIgnoreCase))
				{
					return theme.Name;
				}
			}
		}

		foreach (var theme in ColorThemes)
		{
			if (string.Equals(theme.Name, kDefaultColorThemeName, StringComparison.OrdinalIgnoreCase))
			{
				return theme.Name;
			}
		}

		return ColorThemes.Count > 0 ? ColorThemes[0].Name : kDefaultColorThemeName;
	}

	private void SaveAndNotify()
	{
		m_SaveSettings(m_Settings);
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private static string NormalizeBaseTheme(string baseThemeName)
	{
		return string.Equals(baseThemeName, kDarkThemeName, StringComparison.OrdinalIgnoreCase)
			? kDarkThemeName
			: kLightThemeName;
	}

	internal interface IThemeHost
	{
		IReadOnlyList<AppThemeColorOption> ColorThemes { get; }

		void ChangeBaseTheme(string baseThemeName);

		void ChangeColorTheme(string colorThemeName);
	}

	private sealed class SukiThemeHost : IThemeHost
	{
		private readonly SukiTheme m_SukiTheme;
		private readonly IReadOnlyList<SukiColorTheme> m_SukiColorThemes;

		public SukiThemeHost()
		{
			m_SukiTheme = SukiTheme.GetInstance();
			m_SukiColorThemes = m_SukiTheme.ColorThemes.ToArray();
			ColorThemes = m_SukiColorThemes
				.Select(theme => new AppThemeColorOption(theme.DisplayName, theme.PrimaryBrush))
				.ToArray();
		}

		public IReadOnlyList<AppThemeColorOption> ColorThemes { get; }

		public void ChangeBaseTheme(string baseThemeName)
		{
			ThemeVariant themeVariant = string.Equals(baseThemeName, kDarkThemeName, StringComparison.Ordinal)
				? ThemeVariant.Dark
				: ThemeVariant.Light;
			m_SukiTheme.ChangeBaseTheme(themeVariant);
			if (Application.Current != null)
			{
				Application.Current.RequestedThemeVariant = themeVariant;
			}
		}

		public void ChangeColorTheme(string colorThemeName)
		{
			SukiColorTheme theme = m_SukiColorThemes.FirstOrDefault(item => string.Equals(item.DisplayName, colorThemeName, StringComparison.OrdinalIgnoreCase));
			if (theme != null)
			{
				m_SukiTheme.ChangeColorTheme(theme);
			}
		}
	}

	private sealed class NullThemeHost : IThemeHost
	{
		public IReadOnlyList<AppThemeColorOption> ColorThemes { get; } = new[]
		{
			new AppThemeColorOption(kDefaultColorThemeName)
		};

		public void ChangeBaseTheme(string baseThemeName)
		{
		}

		public void ChangeColorTheme(string colorThemeName)
		{
		}
	}

	internal sealed class TestThemeHost : IThemeHost
	{
		public IReadOnlyList<AppThemeColorOption> ColorThemes { get; } = new[]
		{
			new AppThemeColorOption("Blue"),
			new AppThemeColorOption("Green"),
			new AppThemeColorOption("Orange")
		};

		public string RequestedBaseTheme { get; private set; }

		public string RequestedColorTheme { get; private set; }

		public void ChangeBaseTheme(string baseThemeName)
		{
			RequestedBaseTheme = baseThemeName;
		}

		public void ChangeColorTheme(string colorThemeName)
		{
			RequestedColorTheme = colorThemeName;
		}
	}
}
