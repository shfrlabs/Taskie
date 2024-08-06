using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Taskie.Services.Shared;
using Taskie.Views.UWP.Services;

namespace Taskie.Views.UWP.Pages.Settings
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

            var theme = SettingsService.Instance.Get<string>(SettingsKeys.Theme);

            if (theme == "System")
            {
                SystemRadio.IsChecked = true;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = false;
            }
            else if (theme == "Light")
            {
                SystemRadio.IsChecked = false;
                DarkRadio.IsChecked = false;
                LightRadio.IsChecked = true;
            }
            else if (theme == "Dark")
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
                SettingsService.Instance.Set(SettingsKeys.Theme, selectedTheme);
                isUpdating = false;
            }
        }

        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await CoreApplication.RequestRestartAsync("Theme has been changed");
        }
    }
}