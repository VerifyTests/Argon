// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class LinqBenchmarks
{
    JObject deepDocument;
    string parseJson;

    [GlobalSetup]
    public void Setup()
    {
        deepDocument = BuildDeepDocument();
        parseJson = deepDocument.ToString(Formatting.None);
    }

    [Benchmark]
    public int Descendants()
    {
        var count = 0;
        foreach (var _ in deepDocument.Descendants())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public JToken DeepClone() =>
        deepDocument.DeepClone();

    [Benchmark]
    public string WriteTo()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            deepDocument.WriteTo(writer);
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public JObject Parse() =>
        JObject.Parse(parseJson);

    [Benchmark]
    public JToken SelectTokenRepeatedPath() =>
        deepDocument.SelectToken("$.child1.child2.items[2].name");

    [Benchmark]
    public List<JToken> FilterQuery() =>
        deepDocument.SelectTokens("$..[?(@.name=='name3')]").ToList();

    static JObject BuildDeepDocument()
    {
        JObject Leafs(int start) => new(
            Enumerable.Range(start, 5).Select(i => new JProperty($"p{i}", i)));

        var items = new JArray(Enumerable.Range(0, 8).Select(i => new JObject(
            new JProperty("name", $"name{i}"),
            new JProperty("value", i * 1.5),
            new JProperty("flags", new JArray(i, i + 1, i + 2)),
            new JProperty("leaf", Leafs(i)))));

        return new(
            new JProperty("child1", new JObject(
                new JProperty("child2", new JObject(
                    new JProperty("items", items),
                    new JProperty("meta", Leafs(100)))),
                new JProperty("side", new JArray(Enumerable.Range(0, 20))))),
            new JProperty("top", new JArray(Enumerable.Range(0, 10).Select(i => new JObject(
                new JProperty("k", $"key{i}"),
                new JProperty("v", Leafs(i * 10)))))));
    }
}
