namespace TaskParallelExtractTransform_LoadApp.Models
{
    public record LoadResult
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string TransformationId { get; init; }
        public bool Success { get; init; }
        public string StorageLocation { get; init; }
        public string ErrorMessage { get; init; }
        public DateTime LoadedAt { get; init; } = DateTime.UtcNow;
    }
}