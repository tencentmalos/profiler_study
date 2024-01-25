using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SCLCoreCLR;

namespace Docker;

internal class Utils
{
	public static void BringToFrontNoActivate(Control control)
	{
		NativeMethods.SetWindowPos(control.Handle, IntPtr.Zero, 0, 0, 0, 0, 19u);
	}

	public static Region BitmapToRegion(Bitmap bitmap, Color transparency_colour)
	{
		int height = bitmap.Height;
		int width = bitmap.Width;
		GraphicsPath graphicsPath = new GraphicsPath();
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				if (!(bitmap.GetPixel(j, i) == transparency_colour))
				{
					int num = j;
					for (; j < width && bitmap.GetPixel(j, i) != transparency_colour; j++)
					{
					}
					graphicsPath.AddRectangle(new Rectangle(num, i, j - num, 1));
				}
			}
		}
		Region result = new Region(graphicsPath);
		graphicsPath.Dispose();
		return result;
	}

	public static T FindAttribute<T>(object obj) where T : Attribute
	{
		object[] customAttributes = obj.GetType().GetCustomAttributes(inherit: true);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			Attribute attribute = (Attribute)customAttributes[i];
			if (attribute is T)
			{
				return (T)attribute;
			}
		}
		return null;
	}

	public static Point ReadPoint(XmlReadStream stream, string name, Point default_pt)
	{
		Point result = default_pt;
		if (stream.StartElement(name))
		{
			int value = default_pt.X;
			int value2 = default_pt.Y;
			stream.Read("X", ref value);
			stream.Read("Y", ref value2);
			stream.EndElement();
			result = new Point(value, value2);
		}
		return result;
	}

	public static Size ReadSize(XmlReadStream stream, string name, Size default_size)
	{
		Size result = default_size;
		if (stream.StartElement(name))
		{
			int value = default_size.Width;
			int value2 = default_size.Height;
			stream.Read("Width", ref value);
			stream.Read("Height", ref value2);
			stream.EndElement();
			result = new Size(value, value2);
		}
		return result;
	}

	public static Rectangle ReadRect(XmlReadStream stream, string name, Rectangle default_rect)
	{
		Rectangle result = default_rect;
		if (stream.StartElement(name))
		{
			int value = default_rect.X;
			int value2 = default_rect.Y;
			int value3 = default_rect.Width;
			int value4 = default_rect.Height;
			stream.Read("X", ref value);
			stream.Read("Y", ref value2);
			stream.Read("Width", ref value3);
			stream.Read("Height", ref value4);
			stream.EndElement();
			result = new Rectangle(value, value2, value3, value4);
		}
		return result;
	}

	public static void Write(XmlWriteStream stream, string name, Point pt)
	{
		stream.StartElement(name);
		stream.Write("X", pt.X);
		stream.Write("Y", pt.Y);
		stream.EndElement();
	}

	public static void Write(XmlWriteStream stream, string name, Size size)
	{
		stream.StartElement(name);
		stream.Write("Width", size.Width);
		stream.Write("Height", size.Height);
		stream.EndElement();
	}

	public static void Write(XmlWriteStream stream, string name, Rectangle rect)
	{
		stream.StartElement(name);
		stream.Write("X", rect.X);
		stream.Write("Y", rect.Y);
		stream.Write("Width", rect.Width);
		stream.Write("Height", rect.Height);
		stream.EndElement();
	}

	public static void DrawCloseButton(Rectangle rect, Graphics graphics, bool highlight)
	{
		if (highlight)
		{
			graphics.FillRectangle(SystemBrushes.ControlLight, rect);
			graphics.DrawRectangle(SystemPens.ControlDarkDark, rect);
		}
		Pen pen = (highlight ? SystemPens.ControlDarkDark : SystemPens.ControlDark);
		Rectangle rectangle = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 7, rect.Width - 6);
		graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		graphics.DrawLine(pen, rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Top);
		rectangle = new Rectangle(rectangle.X + 1, rectangle.Y, rectangle.Width, rectangle.Height);
		graphics.DrawLine(pen, rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
		graphics.DrawLine(pen, rectangle.Left, rectangle.Bottom, rectangle.Right, rectangle.Top);
	}

	public static MouseEventArgs ConvertMouseEventArgs(object sender, Control target_control, MouseEventArgs e)
	{
		Point p = ((Control)sender).PointToScreen(e.Location);
		p = target_control.PointToClient(p);
		return new MouseEventArgs(e.Button, e.Clicks, p.X, p.Y, e.Delta);
	}
}
