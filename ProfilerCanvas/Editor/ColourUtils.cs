using System;
using System.Drawing;

namespace Editor;

public class ColourUtils
{
	public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
	{
		int num = Math.Max(color.R, Math.Max(color.G, color.B));
		int num2 = Math.Min(color.R, Math.Min(color.G, color.B));
		hue = color.GetHue();
		saturation = ((num == 0) ? 0.0 : (1.0 - 1.0 * (double)num2 / (double)num));
		value = (double)num / 255.0;
	}

	public static Color ColorFromHSV(double hue, double saturation, double value)
	{
		int num = Convert.ToInt32(Math.Floor(hue / 60.0)) % 6;
		double num2 = hue / 60.0 - Math.Floor(hue / 60.0);
		value *= 255.0;
		int num3 = Convert.ToInt32(value);
		int num4 = Convert.ToInt32(value * (1.0 - saturation));
		int num5 = Convert.ToInt32(value * (1.0 - num2 * saturation));
		int num6 = Convert.ToInt32(value * (1.0 - (1.0 - num2) * saturation));
		return num switch
		{
			0 => Color.FromArgb(255, num3, num6, num4), 
			1 => Color.FromArgb(255, num5, num3, num4), 
			2 => Color.FromArgb(255, num4, num3, num6), 
			3 => Color.FromArgb(255, num4, num5, num3), 
			4 => Color.FromArgb(255, num6, num4, num3), 
			_ => Color.FromArgb(255, num3, num4, num5), 
		};
	}
}
