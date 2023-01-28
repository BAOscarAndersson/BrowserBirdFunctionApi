namespace BrowserBirdFunctionApi.Things;

public record DiscordUser(
    string Id,
    string Username,
    string? Avatar,
    string Discriminator,
    string? Banner,
    int? Banner_color,
    int? Accent_color,
    string Locale);