using System;
using Avalonia;
using Avalonia.Media;

namespace ProfilerStudy.Avalonia;

internal sealed class TimelineDrawingTheme
{
	public IBrush BackgroundBrush { get; }
	public IBrush LabelBackgroundBrush { get; }
	public IBrush EmptyTextBrush { get; }
	public IBrush TextBrush { get; }
	public IBrush MutedTextBrush { get; }
	public IBrush EmptyLaneBrush { get; }
	public IBrush SwitchBrush { get; }
	public IBrush TrackBrush { get; }
	public IBrush WindowBrush { get; }
	public IBrush WindowHighlightBrush { get; }
	public Pen BorderPen { get; }
	public Pen LanePen { get; }
	public Pen AxisPen { get; }
	public Pen HoverPen { get; }
	public Pen SelectionPen { get; }
	public Pen SwitchPen { get; }
	public Pen WindowPen { get; }

	private readonly Color[] m_Palette;

	private TimelineDrawingTheme()
	{
		BackgroundBrush = Brush("TimelineScopePanelBackgroundBrush", "#FFF7F9FC");
		LabelBackgroundBrush = Brush("TimelineScopeSubtleBackgroundBrush", "#FFF0F4F8");
		EmptyTextBrush = Brush("TimelineScopeSubtleTextBrush", "#FF5D6977");
		TextBrush = Brush("TimelineScopeTextBrush", "#FF18212B");
		MutedTextBrush = Brush("TimelineScopeSubtleTextBrush", "#FF5D6977");
		EmptyLaneBrush = Brush("TimelineScopeCardBackgroundBrush", "#FFFFFFFF");
		SwitchBrush = Brush("TimelineScopeInfoBorderBrush", "#FF5A95F0");
		TrackBrush = Brush("TimelineScopeSubtleBackgroundBrush", "#FFF0F4F8");
		WindowBrush = Brush("TimelineScopeInfoSurfaceBrush", "#FFEAF2FF");
		WindowHighlightBrush = Brush("TimelineScopeInfoBorderBrush", "#FF5A95F0");
		BorderPen = Pen("TimelineScopePanelBorderBrush", "#FFD0D9E4");
		LanePen = Pen("TimelineScopeSeparatorBrush", "#FFC5CED9");
		AxisPen = Pen("TimelineScopeSubtleTextBrush", "#FF5D6977");
		HoverPen = new Pen(Brush("SukiPrimaryColor", "#FF2169D6"), 2.0);
		SelectionPen = new Pen(Brush("SukiPrimaryColor", "#FF2169D6"), 1.0);
		SwitchPen = new Pen(SwitchBrush, 1.0);
		WindowPen = Pen("TimelineScopeInfoBorderBrush", "#FF5A95F0");
		m_Palette = new[]
		{
			ThemeColor("TimelineScopeInfoBrush", "#FF2169D6"),
			ThemeColor("TimelineScopeAddedBrush", "#FF128A52"),
			ThemeColor("TimelineScopeWarningBrush", "#FFCC7A00"),
			ThemeColor("TimelineScopeRemovedBrush", "#FFC93B4C"),
			ThemeColor("TimelineScopeAccentBrush", "#FF6A3FE0"),
			ThemeColor("TimelineScopeInfoBorderBrush", "#FF5A95F0"),
		};
	}

	public static TimelineDrawingTheme Current() => new();

	public Color ScopeColor(int index)
	{
		return m_Palette[Math.Abs(index) % m_Palette.Length];
	}

	private static Pen Pen(string key, string fallbackHex)
	{
		return new Pen(Brush(key, fallbackHex), 1.0);
	}

	private static IBrush Brush(string key, string fallbackHex)
	{
		if (Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var value) == true &&
			value is IBrush brush)
		{
			return brush;
		}

		return new SolidColorBrush(global::Avalonia.Media.Color.Parse(fallbackHex));
	}

	private static Color ThemeColor(string key, string fallbackHex)
	{
		IBrush brush = Brush(key, fallbackHex);
		return brush is ISolidColorBrush solidBrush ? solidBrush.Color : global::Avalonia.Media.Color.Parse(fallbackHex);
	}
}
