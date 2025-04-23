using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TaskieLib {
    public partial class EmptyCollectionVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            int count = (int)value;
            return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return null;
        }
    }
}