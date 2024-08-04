using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials.UI;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using Taskie.Services.Shared;
using Taskie.ViewModels;
using Taskie.Views.UWP.Pages;
using Taskie.Views.UWP.Services;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Taskie.Views.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IRecipient<RemovingTaskListViewModelMessage>
    {
        private MainViewModel MainViewModel => (MainViewModel)DataContext;
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(600, 500));
            InitializeComponent();
            InitializeTitleBar();
            CheckSecurity();

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private async void CheckSecurity()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();

            var authUsed = SettingsService.Instance.Get<bool>(SettingsKeys.IsAuthUsed);
            var isPro = SettingsService.Instance.Get<bool>(SettingsKeys.IsPro);

            if (!authUsed)
            {
                TaskListListView.Visibility = Visibility.Visible;
                return;
            }

            var denied = availability != UserConsentVerifierAvailability.Available && authUsed;

            if (!isPro || denied)
            {
                SettingsService.Instance.Set(SettingsKeys.IsAuthUsed, false);

                ContentDialog contentDialog = new()
                {
                    Title = _resourceLoader.GetString("AuthDisabledTitle"),
                    Content = _resourceLoader.GetString("AuthDisabledDescription"),
                    PrimaryButtonText = _resourceLoader.GetString("OK")
                };

                await contentDialog.ShowAsync();

                TaskListListView.Visibility = Visibility.Visible;

                return;
            }

            UserConsentVerificationResult consent = await UserConsentVerifier.RequestVerificationAsync(_resourceLoader.GetString("LoginMessage"));
            if (consent != UserConsentVerificationResult.Verified)
            {
                Application.Current.Exit();
            }

            TaskListListView.Visibility = Visibility.Visible;
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

            sender.ItemsSource = MainViewModel.TaskListViewModels
                .Where(x => x.Name.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase));
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is TaskListViewModel taskListViewModel)
            {
                sender.Text = "";
                searchbox.ItemsSource = new List<string>();
                TaskListListView.SelectedItem = taskListViewModel;
            }
        }

        private void Navigation_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Implement
        }

        private async void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new();
            Frame frame = new();
            frame.Navigate(typeof(UpgradeDialogContentPage));
            dialog.Content = frame;
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.PrimaryButtonText = _resourceLoader.GetString("UpgradeText/Text");
            dialog.PrimaryButtonClick += UpgradeToPro;
            dialog.SecondaryButtonText = _resourceLoader.GetString("Cancel");
            await dialog.ShowAsync();
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

        private async void UpgradeToPro(object _, object __)
        {
            var isPro = SettingsService.Instance.Get<bool>(SettingsKeys.IsPro);

            var builder = new ToastContentBuilder()
                .AddText(isPro ? "Pro status revoked." : "Pro status granted.");
            builder.Show();
            
            SettingsService.Instance.Set(SettingsKeys.IsPro, !isPro);
            
            await CoreApplication.RequestRestartAsync("Pro status changed.");
        }
    }
}