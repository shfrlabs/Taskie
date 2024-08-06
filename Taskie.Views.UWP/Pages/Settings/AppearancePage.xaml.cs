using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Taskie.Views.UWP.Services;

namespace Taskie.Views.UWP.Pages.Settings
{
    public sealed partial class AppearancePage : Page
    {
        private bool _isUpdating;

        public AppearancePage()
        {
            InitializeComponent();
            SetSettings();
        }

        private void SetSettings()
        {
            _isUpdating = true;

            switch (SettingsService.Instance.Theme)
            {
                case "System":
                    SystemRadio.IsChecked = true;
                    DarkRadio.IsChecked = false;
                    LightRadio.IsChecked = false;
                    break;
                case "Light":
                    SystemRadio.IsChecked = false;
                    DarkRadio.IsChecked = false;
                    LightRadio.IsChecked = true;
                    break;
                case "Dark":
                    SystemRadio.IsChecked = false;
                    DarkRadio.IsChecked = true;
                    LightRadio.IsChecked = false;
                    break;
            }

            _isUpdating = false;
        }

        private void RadioButton_StateChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdating)
                return;

            if (sender is not RadioButton radioButton)
                return;

            string selectedTheme = radioButton.Tag?.ToString();

            if (radioButton.IsChecked == true)
            {
                _isUpdating = true;
                SettingsService.Instance.Theme = selectedTheme;
                _isUpdating = false;
            }
        }

        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            await CoreApplication.RequestRestartAsync("Theme has been changed.");
        }
    }
}