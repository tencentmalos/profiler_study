namespace FramePro;

internal class SessionViewTab
{
	private SessionViewTabButton m_Button = new SessionViewTabButton();

	private SessionView m_SessionView;

	public SessionViewTabButton Button => m_Button;

	public SessionView SessionView => m_SessionView;

	public SessionViewTab(SessionView session_view)
	{
		m_Button.ButtonText = session_view.ViewName;
		m_SessionView = session_view;
	}
}
