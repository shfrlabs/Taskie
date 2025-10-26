using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using TaskieLib;
using Windows.ApplicationModel.AppService;
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

namespace Taskie
{
    public sealed partial class MainPage : Page
    {
        public AppServiceConnection _connection;
        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(500, 600));
            InitializeComponent();
            SetupTitleBar();
            CheckSecurity();
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

            if (App.Current is App app && !string.IsNullOrEmpty(app.ToastActivationArgument))
            {
                HandleToastActivation(app.ToastActivationArgument);
            }
        }
        public GridLength sidebarSize
        {
            get => Settings.SidebarSize;
            set
            {
                Settings.SidebarSize = value;
            }
        }
        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            var listId = ((MenuFlyoutItem)sender).Tag?.ToString()?.Replace(".json", "");
            if (!string.IsNullOrEmpty(listId))
            {
                ListTools.DeleteList(listId);
                ListDeleted(listId);
                try
                {
                    Tools.RemoveAttachmentsFromList(listId);
                }
                catch { }
            }
        }

        private async void ExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string tag)
                    {
                        FileSavePicker savePicker = new FileSavePicker
                        {
                            DefaultFileExtension = ".json",
                            SuggestedStartLocation = PickerLocationId.Desktop,
                            SuggestedFileName = tag
                        };
                        savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

                        StorageFile file = await savePicker.PickSaveFileAsync();
                        if (file != null)
                        {
                            CachedFileManager.DeferUpdates(file);

                            string content = ListTools.GetTaskFileContent(tag);
                            if (!string.IsNullOrEmpty(content))
                            {
                                await FileIO.WriteTextAsync(file, content);

                                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                                if (status != FileUpdateStatus.Complete)
                                {
                                    Debug.WriteLine("[List export] File update was not completed successfully.");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("[List export] Task file content is null or empty.");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[List export] Invalid sender or missing Tag.");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[List export] Exception occurred: " + ex.Message);
            }
        }

        private async void RenameList_Click(object sender, RoutedEventArgs e)
        {
            ListMetadata data = ListTools.ReadList(((string)((MenuFlyoutItem)sender).Tag).Replace(".json", null)).Metadata;
            string listname = data.Name;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("ListName"), Text = listname };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameList/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input, DefaultButton = ContentDialogButton.Primary };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                try
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        throw new Exception("No list name");
                    }
                    else
                    {
                        ListTools.RenameList(((string)((MenuFlyoutItem)sender).Tag as string).Replace(".json", null), text);
                    }
                }
                catch
                {
                    tipwrongname.Target = Navigation;
                    tipwrongname.PreferredPlacement = TeachingTipPlacementMode.TopRight;
                    tipwrongname.IsOpen = true;
                }
                listname = text;
                ListRenamed(((string)((MenuFlyoutItem)sender).Tag).Replace(".json", null), text, data.Emoji);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(typeof(SettingsPage));
            Navigation.SelectedItem = null;
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainGrid.ColumnDefinitions[0].Width == new GridLength(0))
            {
                mainGrid.ColumnDefinitions[0].MinWidth = 185;
                mainGrid.ColumnDefinitions[0].Width = new GridLength(185);
                rect2.Visibility = Visibility.Visible;
                contentFrame.Margin = new Thickness(-20, 0, 0, 0);
                SidebarButton.Opacity = 1;
                sb.Visibility = Visibility.Visible;
                Splitter.Visibility = Visibility.Visible;
                SidebarTop.SetBinding(FrameworkElement.WidthProperty, new Windows.UI.Xaml.Data.Binding
                {
                    Path = new PropertyPath("ActualWidth"),
                    ElementName = "Sizer1",
                    Mode = Windows.UI.Xaml.Data.BindingMode.OneWay
                });
            }
            else
            {
                mainGrid.ColumnDefinitions[0].MinWidth = 0;
                mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                rect2.Visibility = Visibility.Collapsed;
                contentFrame.Margin = new Thickness(0, 10, 0, 0);
                SidebarButton.Opacity = 0.7;
                sb.Visibility = Visibility.Collapsed;
                Splitter.Visibility = Visibility.Collapsed;
                SidebarTop.ClearValue(FrameworkElement.WidthProperty);
                SidebarTop.Width = 165;
            }
        }




#if DEBUG
        public bool shouldShowOOBE = false;
#else
                        public bool shouldShowOOBE = true;
