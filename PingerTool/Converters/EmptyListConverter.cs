using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace PingerTool.Converters
{
    public class EmptyListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check the converter is being used in the intended manner
            if( value.GetType() != typeof(int) || targetType != typeof(Visibility) )
                throw new ArgumentException("Converter only valid for a int to Visibility connversion");

            // Return visible if the length of the list is >= 1, otherwise return hidden
            return ( (int)value >= 1 ) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // We cant convert from Visibility back into a Collection, thats just not feasible or needed
            throw new NotImplementedException("Converter only valid for one-way conversion");
        }
    }
}