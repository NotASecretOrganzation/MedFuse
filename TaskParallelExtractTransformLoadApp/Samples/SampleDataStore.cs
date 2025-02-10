using TaskParallelExtractTransform_LoadApp.Interfaces;

namespace TaskParallelExtractTransform_LoadApp.Samples
{
    public class SampleDataStore : IDataStore
    {
        public async Task SaveAsync(object data)
        {
            await Task.Delay(10); // Simulate storage
        }
    }
}

