using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Converters
{
    public class PlaceHolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var isDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (value is bool isEnabled)
            {
                if (isEnabled)
                {
                    return isDarkMode ? Colors.White : Colors.Black;
                }
                return Colors.Gray;
            }

            return Colors.Gray; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
