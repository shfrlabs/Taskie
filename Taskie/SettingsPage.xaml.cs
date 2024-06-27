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
        }

        private class SettingCategory
        {
            public string Emoji { get; set; }
            public string Name { get; set; }
            public string Page { get; set; }
        }
        private void addPages()
        {
            List<SettingCategory> settingCategories = new List<SettingCategory>() {
                new SettingCategory() { Emoji = "🎨", Name = "Appearance", Page = "AppearancePage" },
                new SettingCategory() { Emoji = "☁️", Name = "Backups", Page = "ExportImportPage" },
                new SettingCategory() { Emoji = "🔒", Name = "Security", Page = "SecurityPage" },
                new SettingCategory() { Emoji = "ℹ️", Name = "About", Page = "AboutPage" }
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
