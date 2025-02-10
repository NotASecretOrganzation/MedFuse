using TaskParallelExtractTransform_LoadApp.Models;
using TaskParallelExtractTransformLoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Interfaces
{
    public interface IETLPipeline
    {
        Task<ETLStats> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}

