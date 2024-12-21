using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;


namespace TaskieLib
{
    public class ReminderMenuFlyoutReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime? reminder = value as DateTime?;
            System.Diagnostics.Debug.WriteLine($"Reminder: {reminder?.ToString() ?? "null"} | Now: {DateTime.Now}");
            return (reminder != null && reminder > DateTime.Now) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}