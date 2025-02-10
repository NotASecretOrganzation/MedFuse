namespace TaskParallelExtractTransform_LoadApp.Models
{
    public record RawData()
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string RawContent { get; init;} = string.Empty;
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
