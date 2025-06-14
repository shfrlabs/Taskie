using Humanizer;
using System;
using Windows.UI.Xaml.Data;

namespace TaskieLib
{
    public partial class RemindersTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType = null, object parameter = null, string language = null)
        {
            DateTimeOffset? reminders = value as DateTimeOffset?;
            if (reminders == null)
            {
                return string.Empty;
            }


            TimeSpan? timeDifference = reminders - DateTimeOffset.Now;

            TimeSpan diff = timeDifference ?? TimeSpan.Zero;

            return diff.Humanize(minUnit: Humanizer.Localisation.TimeUnit.Second);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}