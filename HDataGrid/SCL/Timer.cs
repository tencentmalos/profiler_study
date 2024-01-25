using System.Diagnostics;

namespace SCL;

internal class Timer
{
	private string m_Name;

	private Stopwatch m_Stopwatch = new Stopwatch();

	public Timer(string name)
		: this(name, start: true)
	{
	}

	public Timer(string name, bool start)
	{
		m_Name = name;
		if (start)
		{
			m_Stopwatch.Start();
		}
	}

	public void Start()
	{
		m_Stopwatch.Start();
	}

	public void Stop()
	{
		m_Stopwatch.Stop();
	}

	public void Print()
	{
	}
}
