namespace FramePro;

public struct SourceInfoStruct
{
	private bool m_IsValid;

	private string m_Filename;

	private string m_Function;

	private int m_Line;

	private TimeSpanType m_TimeSpanType;

	public bool IsValid => m_IsValid;

	public string Filename => m_Filename;

	public string Function => m_Function;

	public int Line => m_Line;

	public TimeSpanType TimeSpanType => m_TimeSpanType;

	public SourceInfoStruct(SourceInfo source_info)
	{
		if (source_info != null)
		{
			m_IsValid = true;
			m_Filename = source_info.Filename;
			m_Function = source_info.Function;
			m_Line = source_info.Line;
			m_TimeSpanType = source_info.TimeSpanType;
		}
		else
		{
			m_IsValid = false;
			m_Filename = "";
			m_Function = "";
			m_Line = -1;
			m_TimeSpanType = TimeSpanType.Idle;
		}
	}
}
