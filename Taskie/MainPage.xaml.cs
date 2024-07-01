using System;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using TaskieLib;
using Windows.Networking;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using System.Reflection;
using System.Linq;
using Windows.UI.WindowManagement;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Xaml.Hosting;
using System.Collections.Generic;
using Windows.Media.Protection.PlayReady;
using Windows.ApplicationModel.Resources;
using System.Security.Cryptography.X509Certificates;
using Windows.Security.Credentials.UI;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;

namespace Taskie
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(600, 500));
            InitializeComponent();
            SetupTitleBar();
            SetupNavigationMenu();
            CheckSecurity();
            Navigation.Height = rectlist.ActualHeight;
            Tools.ListCreatedEvent += UpdateLists;
            Tools.ListDeletedEvent += ListDeleted;
            Tools.ListRenamedEvent += ListRenamed;
            ActualThemeChanged += MainPage_ActualThemeChanged;
            if (Settings.isPro)
            { proText.Visibility = Visibility.Visible; }
        }

        public async void CheckSecurity()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available && Settings.isAuthUsed)
            {
                Settings.isAuthUsed = false;
                ContentDialog contentDialog = new ContentDialog() { Title = resourceLoader.GetString("AuthDisabledTitle"), Content = resourceLoader.GetString("AuthDisabledDescription"), PrimaryButtonText = "OK" };
                await contentDialog.ShowAsync();
            }
            if (Settings.isAuthUsed)
            {
                Navigation.Visibility = Visibility.Collapsed;
                UserConsentVerificationResult consent = await UserConsentVerifier.RequestVerificationAsync(resourceLoader.GetString("LoginMessage"));
                if (consent != UserConsentVerificationResult.Verified)
                {
                    Application.Current.Exit();
                }
                Navigation.Visibility = Visibility.Visible;
            }
        }


        private void MainPage_ActualThemeChanged(FrameworkElement sender = null, object args = null)
        {
            (rect1.Fill as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (rect1.Fill as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
            (rect2.Fill as SolidColorBrush).Color = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private void ListRenamed(string oldname, string newname)
        {
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString() == oldname)
                    {
                        navigationItem.Tag = newname;
                        StackPanel content = new StackPanel();
                        content.Orientation = Orientation.Horizontal;
                        content.VerticalAlignment = VerticalAlignment.Center;
                        content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14 });
                        content.Children.Add(new TextBlock() { Text = newname, Margin = new Thickness(12, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
                        navigationItem.Content = content;
                        break;
                    }
                }
            }
        }

        private void ListDeleted(string name)
        {
            contentFrame.Content = new StackPanel();
            Navigation.SelectedItem = null;
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString() == name)
                    {
                        Navigation.Items.Remove(item);
                        break;
                    }
                }
            }
        }

        private void SetupTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(TTB);
        }

        private void SetupNavigationMenu()
        {
            foreach (string listName in TaskieLib.Tools.GetLists())
            {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14 });
                content.Children.Add(new TextBlock { Text = listName, Margin = new Thickness(12, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis });
                Navigation.Items.Add(new ListViewItem() { Tag = listName, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
                AddRightClickMenu();
            }
        }

        private void AddRightClickMenu()
        {
            foreach (ListViewItem item in Navigation.Items) {
                item.RightTapped += OpenRightClickList;
            }
        }

        private void OpenRightClickList(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Rename), Text = resourceLoader.GetString("RenameList/Text"), Tag = (sender as ListViewItem).Tag });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Delete), Text = resourceLoader.GetString("DeleteList/Text"), Tag = (sender as ListViewItem).Tag });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Save), Text = resourceLoader.GetString("ExportList/Text"), Tag = (sender as ListViewItem).Tag });
            (flyout.Items[0] as MenuFlyoutItem).Click += RenameList_Click;
            (flyout.Items[1] as MenuFlyoutItem).Click += DeleteList_Click;
            (flyout.Items[2] as MenuFlyoutItem).Click += ExportList_Click;
            flyout.ShowAt(sender as ListViewItem);
        }

        private async void ExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    FileSavePicker savePicker = new FileSavePicker
                    {
                        DefaultFileExtension = ".json",
                        SuggestedStartLocation = PickerLocationId.Desktop,
                        SuggestedFileName = (sender as MenuFlyoutItem)?.Tag?.ToString() ?? string.Empty
                    };
                    savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        CachedFileManager.DeferUpdates(file);

                        string content = Tools.GetTaskFileContent((sender as MenuFlyoutItem)?.Tag?.ToString() ?? string.Empty);
                        await FileIO.WriteTextAsync(file, content);

                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                        if (status != FileUpdateStatus.Complete)
                        {
                            System.Diagnostics.Debug.WriteLine("Status: " + status);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Cancelled");
                    }
                });
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
        }


        private async void RenameList_Click(object sender, RoutedEventArgs e)
        {
            string listname = (sender as  MenuFlyoutItem).Tag as string;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("ListName"), Text = listname };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameList/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                Tools.RenameList(listname, text);
                listname = text;
            }
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            string listname = (sender as MenuFlyoutItem).Tag as string;
            Tools.DeleteList(listname);
        }

        private void UpdateLists(string name)
        {
            Navigation.Items.Clear();
            SetupNavigationMenu();
            contentFrame.Content = null;
        }

        private void AddList(object sender, RoutedEventArgs e)
        {
            string listName = Tools.CreateList(resourceLoader.GetString("NewList"));
            UpdateLists(resourceLoader.GetString("NewList"));
            foreach (ListViewItem item in Navigation.Items)
            {
                if (item.Tag.ToString().Contains(listName)) { Navigation.SelectedItem = item; break; }
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AppWindow window = await AppWindow.TryCreateAsync();
            window.Title = resourceLoader.GetString("SettingsText/Text");
            Frame settingsContent = new Frame();
            settingsContent.Navigate(typeof(SettingsPage));
            window.TitleBar.ExtendsContentIntoTitleBar = true;
            window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            ElementCompositionPreview.SetAppWindowContent(window, settingsContent);
            window.Closed += SettingsWindowClosed;
            (sender as Button).IsEnabled = false;
            if (Settings.Theme == "Dark")
            {
                window.TitleBar.ButtonForegroundColor = Windows.UI.Colors.White;
                window.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.White;

            }
            else if (Settings.Theme == "Light")
            {
                window.TitleBar.ButtonForegroundColor = Windows.UI.Colors.Black;
                window.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Black;
            }
            await window.TryShowAsync();
        }

        private void SettingsWindowClosed(AppWindow sender, AppWindowClosedEventArgs args)
        {
            SettingsButton.IsEnabled = true;
        }

        private void Navigation_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ListView NavList = sender as ListView;
            var selectedItem = NavList.SelectedItem as ListViewItem;
            if (selectedItem != null && selectedItem.Tag is string tag)
            {
                contentFrame.Navigate(typeof(TaskPage), tag);
            }
        }

        private async void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();
            Frame frame = new Frame();
            frame.Navigate(typeof(UpgradeDialogContentPage));
            dialog.Content = frame;
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonText = resourceLoader.GetString("UpgradeText/Text");
            dialog.PrimaryButtonClick += Dialog_UpgradeAction;
            dialog.SecondaryButtonText = resourceLoader.GetString("Cancel");
            await dialog.ShowAsync();
        }

        private void Dialog_UpgradeAction(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Flyout flyout = new Flyout();
            PasswordBox txtbox = new PasswordBox();
            txtbox.PasswordChanged += PasswordChanged;
            txtbox.PlaceholderText = "Enter super secret upgrade test password";
            flyout.Content = txtbox;
            flyout.ShowAt(TTB);
        }

        private async void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as PasswordBox).Password == "uwpcfreakyboykissers")
            {
                ToastContentBuilder builder = new ToastContentBuilder()
                    .AddText("You have entered the correct password.")
                    .AddText("Taskie Pro Preview has been unlocked.");
                builder.Show();
                Settings.isPro = true;
                await CoreApplication.RequestRestartAsync("Pro status changed.");
            }
            else if ((sender as PasswordBox).Password == "cancelprem")
            {
                ToastContentBuilder builder = new ToastContentBuilder()
                    .AddText("You have entered the cancel password.")
                    .AddText("Taskie Pro Preview has been relocked.");
                builder.Show();
                Settings.isPro = false;
                await CoreApplication.RequestRestartAsync("Pro status changed.");
            }
        }

        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navigation.Height = rectlist.ActualHeight;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            sender.ItemsSource = Array.FindAll<string>(Tools.GetLists(), s => s.Contains(sender.Text));
            if (sender.Text == null) { sender.IsSuggestionListOpen = false; }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            contentFrame.Navigate(typeof(TaskPage), Array.FindAll<string>(Tools.GetLists(), s => s.Contains(sender.Text))[0]);
            foreach (ListViewItem item in Navigation.Items)
            {
                Debug.WriteLine(item.Tag.ToString());
                Debug.WriteLine(sender.Text);
                if (item.Tag.ToString().Contains(sender.Text)) { Navigation.SelectedItem = item; break; }
            }
            sender.Text = "";
            searchbox.ItemsSource = null;

        }
    }
}
