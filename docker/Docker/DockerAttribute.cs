using System;

namespace Docker;

public class DockerAttribute : Attribute
{
	private Attributes m_Attributes;

	public bool ShowTitleBar => (m_Attributes & Attributes.HideTitleBar) == 0;

	public bool CanResize => (m_Attributes & Attributes.FixedResize) == 0;

	public DockerAttribute(Attributes attributes)
	{
		m_Attributes = attributes;
	}
}
