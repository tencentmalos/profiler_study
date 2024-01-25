using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Editor;

public class Utils
{
	public static Point PickGoodScreenPositionForForm(Size form_size)
	{
		Point position = Cursor.Position;
		Size size = Screen.PrimaryScreen.Bounds.Size;
		if (position.X + form_size.Width > size.Width)
		{
			position.X -= form_size.Width;
		}
		if (position.Y + form_size.Height > size.Height)
		{
			position.Y -= form_size.Height;
		}
		return position;
	}

	public static bool MakeFileWritableForSave(string path)
	{
		if (!File.Exists(path))
		{
			return true;
		}
		if (new FileInfo(path).IsReadOnly)
		{
			Process.Start("p4", "edit " + path);
			int tickCount = Environment.TickCount;
			while (new FileInfo(path).IsReadOnly && Environment.TickCount - tickCount < 5000)
			{
				Thread.Sleep(1000);
			}
		}
		if (new FileInfo(path).IsReadOnly)
		{
			MessageBox.Show("Unable to write file " + path + ". File is read only.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return false;
		}
		return true;
	}

	public static int FindFilterIndex(string filename, string filters)
	{
		string value = Path.GetExtension(filename).ToLower();
		string[] array = filters.Split('|');
		for (int i = 0; i < array.Length; i += 2)
		{
			if (array[i + 1].Contains(value))
			{
				return i / 2;
			}
		}
		return 0;
	}

	public static string ExpandNewLineChars(string value)
	{
		string text = "";
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			text += ((c == '\n') ? "\\n" : c.ToString());
		}
		return text;
	}

	public static bool MakeFileWritable(string path)
	{
		try
		{
			FileAttributes attributes = File.GetAttributes(path);
			if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
			{
				attributes ^= FileAttributes.ReadOnly;
				File.SetAttributes(path, attributes);
			}
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static bool DeleteFile(string path)
	{
		if (!MakeFileWritable(path))
		{
			return false;
		}
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static bool CopyFile(string src_path, string dst_path)
	{
		try
		{
			if (File.Exists(dst_path))
			{
				DeleteFile(dst_path);
			}
			File.Copy(src_path, dst_path);
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static bool CreateDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			try
			{
				Directory.CreateDirectory(path);
			}
			catch (Exception)
			{
				return false;
			}
		}
		return true;
	}

	public static bool CopyDirectory(string src_path, string dst_path)
	{
		if (!CreateDirectory(dst_path))
		{
			return false;
		}
		try
		{
			bool flag = false;
			string[] files = Directory.GetFiles(src_path);
			foreach (string text in files)
			{
				string dst_path2 = Path.Combine(dst_path, Path.GetFileName(text));
				if (!CopyFile(text, dst_path2))
				{
					flag = true;
				}
			}
			files = Directory.GetDirectories(src_path);
			foreach (string text2 in files)
			{
				if (!CopyDirectory(text2, Path.Combine(dst_path, Path.GetFileName(text2))))
				{
					flag = true;
				}
			}
			return !flag;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
