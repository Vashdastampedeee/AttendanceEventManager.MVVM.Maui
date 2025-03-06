using CommunityToolkit.Maui;
using EventManager.Services;
using EventManager.ViewModels;
using EventManager.Views;
using Microsoft.Extensions.Logging;

namespace EventManager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Icon-Buttons.ttf", "IconButtons");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<BeepService>();
		builder.Services.AddSingleton<IndexViewModel>();
		builder.Services.AddSingleton<LogsViewModel>();

		return builder.Build();
	}
}
