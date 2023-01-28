using Azure;
using Azure.Data.Tables;
using BrowserBirdFunctionApi.Things;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BrowserBirdFunctionApi.Endpoints;

public static class Scores
{
    private const string HighscoreTable = "Highscores";

    public async static
    Task<HttpResponseData>
        TryPost(string userId,
                int score,
                HttpRequestData req,
                ILoggerFactory loggerFactory,
                TableServiceClient tableService)
    {
        ILogger log = loggerFactory.CreateLogger("TryPostScore");

        try
        {
            return await TryUpdateDatabase(userId, score, tableService, req, log);
        }
        catch (Exception ex)
        {
            const string errorMessage = "Failed to save score to database.";
            log.LogError(ex, errorMessage);
            return Responses.Problem(req);
        }
    }

    private static async
    Task<HttpResponseData>
        TryUpdateDatabase(string userId,
                          int score,
                          TableServiceClient tableService,
                          HttpRequestData req, 
                          ILogger log)
    {
        TableClient? table = tableService.GetTableClient(HighscoreTable);

        if (table is null)
            return Responses.Problem(req);

        Score newScore = new(DateTime.UtcNow, score, userId);

        return await UpdateDatabase(userId, newScore, req, table, log);
    }

    private static async
    Task<HttpResponseData>
        UpdateDatabase(string userId,
                       Score newScore,
                       HttpRequestData req,
                       TableClient table,
                       ILogger log)
    {
        List<Score> oldScores = GetScores(table, userId, log);

        IEnumerable<ScoreEntity> highscores =
            Utilities.Highscores(oldScores, newScore);

        try
        {
            await UpdateTable(table, highscores);

            return Responses.CreatedScore(req);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to update table with new highscore for user {userId}", userId);
            return Responses.Problem(req);
        }
    }

    private static async
    Task
        UpdateTable(TableClient table,
                    IEnumerable<ScoreEntity> highscores)
    {
        TableTransactionActionType type = TableTransactionActionType.UpdateReplace;

        IEnumerable<TableTransactionAction> t = highscores
            .Select(e => new TableTransactionAction(type, e));

        _ = await table.SubmitTransactionAsync(t);
    }

    private static async
    Task AddHighscores(TableClient table, IEnumerable<ScoreEntity> highscores)
    {
        foreach (ScoreEntity s in highscores)
            await table.AddEntityAsync(s);
    }

    public static async
    Task<HttpResponseData> TryRetrive(string userId,
                                      HttpRequestData req,
                                      ILoggerFactory logger,
                                      TableServiceClient tableService)
    {
        ILogger log = logger.CreateLogger("TryGetScores");

        try
        {
            return await Get(userId, tableService, req, log);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "In TryGetScores");
            return Responses.NotFound(req);
        }
    }

    private static async
    Task<HttpResponseData> Get(string userId,
                               TableServiceClient tableService,
                               HttpRequestData req,
                               ILogger log)
    {
        TableClient? t = tableService.GetTableClient(HighscoreTable);

        if (t is TableClient table)
            return await GetScoresResponse(table, userId, req, log);
        else
            return Responses.NotFound(req);
    }

    private static async
    Task<HttpResponseData> GetScoresResponse(TableClient table,
                                             string userId,
                                             HttpRequestData req,
                                             ILogger log)
    {
        List<Score> t = GetScores(table, userId, log);

        return await Responses.Found(req, t);
    }

    private static
    List<Score> GetScores(TableClient table, string userId, ILogger log)
    {
        List<TableEntity> data = table
            .Query<TableEntity>(x => x.PartitionKey == userId).ToList();

        List<Score?> t = data.Select(EntityToScoreAndLogNullValues).ToList();

        List<Score> scores = new();

        t.ForEach(AddToScoresIfNotNull);

        return scores;

        void AddToScoresIfNotNull(Score? score)
        {
            if (score is not null)
                scores.Add(score);
        }

        Score? EntityToScoreAndLogNullValues(TableEntity entity)
        {
            return Utilities.EntityToScore(entity, log);
        }
    }
}