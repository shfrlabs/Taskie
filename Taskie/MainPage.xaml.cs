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
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using System.Reflection;
using System.Linq;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;

namespace Taskie
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(600, 500));
            InitializeComponent();
            SetupTitleBar();
            SetupNavigationMenu();
            Navigation.Height = rectlist.ActualHeight;
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
                content.Margin = new Thickness(-7, 0, 0, 0);
                content.Children.Add(new FontIcon() { Glyph = "📄", FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe UI Emoji"), FontSize = 14 });
                content.Children.Add(new TextBlock { Text = listName, Margin = new Thickness(12, 0, 0, 0) });
                Navigation.Items.Add(new ListViewItem() { Tag = listName, Content = content, HorizontalContentAlignment = HorizontalAlignment.Left });
            }
        }

        private void UpdateLists(string name)
        {
            Navigation.Items.Clear();
            SetupNavigationMenu();
            contentFrame.Content = null;
        }

        public string RequestedName = "";

        private void ShowAddItemDialog(object sender, RoutedEventArgs e)
        {
            Flyout flyout = new Flyout();
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Vertical;
            flyout.Content = panel;
            panel.Children.Add(new TextBlock() { Text = "Create a list" });
            TextBox text = new TextBox() { Margin = new Thickness(0, 20, 0, 5) };
            text.MinWidth = 200;
            text.PlaceholderText = "List name";
            text.TextChanged += Text_TextChanged;
            panel.Children.Add(text);
            Button button = new Button() { Content = "Create" };
            button.Click += AddList;
            panel.Children.Add(button);
            AddItemBtn.Flyout = flyout;
            flyout.ShowAt(AddItemBtn);
        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            RequestedName = (sender as TextBox).Text;
        }

        private void AddList(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Ok" + RequestedName);
            Tools.CreateList(RequestedName);
            RequestedName = null;
            UpdateLists(RequestedName);
            AddItemBtn.Flyout.Hide();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AppWindow window = await AppWindow.TryCreateAsync();
            window.Title = "Settings";
            Frame settingsContent = new Frame();
            settingsContent.Navigate(typeof(SettingsPage));
            ElementCompositionPreview.SetAppWindowContent(window, settingsContent);
            await window.TryShowAsync();
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

        private void rectlist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Navigation.Height = rectlist.ActualHeight;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            sender.ItemsSource = Array.FindAll<string>(Tools.GetLists(), s => s.Contains(sender.Text));
            if (sender.Text == null) { sender.IsSuggestionListOpen = false; }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            contentFrame.Navigate(typeof(TaskPage), Array.FindAll<string>(Tools.GetLists(), s => s.Contains(sender.Text))[0]);
            foreach (ListViewItem item in Navigation.Items)
            {
                Debug.WriteLine(item.Tag.ToString());
                Debug.WriteLine(sender.Text);
                if (item.Tag.ToString().Contains(sender.Text)) { Navigation.SelectedItem = item; break; }
            }
            sender.Text = "";
            searchbox.ItemsSource = null;

        }
    }
}
