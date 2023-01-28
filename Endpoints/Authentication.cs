using BrowserBirdFunctionApi.Things;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using static BrowserBirdFunctionApi.AuthenticationUtilities;

namespace BrowserBirdFunctionApi.Endpoints;

public static class Authentication
{
    public static async
    Task<HttpResponseData>
        ExchangeCodeForJwt(string? code,
                           HttpRequestData request,
                           IHttpClientFactory factory,
                           IConfiguration config)
    {
        DiscordToken? t = await code.TryGetTokenForCode(factory, config);

        if (t is DiscordToken token)
            return await TryReturnJwt(token, request, factory, config);
        else
            return Responses.Unauthorized(request);
    }

    private static async
    Task<HttpResponseData> TryReturnJwt(DiscordToken token,
                                        HttpRequestData request,
                                        IHttpClientFactory factory,
                                        IConfiguration config)
    {
        string accessToken = token.Access_token;

        DiscordUser? u =
            await TryAuthenticateUserWithDiscord(factory, accessToken);

        if (u is DiscordUser user)
            return await TryReturnJwt(token, request, config, user);
        else
            return Responses.Unauthorized(request);
    }

    private static async
    Task<DiscordUser?>
        TryAuthenticateUserWithDiscord(IHttpClientFactory factory,
                                       string accessToken)
    {
        HttpClient client = factory.CreateClient("DiscordGetUser");

        string t = $"Bearer {accessToken}";

        client.DefaultRequestHeaders.Add("Authorization", t);

        HttpResponseMessage r = await client.GetAsync("/api/users/@me");

        if (r.IsSuccessStatusCode)
            return await TryGetDiscordUser(r);
        else
            return null;
    }

    private static async
    Task<DiscordUser?> TryGetDiscordUser(HttpResponseMessage r)
    {
        DiscordUser? u =
            await r.Content.ReadFromJsonAsync<DiscordUser>();

        if (u is DiscordUser user)
            return user;
        else
            return null;
    }

    private static async
    Task<HttpResponseData>
        TryReturnJwt(DiscordToken token,
                     HttpRequestData request,
                     IConfiguration config,
                     DiscordUser user)
    {
        string? j = TryGenerateJwt(config, user, token);

        if (j is string jwt)
            return await Responses.JwtCreated(request, jwt);
        else
            return Responses.Unauthorized(request);
    }

    private static async
    Task<DiscordToken?> TryGetTokenForCode(this string? code,
                                           IHttpClientFactory factory,
                                           IConfiguration config)
    {
        if (string.IsNullOrEmpty(code))
            return null;
        else
            return await TryGetTokenForNonEmptyCode(code, factory, config);
    }

    private static async
    Task<DiscordToken?> TryGetTokenForNonEmptyCode(string code,
                                                   IHttpClientFactory factory,
                                                   IConfiguration config)
    {
        HttpClient client = factory.CreateClient("DiscordGetToken");

        FormUrlEncodedContent q = GetTokenQueryContent(config, code);

        HttpResponseMessage r = await client.PostAsync("", q);

        if (r.IsSuccessStatusCode)
            return await TryReadContent(r);
        else
            return null;
    }

    private static async
    Task<DiscordToken?> TryReadContent(HttpResponseMessage r)
    {
        DiscordToken? d = await r.Content.ReadFromJsonAsync<DiscordToken>();

        if (d is DiscordToken token)
            return token;
        else
            return null;
    }
}