using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;

namespace EventManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        
        [ObservableProperty]
        private ObservableCollection<AttendanceLog> attendanceLogs = new();

        [ObservableProperty]
        private bool isBusyIndicator;

        [ObservableProperty]
        private bool isEnabled;

        public LogsViewModel(DatabaseService databaseServiceInjection) 
        {
            databaseService = databaseServiceInjection;
            LoadAttendanceLogs();
        }

        [RelayCommand]
        public async Task LoadAttendanceLogs()
        {
            IsEnabled = false;
            IsBusyIndicator = true;
            var logs = await databaseService.GetAllAttendanceLogs();
          

            if (logs != null)
            {
                AttendanceLogs.Clear();
                await Task.Delay(1000);
                foreach (var log in logs)
                {
                    AttendanceLogs.Add(log);
                }
            }
            IsEnabled = true;
            IsBusyIndicator = false;
        }

    }
}
