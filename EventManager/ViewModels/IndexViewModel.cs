using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.Utilities;

namespace EventManager.ViewModels
{
    public partial class IndexViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly BeepService beepService;
        private readonly LogsViewModel logsViewModel;
        private readonly DashboardViewModel dashboardViewModel;

        [ObservableProperty] private string eventName;
        [ObservableProperty] private string eventDate;
        [ObservableProperty] private string eventTime;
        [ObservableProperty] private ImageSource eventImage;

        [ObservableProperty] private string idNumber;
        [ObservableProperty] private ImageSource idPhoto;
        [ObservableProperty] private string name;
        [ObservableProperty] private string businessUnit;
        [ObservableProperty] private string barcodeNumber;
        [ObservableProperty] private Color color;
        [ObservableProperty] private bool isEntryFocused;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowPage))] 
        private bool isBusyPageIndicator;
        public bool IsShowPage => !IsBusyPageIndicator;

        public IndexViewModel(DatabaseService databaseService, BeepService beepService, LogsViewModel logsViewModel, DashboardViewModel dashboardViewModel)
        {
            this.databaseService = databaseService;
            this.beepService = beepService;
            this.logsViewModel = logsViewModel;
            this.dashboardViewModel = dashboardViewModel;
            SetElementsProperty(
                "ID Number:",
                "blank_id.png",
                "Name:",
                "Business Unit:",
                ScanStatusHelper.FrameColor.Default);
        }

        [RelayCommand]
        public async Task OnNavigatedTo()
        {
 
            IsBusyPageIndicator = true;

            await Task.Yield();

            await databaseService.InitializeTablesAsync();
            await LoadSelectedEvent();
            await SetFocusEntry();
            await beepService.InitializeBeepSound();

            IsBusyPageIndicator = false;
        }

        [RelayCommand]
        public async Task ScanEmployeeId()
        {
            string barcodeIdNumber = BarcodeNumber.Trim();
            if (string.IsNullOrEmpty(barcodeIdNumber))
            {
                return;
            }
            else
            {
                var selectedEvent = await databaseService.GetSelectedEvent();
                if (selectedEvent != null)
                {
                    var scannedEmployee = await databaseService.GetEmployeeIdNumber(barcodeIdNumber);
                    bool isAlreadyScanned = await databaseService.IsEmployeeAlreadyScanned(barcodeIdNumber, selectedEvent.EventName, selectedEvent.EventDate, selectedEvent.FormattedTime);
                    beepService.PlayBeep();

                    if (scannedEmployee != null)
                    {
                        if (isAlreadyScanned)
                        {
                            SetElementsProperty(
                                ScanStatusHelper.StatusString.AlreadyScanned,
                                ImageHelper.ConvertBytesToImage(scannedEmployee.IdPhoto, 130, 130),
                                $"Name {scannedEmployee.Name}",
                                $"Business Unit: {scannedEmployee.BusinessUnit}",
                                ScanStatusHelper.FrameColor.Error);
                        }
                        else
                        {
                            SetElementsProperty(
                                $"ID Number: {scannedEmployee.IdNumber}",
                                ImageHelper.ConvertBytesToImage(scannedEmployee.IdPhoto, 130, 130),
                                $"Name {scannedEmployee.Name}",
                                $"Business Unit: {scannedEmployee.BusinessUnit}",
                                ScanStatusHelper.FrameColor.Success);
                            ResetDashboardAndLogs();
                            await InsertAttendanceLog(scannedEmployee.IdNumber, scannedEmployee.Name, scannedEmployee.BusinessUnit, ScanStatusHelper.StatusString.Success, selectedEvent.EventName, selectedEvent.EventCategory, selectedEvent.EventDate, selectedEvent.FormattedTime);
                        }
                    }
                    else
                    {
                        bool isAlreadyNotFound = await databaseService.IsNotFoundLogAlreadyScanned(barcodeIdNumber, selectedEvent.EventName, selectedEvent.EventDate, selectedEvent.FormattedTime);
                        if (isAlreadyNotFound)
                        {
                            SetElementsProperty(
                                $"ID Number: {barcodeIdNumber} {ScanStatusHelper.StatusString.NotFound}",
                                "invalid.png",
                                $"Name: {ScanStatusHelper.StatusString.NotFound}",
                                $"Business Unit: {ScanStatusHelper.StatusString.NotFound}",
                                ScanStatusHelper.FrameColor.Error);
                        }
                        else
                        {
                            SetElementsProperty(
                                $"ID Number: {barcodeIdNumber} {ScanStatusHelper.StatusString.NotFound}",
                                "invalid.png",
                                $"Name: {ScanStatusHelper.StatusString.NotFound}",
                                $"Business Unit: {ScanStatusHelper.StatusString.NotFound}",
                                ScanStatusHelper.FrameColor.Error);
                            ResetDashboardAndLogs();
                            await InsertAttendanceLog(barcodeIdNumber, string.Empty, string.Empty, ScanStatusHelper.StatusString.NotFound, selectedEvent.EventName, selectedEvent.EventCategory, selectedEvent.EventDate, selectedEvent.FormattedTime);            
                        }
                    }
                    await SetFocusEntry();
                }
                else
                {
                    ClearBarcodeEntry();
                    await ToastHelper.ShowToast("Set event first!", ToastDuration.Long);
                }
            }

        }

        public async Task SetFocusEntry()
        {
            IsEntryFocused = false;
            await Task.Yield();
            IsEntryFocused = true;
        }

        private async Task LoadSelectedEvent()
        {
            var selectedEvent = await databaseService.GetSelectedEvent();
            if (selectedEvent != null)
            {
                LoadSelectedData(selectedEvent.EventName, selectedEvent.EventDate, selectedEvent.FormattedTime, ImageHelper.ConvertBytesToImage(selectedEvent.EventImage));
            }
            else
            {
                await Task.Yield();
                LoadSelectedData("Set Event Name", "Set Event Date", "Set Event Time", ImageSource.FromFile("event_image_placeholder.jpg"));
            }
        }

        private void ClearBarcodeEntry()
        {
            BarcodeNumber = string.Empty;
        }

        private void LoadSelectedData(string eventName, string eventDate, string eventTime, ImageSource eventImage)
        {
            EventName = eventName;
            EventDate = eventDate;
            EventTime = eventTime;
            EventImage = eventImage;
        }

        private void SetElementsProperty(string idNumber, ImageSource idPhoto, string employeeName, string businessUnit, Color color)
        {
            ClearBarcodeEntry();
            IdNumber = idNumber;
            IdPhoto = idPhoto;
            Name = employeeName;
            BusinessUnit = businessUnit;
            Color = color;
        }

        private async Task InsertAttendanceLog(string idNumber, string name, string businessUnit, string status, string eventName, string eventCategory, string eventDate, string eventTime)
        {
            await databaseService.InsertAttendanceLog(idNumber, name, businessUnit, status, eventName, eventCategory, eventDate, eventTime);
        }

        private void ResetDashboardAndLogs()
        {
            logsViewModel.isLogsLoaded = false;
            dashboardViewModel.isAllDashboardDataLoaded = false;
        }
    }
}
