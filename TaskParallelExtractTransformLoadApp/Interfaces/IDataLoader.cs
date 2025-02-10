using TaskParallelExtractTransform_LoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Interfaces
{
    public interface IDataLoader
    {
        Task LoadBatchAsync(List<TransformedData> batch, CancellationToken cancellationToken);
    }
}