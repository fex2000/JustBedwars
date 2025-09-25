
using JustBedwars.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace JustBedwars.Converters
{
    public class FkdrColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double fkdr)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(fkdr, StatType.FKDR));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class WlrColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double wlr)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(wlr, StatType.WLR));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class FinalsColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int finals)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(finals, StatType.Finals));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class WinsColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int wins)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(wins, StatType.Wins));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BblrColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double bblr)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(bblr, StatType.BBLR));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class KdrColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double kdr)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(kdr, StatType.KDR));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class StarsColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int star)
            {
                return new SolidColorBrush(ColorHelper.GetStatColor(star, StatType.Stars));
            }
            return new SolidColorBrush(ColorHelper.BLACK);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