#endif
        private async void CheckOnboarding()
        {
            if (!Settings.Launched && shouldShowOOBE)
            {
                ContentDialog dialog = new ContentDialog();
                Frame frame = new Frame();
                dialog.BorderBrush = (LinearGradientBrush)Application.Current.Resources["ProBG"];
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

        public async void CheckSecurity()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if ((availability != UserConsentVerifierAvailability.Available && Settings.isAuthUsed))
            {
                Settings.isAuthUsed = false;
                ContentDialog contentDialog = new ContentDialog() { Title = resourceLoader.GetString("AuthDisabledTitle"), Content = resourceLoader.GetString("AuthDisabledDescription"), PrimaryButtonText = "OK", DefaultButton = ContentDialogButton.Primary };
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
        public void HandleToastActivation(string argument)
        {
            // probably doesnt work
            var listId = argument.Split('=')[1];

            contentFrame.Navigate(typeof(TaskPage), listId);
            foreach (ListViewItem item in Navigation.Items)
            {
                if (item?.Tag?.ToString()?.Replace(".json", null) == listId)
                {
                    Navigation.SelectedItem = item;
                    break;
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

        private Grid CreateNavigationItemContent(string emoji, string name, Thickness margin)
        {
            var content = new Grid
            {
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fontIcon = new FontIcon
            {
                Glyph = emoji ?? "📋",
                FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"),
                FontSize = 12
            };
            Grid.SetColumn(fontIcon, 0);

            var textBlock = new TextBlock
            {
                Text = name,
                Margin = new Thickness(10, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(textBlock, 1);

            content.Children.Add(fontIcon);
            content.Children.Add(textBlock);

            return content;
        }

        private void SetupNavigationMenu()
        {
            foreach (var list in ListTools.GetLists().Where(l => !string.IsNullOrEmpty(l.name)))
            {
                var listEmoji = list.emoji ?? "📋";
                var content = CreateNavigationItemContent(listEmoji, list.name, new Thickness(-3));

                var item = new ListViewItem
                {
                    Tag = list.id,
                    Content = content,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                Navigation.Items.Add(item);
                AddRightClickMenu(item);
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRing.IsActive = false;
            LoadRing.Visibility = Visibility.Collapsed;
            SetupNavigationMenu();
        }



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


        private void ListRenamed(string listID, string newname, string emoji)
        {
            Debug.WriteLine("List got renamed:" + listID);
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem?.Tag?.ToString()?.Replace(".json", null) == listID && navigationItem != null)
                    {
                        navigationItem.Content = CreateNavigationItemContent(emoji, newname, new Thickness(-3));
                        break;
                    }
                }
            }
        }

        private void ListEmojiChanged(string listID, string name, string emoji)
        {
            Debug.WriteLine("List emoji changed: " + listID);
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem?.Tag?.ToString()?.Replace(".json", null) == listID && navigationItem != null)
                    {
                        navigationItem.Content = CreateNavigationItemContent(emoji, name, new Thickness(-2));
                        break;
                    }
                }
            }
        }

        private void ListDeleted(string listID)
        {
            Debug.WriteLine("List deleted: " + listID);
            contentFrame.Navigate(typeof(EmptyPage));
            Navigation.SelectedItem = null;
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString()?.Replace(".json", null) == listID)
                    {
                        Navigation.Items.Remove(item);
                        break;
                    }
                }
            }
        }



        private void AddRightClickMenu(ListViewItem item)
        {
            item.RightTapped += OpenRightClickList;
        }

        private void OpenRightClickList(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            MenuFlyout flyout = new MenuFlyout();
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Rename), Text = resourceLoader.GetString("RenameList/Text"), Tag = ((ListViewItem)sender)?.Tag?.ToString()?.Replace(".json", "") });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Save), Text = resourceLoader.GetString("ExportList/Text"), Tag = ((ListViewItem)sender)?.Tag?.ToString()?.Replace(".json", "") });
            flyout.Items.Add(new MenuFlyoutItem() { Icon = new SymbolIcon(Symbol.Delete), Text = resourceLoader.GetString("DeleteList/Text"), Tag = ((ListViewItem)sender)?.Tag?.ToString()?.Replace(".json", "") });
            ((MenuFlyoutItem)flyout.Items[0]).Click += RenameList_Click;
            ((MenuFlyoutItem)flyout.Items[1]).Click += ExportList_Click;
            ((MenuFlyoutItem)flyout.Items[2]).Click += DeleteList_Click;
            flyout.ShowAt((ListViewItem)sender);
        }



        private void ListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { // opens a list
            ListView NavList = (ListView)sender;
            var selectedItem = NavList?.SelectedItem as ListViewItem;
            if (selectedItem != null && selectedItem.Tag is string tag)
            {
                contentFrame.Navigate(typeof(TaskPage), tag.Replace(".json", null));
            }
        }


        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navigation.Height = rectlist.ActualHeight;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            {
                sender.IsSuggestionListOpen = false;
                sender.ItemsSource = Array.Empty<string>();
                return;
            }

            var searchTerm = sender.Text.ToLower();
            var lists = ListTools.GetLists()
                .Where(l => l.name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                .Select(l => l.name)
                .ToArray();

            sender.ItemsSource = lists;
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            sender.IsSuggestionListOpen = true;
        }

        private void searchbox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            try
            {
                string foundItem = "";
                foreach ((string name, string id, string emoji) item in ListTools.GetLists())
                {
                    if (args.SelectedItem.ToString() == item.name && item.id != null)
                    {
                        foundItem = item.id.Replace(".json", null);
                    }
                }
                if (!string.IsNullOrEmpty(foundItem))
                {
                    contentFrame.Navigate(typeof(TaskPage), foundItem.Replace(".json", null));
                    foreach (ListViewItem item in Navigation.Items)
                    {
                        if (item.Tag?.ToString()?.Replace(".json", null) == foundItem)
                        {
                            Navigation.SelectedItem = item;
                        }
                    }
                }

                sender.Text = "";
                searchbox.ItemsSource = new List<string>();
            }
            catch (Exception ex) { Debug.WriteLine("[Search box suggestion chooser] Exception occured: " + ex.Message); }
        }

        private void MainPage_ActualThemeChanged(FrameworkElement sender = null, object args = null)
        {
            if (rect1 is FrameworkElement frameworkElement)
            {
                frameworkElement.SetValue(Panel.BackgroundProperty, Application.Current.Resources["SidebarBrush"]);
            }
            if (rect2 is FrameworkElement frameworkElement2)
            {
                frameworkElement2.SetValue(Panel.BackgroundProperty, Application.Current.Resources["SystemAltLowColorBrush"]);
            }
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private void UpdateLists(string listID = null, string listName = null)
        {
            Navigation.Items.Clear();
            SetupNavigationMenu();
            contentFrame.Navigate(typeof(EmptyPage));
        }

        private async void AddList(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();
            TextBox box = new TextBox() { VerticalContentAlignment = VerticalAlignment.Bottom, MaxWidth = 300, BorderThickness = new Thickness(0), PlaceholderText = resourceLoader.GetString("NewList"), Padding = new Thickness(9, 9, 4, 4), FontSize = 15, CornerRadius = new CornerRadius(4) };
            Button emojiButton = new Button() { Content = "📋", Padding = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Width = 40, Height = 40, FontSize = 20 };
            var flyout = new Flyout();
            StackPanel emojiPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var gridView = new GridView();
            gridView.ItemsPanel = (ItemsPanelTemplate)Application.Current.Resources["WrapGridPanel"];
            gridView.ItemTemplate = (DataTemplate)Application.Current.Resources["EmojiBlock"];
            gridView.ItemsSource = new Tools.IncrementalEmojiSource();
            gridView.SelectionMode = ListViewSelectionMode.Single;
            gridView.SelectionChanged += (s, args) =>
            {
                emojiButton.Content = gridView.SelectedItem;
                flyout.Hide();
            };
            AutoSuggestBox searchBox = new AutoSuggestBox() { PlaceholderText = resourceLoader.GetString("SearchBox/PlaceholderText"), Margin = new Thickness(0, 0, 0, 10), Width = 240, MaxWidth = 240, QueryIcon = new SymbolIcon(Symbol.Find) };
            searchBox.TextChanged += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    gridView.ItemsSource = new Tools.IncrementalEmojiSource();
                }
                else
                {
                    var searchTerm = searchBox.Text.ToLower();
                    gridView.ItemsSource = (new Tools()).Emojis.Where(emoji => emoji.SearchTerms.Any(term => term.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            };
            emojiPanel.Children.Add(searchBox);
            emojiPanel.Children.Add(gridView);
            flyout.Content = emojiPanel;
            flyout.Placement = FlyoutPlacementMode.Bottom;
            emojiButton.Click += (sender2, args) => { flyout.ShowAt(emojiButton); };
            Grid panel = new Grid()
            {
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

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var emoji = emojiButton.Content?.ToString() ?? "📋";
                var listName2 = box.Text.Trim();

                if (string.IsNullOrEmpty(listName2))
                {
                    listName2 = resourceLoader.GetString("NewList");
                }

                var newListId = ListTools.CreateList(listName2, emoji);
                if (!string.IsNullOrEmpty(newListId))
                {
                    UpdateLists();
                    var newItem = Navigation.Items
                        .OfType<ListViewItem>()
                        .FirstOrDefault(i => i.Tag?.ToString()?.Replace(".json", null) == newListId);

                    if (newItem != null)
                    {
                        Navigation.SelectedItem = newItem;
                    }
                }
            };

            await dialog.ShowAsync();
        }

        private void mainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (mainGrid.ColumnDefinitions[0].ActualWidth + 300 > ActualWidth) {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(185);
            }
        }

        private void Sizer1_SizeChanged(object sender, SizeChangedEventArgs e) {
            SidebarTop.Width = e.NewSize.Width;
        }
    }
}
