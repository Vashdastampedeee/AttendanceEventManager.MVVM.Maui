using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Kotlin.Properties;
using Syncfusion.Maui.Buttons;

namespace EventManager.ViewModels
{
    public partial class OptionViewModel : ObservableObject
    {
        private const string ThemeKey = "AppTheme";

        [ObservableProperty]
        public ObservableCollection<SfSegmentItem> themeOption = new() 
        { 
        new SfSegmentItem() { Text = "System", ImageSource ="light_and_dark" },
        new SfSegmentItem() { Text = "Light", ImageSource ="light"},
        new SfSegmentItem() { Text = "Dark", ImageSource="dark" }
        };

        [ObservableProperty]
        private int selectedTheme = 0;

        public OptionViewModel()
        {
            SelectedTheme = Preferences.Get(ThemeKey, 0);
            ApplyTheme(SelectedTheme);
        }

        partial void OnSelectedThemeChanged(int value)
        {
            ApplyTheme(value);
            Preferences.Set(ThemeKey, value); 
        }

        private void ApplyTheme(int themeIndex)
        {
            switch (themeIndex)
            {
                case 0:
                    Application.Current.UserAppTheme = AppTheme.Unspecified;
                    break;
                case 1:
                    Application.Current.UserAppTheme = AppTheme.Light;
                    break;
                case 2:
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
            }
        }
    }
}
