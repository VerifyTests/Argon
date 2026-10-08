// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class JPropertyKeyedCollection() :
    Collection<JToken>([])
{
    // An object with a handful of properties is searched faster by comparing names than by
    // hashing one, and most objects are that small. The dictionary also costs more memory than
    // the properties it indexes, so it is only built once an object grows past this size.
    const int dictionaryThreshold = 8;

    static readonly IEqualityComparer<string> comparer = StringComparer.Ordinal;

    // null while the object is small enough to search by walking the list
    Dictionary<string, JToken>? dictionary;

    protected override void ClearItems()
    {
        base.ClearItems();

        dictionary?.Clear();
    }

    public bool Contains(string key)
    {
        if (dictionary == null)
        {
            return IndexOfKey(key) != -1;
        }

        return dictionary.ContainsKey(key);
    }

    int IndexOfKey(string key)
    {
        var items = InnerList;
        for (var index = 0; index < items.Count; index++)
        {
            if (string.Equals(GetKeyForItem(items[index]), key))
            {
                return index;
            }
        }

        return -1;
    }

    void EnsureDictionary()
    {
        if (dictionary != null)
        {
            return;
        }

        var items = InnerList;
        var created = new Dictionary<string, JToken>(Math.Max(items.Count * 2, dictionaryThreshold * 2), comparer);
        foreach (var item in items)
        {
            created[GetKeyForItem(item)] = item;
        }

        dictionary = created;
    }

    static string GetKeyForItem(JToken item) =>
        ((JProperty) item).Name;

    protected override void InsertItem(int index, JToken item)
    {
        if (dictionary == null &&
            Count >= dictionaryThreshold)
        {
            EnsureDictionary();
        }

        dictionary?[GetKeyForItem(item)] = item;

        base.InsertItem(index, item);
    }

    public bool Remove(string key)
    {
        if (dictionary == null)
        {
            var index = IndexOfKey(key);
            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        return dictionary.TryGetValue(key, out var value) && Remove(value);
    }

    protected override void RemoveItem(int index)
    {
        var keyForItem = GetKeyForItem(Items[index]);
        RemoveKey(keyForItem);
        base.RemoveItem(index);
    }

    void RemoveKey(string key) =>
        dictionary?.Remove(key);

    protected override void SetItem(int index, JToken item)
    {
        var keyForItem = GetKeyForItem(item);
        var keyAtIndex = GetKeyForItem(Items[index]);

        if (dictionary != null)
        {
            dictionary[keyForItem] = item;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (keyAtIndex != null &&
                !comparer.Equals(keyAtIndex, keyForItem))
            {
                dictionary.Remove(keyAtIndex);
            }
        }

        base.SetItem(index, item);
    }

    public JToken this[string key]
    {
        get
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException();
        }
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out JToken? value)
    {
        if (dictionary == null)
        {
            var index = IndexOfKey(key);
            if (index == -1)
            {
                value = null;
                return false;
            }

            value = InnerList[index];
            return true;
        }

        return dictionary.TryGetValue(key, out value);
    }

    public ICollection<string> Keys
    {
        get
        {
            EnsureDictionary();
            return dictionary!.Keys;
        }
    }

    public ICollection<JToken> Values
    {
        get
        {
            EnsureDictionary();
            return dictionary!.Values;
        }
    }

    // Same backing list Collection<T> wraps, exposed so hot loops can foreach
    // without boxing the enumerator through the IList<T> interface dispatch.
    internal List<JToken> InnerList => (List<JToken>) Items;

    public int IndexOfReference(JToken t) =>
        ((List<JToken>) Items).IndexOfReference(t);

    public bool Compare(JPropertyKeyedCollection other)
    {
        if (this == other)
        {
            return true;
        }

        // dictionaries in JavaScript aren't ordered
        // ignore order when comparing properties
        if (Count != other.Count)
        {
            return false;
        }

        foreach (var item in InnerList)
        {
            var p1 = (JProperty) item;
            if (!other.TryGetValue(p1.Name, out var secondValue))
            {
                return false;
            }

            var p2 = (JProperty) secondValue;

            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (p1.Value == null)
            {
                return p2.Value == null;
            }
            // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

            if (!p1.Value.DeepEquals(p2.Value))
            {
                return false;
            }
        }

        return true;
    }
}