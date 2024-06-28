using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TaskieLib;

namespace Taskie.SettingsPages
{
    public sealed partial class AppearancePage : Page
    {
        private bool isUpdating = false;

        public AppearancePage()
        {
            this.InitializeComponent();
            SetSettings();
        }

        private void SetSettings()
        {
            isUpdating = true;

            if (Settings.Theme == "System")
            {
                SystemRadio.IsChecked = true;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = false;
            }
            else if (Settings.Theme == "Light")
            {
                SystemRadio.IsChecked = false;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = true;
            }
            else if (Settings.Theme == "Dark")
            {
                SystemRadio.IsChecked = false;
                DarkRadio.IsChecked = true;
                LightRadio.IsChecked = false;
            }

            isUpdating = false;
        }

        private void RadioButton_StateChanged(object sender, RoutedEventArgs e)
        {
            if (isUpdating)
                return;

            var radioButton = sender as RadioButton;
            if (radioButton == null)
                return;

            string selectedTheme = radioButton.Tag?.ToString();

            if (radioButton.IsChecked == true)
            {
                isUpdating = true;
                Settings.Theme = selectedTheme;
                isUpdating = false;
            }
        }
    }
}
