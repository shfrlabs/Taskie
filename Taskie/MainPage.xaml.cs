using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Taskie {
    public sealed partial class MainPage : Page {
        public MainPage() {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(500, 600));
            InitializeComponent();
            SetupTitleBar();
            CheckSecurity();
            DeterminePro();
            CheckOnboarding();

            Navigation.Height = rectlist.ActualHeight;

            ListTools.ListCreatedEvent += UpdateLists;
            ListTools.ListDeletedEvent += ListDeleted;
            ListTools.ListRenamedEvent += ListRenamed;
            ListTools.ListEmojiChangedEvent += ListEmojiChanged;
            ListTools.AWOpenEvent += Tools_AWOpenEvent;
            ListTools.AWClosedEvent += Tools_AWClosedEvent;
            ActualThemeChanged += MainPage_ActualThemeChanged;

            contentFrame.Navigate(typeof(EmptyPage));

            if (App.Current is App app && !string.IsNullOrEmpty(app.ToastActivationArgument)) {
                HandleToastActivation(app.ToastActivationArgument);
            }
        }

        #region Click handlers

        private void DeleteList_Click(object sender, RoutedEventArgs e) {
            string? listname = ((MenuFlyoutItem)sender).Tag as string;
            ListTools.DeleteList(listname?.Replace(".json", null));
            ListDeleted(((MenuFlyoutItem)sender).Tag.ToString().Replace(".json", null));
            DeterminePro();
        }

        private async void ExportList_Click(object sender, RoutedEventArgs e) {
            try {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                    FileSavePicker savePicker = new FileSavePicker {
                        DefaultFileExtension = ".json",
                        SuggestedStartLocation = PickerLocationId.Desktop,
                        SuggestedFileName = ((MenuFlyoutItem)sender)?.Tag?.ToString() ?? string.Empty
                    };
                    savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null) {
                        CachedFileManager.DeferUpdates(file);

                        string content = ListTools.GetTaskFileContent((sender as MenuFlyoutItem).Tag.ToString());
                        await FileIO.WriteTextAsync(file, content);

                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    }
                });
            }
            catch (Exception ex) { Debug.WriteLine("[List export] Exception occured: " + ex.Message); }
        }

        private async void RenameList_Click(object sender, RoutedEventArgs e) {
            ListMetadata data = ListTools.ReadList((((MenuFlyoutItem)sender).Tag as string).Replace(".json", null)).Metadata;
            string listname = data.Name;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("ListName"), Text = listname };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameList/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input, DefaultButton = ContentDialogButton.Primary };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                string text = input.Text;
                try {
                    if (string.IsNullOrEmpty(text)) {
                        throw new Exception("No list name");
                    }
                    else {
                        ListTools.RenameList((((MenuFlyoutItem)sender).Tag as string).Replace(".json", null), text);
                    }
                }
                catch {
                    tipwrongname.Target = Navigation;
                    tipwrongname.PreferredPlacement = TeachingTipPlacementMode.TopRight;
                    tipwrongname.IsOpen = true;
                }
                listname = text;
                ListRenamed((((MenuFlyoutItem)sender).Tag as string).Replace(".json", null), text, data.Emoji);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) {
            contentFrame.Navigate(typeof(SettingsPage));
            Navigation.SelectedItem = null;
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e) {
            if (mainGrid.ColumnDefinitions[0].Width == new GridLength(0)) {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(200);
                rect2.Visibility = Visibility.Visible;
                contentFrame.Margin = new Thickness(-20, 0, 0, 0);
                SidebarButton.Opacity = 1;
            }
            else {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                rect2.Visibility = Visibility.Collapsed;
                contentFrame.Margin = new Thickness(0, 10, 0, 0);
                SidebarButton.Opacity = 0.7;
            }
        }

        private async void UpgradeButton_Click(object sender, RoutedEventArgs e) {
            ContentDialog dialog = new ContentDialog();
            Frame frame = new Frame();
            dialog.BorderBrush = Application.Current.Resources["ProBG"] as LinearGradientBrush;
            dialog.BorderThickness = new Thickness(2);
            frame.Navigate(typeof(UpgradeDialogContentPage));
            dialog.Content = frame;
            dialog.PrimaryButtonText = string.Format(resourceLoader.GetString("UpgradeFor"), await Settings.GetProPriceAsync());
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonClick += Dialog_UpgradeAction;
            dialog.SecondaryButtonText = resourceLoader.GetString("Cancel");
            await dialog.ShowAsync();
        }
        #endregion

        #region Startup methods, loaded events, activation handlers


