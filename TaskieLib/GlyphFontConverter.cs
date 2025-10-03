using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace TaskieLib {
    public class GlyphFontConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool isFairmark) {
                if (!isFairmark) {
                    return new FontFamily(new FontFamily("Segoe Fluent Icons") != null ? "Segoe Fluent Icons" : "Segoe MDL2 Assets");
                }
                else {
                    return new FontFamily("Segoe UI Emoji");
                }
            }
            else {
                return new FontFamily(new FontFamily("Segoe Fluent Icons") != null ? "Segoe Fluent Icons" : "Segoe MDL2 Assets");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
