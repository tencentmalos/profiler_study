using System;

namespace Editor;

public interface ICommand : IDisposable
{
	string Description { get; }

	void Do();

	void Undo();
}
