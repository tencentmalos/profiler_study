using System.Windows.Forms;

namespace Editor;

public class RenderableMainform : Form
{
	public virtual void Update(long frame_start_time, float el)
	{
	}

	public virtual void Render()
	{
	}

	public virtual bool HandleGlobalMessage(ref Message msg)
	{
		return false;
	}
}
