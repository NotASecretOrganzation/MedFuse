namespace TaskParallelExtractTransformLoadApp.Models
{
    public record ETLStats
    {
        public int ExtractedCount { get; set; }
        public int TransformedCount { get; set; }
        public int LoadedCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime StartTime { get; init; } = DateTime.UtcNow;
        public string Status { get; set; } = "Running";
    }
}