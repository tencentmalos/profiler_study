namespace SCLCoreCLR;

public class ThreadJobContext
{
	public delegate void JobCancelledHandler();

	private bool m_Cancel;

	private Progress m_Progress = new Progress();

	public bool Cancel => m_Cancel;

	public Progress Progress => m_Progress;

	public event JobCancelledHandler JobCancelled;

	public void CancelJob()
	{
		m_Cancel = true;
		if (this.JobCancelled != null)
		{
			this.JobCancelled();
		}
	}
}
