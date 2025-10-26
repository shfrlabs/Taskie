using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Services.Store;
using Windows.Storage;
using Windows.UI.Xaml;

namespace TaskieLib
{
    public static class Settings
    {
        private static IPropertySet savedSettings = ApplicationData.Current.LocalSettings.Values;

        // Application theme (light/dark)
        public static string Theme
        {
            get
            {
                if (savedSettings.ContainsKey("theme"))
                {
                    var theme = savedSettings["theme"] as string;
                    if (theme == "Light" || theme == "Dark" || theme == "System")
                    {
                        return theme;
                    }
                }
                return "System";
            }
            set
            {
                if (value == "Light" || value == "Dark" || value == "System")
                {
                    savedSettings["theme"] = value;
                }
                Tools.SetTheme(value);
            }
        }

        // Authentication via Windows Hello
        public static bool isAuthUsed
        {
            get
            {
                return savedSettings.ContainsKey("auth") && (string)savedSettings["auth"] == "1";
            }
            set
            {
                savedSettings["auth"] = value ? "1" : "0";
            }
        }

        // Gets/sets whether the app has been launched, used for the OOBE and tips.
        public static bool Launched
        {
            get
            {
                return savedSettings.ContainsKey("launched") && (string)savedSettings["launched"] == "1";
            }
            set
            {
                savedSettings["launched"] = value ? "1" : "0";
            }
        }

        public static GridLength SidebarSize {
            get {
                if (savedSettings.ContainsKey("size") && savedSettings["size"] is int sizeInt)
                {
                    return new GridLength(sizeInt);
                }
                return new GridLength(185);
            }
            set {
                if (value.GridUnitType == GridUnitType.Pixel)
                {
                    savedSettings["size"] = (int)value.Value;
                }
                else
                {
                    savedSettings["size"] = 185;
                }
            }
        }
    }
}
