using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using SCLCoreCLR;

namespace Editor;

public class ClassSerialiser
{
	private static void Read(object obj, XmlReadStream stream)
	{
		FieldInfo[] fields = obj.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!stream.StartElement(fieldInfo.Name))
			{
				continue;
			}
			if (fieldInfo.FieldType == typeof(List<string>))
			{
				List<string> list = (List<string>)fieldInfo.GetValue(obj);
				list.Clear();
				for (int j = 0; j < stream.Count; j++)
				{
					stream.StartElement(j);
					list.Add(stream.CurrentValue);
					stream.EndElement();
				}
			}
			else if (fieldInfo.FieldType == typeof(Size))
			{
				Size size = (Size)fieldInfo.GetValue(obj);
				int value = size.Width;
				stream.Read("Width", ref value);
				int value2 = size.Height;
				stream.Read("Height", ref value2);
				fieldInfo.SetValue(obj, new Size(value, value2));
			}
			stream.EndElement();
		}
	}

	private static void Write(object obj, XmlWriteStream stream)
	{
		FieldInfo[] fields = obj.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			stream.StartElement(fieldInfo.Name);
			if (fieldInfo.FieldType == typeof(List<string>))
			{
				foreach (string item in (List<string>)fieldInfo.GetValue(obj))
				{
					stream.Write("Item", item);
				}
			}
			else if (fieldInfo.FieldType == typeof(Size))
			{
				Size size = (Size)fieldInfo.GetValue(obj);
				stream.Write("Width", size.Width);
				stream.Write("Height", size.Height);
			}
			stream.EndElement();
		}
	}

	public static void Write(object obj, string filename)
	{
		string directoryName = Path.GetDirectoryName(filename);
		if (!Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		XmlWriteStream xmlWriteStream = new XmlWriteStream();
		xmlWriteStream.StartElement("Settings");
		Write(obj, xmlWriteStream);
		xmlWriteStream.EndElement();
		xmlWriteStream.Save(filename);
	}

	public static bool Read(object obj, string filename)
	{
		if (!File.Exists(filename))
		{
			return false;
		}
		XmlReadStream xmlReadStream = new XmlReadStream();
		xmlReadStream.Load(filename);
		if (xmlReadStream.StartElement("Settings"))
		{
			Read(obj, xmlReadStream);
			xmlReadStream.EndElement();
		}
		return true;
	}
}
