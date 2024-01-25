using System;
using System.Collections.Generic;
using System.Drawing;
using FramePro.Properties;

namespace FramePro;

internal class HatchBrushManager
{
	private struct HatchBrushKey : IEquatable<HatchBrushKey>
	{
		private Color m_BackColour;

		private Color m_ForeColour;

		private Point m_Offset;

		private int m_HashCode;

		public HatchBrushKey(Color back_colour, Color fore_colour, Point offset)
		{
			m_BackColour = back_colour;
			m_ForeColour = fore_colour;
			m_Offset = offset;
			m_HashCode = m_BackColour.GetHashCode() ^ m_ForeColour.GetHashCode() ^ m_Offset.GetHashCode();
		}

		public override int GetHashCode()
		{
			return m_HashCode;
		}

		public bool Equals(HatchBrushKey other)
		{
			if (m_BackColour == other.m_BackColour && m_ForeColour == other.m_ForeColour)
			{
				return m_Offset == other.m_Offset;
			}
			return false;
		}
	}

	private Dictionary<HatchBrushKey, Brush> m_Brushes = new Dictionary<HatchBrushKey, Brush>();

	private Size m_ImageSize;

	public HatchBrushManager()
	{
		m_ImageSize = Resources.diagonal_line.Size;
	}

	public Brush GetBrush(Color back_colour, Color fore_colour, Point offset)
	{
		offset = new Point(offset.X % m_ImageSize.Width, offset.Y % m_ImageSize.Height);
		HatchBrushKey key = new HatchBrushKey(back_colour, fore_colour, offset);
		if (!m_Brushes.TryGetValue(key, out var value))
		{
			Bitmap bitmap = new Bitmap(Resources.diagonal_line);
			Color pixel = bitmap.GetPixel(bitmap.Width - 1, 0);
			Color pixel2 = bitmap.GetPixel(0, 0);
			for (int i = 0; i < bitmap.Height; i++)
			{
				for (int j = 0; j < bitmap.Width; j++)
				{
					Color color = bitmap.GetPixel(j, i);
					if (color == pixel)
					{
						color = fore_colour;
					}
					else if (color == pixel2)
					{
						color = back_colour;
					}
					bitmap.SetPixel(j, i, color);
				}
			}
			TextureBrush textureBrush = new TextureBrush(bitmap);
			textureBrush.TranslateTransform(offset.X, offset.Y);
			value = textureBrush;
			m_Brushes[key] = value;
		}
		return value;
	}
}
