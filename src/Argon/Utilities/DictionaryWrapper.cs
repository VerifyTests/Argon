// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#nullable disable

interface IWrappedDictionary
    : IDictionary
{
    object UnderlyingDictionary { get; }
}

sealed class ReadOnlyCollectionWrapper<T>(IEnumerable<T> source, int count)
    : ICollection<T>, ICollection
{
    public int Count => count;

    public bool IsReadOnly => true;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    public IEnumerator<T> GetEnumerator() =>
        source.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        source.GetEnumerator();

    public bool Contains(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var x in source)
        {
            if (comparer.Equals(x, item))
            {
                return true;
            }
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var x in source)
        {
            array[arrayIndex++] = x;
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        foreach (var x in source)
        {
            array.SetValue(x, index++);
        }
    }

    public void Add(T item) =>
        throw new NotSupportedException();

    public void Clear() =>
        throw new NotSupportedException();

    public bool Remove(T item) =>
        throw new NotSupportedException();
}

class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>, IWrappedDictionary
{
    readonly IDictionary dictionary;
    readonly IDictionary<TKey, TValue> genericDictionary;
    readonly IReadOnlyDictionary<TKey, TValue> readOnlyDictionary;

    // ReSharper disable once UnusedMember.Global
    public DictionaryWrapper(IDictionary dictionary) =>
        this.dictionary = dictionary;

    // ReSharper disable once UnusedMember.Global
    public DictionaryWrapper(IDictionary<TKey, TValue> dictionary) =>
        genericDictionary = dictionary;

