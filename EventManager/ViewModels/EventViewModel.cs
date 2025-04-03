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
        private bool isAllEventsDataLoaded;
        private bool isLoading;

        [ObservableProperty] private ObservableCollection<Event> events = new();

        [ObservableProperty] private bool isNoDataVisible;
        [ObservableProperty] private bool isBusyPageIndicator;

        private bool isFiltering;
        [ObservableProperty] private string selectedCategory = "ALL";
        [ObservableProperty] private string selectedOrder = "Latest";

        private bool isSearching;
        [ObservableProperty]private string searchText; 

        public EventViewModel(DatabaseService databaseService, SqlServerService sqlServerService)
        {
            this.databaseService = databaseService;
            this.sqlServerService = sqlServerService;
        }

        [RelayCommand]
        private async Task OnNavigatedTo()
        {
            await ConditionalOnNavigated();
        }

        private async Task ConditionalOnNavigated()
        {
            if (sqlServerService.isSync)
            {
                await RefreshEvents();
                sqlServerService.isSync = false;
            }
            else
            {
                if (Events.Count == 0)
                {
                    await LoadEventsData();
                }
            }
        }

        [RelayCommand]
        public async Task LoadEventsData()
        {            
            if (isLoading || isAllEventsDataLoaded)
            {
                return;
            }
            else
            {
                IsBusyPageIndicator = Events.Count == 0;
                isLoading = true;

                await Task.Yield();

                List<Event> events;
                if (isSearching)
                {
                    events = await databaseService.SearchEvents(SearchText.Trim(), lastLoadedIndex, pageSize);
                }
                else if (isFiltering)
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

                IsNoDataVisible = Events.Count == 0;

                isLoading = false;
                IsBusyPageIndicator = false;
            }
        }
        [RelayCommand]
        public async Task RefreshEvents()
        { 
            SetSearchRefreshFilterData(string.Empty, false, 0, false, false);
            Events.Clear();
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
            SetSearchRefreshFilterData(string.Empty, false, 0, true, false);

            SelectedCategory = eventFilter.Category;
            SelectedOrder = eventFilter.Order ? "Latest" : "Oldest";

            Events.Clear();
            await LoadEventsData();
        }

        [RelayCommand]
        public async Task SearchEvents()
        {
            SetSearchRefreshFilterData(false, 0, false, !string.IsNullOrEmpty(SearchText.Trim()));
            Events.Clear();
            await LoadEventsData();
        }

        private void SetSearchRefreshFilterData(string searchText, bool isAllEventsDataLoaded, int lastLoadedIndex, bool isFiltering, bool isSearching)
        {
            SearchText = searchText;
            this.isAllEventsDataLoaded = isAllEventsDataLoaded;
            this.lastLoadedIndex = lastLoadedIndex;
            this.isFiltering = isFiltering;
            this.isSearching = isSearching;
        }

        private void SetSearchRefreshFilterData(bool isAllEventsDataLoaded, int lastLoadedIndex, bool isFiltering, bool isSearching)
        {
            this.isAllEventsDataLoaded = isAllEventsDataLoaded;
            this.lastLoadedIndex = lastLoadedIndex;
            this.isFiltering = isFiltering;
            this.isSearching = isSearching;
        }

    }
}
