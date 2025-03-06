using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using EventManager.Services;

namespace EventManager.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        public LogsViewModel(DatabaseService databaseServiceInjection) 
        {
            databaseService = databaseServiceInjection;
        }
    }
}
