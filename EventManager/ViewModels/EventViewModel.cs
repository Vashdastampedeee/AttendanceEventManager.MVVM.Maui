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
        private readonly SqlServerService sqlServerService;

        private const int pageSize = 5;
        private int lastLoadedIndex = 0;
        private bool isLoadingMoreEvents;
        private bool isAllEventsDataLoaded;

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
        public EventViewModel(DatabaseService databaseService, SqlServerService sqlServerService)
        {
            this.databaseService = databaseService;
            this.sqlServerService = sqlServerService;
        }

        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            if (sqlServerService.isSync)
            {
                await RefreshEvents();
                sqlServerService.isSync = false;
            }
            else
            {
                if(Events.Count == 0)
                {
                    await LoadEventsData();
                }
            }
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
                events = await databaseService.SearchEvents(SearchText.Trim(), lastLoadedIndex, pageSize);
            }
            else if (IsFiltering)
            {
                events = await databaseService.SetFilterEvents(SelectedCategory, SelectedOrder == "Latest", lastLoadedIndex, pageSize);
            }
            else
            {
                events = await databaseService.GetEventsPaginated(lastLoadedIndex, pageSize);
            }

            if (events.Count != 0)
            {
                var sortedEvents = events.OrderByDescending(e => e.isSelected).ToList();

                foreach (var eventData in sortedEvents)
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
        private async Task EventItemTapped(int eventId)
        {
            var selectedEvent = Events.FirstOrDefault(e => e.Id == eventId);
            if (selectedEvent == null ||selectedEvent.isSelected)
            {
                return;
            }
            else
            {
                bool useEvent = await Shell.Current.DisplayAlert($"{selectedEvent.EventName}", "Do you want to use this event?", "Use", "Cancel");
                if (useEvent)
                {
                    await databaseService.UseSelectedEvent(eventId);
                    await RefreshEvents();
                }
            }

        }
     
        [RelayCommand]
        public async Task FilterEvents()
        {
            var activeEvent = databaseService.GetSelectedEvent();
            if(activeEvent == null)
            {
                await ToastHelper.ShowToast("Set Event First!", ToastDuration.Short);
                return;
            }
            else
            {
                var filterEventViewModel = new FilterEventViewModel(databaseService, this, SelectedCategory, SelectedOrder);
                var filterEvent = new FilterEvent(filterEventViewModel);
                await MopupService.Instance.PushAsync(filterEvent);
            }
        }
        [RelayCommand]
        public async Task ApplyFilterEvents(EventFilter eventFilter)
        {
            isAllEventsDataLoaded = false; 
            lastLoadedIndex = 0;
            SearchText = string.Empty;
            IsFiltering = true;
            IsSearching = false;

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
            IsFiltering = false;
            IsSearching = !string.IsNullOrEmpty(SearchText.Trim()); 

            Events.Clear();
            await Task.Delay(100);
            await LoadEventsData();
        }
    }
}
