using TaskParallelExtractTransform_LoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Interfaces
{
    public interface IDataSource
    {
        IAsyncEnumerable<List<RawData>> ExtractBatchesAsync(CancellationToken cancellationToken);
    }
}