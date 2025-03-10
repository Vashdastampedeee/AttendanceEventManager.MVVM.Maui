using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;

namespace EventManager.ViewModels
{
    public partial class DatabaseViewModel : ObservableObject
    {
        private readonly IFileSaver fileSaver;
        private readonly DatabaseService databaseService;

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
                string dbPath = databaseService.GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    Debug.WriteLine("[DatabaseViewModel] Database file not found.");
                    return;
                }

                using var dbStream = File.OpenRead(dbPath);
                var fileSaverResult = await fileSaver.SaveAsync("eventmanager.db", dbStream, CancellationToken.None);

                if (fileSaverResult.IsSuccessful)
                {
                    Debug.WriteLine($"[DatabaseViewModel] File saved at: {fileSaverResult.FilePath}");
                }
                else
                {
                    Debug.WriteLine($"[DatabaseViewModel] Error saving file: {fileSaverResult.Exception?.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseViewModel] Exception: {ex.Message}");
            }
        }
    }
}
