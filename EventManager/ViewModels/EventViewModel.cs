using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.ViewModels.Popups;

namespace EventManager.ViewModels
{
    public partial class EventViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly IPopupService popupService;

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;
        public EventViewModel(DatabaseService databaseServiceInjection, IPopupService popupServiceInjection) 
        {
            popupService = popupServiceInjection;
            databaseService = databaseServiceInjection;
        }

        [RelayCommand]
        public async Task AddEventPopup()
        {
            if (IsBusy) return;
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));

            await Task.Delay(1500);

            popupService.ShowPopup<AddEventViewModel>();

            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }
}
