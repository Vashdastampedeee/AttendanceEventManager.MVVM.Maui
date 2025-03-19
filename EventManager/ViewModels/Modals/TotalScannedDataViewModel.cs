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

namespace EventManager.ViewModels.Modals
{
    [QueryProperty(nameof(SelectedBusinessUnit), "SelectedBusinessUnit")]
    public partial class TotalScannedDataViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private const int PageSize = 10;
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreData;
        private bool isAllDataLoaded;
        private string isLastSelectedBuName;

        [ObservableProperty]
        private ObservableCollection<EmployeeAttendanceStatus> employeeAttendance = new();

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        public string selectedBusinessUnit;
        public TotalScannedDataViewModel(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        [RelayCommand]
        public async Task OnNavigatedTo()
        {
            if (isLastSelectedBuName != SelectedBusinessUnit)
            {
                EmployeeAttendance.Clear();
                lastLoadedIndex = 0;
                isAllDataLoaded = false;
                await Task.Delay(100);
                await LoadEmployeeAttendanceStatus();
            }
        }

        [RelayCommand]
        public async Task LoadEmployeeAttendanceStatus()
        {
            if (isLoadingMoreData || isAllDataLoaded) return;

            isLoadingMoreData = true;
            IsLoadingDataIndicator = EmployeeAttendance.Count > 0;
            IsBusyPageIndicator = EmployeeAttendance.Count == 0;

            List<EmployeeAttendanceStatus> employees;

            if (SelectedBusinessUnit == "ALL")
            {
                employees = await databaseService.GetTotalScannedDataPaginated(lastLoadedIndex, PageSize);
            }
            else
            {
                employees = await databaseService.GetTotalScannedDataByBusinessUnitPaginated(SelectedBusinessUnit, lastLoadedIndex, PageSize);
            }

            if (employees.Count != 0)
            {
                await Task.Delay(500);
                foreach (var emp in employees)
                {
                    EmployeeAttendance.Add(emp);
                }
                lastLoadedIndex += employees.Count;
            }

            if (employees.Count < PageSize)
            {
                isAllDataLoaded = true;
            }

            isLastSelectedBuName = SelectedBusinessUnit;
            IsBusyPageIndicator = false;
            IsLoadingDataIndicator = false;
            isLoadingMoreData = false;
        }


    }
}
