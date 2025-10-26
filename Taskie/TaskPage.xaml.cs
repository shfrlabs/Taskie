﻿using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using TaskieLib;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace Taskie
{
    public sealed partial class TaskPage : Page
    {
        private List<ListTask> tasks;

        public TaskPage()
        {
            this.InitializeComponent();
            ActualThemeChanged += TaskPage_ActualThemeChanged;
            ListTools.ListRenamedEvent += ListRenamed;
        }



        private async void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as AppBarButton;
            if (button == null)
                return;

            DependencyObject current = button;
            while (current != null && !(current is ListView))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            var listView = current as ListView;
            if (listView != null && button.DataContext as AttachmentMetadata != null)
            {
                var parentDataContext = listView.DataContext as ListTask;
                var attachment = button.DataContext as AttachmentMetadata;
                await parentDataContext?.RemoveAttachmentAsync(attachment);

                // Persist the removal of Fairmark attachments
                if (attachment != null && attachment.IsFairmark)
                {
                    // Update the tasks list to reflect the removal
                    var index = tasks.FindIndex(t => t.CreationDate == parentDataContext.CreationDate);
                    if (index != -1)
                    {
                        tasks[index] = parentDataContext;
                    }
                }
                else
                {
                    // Always update the instance for file attachments too
                    var index = tasks.FindIndex(t => t.CreationDate == parentDataContext.CreationDate);
                    if (index != -1)
                    {
                        tasks[index] = parentDataContext;
                    }
                }
                ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
            }
        }

        private async void OpenAttachment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((sender as AppBarButton)?.DataContext as AttachmentMetadata).IsFairmark) {
                    await Launcher.LaunchUriAsync(new Uri("fairmark://default/" + ((sender as AppBarButton)?.DataContext as AttachmentMetadata).Id));
                }
                else {
                    await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, ((sender as AppBarButton)?.DataContext as AttachmentMetadata).RelativePath)));
                }
            }
            catch { }
        }
        private async void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
             MenuFlyout menuFlyout = new MenuFlyout();
                MenuFlyoutItem fileItem = new MenuFlyoutItem() { Text = resourceLoader.GetString("AttachFile"), Icon = new SymbolIcon(Symbol.OpenFile) };
                fileItem.Click += async (s, args) => {
                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    openPicker.FileTypeFilter.Add("*");
                    StorageFile file = await openPicker.PickSingleFileAsync();
                    if (file != null) {
                        var task = (sender as Button)?.DataContext as ListTask;
                        if (task != null) {
                            await task.AddAttachmentAsync(file, listId);
                            // Ensure the correct instance in tasks is updated
                            var index = tasks.FindIndex(t => t.CreationDate == task.CreationDate);
                            if (index != -1) {
                                tasks[index] = task;
                            }
                            ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
                        }
                    }
                    else { }
                };
                MenuFlyoutItem noteItem = new MenuFlyoutItem() { Text = resourceLoader.GetString("AttachNote"), Icon = new SymbolIcon(Symbol.Document) };
                noteItem.Click += async (s, args) => {
                    FairmarkFlyout flyout = new FairmarkFlyout();
                    flyout.Closed += async (snd, arg) => {
                        var selectedNote = flyout.selectedNote;
                        if (selectedNote != null)
                        {
                            Debug.WriteLine("Adding note attachment");
                            var task = (sender as Button)?.DataContext as ListTask;
                            if (task != null)
                            {
                                task.AddFairmarkAttachment(selectedNote, listId);
                                Debug.WriteLine("Note attachment added");
                                // Ensure the correct instance in tasks is updated
                                var index = tasks.FindIndex(t => t.CreationDate == task.CreationDate);
                                if (index != -1) {
                                    tasks[index] = task;
                                }
                                ListTools.SaveList(listId, tasks, ListTools.ReadList(listId).Metadata);
                            }
                        }
                    };
                    flyout.ShowAt(sender as AppBarButton);
                };
                menuFlyout.Items.Add(fileItem);
                menuFlyout.Items.Add(noteItem);
                menuFlyout.ShowAt(sender as AppBarButton);
        }
        private async void CustomizeList_Click(object sender, RoutedEventArgs e)
        {
            StackPanel panel = new StackPanel() { Margin = new Thickness(2) };

            StackPanel bgbtnpanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new FontIcon() { Glyph = "\uE70F", FontSize = 14, Margin = new Thickness(0, 0, 10, 0) },
                    new TextBlock() { Text = resourceLoader.GetString("ChangeListBackground"), FontSize = 14 }
                }
            };
            HyperlinkButton button = new HyperlinkButton()
            {
                Content = bgbtnpanel,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            button.Click += async (sender2, args) =>
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".jpeg");
                openPicker.FileTypeFilter.Add(".png");
                openPicker.CommitButtonText = resourceLoader.GetString("SetAsBackground");
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    await ListTools.ChangeListBackground(listId, file);

                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        bgImage.Source = bitmapImage;
                    }

                    AnimateOpacity(bgImage);
                }
            };
            panel.Children.Add(button);


            Expander fontExpander = new Expander()
            {
                Header = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Children = { new FontIcon() { Glyph = "\uE8D2", Margin = new Thickness(0, 0, 10, 0) }, new TextBlock() { Text = resourceLoader.GetString("ChangeFont") } } },
                Width = 300,
            };
            ListView fontChooser = new ListView()
            {
                SelectionMode = ListViewSelectionMode.Single,
                Height = 300,
                Width = 250,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            fontChooser.SelectionChanged += (s, a) =>
            {
                ListTools.ChangeListFont(listId, ((ListViewItem)fontChooser.SelectedItem).Tag.ToString());
                testname.FontFamily = new FontFamily(((ListViewItem)fontChooser.SelectedItem).Tag.ToString());
            };

            fontExpander.Content = fontChooser;

            foreach (string font in Microsoft.Graphics.Canvas.Text.CanvasTextFormat.GetSystemFontFamilies())
            {
                ListViewItem subfont = new ListViewItem() { Tag = font, Content = font, FontFamily = new FontFamily(font) };
                fontChooser.Items.Add(subfont);
            }
            ListMetadata data = ListTools.ReadList(listId).Metadata;
            fontChooser.SelectedItem = data.TitleFont;
            panel.Children.Add(fontExpander);


            Expander emojiExpander = new Expander()
            {
                Header = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Children = { new FontIcon() { Glyph = "\uE899", Margin = new Thickness(0, 0, 10, 0) }, new TextBlock() { Text = resourceLoader.GetString("ChangeEmoji") } } },
                Width = 300,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var emojiSource = new Tools.IncrementalEmojiSource();
            GridView content = new GridView
            {
                ItemsSource = emojiSource,
                ItemTemplate = (DataTemplate)Application.Current.Resources["EmojiBlock"],
                SelectionMode = ListViewSelectionMode.Single,
                Width = 250,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 200,
            };
            content.ItemsPanel = (ItemsPanelTemplate)Application.Current.Resources["WrapGridPanel"];

            var selectedEmoji = emojiSource.FirstOrDefault(e2 => e2.ToString() == data.Emoji);
            content.SelectedItem = selectedEmoji;

            AutoSuggestBox searchBox = new AutoSuggestBox() { PlaceholderText = resourceLoader.GetString("SearchBox/PlaceholderText"), Margin = new Thickness(0, 0, 0, 10), Width = 250, MaxWidth = 250, QueryIcon = new SymbolIcon(Symbol.Find) };
            searchBox.TextChanged += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    content.ItemsSource = new Tools.IncrementalEmojiSource();
                }
                else
                {
                    var searchTerm = searchBox.Text.ToLower();
                    content.ItemsSource = (new Tools()).Emojis.Where(e3 => e3.SearchTerms.Any(term => term.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            };

            content.SelectionChanged += (sender2, args) =>
            {
                if (!string.IsNullOrEmpty(listId) && (sender2 as GridView)?.SelectedItem != null)
                {
                    var selected = ((GridView)sender2).SelectedItem;
                    string emojiString = selected?.ToString();
                    ListTools.ChangeListEmoji(listId.Replace(".json", string.Empty), emojiString);
                }
            };
            StackPanel emojiPanel = new StackPanel() { Orientation = Orientation.Vertical };
            emojiPanel.Children.Add(searchBox);
            emojiPanel.Children.Add(content);
            emojiExpander.Content = emojiPanel;
            panel.Children.Add(emojiExpander);

            Flyout flyout = new Flyout();
            flyout.Content = panel;
            flyout.ShowAt(topoptions, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
        }
        private void RenameTask_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            var note = menuFlyoutItem.DataContext as ListTask;

            if (note == null)
            {
                return;
            }

            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("TaskName"),
                Text = note.Name,
                Margin = new Thickness(-10),
                Width = NameBox.ActualWidth + 40,
                MaxWidth = 400
            };

            Flyout flyout = new Flyout()
            {
                Content = input,
                Placement = FlyoutPlacementMode.Left
            };

            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };

            flyout.Closed += (s, args) =>
            {
                string newName = input.Text;
                if (!string.IsNullOrEmpty(newName) && newName != note.Name)
                {
                    note.Name = newName;

                    var data = ListTools.ReadList(listId);
                    var metadata = data.Metadata;
                    var tasks = data.Tasks;

                    int index = tasks.FindIndex(task => task.CreationDate == note.CreationDate);
                    tasks[index] = note;
                    ListTools.SaveList(listId, tasks, metadata);
                }
            };
            if (menuFlyoutItem.Tag is Button button)
            {
                flyout.ShowAt(button);
            }
        }

        private async void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask taskToDelete = (sender as MenuFlyoutItem)?.DataContext as ListTask;
            if (taskToDelete == null)
            {
                return;
            }
            await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
            {
                try
                {
                    foreach (AttachmentMetadata attachment in taskToDelete.Attachments)
                    {
                        await taskToDelete.RemoveAttachmentAsync(attachment);
                    }
                }
                catch (Exception ex) { Debug.WriteLine("[DeleteTask_Click] Exception occured: " + ex.Message); }
            });
            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;
            int index = tasks.FindIndex(task => task.CreationDate == taskToDelete.CreationDate);
            if (index != -1)
            {
                tasks.RemoveAt(index);
                ListTools.SaveList(listId, tasks, metadata);
                taskListView.Items.Remove(taskToDelete);
            }
            ListTools.SaveList(listId, tasks, metadata);
        }

        private void RenameList_Click(object sender, RoutedEventArgs e)
        {
            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("ListName"),
                Text = listname,
                Width = NameBox.ActualWidth + 55,
                MaxWidth = 400
            };
            Flyout flyout = new Flyout()
            {
                Content = new StackPanel()
                {
                    Children =
            {
                input
            },
                    Margin = new Thickness(-10),
                }
            };
            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };
            flyout.Closed += (s, args) =>
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
            flyout.ShowAt(topoptions, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Left });
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            ListTools.DeleteList(listId);
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
                    else { }
                });
            }
            catch (Exception ex) { Debug.WriteLine("[List export] Exception occured: " + ex.Message); }
        }

        private void RenameSubTask_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            var subTask = menuFlyoutItem.DataContext as ListTask;
            if (subTask == null)
            {
                return;
            }

            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("TaskName"),
                Text = subTask.Name,
                Margin = new Thickness(-10),
                Width = NameBox.ActualWidth + 65,
                MaxWidth = 400
            };

            Flyout flyout = new Flyout()
            {
                Content = input,
                Placement = FlyoutPlacementMode.Left
            };

            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };

            flyout.Closed += (s, args) =>
            {
                if (!string.IsNullOrEmpty(input.Text))
                {
                    int index = tasks.FindIndex(task => task.CreationDate == subTask?.ParentCreationDate);
                    if (index > -1)
                    {
                        ListTask parentTask = tasks[index];

                        ListTask taskToRemove = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                        if (taskToRemove != null)
                        {
                            var subTaskToUpdate = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                            if (subTaskToUpdate != null)
                            {
                                subTaskToUpdate.Name = input.Text;
                            }
                            tasks[index] = parentTask;
                            ListTools.SaveList(listId, tasks, metadata);
                            ((ListTask)taskListView.Items[index]).SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate).Name = input.Text;
                        }
                    }
                }
            };

            if (menuFlyoutItem.Tag is Button button)
            {
                flyout.ShowAt(button);
            }
        }

        private void DeleteSubTask_Click(object sender, RoutedEventArgs e)
        {
            ListTask subTask = (sender as MenuFlyoutItem)?.DataContext as ListTask;
            if (subTask == null)
            {
                return;
            }

            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            int index = tasks.FindIndex(task => task.CreationDate == subTask?.ParentCreationDate);
            if (index > -1)
            {
                ListTask parentTask = tasks[index];

                ListTask taskToRemove = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                if (taskToRemove != null)
                {
                    parentTask.SubTasks.Remove(taskToRemove);
                    tasks[index] = parentTask;
                    (taskListView.Items[index] as ListTask).SubTasks = parentTask.SubTasks;
                }
                else
                {
                }
            }
            else
            {
            }
            ListTools.SaveList(listId, tasks, metadata);
        }

        private async void CompactOverlay_Click(object sender, RoutedEventArgs e)
        {
            AppWindow window = await AppWindow.TryCreateAsync();
            window.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);

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
            window.TitleBar.ExtendsContentIntoTitleBar = true;
            cobtn.Visibility = Visibility.Collapsed;
            await window.TryShowAsync();
            this.Frame.Navigate(typeof(COClosePage));
        }



        private void TaskNameText_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            ListTask note = textBlock.DataContext as ListTask;

            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("TaskName"),
                Text = note?.Name,
                Margin = new Thickness(-10),
                Width = NameBox.ActualWidth + 40,
                MaxWidth = 400
            };

            Flyout flyout = new Flyout()
            {
                Content = input,
                Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
            };

            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };

            flyout.Closed += (s, args) =>
            {
                string newName = input.Text;
                if (!string.IsNullOrEmpty(newName) && newName != note.Name)
                {
                    note.Name = newName;

                    var data = ListTools.ReadList(listId);
                    var metadata = data.Metadata;
                    var tasks = data.Tasks;

                    int index = tasks.FindIndex(task => task.CreationDate == note.CreationDate);
                    tasks[index] = note;
                    ListTools.SaveList(listId, tasks, metadata);
                }
            };
            flyout.ShowAt(sender as TextBlock);
        }

        private void SubTaskNameText_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TextBlock textBlock = (TextBlock)sender;
            var subTask = textBlock.DataContext as ListTask;
            if (subTask == null)
            {
                return;
            }

            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("TaskName"),
                Text = subTask.Name,
                Margin = new Thickness(-10),
                Width = NameBox.ActualWidth + 65,
                MaxWidth = 400
            };

            Flyout flyout = new Flyout()
            {
                Content = input,
                Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
            };

            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };

            flyout.Closed += (s, args) =>
            {
                if (!string.IsNullOrEmpty(input.Text))
                {
                    int index = tasks.FindIndex(task => task.CreationDate == subTask?.ParentCreationDate);
                    if (index > -1)
                    {
                        ListTask parentTask = tasks[index];

                        ListTask taskToRemove = parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate);
                        if (taskToRemove != null)
                        {
                            parentTask.SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate).Name = input.Text;
                            tasks[index] = parentTask;
                            (taskListView.Items[index] as ListTask).SubTasks.FirstOrDefault(t => t.CreationDate == subTask.CreationDate).Name = input.Text;
                        }
                        else
                        {
                        }
                    }
                }
            };

            flyout.ShowAt(sender as TextBlock);
        }

        private void testname_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TextBox input = new TextBox()
            {
                PlaceholderText = resourceLoader.GetString("ListName"),
                Text = listname,
                Width = NameBox.ActualWidth + 55,
                MaxWidth = 400
            };
            Flyout flyout = new Flyout()
            {
                Content = new StackPanel()
                {
                    Children =
            {
                input
            },
                    Margin = new Thickness(-10),
                }
            };
            input.KeyDown += (s, args) => { if (args.Key == VirtualKey.Enter) { flyout.Hide(); } };
            flyout.Closed += (s, args) =>
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
            flyout.ShowAt(sender as TextBlock, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft });
        }

        private async void TPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                if (File.Exists(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "bg_" + listId)))
                {
                    var file = await ApplicationData.Current.LocalFolder.GetFileAsync("bg_" + listId);
                    var uri = new Uri(file.Path);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    {
                        var bitmapImage = new BitmapImage(uri);
                        bgImage.Source = bitmapImage;
                        AnimateOpacity(bgImage);
                    });
                }
            });
        }

        private void testname_Loaded(object sender, RoutedEventArgs e)
        {
            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            try
            {
                testname.FontFamily = new FontFamily(metadata.TitleFont);
            }
            catch
            {
                testname.FontFamily = new FontFamily("Segoe UI Variable");
                metadata.TitleFont = "Segoe UI Variable";
            }
        }

        private void NameBox_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeWidth(sender);
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
                cobtn.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuFlyoutItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListTools.isAWOpen)
            {
                (sender as MenuFlyoutItem).Visibility = Visibility.Collapsed;
            }
        }

        private void TaskThreeDots_Loaded(object sender, RoutedEventArgs e)
        {
            tasks = ListTools.ReadList(listId).Tasks;
            Button button = sender as Button;
            ((MenuFlyout)button?.Flyout).Items[0].Tag = button;
            ListTask boundTask = button.DataContext as ListTask;
            ListTask task;
            try
            {
                task = tasks.FirstOrDefault(t => t.CreationDate == boundTask.CreationDate);
                if (task == null)
                {
                    return;
                }
                MenuFlyout flyout = button.Flyout as MenuFlyout;
                UpdateFlyoutMenu(flyout, task, button);
            }
            catch (Exception ex) { Debug.WriteLine("[TaskThreeDots_Loaded] Exception occured: " + ex.Message); }
        }

        private void SubTaskThreeDots_Loaded(object sender, RoutedEventArgs e)
        {
            ((sender as Button).Flyout as MenuFlyout).Items[0].Tag = sender;
        }


        private void AnimateOpacity(UIElement element)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 0.4,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Begin();
        }
        private void ChangeWidth(object sender)
        {
            double newWidth = (sender as Rectangle)?.ActualWidth ?? 0;

            foreach (var task in taskListView.Items)
            {
                ListViewItem taskContainer = taskListView.ContainerFromItem(task) as ListViewItem;
                if (taskContainer == null)
                {
                    continue;
                }

                var expander = FindDescendant<Expander>(taskContainer, "rootGrid");
                if (expander != null)
                {
                    if (expander.Header is FrameworkElement headerElement)
                    {
                        var taskNameText = FindDescendant<Grid>(headerElement, "TaskNameTextCont");
                        if (taskNameText != null)
                        {
                            taskNameText.Width = newWidth;
                        }
                    }

                    if (expander.Content is FrameworkElement contentElement)
                    {
                        var subTaskList = FindDescendant<ListView>(contentElement, "SubTaskListView");
                        if (subTaskList != null)
                        {
                            var listTask = task as ListTask;
                            if (listTask?.SubTasks != null)
                            {
                                foreach (var subTask in listTask.SubTasks)
                                {
                                    ListViewItem subTaskContainer = subTaskList.ContainerFromItem(subTask) as ListViewItem;
                                    if (subTaskContainer != null)
                                    {
                                        var subTaskNameText = FindDescendant<TextBlock>(subTaskContainer, "TaskNameText");
                                        if (subTaskNameText != null)
                                        {
                                            subTaskNameText.Width = newWidth;
                                        }
                                    }
                                }
                            }
                        }
                        var addSubTaskBox = FindDescendant<AutoSuggestBox>(contentElement, "AddSubTaskBox");
                        if (addSubTaskBox != null)
                        {
                            addSubTaskBox.Width = newWidth + 120;
                            addSubTaskBox.MaxWidth = newWidth + 120;
                        }
                        var attachmentGrid = FindDescendant<ScrollViewer>(contentElement, "AttachmentScroll");
                        if (attachmentGrid != null)
                        {
                            attachmentGrid.Width = newWidth + 120;
                            attachmentGrid.MaxWidth = newWidth + 120;
                        }
                    }
                }
            }
        }
        private T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is FrameworkElement feChild && feChild.Name == name)
                {
                    return (T)child;
                }
                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        private T FindDescendant<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T frameworkElement && frameworkElement.Name == name)
                    return frameworkElement;
                var result = FindDescendant<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
        public ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
        public string listname { get; set; }
        public string listId { get; set; }
        private static SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
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
                    if (date > DateTime.Now) { task.AddReminder(date); task.SetReminderText(); }
                    timeChooser.Hide();
                };

                addReminderItem.Click += (s, args) =>
                {
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

        private void ChangeWidthAttachments(object sender)
        {
            if (sender is ScrollViewer sv)
            {
                sv.Width = NameBox.ActualWidth + 73;
            }
        }
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ChangeWidth(NameBox);
        }

        private void TaskPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ListTools.ListRenamedEvent -= ListRenamed;
        }

        private void AWClosed(AppWindow sender, AppWindowClosedEventArgs args)
        {
            ListTools.isAWOpen = false;
            cobtn.Visibility = Visibility.Visible;
            this.Frame.Navigate(typeof(EmptyPage));
        }

        private void ListRenamed(string oldname, string newname, string emoji)
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            listId = e.Parameter.ToString();

            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;
            
            if (e.Parameter != null)
            {
                string name = metadata.Name;
                testname.Text = name;
                listname = name;
            }
            base.OnNavigatedTo(e);

            await Task.Run(async () =>
            {
                if (tasks != null && data != null)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    {
                        foreach (ListTask task in tasks)
                        {
                            task.LoadAttachments(listId);
                            taskListView.Items.Add(task);
                        }
                    });
                }
            });
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.QueryText))
            {
                var data = ListTools.ReadList(listId);
                var metadata = data.Metadata;
                var tasks = data.Tasks;

                ListTask task = new ListTask(listId)
                {
                    Name = args.QueryText,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                tasks.Add(task);
                sender.Text = string.Empty;
                taskListView.Items.Add(task);
                ListTools.SaveList(listId, tasks, metadata);
            }
        }

        private void TaskStateChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null)
            {
                return;
            }

            var tasktoChange = checkBox.DataContext as ListTask;
            if (tasktoChange == null)
            {
                return;
            }

            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            try
            {
                if (checkBox.IsChecked.HasValue)
                {
                    tasktoChange.IsDone = checkBox.IsChecked.Value;
                    int index = tasks.FindIndex(task => task.CreationDate == tasktoChange.CreationDate);
                    if (index != -1)
                    {
                        tasks[index] = tasktoChange;
                        ListTools.SaveList(listId, tasks, metadata);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Task state change] Exception occurred: " + ex.Message);
            }
        }
        private void NameBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeWidth(sender);
        }

        private void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrEmpty((sender as AutoSuggestBox).Text))
            {
                var data = ListTools.ReadList(listId);
                var metadata = data.Metadata;
                var tasks = data.Tasks;

                ListTask task = new ListTask(listId)
                {
                    Name = (sender as AutoSuggestBox).Text,
                    CreationDate = DateTime.Now,
                    IsDone = false
                };
                tasks.Add(task);
                taskListView.Items.Add(task);
                (sender as AutoSuggestBox).Text = string.Empty;
                ListTools.SaveList(listId, tasks, metadata);
            }
        }

        private void TaskAdded_Grid(object sender, RoutedEventArgs e)
        {
            ChangeWidth(NameBox);
            double newWidth = NameBox.ActualWidth;
            Expander expander = sender as Expander;
            if (expander.Header is FrameworkElement headerElement)
            {
                var taskNameText = FindDescendant<Grid>(headerElement, "TaskNameTextCont");
                if (taskNameText != null)
                {
                    taskNameText.Width = newWidth;
                }
            }

            if (expander.Content is FrameworkElement contentElement)
            {
                var subTaskList = FindDescendant<ListView>(contentElement, "SubTaskListView");
                if (subTaskList != null)
                {
                    var listTask = (sender as Expander).DataContext as ListTask;
                    if (listTask?.SubTasks != null)
                    {
                        foreach (var subTask in listTask.SubTasks)
                        {
                            ListViewItem subTaskContainer = subTaskList.ContainerFromItem(subTask) as ListViewItem;
                            if (subTaskContainer != null)
                            {
                                var subTaskNameText = FindDescendant<TextBlock>(subTaskContainer, "TaskNameText");
                                if (subTaskNameText != null)
                                {
                                    subTaskNameText.Width = newWidth;
                                }
                            }
                        }
                    }
                }
                var addSubTaskBox = FindDescendant<AutoSuggestBox>(contentElement, "AddSubTaskBox");
                if (addSubTaskBox != null)
                {
                    addSubTaskBox.Width = newWidth + 120;
                    addSubTaskBox.MaxWidth = newWidth + 120;
                }


                addSubTaskBox.Loaded += (s, args) =>
                {

                    if (subTaskList != null)
                    {
                        var listTask = (sender as Expander).DataContext as ListTask;
                        if (listTask?.SubTasks != null)
                        {
                            foreach (var subTask in listTask.SubTasks)
                            {
                                ListViewItem subTaskContainer = subTaskList.ContainerFromItem(subTask) as ListViewItem;
                                if (subTaskContainer != null)
                                {
                                    var subTaskNameText = FindDescendant<TextBlock>(subTaskContainer, "TaskNameText");
                                    if (subTaskNameText != null)
                                    {
                                        subTaskNameText.Width = newWidth;
                                    }
                                }
                            }
                        }
                    }

                    addSubTaskBox.Width = NameBox.ActualWidth + 120;
                    addSubTaskBox.MaxWidth = NameBox.ActualWidth + 120;
                };
            }

        }

        private async void SubTaskStateChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null)
                return;

            var taskToChange = checkBox.DataContext as ListTask;
            if (taskToChange == null)
                return;

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

        private void SubTaskBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var data = ListTools.ReadList(listId);
            var metadata = data.Metadata;
            var tasks = data.Tasks;

            ListTask parent = sender.DataContext as ListTask;
            int index = tasks.FindIndex(task => task.CreationDate == parent?.CreationDate);
            if (parent != null)
            {

                if (!string.IsNullOrEmpty(sender.Text))
                {
                    ListTask task2add = new ListTask(listId)
                    {
                        CreationDate = DateTime.Now,
                        ParentCreationDate = parent.CreationDate,
                        IsDone = false,
                        Name = sender.Text,
                        SubTasks = new ObservableCollection<ListTask>()
                    };
                    try
                    {
                        parent.SubTasks.Add(task2add);
                        sender.Text = string.Empty;
                    }
                    catch
                    {
                        if (parent.SubTasks == null)
                        {
                            parent.SubTasks = new ObservableCollection<ListTask> { task2add };
                        }
                    }
                }
            }
            if (index > -1)
            {
                tasks[index] = parent;
                ListTools.SaveList(listId, tasks, metadata);
            }
        }

        private void rootGrid_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            double newWidth = NameBox.ActualWidth;
            Expander expander = sender;
            if (expander.Header is FrameworkElement headerElement)
            {
                var taskNameText = FindDescendant<Grid>(headerElement, "TaskNameTextCont");
                if (taskNameText != null)
                {
                    taskNameText.Width = newWidth;
                }
            }

            if (expander.Content is FrameworkElement contentElement)
            {
                var subTaskList = FindDescendant<ListView>(contentElement, "SubTaskListView");
                if (subTaskList != null)
                {
                    var listTask = sender.DataContext as ListTask;
                    if (listTask?.SubTasks != null)
                    {
                        foreach (var subTask in listTask.SubTasks)
                        {
                            ListViewItem subTaskContainer = subTaskList.ContainerFromItem(subTask) as ListViewItem;
                            if (subTaskContainer != null)
                            {
                                var subTaskNameText = FindDescendant<TextBlock>(subTaskContainer, "TaskNameText");
                                if (subTaskNameText != null)
                                {
                                    subTaskNameText.Width = newWidth;
                                }
                            }
                        }
                    }
                }
                var addSubTaskBox = FindDescendant<AutoSuggestBox>(contentElement, "AddSubTaskBox");
                if (addSubTaskBox != null)
                {
                    addSubTaskBox.Width = newWidth + 120;
                    addSubTaskBox.MaxWidth = newWidth + 120;
                }
            }
        }
        private void AttachmentListView_Loaded(object sender, RoutedEventArgs e) { ChangeWidthAttachments(sender); }
        private void AttachmentListView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) { ChangeWidthAttachments(sender); }
        private void AttachmentListView_SizeChanged(object sender, SizeChangedEventArgs e) { ChangeWidthAttachments(sender); }

        private void ReminderText_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ((sender as TextBlock).DataContext as ListTask).SetReminderText();
            }
            catch
            {
                // it crashes sometimes but thats not a big deal, it just means the task is null or something
            }
            finally
            {
                DispatcherTimer timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(1), // should be 60, bit of a workaround
                };
                timer.Tick += (s, args) =>
                {
                    try
                    {
                        ((sender as TextBlock).DataContext as ListTask).SetReminderText();
                    }
                    catch
                    {

                    }
                };
                timer.Start();
            }
        }
    }
}
