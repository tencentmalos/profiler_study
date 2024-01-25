namespace SCLCoreCLR;

public class CallbackLog : ILog
{
	public event LogWriteHandler WriteEvent;

	public event LogDebugWriteHandler DebugWriteEvent;

	public void Write(string text)
	{
		if (this.WriteEvent != null)
		{
			this.WriteEvent(text);
		}
	}

	public void DebugWrite(string text)
	{
		if (this.DebugWriteEvent != null)
		{
			this.DebugWriteEvent(text);
		}
	}
}
