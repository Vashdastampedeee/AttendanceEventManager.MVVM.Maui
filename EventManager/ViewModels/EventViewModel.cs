using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public EventViewModel(DatabaseService databaseServiceInjection, IPopupService popupServiceInjection) 
        {
            popupService = popupServiceInjection;
            databaseService = databaseServiceInjection;
        }

        [RelayCommand]
        public void AddEventPopup()
        {
            popupService.ShowPopup<AddEventViewModel>();
        }
    }
}
