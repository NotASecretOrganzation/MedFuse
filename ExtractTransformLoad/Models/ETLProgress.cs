namespace ExtractTransformLoad.Models
{
    public record ETLProgress
    {
        public string JobId { get; init; }
        public string Stage { get; init; }
        public int ProcessedItems { get; init; }
        public int TotalItems { get; init; }
        public string Status { get; init; }
    }
}