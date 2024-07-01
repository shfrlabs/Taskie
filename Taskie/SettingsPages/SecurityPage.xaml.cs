using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TaskieLib;
using Windows.Security.Credentials.UI;

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
