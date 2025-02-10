using ExtractTransformLoad.Interfaces;

namespace ExtractTransformLoad.Models
{
    public class SampleDataSource : IDataSource
    {
        public async IAsyncEnumerable<object> ExtractDataAsync()
        {
            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(10);
                yield return new { Id = i, Data = $"Sample_{i}" };
            }
        }
    }
}