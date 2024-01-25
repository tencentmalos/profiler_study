using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml;

namespace SCLCoreCLR;

public class XmlWriteStream
{
	private XmlDocument m_Document;

	private XmlElement m_CurrentElement;

	public XmlDocument XmlDocument => m_Document;

	public XmlElement CurrentElement => m_CurrentElement;

	public XmlWriteStream()
	{
		m_Document = new XmlDocument();
	}

	public XmlWriteStream(XmlDocument xml_doc)
		: this(xml_doc, null)
	{
	}

	public XmlWriteStream(XmlDocument xml_doc, XmlElement current_element)
	{
		m_Document = xml_doc;
		m_CurrentElement = current_element;
	}

	public bool Save(string path)
	{
		try
		{
			m_Document.Save(path);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public void StartElement(string name)
	{
		XmlElement xmlElement = m_Document.CreateElement(name);
		if (m_CurrentElement != null)
		{
			m_CurrentElement.AppendChild(xmlElement);
		}
		else
		{
			m_Document.AppendChild(xmlElement);
		}
		m_CurrentElement = xmlElement;
	}

	public void EndElement()
	{
		m_CurrentElement = m_CurrentElement.ParentNode as XmlElement;
	}

	public void Write<T>(string name, T value)
	{
		StartElement(name);
		m_CurrentElement.InnerText = ((value != null) ? value.ToString() : "");
		EndElement();
	}

	public void Write(string name, Color value)
	{
		StartElement(name);
		m_CurrentElement.InnerText = value.A + "," + value.R + "," + value.G + "," + value.B;
		EndElement();
	}

	public void Write(string name, Point value)
	{
		StartElement(name);
		m_CurrentElement.InnerText = value.X + "," + value.Y;
		EndElement();
	}

	public void Write(string name, Size value)
	{
		StartElement(name);
		m_CurrentElement.InnerText = value.Width + "," + value.Height;
		EndElement();
	}

	public void Write<T>(string name, List<T> list) where T : IXmlSerialisable, new()
	{
		StartElement(name);
		foreach (T item in list)
		{
			IXmlSerialisable xmlSerialisable = item;
			StartElement(xmlSerialisable.GetType().Name);
			xmlSerialisable.Write(this);
			EndElement();
		}
		EndElement();
	}

	public void Write<T>(string name, LinkedList<T> list) where T : LinkedListNode, IXmlSerialisable, new()
	{
		StartElement(name);
		foreach (T item in list)
		{
			StartElement(item.GetType().Name);
			item.Write(this);
			EndElement();
		}
		EndElement();
	}

	public void Write<T>(string name, System.Collections.Generic.LinkedList<T> list)
	{
		StartElement(name);
		foreach (T item in list)
		{
			Write(item.GetType().Name, item);
		}
		EndElement();
	}

	public void Write(string name, List<bool> list)
	{
		StartElement(name);
		foreach (bool item in list)
		{
			Write("bool", item);
		}
		EndElement();
	}

	public void Write(string name, List<int> list)
	{
		StartElement(name);
		foreach (int item in list)
		{
			float value = item;
			Write("int", value);
		}
		EndElement();
	}

	public void Write(string name, List<float> list)
	{
		StartElement(name);
		foreach (float item in list)
		{
			Write("float", item);
		}
		EndElement();
	}

	public void Write(string name, List<double> list)
	{
		StartElement(name);
		foreach (double item in list)
		{
			Write("double", item);
		}
		EndElement();
	}

	public void Write(string name, List<string> list)
	{
		StartElement(name);
		foreach (string item in list)
		{
			Write("string", item);
		}
		EndElement();
	}

	public void Save(FileStream file_stream)
	{
		m_Document.Save(file_stream);
	}
}
