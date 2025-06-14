using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace TaskieLib
{
    public class FileTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string fileType)
            {
                return string.Format(ResourceLoader.GetForCurrentView().GetString("File"), fileType.ToUpper());
            }
            else return ResourceLoader.GetForCurrentView().GetString("UnknownFile");
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
