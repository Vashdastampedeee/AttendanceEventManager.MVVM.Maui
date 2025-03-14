using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;
using EventManager.Utilities;
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
        private string lastActiveEventName;

        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        [ObservableProperty]
        private bool isFiltering;

        private LogFilter selectedFilter;

        public LogsViewModel(DatabaseService databaseServiceInjection) 
        {
            databaseService = databaseServiceInjection;
        }
        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            await ConditionalOnNavigated();
        }
        private async Task ConditionalOnNavigated()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if (AttendanceLogs.Count == 0)
            {
                await LoadAttendanceLogs();
            }
            else if(lastActiveEventName != activeEvent.EventName)
            {
                await RefreshLogs();
            }
            lastActiveEventName = activeEvent.EventName;
        }
        [RelayCommand]
        public async Task LoadAttendanceLogs()
        {
            if (isLoadingMoreLogs || isAllLogsDataLoaded)
            {
                return;
            }

            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null)
            {
                await ToastHelper.ShowToast("No active event!", ToastDuration.Short);
                return;
            }
            isLoadingMoreLogs = true;
            IsEnabled = false;
            IsBusyPageIndicator = AttendanceLogs.Count == 0; 
            IsLoadingDataIndicator = AttendanceLogs.Count > 0;

            List<AttendanceLog> logs;
            if (IsFiltering)
            {
                logs = await databaseService.GetFilteredLogs(selectedFilter, lastLoadedIndex, pageSize);
            }
            else
            {
                logs = await databaseService.GetAttendanceLogsPaginated(activeEvent.EventName, activeEvent.EventCategory, activeEvent.EventDate, activeEvent.FormattedTime, lastLoadedIndex, pageSize);

            }

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
            IsFiltering = false;
            selectedFilter = null;

            AttendanceLogs.Clear();
            await LoadAttendanceLogs();
        }
        [RelayCommand]
        public async Task FilterLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            var filterLogViewModel = new FilterLogViewModel(databaseService, this);
            var filterLog = new FilterLog(filterLogViewModel);
            await MopupService.Instance.PushAsync(filterLog);
        }
        public async Task ApplyFilterLogs(LogFilter filter)
        {
            isAllLogsDataLoaded = false;
            lastLoadedIndex = 0;
            IsFiltering = true;
            selectedFilter = filter;

            AttendanceLogs.Clear();
            await LoadAttendanceLogs();
        }

    }
}
