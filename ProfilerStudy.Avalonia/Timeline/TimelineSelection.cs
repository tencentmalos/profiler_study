namespace ProfilerStudy.Avalonia;

public sealed class TimelineSelection : ObservableObject
{
	private int m_SelectedFrameIndex = -1;
	private int m_HoveredFrameIndex = -1;
	private double m_SelectedFrameTimeMs;
	private double m_HoveredFrameTimeMs;

	public int SelectedFrameIndex
	{
		get => m_SelectedFrameIndex;
		set => SetProperty(ref m_SelectedFrameIndex, value);
	}

	public int HoveredFrameIndex
	{
		get => m_HoveredFrameIndex;
		set => SetProperty(ref m_HoveredFrameIndex, value);
	}

	public double SelectedFrameTimeMs
	{
		get => m_SelectedFrameTimeMs;
		set => SetProperty(ref m_SelectedFrameTimeMs, value);
	}

	public double HoveredFrameTimeMs
	{
		get => m_HoveredFrameTimeMs;
		set => SetProperty(ref m_HoveredFrameTimeMs, value);
	}

	public void SelectFrame(int frameIndex, double frameTimeMs)
	{
		SelectedFrameTimeMs = frameIndex < 0 ? 0.0 : frameTimeMs;
		SelectedFrameIndex = frameIndex;
	}

	public void HoverFrame(int frameIndex, double frameTimeMs)
	{
		HoveredFrameTimeMs = frameIndex < 0 ? 0.0 : frameTimeMs;
		HoveredFrameIndex = frameIndex;
	}

	public void ClearSelectedFrame()
	{
		SelectFrame(-1, 0.0);
	}

	public void ClearHoveredFrame()
	{
		HoverFrame(-1, 0.0);
	}

	public void Clear()
	{
		ClearSelectedFrame();
		ClearHoveredFrame();
	}
}
