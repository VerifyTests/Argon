// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class ThisGenericTest<T> where T : IKeyValueId
{
    Dictionary<string, T> dict1 = [];

    public string MyProperty { get; set; }

    public void Add(T item) =>
        dict1.Add(item.Key, item);

    public T this[string key]
    {
        get => dict1[key];
        set => dict1[key] = value;
    }

    public T this[int id]
    {
        get => dict1.Values.FirstOrDefault(_ => _.Id == id);
        set
        {
            var item = this[id];

            if (item == null)
            {
                Add(value);
            }
            else
            {
                dict1[item.Key] = value;
            }
        }
    }

    public string ToJson() =>
        JsonConvert.SerializeObject(this, Formatting.Indented);

    public T[] TheItems
    {
        get => dict1.Values.ToArray();
        set
        {
            foreach (var item in value)
            {
                Add(item);
            }
        }
    }
}