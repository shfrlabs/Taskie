using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Taskie.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taskie.Views.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IRecipient<RemovingTaskListViewModelMessage>
    {
        private MainViewModel MainViewModel => (MainViewModel)DataContext;

        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(600, 500));
            InitializeComponent();
            InitializeTitleBar();
            
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private void InitializeTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(TitleBarGrid);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(sender.Text))
            {
                sender.IsSuggestionListOpen = false;
                sender.ItemsSource = new List<string>();
                return;
            }

            sender.ItemsSource = MainViewModel.TaskListViewModels.Select(x => x.Name.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase));
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // TODO: Implement
        }

        private void Navigation_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Implement
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement
        }

        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Implement
        }

        private void TaskListListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView listView)
            {
                return;
            }
            
            if (listView.SelectedItem is not TaskListViewModel taskListViewModel)
            {
                ContentFrame.Navigate(typeof(TaskListPlaceholderPage), null, new DrillInNavigationTransitionInfo());
                return;
            }

            // TODO: Play sliding animation in up/down direction depending on relative index change
            ContentFrame.Navigate(typeof(TaskListPage), taskListViewModel);
        }

        public void Receive(RemovingTaskListViewModelMessage message)
        {
            var index = MainViewModel.TaskListViewModels.IndexOf(message.Value);
            var desiredIndex = Math.Clamp(index + 1, 0, MainViewModel.TaskListViewModels.Count - 1);

            if (MainViewModel.TaskListViewModels.Count == 0)
            {
                TaskListListView.SelectedIndex = -1;
                return;
            }

            TaskListListView.SelectedIndex = desiredIndex;
        }
    }
}