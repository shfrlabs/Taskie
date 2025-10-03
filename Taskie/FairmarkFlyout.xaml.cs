using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TaskieLib.Models;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Taskie {
    public sealed partial class FairmarkFlyout : Flyout {
        public FairmarkFlyout() {
            this.InitializeComponent();
        }
        public FairmarkNoteData note;
        public AppServiceConnection _connection;
        private async void ListView_Loaded(object sender, RoutedEventArgs e) {
            (sender as ListView).Visibility = Visibility.Collapsed;
            TextBlock temp = new TextBlock() { Opacity = .7, Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Center };
            temp.Text = "~Loading...";
            panel.Children.Add(temp);

            if (_connection == null) {
                _connection = new AppServiceConnection();
                _connection.AppServiceName = "com.sheferslabs.fairmarkservices";
                _connection.PackageFamilyName = "BRStudios.3763783C2F5C2_ynj0a7qyfqv8c";

                var status = await _connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success) {
                   temp.Text = "~Failed to connect to service. Is Fairmark installed?";
                    _connection = null;
                    return;
                }
            }

            var response = await _connection.SendMessageAsync(new Windows.Foundation.Collections.ValueSet());

            if (response.Status == AppServiceResponseStatus.Success) {
                string resultString = response.Message["Result"] as string;
                Debug.WriteLine("Received: " + resultString);
                (sender as ListView).ItemsSource = System.Text.Json.JsonSerializer.Deserialize<List<TaskieLib.Models.FairmarkNoteData>>(resultString);
                if ((sender as ListView).Items.Count > 0) {
                    (sender as ListView).Visibility = Visibility.Visible;
                    panel.Children.Remove(temp);
                }
                else {
                    temp.Text = "~No notes found.";
                }
            }
            else {
                Debug.WriteLine("Failed to get a response from the service.");
            }
        }

        public FairmarkNoteData selectedNote {
            get { return note; }
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e) {
            this.Hide();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            note = (sender as ListView).SelectedItem as FairmarkNoteData;
            if (note != null) {
                AttachButton.IsEnabled = true;
            }
            else {
                AttachButton.IsEnabled = false;
            }
        }
    }
}
