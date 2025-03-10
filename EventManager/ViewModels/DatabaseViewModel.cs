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

namespace EventManager.ViewModels
{
    public partial class DatabaseViewModel : ObservableObject
    {
        private readonly IFileSaver fileSaver;
        private readonly DatabaseService databaseService;
        private readonly SqlServerService sqlSyncService;

        [ObservableProperty]
        private bool isSyncBusy;
        public bool IsSyncNotBusy => !IsSyncBusy;

        [ObservableProperty]
        private bool isExportBusy;
        public bool IsExportNotBusy => !IsExportBusy;

        public DatabaseViewModel(IFileSaver fileSaver, DatabaseService databaseService, SqlServerService sqlServerService)
        {
            this.fileSaver = fileSaver;
            this.databaseService = databaseService;
            this.sqlSyncService = sqlServerService;
        }
        private static async Task ShowToast(string message, ToastDuration duration)
        {
            await Toast.Make(message, duration, 14).Show();
        }

        [RelayCommand]
        public async Task SyncEmployees()
        {
            if (IsSyncBusy) return;
            IsSyncBusy = true;
            OnPropertyChanged(nameof(IsSyncNotBusy));

            await Task.Delay(1000);
            await sqlSyncService.SyncEmployeesFromSQLServer();
          
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
                await Task.Delay(1000);
                string dbPath = databaseService.GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    Debug.WriteLine("[DatabaseViewModel] Database file not found.");
                    await ShowToast("Database file not found!", ToastDuration.Long);
                    return;
                }

                using var dbStream = File.OpenRead(dbPath);
                var fileSaverResult = await fileSaver.SaveAsync("eventmanager.db", dbStream, CancellationToken.None);

                if (fileSaverResult.IsSuccessful)
                {
                    string filePath = fileSaverResult.FilePath;
                    Debug.WriteLine($"[DatabaseViewModel] File saved at: {filePath}");
                    await ShowToast($"Database exported: \n {filePath}", ToastDuration.Long);
                }
                else
                {
                    Debug.WriteLine($"[DatabaseViewModel] Error saving file: {fileSaverResult.Exception?.Message}");
                    await ShowToast($"Export failed: {fileSaverResult.Exception?.Message}", ToastDuration.Long);
                }
                IsExportBusy = false;
                OnPropertyChanged(nameof(IsExportNotBusy));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseViewModel] Exception: {ex.Message}");
                await ShowToast($"Error: {ex.Message}", ToastDuration.Long);
            }
        }
   
    }
}
