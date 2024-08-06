using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Storage;
using Taskie.Services;
using Taskie.Services.Shared;

namespace Taskie.Views.UWP.Services
{
    public class SettingsService : ISettingsService
    {
        private SettingsService()
        {
        }

        public static SettingsService Instance { get; } = new();

        private IPropertySet _localSettings = ApplicationData.Current.LocalSettings.Values;

        /// <summary>
        /// Applies the default app settings for non-present keys.
        /// </summary>
        public void SetDefaults()
        {
            var localSettings = ApplicationData.Current.LocalSettings.Values;

            var defaultSettings = new Dictionary<string, object>
            {
                { SettingsKeys.IsAuthUsed, false },
                { SettingsKeys.IsPro, false },
            };

            foreach (var pair in defaultSettings.Where(pair => !localSettings.ContainsKey(pair.Key)))
            {
                localSettings[pair.Key] = pair.Value;
            }
        }

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