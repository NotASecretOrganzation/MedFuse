using System.Collections.Concurrent;
using System.Threading.Channels;
using ExtractTransformLoad.Models;

public record ETLContext
{
    public string JobId { get; init; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    public ConcurrentDictionary<string, object> Metadata { get; init; } = new();
    public Channel<object> DataChannel { get; init; } = Channel.CreateBounded<object>(1000);
    public IProgress<ETLProgress> Progress { get; init; }
}