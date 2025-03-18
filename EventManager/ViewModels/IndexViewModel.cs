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

        public IndexViewModel(DatabaseService databaseService, BeepService beepService, LogsViewModel logsViewModel, DashboardViewModel dashboardViewModel)
        {
            this.databaseService = databaseService;
            this.beepService = beepService;
            this.logsViewModel = logsViewModel;
            this.dashboardViewModel = dashboardViewModel;
            InitializeElementProperty();
        }
        private void InitializeElementProperty()
        {
            Debug.WriteLine("[IndexViewModel] Intialize Property");
            IdNumber = "ID Number:";
            IdPhoto = "blank_id.png";
            Name = "Name:";
            BusinessUnit = "Business Unit:";
            BarcodeNumber = string.Empty;
            Color = Colors.Black;
        }
        public async Task SetFocusEntry()
        {
            Debug.WriteLine("[IndexViewModel] Focus Set To Entry");
            IsEntryFocused = false;
            await Task.Delay(50);
            IsEntryFocused = true;
        }
        [RelayCommand]
        public async Task OnNavigatedTo()
        {
            Debug.WriteLine("[IndexViewModel] Page Appearing");
            await databaseService.InitializeTablesAsync();
            await LoadSelectedEvent();
            await SetFocusEntry();
            await beepService.InitializeBeepSound();
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
                            BarcodeNumber = string.Empty;
                            IdNumber = "ALREADY SCANNED";
                            IdPhoto = ImageHelper.ConvertBytesToImage(scannedEmployee.IdPhoto, 130, 130);
                            Name = $"Name {scannedEmployee.Name}";
                            BusinessUnit = $"Business Unit: {scannedEmployee.BusinessUnit}";
                            Color = Colors.Red;
                        }
                        else
                        {
                            BarcodeNumber = string.Empty;
                            IdNumber = $"ID Number: {scannedEmployee.IdNumber}";
                            IdPhoto = ImageHelper.ConvertBytesToImage(scannedEmployee.IdPhoto, 130, 130);
                            Name = $"Name {scannedEmployee.Name}";
                            BusinessUnit = $"Business Unit: {scannedEmployee.BusinessUnit}";
                            Color = Colors.Green;
                            logsViewModel.isLogsLoaded = false;
                            dashboardViewModel.isAllDashboardDataLoaded = false;
                            await databaseService.InsertAttendanceLog(scannedEmployee.IdNumber, scannedEmployee.Name, scannedEmployee.BusinessUnit, "SUCCESS", selectedEvent.EventName, selectedEvent.EventCategory, selectedEvent.EventDate, selectedEvent.FormattedTime);
                        }
                    }
                    else
                    {
                        bool isAlreadyNotFound = await databaseService.IsNotFoundLogAlreadyScanned(barcodeIdNumber, selectedEvent.EventName, selectedEvent.EventDate, selectedEvent.FormattedTime);
                        
                        if(isAlreadyNotFound)
                        {
                            BarcodeNumber = string.Empty;
                            IdNumber = $"ID Number: {barcodeIdNumber} Not Found";
                            IdPhoto = "invalid.png";
                            Name = "Name: Not Found";
                            BusinessUnit = "Business Unit: Not Found";
                            Color = Colors.Red;
                        }
                        else
                        {
                            BarcodeNumber = string.Empty;
                            IdNumber = $"ID Number: {barcodeIdNumber} Not Found";
                            IdPhoto = "invalid.png";
                            Name = "Name: Not Found";
                            BusinessUnit = "Business Unit: Not Found";
                            Color = Colors.Red;
                            logsViewModel.isLogsLoaded = false;
                            dashboardViewModel.isAllDashboardDataLoaded = false;
                            await databaseService.InsertAttendanceLog(barcodeIdNumber, "", "", "NOT FOUND", selectedEvent.EventName, selectedEvent.EventCategory, selectedEvent.EventDate, selectedEvent.FormattedTime);
                        }
                    }
                    await SetFocusEntry();
                }
                else
                {
                    await ToastHelper.ShowToast("Set event first!", ToastDuration.Long);
                }
            }
          
        }
        private async Task LoadSelectedEvent()
        {
            var selectedEvent = await databaseService.GetSelectedEvent();
            if (selectedEvent != null)
            {
                EventName = selectedEvent.EventName;
                EventDate = selectedEvent.EventDate;
                EventTime = selectedEvent.FormattedTime;
                EventImage = ImageHelper.ConvertBytesToImage(selectedEvent.EventImage);
            }
            else
            {
                await Task.Delay(50);
                EventName = "Set Event Name";
                EventDate = "Set Event Date";
                EventTime = "Set Event Time";
                EventImage = ImageSource.FromFile("event_image_placeholder.jpg");
            }
        }
    }
}
