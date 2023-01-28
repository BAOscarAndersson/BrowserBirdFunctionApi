using Azure;
using Azure.Data.Tables;

namespace BrowserBirdFunctionApi.Things;

public class ScoreEntity : ITableEntity
{
    public ScoreEntity(Score score, int RowNr)
    {
        PartitionKey = score.UserId;
        RowKey = RowNr.ToString();
        TimeOfScore = ConvertToUtc(score.TimeOfScore);
        Value = score.Value;
    }

    private static DateTime ConvertToUtc(DateTime t)
    {
        return DateTime.SpecifyKind(t, DateTimeKind.Utc);
    }

    public DateTime TimeOfScore { get; set; }
    public int Value { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}