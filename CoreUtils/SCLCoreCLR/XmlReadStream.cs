using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace SCLCoreCLR;

public class XmlReadStream
{
	private XmlDocument m_Document;

	private XmlElement m_CurrentElement;

	public int Count => m_CurrentElement.ChildNodes.Count;

	public XmlDocument XmlDocument => m_Document;

	public XmlElement CurrentElement => m_CurrentElement;

	public string CurrentValue => CurrentElement.InnerText;

	public XmlReadStream()
	{
		m_Document = new XmlDocument();
	}

	public XmlReadStream(XmlDocument xml_document)
		: this(xml_document, null)
	{
	}

	public XmlReadStream(XmlDocument xml_document, XmlElement current_element)
	{
		m_Document = xml_document;
		m_CurrentElement = current_element;
	}

	public void Load(FileStream file_stream)
	{
		m_Document.Load(file_stream);
	}

	public bool Load(string path)
	{
		if (!File.Exists(path))
		{
			return false;
		}
		FileStream fileStream = null;
		try
		{
			fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			Load(fileStream);
			fileStream.Close();
			return true;
		}
		catch (Exception ex)
		{
			fileStream?.Close();
			Log.WriteLine(ex.Message);
			return false;
		}
	}

	public bool StartElement(string name)
	{
		if (m_CurrentElement != null)
		{
			foreach (XmlNode childNode in m_CurrentElement.ChildNodes)
			{
				if (childNode is XmlElement xmlElement && xmlElement.Name == name)
				{
					m_CurrentElement = xmlElement;
					return true;
				}
			}
		}
		else if (m_Document.ChildNodes.Count > 0 && m_Document.FirstChild.Name == name)
		{
			m_CurrentElement = (XmlElement)m_Document.FirstChild;
			return true;
		}
		return false;
	}

	public bool StartElement(int index)
	{
		if (index < 0 || index >= Count)
		{
			return false;
		}
		m_CurrentElement = (XmlElement)m_CurrentElement.ChildNodes[index];
		return true;
	}

	public void EndElement()
	{
		m_CurrentElement = m_CurrentElement.ParentNode as XmlElement;
	}

