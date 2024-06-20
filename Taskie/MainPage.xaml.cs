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
            foreach (var item in Navigation.MenuItems)
            {
                if (item is Microsoft.UI.Xaml.Controls.NavigationViewItem navigationItem)
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
            foreach (var item in Navigation.MenuItems)
            {
                if (item is Microsoft.UI.Xaml.Controls.NavigationViewItem navigationItem)
                {
                    if (navigationItem.Tag.ToString() == name)
                    {
                        Navigation.MenuItems.Remove(item);
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
            Navigation.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Tag = "AddItem", Content = "Add a new list", Icon = new SymbolIcon(Symbol.Add) });
            Navigation.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Tag = "ExportImport", Content = "Export/Import lists", Icon = new SymbolIcon(Symbol.Import) });
            Navigation.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItemSeparator() { Content = "Your lists" });
            foreach (string listName in TaskieLib.Tools.GetLists())
            {
                Navigation.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Tag = listName, Content = listName, Icon = new SymbolIcon(Symbol.Document) });
            }
        }

        private void UpdateLists(string name)
        {
            if (dialogTimer != null && dialogTimer.IsEnabled)
                dialogTimer.Stop();
            dialogTimer = new DispatcherTimer();
            dialogTimer.Interval = TimeSpan.FromMilliseconds(500);
            dialogTimer.Tick += async (s, e) =>
            {
                dialogTimer.Stop();
                Microsoft.UI.Xaml.Controls.NavigationViewItem item = new Microsoft.UI.Xaml.Controls.NavigationViewItem() { Tag = name, Content = name, Icon = new SymbolIcon(Symbol.Document) };
                Navigation.MenuItems.Add(item);
            };
            dialogTimer.Start();
        }
        private async void Navigation_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            if (selectedItem != null && selectedItem.Tag is string tag)
            {
                if ((string)selectedItem.Tag == "AddItem")
                {
                    contentFrame.Content = null;
                    sender.SelectedItem = null;
                    await ShowAddItemDialog();
                }
                else if ((string)selectedItem.Tag == "ExportImport")
                {
                    contentFrame.Navigate(typeof(ExportImportPage));
                }
                else if (args.IsSettingsSelected)
                {
                }
                else
                {
                    contentFrame.Navigate(typeof(TaskPage), tag);
                }
            }
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
    }
}
