using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;
using EventManager.Utilities;
using EventManager.ViewModels.Popups;
using EventManager.Views.Popups;
using Mopups.Services;

namespace EventManager.ViewModels
{
    public partial class EventViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;

        private const int pageSize = 5;
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreEvents;
        private bool isAllEventsDataLoaded;
        private bool isRefreshEvent;

        [ObservableProperty]
        private ObservableCollection<Event> events = new();

        [ObservableProperty]
        private bool isNoDataVisible;

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;
        [ObservableProperty]
        private bool isFiltering;
        [ObservableProperty]
        private string selectedCategory = "ALL";
        [ObservableProperty]
        private string selectedOrder = "Latest";
        [ObservableProperty]
        private string searchText; 
        [ObservableProperty]
        private bool isSearching;   
        public EventViewModel(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            if (Events.Count == 0)
            {
                await LoadEventsData();
            }
        }

        [RelayCommand]
        public async Task AddEventPopup()
        {
            var addEventViewModel = new AddEventViewModel(databaseService, this);
            var addEvent = new AddEvent(addEventViewModel);

            if (IsBusy) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));

            await Task.Delay(500);
            await MopupService.Instance.PushAsync(addEvent);

            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
        [RelayCommand]
        public async Task LoadEventsData()
        {            
            if (isLoadingMoreEvents || isAllEventsDataLoaded)
            {
                return;
            }

            isLoadingMoreEvents = true;
            IsEnabled = false;
            IsBusyPageIndicator = Events.Count == 0;
            IsLoadingDataIndicator = Events.Count > 0;

            List<Event> events;

            if (IsSearching)
            {
                events = await databaseService.SearchEvents(SearchText, lastLoadedIndex, pageSize);
            }
            else if (IsFiltering)
            {
                events = await databaseService.SetFilterEvents(SelectedCategory, SelectedOrder == "Latest", lastLoadedIndex, pageSize);
            }
            else
            {
                events = await databaseService.GetEventsPaginated(lastLoadedIndex, pageSize);
            }

            if (events.Any())
            {
                if(!isRefreshEvent)
                {
                    await Task.Delay(1000);
                }
   
                foreach (var eventData in events)
                {
                    eventData.IsDefaultVisible = eventData.isSelected;  
                    Events.Add(eventData);
                }

                lastLoadedIndex += events.Count();
            }

            if (events.Count < pageSize)
            {
                isAllEventsDataLoaded = true;
            }

            isRefreshEvent = false;
            IsEnabled = true;
            IsBusyPageIndicator = false;
            IsLoadingDataIndicator = false;
            isLoadingMoreEvents = false;
            IsNoDataVisible = Events.Count == 0;
        }
        [RelayCommand]
        public async Task RefreshEvents()
        {
            Debug.WriteLine("[EventViewModel] - refereshing events");

            IsNoDataVisible = false;
            SearchText = string.Empty; 
            IsSearching = false;
            IsFiltering = false;
            isAllEventsDataLoaded = false;
            lastLoadedIndex = 0;

            Events.Clear();
            await Task.Delay(100);
            await LoadEventsData();
        }
        [RelayCommand]
        private async Task EditSelectedEvent(int eventId)
        {
            var editEventViewModel = new EditEventViewModel(databaseService, this, eventId);
            var editEvent = new EditEvent(editEventViewModel);
            await MopupService.Instance.PushAsync(editEvent);
        }
        [RelayCommand]
        private async Task DeleteSelectedEvent(int eventId)
        {
            await databaseService.DeleteSelectedEvent(eventId);
            await Task.Delay(100);
            await RefreshEvents();
        }
        [RelayCommand]
        private async Task UseSelectedEvent(int eventId)
        {
            await databaseService.UseSelectedEvent(eventId);
            await Task.Delay(100);
            await RefreshEvents();
        }
        [RelayCommand]
        public async Task FilterEvents()
        {
            if (Events.Count == 0)
            {
                await ToastHelper.ShowToast("No data available for filter!", ToastDuration.Short);
                return;
            }
   
            var filterEventViewModel = new FilterEventViewModel(databaseService, this, SelectedCategory, SelectedOrder);
            var filterEvent = new FilterEvent(filterEventViewModel);
            await MopupService.Instance.PushAsync(filterEvent);
        }
        [RelayCommand]
        public async Task ApplyFilterEvents(EventFilter eventFilter)
        {
            isAllEventsDataLoaded = false; 
            lastLoadedIndex = 0; 
            IsFiltering = true;

            SelectedCategory = eventFilter.Category;
            SelectedOrder = eventFilter.Order ? "Latest" : "Oldest";

            Events.Clear();
            await Task.Delay(100);
            await LoadEventsData();
        }
        [RelayCommand]
        public async Task SearchEvents()
        {
            isAllEventsDataLoaded = false;
            lastLoadedIndex = 0; 
            IsSearching = !string.IsNullOrEmpty(SearchText); 

            Events.Clear();
            await LoadEventsData();
        }
    }
}
