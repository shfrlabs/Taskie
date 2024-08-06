namespace Taskie.Services.Shared;

/// <summary>
/// The registry for settings keys.
/// </summary>
public static class SettingsKeys
{
    /// <summary>
    /// The user's preferred theme.
    /// <br/>
    /// Legal values include: <c>System</c>, <c>Light</c>, <c>Dark</c>.
    /// </summary>
    public const string Theme = "theme";

    /// <summary>
    /// Whether the user should be prompted to authenticate prior to viewing tasks.
    /// </summary>
    public const string IsAuthUsed = "auth";
    
    /// <summary>
    /// Whether the application instance is registered as the pro tier.
    /// </summary>
    public const string IsPro = "pro";
}