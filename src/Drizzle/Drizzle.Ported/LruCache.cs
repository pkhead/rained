using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Drizzle.Ported;

public sealed class LruCache<TKey, TValue>
{
    private readonly Dictionary<TKey, int> _cacheEntries;
    private readonly CacheEntry[] _cache;

    // Woomy
    private int _freshest;
    private int _driest;

    public LruCache(int size)
    {
        _cache = new CacheEntry[size];
        _cacheEntries = new Dictionary<TKey, int>(size);

        Clear();
    }

    public TValue Get(TKey key, Func<TKey, TValue> load)
    {
        return Get(key, load, static (func, key) => func(key));
    }

    public TValue Get<TState>(TKey key, TState state, Func<TState, TKey, TValue> load)
    {
        if (_cacheEntries.TryGetValue(key, out var cacheIdx))
        {
            // Have it in cache, just have to refresh the accessed value.
            ref var cacheEntry = ref _cache[cacheIdx];
            if (cacheIdx != _freshest)
            {
                if (cacheEntry.Drier != -1)
                    _cache[cacheEntry.Drier].Fresher = cacheEntry.Fresher;

                if (cacheIdx == _driest)
                    _driest = cacheEntry.Fresher;

                _cache[cacheEntry.Fresher].Drier = cacheEntry.Drier;
                _cache[_freshest].Fresher = cacheIdx;
                cacheEntry.Drier = _freshest;
                _freshest = cacheIdx;
                cacheEntry.Fresher = -1;
            }

            return cacheEntry.Value;
        }

        // Load new value.
        var value = load(state, key);

        // Replace oldest entry.
        var newIdx = _driest;
        ref var newEntry = ref _cache[newIdx];
        if (newEntry.Valid)
        {
            _cacheEntries.Remove(newEntry.Key);
            _cache[newEntry.Fresher].Drier = -1;
        }

        _driest = newEntry.Fresher;
        _cache[_driest].Drier = -1;

        _cacheEntries.Add(key, newIdx);
        newEntry.Fresher = -1;
        newEntry.Drier = _freshest;
        newEntry.Value = value;
        newEntry.Key = key;
        newEntry.Valid = true;

        _cache[_freshest].Fresher = newIdx;
        _freshest = newIdx;
        return value;
    }

    public void Clear()
    {
        _cacheEntries.Clear();

        _freshest = _cache.Length - 1;
        _driest = 0;

        for (var i = 0; i < _cache.Length; i++)
        {
            _cache[i] = new CacheEntry
            {
                Drier = i - 1,
                // Note: this means the end of the cache actually has an invalid fresher link.
                // This is fine, because it just gets bulldozed on first alloc.
                Fresher = i + 1
            };
        }
    }

    [DebuggerDisplay("D: {Drier}, F: {Fresher} V: {Valid} K: {Key}")]
    private struct CacheEntry
    {
        public TValue Value;
        public TKey Key;
        public int Fresher;
        public int Drier;
        public bool Valid;
    }
}
