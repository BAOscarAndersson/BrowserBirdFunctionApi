using Azure.Data.Tables;
using BrowserBirdFunctionApi.Endpoints;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BrowserBirdFunctionApi;

public static class BrowserBirdFunctionApi
{
    const AuthorizationLevel level = AuthorizationLevel.Anonymous;

    [Function("liveness")]
    public static
    HttpResponseData
        Liveness([HttpTrigger(level, "get")] HttpRequestData req)
    {
        HttpResponseData r = req.CreateResponse(HttpStatusCode.OK);
        r.StatusCode = HttpStatusCode.OK;

        return r;
    }

    [Function("jwt")]
    public static async
    Task<HttpResponseData>
        ExchangeCodeForJwt(
            [HttpTrigger(level, "post", Route = "jwt/{code}")]
            HttpRequestData req,
            FunctionContext executionContext,
            string code)
    {
        IConfiguration? config =
            WeirdDependencyInjection<IConfiguration>(executionContext);

        IHttpClientFactory? factory =
            WeirdDependencyInjection<IHttpClientFactory>(executionContext);

        if (config is null || factory is null)
            return Responses.FailedDependency(req);

        HttpResponseData r = await Authentication
            .ExchangeCodeForJwt(code, req, factory, config);

        return r;
    }

    [Function("score")]
    public static async
    Task<HttpResponseData> PostScore(
        [HttpTrigger(level, "post", Route = "score/{score}")]
        HttpRequestData req,
        FunctionContext executionContext,
        int score)
    {
        IConfiguration? config =
            WeirdDependencyInjection<IConfiguration>(executionContext);

        TableServiceClient? table =
            WeirdDependencyInjection<TableServiceClient>(executionContext);

        ILoggerFactory? factory =
            WeirdDependencyInjection<ILoggerFactory>(executionContext);

        if (table is null || factory is null || config is null)
            return Responses.FailedDependency(req);

        string? userId = GetUserId(req, config);

        if (userId is null)
            return Responses.Unauthorized(req);

        HttpResponseData r =
            await Scores.TryPost(userId, score, req, factory, table);

        return r;
    }

    [Function("scores")]
    public static async
        Task<HttpResponseData> TryRetrive(
            [HttpTrigger(level, "get")]
            HttpRequestData req,
            FunctionContext executionContext)
    {
        IConfiguration? config =
            WeirdDependencyInjection<IConfiguration>(executionContext);

        TableServiceClient? table =
            WeirdDependencyInjection<TableServiceClient>(executionContext);

        ILoggerFactory? factory =
            WeirdDependencyInjection<ILoggerFactory>(executionContext);

        if (table is null || factory is null || config is null)
            return Responses.FailedDependency(req);

        string? userId = GetUserId(req, config);

        if (userId is null)
            return Responses.Unauthorized(req);

        HttpResponseData r =
            await Scores.TryRetrive(userId, req, factory, table);

        return r;
    }

    private static
    T? WeirdDependencyInjection<T>(FunctionContext executionContext)
    {
        IServiceProvider instances = executionContext.InstanceServices;

        return (T?)instances.GetService(typeof(T));
    }

    private static
    string? GetUserId(HttpRequestData httpContext,
                      IConfiguration config)
    {
        string? j = JwtFromContext(httpContext);

        if (j is string jwt)
            return AuthenticationUtilities.GetUserId(jwt, config);
        else
            return null;
    }

    private static 
    string? JwtFromContext(HttpRequestData httpContext)
    {
        bool bearerExists = httpContext.Headers
            .TryGetValues("Authorization", out IEnumerable<string>? values);

        if (!bearerExists || values is null)
            return null;

        string? bearerToken = values
            .SingleOrDefault(x => x.StartsWith("Bearer "));

        if (bearerToken is null)
            return null;

        return bearerToken.AsSpan().Slice(7).ToString();
    }
}