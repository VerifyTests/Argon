// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class ThreadSafeStore<TKey, TValue>(Func<TKey, TValue> creator)
    where TKey : notnull
{
    readonly ConcurrentDictionary<TKey, TValue> concurrentStore = new();

    public TValue Get(TKey key) =>
        concurrentStore.GetOrAdd(key, creator);
}