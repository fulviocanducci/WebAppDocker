using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace WebAppDocker;

public static class Extensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptionsDefault = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static T Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, JsonSerializerOptionsDefault)!;
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonSerializerOptionsDefault);
    }

    public static async Task<T> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> factory, DistributedCacheEntryOptions? options = null)
    {
        var cached = await cache.GetStringAsync(key);
        if (cached != null) return Deserialize<T>(cached)!;
        await _lock.WaitAsync();
        try
        {
            cached = await cache.GetStringAsync(key);
            if (cached != null) return Deserialize<T>(cached);
            var value = await factory();
            await cache.SetStringAsync(key, Serialize(value), options ?? new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            return value;
        }
        finally
        {
            _lock.Release();
        }
    }
}