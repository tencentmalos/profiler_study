using System;
using System.Drawing;
using System.IO;

namespace SCLCoreCLR;

public class Misc
{
	public static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static long Clamp(long value, long min, long max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static float Clamp(float value, float min, float max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static double Clamp(double value, double min, double max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static void Swap<T>(ref T a, ref T b)
	{
		T val = a;
		a = b;
		b = val;
	}

	public static bool HexStringToULong(string str, ref ulong value)
	{
		str = str.ToLower();
		ulong num = 0uL;
		string text = str;
		foreach (char c in text)
		{
			num <<= 4;
			ulong num2 = 0uL;
			if (c >= '0' && c <= '9')
			{
				num2 = (ulong)(c - 48);
			}
			else
			{
				if (c < 'a' || c > 'f')
				{
					return false;
				}
				num2 = 10uL + (ulong)(c - 97);
			}
			num |= num2;
		}
		value = num;
		return true;
	}

	public static Point Sub(Point p1, Point p2)
	{
		return new Point(p1.X - p2.X, p1.Y - p2.Y);
	}

	public static int Convert(string value)
	{
		try
		{
			return System.Convert.ToInt32(value);
		}
		catch (Exception ex)
		{
			Log.WriteLine(ex.Message);
			return 0;
		}
	}

	public static string ReplaceExt(string path, string new_ext)
	{
		string directoryName = Path.GetDirectoryName(path);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		return Path.Combine(directoryName, fileNameWithoutExtension) + new_ext;
	}

	private static bool IsLowercase(string str)
	{
		foreach (char c in str)
		{
			if (c >= 'A' && c <= 'Z')
			{
				return false;
			}
		}
		return true;
	}

	private static bool PathStartsWith(string path, string root_path)
	{
		if (path.StartsWith(root_path))
		{
			int length = root_path.Length;
			if (path.Length != length)
			{
				return path[length] == Path.DirectorySeparatorChar;
			}
			return true;
		}
		return false;
	}

	public static string GetRelativePath(string root_path, string filename)
	{
		try
		{
			string text = root_path.ToLower().TrimEnd(Path.DirectorySeparatorChar);
			string path = Path.GetDirectoryName(filename).ToLower();
			string text2 = "";
			while (text != null)
			{
				if (PathStartsWith(path, text))
				{
					int num = text.Length;
					if (text[text.Length - 1] != Path.DirectorySeparatorChar)
					{
						num++;
					}
					return text2 + filename.Substring(num);
				}
				text2 += "..\\";
				text = Path.GetDirectoryName(text);
			}
			return filename;
		}
		catch (Exception ex)
		{
			Log.WriteLine("GetRelativePath error: " + ex.Message + "\nroot_path: " + root_path + " filename: " + filename);
			return filename;
		}
	}

	public static Color ColorFromHSV(double h, double S, double V)
	{
		double num;
		for (num = h; num < 0.0; num += 360.0)
		{
		}
		while (num >= 360.0)
		{
			num -= 360.0;
		}
		double num4;
		double num3;
		double num2;
		if (V <= 0.0)
		{
			num4 = (num3 = (num2 = 0.0));
		}
		else if (S <= 0.0)
		{
			num4 = (num3 = (num2 = V));
		}
		else
		{
			double num5 = num / 60.0;
			int num6 = (int)Math.Floor(num5);
			double num7 = num5 - (double)num6;
			double num8 = V * (1.0 - S);
			double num9 = V * (1.0 - S * num7);
			double num10 = V * (1.0 - S * (1.0 - num7));
			switch (num6)
			{
			case 0:
				num4 = V;
				num3 = num10;
				num2 = num8;
				break;
			case 1:
				num4 = num9;
				num3 = V;
				num2 = num8;
				break;
			case 2:
				num4 = num8;
				num3 = V;
				num2 = num10;
				break;
			case 3:
				num4 = num8;
				num3 = num9;
				num2 = V;
				break;
			case 4:
				num4 = num10;
				num3 = num8;
				num2 = V;
				break;
			case 5:
				num4 = V;
				num3 = num8;
				num2 = num9;
				break;
			case 6:
				num4 = V;
				num3 = num10;
				num2 = num8;
				break;
			case -1:
				num4 = V;
				num3 = num8;
				num2 = num9;
				break;
			default:
				num4 = (num3 = (num2 = V));
				break;
			}
		}
		int red = Clamp((int)(num4 * 255.0), 0, 255);
		int green = Clamp((int)(num3 * 255.0), 0, 255);
		int blue = Clamp((int)(num2 * 255.0), 0, 255);
		return Color.FromArgb(red, green, blue);
	}

	public static bool IsPow2(uint value)
	{
		return (value & (value - 1)) == 0;
	}
}
