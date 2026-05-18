// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Collections;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class DictionaryWrapperKeysBenchmark
{
    Hashtable hashtable = null!;
    Dictionary<string, int> dictionary = null!;
    ReadOnlyDictionary<string, int> readOnlyDictionary = null!;
    DictionaryWrapper<string, int> wrapperHashtable = null!;
    DictionaryWrapper<string, int> wrapperReadOnly = null!;
    DictionaryWrapper<string, int> wrapperGeneric = null!;

    [Params(5, 50, 500)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        hashtable = [];
        dictionary = new();
        for (var i = 0; i < Count; i++)
        {
            var key = "K" + i;
            hashtable[key] = i;
            dictionary[key] = i;
        }

        readOnlyDictionary = new(dictionary);
        wrapperHashtable = new((IDictionary) hashtable);
        wrapperReadOnly = new((IReadOnlyDictionary<string, int>) readOnlyDictionary);
        wrapperGeneric = new((IDictionary<string, int>) dictionary);
    }

    // IDictionary-backed (Hashtable): old materializes List<string> via Cast+ToList; new returns wrapper

    [Benchmark(Baseline = true)]
    public ICollection<string> Hashtable_Old() =>
        hashtable.Keys.Cast<string>().ToList();

    [Benchmark]
    public ICollection<string> Hashtable_New() =>
        wrapperHashtable.Keys;

    [Benchmark]
    public int Hashtable_Old_Iterate()
    {
        var total = 0;
        foreach (var key in hashtable.Keys.Cast<string>().ToList())
        {
            total += key.Length;
        }

        return total;
    }

    [Benchmark]
    public int Hashtable_New_Iterate()
    {
        var total = 0;
        foreach (var key in wrapperHashtable.Keys)
        {
            total += key.Length;
        }

        return total;
    }

    // IReadOnlyDictionary-backed: old materializes List<string>; new returns wrapper

    [Benchmark]
    public ICollection<string> ReadOnly_Old() =>
        readOnlyDictionary.Keys.ToList();

    [Benchmark]
    public ICollection<string> ReadOnly_New() =>
        wrapperReadOnly.Keys;

    // IDictionary<,>.Keys surfaced via non-generic IDictionary.Keys: old ToList'd into ICollection; new returns wrapper

    [Benchmark]
    public ICollection Generic_Old_NonGeneric() =>
        dictionary.Keys.ToList();

    [Benchmark]
    public ICollection Generic_New_NonGeneric() =>
        ((IDictionary) wrapperGeneric).Keys;
}