	public bool Read(string name, ref bool value)
	{
		if (StartElement(name))
		{
			value = Convert.ToBoolean(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref int value)
	{
		if (StartElement(name))
		{
			value = Convert.ToInt32(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref uint value)
	{
		if (StartElement(name))
		{
			value = Convert.ToUInt32(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref long value)
	{
		if (StartElement(name))
		{
			value = Convert.ToInt64(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref ulong value)
	{
		if (StartElement(name))
		{
			value = Convert.ToUInt64(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref float value)
	{
		if (StartElement(name))
		{
			value = Convert.ToSingle(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref double value)
	{
		if (StartElement(name))
		{
			value = Convert.ToDouble(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref string value)
	{
		if (StartElement(name))
		{
			value = m_CurrentElement.InnerText;
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref DateTime value)
	{
		if (StartElement(name))
		{
			value = DateTime.Parse(m_CurrentElement.InnerText);
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read<T>(string name, ref T value) where T : struct, IConvertible
	{
		if (!typeof(T).IsEnum)
		{
			throw new ArgumentException("T must be an enumerated type");
		}
		if (StartElement(name))
		{
			bool result = false;
			if (!string.IsNullOrEmpty(m_CurrentElement.InnerText.Trim()))
			{
				try
				{
					value = (T)Enum.Parse(typeof(T), m_CurrentElement.InnerText.Trim());
					result = true;
				}
				catch (Exception)
				{
				}
			}
			EndElement();
			return result;
		}
		return false;
	}

	public bool Read(string name, ref Color value)
	{
		if (StartElement(name))
		{
			try
			{
				string[] array = m_CurrentElement.InnerText.Split(',');
				value = Color.FromArgb(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]), Convert.ToInt32(array[3]));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.Message);
				value = Color.Black;
			}
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref Point value)
	{
		if (StartElement(name))
		{
			try
			{
				string[] array = m_CurrentElement.InnerText.Split(',');
				value = new Point(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.Message);
				value = new Point(0, 0);
			}
			EndElement();
			return true;
		}
		return false;
	}

	public bool Read(string name, ref Size value)
	{
		if (StartElement(name))
		{
			try
			{
				string[] array = m_CurrentElement.InnerText.Split(',');
				value = new Size(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.Message);
				value = new Size(0, 0);
			}
			EndElement();
			return true;
		}
		return false;
	}

	public bool ReadEnum<T>(string name, ref T value)
	{
		if (StartElement(name))
		{
			try
			{
				value = (T)Enum.Parse(typeof(T), m_CurrentElement.InnerText);
			}
			catch (Exception)
			{
				return false;
			}
			EndElement();
			return true;
		}
		return false;
	}

	public void Read<T>(string name, ref List<T> list) where T : IXmlSerialisable, new()
	{
		if (StartElement(name))
		{
			list = new List<T>(Count);
			for (int i = 0; i < Count; i++)
			{
				StartElement(i);
				T item = (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
				item.Read(this);
				list.Add(item);
				EndElement();
			}
			EndElement();
		}
	}

	public void Read<T>(string name, ref LinkedList<T> list) where T : LinkedListNode, IXmlSerialisable, new()
	{
		if (StartElement(name))
		{
			list = new LinkedList<T>();
			for (int i = 0; i < Count; i++)
			{
				StartElement(i);
				T val = (T)typeof(T).GetConstructor(new Type[0]).Invoke(new object[0]);
				val.Read(this);
				list.AddLast(val);
				EndElement();
			}
			EndElement();
		}
	}

	public void Read(string name, ref System.Collections.Generic.LinkedList<string> list)
	{
		if (StartElement(name))
		{
			list = new System.Collections.Generic.LinkedList<string>();
			for (int i = 0; i < Count; i++)
			{
				string value = "";
				Read(i, ref value);
				list.AddLast(value);
			}
			EndElement();
		}
	}

	public void Read(string name, ref List<bool> list)
	{
		if (StartElement(name))
		{
			list = new List<bool>(Count);
			for (int i = 0; i < Count; i++)
			{
				bool value = false;
				Read(i, ref value);
				list.Add(value);
			}
			EndElement();
		}
	}

	public void Read(string name, ref List<int> list)
	{
		if (StartElement(name))
		{
			list = new List<int>(Count);
			for (int i = 0; i < Count; i++)
			{
				int value = 0;
				Read(i, ref value);
				list.Add(value);
			}
			EndElement();
		}
	}

	public void Read(string name, ref List<float> list)
	{
		if (StartElement(name))
		{
			list = new List<float>(Count);
			for (int i = 0; i < Count; i++)
			{
				float value = 0f;
				Read(i, ref value);
				list.Add(value);
			}
			EndElement();
		}
	}

	public void Read(string name, ref List<double> list)
	{
		if (StartElement(name))
		{
			list = new List<double>(Count);
			for (int i = 0; i < Count; i++)
			{
				double value = 0.0;
				Read(i, ref value);
				list.Add(value);
			}
			EndElement();
		}
	}

	public bool Read(string name, ref List<string> list)
	{
		if (StartElement(name))
		{
			list = new List<string>(Count);
			for (int i = 0; i < Count; i++)
			{
				string value = "";
				Read(i, ref value);
				list.Add(value);
			}
			EndElement();
			return true;
		}
		return false;
	}

	public void Read(int index, ref bool value)
	{
		StartElement(index);
		value = Convert.ToBoolean(m_CurrentElement.InnerText);
		EndElement();
	}

	public void Read(int index, ref int value)
	{
		StartElement(index);
		value = Convert.ToInt32(m_CurrentElement.InnerText);
		EndElement();
	}

	public void Read(int index, ref long value)
	{
		StartElement(index);
		value = Convert.ToInt64(m_CurrentElement.InnerText);
		EndElement();
	}

	public void Read(int index, ref float value)
	{
		StartElement(index);
		value = Convert.ToSingle(m_CurrentElement.InnerText);
		EndElement();
	}

	public void Read(int index, ref double value)
	{
		StartElement(index);
		value = Convert.ToDouble(m_CurrentElement.InnerText);
		EndElement();
	}

	public void Read(int index, ref string value)
	{
		StartElement(index);
		value = m_CurrentElement.InnerText;
		EndElement();
	}

	public void Read(int index, ref Color value)
	{
		StartElement(index);
		try
		{
			string[] array = m_CurrentElement.InnerText.Split(',');
			value = Color.FromArgb(Convert.ToInt32(array[0]), Convert.ToInt32(array[1]), Convert.ToInt32(array[2]), Convert.ToInt32(array[3]));
		}
		catch (Exception ex)
		{
			Log.WriteLine(ex.Message);
			value = Color.Black;
		}
		EndElement();
	}

	public void ReadEnum<T>(int index, ref T value)
	{
		StartElement(index);
		value = (T)Enum.Parse(typeof(T), m_CurrentElement.InnerText);
		EndElement();
	}
}
