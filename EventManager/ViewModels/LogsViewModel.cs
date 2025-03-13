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
using EventManager.ViewModels.Popups;
using EventManager.Views.Popups;
using Mopups.Services;

namespace EventManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private const int pageSize = 10; 
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreLogs;
        private bool isAllLogsDataLoaded;

        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isBusyPageIndicator;

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
            IsBusyPageIndicator = AttendanceLogs.Count == 0; 
            IsLoadingDataIndicator = AttendanceLogs.Count > 0; 
            var logs = await databaseService.GetAttendanceLogsPaginated(lastLoadedIndex, pageSize);

            if (logs.Any())
            {
                await Task.Delay(1000);
                foreach (var log in logs)
                {
                    AttendanceLogs.Add(log);
                }
                lastLoadedIndex += logs.Count; 
            }

            if (logs.Count < pageSize)
            {
                isAllLogsDataLoaded = true;
            }

            IsEnabled = true;
            IsBusyPageIndicator = false;
            IsLoadingDataIndicator = false;
            isLoadingMoreLogs = false;
        }

        [RelayCommand]
        public async Task RefreshLogs()
        {
            Debug.WriteLine("[LogsViewModel] - refreshing logs");

            lastLoadedIndex = 0;
            isAllLogsDataLoaded = false;
            AttendanceLogs.Clear();
            await LoadAttendanceLogs();
        }
        [RelayCommand]
        public async Task FilterLogs()
        {
            var filterLogViewModel = new FilterLogViewModel();
            var filterLog = new FilterLog(filterLogViewModel);
            await MopupService.Instance.PushAsync(filterLog);
        }

    }
}
