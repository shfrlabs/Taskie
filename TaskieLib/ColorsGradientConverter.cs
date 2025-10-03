using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace TaskieLib {
    public class ColorsGradientConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is null || !(value is IEnumerable<Windows.UI.Color> colors)) {
                return new SolidColorBrush(Windows.UI.Colors.Transparent);
            }
            else {
                LinearGradientBrush brush = new LinearGradientBrush() {
                    Opacity = 0.07,
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 0)
                };
                for (int i = 0; i < ((Windows.UI.Color[])value).Length; i++) {
                    var tag = ((Windows.UI.Color[])value)[i];
                    double offset = (((Windows.UI.Color[])value).Length == 1) ? 0.5 : (double)i / (((Windows.UI.Color[])value).Length - 1);
                    brush.GradientStops.Add(new GradientStop() {
                        Color = tag,
                        Offset = offset
                    });
                }
                return brush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException("ConvertBack is not implemented.");
        }
    }
}
