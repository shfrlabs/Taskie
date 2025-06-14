using System;
using Windows.UI.Xaml.Data;

namespace TaskieLib
{
    public class ProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ValueTuple<int, int> tuple)
            {
                double subTaskTotal = tuple.Item1;
                double subTaskCompleted = tuple.Item2;
                return subTaskTotal != 0 ? ((subTaskCompleted / subTaskTotal) * 100) : 0;
            }
            throw new ArgumentException("Expected value to be a Tuple<int, int>, instead is " + value.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
