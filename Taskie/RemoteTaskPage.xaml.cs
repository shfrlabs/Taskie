using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Taskie
{
    public sealed partial class RemoteTaskPage : Page
    {
        public List<ListTask> currentTask = new List<ListTask>();
        public string listcode { get; set; }

        public RemoteTaskPage()
        {
            this.InitializeComponent();
            ServerCommunication.TaskAdded += (task) =>
            {
                Debug.WriteLine($"Task added: {task.Name}"); // Check if this line gets executed
                                                             // Add the task to your UI or list here
            };

            ActualThemeChanged += TaskPage_ActualThemeChanged;
        }

        private void TaskPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            // Background theme change handling
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                testname.Text = e.Parameter.ToString();
                listcode = e.Parameter.ToString();
            }
            base.OnNavigatedTo(e);

            try
            {
                await ServerCommunication.InitializeSignalRConnection(listcode); // Initialize SignalR
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
            RegisterSignalREvents(); // Register SignalR events

            // Load current tasks
            await LoadTasks();
        }

        private async Task LoadTasks()
        {
            currentTask = await ServerCommunication.GetList(listcode);
            try
            {
                taskListView.ItemsSource = new ObservableCollection<ListTask>(currentTask);
            }
            catch
            {
                this.Frame.Content = null;
                await ServerCommunication.StopSignalRConnection(listcode);
            }
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.QueryText))
            {
                await ServerCommunication.AddTask(listcode, args.QueryText);
            }
        }

        public ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        private async void RenameTask_Click(object sender, RoutedEventArgs e)
        {
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
            ListTask taskToDelete = (sender as MenuFlyoutItem).DataContext as ListTask;
            try
            {
                (taskListView.ItemsSource as ObservableCollection<ListTask>).Remove(taskToDelete);
                await ServerCommunication.DeleteTask(listcode, taskToDelete.CreationDate);
            }
            catch { }
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
                        string content = JsonSerializer.Serialize(await ServerCommunication.GetList(listcode));
                        await FileIO.WriteTextAsync(file, content);
                        FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    }
                });
            }
            catch { }
        }

        private async void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            await ServerCommunication.DeleteList(listcode);
            this.Frame.Content = null;
        }

        private async void TaskStateChanged(object sender, RoutedEventArgs e)
        {
            ListTask taskToChange = (sender as CheckBox).DataContext as ListTask;
            taskToChange.IsDone = (bool)(sender as CheckBox).IsChecked;
            (taskListView.ItemsSource as ObservableCollection<ListTask>).FirstOrDefault(t => t.CreationDate == taskToChange.CreationDate).IsDone = (bool)(sender as CheckBox).IsChecked;
            await ServerCommunication.ToggleTask(listcode, taskToChange.CreationDate);
        }

        private void RegisterSignalREvents()
        {
            ServerCommunication.TaskAdded += OnTaskAdded;
            ServerCommunication.TaskRenamed += OnTaskRenamed;
            ServerCommunication.TaskDeleted += OnTaskDeleted;
            ServerCommunication.TaskToggled += OnTaskToggled;
        }

        private void UnregisterSignalREvents()
        {
            ServerCommunication.TaskAdded -= OnTaskAdded;
            ServerCommunication.TaskRenamed -= OnTaskRenamed;
            ServerCommunication.TaskDeleted -= OnTaskDeleted;
            ServerCommunication.TaskToggled -= OnTaskToggled;
        }

        private void OnTaskAdded(ListTask task)
        {
            var existingTasks = taskListView.ItemsSource as ObservableCollection<ListTask>;
            Debug.WriteLine(JsonSerializer.Serialize(task));
            existingTasks?.Add(task);
        }

        private void OnTaskRenamed(ListTask task)
        {
            var existingTasks = taskListView.ItemsSource as ObservableCollection<ListTask>;
            var existingTask = existingTasks?.FirstOrDefault(t => t.CreationDate == task.CreationDate);
            if (existingTask != null)
            {
                existingTask.Name = task.Name;
            }
        }

        private void OnTaskDeleted(string taskId)
        {
            var existingTasks = taskListView.ItemsSource as ObservableCollection<ListTask>;
            var taskToRemove = existingTasks?.FirstOrDefault(t => t.CreationDate.ToString() == taskId);
            if (taskToRemove != null)
            {
                existingTasks.Remove(taskToRemove);
            }
        }

        private void OnTaskToggled(ListTask task)
        {
            var existingTasks = taskListView.ItemsSource as ObservableCollection<ListTask>;
            var existingTask = existingTasks?.FirstOrDefault(t => t.CreationDate == task.CreationDate);
            if (existingTask != null)
            {
                existingTask.IsDone = task.IsDone;
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await ServerCommunication.StopSignalRConnection(listcode);
            UnregisterSignalREvents();
            base.OnNavigatedFrom(e);
        }

        private async void AutoSuggestBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
                {
                    await ServerCommunication.AddTask(listcode, (sender as AutoSuggestBox).Text);
                }
            }
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
            ChangeWidth(null);
        }

        private void NameBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeWidth(sender);
        }

        private void rootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeWidth(NameBox);
        }
    }
}
