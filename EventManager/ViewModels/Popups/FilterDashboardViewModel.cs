using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Services;

namespace EventManager.ViewModels.Popups
{
    public partial class FilterDashboardViewModel : ObservableObject
    {
        private readonly DashboardViewModel dashboardViewModel;

        [ObservableProperty]
        private string selectedBusinessUnit;

        public List<string> BusinessUnitOption { get; } = new() { "ALL", "BAG", "HLB", "JLINE", "RAWLINGS", "SUPPORT GROUP" };
        public FilterDashboardViewModel(DashboardViewModel dashboardViewModel) 
        {
            this.dashboardViewModel = dashboardViewModel;
            SelectedBusinessUnit = dashboardViewModel.SelectedBusinessUnit; 
        }

        [RelayCommand]
        public async Task ApplyFilter()
        {
            await MopupService.Instance.PopAsync();
            await dashboardViewModel.ApplyFilter(SelectedBusinessUnit);
        }


    }
}
