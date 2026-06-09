using System;
using System.Windows.Input;

namespace ProfilerStudy.Avalonia;

internal sealed class RelayCommand : ICommand
{
	private readonly Func<object, bool> m_CanExecute;
	private readonly Action<object> m_Execute;

	public event EventHandler CanExecuteChanged;

	public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
	{
		m_Execute = execute ?? throw new ArgumentNullException(nameof(execute));
		m_CanExecute = canExecute;
	}

	public bool CanExecute(object parameter)
	{
		return m_CanExecute == null || m_CanExecute(parameter);
	}

	public void Execute(object parameter)
	{
		m_Execute(parameter);
	}

	public void RaiseCanExecuteChanged()
	{
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}
