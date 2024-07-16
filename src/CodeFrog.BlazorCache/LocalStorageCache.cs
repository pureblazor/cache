using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.JSInterop;

namespace CodeFrog.BlazorCache;

public record LocalStorageCacheItem(string Contents, DateTimeOffset ExpiresAt, TimeSpan? SlidingExpiration = null);

/// <summary>
/// TODO: provide options for both server + wasm, currently only wasm
/// </summary>
/// <param name="localStorage"></param>
public class LocalStorageCache(ILocalStorageService localStorage)
    : IDistributedCache
{
    public byte[]? Get(string key)
    {
        // TODO: prefix key with a unique identifier for fusioncache?
        var item = localStorage.GetItem<LocalStorageCacheItem>(key);

        // todo: confirm with the spec if its < or <=
        if (item is null || item.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(item.Contents);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = new())
    {
        return Task.FromResult(Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var contents = Encoding.UTF8.GetString(value) ?? "";
        var expiresAt = options.AbsoluteExpiration ?? DateTimeOffset.MaxValue;
        if (options.SlidingExpiration is not null)
        {
            expiresAt = DateTimeOffset.UtcNow.Add(options.SlidingExpiration.Value);
        }

        var item = new LocalStorageCacheItem(contents, expiresAt, options.SlidingExpiration);
        localStorage.SetItem(key, item);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = new())
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        // TODO: confirm if we need to check if it's already expired
        var item = localStorage.GetItem<LocalStorageCacheItem>(key);
        if (item?.SlidingExpiration == null || item.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return;
        }

        item = item with { ExpiresAt = DateTimeOffset.UtcNow.Add(item.SlidingExpiration ?? TimeSpan.Zero) };
        localStorage.SetItem(key, item);
    }

    public Task RefreshAsync(string key, CancellationToken token = new())
    {
        Refresh(key);

        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        localStorage.RemoveItem(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        Remove(key);

        return Task.CompletedTask;
    }
}
