using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
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
using Windows.UI.Xaml.Controls.Primitives;
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
                ContentDialog dialog = new ContentDialog() { Title = resourceLoader.GetString("RenameTask/Text"), PrimaryButtonText = "OK", SecondaryButtonText = resourceLoader.GetString("Cancel"), Content = input, DefaultButton = ContentDialogButton.Primary };
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

        private void RenameList_Click(object sender, RoutedEventArgs e)
        {
            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("ListName"),
                Text = listname,
                Width = 200
            };
            Button okButton = new Button()
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 50,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Flyout flyout = new Flyout()
            {
                Content = new StackPanel()
                {
                    Children =
            {
                input,
                okButton
            }
                }
            };
            okButton.Click += (s, args) =>
            {
                string text = input.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    ListTools.RenameList(listId, text);
                    listname = text;
                    testname.Text = listname;
                }
                flyout.Hide();
            };
            flyout.ShowAt(topoptions);
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
            catch (Exception ex) { Debug.WriteLine("[List export] Exception occured: " + ex.Message); }
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
            catch (Exception ex) { Debug.WriteLine("[Task state change] Exception occured: " + ex.Message); }

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
                        taskNameText.Width = (sender as Rectangle).ActualWidth;
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

        private async void AddSubTask_Click(object sender, RoutedEventArgs e)
        {
            (ListMetadata meta, List<ListTask> tasklist) = ListTools.ReadList(listId);
            ListTask parent = (sender as MenuFlyoutItem).DataContext as ListTask;
            int index = tasklist.FindIndex(task => task.CreationDate == parent?.CreationDate);
            if (parent != null)
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = resourceLoader.GetString("NewSubTaskTitle");
                TextBox box = new TextBox();
                dialog.Content = box;
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = resourceLoader.GetString("Cancel");

                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(box.Text))
                {
                    ListTask task2add = new ListTask
                    {
                        CreationDate = DateTime.Now,
                        ParentCreationDate = parent.CreationDate,
                        IsDone = false,
                        Name = box.Text,
                        SubTasks = new ObservableCollection<ListTask>()
                    };
                    parent.SubTasks.Add(task2add);
                }
            }
            Debug.WriteLine(index);
            if (index > -1)
            {
                tasklist[index] = parent;
                ListTools.SaveList(listId, tasklist, meta);
            }
        }

        // no, i dont know what a sephamore is this isnt my first language
        // a very special someone helped me here
        private static SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);

        private async void SubTaskStateChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            var taskToChange = checkBox.DataContext as ListTask;
            if (taskToChange == null) return;

            try
            {
                await _updateSemaphore.WaitAsync();

                var currentList = ListTools.ReadList(listId);
                var tasks = currentList.Tasks;

                foreach (var task in tasks)
                {
                    var subTask = task.SubTasks.FirstOrDefault(st => st.CreationDate == taskToChange.CreationDate);
                    if (subTask != null)
                    {
                        subTask.IsDone = checkBox.IsChecked ?? false;
                        break;
                    }
                }
                ListTools.SaveList(listId, tasks, currentList.Metadata);
            }
            catch (Exception ex) { Debug.WriteLine("[Subtask state change] Exception occured: " + ex.Message); }
            finally
            {
                _updateSemaphore.Release();
            }
        }


        private void DeleteSubTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask subTask = (sender as MenuFlyoutItem)?.DataContext as ListTask;
            if (subTask == null)
            {
                return;
            }

            (ListMetadata meta, List<ListTask> tasks) = ListTools.ReadList(listId);

            int index = tasks.FindIndex(task => task.CreationDate == subTask?.ParentCreationDate);
            if (index > -1)
            {
                ListTask parentTask = tasks[index];

                ListTask taskToRemove = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                if (taskToRemove != null)
                {
                    parentTask.SubTasks.Remove(taskToRemove);
                    tasks[index] = parentTask;
                    taskListView.Items[index] = parentTask; // TODO: this is baaddd
                }
                else
                {
                }
            }
            else
            {
            }
            ListTools.SaveList(listId, tasks, meta);
        }


        private async void RenameSubTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask subTask = (sender as MenuFlyoutItem)?.DataContext as ListTask;
            if (subTask == null)
            {
                return;
            }
            (ListMetadata meta, List<ListTask> tasks) = ListTools.ReadList(listId);

            if (!ListTools.isAWOpen)
            {
                ContentDialog dialog = new ContentDialog();
                dialog.Title = resourceLoader.GetString("RenameSubTaskTitle");
                TextBox box = new TextBox() { Text = subTask.Name };
                dialog.Content = box;
                dialog.DefaultButton = ContentDialogButton.Primary;
                dialog.PrimaryButtonText = "OK";
                dialog.SecondaryButtonText = resourceLoader.GetString("Cancel");

                ContentDialogResult result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(box.Text))
                {
                    int index = tasks.FindIndex(task => task.CreationDate == subTask?.ParentCreationDate);
                    if (index > -1)
                    {
                        ListTask parentTask = tasks[index];

                        ListTask taskToRemove = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                        if (taskToRemove != null)
                        {
                            parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate).Name = box.Text;
                            tasks[index] = parentTask;
                            taskListView.Items[index] = parentTask; // TODO: this is baaddd
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                    ListTools.SaveList(listId, tasks, meta);
                }
            }
        }

        private void UpdateFlyoutMenu(MenuFlyout flyout, ListTask task, Button btn)
        {
            flyout.Items.Remove(flyout.Items.FirstOrDefault(item => (item as MenuFlyoutItem)?.Tag?.ToString() == "Reminder"));

            if (!task.HasReminder())
            {
                var addReminderItem = new MenuFlyoutItem
                {
                    Icon = new SymbolIcon(Symbol.Calendar),
                    Text = resourceLoader.GetString("AddReminder/Text"),
                    Tag = "Reminder"
                };
                Button addReminderBtn = new Button();
                addReminderBtn.Margin = new Thickness(0, 5, 0, 0);
                addReminderBtn.Content = resourceLoader.GetString("AddReminder/Text");
                addReminderBtn.Width = 250;

                CalendarDatePicker datePicker = new CalendarDatePicker();
                
                datePicker.Date = DateTime.Now;
                datePicker.MinDate = DateTime.Now;
                datePicker.Width = 250;

                TimePicker timePicker = new TimePicker();
                timePicker.Time = DateTime.Now.TimeOfDay;
                timePicker.Width = 250;
                timePicker.Margin = new Thickness(0, 5, 0, 0);

                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

                stackPanel.Children.Add(datePicker);
                stackPanel.Children.Add(timePicker);
                stackPanel.Children.Add(addReminderBtn);

                Flyout timeChooser = new Flyout();
                timeChooser.Content = stackPanel;
                addReminderBtn.Click += (s, args) =>
                {
                    DateTime date = new DateTime(datePicker.Date.Value.Year, datePicker.Date.Value.Month, datePicker.Date.Value.Day, timePicker.Time.Hours, timePicker.Time.Minutes, timePicker.Time.Seconds);
                    if (date > DateTime.Now)
                    { task.AddReminder(date, listId); }
                    timeChooser.Hide();
                };

                addReminderItem.Click += (s, args) => {
                    timeChooser.ShowAt(btn, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Bottom });
                };

                flyout.Items.Add(addReminderItem);
            }
            else
            {
                var removeReminderItem = new MenuFlyoutItem
                {
                    Icon = new SymbolIcon(Symbol.Delete),
                    Text = resourceLoader.GetString("RemoveReminder/Text"),
                    Tag = "Reminder"
                };

                removeReminderItem.Click += (s, args) =>
                {
                    task.RemoveReminder();
                    UpdateFlyoutMenu(flyout, task, btn);
                };

                flyout.Items.Add(removeReminderItem);
            }
        }

        private void TaskThreeDots_Loaded(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ListTask boundTask = button.DataContext as ListTask;
            var taskList = ListTools.ReadList(listId).Tasks;
            ListTask task;
            try
            {
                task = taskList.FirstOrDefault(t => t.CreationDate == boundTask.CreationDate);
                if (task == null)
                {
                    return;
                }
                MenuFlyout flyout = button.Flyout as MenuFlyout;
                UpdateFlyoutMenu(flyout, task, button);
            } catch (Exception ex) { Debug.WriteLine("[TaskThreeDots_Loaded] Exception occured: " + ex.Message); }
        }

    }
}