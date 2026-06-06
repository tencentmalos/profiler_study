namespace ProfilerStudy.Avalonia;

internal interface ISourceViewerLauncher
{
	bool TryLaunch(string sourceFile, int sourceLine, out string error);
}
