using System;

namespace Taskie.Services;

/// <summary>
/// Interface providing access to application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Whether authentication is required to access saved task lists.
    /// </summary>
    public bool AuthUsed { get; set; }
    
    /// <summary>
    /// Whether the pro mode is activated.
    /// </summary>
    public bool IsPro { get; set; }
    
    /// <summary>
    /// An event which is raised when a setting changes.
    /// The provided argument is the setting's key.
    /// </summary>
    event EventHandler<string> Changed;
}