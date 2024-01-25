using System.ComponentModel;
using System.Windows.Forms;

namespace FramePro;

public class ScrollPanel : Panel
{
	private IContainer components;

	public ScrollPanel()
	{
		InitializeComponent();
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if ((Control.ModifierKeys & Keys.Shift) != 0)
		{
			base.OnMouseWheel(e);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
	}
}