#if DEBUG
        public bool shouldShowOOBE = false;
#else
                        public bool shouldShowOOBE = true;
#endif
        private async void CheckOnboarding() {
            if (!Settings.Launched && shouldShowOOBE) {
                ContentDialog dialog = new ContentDialog();
                Frame frame = new Frame();
                dialog.BorderBrush = Application.Current.Resources["ProBG"] as LinearGradientBrush;
                dialog.BorderThickness = new Thickness(2);
                frame.Navigate(typeof(OOBEDialogPage));
                dialog.Content = frame;
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.PrimaryButtonText = resourceLoader.GetString("GetStarted");
                dialog.PrimaryButtonClick += (sender, args) => { };
                dialog.SecondaryButtonText = resourceLoader.GetString("Exit");
                dialog.SecondaryButtonClick += (sender, args) => { Application.Current.Exit(); };
                await dialog.ShowAsync();
            }
            Settings.Launched = true;
        }

        public async void CheckSecurity() {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if ((availability != UserConsentVerifierAvailability.Available && Settings.isAuthUsed) || Settings.isAuthUsed) {
                Settings.isAuthUsed = false;
                ContentDialog contentDialog = new ContentDialog() { Title = resourceLoader.GetString("AuthDisabledTitle"), Content = resourceLoader.GetString("AuthDisabledDescription"), PrimaryButtonText = "OK", DefaultButton = ContentDialogButton.Primary };
                await contentDialog.ShowAsync();
            }
            else if (Settings.isAuthUsed) {
                Navigation.Visibility = Visibility.Collapsed;
                UserConsentVerificationResult consent = await UserConsentVerifier.RequestVerificationAsync(resourceLoader.GetString("LoginMessage"));
                if (consent != UserConsentVerificationResult.Verified) {
                    Application.Current.Exit();
                }
                Navigation.Visibility = Visibility.Visible;
            }
        }

        public async void DeterminePro() // Locks down features for free users.
        {
            if (await Settings.CheckIfProAsync()) {
                proText.Text = "PRO";
                BottomRow.Height = new GridLength(62);
                UpdateButton.Visibility = Visibility.Collapsed;
            }
            else {
                proText.Text = "FREE";
            }
        }

        public void HandleToastActivation(string argument) {
            // Extract the task ID from the argument
            var listId = argument.Split('=')[1];
            System.Diagnostics.Debug.WriteLine($"Toast notification activated. List ID: {listId}");

            // Navigate to the specific task page
            contentFrame.Navigate(typeof(TaskPage), listId);
            foreach (ListViewItem item in Navigation.Items) {
                if (item.Tag.ToString().Replace(".json", null) == listId) {
                    Navigation.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetupTitleBar() {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(TTB);
        }

        private void SetupNavigationMenu() {
            foreach ((string listName, string listID, string listEmoji) in TaskieLib.ListTools.GetLists()) {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(new FontIcon() { Glyph = listEmoji ?? "📋", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 13 });
                content.Children.Add(new TextBlock { Text = listName, Margin = new Thickness(10, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1, Width = 100, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
                Navigation.Items.Add(new ListViewItem() { Tag = listID, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
                AddRightClickMenu(Navigation.Items.Last() as ListViewItem);
            }
            DeterminePro();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e) {
            LoadRing.IsActive = false;
            LoadRing.Visibility = Visibility.Collapsed;
            SetupNavigationMenu();
        }

        #endregion

        #region Taskie Mini events
        private void Tools_AWClosedEvent() // Executes when Taskie Mini is closed.
        {
            Navigation.IsEnabled = true;
            Navigation.SelectedItem = null;
            AddItemBtn.IsEnabled = true;
            searchbox.IsEnabled = true;
            NewListBtnIcon.Foreground = new SolidColorBrush(Colors.Black);
            NewListBtnText.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void Tools_AWOpenEvent() // Executes when Taskie Mini is opened.
        {
            AddItemBtn.IsEnabled = false;
            searchbox.IsEnabled = false;
            Navigation.IsEnabled = false;
            NewListBtnIcon.Foreground = (Brush)Resources["ButtonDisabledForegroundThemeBrush"];
            NewListBtnText.Foreground = (Brush)Resources["ButtonDisabledForegroundThemeBrush"];
        }
        #endregion

        #region List-related events
        private void ListRenamed(string listID, string newname, string emoji) {
            Debug.WriteLine("List got renamed:" + listID);
            foreach (var item in Navigation.Items) {
                if (item is ListViewItem navigationItem) {
                    if (navigationItem.Tag.ToString().Replace(".json", null) == listID) {
                        StackPanel content = new StackPanel();
                        content.Orientation = Orientation.Horizontal;
                        content.VerticalAlignment = VerticalAlignment.Center;
                        content.Children.Add(new FontIcon() { Glyph = emoji ?? "📋", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 13 });
                        content.Children.Add(new TextBlock { Text = newname, Margin = new Thickness(10, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1, Width = 100, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
                        navigationItem.Content = content;
                        break;
                    }
                }
            }
        }

        private void ListEmojiChanged(string listID, string name, string emoji) {
            Debug.WriteLine("List emoji changed:" + listID);
            foreach (var item in Navigation.Items) {
                if (item is ListViewItem navigationItem) {
                    if (navigationItem.Tag.ToString().Replace(".json", null) == listID) {
                        StackPanel content = new StackPanel();
                        content.Orientation = Orientation.Horizontal;
                        content.VerticalAlignment = VerticalAlignment.Center;
                        content.Children.Add(new FontIcon() { Glyph = emoji ?? "📋", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 13 });
                        content.Children.Add(new TextBlock { Text = name, Margin = new Thickness(10, 0, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis, MaxLines = 1, Width = 100, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
                        navigationItem.Content = content;
                        break;
                    }
                }
            }
        }

        private void ListDeleted(string? listID) {
            Debug.WriteLine("List deleted: " + listID);
            contentFrame.Navigate(typeof(EmptyPage));
            Navigation.SelectedItem = null;
            foreach (var item in Navigation.Items) {
                if (item is ListViewItem navigationItem) {
                    if (navigationItem.Tag.ToString()?.Replace(".json", null) == listID) {
                        Navigation.Items.Remove(item);
                        break;
                    }
                }
            }
            DeterminePro();
        }

        #endregion

        #region Right click handlers
        private void AddRightClickMenu(ListViewItem item) {
            item.RightTapped += OpenRightClickList;
        }

        private void OpenRightClickList(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e) {
            MenuFlyout flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Rename), Text = resourceLoader.GetString("RenameList/Text"), Tag = (sender as ListViewItem).Tag.ToString().Replace(".json", "") });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Save), Text = resourceLoader.GetString("ExportList/Text"), Tag = (sender as ListViewItem).Tag.ToString().Replace(".json", "") });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Delete), Text = resourceLoader.GetString("DeleteList/Text"), Tag = (sender as ListViewItem).Tag.ToString().Replace(".json", "") });
            (flyout.Items[0] as MenuFlyoutItem).Click += RenameList_Click;
            (flyout.Items[1] as MenuFlyoutItem).Click += ExportList_Click;
            (flyout.Items[2] as MenuFlyoutItem).Click += DeleteList_Click;
            flyout.ShowAt(sender as ListViewItem);
        }
        #endregion

        #region Other events

        private void ListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) { // opens a list
            ListView NavList = sender as ListView;
            var selectedItem = NavList.SelectedItem as ListViewItem;
            if (selectedItem != null && selectedItem.Tag is string tag) {
                contentFrame.Navigate(typeof(TaskPage), tag.Replace(".json", null));
            }
        }


        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e) {
            Navigation.Height = rectlist.ActualHeight;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
            if (string.IsNullOrWhiteSpace(sender.Text)) { sender.IsSuggestionListOpen = false; sender.ItemsSource = new List<string>(); }
            else {
                sender.ItemsSource = Array.FindAll<(string, string, string)>(ListTools.GetLists(), s => s.Item1.ToLower().Contains(sender.Text.ToLower())).Select(t => t.Item1).ToArray();
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
            sender.IsSuggestionListOpen = true;
        }

        private void searchbox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) {
            try {
                string foundItem = "";
                foreach ((string name, string id, string emoji) item in ListTools.GetLists()) {
                    if (args.SelectedItem.ToString() == item.name) {
                        foundItem = item.id.Replace(".json", null);
                    }
                }
                if (!string.IsNullOrEmpty(foundItem)) {
                    contentFrame.Navigate(typeof(TaskPage), foundItem.Replace(".json", null));
                    foreach (ListViewItem item in Navigation.Items) {
                        if (item.Tag.ToString().Replace(".json", null) == foundItem) {
                            Navigation.SelectedItem = item;
                        }
                    }
                }

                sender.Text = "";
                searchbox.ItemsSource = new List<string>();
            }
            catch (Exception ex) { Debug.WriteLine("[Search box suggestion chooser] Exception occured: " + ex.Message); }
        }

        #endregion

        private void MainPage_ActualThemeChanged(FrameworkElement sender = null, object args = null) {
            (rect1.Fill as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (rect1.Fill as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
            (rect2.Fill as SolidColorBrush).Color = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private void UpdateLists(string listID = null, string listName = null) {
            Navigation.Items.Clear();
            SetupNavigationMenu();
            contentFrame.Navigate(typeof(EmptyPage));
            DeterminePro();
        } // Resets the main view

        private async void AddList(object sender, RoutedEventArgs e) {
            ContentDialog dialog = new ContentDialog();
            TextBox box = new TextBox() { VerticalContentAlignment = VerticalAlignment.Bottom, MaxWidth = 300, BorderThickness = new Thickness(0), PlaceholderText = resourceLoader.GetString("NewList"), Padding = new Thickness(9, 9, 4, 4), FontSize = 15, CornerRadius = new CornerRadius(4) };
            Button emojiButton = new Button() { Content = "📋", Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Width = 40, Height = 40, FontSize = 20 };
            var flyout = new Flyout();
            var gridView = new GridView();

            gridView.ItemsPanel = (ItemsPanelTemplate)Application.Current.Resources["WrapGridPanel"];
            gridView.ItemTemplate = (DataTemplate)Application.Current.Resources["EmojiBlock"];
            gridView.ItemsSource = new Tools.IncrementalEmojiSource(Tools.GetSystemEmojis());
            gridView.SelectionMode = ListViewSelectionMode.Single;
            gridView.SelectionChanged += (s, args) => {
                emojiButton.Content = gridView.SelectedItem;
                flyout.Hide();
            };

            flyout.Content = gridView;
            flyout.Placement = FlyoutPlacementMode.Bottom;
            emojiButton.Click += (sender, args) => { flyout.ShowAt(emojiButton); };
            Grid panel = new Grid() {
                Margin = new Thickness(0, 10, 0, 0),
                Children = { emojiButton, box },
                ColumnDefinitions = { new ColumnDefinition() { Width = new GridLength(47) }, new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) } },
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(box, 1);
            string listName = resourceLoader.GetString("NewList");
            dialog.Content = panel;
            dialog.Title = resourceLoader.GetString("CreateListHeader");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.SecondaryButtonText = resourceLoader.GetString("Cancel");
            dialog.PrimaryButtonText = "OK";
            dialog.PrimaryButtonClick += (sender, args) => {
                if (string.IsNullOrEmpty(box.Text)) {
                    listName = ListTools.CreateList(resourceLoader.GetString("NewList"), null, emojiButton.Content.ToString());
                }
                else {
                    listName = ListTools.CreateList(box.Text, null, emojiButton.Content.ToString());
                }
                UpdateLists();
                foreach (ListViewItem item in Navigation.Items) {
                    if (!string.IsNullOrEmpty(listName) && item.Tag.ToString().Contains(listName)) { Navigation.SelectedItem = item; break; }
                }
            };
            await dialog.ShowAsync();
        }


        private async void Dialog_UpgradeAction(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            if (!await Settings.CheckIfProAsync()) {
                try {
                    ProductPurchaseStatus result = (await CurrentApp.RequestProductPurchaseAsync("ProLifetime")).Status;
                    if (result.HasFlag(ProductPurchaseStatus.Succeeded) || result.HasFlag(ProductPurchaseStatus.AlreadyPurchased)) {
                        var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                        var stringElements = toastXml.GetElementsByTagName("text");
                        stringElements[0].AppendChild(toastXml.CreateTextNode(resourceLoader.GetString("successfulUpgrade")));
                        stringElements[1].AppendChild(toastXml.CreateTextNode(resourceLoader.GetString("successfulUpgradeSub")));

                        // Add arguments to the toast notification
                        var toastElement = (XmlElement)toastXml.SelectSingleNode("/toast");

                        var toast = new ScheduledToastNotification(toastXml, DateTimeOffset.Now.AddSeconds(1)) {
                            Id = "proUpgrade"
                        };

                        ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
                        await CoreApplication.RequestRestartAsync("Pro status changed.");
                    }
                }
                catch { }
            }
        }

    }
}
