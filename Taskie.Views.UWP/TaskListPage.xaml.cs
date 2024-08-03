using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using Taskie.ViewModels;

namespace Taskie.Views.UWP
{
    public sealed partial class TaskListPage : Page
    {
        private TaskListViewModel TaskListViewModel => (TaskListViewModel)DataContext;
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        public TaskListPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is TaskListViewModel taskListViewModel)
            {
                DataContext = taskListViewModel;
            }

            base.OnNavigatedTo(e);
        }

        private async void Rename_OnClick(object sender, RoutedEventArgs e)
        {
            TextBox input = new()
            {
                PlaceholderText = _resourceLoader.GetString("TaskName"),
                Text = TaskListViewModel.Name
            };
            
            ContentDialog dialog = new()
            {
                Title = _resourceLoader.GetString("RenameTask/Text"),
                PrimaryButtonText = _resourceLoader.GetString("OK"),
                SecondaryButtonText = _resourceLoader.GetString("Cancel"),
                Content = input
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            TaskListViewModel.Name = input.Text;
        }

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new RemovingTaskListViewModelMessage(TaskListViewModel));
            WeakReferenceMessenger.Default.Send(new RemoveTaskListViewModelMessage(TaskListViewModel));
        }
    }
}