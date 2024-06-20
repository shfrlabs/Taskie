using System;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using TaskieLib;
using Windows.Networking;
using Windows.UI.Xaml.Controls.Primitives;

namespace Taskie
{
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer dialogTimer;

        public MainPage()
        {
            InitializeComponent();
            SetupTitleBar();
            SetupNavigationMenu();
            TaskieLib.Tools.ListCreatedEvent += UpdateLists;
            Tools.ListDeletedEvent += ListDeleted;
            Tools.ListRenamedEvent += ListRenamed;
        }

        private void ListRenamed(string oldname, string newname)
        {
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString() == oldname)
                    {
                        navigationItem.Tag = newname;
                        navigationItem.Content = newname;
                        break;
                    }
                }
            }
        }

        private void ListDeleted(string name)
        {
            contentFrame.Content = new StackPanel();
            Navigation.SelectedItem = null;
            foreach (var item in Navigation.Items)
            {
                if (item is ListViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString() == name)
                    {
                        Navigation.Items.Remove(item);
                        break;
                    }
                }
            }
        }

        private void SetupTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(TTB);
        }

        private void SetupNavigationMenu()
        {
            foreach (string listName in TaskieLib.Tools.GetLists())
            {
                StackPanel content = new StackPanel();
                content.Orientation = Orientation.Horizontal;
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14});
                content.Children.Add(new TextBlock { Text = listName, Margin = new Thickness(12, 0, 0, 0) });
                Navigation.Items.Add(new ListViewItem() { Tag = listName, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
            }
        }

        private void UpdateLists(string name)
        {
            if (dialogTimer != null && dialogTimer.IsEnabled)
                dialogTimer.Stop();
            dialogTimer = new DispatcherTimer();
            dialogTimer.Interval = TimeSpan.FromMilliseconds(500);
            dialogTimer.Tick += (s, e) =>
            {
                dialogTimer.Stop();
                StackPanel content = new StackPanel();
                content.VerticalAlignment = VerticalAlignment.Center;
                content.Orientation = Orientation.Horizontal;
                content.Margin = new Thickness(-7, 0, 0, 0);
                content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14});
                content.Children.Add(new TextBlock { Text = name, Margin = new Thickness(12, 0, 0, 0) });
                Navigation.Items.Add(new ListViewItem() { Tag = name, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
            };
            dialogTimer.Start();
        }

        public string RequestedName = "";

        private async Task ShowAddItemDialog()
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Create a list";
            TextBox text = new TextBox();
            text.PlaceholderText = "List name";
            text.TextChanged += Text_TextChanged;
            dialog.Content = text;
            dialog.PrimaryButtonText = "Create";
            dialog.SecondaryButtonText = "Cancel";
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
            await dialog.ShowAsync();
        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            RequestedName = (sender as TextBox).Text;
        }

        private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Tools.CreateList(RequestedName);
            RequestedName = null;
        }

        private void AddItemBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExportImportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Navigation_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ListView NavList = sender as ListView;
            var selectedItem = NavList.SelectedItem as ListViewItem;
            if (selectedItem != null && selectedItem.Tag is string tag)
            {
                contentFrame.Navigate(typeof(TaskPage), tag);
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
