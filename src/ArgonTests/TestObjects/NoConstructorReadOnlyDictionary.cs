// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class NoConstructorReadOnlyDictionary<TKey, TValue> : ReadOnlyDictionary<TKey, TValue>
{
    public NoConstructorReadOnlyDictionary()
        : base(new Dictionary<TKey, TValue>())
    {
    }
}