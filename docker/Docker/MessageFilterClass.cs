using System.Windows.Forms;

namespace Docker;

internal class MessageFilterClass : IMessageFilter
{
	private DockManager m_DockManager;

	public MessageFilterClass(DockManager dock_manager)
	{
		m_DockManager = dock_manager;
	}

	public bool PreFilterMessage(ref Message m)
	{
		if (m.Msg == 513)
		{
			m_DockManager.OnLeftMouseButtonDownGlobal();
		}
		return false;
	}
}
