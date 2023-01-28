namespace BrowserBirdFunctionApi.Things;

public record DiscordToken(
    string Access_token,
    string Token_type,
    int Expires_in,
    string Refresh_token,
    string Scope);