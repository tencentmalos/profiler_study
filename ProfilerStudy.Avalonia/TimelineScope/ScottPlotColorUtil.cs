using Avalonia;
using Avalonia.Styling;
using ScottPlot;
using System;

namespace ProfilerStudy.Avalonia.Timeline;


public class ScottPlotColorUtil
{
    private static readonly Color[] DarkIndustrialPalette =
    {
        Color.FromHex("#5FA8FF"),
        Color.FromHex("#FFB347"),
        Color.FromHex("#63D79B"),
        Color.FromHex("#D98CFF"),
        Color.FromHex("#7FD1C8"),
        Color.FromHex("#F7768E"),
        Color.FromHex("#C6D05B"),
        Color.FromHex("#6EC1FF"),
        Color.FromHex("#F4A261"),
        Color.FromHex("#B8C0FF"),
        Color.FromHex("#4DD0E1"),
        Color.FromHex("#FFD166"),
    };

    private static readonly Color[] LightIndustrialPalette =
    {
        Color.FromHex("#1F77D0"),
        Color.FromHex("#D9822B"),
        Color.FromHex("#238B5A"),
        Color.FromHex("#8A5FD3"),
        Color.FromHex("#0F8B8D"),
        Color.FromHex("#D1495B"),
        Color.FromHex("#7A8F2A"),
        Color.FromHex("#2F6DB3"),
        Color.FromHex("#C76B29"),
        Color.FromHex("#6B73D6"),
        Color.FromHex("#0E7490"),
        Color.FromHex("#B7791F"),
    };

    public static Color kDustyRose => new(221, 193, 191);
    public static Color kHazyBlue => new(162, 181, 193);
    public static Color SageGreen => new(188, 196, 182);
    public static Color CreamWhite => new(240, 232, 215);
    public static Color LightKhaki => new(220, 211, 195);
    public static Color ElegantGray => new(185, 185, 185);
    public static Color DustyLavender => new(204, 195, 204);
    public static Color FleshTone => new(223, 206, 195);
    public static Color WitheredRose => new(186, 149, 148);
    public static Color MutedTurmeric = new(228, 188, 137);

    // Use the golden ratio conjugate, an ideal choice for generating evenly distributed points on a circle.
    private const double GoldenRatioConjugate = 0.618033988749895;

    // You can use a random start hue, or a fixed one to ensure the same sequence of colors every time.
    private const double StartHue = 0.5;


    /// <summary>
    /// Generates a high-contrast, vibrant color based on an index.
    /// </summary>
    /// <param name="index">The index of the color, starting from 0.</param>
    /// <param name="saturation">The saturation of the color (from 0.0 to 1.0). ~0.8 is recommended for vibrant colors.</param>
    /// <param name="value">The value (brightness) of the color (from 0.0 to 1.0). ~0.95 is recommended for bright colors.</param>
    /// <returns>A System.Drawing.Color object.</returns>
    public static Color GetColor(int index, double saturation = 0.8, double value = 0.95)
    {
        var palette = GetPaletteForCurrentTheme();
        if (index >= 0 && index < palette.Length)
        {
            return palette[index];
        }

        // Calculate the hue using the golden ratio.
        // Each increment of the golden ratio conjugate yields a new point that is evenly spaced on the color wheel.
        double hue = (StartHue + Math.Max(index, 0) * GoldenRatioConjugate) % 1.0;

        return FromHsv(hue, GetThemeAdjustedSaturation(saturation), GetThemeAdjustedValue(value));
    }

    public static Color GetMutedColor(int index, byte alpha = 255)
    {
        var baseColor = GetColor(index, 0.58, IsDarkTheme() ? 0.92 : 0.80);
        return new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
    }

    private static Color[] GetPaletteForCurrentTheme()
    {
        return IsDarkTheme() ? DarkIndustrialPalette : LightIndustrialPalette;
    }

    private static bool IsDarkTheme()
    {
        var themeVariant = Application.Current?.ActualThemeVariant;
        return themeVariant == ThemeVariant.Dark;
    }

    private static double GetThemeAdjustedSaturation(double saturation)
    {
        return IsDarkTheme() ? Math.Min(0.72, saturation) : Math.Min(0.68, saturation);
    }

    private static double GetThemeAdjustedValue(double value)
    {
        return IsDarkTheme() ? Math.Min(0.95, value) : Math.Min(0.82, value);
    }

    /// <summary>
    /// Converts an HSV color model to an RGB (System.Drawing.Color) model.
    /// </summary>
    /// <param name="hue">Hue (from 0.0 to 1.0).</param>
    /// <param name="saturation">Saturation (from 0.0 to 1.0).</param>
    /// <param name="value">Value (brightness) (from 0.0 to 1.0).</param>
    /// <returns>The converted Color object.</returns>
    public static Color FromHsv(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue * 6)) % 6;
        double f = hue * 6 - Math.Floor(hue * 6);

        value = value * 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        if (hi == 0)
            return new(v, t, p);
        else if (hi == 1)
            return new(q, v, p);
        else if (hi == 2)
            return new(p, v, t);
        else if (hi == 3)
            return new(p, q, v);
        else if (hi == 4)
            return new(t, p, v);
        else
            return new(v, p, q);
    }
}
