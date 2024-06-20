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

namespace Taskie
{
    public sealed partial class TaskPage : Page
    {
        public TaskPage()
        {
            this.InitializeComponent();
        }

        public string listname { get; set; }



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

        private async void RenameTask_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlyoutItem = (MenuFlyoutItem)sender;
            var note = menuFlyoutItem.DataContext as ListTask;
            TextBox input = new TextBox() { PlaceholderText = "Task name", Text = note.Name };
            ContentDialog dialog = new ContentDialog() { Title = "Rename task", PrimaryButtonText = "OK", SecondaryButtonText = "Cancel", Content = input };
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
            TextBox input = new TextBox() { PlaceholderText = "List name", Text = listname };
            ContentDialog dialog = new ContentDialog() { Title = "Rename list", PrimaryButtonText = "OK", SecondaryButtonText = "Cancel", Content = input };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string text = input.Text;
                Tools.RenameList(listname, text);
                listname = text;
                testname.Text = listname;
            }
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
    }
}
