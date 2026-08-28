// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class SerializeJTokenList
{
    List<JObject> tokens = null!;
    JsonSerializer defaultSerializer = null!;
    JsonSerializer serializerWithConverters = null!;

    [GlobalSetup]
    public void Setup()
    {
        tokens = [];
        for (var i = 0; i < 100; i++)
        {
            tokens.Add(
                new()
                {
                    ["id"] = i,
                    ["name"] = $"item-{i}",
                    ["active"] = i % 2 == 0,
                    ["score"] = i * 1.5
                });
        }

        defaultSerializer = JsonSerializer.CreateDefault();

        serializerWithConverters = JsonSerializer.CreateDefault();
        serializerWithConverters.Converters.Add(new StringEnumConverter());
        serializerWithConverters.Converters.Add(new IsoDateTimeConverter());
    }

    [Benchmark]
    public string NoConverters()
    {
        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        defaultSerializer.Serialize(jsonWriter, tokens);
        return stringWriter.ToString();
    }

    [Benchmark]
    public string WithConverters()
    {
        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        serializerWithConverters.Serialize(jsonWriter, tokens);
        return stringWriter.ToString();
    }
}
