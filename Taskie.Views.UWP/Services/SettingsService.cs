using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Applies the default app settings for non-present keys.
        /// </summary>
        public void SetDefaults()
        {
            var localSettings = ApplicationData.Current.LocalSettings.Values;

            var defaultSettings = new Dictionary<string, object>
            {
                { nameof(AuthUsed), false },
                { nameof(IsPro), false },
            };

            foreach (var pair in defaultSettings.Where(pair => !localSettings.ContainsKey(pair.Key)))
            {
                localSettings[pair.Key] = pair.Value;
            }

            // TODO: Throw exception if not all settings have a default value defined in defaultSettings 
        }

        private T Get<T>([CallerMemberName] string? key = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return (T)_localSettings[key];
        }

        private void Set<T>(T value, [CallerMemberName] string? key = null)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _localSettings[key] = value;
            Changed?.Invoke(this, key);
        }

        public event EventHandler<string> Changed;

        #region Properties

        public string Theme
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool AuthUsed
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsPro
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion
    }
}