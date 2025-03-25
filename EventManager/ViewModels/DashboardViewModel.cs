using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;
using EventManager.Utilities;
using EventManager.ViewModels.Popups;
using EventManager.Views.Modals;
using EventManager.Views.Popups;
using Mopups.Services;

namespace EventManager.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly IFileSaver fileSaverService;
        private int lastActiveEventId;
        private double totalScannedEmployeePercentage;
        private string presentEmployeeToString,
                       totalEmployeeToString,
                       totalScannedEmployeeToString;
        public bool isAllDashboardDataLoaded;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;
        [ObservableProperty]
        private bool isViewDataBusy;
        public bool IsViewDataNotBusy => !IsViewDataBusy;
        [ObservableProperty]
        private bool isExportDataBusy;
        public bool IsExportDataNotBusy => !IsExportDataBusy;
        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private string selectedBusinessUnit = "ALL";

        [ObservableProperty]
        private double totalEmployees;

        [ObservableProperty]
        private double presentEmployees;

        [ObservableProperty]
        private double absentEmployees;

        [ObservableProperty]
        private string totalScannedEmployee;

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        private ObservableCollection<AttendanceCategory> attendanceSummary = new();

        [ObservableProperty]
        private ObservableCollection<AttendanceByBU> attendanceByBusinessUnit = new();

        [ObservableProperty]
        private ObservableCollection<Brush> attendanceSummaryPalleteBrushes = new();

        [ObservableProperty]
        private ObservableCollection<Brush> attendanceByBusinessUnitPalleteBrushes = new();

        [ObservableProperty]
        private string cartesianCategory;
        [ObservableProperty]
        private string cartesianNumerical;
        [ObservableProperty]
        private string cartesianLegend;
        [ObservableProperty]
        private bool isShowDataLabel;

        public DashboardViewModel(DatabaseService databaseService, IFileSaver fileSaverService) 
        {
            this.databaseService = databaseService;
            this.fileSaverService = fileSaverService;
        }

        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            await ConditionalOnNavigatedTo();
        }

        private async Task ConditionalOnNavigatedTo()
        {
            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null)
            {
                ResetDashboardData();
                return;
            } 
            else if(lastActiveEventId != activeEvent.Id || !isAllDashboardDataLoaded)
            {
                await LoadDashboardData();
                isAllDashboardDataLoaded = true;
                lastActiveEventId = activeEvent.Id;
            }
        }

        public async Task LoadDashboardData()
        {
            IsBusyPageIndicator = true;

            var activeEvent = await databaseService.GetSelectedEvent();

            if (SelectedBusinessUnit == "ALL")
            {
                TotalEmployees = await databaseService.GetTotalEmployeeCountAsync();
                PresentEmployees = await databaseService.GetPresentEmployeeCountForActiveEvent();
            }
            else
            {
                TotalEmployees = await databaseService.GetTotalEmployeeCountByBUAsync(SelectedBusinessUnit);
                PresentEmployees = await databaseService.GetPresentEmployeeCountByBUAsync(SelectedBusinessUnit);
            }

            if (TotalEmployees > 0)
            {
                totalScannedEmployeePercentage = (PresentEmployees / TotalEmployees) * 100;
            }
            else
            {
                totalScannedEmployeePercentage = 0;
            }

            presentEmployeeToString = PresentEmployees.ToString();
            totalEmployeeToString = TotalEmployees.ToString();
            totalScannedEmployeeToString = totalScannedEmployeePercentage.ToString("F2");
            TotalScannedEmployee = $"{presentEmployeeToString} / {totalEmployeeToString} - {totalScannedEmployeeToString}%";
          
            AbsentEmployees = TotalEmployees - PresentEmployees;

            AttendanceSummary.Clear();
            AttendanceSummary.Add(new AttendanceCategory { Category = "Present", Count = PresentEmployees });
            AttendanceSummary.Add(new AttendanceCategory { Category = "Absent", Count = AbsentEmployees });

            AttendanceSummaryPalleteBrushes.Clear();

            for (int i = 0; i < AttendanceSummary.Count; i++)
            {
                if (AttendanceSummary[i].Category == "Present")
                {
                    AttendanceSummaryPalleteBrushes.Add(new SolidColorBrush(Color.FromArgb("#00C853")));
                }
                else
                {
                    AttendanceSummaryPalleteBrushes.Add(new SolidColorBrush(Color.FromArgb("#F5090B")));
                }
            }

            AttendanceByBusinessUnitPalleteBrushes.Clear();
            AttendanceByBusinessUnit.Clear();
            var tempList = new List<AttendanceByBU>();
            var businessUnits = new List<string> { "BAG", "HLB", "JLINE", "RAWLINGS", "SUPPORT GROUP" };

            for (int i = 0; i < businessUnits.Count; i++)
            {
                string bu = businessUnits[i];
                int buCount = 0;

                if (SelectedBusinessUnit == "ALL" || SelectedBusinessUnit == bu)
                {
                    AttendanceByBusinessUnitPalleteBrushes.Add(new SolidColorBrush(Color.FromArgb("#009AFE")));
                    buCount = await databaseService.GetPresentEmployeeCountByBUAsync(bu);
                }
                else
                {
                    AttendanceByBusinessUnitPalleteBrushes.Add(new SolidColorBrush(Color.FromArgb("#505050")));  
                }

                tempList.Add(new AttendanceByBU { BusinessUnit = bu, Count = buCount });
            }

            AttendanceByBusinessUnit.Clear();
            foreach (var item in tempList)
            {
                AttendanceByBusinessUnit.Add(item);
            }

            CartesianCategory = "Comparison of Business Units";
            CartesianNumerical = "Number of Attendees";
            CartesianLegend = "Employee";
            IsShowDataLabel = true;
            IsVisible = PresentEmployees > 0;
            IsBusyPageIndicator = false;
        }
        private void ResetDashboardData()
        {
            IsBusyPageIndicator = true;
            IsShowDataLabel = false;
            TotalEmployees = 0;
            PresentEmployees = 0;
            AbsentEmployees = 0;
            CartesianCategory = "No Data";
            CartesianLegend = "No Data";
            CartesianNumerical = "No Data";  
            presentEmployeeToString = "0";
            totalEmployeeToString = "0";
            totalScannedEmployeeToString = "0";
            TotalScannedEmployee = "No Data";

            AttendanceSummary.Clear();
            AttendanceByBusinessUnit.Clear();
            AttendanceSummaryPalleteBrushes.Clear();

            AttendanceSummary.Add(new AttendanceCategory { Category = "No Data", Count = 1 });
            AttendanceSummaryPalleteBrushes.Add(new SolidColorBrush(Color.FromArgb("#505050")));
            AttendanceByBusinessUnit.Add(new AttendanceByBU { BusinessUnit = "No Data", Count = 0 });
            IsBusyPageIndicator = false;
        }

        [RelayCommand]
        public async Task FilterDashboard()
        {
            var filterDashboardViewModel = new FilterDashboardViewModel(this);
            var filterDashboard = new FilterDashboard(filterDashboardViewModel);
            var activeEvent = await databaseService.GetSelectedEvent();

            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("No Data Found!", ToastDuration.Short);
                return;
            }

            if (IsBusy)
            { 
                return; 
            }

            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            await Task.Delay(300);
            await MopupService.Instance.PushAsync(filterDashboard);
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }

        public async Task ApplyFilter(string businessUnit)
        {
            SelectedBusinessUnit = businessUnit;
            await LoadDashboardData();
        }
        [RelayCommand]
        public async Task ViewData()
        {
            IsViewDataBusy = true;
            OnPropertyChanged(nameof(IsViewDataNotBusy));
            var parameter = new ShellNavigationQueryParameters {{"SelectedBusinessUnit", SelectedBusinessUnit}};
            await Shell.Current.GoToAsync(nameof(TotalScannedData),parameter);
            IsViewDataBusy = false;
            OnPropertyChanged(nameof(IsViewDataNotBusy));
        }

        [RelayCommand]
        public async Task ExportData()
        {
            IsExportDataBusy = true;
            OnPropertyChanged(nameof(IsExportDataNotBusy));    
            var activeEvent = await databaseService.GetSelectedEvent();

            List<EmployeeAttendanceStatus> employees;

            if (SelectedBusinessUnit == "ALL")
            {
                employees = await databaseService.GetTotalScannedDataPaginated(0, int.MaxValue);
            }
            else
            {
                employees = await databaseService.GetTotalScannedDataByBusinessUnitPaginated(SelectedBusinessUnit, 0, int.MaxValue);
            }

            try
            {
                string fileName = $"AttendanceData_{activeEvent.EventName}_{SelectedBusinessUnit}_{DateTime.Now}.xlsx";
                var memoryStream = new MemoryStream();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendance Data");

                    worksheet.Cell(1, 1).Value = "Event Name";
                    worksheet.Cell(1, 2).Value = "Category";
                    worksheet.Cell(1, 3).Value = "Date";
                    worksheet.Cell(1, 4).Value = "Time";
                    worksheet.Cell(1, 5).Value = "ID Number";
                    worksheet.Cell(1, 6).Value = "Name";
                    worksheet.Cell(1, 7).Value = "Business Unit";
                    worksheet.Cell(1, 8).Value = "Status";

                    int row = 2;
                    foreach (var employee in employees)
                    {
                        worksheet.Cell(row, 1).Value = activeEvent.EventName;
                        worksheet.Cell(row, 2).Value = activeEvent.EventCategory;
                        worksheet.Cell(row, 3).Value = activeEvent.EventDate;
                        worksheet.Cell(row, 4).Value = activeEvent.FormattedTime;
                        worksheet.Cell(row, 5).Value = employee.IdNumber;
                        worksheet.Cell(row, 6).Value = employee.Name;
                        worksheet.Cell(row, 7).Value = employee.BusinessUnit;
                        worksheet.Cell(row, 8).Value = employee.Status;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(memoryStream);
                    memoryStream.Position = 0;
                }

                var fileResult = await fileSaverService.SaveAsync(fileName, memoryStream, CancellationToken.None);

                if (!fileResult.IsSuccessful)
                {
                    await ToastHelper.ShowToast($"Export failed: {fileResult.Exception?.Message}", ToastDuration.Long);
                    return;
                }

                await ToastHelper.ShowToast($"Export successful!\nFile saved: {fileResult.FilePath}", ToastDuration.Long);
            }
            catch (Exception ex)
            {
                await ToastHelper.ShowToast($"Export failed! {ex.Message}", ToastDuration.Short);
            }
            finally
            {
                IsExportDataBusy = false;
                OnPropertyChanged(nameof(IsExportDataNotBusy));
            }
        }


    }
}
