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
        private FilterLogViewModel filterLogViewModel;
        private const int pageSize = 10; 
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreLogs;
        private bool isAllLogsDataLoaded;
        private string lastActiveEventName;

        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isNoDataVisible;

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

            if (selectedFilter == null || selectedFilter.Name != activeEvent.EventName)
            {
                selectedFilter = new LogFilter
                {
                    Name = activeEvent.EventName,
                    Category = activeEvent.EventCategory,
                    Date = activeEvent.EventDate,
                    Time = activeEvent.FormattedTime
                };
            }

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
            IsNoDataVisible = AttendanceLogs.Count == 0;
        }

        [RelayCommand]
        public async Task RefreshLogs()
        {
            Debug.WriteLine("[LogsViewModel] - refreshing logs");

            lastLoadedIndex = 0;
            isAllLogsDataLoaded = false;
            IsFiltering = false;

            var activeEvent = await databaseService.GetSelectedEvent();
            selectedFilter = new LogFilter
            {
                Name = activeEvent.EventName,
                Category = activeEvent.EventCategory,
                Date = activeEvent.EventDate,
                Time = activeEvent.FormattedTime
            };

            AttendanceLogs.Clear();
            await LoadAttendanceLogs();
        }
        [RelayCommand]
        public async Task FilterLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if (filterLogViewModel == null)
            {
                filterLogViewModel = new FilterLogViewModel(databaseService, this);
            }
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

        [RelayCommand]
        public async Task ExportFilteredLogs()
        {
            if (selectedFilter == null)
            {
                await ToastHelper.ShowToast("No filter applied!", ToastDuration.Short);
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
                IsBusy = true;
                OnPropertyChanged(nameof(IsNotBusy));

                await Task.Delay(500);

                string fileName = $"ExportedLogs({DateTime.Now}).xlsx";
                var fileResult = await fileSaverService.SaveAsync(fileName, new MemoryStream(), CancellationToken.None);

                if (!fileResult.IsSuccessful)
                {
                    Debug.WriteLine($"[LogsViewModel] Error saving file: {fileResult.Exception?.Message}");
                    await ToastHelper.ShowToast($"Export failed: {fileResult.Exception?.Message}", ToastDuration.Long);
                    return;
                }

                string filePath = fileResult.FilePath;

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

                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.SaveAs(stream);
                    }
                }

                await ToastHelper.ShowToast($"Export successful!\nFile saved: {filePath}", ToastDuration.Long);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogsViewModel] Export Error: {ex.Message}");
                await ToastHelper.ShowToast("Export failed!", ToastDuration.Short);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }

    }
}
