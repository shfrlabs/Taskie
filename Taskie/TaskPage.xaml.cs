using System;
using System.Collections.Generic;
using System.Linq;
using TaskieLib;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Taskie
{
    public sealed partial class TaskPage : Page
    {
        public TaskPage()
        {
            this.InitializeComponent();
            ActualThemeChanged += TaskPage_ActualThemeChanged;
            ListTools.ListRenamedEvent += ListRenamed;
            testname.FontFamily = ListTools.ReadList(listId).Metadata.TitleFont != null ? new FontFamily(ListTools.ReadList(listId).Metadata.TitleFont) : new FontFamily("Segoe UI Variable");
        }

        private void ListRenamed(string oldname, string newname)
        {
          testname.Text = newname;
        }

        private void TaskPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (ListTools.isAWOpen)
            {
                if (Settings.Theme == "System")
                {
                    if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    {
                        TPage.Background = new SolidColorBrush { Color = Color.FromArgb(255, 33, 33, 33) };
                    }
                    else if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                    {
                        TPage.Background = new SolidColorBrush { Color = Colors.White };
                    }
                }
                else
                {
                    if (Settings.Theme == "Dark")
                    {
                        TPage.Background = new SolidColorBrush { Color = Color.FromArgb(255, 33, 33, 33) };
                    }
                    else if (Settings.Theme == "Light")
                    {
                        TPage.Background = new SolidColorBrush { Color = Colors.White };
                    }
                }
            }

            Brush bg = Application.Current.Resources["LayerFillColorDefaultBrush"] as Brush;
            addTaskRect.Fill = bg;

            foreach (var item in taskListView.Items)
            {
                var container = taskListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var rootGrid = FindVisualChild<Grid>(container, "rootGrid");
                    if (rootGrid != null)
                    {
                        rootGrid.Background = bg;
                    }
                }
            }
        }

        public string listname { get; set; }
        public string listId { get; set; }

        private T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is FrameworkElement && ((FrameworkElement)child).Name == name)
                {
                    return (T)child;
                }

                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            listId = e.Parameter.ToString();
            if (e.Parameter != null)
            {
                testname.Text = ListTools.ReadList(listId).Metadata.Name;
                listname = ListTools.ReadList(listId).Metadata.Name;
            }
            base.OnNavigatedTo(e);

            if (!(ListTools.ReadList(listId).Tasks == null || ListTools.ReadList(listId).Metadata == null))
            {
                foreach (ListTask task in ListTools.ReadList(listId).Tasks)
                {
                    taskListView.Items.Add(task);
                }
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.QueryText)) {
                ListMetadata metadata = ListTools.ReadList(listId).Metadata;
                List<ListTask> tasks = new List<ListTask>();
                if (ListTools.ReadList(listId).Tasks.Count > 0)
                {
                    foreach (ListTask task2add in ListTools.ReadList(listId).Tasks)
                    {
                        tasks.Add(task2add);
                    }
                };
                ListTask task = new ListTask()
                {
                    Name = args.QueryText,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                tasks.Add(task);
                taskListView.Items.Add(task);
                ListTools.SaveList(listId, tasks, metadata);
            }
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private async void RenameTask_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            var note = menuFlyoutItem.DataContext as ListTask;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("TaskName"), Text = note.Name };
            if (!ListTools.isAWOpen)
            {
                ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameTask/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    string text = input.Text;
                    note.Name = text;
                    List<ListTask> tasks = new List<ListTask>();
                    if (!(ListTools.ReadList(listId).Tasks == null || ListTools.ReadList(listId).Metadata == null))
                    {
                        foreach (ListTask task2add in ListTools.ReadList(listId).Tasks)
                        {
                            tasks.Add(task2add);
                        }
                    };
                    int index = tasks.FindIndex(task => task.CreationDate == note.CreationDate);
                    tasks[index] = note;
                    ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
                }
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask taskToDelete = (sender as MenuFlyoutItem).DataContext as ListTask;
            List<ListTask> tasks = ListTools.ReadList(listId).Tasks;
            int index = tasks.FindIndex(task => task.CreationDate == taskToDelete.CreationDate);
            if (index != -1)
            {
                tasks.RemoveAt(index);
                ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
                taskListView.Items.Remove(taskToDelete);
            }
            ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
        }

        private async void RenameList_Click(object sender, RoutedEventArgs e)
        {
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("ListName"), Text = listname };
            string renamelisttext = resourceLoader.GetString("RenameList/Text");
            ContentDialog dialog = new ContentDialog() { Title = renamelisttext, PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                ListTools.RenameList(listId, text);
                listname = text;
                testname.Text = listname;
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
                        SuggestedFileName = listname
                    };
                    savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        CachedFileManager.DeferUpdates(file);
                        string content = ListTools.GetTaskFileContent(listId);
                        await FileIO.WriteTextAsync(file, content);

                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    }
                    else
                    { }
                });
            }
            catch { }
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            ListTools.DeleteList(listId);
        }

        private void TaskStateChanged(object sender, RoutedEventArgs e)
        {
            ListTask tasktoChange = (sender as CheckBox).DataContext as ListTask;
            List<ListTask> tasks = ListTools.ReadList(listId).Tasks;
            try
            {
                int index = tasks.FindIndex(task => task.CreationDate == tasktoChange.CreationDate);
                if (index != -1)
                {
                    tasktoChange.IsDone = (bool)(sender as CheckBox).IsChecked;
                    tasks[index] = tasktoChange;
                    ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
                }
            }
            catch { }

        }

        private void NameBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeWidth(sender);
        }

        private void ChangeWidth(object sender)
        {
            foreach (ListTask task in taskListView.Items)
            {
                var item = taskListView.ContainerFromItem(task) as ListViewItem;
                if (item != null)
                {
                    var taskNameText = FindDescendant<TextBlock>(item, "TaskNameText");

                    if (taskNameText != null)
                    {
                        taskNameText.MaxWidth = (sender as Rectangle).ActualWidth;
                    }
                }
            }
        }

        private T FindDescendant<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T frameworkElement && frameworkElement.Name == name)
                {
                    return frameworkElement;
                }

                var result = FindDescendant<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void NameBox_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeWidth(sender);
        }

        private void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                List<ListTask> tasks = new List<ListTask>();
                if (!(ListTools.ReadList(listId).Tasks == null || ListTools.ReadList(listId).Metadata == null))
                {
                    foreach (ListTask task2add in ListTools.ReadList(listId).Tasks)
                    {
                        tasks.Add(task2add);
                    }
                };
                ListMetadata metadata = ListTools.ReadList(listId).Metadata;
                ListTask task = new ListTask()
                {
                    Name = (sender as AutoSuggestBox).Text,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                tasks.Add(task);
                taskListView.Items.Add(task);
                ListTools.SaveList(listId, tasks, metadata);
            }
        }

        private void TaskAdded_Grid(object sender, RoutedEventArgs e)
        {
            ChangeWidth(NameBox);
        }
        private async void CompactOverlay_Click(object sender, RoutedEventArgs e)
        {
            AppWindow window = await AppWindow.TryCreateAsync();
            window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            if (Settings.Theme == "Dark")
            {
                window.TitleBar.ButtonForegroundColor = Colors.White;
            }
            else if (Settings.Theme == "Light")
            {
                window.TitleBar.ButtonForegroundColor = Colors.Black;
            }
            window.Closed += AWClosed;
            Frame frame = new Frame();
            frame.Navigate(typeof(TaskPage), listId.Replace(".json", null));
            ListTools.isAWOpen = true;
            ElementCompositionPreview.SetAppWindowContent(window, frame);
            window.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
            window.TitleBar.ExtendsContentIntoTitleBar = true;
            await window.TryShowAsync();
            IList<AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            await resourceInfos[0].StartSuspendAsync();
            cobtn.Visibility = Visibility.Collapsed;
            this.Frame.Navigate(typeof(COClosePage));
        }

        private void AWClosed(AppWindow sender, AppWindowClosedEventArgs args)
        {
            ListTools.isAWOpen = false;
            cobtn.Visibility = Visibility.Visible;
        }

        private void topoptions_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListTools.isAWOpen)
            {
                if (Settings.Theme == "System")
                {
                    if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    {
                        TPage.Background = new SolidColorBrush { Color = Color.FromArgb(255, 33, 33, 33) };
                    }
                    else if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                    {
                        TPage.Background = new SolidColorBrush { Color = Colors.White };
                    }
                }
                else
                {
                    if (Settings.Theme == "Dark")
                    {
                        TPage.Background = new SolidColorBrush { Color = Color.FromArgb(255, 33, 33, 33) };
                    }
                    else if (Settings.Theme == "Light")
                    {
                        TPage.Background = new SolidColorBrush { Color = Colors.White };
                    }
                }
                topoptions.Visibility = Visibility.Collapsed;
            }
        }

        private void testname_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (Settings.isPro)
            {
                MenuFlyout flyout = new MenuFlyout();
                MenuFlyoutSubItem item = new MenuFlyoutSubItem();
                item.Icon = new SymbolIcon(Symbol.Font);
                item.Text = resourceLoader.GetString("ChangeFont");
                foreach (string font in Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies())
                {
                    MenuFlyoutItem subfont = new MenuFlyoutItem() { Tag = font, Text = font, FontFamily = new FontFamily(font) };
                    subfont.Click += (sender, args) => {
                        System.Diagnostics.Debug.WriteLine(listId);
                        ListTools.ChangeListFont(listId, (sender as MenuFlyoutItem).Tag.ToString());
                        testname.FontFamily = new FontFamily(font);
                    };
                    item.Items.Add(subfont);
                }
                flyout.Items.Add(item);
                flyout.ShowAt(testname);
            }
        }

        private void MenuFlyoutItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListTools.isAWOpen)
            {
                (sender as MenuFlyoutItem).Visibility = Visibility.Collapsed;
            }
        }
    }
}