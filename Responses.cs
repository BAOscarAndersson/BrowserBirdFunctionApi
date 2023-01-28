using BrowserBirdFunctionApi.Things;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace BrowserBirdFunctionApi;

public static class Responses
{
    public static async
    Task<HttpResponseData> JwtCreated(HttpRequestData request, string jwt)
    {
        HttpResponseData t = Response(request, HttpStatusCode.Created);

        await t.WriteAsJsonAsync(jwt);

        return t;
    }

    public static async
    Task<HttpResponseData> Found(HttpRequestData request,
                                 IEnumerable<Score> scores)
    {
        HttpResponseData t = Response(request, HttpStatusCode.Found);

        await t.WriteAsJsonAsync(scores);

        return t;
    }


    public static 
    HttpResponseData Unauthorized(HttpRequestData request)
    {
        return Response(request, HttpStatusCode.Unauthorized);
    }

    public static
    HttpResponseData Problem(HttpRequestData request)
    {
        return Response(request, HttpStatusCode.InternalServerError);
    }

    public static
    HttpResponseData CreatedScore(HttpRequestData request)
    {
        return Response(request, HttpStatusCode.Created);
    }

    public static
    HttpResponseData NotFound(HttpRequestData request)
    {
        return Response(request, HttpStatusCode.NotFound);
    }

    public static 
    HttpResponseData FailedDependency(HttpRequestData request)
    {
        return Response(request, HttpStatusCode.FailedDependency);
    }

    private static
    HttpResponseData Response(HttpRequestData request, HttpStatusCode code)
    {
        return request.CreateResponse(code);
    }
} 