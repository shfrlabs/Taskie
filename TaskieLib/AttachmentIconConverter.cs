using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace TaskieLib {
    public class AttachmentIconConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is string emoji && !string.IsNullOrEmpty(emoji)) {
                return emoji;
            }
            else {
                return "\uE8A5";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
