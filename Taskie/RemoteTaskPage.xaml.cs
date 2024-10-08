using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed partial class RemoteTaskPage : Page
    {
        public List<ListTask> currentTask = new List<ListTask>();
        public RemoteTaskPage()
        {
            this.InitializeComponent();
            ActualThemeChanged += TaskPage_ActualThemeChanged;
        }

        private void TaskPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (Tools.isAWOpen)
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

        public string listcode { get; set; }

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
            if (e.Parameter != null)
            {
                testname.Text = e.Parameter.ToString();
                listcode = e.Parameter.ToString();
            }
            base.OnNavigatedTo(e);

            refreshList(null);
            foreach (ListTask task in currentTask)
            {
                taskListView.Items.Add(task);
            }
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.QueryText))
            {
                await ServerCommunication.AddTask(listcode, args.QueryText);
                refreshList(null);
            }
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private async void RenameTask_Click(object sender, RoutedEventArgs e)
        {
            var currentTask = await ServerCommunication.GetList(listcode);
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            var note = menuFlyoutItem.DataContext as ListTask;
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("TaskName"), Text = note.Name };
            ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameTask/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                note.Name = text;
                await ServerCommunication.RenameTask(listcode, note.CreationDate, text);
                (taskListView.ItemsSource as ObservableCollection<ListTask>).FirstOrDefault(t => t.CreationDate == note.CreationDate).Name = text;
            }
        }

        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            var currentTask = await ServerCommunication.GetList(listcode);
            ListTask taskToDelete = (sender as MenuFlyoutItem).DataContext as ListTask;
            List<ListTask> tasks = currentTask;
            try
            {
                int index = tasks.FindIndex(task => task.CreationDate == taskToDelete.CreationDate);
                if (index != -1)
                {
                    (taskListView.ItemsSource as ObservableCollection<ListTask>).Remove(taskToDelete);
                    await ServerCommunication.DeleteTask(listcode, taskToDelete.CreationDate);
                    refreshList(null);
                }
            } catch { }
            
        }

        private async void RenameList_Click(object sender, RoutedEventArgs e)
        {
            TextBox input = new TextBox() { PlaceholderText = resourceLoader.GetString("listcode"), Text = listcode };
            string renamelisttext = resourceLoader.GetString("RenameList/Text");
            ContentDialog dialog = new ContentDialog() { Title = renamelisttext, PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                await ServerCommunication.RenameList(listcode, text);
                testname.Text = text;
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
                        SuggestedFileName = listcode
                    };
                    savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        CachedFileManager.DeferUpdates(file);
                        string content = JsonConvert.SerializeObject(ServerCommunication.GetList(listcode));
                        await FileIO.WriteTextAsync(file, content);

                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    }
                    else
                    { }
                });
            }
            catch { }
        }

        private async void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            await ServerCommunication.DeleteList(listcode);
            this.Frame.Content = null;
            shouldRun = false;

        }

        private async void TaskStateChanged(object sender, RoutedEventArgs e)
        {
            var currentTask = await ServerCommunication.GetList(listcode);
            Debug.WriteLine("tried to toggle?");
            ListTask tasktoChange = (sender as CheckBox).DataContext as ListTask;
            List<ListTask> tasks = currentTask;
            try
            {
                int index = tasks.FindIndex(task => task.CreationDate == tasktoChange.CreationDate);
                if (index != -1)
                {
                    tasktoChange.IsDone = (bool)(sender as CheckBox).IsChecked;
                    (taskListView.ItemsSource as ObservableCollection<ListTask>).FirstOrDefault(t => t.CreationDate == tasktoChange.CreationDate).IsDone = (bool)(sender as CheckBox).IsChecked;
                    await ServerCommunication.ToggleTask(listcode, tasktoChange.CreationDate);
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

        private async void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                ListTask task = new ListTask()
                {
                    Name = (sender as AutoSuggestBox).Text,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                taskListView.Items.Add(task);
                await ServerCommunication.AddTask(listcode, (sender as AutoSuggestBox).Text);
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
            frame.Navigate(typeof(TaskPage), listcode);
            Tools.isAWOpen = true;
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
            Tools.isAWOpen = false;
            cobtn.Visibility = Visibility.Visible;
        }

        private void topoptions_Loaded(object sender, RoutedEventArgs e)
        {
            if (Tools.isAWOpen)
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

        public bool shouldRun = true;

        private async void taskListView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                currentTask = await ServerCommunication.GetList(listcode);
                taskListView.ItemsSource = currentTask;
                while (shouldRun)
                {
                    refreshList(null);
                    await Task.Delay(5000);
                }
            }
            catch
            {
                this.Frame.Content = null;
                shouldRun = false;
            }

        }

        private async void refreshList(object state)
        {
            try
            {
                var currentTask = await ServerCommunication.GetList(listcode);
                Debug.WriteLine(currentTask);

                var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var existingTasks = taskListView.ItemsSource as ObservableCollection<ListTask>;

                    if (existingTasks != null)
                    {
                        var existingTasksDict = existingTasks.ToDictionary(task => task.CreationDate);

                        foreach (var newTask in currentTask)
                        {
                            if (existingTasksDict.TryGetValue(newTask.CreationDate, out var existingTask))
                            {
                                // Update the Name if it has changed
                                if (existingTask.Name != newTask.Name)
                                {
                                    existingTask.Name = newTask.Name;
                                }

                                // Update the IsDone state if it has changed
                                if (existingTask.IsDone != newTask.IsDone)
                                {
                                    existingTask.IsDone = newTask.IsDone;
                                }
                            }
                            else
                            {
                                int index = existingTasks.ToList().FindIndex(t => t.CreationDate > newTask.CreationDate);
                                if (index < 0)
                                {
                                    existingTasks.Add(newTask);
                                }
                                else
                                {
                                    existingTasks.Insert(index, newTask);
                                }
                            }
                        }

                        var currentTaskDates = new HashSet<DateTime>(currentTask.Select(t => t.CreationDate));
                        var tasksToRemove = existingTasks.Where(t => !currentTaskDates.Contains(t.CreationDate)).ToList();
                        foreach (var task in tasksToRemove)
                        {
                            existingTasks.Remove(task);
                        }
                    }
                    else
                    {
                        taskListView.ItemsSource = new ObservableCollection<ListTask>(currentTask);
                    }
                });
            }
            catch
            {
                try
                {
                    await (new ContentDialog()
                    {
                        Title = resourceLoader.GetString("Oops"),
                        Content = resourceLoader.GetString("ConnectionProblem"),
                        PrimaryButtonText = resourceLoader.GetString("Close")
                    }).ShowAsync();
                }
                catch { }
                this.Frame.Content = null;
            }
        }

    }
}