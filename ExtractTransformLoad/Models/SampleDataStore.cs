using ExtractTransformLoad.Interfaces;

namespace ExtractTransformLoad.Models
{

    public class SampleDataStore : IDataStore
    {
        public async Task SaveAsync(object data)
        {
            await Task.Delay(10); // Simulate storage
        }
    }
}
