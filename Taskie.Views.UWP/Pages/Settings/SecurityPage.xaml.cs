using System;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Taskie.Services.Shared;
using Taskie.Views.UWP.Services;

namespace Taskie.Views.UWP.Pages.Settings
{
    public sealed partial class SecurityPage : Page
    {
        public SecurityPage()
        {
            this.InitializeComponent();
            SetSettings();
            CheckSecurity();
        }

        private async void CheckSecurity()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();

            var isPro = SettingsService.Instance.Get<bool>(SettingsKeys.IsPro);

            if (!(availability != UserConsentVerifierAvailability.Available | !isPro)) return;

            AuthToggle.IsOn = false;
            AuthToggle.IsEnabled = false;
            SettingsService.Instance.Set(SettingsKeys.IsAuthUsed, false);
        }

        private void SetSettings()
        {
            AuthToggle.IsOn = SettingsService.Instance.Get<bool>(SettingsKeys.IsAuthUsed);
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch && toggleSwitch.Tag.ToString() == "Auth")
            {
                SettingsService.Instance.Set(SettingsKeys.IsAuthUsed, toggleSwitch.IsOn);
            }
        }
    }
}