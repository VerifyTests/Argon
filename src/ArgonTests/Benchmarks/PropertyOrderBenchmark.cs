// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class PropertyOrderBenchmark
{
    JsonPropertyCollection properties = null!;

    [Params(5, 20, 100)]
    public int Count { get; set; }

    [Params(false, true)]
    public bool AnyOrder { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        properties = [with(typeof(object))];
        for (var i = 0; i < Count; i++)
        {
            var property = new JsonProperty(typeof(string), typeof(object))
            {
                PropertyName = "P" + i
            };
            if (AnyOrder && i % 3 == 0)
            {
                property.Order = Count - i;
            }

            properties.AddProperty(property);
        }
    }

    [Benchmark(Baseline = true)]
    public IList<JsonProperty> OrderByToList() =>
        properties.OrderBy(_ => _.Order ?? -1).ToList();

    [Benchmark]
    public IList<JsonProperty> InPlaceSort()
    {
        var list = new List<JsonProperty>(properties);

        var needsSort = false;
        foreach (var property in list)
        {
            if (property.Order != null)
            {
                needsSort = true;
                break;
            }
        }

        if (needsSort)
        {
            list.Sort(static (a, b) => (a.Order ?? -1).CompareTo(b.Order ?? -1));
        }

        return list;
    }
}
