using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace EventManager.Utilities
{
    public static class ToastHelper
    {
        public static async Task ShowToast(string message, ToastDuration duration = ToastDuration.Short)
        {
            await Toast.Make(message, duration, 12).Show();
        }
    }
}
