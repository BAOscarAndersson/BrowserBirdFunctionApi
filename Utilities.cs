using Azure;
using Azure.Data.Tables;
using BrowserBirdFunctionApi.Things;
using Microsoft.Extensions.Logging;

namespace BrowserBirdFunctionApi;

public static class Utilities
{
    public static 
    IEnumerable<ScoreEntity> 
        Highscores(
            List<Score> oldScores, 
            Score newScore)
    {
        oldScores.Add(newScore);

        return CreateHighscoresFromListOfScores(oldScores);
    }

    private static
    IEnumerable<ScoreEntity> 
        CreateHighscoresFromListOfScores(List<Score> scores)
    {
        IOrderedEnumerable<Score> ordered = scores
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.TimeOfScore);

        const int n = 10;
        IEnumerable<int> ns = Enumerable.Range(1, n);

        IEnumerable<(Score a, int b)> orderedWithNumbering = ordered
            .Take(n)
            .Zip(ns);

        return orderedWithNumbering.Select(x => new ScoreEntity(x.a, x.b));
    }

    public static 
    List<Score> TableEntityToScore(Pageable<TableEntity> tableEntities, ILogger log)
    {
        List<Score> scores = new();

        foreach (TableEntity qEntity in tableEntities)
            TryAddEntityToScores(qEntity, scores, log);

        return scores;
    }

    private static void 
    TryAddEntityToScores(TableEntity qEntity, List<Score> scores, ILogger log)
    {
        Score? t = EntityToScore(qEntity, log);

        if (t is Score score)
            scores.Add(score);
    }

    public static
    Score? EntityToScore(TableEntity qEntity, ILogger log)
    {
        DateTime? d = qEntity.GetDateTime("TimeOfScore");

        string userId = qEntity.PartitionKey;

        if (d is DateTime date)
            return EntityAndDateToScore(qEntity, date, log);
        else
            return LogNullScoreAndReturn("Date was null for user {user}", 
                userId, log);
    }

    private static
    Score? EntityAndDateToScore(TableEntity qEntity, DateTime date, ILogger log)
    {
        int? v = qEntity.GetInt32("Value");
        string userId = qEntity.PartitionKey;

        if (v is int value)
            return new Score(date, value, userId);
        else
            return LogNullScoreAndReturn("Value was null for user {user}", 
                userId, log);
    }

    private static 
    Score? LogNullScoreAndReturn(string message,
                                 string userId,
                                 ILogger log)
    {
        log.LogWarning(message, userId);

        return null;
    }
}