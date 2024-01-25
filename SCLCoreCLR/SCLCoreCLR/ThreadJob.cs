using System.Threading;

namespace SCLCoreCLR;

public class ThreadJob
{
	private Thread m_Thread;

	private ThreadJobMain m_ThreadJobMain;

	private object m_Arg;

	private object m_Result;

	private volatile bool m_Finished;

	private ThreadJobContext m_Context = new ThreadJobContext();

	public bool Finished => m_Finished;

	public object Result => m_Result;

	public int PercentComplete => m_Context.Progress.PercentComplete;

	public ThreadJob(ThreadJobMain thread_job_main)
		: this(thread_job_main, null)
	{
	}

	public ThreadJob(ThreadJobMain thread_job_main, object arg)
	{
		m_ThreadJobMain = thread_job_main;
		m_Arg = arg;
	}

	public void Run()
	{
		m_Thread = new Thread(ThreadMain);
		m_Thread.Start();
	}

	public void Abort()
	{
		if (m_Thread.IsAlive)
		{
			m_Thread.Abort();
		}
	}

	private void ThreadMain()
	{
		m_Context.Progress.Start();
		m_Result = m_ThreadJobMain(m_Arg, m_Context);
		m_Context.Progress.Finish();
		m_Finished = true;
	}

	public void Cancel()
	{
		m_Context.CancelJob();
	}
}
