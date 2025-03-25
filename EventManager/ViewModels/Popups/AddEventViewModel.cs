using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.Utilities;
using Microsoft.IdentityModel.Tokens;
using Mopups.Services;

namespace EventManager.ViewModels.Popups
{
    public partial class AddEventViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly EventViewModel eventViewModel;
        public List<string> CategoryOptions { get; } = new() {"Company Event", "Orientation", "Seminar", "Training" };

        [ObservableProperty] private string eventName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private DateTime eventDate = DateTime.Today;
        [ObservableProperty] private TimeSpan fromTime;
        [ObservableProperty] private TimeSpan toTime;
        [ObservableProperty] private ImageSource eventImagePreview = "event_image_placeholder.jpg";
        [ObservableProperty] private bool isToTimeEnabled;
        private byte[] eventImageData;
        public AddEventViewModel(DatabaseService databaseService, EventViewModel eventViewModel)
        {
            this.databaseService = databaseService;
            this.eventViewModel = eventViewModel;
        }

        [RelayCommand]
        private async Task SubmitEvent()
        {
            if (string.IsNullOrEmpty(EventName))
            {
                await ToastHelper.ShowToast("Event Name is required!", ToastDuration.Short);
                return;
            }
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                await ToastHelper.ShowToast("Event Category is required!", ToastDuration.Short);
                return;
            }
            if (FromTime == TimeSpan.Zero)
            {
                await ToastHelper.ShowToast("Event Time is required!", ToastDuration.Short);
                return;
            }

            string formattedEventDate = EventDate.ToString("MM/dd/yyyy");
            string formattedFromTime = DateTime.Today.Add(FromTime).ToString("hh:mm tt");
            string formattedToTime = DateTime.Today.Add(ToTime).ToString("hh:mm tt");

            bool isExistingEvent = await databaseService.IsExistingEvent(EventName.Trim(), SelectedCategory, formattedEventDate, formattedFromTime, formattedToTime);

            if (isExistingEvent)
            {
                await ToastHelper.ShowToast("Event with the same time already exists!", ToastDuration.Short);
                return;
            }
            else
            {
                await databaseService.InsertEvent(EventName.Trim(), SelectedCategory, eventImageData, formattedEventDate, formattedFromTime, formattedToTime);
                await MopupService.Instance.PopAsync();
                await eventViewModel.RefreshEvents();
                await ToastHelper.ShowToast("Event Added", ToastDuration.Short);
            }
        }

        [RelayCommand]
        private async Task UploadImage()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    eventImageData = memoryStream.ToArray();
                }
                else
                {
                    eventImageData = await File.ReadAllBytesAsync("event_image_placeholder.jpg");
                }

                EventImagePreview = ImageSource.FromStream(() => new MemoryStream(eventImageData));
            }
            catch (Exception ex)
            {
                await ToastHelper.ShowToast($"Upload failed! {ex.Message}", ToastDuration.Short);
            }
        }

        partial void OnFromTimeChanged(TimeSpan value)
        {
            if (value == TimeSpan.Zero)
            {
                IsToTimeEnabled = false;
            }
            else
            {
                IsToTimeEnabled = true;

                if (ToTime <= value)
                {
                    ToTime = value.Add(TimeSpan.FromHours(1));
                }
            }
        }

        partial void OnToTimeChanged(TimeSpan value)
        {
            if (value <= FromTime)
            {
                ToTime = FromTime.Add(TimeSpan.FromHours(1));
            }
        }

    }
}
