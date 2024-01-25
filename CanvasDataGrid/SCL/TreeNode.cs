using System;
using System.Drawing;
using System.Windows.Forms;

namespace SCL;

public class TreeNode : Control, ICloneable, ITextObject
{
	public delegate void RenamingHandler(TreeNode tree_node, ref string new_name);

	private Image m_Image;

	private object m_UserData;

	public Image Image
	{
		get
		{
			return m_Image;
		}
		set
		{
			m_Image = value;
			Refresh();
		}
	}

	public override string Text
	{
		set
		{
			base.Text = value;
			Refresh();
		}
	}

	public object UserData
	{
		get
		{
			return m_UserData;
		}
		set
		{
			m_UserData = value;
		}
	}

	string ITextObject.Text
	{
		get
		{
			return Text;
		}
		set
		{
			if (Text != value)
			{
				if (this.Renaming != null)
				{
					this.Renaming(this, ref value);
				}
				Text = value;
			}
		}
	}

	public event RenamingHandler Renaming;

	public TreeNode(string text)
	{
		DoubleBuffered = true;
		Text = text;
	}

	public TreeNode(Image image, string text)
	{
		DoubleBuffered = true;
		m_Image = image;
		Text = text;
	}

	public object Clone()
	{
		return new TreeNode(m_Image, Text);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (m_Image != null)
		{
			int num = 0;
			int num2 = (base.ClientSize.Height - m_Image.Height) / 2;
			e.Graphics.DrawImageUnscaled(m_Image, num, num2);
		}
		int num3 = ((m_Image != null) ? (m_Image.Width + 4) : 0);
		int num4 = (int)(((float)base.Height - Font.GetHeight()) / 2f);
		e.Graphics.DrawString(Text, Font, SystemBrushes.ControlText, num3, num4);
		base.OnPaint(e);
	}

	public override string ToString()
	{
		return Text;
	}
}
