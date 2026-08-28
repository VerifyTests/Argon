// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class SerializeBenchmarks
{
    static readonly IList<RootObject> LargeCollection;

    static SerializeBenchmarks()
    {
        var json = File.ReadAllText("large.json");

        LargeCollection = JsonConvert.DeserializeObject<IList<RootObject>>(json);
    }

    [Benchmark]
    public void SerializeLargeJsonFile()
    {
        using var file = File.CreateText("largewrite.json");
        var serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented
        };
        serializer.Serialize(file, LargeCollection);
    }
}