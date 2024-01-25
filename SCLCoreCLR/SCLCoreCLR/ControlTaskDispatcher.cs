using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SCLCoreCLR;

public class ControlTaskDispatcher
{
	public delegate void TaskFunction();

	public delegate void TaskFunction1Arg(object arg);

	public delegate void TaskFunction2Arg(object arg1, object arg2);

	private enum WM
	{
		WM_USER = 0x400
	}

	private enum CustomMessages
	{
		WM_TASK_MANAGER_WAKE_UP = 1126
	}

	private const int m_Timeout = 10;

	private Queue<Action> m_Actions = new Queue<Action>();

	private IntPtr m_hWnd;

	private bool m_ProcessingTask;

	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

	public void QueueTask(Action action)
	{
		lock (m_Actions)
		{
			m_Actions.Enqueue(action);
			Wakeup();
		}
	}

	private void Wakeup()
	{
		if (m_hWnd != IntPtr.Zero)
		{
			PostMessage(m_hWnd, 1126u, 0, 0);
		}
	}

	public void ProcessMessage(IntPtr hWnd, ref Message m)
	{
		_ = m_hWnd == IntPtr.Zero;
		m_hWnd = hWnd;
		if (!m_ProcessingTask)
		{
			m_ProcessingTask = true;
			DoActions();
			m_ProcessingTask = false;
		}
	}

	private Action DequeueAction()
	{
		lock (m_Actions)
		{
			return (m_Actions.Count != 0) ? m_Actions.Dequeue() : null;
		}
	}

	private void DoActions()
	{
		Action action = DequeueAction();
		int tickCount = Environment.TickCount;
		while (action != null)
		{
			action();
			if (Environment.TickCount - tickCount > 10)
			{
				Wakeup();
				break;
			}
			action = DequeueAction();
		}
	}
}
