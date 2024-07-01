using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using TaskieLib;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;
using System.ComponentModel;
using Windows.UI;
using System.Reflection;
using Windows.ApplicationModel.Resources;
using System.Diagnostics;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;

namespace Taskie
{
    public sealed partial class TaskPage : Page
    {
        public TaskPage()
        {
            this.InitializeComponent();
            ActualThemeChanged += TaskPage_ActualThemeChanged;

        }

        private void TaskPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
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
                        ;
                    }
                }
            }
        }

        public string listname { get; set; }

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
                listname = e.Parameter.ToString();
            }
            base.OnNavigatedTo(e);

            if (Tools.ReadList(listname) != null)
            {
                foreach (ListTask task in Tools.ReadList(listname))
                {
                    taskListView.Items.Add(task);
                }
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            List<ListTask> tasks = new List<ListTask>();
            if (Tools.ReadList(listname) != null && (Tools.ReadList(listname)).Count > 0)
            {
                foreach (ListTask task2add in Tools.ReadList(listname))
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
            Tools.SaveList(listname, tasks);
        }

        public ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

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
                List<ListTask> tasks = new List<ListTask>();
                if (Tools.ReadList(listname) != null && (Tools.ReadList(listname)).Count > 0)
                {
                    foreach (ListTask task2add in Tools.ReadList(listname))
                    {
                        tasks.Add(task2add);
                    }
                };
                int index = tasks.FindIndex(task => task.CreationDate == note.CreationDate);
                tasks[index] = note;
                Tools.SaveList(listname, tasks);
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask taskToDelete = (sender as MenuFlyoutItem).DataContext as ListTask;
            List<ListTask> tasks = Tools.ReadList(listname);
            int index = tasks.FindIndex(task => task.CreationDate == taskToDelete.CreationDate);
            if (index != -1)
            {
                tasks.RemoveAt(index);
                Tools.SaveList(listname, tasks);
                taskListView.Items.Remove(taskToDelete);
            }
            Tools.SaveList(listname, tasks);
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
                Tools.RenameList(listname, text);
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
                        string content = Tools.GetTaskFileContent(listname);
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
            Tools.DeleteList(listname);
        }

        private void TaskStateChanged(object sender, RoutedEventArgs e)
        {
            ListTask tasktoChange = (sender as CheckBox).DataContext as ListTask;
            List<ListTask> tasks = Tools.ReadList(listname);
            try
            {
                int index = tasks.FindIndex(task => task.CreationDate == tasktoChange.CreationDate);
                if (index != -1)
                {
                    tasktoChange.IsDone = (bool)(sender as CheckBox).IsChecked;
                    tasks[index] = tasktoChange;
                    Tools.SaveList(listname, tasks);
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
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                List<ListTask> tasks = new List<ListTask>();
                if (Tools.ReadList(listname) != null && (Tools.ReadList(listname)).Count > 0)
                {
                    foreach (ListTask task2add in Tools.ReadList(listname))
                    {
                        tasks.Add(task2add);
                    }
                };
                ListTask task = new ListTask()
                {
                    Name = (sender as AutoSuggestBox).Text,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                tasks.Add(task);
                taskListView.Items.Add(task);
                Tools.SaveList(listname, tasks);
            }
        }

        private void TaskAdded_Grid(object sender, RoutedEventArgs e)
        {
            ChangeWidth(NameBox);
        }
    }
}
