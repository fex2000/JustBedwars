
using JustBedwars.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace JustBedwars.Converters
{
    public class UsernameColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double score)
            {
                return new SolidColorBrush(ColorHelper.GetUsernameColor((int)Math.Round((double)score)));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
