namespace ExtractTransformLoad.Interfaces
{
    public interface IDataSource
    {
        IAsyncEnumerable<object> ExtractDataAsync();
    }
}