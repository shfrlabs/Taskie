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
using Taskie.SettingsPages;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;

namespace Taskie
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            addPages();
            settingPageList.SelectedItem = settingPageList.Items.FirstOrDefault();
            contentFrame.Navigate(typeof(AppearancePage));
            ActualThemeChanged += SettingsPage_ActualThemeChanged;
        }

        private void SettingsPage_ActualThemeChanged(FrameworkElement sender, object args)
        {
            (this.Background as AcrylicBrush).TintColor = (Color)Application.Current.Resources["SystemAltHighColor"];
            (this.Background as AcrylicBrush).FallbackColor = (Color)Application.Current.Resources["SystemAltLowColor"];
            (rect2.Fill as SolidColorBrush).Color = (Color)Application.Current.Resources["SystemAltLowColor"];
        }

        private class SettingCategory
        {
            public string Emoji { get; set; }
            public string Name { get; set; }
            public string Page { get; set; }
        }
        private void addPages()
        {
            ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            List<SettingCategory> settingCategories = new List<SettingCategory>() {
                new SettingCategory() { Emoji = "🎨", Name = resourceLoader.GetString("AppearanceCategory"), Page = "AppearancePage" },
                new SettingCategory() { Emoji = "☁️", Name = resourceLoader.GetString("BackupsCategory"), Page = "ExportImportPage" },
                new SettingCategory() { Emoji = "🔒", Name = resourceLoader.GetString("SecurityCategory"), Page = "SecurityPage" },
                new SettingCategory() { Emoji = "ℹ️", Name = resourceLoader.GetString("AboutCategory"), Page = "AboutPage" }
            };
            settingPageList.ItemsSource = settingCategories;
        }

        private void settingPageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCategory = (sender as ListView).SelectedItem as SettingCategory;
            if (selectedCategory != null)
            {
                Type pageType = Type.GetType($"Taskie.SettingsPages.{selectedCategory.Page}");
                if (pageType != null)
                {
                    contentFrame.Navigate(pageType);
                }
            }
        }
    }
}
