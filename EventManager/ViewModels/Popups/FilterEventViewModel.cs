using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;
using Mopups.Services;

namespace EventManager.ViewModels.Popups
{
    public partial class FilterEventViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly EventViewModel eventViewModel;

        [ObservableProperty]
        private string selectedCategory;

        [ObservableProperty]
        private string selectedOrder;

        public List<string> CategoryOptions { get; } = new() { "ALL", "Company Event", "Orientation", "Seminar", "Training" };
        public List<string> OrderOption { get; } = new() { "Latest", "Oldest" };
        public FilterEventViewModel(DatabaseService databaseService, EventViewModel eventViewModel, string lastCategory, string lastOrder) 
        {
            this.databaseService = databaseService;
            this.eventViewModel = eventViewModel;
            SelectedCategory = lastCategory;
            SelectedOrder = lastOrder;
        }
        [RelayCommand]
        private async Task ApplyFilter()
        {
            bool sortByLatest = SelectedOrder == "Latest";

            var filterModel = new EventFilter
            {
                Category = SelectedCategory,
                Order = sortByLatest
            };
            await MopupService.Instance.PopAsync();
            await eventViewModel.ApplyFilterEvents(filterModel);
        }
    }
}
