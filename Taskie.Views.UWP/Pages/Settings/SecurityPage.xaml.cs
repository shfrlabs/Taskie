using System;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
            
            if (!(availability != UserConsentVerifierAvailability.Available | !SettingsService.Instance.IsPro)) return;

            AuthToggle.IsOn = false;
            AuthToggle.IsEnabled = false;
            SettingsService.Instance.AuthUsed = false;
        }

        private void SetSettings()
        {
            AuthToggle.IsOn = SettingsService.Instance.AuthUsed;
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch && toggleSwitch.Tag.ToString() == "Auth")
            {
                SettingsService.Instance.AuthUsed = toggleSwitch.IsOn;
            }
        }
    }
}