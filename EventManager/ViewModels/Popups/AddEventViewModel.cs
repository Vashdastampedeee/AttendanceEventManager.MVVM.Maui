using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EventManager.ViewModels.Popups
{
    public partial class AddEventViewModel : ObservableObject
    {
        private readonly IPopupService popupService;

        public AddEventViewModel(IPopupService popupServiceInjection)
        {
            popupService = popupServiceInjection;
        }
    }
}
