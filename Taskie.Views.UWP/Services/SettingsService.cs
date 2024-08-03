using System;
using Windows.Foundation.Collections;
using Windows.Storage;
using Taskie.Services;

namespace Taskie.Views.UWP.Services
{
    public class SettingsService : ISettingsService
    {
        private SettingsService()
        {
        }

        public static SettingsService Instance { get; } = new();

        private IPropertySet _localSettings = ApplicationData.Current.LocalSettings.Values;
        
        public T Get<T>(string key)
        {
            return (T)_localSettings[key];
        }

        public void Set<T>(string key, T value)
        {
            _localSettings[key] = value;
        }

        public event EventHandler<string> Changed;
    }
}