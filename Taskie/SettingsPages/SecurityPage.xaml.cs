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

namespace Taskie.SettingsPages
{
    public sealed partial class SecurityPage : Page
    {
        public SecurityPage()
        {
            this.InitializeComponent();
            SetSettings();
        }

        private void SetSettings()
        {
            if (Settings.isAuthUsed)
            { AuthToggle.IsOn = true; }
            else
            { AuthToggle.IsOn = false; }
            if (Settings.isDataEncrypted)
            { EncryptionToggle.IsOn = true; }
            else
            { EncryptionToggle.IsOn = false; }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch)?.Tag?.ToString() == "Auth")
            {
                Settings.isAuthUsed = (sender as ToggleSwitch).IsOn;
            }
            else if ((sender as ToggleSwitch)?.Tag?.ToString() == "Encryption")
            {
                Settings.isDataEncrypted = (sender as ToggleSwitch).IsOn;
            }
        }
    }
}
