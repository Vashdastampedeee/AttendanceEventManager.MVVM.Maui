using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
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
        private readonly IFileSaver fileSaverService;
        private const int pageSize = 10;
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreLogs;
        private bool isAllLogsDataLoaded;
        private int lastActiveEventId;
        public bool isLogsLoaded;

        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isNoDataVisible;

        [ObservableProperty]
        private string isNoDataLabel;

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private bool isFiltering;

        private LogFilter selectedFilter;

        [ObservableProperty]
        private bool isSearching;

        [ObservableProperty]
        private string searchText;



        public LogsViewModel(DatabaseService databaseService, IFileSaver fileSaverService)
        {
            this.databaseService = databaseService;
            this.fileSaverService = fileSaverService;
        }
        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            await ConditionalOnNavigated();
        }
        private async Task ConditionalOnNavigated()
        {
            var activeEvent = await databaseService.GetSelectedEvent();

            if (activeEvent == null)
            {
                IsNoDataVisible = AttendanceLogs.Count == 0;
                IsNoDataLabel = "No active event set";
                return;
            }

            selectedFilter = new LogFilter
            {
                Name = activeEvent.EventName,
                Category = activeEvent.EventCategory,
                Date = activeEvent.EventDate,
                Time = activeEvent.FormattedTime
            };
        
            if (AttendanceLogs.Count == 0)
            {
                await Task.Delay(100);
                if (lastActiveEventId != activeEvent.Id || !isLogsLoaded)
                {
                    await RefreshLogs();
                }
                else
                {
                    await LoadAttendanceLogs();
                }               
            }
            else if (lastActiveEventId != activeEvent.Id || !isLogsLoaded)
            {
          
                await RefreshLogs();
            }
            lastActiveEventId = activeEvent.Id;
      
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
                return;
            }
            isLoadingMoreLogs = true;
            IsEnabled = false;
            IsBusyPageIndicator = AttendanceLogs.Count == 0; 
            IsLoadingDataIndicator = AttendanceLogs.Count > 0;

            List<AttendanceLog> logs;

            if (IsFiltering && IsSearching)
            {
                logs = await databaseService.SearchFilteredLogs(selectedFilter, SearchText.Trim(), lastLoadedIndex, pageSize);
            }
            else if (IsFiltering)
            {
                logs = await databaseService.GetFilteredLogs(selectedFilter, lastLoadedIndex, pageSize);
            }
            else
            {
                logs = await databaseService.GetAttendanceLogsPaginated(activeEvent.EventName, activeEvent.EventCategory, activeEvent.EventDate, activeEvent.FormattedTime, lastLoadedIndex, pageSize);
            }

            if (logs.Count != 0)
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
            isLogsLoaded = true;
            IsNoDataVisible = AttendanceLogs.Count == 0;
            IsNoDataLabel = "No attendance log data found";
        }

        [RelayCommand]
        public async Task RefreshLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
            }

            IsNoDataVisible = false;
            SearchText = string.Empty;
            IsSearching = false;
            IsFiltering = false;
            lastLoadedIndex = 0;
            isAllLogsDataLoaded = false;
       
            selectedFilter = new LogFilter
            {
                Name = activeEvent.EventName,
                Category = activeEvent.EventCategory,
                Date = activeEvent.EventDate,
                Time = activeEvent.FormattedTime
            };

            AttendanceLogs.Clear();
            await Task.Delay(100);
            await LoadAttendanceLogs();
        }
        [RelayCommand]
        public async Task FilterLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
                return;
            }
            else
            {
                if (AttendanceLogs.Count == 0)
                {
                    await ToastHelper.ShowToast("Scan Employee First!", ToastDuration.Short);
                    return;
                }
                else
                {
                    var filterLogViewModel = new FilterLogViewModel(databaseService, this, selectedFilter);
                    var filterLog = new FilterLog(filterLogViewModel);
                    await MopupService.Instance.PushAsync(filterLog);
                }
            }
        }
        public async Task ApplyFilterLogs(LogFilter filter)
        {
            isAllLogsDataLoaded = false;
            lastLoadedIndex = 0;
            SearchText = string.Empty;
            IsSearching = false;
            IsFiltering = true;
            selectedFilter = filter;

            AttendanceLogs.Clear();
            await Task.Delay(100);
            await LoadAttendanceLogs();
        }


        [RelayCommand]
        public async Task SearchLogs()
        {
            isAllLogsDataLoaded = false;
            lastLoadedIndex = 0;
            IsFiltering = true;
            IsSearching = !string.IsNullOrEmpty(SearchText);

            AttendanceLogs.Clear();
            await Task.Delay(100);
            await LoadAttendanceLogs();
        }

        [RelayCommand]
        public async Task ExportFilteredLogs()
        {
            if (selectedFilter == null)
            {
                var activeEvent = await databaseService.GetSelectedEvent();
                if (activeEvent == null)
                {
                    await ToastHelper.ShowToast("Set event first!", ToastDuration.Short);
                }
                return;
            }

            var logs = await databaseService.GetFilteredLogsForExport(selectedFilter);

            if (logs.Count == 0)
            {
                await ToastHelper.ShowToast("No data available for export!", ToastDuration.Short);
                return;
            }

            try
            {
                IsEnabled = false;
                IsBusy = true;
                OnPropertyChanged(nameof(IsNotBusy));

                await Task.Delay(300);

                string fileName = $"Logs({DateTime.Now}).xlsx";
                var memoryStream = new MemoryStream();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendance Logs");

                    worksheet.Cell(1, 1).Value = "EventName";
                    worksheet.Cell(1, 2).Value = "Category";
                    worksheet.Cell(1, 3).Value = "Date";
                    worksheet.Cell(1, 4).Value = "Time";
                    worksheet.Cell(1, 5).Value = "ID Number";
                    worksheet.Cell(1, 6).Value = "Name";
                    worksheet.Cell(1, 7).Value = "Business Unit";
                    worksheet.Cell(1, 8).Value = "Status";
                    worksheet.Cell(1, 9).Value = "Timestamp";

                    int row = 2;
                    foreach (var log in logs)
                    {
                        worksheet.Cell(row, 1).Value = log.EventName;
                        worksheet.Cell(row, 2).Value = log.EventCategory;
                        worksheet.Cell(row, 3).Value = log.EventDate;
                        worksheet.Cell(row, 4).Value = log.EventTime;
                        worksheet.Cell(row, 5).Value = log.IdNumber;
                        worksheet.Cell(row, 6).Value = log.Name;
                        worksheet.Cell(row, 7).Value = log.BusinessUnit;
                        worksheet.Cell(row, 8).Value = log.Status;
                        worksheet.Cell(row, 9).Value = log.Timestamp;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(memoryStream);
                    memoryStream.Position = 0;
                }

                var fileResult = await fileSaverService.SaveAsync(fileName, memoryStream, CancellationToken.None);

                if (!fileResult.IsSuccessful)
                {
                    Debug.WriteLine($"[LogsViewModel] Error saving file: {fileResult.Exception?.Message}");
                    await ToastHelper.ShowToast($"Export failed: {fileResult.Exception?.Message}", ToastDuration.Long);
                    return;
                }

                await ToastHelper.ShowToast($"Export successful!\nFile saved: {fileResult.FilePath}", ToastDuration.Long);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogsViewModel] Export Error: {ex.Message}\nStack Trace: {ex.StackTrace}");
                await ToastHelper.ShowToast($"Export failed! {ex.Message}", ToastDuration.Short);
            }
            finally
            {
                IsEnabled = true;
                IsBusy = false;
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }
}
