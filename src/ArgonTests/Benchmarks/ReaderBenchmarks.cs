// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class ReaderBenchmarks
{
    string singleDigitJson;
    string floatJson;
    string quotedDoubleJson;
    string dateOffsetJson;
    string stringHeavyJson;

    [GlobalSetup]
    public void Setup()
    {
        singleDigitJson = "[" + string.Join(",", Enumerable.Range(0, 200).Select(i => i % 9)) + "]";
        floatJson = "[" + string.Join(",", Enumerable.Range(0, 200).Select(i => $"{i}.5")) + "]";
        quotedDoubleJson = "[" + string.Join(",", Enumerable.Range(0, 200).Select(i => $"\"{i}.25\"")) + "]";
        dateOffsetJson = "[" + string.Join(",", Enumerable.Range(0, 100).Select(i => $"\"2024-0{i % 9 + 1}-15T10:20:30+02:00\"")) + "]";
        stringHeavyJson = "[" + string.Join(",", Enumerable.Range(0, 50).Select(i => $"\"a perfectly clean string with no escapable characters at all number {i} padded padded padded\"")) + "]";
    }

    [Benchmark]
    public void ReadSingleDigitIntegers()
    {
        var reader = new JsonTextReader(new StringReader(singleDigitJson));
        while (reader.Read())
        {
        }
    }

    [Benchmark]
    public void ReadFloats()
    {
        var reader = new JsonTextReader(new StringReader(floatJson));
        while (reader.Read())
        {
        }
    }

    [Benchmark]
    public void ReadQuotedAsDouble()
    {
        var reader = new JsonTextReader(new StringReader(quotedDoubleJson));
        reader.Read();
        while (reader.ReadAsDouble() != null)
        {
        }
    }

    [Benchmark]
    public void ReadDateTimeOffsets()
    {
        var reader = new JsonTextReader(new StringReader(dateOffsetJson));
        reader.Read();
        while (reader.ReadAsDateTimeOffset() != null)
        {
        }
    }

    [Benchmark]
    public void ReadStringHeavy()
    {
        var reader = new JsonTextReader(new StringReader(stringHeavyJson));
        while (reader.Read())
        {
        }
    }
}
