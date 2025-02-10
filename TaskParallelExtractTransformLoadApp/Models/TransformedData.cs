namespace TaskParallelExtractTransform_LoadApp.Models
{
    public record TransformedData(string Id, object? ProcessedData, DateTime ProcessedAt);
}
