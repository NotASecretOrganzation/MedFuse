using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Models
{
    public record TransformationResult
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public object? OriginalData { get; init; }
        public object? TransformedData { get; init; }
        public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
        public List<string> Warnings { get; init; } = new();
        public List<string> ValidationErrors { get; init; } = new();
    }
}