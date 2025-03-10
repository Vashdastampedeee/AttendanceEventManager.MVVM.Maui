using System.Diagnostics;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using CommunityToolkit.Maui.Core;
using System.Globalization;

namespace EventManager.ViewModels
{
    public partial class DatabaseViewModel : ObservableObject
    {
        private readonly IFileSaver fileSaver;
        private readonly DatabaseService databaseService;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;

        public DatabaseViewModel(IFileSaver fileSaver, DatabaseService databaseService)
        {
            this.fileSaver = fileSaver;
            this.databaseService = databaseService;
        }

        [RelayCommand]
        public async Task ExportDatabase()
        {
            try
            {
                if (IsBusy) return;
                IsBusy = true;
                OnPropertyChanged(nameof(IsNotBusy));
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
                    await ShowToast($"Database exported: {filePath}", ToastDuration.Long);
                }
                else
                {
                    Debug.WriteLine($"[DatabaseViewModel] Error saving file: {fileSaverResult.Exception?.Message}");
                    await ShowToast($"Export failed: {fileSaverResult.Exception?.Message}", ToastDuration.Long);
                }
                IsBusy = false;
                OnPropertyChanged(nameof(IsNotBusy));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseViewModel] Exception: {ex.Message}");
                await ShowToast($"Error: {ex.Message}", ToastDuration.Long);
            }
        }
        private async Task ShowToast(string message, ToastDuration duration)
        {
            await Toast.Make(message, duration, 14).Show();
        }
    }
}
