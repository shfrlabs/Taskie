namespace Taskie.Services.Shared;

/// <summary>
/// The registry for settings keys.
/// </summary>
public static class SettingsKeys
{
    /// <summary>
    /// Whether the user should be prompted to authenticate prior to viewing tasks.
    /// </summary>
    public const string IsAuthUsed = "auth";
    
    /// <summary>
    /// Whether the application instance is registered as the pro tier.
    /// </summary>
    public const string IsPro = "pro";
}