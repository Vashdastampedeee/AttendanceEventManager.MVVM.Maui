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
using Microsoft.IdentityModel.Tokens;

namespace EventManager.ViewModels.Modals
{
    [QueryProperty(nameof(SelectedBusinessUnit), "SelectedBusinessUnit")]
    public partial class TotalScannedDataViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;

        private const int PageSize = 10;
        private int lastLoadedIndex = 0;
        private bool isLoading;
        private bool isAllDataLoaded;

        [ObservableProperty]
        private ObservableCollection<EmployeeAttendanceStatus> employeeAttendance = new();

        [ObservableProperty] private bool isBusyPageIndicator;

        [ObservableProperty] public string selectedBusinessUnit;
        private string isLastSelectedBuName;

        [ObservableProperty] public string searchText;
        private bool isSearching;

        [ObservableProperty] public bool isNoDataVisible;
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
                SearchText = string.Empty;
                await Task.Delay(100);
                await LoadEmployeeAttendanceStatus();
            }
        }

        [RelayCommand]
        public async Task LoadEmployeeAttendanceStatus()
        {
            if (isLoading || isAllDataLoaded)
            {
                return;
            }
            else
            {
                IsBusyPageIndicator = EmployeeAttendance.Count == 0;
                isLoading = true;

                await Task.Yield();

                List<EmployeeAttendanceStatus> employees;

                if (SelectedBusinessUnit == "ALL")
                {
                    if (isSearching)
                    {
                        employees = await databaseService.SearchTotalScannedData(SearchText.Trim(), lastLoadedIndex, PageSize);
                    }
                    else
                    {
                        employees = await databaseService.GetTotalScannedDataPaginated(lastLoadedIndex, PageSize);
                    }

                }
                else
                {
                    if (isSearching)
                    {
                        employees = await databaseService.SearchTotalScannedDataByBusinessUnit(SelectedBusinessUnit, SearchText.Trim(), lastLoadedIndex, PageSize);
                    }
                    else
                    {
                        employees = await databaseService.GetTotalScannedDataByBusinessUnitPaginated(SelectedBusinessUnit, lastLoadedIndex, PageSize);
                    }
                }

                if (employees.Count != 0)
                {
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
                isLoading = false;
                IsNoDataVisible = EmployeeAttendance.Count == 0;
            }
        }

        [RelayCommand]
        public async Task SearchData()
        {
            SetSearchAndRefreshData(!string.IsNullOrEmpty(SearchText.Trim()), false, 0);
            EmployeeAttendance.Clear();
            await LoadEmployeeAttendanceStatus();
        }
        [RelayCommand]
        public async Task RefreshData()
        {
            SearchText = string.Empty;
            SetSearchAndRefreshData(false, false, 0);
            EmployeeAttendance.Clear();
            await LoadEmployeeAttendanceStatus();
        }

        private void SetSearchAndRefreshData(bool isSearching, bool isAllDataLoaded, int lastLoadedIndex)
        {
            this.isSearching = isSearching;
            this.isAllDataLoaded = isAllDataLoaded;
            this.lastLoadedIndex = lastLoadedIndex;
        }


    }
}
