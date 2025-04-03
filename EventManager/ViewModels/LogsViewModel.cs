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

namespace EventManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly IFileSaver fileSaverService;

        private const int pageSize = 10;
        private int lastLoadedIndex = 0;
        private bool isAllDataLoaded;
        private bool isLoading;
        public bool isLogsLoaded;

        [ObservableProperty] private bool isBusyPageIndicator;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isExportBusy;
        public bool IsNotBusy => !IsExportBusy;

        [ObservableProperty] private ObservableCollection<AttendanceLog> attendanceLogs = new();
        [ObservableProperty] private bool isNoDataVisible;
        [ObservableProperty] private string isNoDataLabel;
 
        private bool isSearching;
        [ObservableProperty] private string searchText;

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
                SetVisibilityAndLabelData(AttendanceLogs.Count == 0, "No active event set");
                return;
            }
            else
            {
                if (AttendanceLogs.Count == 0)
                {
                    if (!isLogsLoaded)
                    {
                        await RefreshLogs();
                    }
                    else
                    {
                        await LoadAttendanceLogs();
                    }
                }
                else
                {
                    if (!isLogsLoaded)
                    {
                        await RefreshLogs();
                    }
                }
            }
        }
        
        [RelayCommand]
        public async Task LoadAttendanceLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null || isLoading || isAllDataLoaded)
            {
                return;
            }
            else
            {
                IsBusyPageIndicator = AttendanceLogs.Count == 0;
                isLoading = true;

                await Task.Yield();

                List<AttendanceLog> logs;

                if (isSearching)
                {
                    logs = await databaseService.SearchLogs(SearchText.Trim(), lastLoadedIndex, pageSize);
                }
                else
                {
                    logs = await databaseService.GetAttendanceLogsPaginated(lastLoadedIndex, pageSize);
                }

                if (logs.Count != 0)
                {
                    foreach (var log in logs)
                    {
                        AttendanceLogs.Add(log);
                    }

                    lastLoadedIndex += logs.Count;
                }

                if (logs.Count < pageSize)
                {
                    isAllDataLoaded = true;
                }

                SetVisibilityAndLabelData(AttendanceLogs.Count == 0, "No attendance log data found");

                isLogsLoaded = true;
                isLoading = false;
                IsBusyPageIndicator = false;
            }         
        }

        [RelayCommand]
        public async Task RefreshLogs()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
            }
            else
            {
                SearchText = string.Empty;
                SetSearchAndRefreshData(false, false, 0);
                AttendanceLogs.Clear();
                await LoadAttendanceLogs();
            }
        }

        [RelayCommand]
        public async Task SearchLogs()
        {
            SetSearchAndRefreshData(!string.IsNullOrEmpty(SearchText), false, 0);
            AttendanceLogs.Clear();
            await LoadAttendanceLogs();
        }

        [RelayCommand]
        public async Task ExportFilteredLogs()
        {
            IsExportBusy = true;

            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null)
            {
                await ToastHelper.ShowToast("Set event first!", ToastDuration.Short);
                return;
            }
            else
            {
                var logs = await databaseService.GetAttendanceLogsPaginated(0, int.MaxValue);
                if (logs.Count == 0)
                {
                    await ToastHelper.ShowToast("No data available for export!", ToastDuration.Short);
                    return;
                }
                else
                {
                    try
                    {
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
                }
            }

            IsExportBusy = false;
        }
        
        private void SetVisibilityAndLabelData(bool isNoDataVisible, string isNoDataLabel)
        {
            IsNoDataVisible = isNoDataVisible;
            IsNoDataLabel = isNoDataLabel;
        }

        private void SetSearchAndRefreshData(bool isSearching, bool isAllDataLoaded, int lastLoadedIndex)
        {
            this.isSearching = isSearching;
            this.isAllDataLoaded = isAllDataLoaded;
            this.lastLoadedIndex = lastLoadedIndex;
        }

    }
}
