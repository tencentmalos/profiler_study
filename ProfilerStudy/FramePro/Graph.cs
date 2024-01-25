using System.Drawing;

namespace FramePro;

internal class Graph
{
	public string m_StatName;

	public long m_NameId;

	public Color m_Colour;

	public string m_GraphName;

	public string m_Unit;

	public XAxisMode m_XAxisMode;

	public Graph(string stat_name, long name_id, Color colour, string graph_name, string unit, XAxisMode x_axis_mode)
	{
		m_StatName = stat_name;
		m_NameId = name_id;
		m_Colour = colour;
		m_GraphName = graph_name;
		m_Unit = unit;
		m_XAxisMode = x_axis_mode;
	}

	public bool Equals(Graph other)
	{
		if (m_StatName == other.m_StatName && m_NameId == other.m_NameId && m_Colour == other.m_Colour && m_GraphName == other.m_GraphName && m_Unit == other.m_Unit)
		{
			return m_XAxisMode == other.m_XAxisMode;
		}
		return false;
	}
}
