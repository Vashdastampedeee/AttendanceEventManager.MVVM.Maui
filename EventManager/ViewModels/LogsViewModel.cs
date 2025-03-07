using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;

namespace EventManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private const int PageSize = 10; 
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreLogs;
        private bool isAllLogsDataLoaded;

        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isBusyIndicator;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        public LogsViewModel(DatabaseService databaseServiceInjection) 
        {
            databaseService = databaseServiceInjection;
        }
        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            if (AttendanceLogs.Count == 0)
            {
                await LoadAttendanceLogs();
            }
        }

        [RelayCommand]
        public async Task LoadAttendanceLogs()
        {
            if (isLoadingMoreLogs || isAllLogsDataLoaded)
            {
                return;
            }

            isLoadingMoreLogs = true;
            IsEnabled = false;
            IsBusyIndicator = AttendanceLogs.Count == 0; 
            IsLoadingDataIndicator = AttendanceLogs.Count > 0; 
            var logs = await databaseService.GetAttendanceLogsPaginated(lastLoadedIndex, PageSize);

            if (logs.Any())
            {
                await Task.Delay(1000);
                foreach (var log in logs)
                {
                    AttendanceLogs.Add(log);
                }
                lastLoadedIndex += logs.Count; 
            }

            if (logs.Count < PageSize)
            {
                isAllLogsDataLoaded = true;
            }

            IsEnabled = true;
            IsBusyIndicator = false;
            IsLoadingDataIndicator = false;
            isLoadingMoreLogs = false;
        }

    }
}
