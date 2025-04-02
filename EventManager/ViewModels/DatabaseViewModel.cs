using System.Diagnostics;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using CommunityToolkit.Maui.Core;
using System.Globalization;
using EventManager.ViewModels.Popups;
using Mopups.Services;
using EventManager.Views.Popups;
using EventManager.Utilities;

namespace EventManager.ViewModels
{
    public partial class DatabaseViewModel : ObservableObject
    {
        private readonly IFileSaver fileSaver;
        private readonly SqlServerService sqlServerService;

        [ObservableProperty]
        private bool isSyncBusy;
        public bool IsSyncNotBusy => !IsSyncBusy;

        [ObservableProperty]
        private bool isExportBusy;
        public bool IsExportNotBusy => !IsExportBusy;

        public DatabaseViewModel(IFileSaver fileSaver, SqlServerService sqlServerService)
        {
            this.fileSaver = fileSaver;
            this.sqlServerService = sqlServerService;
        }

        [RelayCommand]
        public async Task SyncSql()
        {
            if (IsSyncBusy) return;
            IsSyncBusy = true;
            OnPropertyChanged(nameof(IsSyncNotBusy));

            await sqlServerService.SyncEventsFromSqlServer();
            await sqlServerService.SyncEmployeesFromSQLServer();

          
            IsSyncBusy = false;
            OnPropertyChanged(nameof(IsSyncNotBusy));
        }


        [RelayCommand]
        public async Task ExportDatabase()
        {
            try
            {
                if (IsExportBusy) return;
                IsExportBusy = true;
                OnPropertyChanged(nameof(IsExportNotBusy));
                string dbPath = DatabaseService.GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    Debug.WriteLine("[DatabaseViewModel] Database file not found.");
                    await ToastHelper.ShowToast("Database file not found!", ToastDuration.Long);
                    return;
                }

                using var dbStream = File.OpenRead(dbPath);
                var fileSaverResult = await fileSaver.SaveAsync("eventmanager.db", dbStream, CancellationToken.None);

                if (fileSaverResult.IsSuccessful)
                {
                    string filePath = fileSaverResult.FilePath;
                    Debug.WriteLine($"[DatabaseViewModel] File saved at: {filePath}");
                    await ToastHelper.ShowToast($"Database exported: \n {filePath}", ToastDuration.Long);
                }
                else
                {
                    Debug.WriteLine($"[DatabaseViewModel] Error saving file: {fileSaverResult.Exception?.Message}");
                    await ToastHelper.ShowToast($"Export failed: {fileSaverResult.Exception?.Message}", ToastDuration.Long);
                }
                IsExportBusy = false;
                OnPropertyChanged(nameof(IsExportNotBusy));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseViewModel] Exception: {ex.Message}");
                await ToastHelper.ShowToast($"Error: {ex.Message}", ToastDuration.Long);
            }
        }
   
    }
}
