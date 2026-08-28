// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class WriterBenchmarks
{
    Guid[] guids;
    TimeSpan[] timeSpans;
    double[] doubles;
    string[] escapyStrings;
    string[] cleanStrings;

    [GlobalSetup]
    public void Setup()
    {
        guids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();
        timeSpans = Enumerable.Range(0, 100).Select(i => TimeSpan.FromMinutes(i * 7.5)).ToArray();
        doubles = Enumerable.Range(0, 100).Select(i => i * 1.37).ToArray();
        escapyStrings = Enumerable.Range(0, 50).Select(i => $"line1\nline2 \"quoted{i}\" and\ttabs plus some longer tail text to scan {i}").ToArray();
        cleanStrings = Enumerable.Range(0, 50).Select(i => $"a perfectly clean string with no escapable characters at all number {i} padded padded padded").ToArray();
    }

    [Benchmark]
    public string WritePropertyNames()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartObject();
            for (var i = 0; i < 20; i++)
            {
                writer.WritePropertyName("someProperty" + i % 4);
                writer.WriteValue(1);
            }

            writer.WriteEndObject();
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public string WriteDoubles()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            foreach (var value in doubles)
            {
                writer.WriteValue(value);
            }

            writer.WriteEndArray();
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public string WriteGuids()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            foreach (var value in guids)
            {
                writer.WriteValue(value);
            }

            writer.WriteEndArray();
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public string WriteTimeSpans()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            foreach (var value in timeSpans)
            {
                writer.WriteValue(value);
            }

            writer.WriteEndArray();
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public string WriteEscapedStrings()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            foreach (var value in escapyStrings)
            {
                writer.WriteValue(value);
            }

            writer.WriteEndArray();
        }

        return stringWriter.ToString();
    }

    [Benchmark]
    public string WriteCleanStrings()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            foreach (var value in cleanStrings)
            {
                writer.WriteValue(value);
            }

            writer.WriteEndArray();
        }

        return stringWriter.ToString();
    }
}
