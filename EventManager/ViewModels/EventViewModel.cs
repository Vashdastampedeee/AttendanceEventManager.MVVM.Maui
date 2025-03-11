using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using EventManager.Models;
using EventManager.Services;
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

        [ObservableProperty]
        private ObservableCollection<Event> events = new();

        [ObservableProperty]
        private bool isBusyPageIndicator;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private bool isLoadingDataIndicator;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;
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
            async Task ExecuteAdd()
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

            await ExecuteAdd();
    
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

            var events = await databaseService.GetEventsPaginated(lastLoadedIndex, pageSize);

            if (events.Any())
            {
                await Task.Delay(1000);

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

            IsEnabled = true;
            IsBusyPageIndicator = false;
            IsLoadingDataIndicator = false;
            isLoadingMoreEvents = false;
        }
        [RelayCommand]
        public async Task RefreshEvents()
        {
            Debug.WriteLine("[EventViewModel] - refereshing events");

            lastLoadedIndex = 0;
            isAllEventsDataLoaded = false;
            Events.Clear();
            await LoadEventsData();
        }
        [RelayCommand]
        private async Task DeleteSelectedEvent(int eventId)
        {
            await databaseService.DeleteSelectedEvent(eventId);
            await RefreshEvents();
        }
        [RelayCommand]
        private async Task UseSelectedEvent(int eventId)
        {
            await databaseService.UseSelectedEvent(eventId);
            await RefreshEvents();
        }
    }
}
