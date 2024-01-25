using System.Drawing;
using System.Windows.Forms;

namespace FramePro;

internal class SessionView : UserControl
{
	private bool m_Active;

	private bool m_TrackEnd;

	public virtual Session Session => null;

	public virtual string ViewName => null;

	public virtual int HighlightedTimeSpanCount => 0;

	public bool Active
	{
		get
		{
			return m_Active;
		}
		set
		{
			if (value != m_Active)
			{
				m_Active = value;
				OnActiveChanged();
			}
		}
	}

	public bool TrackEnd
	{
		get
		{
			return m_TrackEnd;
		}
		set
		{
			if (m_TrackEnd != value)
			{
				m_TrackEnd = value;
				OnTrackEndChanged();
			}
		}
	}

	public event StopTrackingEndHandler StopTrackingEndEvent;

	public virtual void OnTargetFrameTimeChanged()
	{
	}

	public virtual void OnThreadScopeHeightChanged()
	{
	}

	public virtual void OnMouseWheel(int delta, Point location)
	{
	}

	public virtual void OnActiveChanged()
	{
	}

	public virtual void UpdateView()
	{
	}

	public virtual void OnTrackEndChanged()
	{
	}

	public virtual void GotoPrev(string filter)
	{
	}

	public virtual void GotoNext(string filter)
	{
	}

	public virtual void OnScopeColourChanged()
	{
	}

	public virtual void OnCustomStatInfoChanged()
	{
	}

	public virtual void OnCustomStatColourChanged()
	{
	}

	public void StopTrackingEnd()
	{
		if (this.StopTrackingEndEvent != null)
		{
			this.StopTrackingEndEvent();
		}
	}
}
