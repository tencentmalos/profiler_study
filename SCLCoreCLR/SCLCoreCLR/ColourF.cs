using System.Drawing;

namespace SCLCoreCLR;

public struct ColourF
{
	public float R;

	public float G;

	public float B;

	public float A;

	public static ColourF Black = new ColourF(0f, 0f, 0f);

	public static ColourF White = new ColourF(1f, 1f, 1f);

	public static ColourF Red = new ColourF(1f, 0f, 0f);

	public static ColourF Green = new ColourF(0f, 1f, 0f);

	public static ColourF Blue = new ColourF(0f, 0f, 1f);

	public static ColourF Yellow = new ColourF(1f, 1f, 0f);

	public static ColourF Orange = new ColourF(1f, 0.5f, 0f);

	public ColourF(float r, float g, float b)
	{
		R = r;
		G = g;
		B = b;
		A = 1f;
	}

	public ColourF(float r, float g, float b, float a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public ColourF(ColourF colour, float a)
	{
		R = colour.R;
		G = colour.G;
		B = colour.B;
		A = a;
	}

	public ColourF(Color colour)
	{
		R = (float)(int)colour.R / 255f;
		G = (float)(int)colour.G / 255f;
		B = (float)(int)colour.B / 255f;
		A = (float)(int)colour.A / 255f;
	}

	public ColourF(Color colour, float a)
	{
		R = (float)(int)colour.R / 255f;
		G = (float)(int)colour.G / 255f;
		B = (float)(int)colour.B / 255f;
		A = a;
	}

	public Color ToColor()
	{
		return Color.FromArgb((int)(A * 255f), (int)(R * 255f), (int)(G * 255f), (int)(B * 255f));
	}
}
