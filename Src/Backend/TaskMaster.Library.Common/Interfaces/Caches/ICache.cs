namespace TaskMaster.Library.Common.Interfaces.Caches
{
    internal interface ICache
    {
        T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiry = null);

        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
    }
}
