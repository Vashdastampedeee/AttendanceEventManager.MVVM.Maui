using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using EventManager.Services;
using EventManager.ViewModels;
using EventManager.ViewModels.Popups;
using EventManager.Views;
using Mopups.Hosting;	
using EventManager.Views.Popups;
using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
using Mopups.Services;
using EventManager.ViewModels.Modals;
using Syncfusion.Maui.Core.Hosting;

namespace EventManager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureMopups()
			.ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Icon-Buttons.ttf", "IconButtons");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<SqlServerService>();
        builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<BeepService>();
		builder.Services.AddSingleton<IndexViewModel>();
		builder.Services.AddSingleton<LogsViewModel>();
		builder.Services.AddSingleton<EventViewModel>();
		builder.Services.AddSingleton<DatabaseViewModel>();
		builder.Services.AddSingleton<DashboardViewModel>();
		builder.Services.AddSingleton<TotalScannedDataViewModel>();

        builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);

        return builder.Build();
	}
}
