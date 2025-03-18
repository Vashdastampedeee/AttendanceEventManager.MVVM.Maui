using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.Utilities;
using EventManager.ViewModels.Popups;
using EventManager.Views.Popups;
using Mopups.Services;

namespace EventManager.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
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

        public DashboardViewModel(DatabaseService databaseService) 
        {
            this.databaseService = databaseService;
          
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
                lastActiveEventId = activeEvent.Id;
                isAllDashboardDataLoaded = true;
            }
        }

        public async Task LoadDashboardData()
        {
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

            totalScannedEmployeePercentage = (PresentEmployees / TotalEmployees) * 100;

            presentEmployeeToString = PresentEmployees.ToString();
            totalEmployeeToString = TotalEmployees.ToString();
            totalScannedEmployeeToString = totalScannedEmployeePercentage.ToString("F2");

            TotalScannedEmployee = $"{presentEmployeeToString} / {totalEmployeeToString} - {totalScannedEmployeeToString}%";
          
            AbsentEmployees = TotalEmployees - PresentEmployees;

            IsVisible = PresentEmployees > 0;
        }

        private void ResetDashboardData()
        {
            IsVisible = false;
            TotalEmployees = 0;
            PresentEmployees = 0;
            AbsentEmployees = 0;
  
            presentEmployeeToString = "0";
            totalEmployeeToString = "0";
            totalScannedEmployeeToString = "0";
            TotalScannedEmployee = $"{presentEmployeeToString} / {totalEmployeeToString} - {totalScannedEmployeeToString}%";
        }

        [RelayCommand]
        public async Task FilterDashboard()
        {
            var filterDashboardViewModel = new FilterDashboardViewModel(this);
            var filterDashboard = new FilterDashboard(filterDashboardViewModel);
            var activeEvent = await databaseService.GetSelectedEvent();

            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
                return;
            }

            if (IsBusy)
            { 
                return; 
            }

            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            await Task.Delay(500);
            await MopupService.Instance.PushAsync(filterDashboard);
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }

        public async Task ApplyFilter(string businessUnit)
        {
            SelectedBusinessUnit = businessUnit;
            await LoadDashboardData();
        }
    }
}
