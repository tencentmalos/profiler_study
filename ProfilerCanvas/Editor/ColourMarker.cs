using System.Drawing;

namespace Editor;

internal class ColourMarker
{
	private const int m_MarkerSize = 10;

	private static int[] m_Marker = new int[100]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
		0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
		0, 1, 0, 0, 0, 0, 0, 0, 1, 0,
		0, 1, 0, 0, 0, 0, 0, 0, 1, 0,
		0, 1, 0, 0, 0, 0, 0, 0, 1, 0,
		0, 1, 0, 0, 0, 0, 0, 0, 1, 0,
		0, 0, 1, 0, 0, 0, 0, 1, 0, 0,
		0, 0, 0, 1, 1, 1, 1, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0
	};

	public static void DrawMarker(Bitmap bitmap, Point pos, Color back_color)
	{
		Point point = new Point(pos.X - 5, pos.Y - 5);
		Color color = Color.White;
		ColourUtils.ColorToHSV(back_color, out var _, out var _, out var value);
		if (value > 0.5)
		{
			color = Color.Black;
		}
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < 10; j++)
			{
				int num = j + point.X;
				int num2 = i + point.Y;
				if (m_Marker[j + i * 10] == 1 && num >= 0 && num < bitmap.Width && num2 >= 0 && num2 < bitmap.Height)
				{
					bitmap.SetPixel(num, num2, color);
				}
			}
		}
	}
}
