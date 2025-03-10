using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EventManager.ViewModels.Popups
{
    public partial class SyncDataViewModel : ObservableObject
    {
        [ObservableProperty]
        private string statusMessage = "Initializing...";

        [ObservableProperty]
        private bool isBusy;
        public bool IsNotBusy => !IsBusy;

        public async Task UpdateProgress(string message)
        {
            StatusMessage = message;
            await Task.Delay(100);
        }

    }
}
