using System.Windows.Forms;

namespace Docker;

internal class FillerPanel : Control
{
	public FillerPanel()
	{
		SetStyle(ControlStyles.Selectable, value: false);
		SetStyle(ControlStyles.ContainerControl, value: true);
		SetStyle(ControlStyles.StandardClick, value: true);
		SetStyle(ControlStyles.EnableNotifyMessage, value: true);
	}
}
