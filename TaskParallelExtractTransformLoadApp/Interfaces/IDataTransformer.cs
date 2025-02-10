using TaskParallelExtractTransform_LoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Interfaces
{
    public interface IDataTransformer
    {
        Task<TransformedData> PreProcessAsync(RawData data, CancellationToken cancellationToken);
        Task<TransformedData> TransformAsync(TransformedData data, CancellationToken cancellationToken);
    }
}
