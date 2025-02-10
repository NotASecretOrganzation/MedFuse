using ExtractTransformLoad.Interfaces;

namespace ExtractTransformLoad.Interfaces
{
    public interface IDataStore
    {
        Task SaveAsync(object data);
    }
}