using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.Utilities;

namespace EventManager.ViewModels
{
    public partial class IndexViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly BeepService beepService;

        [ObservableProperty] private string idNumber;
        [ObservableProperty] private ImageSource idPhoto;
        [ObservableProperty] private string name;
        [ObservableProperty] private string businessUnit;
        [ObservableProperty] private string barcodeNumber;
        [ObservableProperty] private Color color;
        [ObservableProperty] private bool isEntryFocused;

        public IndexViewModel(DatabaseService databaseServiceInjection, BeepService beepServiceInjection)
        {
            databaseService = databaseServiceInjection;
            beepService = beepServiceInjection;
            InitializeElementProperty();
            Task.Run(async () => await beepService.InitBeep()); 
        }
        private void InitializeElementProperty()
        {
            Debug.WriteLine("[IndexViewModel] Intialize Property");
            IdNumber = "ID Number:";
            IdPhoto = "blank_id.png";
            Name = "Name:";
            BusinessUnit = "Business Unit:";
            BarcodeNumber = string.Empty;
            Color = Colors.Black;   
        }
        public async Task SetFocusEntry()
        {
            Debug.WriteLine("[IndexViewModel] Focus Set To Entry");
            IsEntryFocused = false;
            await Task.Delay(50);
            IsEntryFocused = true;
        }

        [RelayCommand]
        public async Task PageAppearing()
        {
            Debug.WriteLine("[IndexViewModel] Page Appearing");
            await SetFocusEntry();
        }

        [RelayCommand]
        public async Task ScanEmployeeId()
        {
            string barcodeIdNumber = BarcodeNumber.Trim();
            if(string.IsNullOrEmpty(barcodeIdNumber) || string.IsNullOrWhiteSpace(barcodeIdNumber))
            {
                return;
            }

            var scannedEmployee = await databaseService.GetEmployeeIdNumber(barcodeIdNumber);

            beepService.PlayBeep();

            if (scannedEmployee != null)
            {
                BarcodeNumber = string.Empty;
                IdNumber =$"ID Number: {scannedEmployee.IdNumber}";
                IdPhoto = ImageHelper.ConvertBytesToImage(scannedEmployee.IdPhoto, 130, 130);
                Name = $"Name {scannedEmployee.Name}";
                BusinessUnit = $"Business Unit: {scannedEmployee.BusinessUnit}";
                Color = Colors.Green;
            }
            else 
            {
                BarcodeNumber = string.Empty;
                IdNumber = $"ID Number: {barcodeIdNumber} Not Found";
                IdPhoto = "invalid.png";
                Name = "Name: Not Found";
                BusinessUnit = "Business Unit: Not Found";
                Color = Colors.Red;
            }

            await SetFocusEntry();
        }

    }
}
