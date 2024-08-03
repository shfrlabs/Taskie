using System;
using Taskie.Services.Shared;

namespace Taskie.Services;

/// <summary>
/// Interface providing access to application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the value for the specified key.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="key">The key, as specified in <see cref="SettingsKeys"/>.</param>
    /// <returns>The value for the specified key.</returns>
    T Get<T>(string key);
    
    /// <summary>
    /// An event which is raised when a setting changes.
    /// The provided argument is the setting's key.
    /// </summary>
    event EventHandler<string> Changed;
}