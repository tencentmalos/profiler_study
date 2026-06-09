using SCLCoreCLR;

namespace ProfilerStudy;

public class CustomStatInfo
{
	private const CustomStatXAxisMode m_DefaultXAxisMode = CustomStatXAxisMode.Frame;

	public CustomStatXAxisMode m_XAxisMode;

	public bool Read(XmlReadStream stream)
	{
		bool result = true;
		if (stream.StartElement("CustomStatInfo"))
		{
			if (!stream.Read("XAxisMode", ref m_XAxisMode))
			{
				result = false;
			}
			stream.EndElement();
		}
		return result;
	}

	public void Write(XmlWriteStream stream)
	{
		stream.StartElement("CustomStatInfo");
		stream.Write("XAxisMode", m_XAxisMode);
		stream.EndElement();
	}
}
