namespace SCL;

internal class ResizeTitleInfo
{
	public enum Mode
	{
		None,
		Column,
		Row
	}

	public Mode m_Mode;

	public bool m_Active;

	public int m_StartValue;

	public int m_StartMousePos;
}
