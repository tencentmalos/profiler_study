namespace ProfilerStudy.Avalonia;

internal interface IAppSettingsService
{
	AppSettings Load();

	void Save(AppSettings settings);
}
