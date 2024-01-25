namespace SCL;

internal struct DragRowInfo
{
	public bool m_Active;

	public Row m_Row;

	public int m_StartHeight;

	public int m_MouseStartY;

	public const int m_DragResizeExtent = 4;
}
