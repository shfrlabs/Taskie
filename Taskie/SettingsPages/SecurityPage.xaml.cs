using System;
using TaskieLib;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Taskie.SettingsPages
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
            if (availability != UserConsentVerifierAvailability.Available)
            {
                AuthToggle.IsOn = false;
                AuthToggle.IsEnabled = false;
                Settings.isAuthUsed = false;
            }
        }

        private void SetSettings()
        {
            if (Settings.isAuthUsed)
            { AuthToggle.IsOn = true; }
            else
            { AuthToggle.IsOn = false; }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch)?.Tag?.ToString() == "Auth")
            {
                Settings.isAuthUsed = (sender as ToggleSwitch).IsOn;
            }
        }
    }
}
