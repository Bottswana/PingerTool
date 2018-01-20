using System;
using System.Windows.Data;
using System.Globalization;
using System.Collections.ObjectModel;

namespace PingerTool.Converters
{
    public class CollectionToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if( value[0] is ObservableCollection<string> ArrayLines && ArrayLines.Count > 0 )
            {
                return string.Join("\n", ArrayLines);
            }
            else
            {
                return String.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}