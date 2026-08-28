// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonTextReaderBenchmarks
{
    static readonly string FloatArrayJson;

    static JsonTextReaderBenchmarks() =>
        FloatArrayJson = new JArray(Enumerable.Range(0, 5000).Select(_ => _ * 1.1m)).ToString(Formatting.None);

    [Benchmark]
    public void ReadLargeJson()
    {
        using var fileStream = File.OpenText("large.json");
        using var jsonTextReader = new JsonTextReader(fileStream);
        while (jsonTextReader.Read())
        {
        }
    }

    [Benchmark]
    public void ReadAsDecimal()
    {
        using var jsonTextReader = new JsonTextReader(new StringReader(FloatArrayJson));
        jsonTextReader.Read();

        while (jsonTextReader.ReadAsDecimal() != null)
        {
        }
    }
}