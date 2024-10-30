using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using ColorPicker = Windows.UI.Xaml.Controls.ColorPicker;

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
            DeterminePro();
            CheckOnboarding();
            ProFlyout();
            Navigation.Height = rectlist.ActualHeight;
            Tools.ListCreatedEvent += UpdateLists;
            Tools.ListDeletedEvent += ListDeleted;
            Tools.ListRenamedEvent += ListRenamed;
            Tools.AWOpenEvent += Tools_AWOpenEvent;
            Tools.AWClosedEvent += Tools_AWClosedEvent;
            ActualThemeChanged += MainPage_ActualThemeChanged;
        }

        private void ProFlyout()
        {
            if (!Settings.isPro && (new Random()).Next(1, 5) == 1)
            {
                protip.Target = UpdateButton;
                protip.IsOpen = true;
            }
        }

        private void CheckOnboarding()
        {
            if (!Settings.Launched)
            {
                tip1.Target = AddItemBtn;
                tip1.IsOpen = true;
            }
            Settings.Launched = true;
        }

        private void Tools_AWClosedEvent()
        {
            Navigation.Visibility = Visibility.Visible;
            Navigation.SelectedItem = null;
        }

        private void Tools_AWOpenEvent()
        {
            Navigation.Visibility = Visibility.Collapsed;
        }

        public void DeterminePro()
        {
            if (Settings.isPro)
            { 
                proText.Visibility = Visibility.Visible;
                BottomRow.Height = new GridLength(65);
                UpdateButton.Visibility = Visibility.Collapsed;
            }
            if (!Settings.isPro && Tools.GetLists().Count() > 2)
            {
                AddItemBtn.IsEnabled = false;
                NewListBtnIcon.Foreground = new SolidColorBrush(Colors.White);
                NewListBtnIcon.Opacity = 0.7;
                NewListBtnText.Foreground = new SolidColorBrush(Colors.White);
                NewListBtnText.Opacity = 0.7;
            }
            else
            {
                AddItemBtn.IsEnabled = true;
                NewListBtnIcon.Foreground = new SolidColorBrush(Colors.Black);
                NewListBtnText.Foreground = new SolidColorBrush(Colors.Black);
                NewListBtnText.Opacity = 1;
                NewListBtnIcon.Opacity = 1;
            }
        }

        public async void CheckSecurity()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if ((availability != UserConsentVerifierAvailability.Available && Settings.isAuthUsed) || (Settings.isAuthUsed && !Settings.isPro))
            {
                Settings.isAuthUsed = false;
                ContentDialog contentDialog = new ContentDialog() { Title = resourceLoader.GetString("AuthDisabledTitle"), Content = resourceLoader.GetString("AuthDisabledDescription"), PrimaryButtonText = "OK" };
                await contentDialog.ShowAsync();
            }
            else if (Settings.isAuthUsed)
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
                        content.Children.Add(new TextBlock() { Text = newname, Margin = new Thickness(12, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 2 });
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
            DeterminePro();
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
            foreach (string folderName in TaskieLib.Tools.GetFolders())
            {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(new FontIcon() { Glyph = "", FontSize = 14, Foreground = Tools.GetColorBrush(folderName) });
                content.Children.Add(new TextBlock { Text = folderName, Margin = new Thickness(12, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 2, Width = 80 });
                Navigation.Items.Add(new ListViewItem() { Tag = folderName, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
                AddRightClickMenu();
            }
            foreach (string listName in TaskieLib.Tools.GetLists())
            {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14 });
                content.Children.Add(new TextBlock { Text = listName, Margin = new Thickness(12, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 2, Width = 80 });
                Navigation.Items.Add(new ListViewItem() { Tag = listName, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
                AddRightClickMenu();
            }
            DeterminePro();
        }

        private void AddRightClickMenu()
        {
            foreach (ListViewItem item in Navigation.Items)
            {
                item.RightTapped += OpenRightClickList;
            }
        }

        private void OpenRightClickList(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();
            if (Tools.GetLists().Contains((sender as ListViewItem).Tag as string))
            {
                flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Rename), Text = resourceLoader.GetString("RenameList/Text"), Tag = (sender as ListViewItem).Tag });
                flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Delete), Text = resourceLoader.GetString("DeleteList/Text"), Tag = (sender as ListViewItem).Tag });
                flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Save), Text = resourceLoader.GetString("ExportList/Text"), Tag = (sender as ListViewItem).Tag });
                (flyout.Items[0] as MenuFlyoutItem).Click += RenameList_Click;
                (flyout.Items[1] as MenuFlyoutItem).Click += DeleteList_Click;
                (flyout.Items[2] as MenuFlyoutItem).Click += ExportList_Click;
            }
            else
            {
                flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Rename), Text = resourceLoader.GetString("RenameFolder/Text"), Tag = (sender as ListViewItem).Tag });
                flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Delete), Text = resourceLoader.GetString("DeleteFolder/Text"), Tag = (sender as ListViewItem).Tag });
                (flyout.Items[0] as MenuFlyoutItem).Click += RenameFolder_Click;
                (flyout.Items[1] as MenuFlyoutItem).Click += DeleteFolder_Click;
            }
            flyout.ShowAt(sender as ListViewItem);
        }

        private void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            Tools.DeleteFolder((sender as MenuFlyoutItem).Tag as string);
        }

        private async void RenameFolder_Click(object sender, RoutedEventArgs e)
        {
            string foldername = (sender as MenuFlyoutItem).Tag as string;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("FolderName"), Text = foldername };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameFolder/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                try
                {
                    Tools.RenameFolder(foldername, text);
                }
                catch (ArgumentException)
                {
                    tipwrongname.Target = Navigation;
                    tipwrongname.PreferredPlacement = TeachingTipPlacementMode.TopRight;
                    tipwrongname.IsOpen = true;
                }
            }
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
                    }
                });
            }
            catch { }
        }


        private async void RenameList_Click(object sender, RoutedEventArgs e)
        {
            string listname = (sender as MenuFlyoutItem).Tag as string;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("ListName"), Text = listname };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameList/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                try
                {
                    Tools.RenameList(listname, text);
                }
                catch (ArgumentException) {
                    tipwrongname.Target = Navigation;
                    tipwrongname.PreferredPlacement = TeachingTipPlacementMode.TopRight;
                    tipwrongname.IsOpen = true;
                }
                listname = text;
            }
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            string listname = (sender as MenuFlyoutItem).Tag as string;
            Tools.DeleteList(listname);
            DeterminePro();
        }

        private void UpdateLists(string name)
        {
            Navigation.Items.Clear();
            SetupNavigationMenu();
            contentFrame.Content = null;
            DeterminePro();
        }

        private void AddList(object sender, RoutedEventArgs e)
        {
            string listName = Tools.CreateList(resourceLoader.GetString("NewList"));
            UpdateLists(resourceLoader.GetString("NewList"));
            foreach (ListViewItem item in Navigation.Items)
            {
                if (item.Tag.ToString().Contains(listName))
                { Navigation.SelectedItem = item; break; }
            }
        }
        private string folderName = "";
        private async void AddFolder(object sender, RoutedEventArgs e) { 
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "New folder";
            dialog.PrimaryButtonText = "Create";
            dialog.IsPrimaryButtonEnabled = false;
            dialog.SecondaryButtonText = "Cancel";
            StackPanel stackPanel = new StackPanel();
            ColorPicker colorPicker = new ColorPicker();
            stackPanel.Children.Add(colorPicker);
            TextBox textBox = new TextBox() { PlaceholderText = "Folder name", Margin = new Thickness(0, 20, 0, 0) };
            textBox.TextChanged += (sender, e) =>
            {
                folderName = textBox.Text;
                if (!string.IsNullOrEmpty(folderName)) {
                    dialog.IsPrimaryButtonEnabled = true;
                }
                else
                {
                    dialog.IsPrimaryButtonEnabled = false;
                }
            };
            dialog.PrimaryButtonClick += (sender, e) =>
            {
                Tools.CreateFolder(folderName, colorPicker.Color);
            };
            stackPanel.Children.Add(textBox);
            stackPanel.Orientation = Orientation.Vertical;
            dialog.Content = stackPanel;
            await dialog.ShowAsync();
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
                if (Tools.GetLists().Contains(tag))
                {
                    contentFrame.Navigate(typeof(TaskPage), tag);
                }
                else
                {
                    Flyout flyout = new Flyout();
                    flyout.Content = new TextBlock() { Text = "hi girl im a FOLDER 💜" };
                    flyout.ShowAt(selectedItem);
                    flyout.Closed += (sender, e) =>
                    {
                        contentFrame.Content = null;
                        NavList.SelectedItem = null;
                    };
                }
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

        private async void Dialog_UpgradeAction(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // DEBUG UPGRADE OPTION! peri add store stuff here idk
            if (!Settings.isPro)
            {
                ToastContentBuilder builder = new ToastContentBuilder()
                    .AddText(resourceLoader.GetString("successfulUpgrade"))
                    .AddText(resourceLoader.GetString("successfulUpgradeSub"));
                builder.Show();
                Settings.isPro = true;
                await CoreApplication.RequestRestartAsync("Pro status changed.");
            }
        }

        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navigation.Height = rectlist.ActualHeight;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            { sender.IsSuggestionListOpen = false; sender.ItemsSource = new List<string>(); }
            else
            {
                sender.ItemsSource = Array.FindAll<string>(Tools.GetLists(), s => s.ToLower().Contains(sender.Text.ToLower()));
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(sender.Text))
            {
                try
                {
                    string foundItem = "";
                    foreach (ListViewItem item in Navigation.Items)
                    {
                        if (item.Tag.ToString().ToLower() == sender.Text.ToLower())
                        { Navigation.SelectedItem = item; foundItem = item.Tag.ToString(); break; }
                    }
                    if (string.IsNullOrEmpty(foundItem))
                    {
                        foreach (ListViewItem item in Navigation.Items)
                        {
                            if (item.Tag.ToString().ToLower().Contains(sender.Text.ToLower()))
                            { Navigation.SelectedItem = item; foundItem = item.Tag.ToString(); break; }
                        }
                    }
                    if (!string.IsNullOrEmpty(foundItem))
                    {
                        contentFrame.Navigate(typeof(TaskPage), foundItem);
                    }

                    sender.Text = "";
                    searchbox.ItemsSource = new List<string>();
                }
                catch { }
            }
        }

        internal int hovercount = 0;

        private async void proText_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            hovercount++;
            if (Settings.isPro && hovercount == 10)
            {
                hovercount = 0;
                ToastContentBuilder builder = new ToastContentBuilder()
                    .AddText(resourceLoader.GetString("ProCancelled"))
                    .AddText(resourceLoader.GetString("ProCancelledSub"));
                builder.Show();
                Settings.isPro = false;
                await CoreApplication.RequestRestartAsync("Pro status changed.");
            }
        }
    }
}
