using System;

namespace Docker;

internal class MDITab : IDisposable
{
	private MDIForm m_Form;

	private int m_X;

	private int m_Width;

	public MDIForm Form => m_Form;

	public int X
	{
		get
		{
			return m_X;
		}
		set
		{
			m_X = value;
		}
	}

	public int Width
	{
		get
		{
			return m_Width;
		}
		set
		{
			m_Width = value;
		}
	}

	public string Name => m_Form.Text;

	public event MDITabNameChangedHandler MDITabNameChanged;

	public MDITab(MDIForm form)
	{
		m_Form = form;
		form.TextChanged += FormTextChanged;
	}

	public void Dispose()
	{
		m_Form.TextChanged += FormTextChanged;
	}

	private void FormTextChanged(object sender, EventArgs e)
	{
		if (this.MDITabNameChanged != null)
		{
			this.MDITabNameChanged();
		}
	}

	public override string ToString()
	{
		return "MDITab containing " + m_Form;
	}
}
