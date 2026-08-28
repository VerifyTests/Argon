// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class DeserializeBenchmarks
{
    static readonly string LargeJsonText;
    static readonly string FloatArrayJson;

    static DeserializeBenchmarks()
    {
        LargeJsonText = File.ReadAllText("large.json");

        FloatArrayJson = new JArray(Enumerable.Range(0, 5000).Select(_ => _ * 1.1m)).ToString(Formatting.None);
    }

    [Benchmark]
    public IList<RootObject> DeserializeLargeJsonText() =>
        JsonConvert.DeserializeObject<IList<RootObject>>(LargeJsonText);

    [Benchmark]
    public IList<RootObject> DeserializeLargeJsonFile()
    {
        using var jsonFile = File.OpenText("large.json");
        using var jsonTextReader = new JsonTextReader(jsonFile);
        var serializer = new JsonSerializer();
        return serializer.Deserialize<IList<RootObject>>(jsonTextReader);
    }

    [Benchmark]
    public IList<double> DeserializeDoubleList() =>
        JsonConvert.DeserializeObject<IList<double>>(FloatArrayJson);

    [Benchmark]
    public IList<decimal> DeserializeDecimalList() =>
        JsonConvert.DeserializeObject<IList<decimal>>(FloatArrayJson);
}