using System.Windows.Forms;

namespace Editor;

internal class MessageFilter : IMessageFilter
{
	public bool PreFilterMessage(ref Message m)
	{
		if (ProgramMain.IReallyDoNeedAnUpdate)
		{
			ProgramMain.UpdateAndRender();
		}
		return ProgramMain.MainForm.HandleGlobalMessage(ref m);
	}
}
