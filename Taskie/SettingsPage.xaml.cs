using System;
using System.Collections.Generic;
using System.Linq;
using Taskie.SettingsPages;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Taskie
{
    public sealed partial class SettingsPage : Page
    {
        private bool isUpdating = false;

        public SettingsPage()
        {
            this.InitializeComponent();
            SetAppearance();
            ActualThemeChanged += SettingsPage_ActualThemeChanged;
        }

        private void SetAppearance()
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

        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await CoreApplication.RequestRestartAsync("Theme has been changed");
        }

        private void SettingsPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            (this.Background as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (this.Background as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        private class SettingCategory
        {
            public string Emoji { get; set; }
            public string Name { get; set; }
            public string Page { get; set; }
        }
    }
}