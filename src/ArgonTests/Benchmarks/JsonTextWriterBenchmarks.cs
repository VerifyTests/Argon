// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JsonTextWriterBenchmarks
{
    static readonly string UnicodeCharsString = new('\0', 30);

    [Benchmark]
    public string SerializeUnicodeChars()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteValue(UnicodeCharsString);
        jsonWriter.Flush();

        return stringWriter.ToString();
    }

    [Benchmark]
    public string SerializeIntegers()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        for (var i = 0; i < 10000; i++)
        {
            jsonWriter.WriteValue(i);
        }

        jsonWriter.Flush();

        return stringWriter.ToString();
    }
}