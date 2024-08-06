using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Taskie.Views.UWP.Pages.Settings;

namespace Taskie.Views.UWP.Pages
{
    public class SettingCategory
    {
        public string Emoji { get; init; }
        public string Name { get; init; }
        public string Page { get; init; }
    }

    public sealed partial class SettingsPage : Page
    {
        public SettingCategory[] SettingCategories { get; }
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView();

        public SettingsPage()
        {
            this.InitializeComponent();
            SettingCategories =
            [
                new SettingCategory
                {
                    Emoji = "🎨",
                    Name = _resourceLoader.GetString("AppearanceCategory"),
                    Page = "AppearancePage"
                },
                new SettingCategory
                {
                    Emoji = "☁️",
                    Name = _resourceLoader.GetString("BackupsCategory"),
                    Page = "ExportImportPage"
                },
                new SettingCategory
                {
                    Emoji = "🔒",
                    Name = _resourceLoader.GetString("SecurityCategory"),
                    Page = "SecurityPage"
                },
                new SettingCategory
                {
                    Emoji = "ℹ️",
                    Name = _resourceLoader.GetString("AboutCategory"),
                    Page = "AboutPage"
                },
            ];

            contentFrame.Navigate(typeof(AppearancePage));
            ActualThemeChanged += SettingsPage_ActualThemeChanged;
            DataContext = this;
        }

        private void SettingsPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            (this.Background as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (this.Background as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
            (rect2.Fill as SolidColorBrush).Color = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView { SelectedItem: SettingCategory settingCategory }) return;

            var pageType = Type.GetType($"Taskie.Views.UWP.Pages.Settings.{settingCategory.Page}");

            if (pageType != null)
            {
                contentFrame.Navigate(pageType);
            }
        }
    }
}