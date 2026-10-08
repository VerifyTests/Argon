// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// A converter list that remembers which converter matched each type.
/// </summary>
/// <remarks>
/// JToken.WriteTo hands its converter list to every token beneath it, and each JValue then asks
/// every converter in turn whether it converts the value's type. A document has thousands of
/// values but only a handful of distinct value types, so the answer is remembered per type for as
/// long as this list is in use. It is a list in its own right so that it can travel through the
/// public WriteTo signature unchanged.
/// </remarks>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
sealed class ConverterListCache(IList<JsonConverter> inner) :
    IList<JsonConverter>
{
    // a JValue holds one of a dozen or so CLR types, and a short linear probe is quicker than
    // hashing the type. any types past this many are simply not remembered
    const int capacity = 8;

    struct Match
    {
        public Type Type;
        public JsonConverter? Converter;
    }

    readonly Match[] matches = new Match[capacity];
    int count;

    internal static IList<JsonConverter> Wrap(IList<JsonConverter> converters)
    {
        if (converters.Count == 0 ||
            converters is ConverterListCache)
        {
            return converters;
        }

        return new ConverterListCache(converters);
    }

    internal JsonConverter? GetMatching(Type type)
    {
        for (var index = 0; index < count; index++)
        {
            if (matches[index].Type == type)
            {
                return matches[index].Converter;
            }
        }

        var converter = JsonSerializer.GetMatchingConverter(inner, type);
        if (count < capacity)
        {
            matches[count].Type = type;
            matches[count].Converter = converter;
            count++;
        }

        return converter;
    }

    public int Count => inner.Count;

    public bool IsReadOnly => inner.IsReadOnly;

    public JsonConverter this[int index]
    {
        get => inner[index];
        set
        {
            inner[index] = value;
            count = 0;
        }
    }

    public void Add(JsonConverter item)
    {
        inner.Add(item);
        count = 0;
    }

    public void Clear()
    {
        inner.Clear();
        count = 0;
    }

    public void Insert(int index, JsonConverter item)
    {
        inner.Insert(index, item);
        count = 0;
    }

    public bool Remove(JsonConverter item)
    {
        count = 0;
        return inner.Remove(item);
    }

    public void RemoveAt(int index)
    {
        inner.RemoveAt(index);
        count = 0;
    }

    public bool Contains(JsonConverter item) =>
        inner.Contains(item);

    public void CopyTo(JsonConverter[] array, int arrayIndex) =>
        inner.CopyTo(array, arrayIndex);

    public int IndexOf(JsonConverter item) =>
        inner.IndexOf(item);

    public IEnumerator<JsonConverter> GetEnumerator() =>
        inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        inner.GetEnumerator();
}
