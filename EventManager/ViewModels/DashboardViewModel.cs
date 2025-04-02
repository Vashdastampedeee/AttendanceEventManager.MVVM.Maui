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

        private int previousActiveEvent;
        public bool isAllDashboardDataLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowPage))]
        private bool isBusyPageIndicator;
        public bool IsShowPage => !IsBusyPageIndicator;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isFilterBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isViewDataBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAnyBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isExportDataBusy;
        public bool IsAnyBusy => IsFilterBusy || IsViewDataBusy || IsExportDataBusy;
        public bool IsNotBusy => !IsAnyBusy;

        [ObservableProperty] private string selectedBusinessUnit = "ALL";

        [ObservableProperty] private double totalEmployees;
        [ObservableProperty] private double presentEmployees;
        [ObservableProperty] private double absentEmployees;
        [ObservableProperty] private string totalScannedEmployee;
        private double totalScannedPercentage;

        [ObservableProperty] private ObservableCollection<AttendanceData> attendanceData = new();
        [ObservableProperty] private ObservableCollection<Brush> attendanceDataChartColor = new();
        private List<string> businessUnitsData = new List<string>{ "BAG", "HLB", "JLINE", "RAWLINGS", "SUPPORT GROUP" };
        private string businessUnitCategory;
        private int businessUnitCategoryCount;

        [ObservableProperty] private ObservableCollection<AttendanceStatus> attendanceStatus = new();
        [ObservableProperty] private ObservableCollection<Brush> attendanceStatusChartColor = new();
        private List<string> businessUnitsStatus = new List<string> { "Present", "Absent" };
        private string businessUnitStatus;
        private int businessUnitStatusCount;

        [ObservableProperty] private string cartesianLegend;
        [ObservableProperty] private bool isShowDataLabel;

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
            else
            {
                if (previousActiveEvent != activeEvent.Id || !isAllDashboardDataLoaded)
                {
                    await LoadDashboardData();
                    previousActiveEvent = activeEvent.Id;
                }
            }
        }
        
        public async Task LoadDashboardData()
        {
            IsBusyPageIndicator = true;

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
                totalScannedPercentage = (PresentEmployees / TotalEmployees) * 100;
            }   
            else
            {
                totalScannedPercentage = 0;
            }

            TotalScannedEmployee = $"{Convert.ToString(PresentEmployees)} / {Convert.ToString(TotalEmployees)} - {totalScannedPercentage:F2}%";
            AbsentEmployees = TotalEmployees - PresentEmployees;

            AttendanceStatus.Clear();
            AttendanceStatusChartColor.Clear();

            for (int i = 0; i < businessUnitsStatus.Count; i++)
            {
                businessUnitStatus = businessUnitsStatus[i];
                if (businessUnitStatus == "Present")
                {
                    AttendanceStatusChartColor.Add(new SolidColorBrush(Color.FromArgb("#00C853")));
                    businessUnitStatusCount = (int)PresentEmployees; 
                }
                else
                {
                    AttendanceStatusChartColor.Add(new SolidColorBrush(Color.FromArgb("#F5090B")));
                    businessUnitStatusCount = (int)AbsentEmployees; 
                }
                AttendanceStatus.Add(new AttendanceStatus { Category = businessUnitStatus, Count = businessUnitStatusCount });
            }

            AttendanceData.Clear();
            AttendanceDataChartColor.Clear();

            for (int i = 0; i < businessUnitsData.Count; i++)
            {
                businessUnitCategory = businessUnitsData[i];
                businessUnitCategoryCount = 0;

                if (SelectedBusinessUnit == "ALL" || SelectedBusinessUnit == businessUnitCategory)
                {
                    AttendanceDataChartColor.Add(new SolidColorBrush(Color.FromArgb("#009AFE")));
                    businessUnitCategoryCount = await databaseService.GetPresentEmployeeCountByBUAsync(businessUnitCategory);
                }
                else
                {
                    AttendanceDataChartColor.Add(new SolidColorBrush(Color.FromArgb("#505050")));  
                }
                AttendanceData.Add(new AttendanceData { BusinessUnit = businessUnitCategory, Count = businessUnitCategoryCount });
            }

            CartesianLegend = "Number of Attendees";
            IsShowDataLabel = true;
            isAllDashboardDataLoaded = true;

            IsBusyPageIndicator = false;
   
        }
        private void ResetDashboardData()
        {
            IsBusyPageIndicator = true;

            TotalEmployees = 0;
            PresentEmployees = 0;
            AbsentEmployees = 0;
            totalScannedPercentage = 0;

            IsShowDataLabel = false;
            TotalScannedEmployee = "No Data";
            CartesianLegend = "No Data";

            AttendanceStatus.Clear();
            AttendanceData.Clear();
            AttendanceStatusChartColor.Clear();
            AttendanceDataChartColor.Clear();

            AttendanceStatus.Add(new AttendanceStatus { Category = "No Data", Count = 1 });
            AttendanceStatusChartColor.Add(new SolidColorBrush(Color.FromArgb("#F5090B")));
            AttendanceData.Add(new AttendanceData { BusinessUnit = "No Data", Count = 1 });
            AttendanceDataChartColor.Add(new SolidColorBrush(Color.FromArgb("#F5090B")));

            IsBusyPageIndicator = false;
        }

        [RelayCommand]
        public async Task FilterDashboard()
        {
            IsFilterBusy = true;

            var activeEvent = await databaseService.GetSelectedEvent();
            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
            }
            else
            {
                var filterDashboardViewModel = new FilterDashboardViewModel(this);
                var filterDashboard = new FilterDashboard(filterDashboardViewModel);
                await MopupService.Instance.PushAsync(filterDashboard);
            }

            IsFilterBusy = false;
        }

        public async Task ApplyFilter(string businessUnit)
        {
            if (SelectedBusinessUnit == businessUnit)
            {
                await ToastHelper.ShowToast("Filter already applied!", ToastDuration.Short);
                return;
            }
            else
            {
                SelectedBusinessUnit = businessUnit;
                await LoadDashboardData();
            }
        }

        [RelayCommand]
        public async Task ViewData()
        {
            IsViewDataBusy = true;

            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
            }
            else if(PresentEmployees == 0)
            {
                await ToastHelper.ShowToast("No Data Found!", ToastDuration.Short);
            }
            else
            {
                var parameter = new ShellNavigationQueryParameters { { "SelectedBusinessUnit", SelectedBusinessUnit } };
                await Shell.Current.GoToAsync(nameof(TotalScannedData), parameter);
            }

            IsViewDataBusy = false;
        }

        [RelayCommand]
        public async Task ExportData()
        {
            IsExportDataBusy = true;
    
            var activeEvent = await databaseService.GetSelectedEvent();
            if (activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
            }
            else if (PresentEmployees == 0)
            {
                await ToastHelper.ShowToast("No Data Found!", ToastDuration.Short);
            }
            else
            {
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
                    string fileName = $"AttendanceData_{activeEvent?.EventName}_{SelectedBusinessUnit}({DateTime.Now}).xlsx";
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
                    }
                    else
                    {
                        await ToastHelper.ShowToast($"Export successful!\nFile saved: {fileResult.FilePath}", ToastDuration.Long);
                    }
                }
                catch (Exception ex)
                {
                    await ToastHelper.ShowToast($"Export failed! {ex.Message}", ToastDuration.Short);
                }
            }

            IsExportDataBusy = false;
        }
    }
}