    // ReSharper disable once UnusedMember.Global
    public DictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary) =>
        readOnlyDictionary = dictionary;

    public void Add(TKey key, TValue value)
    {
        if (dictionary != null)
        {
            dictionary.Add(key, value);
        }
        else if (genericDictionary != null)
        {
            genericDictionary.Add(key, value);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public bool ContainsKey(TKey key)
    {
        if (dictionary != null)
        {
            return dictionary.Contains(key);
        }

        if (readOnlyDictionary != null)
        {
            return readOnlyDictionary.ContainsKey(key);
        }

        return genericDictionary.ContainsKey(key);
    }

    public ICollection<TKey> Keys
    {
        get
        {
            if (dictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TKey>(dictionary.Keys.Cast<TKey>(), dictionary.Count);
            }

            if (readOnlyDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TKey>(readOnlyDictionary.Keys, readOnlyDictionary.Count);
            }

            return genericDictionary.Keys;
        }
    }

    public bool Remove(TKey key)
    {
        if (dictionary != null)
        {
            if (dictionary.Contains(key))
            {
                dictionary.Remove(key);
                return true;
            }

            return false;
        }

        if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }

        return genericDictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (dictionary != null)
        {
            if (!dictionary.Contains(key))
            {
                value = default;
                return false;
            }

            value = (TValue) dictionary[key];
            return true;
        }

        if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }

        return genericDictionary.TryGetValue(key, out value);
    }

    public ICollection<TValue> Values
    {
        get
        {
            if (dictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TValue>(dictionary.Values.Cast<TValue>(), dictionary.Count);
            }

            if (readOnlyDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TValue>(readOnlyDictionary.Values, readOnlyDictionary.Count);
            }

            return genericDictionary.Values;
        }
    }

    public TValue this[TKey key]
    {
        get
        {
            if (dictionary != null)
            {
                return (TValue) dictionary[key];
            }

            if (readOnlyDictionary != null)
            {
                return readOnlyDictionary[key];
            }

            return genericDictionary[key];
        }
        set
        {
            if (dictionary != null)
            {
                dictionary[key] = value;
            }
            else if (readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                genericDictionary[key] = value;
            }
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        if (dictionary != null)
        {
            ((IList) dictionary).Add(item);
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary?.Add(item);
        }
    }

    public void Clear()
    {
        if (dictionary != null)
        {
            dictionary.Clear();
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary.Clear();
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (dictionary != null)
        {
            return ((IList) dictionary).Contains(item);
        }

        if (readOnlyDictionary != null)
        {
            return readOnlyDictionary.Contains(item);
        }

        return genericDictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (dictionary != null)
        {
            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
            var e = dictionary.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    var entry = e.Entry;
                    array[arrayIndex++] = new((TKey) entry.Key, (TValue) entry.Value);
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary.CopyTo(array, arrayIndex);
        }
    }

    public int Count
    {
        get
        {
            if (dictionary != null)
            {
                return dictionary.Count;
            }

            if (readOnlyDictionary != null)
            {
                return readOnlyDictionary.Count;
            }

            return genericDictionary.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            if (dictionary != null)
            {
                return dictionary.IsReadOnly;
            }

            if (readOnlyDictionary == null)
            {
                return genericDictionary.IsReadOnly;
            }

            return true;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (dictionary != null)
        {
            if (dictionary.Contains(item.Key))
            {
                var value = dictionary[item.Key];

                if (Equals(value, item.Value))
                {
                    dictionary.Remove(item.Key);
                    return true;
                }

                return false;
            }

            return true;
        }

        if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }

        return genericDictionary.Remove(item);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        if (dictionary != null)
        {
            return dictionary.Cast<DictionaryEntry>()
                .Select(de => new KeyValuePair<TKey, TValue>((TKey) de.Key, (TValue) de.Value))
                .GetEnumerator();
        }

        if (readOnlyDictionary != null)
        {
            return readOnlyDictionary.GetEnumerator();
        }

        return genericDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    void IDictionary.Add(object key, object value)
    {
        if (dictionary != null)
        {
            dictionary.Add(key, value);
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary.Add((TKey) key, (TValue) value);
        }
    }

    object IDictionary.this[object key]
    {
        get
        {
            if (dictionary != null)
            {
                return dictionary[key];
            }

            if (readOnlyDictionary != null)
            {
                return readOnlyDictionary[(TKey) key];
            }

            return genericDictionary[(TKey) key];
        }
        set
        {
            if (dictionary != null)
            {
                dictionary[key] = value;
            }
            else if (readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                // Consider changing this code to call GenericDictionary.Remove when value is null.
                //
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                genericDictionary[(TKey) key] = (TValue) value;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment.
            }
        }
    }

    readonly struct DictionaryEnumerator<TEnumeratorKey, TEnumeratorValue>(IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> e)
        : IDictionaryEnumerator
    {
        public DictionaryEntry Entry => new(e.Current.Key, e.Current.Value);

        public object Key => e.Current.Key;

        public object Value => e.Current.Value;

        public object Current => new DictionaryEntry(e.Current.Key, e.Current.Value);

        public bool MoveNext() =>
            e.MoveNext();

        public void Reset() =>
            e.Reset();
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        if (dictionary != null)
        {
            return dictionary.GetEnumerator();
        }

        if (readOnlyDictionary != null)
        {
            return new DictionaryEnumerator<TKey, TValue>(readOnlyDictionary.GetEnumerator());
        }

        return new DictionaryEnumerator<TKey, TValue>(genericDictionary.GetEnumerator());
    }

    bool IDictionary.Contains(object key)
    {
        if (genericDictionary != null)
        {
            return genericDictionary.ContainsKey((TKey) key);
        }

        if (readOnlyDictionary != null)
        {
            return readOnlyDictionary.ContainsKey((TKey) key);
        }

        return dictionary!.Contains(key);
    }

    bool IDictionary.IsFixedSize
    {
        get
        {
            if (genericDictionary != null)
            {
                return false;
            }

            if (readOnlyDictionary != null)
            {
                return true;
            }

            return dictionary!.IsFixedSize;
        }
    }

    ICollection IDictionary.Keys
    {
        get
        {
            if (genericDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TKey>(genericDictionary.Keys, genericDictionary.Count);
            }

            if (readOnlyDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TKey>(readOnlyDictionary.Keys, readOnlyDictionary.Count);
            }

            return dictionary!.Keys;
        }
    }

    public void Remove(object key)
    {
        if (dictionary != null)
        {
            dictionary.Remove(key);
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary.Remove((TKey) key);
        }
    }

    ICollection IDictionary.Values
    {
        get
        {
            if (genericDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TValue>(genericDictionary.Values, genericDictionary.Count);
            }

            if (readOnlyDictionary != null)
            {
                return new ReadOnlyCollectionWrapper<TValue>(readOnlyDictionary.Values, readOnlyDictionary.Count);
            }

            return dictionary!.Values;
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        if (dictionary != null)
        {
            dictionary.CopyTo(array, index);
        }
        else if (readOnlyDictionary != null)
        {
            throw new NotSupportedException();
        }
        else
        {
            genericDictionary.CopyTo((KeyValuePair<TKey, TValue>[]) array, index);
        }
    }

    bool ICollection.IsSynchronized
    {
        get
        {
            if (dictionary != null)
            {
                return dictionary.IsSynchronized;
            }

            return false;
        }
    }

    object ICollection.SyncRoot
    {
        get
        {
            if (field == null)
            {
                Interlocked.CompareExchange(ref field, new(), null);
            }

            return field;
        }
    }

    public object UnderlyingDictionary
    {
        get
        {
            if (dictionary != null)
            {
                return dictionary;
            }

            if (readOnlyDictionary != null)
            {
                return readOnlyDictionary;
            }

            return genericDictionary;
        }
    }
}