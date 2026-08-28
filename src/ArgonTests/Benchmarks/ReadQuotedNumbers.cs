// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[MemoryDiagnoser]
public class ReadQuotedNumbers
{
    string json = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new StringBuilder("[");
        for (var i = 0; i < 500; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append('"').Append(i * 12345).Append('"');
        }

        builder.Append(']');
        json = builder.ToString();
    }

    [Benchmark]
    public long ReadAsInt32()
    {
        using var stringReader = new StringReader(json);
        using var reader = new JsonTextReader(stringReader);
        long sum = 0;
        reader.Read();
        while (reader.ReadAsInt32() is { } value)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public decimal ReadAsDecimal()
    {
        using var stringReader = new StringReader(json);
        using var reader = new JsonTextReader(stringReader);
        decimal sum = 0;
        reader.Read();
        while (reader.ReadAsDecimal() is { } value)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public double ReadAsDouble()
    {
        using var stringReader = new StringReader(json);
        using var reader = new JsonTextReader(stringReader);
        double sum = 0;
        reader.Read();
        while (reader.ReadAsDouble() is { } value)
        {
            sum += value;
        }

        return sum;
    }
}
