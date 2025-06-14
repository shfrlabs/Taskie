using TaskieLib;
using Windows.UI.Xaml.Controls;

namespace Taskie
{
    public sealed partial class COClosePage : Page
    {
        public COClosePage()
        {
            this.InitializeComponent();
            ListTools.AWClosedEvent += Tools_AWClosedEvent;
        }

        private void Tools_AWClosedEvent()
        {
            this.Frame.Content = null;
        }
    }
}
