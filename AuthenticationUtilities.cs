using BrowserBirdFunctionApi.Things;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BrowserBirdFunctionApi;

public static class AuthenticationUtilities
{
    internal static
    FormUrlEncodedContent
        GetTokenQueryContent(IConfiguration config, string code)
    {
        return new(GetTokenQueryParameters(config, code));
    }

    private static
    Dictionary<string, string>
        GetTokenQueryParameters(IConfiguration config, string code)
    {
        string id = ValueForKeyOrThrow("DiscordClientId", config);

        string secret = ValueForKeyOrThrow("DiscordClientSecret", config);

        string uri = ValueForKeyOrThrow("RedirectUri", config);

        return new()
        {
            {"client_id", id},
            {"client_secret", secret},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", uri}
        };
    }

    internal static
    string? TryGenerateJwt(IConfiguration config,
                           DiscordUser user,
                           DiscordToken discordAccessToken)
    {
        try
        {
            return GenerateJwt(config, user, discordAccessToken);
        }
        catch
        {
            return null;
        }
    }

    private static
    string GenerateJwt(IConfiguration config,
                       DiscordUser user,
                       DiscordToken discordAccessToken)
    {
        ClaimsIdentity subject = SubjectClaim(user);

        DateTime expires = ExpiresIn(discordAccessToken);

        SigningCredentials signingCredentials = GetSigningCredentials(config);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = subject,
            Expires = expires,
            SigningCredentials = signingCredentials
        };

        return GenerateStringJwt(tokenDescriptor);
    }

    private static
    string ValueForKeyOrThrow(string key, IConfiguration config)
    {
        string? v = config[key];

        if (string.IsNullOrEmpty(v))
            throw new KeyNotFoundException
                ($"Could not find value for key: {key}");
        else
            return v;
    }

    public static
    string GenerateStringJwt(SecurityTokenDescriptor tokenDescriptor)
    {
        JwtSecurityTokenHandler tokenHandler = new();

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private static
    ClaimsIdentity SubjectClaim(DiscordUser user)
    {
        Claim claim = new("id", user.Id.ToString());

        Claim[] claims = new[] { claim };

        return new ClaimsIdentity(claims);
    }

    private static
    DateTime ExpiresIn(DiscordToken discordAccessToken)
    {
        int expiresInSeconds = discordAccessToken.Expires_in;

        return DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }

    internal static
    TokenValidationParameters TokenValidationParameters(IConfiguration config)
    {
        byte[] key = GetSigningKey(config);

        SymmetricSecurityKey issuerSigningKey = new(key);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = issuerSigningKey,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private static
    SigningCredentials GetSigningCredentials(IConfiguration config)
    {
        byte[] key = GetSigningKey(config);

        SymmetricSecurityKey s = new(key);

        string t = SecurityAlgorithms.HmacSha256Signature;

        return new SigningCredentials(s, t);
    }

    private static
    byte[] GetSigningKey(IConfiguration config)
    {
        string? jwtSecret = config["jwtsecret"];

        if (jwtSecret is string s)
            return Encoding.ASCII.GetBytes(s);
        else
            throw new KeyNotFoundException("Secret key for jwt not found.");
    }

    public static
    string? GetUserId(string token, IConfiguration config)
    {
        Claim? userIdClaim = GetUserClaim(token, config);

        if (userIdClaim is null)
            return null;

        string userId = userIdClaim.Value;

        return userId;
    }

    private static
    Claim? GetUserClaim(string token, IConfiguration config)
    {
        JwtSecurityToken? t = ValidateToken(token, config);

        if (t is null)
            return null;

        return t.Claims.FirstOrDefault(x => x.Type == "id");
    }

    private static
    JwtSecurityToken? ValidateToken(string token, IConfiguration config)
    {
        TokenValidationParameters tokenValidationParam =
            TokenValidationParameters(config);

        try
        {
            return ValidateToken(token, tokenValidationParam);
        }
        catch
        {
            return null;
        }
    }

    private static 
    JwtSecurityToken ValidateToken(string token, 
        TokenValidationParameters parameters)
    {
        JwtSecurityTokenHandler tokenHandler = new();

        tokenHandler.ValidateToken(
            token,
            parameters,
            out _);

        JwtSecurityToken jwt = tokenHandler.ReadJwtToken(token);

        return jwt;
    }
}