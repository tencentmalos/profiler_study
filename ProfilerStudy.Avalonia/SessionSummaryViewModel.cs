using System;

namespace ProfilerStudy.Avalonia;

internal sealed class SessionSummaryViewModel : ObservableObject
{
	private int m_FrameCount;
	private double m_AverageFrameTimeMs;
	private double m_MaxFrameTimeMs;
	private double m_TargetFrameTimeMs = 33.333;
	private int m_FirstFrameIndex;
	private int m_LastFrameIndex;
	private int m_ThreadCount;
	private string m_SourceName = "No session loaded";

	public int FrameCount
	{
		get => m_FrameCount;
		set => SetProperty(ref m_FrameCount, value);
	}

	public double AverageFrameTimeMs
	{
		get => m_AverageFrameTimeMs;
		set
		{
			if (SetProperty(ref m_AverageFrameTimeMs, value))
			{
				RaisePropertyChanged(nameof(AverageFrameTimeText));
			}
		}
	}

	public double MaxFrameTimeMs
	{
		get => m_MaxFrameTimeMs;
		set
		{
			if (SetProperty(ref m_MaxFrameTimeMs, value))
			{
				RaisePropertyChanged(nameof(MaxFrameTimeText));
			}
		}
	}

	public double TargetFrameTimeMs
	{
		get => m_TargetFrameTimeMs;
		set
		{
			if (SetProperty(ref m_TargetFrameTimeMs, value))
			{
				RaisePropertyChanged(nameof(TargetFrameTimeText));
			}
		}
	}

	public int FirstFrameIndex
	{
		get => m_FirstFrameIndex;
		set
		{
			if (SetProperty(ref m_FirstFrameIndex, value))
			{
				RaisePropertyChanged(nameof(RangeText));
			}
		}
	}

	public int LastFrameIndex
	{
		get => m_LastFrameIndex;
		set
		{
			if (SetProperty(ref m_LastFrameIndex, value))
			{
				RaisePropertyChanged(nameof(RangeText));
			}
		}
	}

	public string SourceName
	{
		get => m_SourceName;
		set => SetProperty(ref m_SourceName, value);
	}

	public int ThreadCount
	{
		get => m_ThreadCount;
		set => SetProperty(ref m_ThreadCount, value);
	}

	public string AverageFrameTimeText => FormatMs(AverageFrameTimeMs);

	public string MaxFrameTimeText => FormatMs(MaxFrameTimeMs);

	public string TargetFrameTimeText => FormatMs(TargetFrameTimeMs);

	public string RangeText => FrameCount == 0 ? string.Empty : $"frames {FirstFrameIndex} - {LastFrameIndex}";

	public void Apply(SessionSummary summary)
	{
		FrameCount = summary.FrameCount;
		AverageFrameTimeMs = summary.AverageFrameTimeMs;
		MaxFrameTimeMs = summary.MaxFrameTimeMs;
		TargetFrameTimeMs = summary.TargetFrameTimeMs;
		FirstFrameIndex = summary.FirstFrameIndex;
		LastFrameIndex = summary.LastFrameIndex;
		ThreadCount = summary.ThreadCount;
		SourceName = summary.SourceName;
	}

	private static string FormatMs(double value)
	{
		return value <= 0.0 ? "-" : Math.Round(value, 3).ToString("0.###") + " ms";
	}
}
