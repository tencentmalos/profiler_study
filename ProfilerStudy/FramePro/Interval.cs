namespace FramePro;

internal struct Interval
{
	private long m_Unit;

	private double m_Step;

	private string m_UnitName;

	public long Unit => m_Unit;

	public double StepSize => m_Step * (double)m_Unit;

	public double Step => m_Step;

	public string UnitName => m_UnitName;

	public Interval(long unit, double step, string unit_name)
	{
		m_Unit = unit;
		m_Step = step;
		m_UnitName = unit_name;
	}
}
