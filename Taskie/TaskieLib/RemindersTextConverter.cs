using Humanizer;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TaskieLib {
    public partial class RemindersTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            IReadOnlyList<DateTimeOffset> reminders = value as IReadOnlyList<DateTimeOffset>;
            if (reminders == null || reminders.Count == 0)
            {
                return string.Empty;
            }

            TimeSpan timeDifference = reminders[0] - DateTimeOffset.Now;

            return timeDifference.Humanize(minUnit: Humanizer.Localisation.TimeUnit.Minute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}