// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

// Benchmarks covering the performance fixes from the deep-dive audit.

// DefaultJsonNameTable.Add: the lock-target fix keeps a single monitor across a Grow(), so
// concurrent property-name interning stays correct and contended adds do not corrupt the table.
[MemoryDiagnoser]
public class NameTableAddBenchmark
{
    string[] names;

    [GlobalSetup]
    public void Setup() =>
        names = Enumerable.Range(0, 256)
            .Select(_ => $"propertyName{_}")
            .ToArray();

    [Benchmark]
    public string AddAcrossGrowth()
    {
        var table = new DefaultJsonNameTable();
        string last = null;
        foreach (var name in names)
        {
            last = table.Add(name);
        }

        return last;
    }
}

// JavaScriptUtils.ToEscapedJavaScriptString: the \uXXXX branch rents a pooled buffer. Before the
// fix that buffer was never returned, turning pooling into pure allocation plus pool churn.
[MemoryDiagnoser]
public class EscapeToStringBenchmark
{
    string[] values;

    [GlobalSetup]
    public void Setup() =>
        // the \u00xx accented characters force the \uXXXX escape path under EscapeNonAscii
        values = Enumerable.Range(0, 100)
            .Select(_ => $"caf\u00e9 na\u00efve \u00fcber sn\u00f6w {_}    tail text here")
            .ToArray();

    [Benchmark]
    public string ToStringEscapeNonAscii()
    {
        string last = null;
        foreach (var value in values)
        {
            last = JsonConvert.ToString(value, '"', EscapeHandling.EscapeNonAscii);
        }

        return last;
    }
}

// EnumUtils.ToUInt64: the underlying type code is now cached on EnumInfo instead of being
// re-derived reflectively for every enum value written.
[MemoryDiagnoser]
public class EnumWriteBenchmark
{
    AuditColor[] values;
    JsonConverter[] converters;

    [GlobalSetup]
    public void Setup()
    {
        values = Enumerable.Range(0, 500)
            .Select(i => (AuditColor) (i % 3))
            .ToArray();
        converters = [new StringEnumConverter()];
    }

    [Benchmark]
    public string SerializeEnums() =>
        JsonConvert.SerializeObject(values, converters);

    public enum AuditColor
    {
        Red,
        Green,
        Blue
    }
}

// BooleanQueryExpression regex: the pattern/options are now parsed once at path construction
// instead of being re-sliced, re-allocated and re-parsed for every candidate token.
[MemoryDiagnoser]
public class JsonPathRegexBenchmark
{
    JArray data;

    [GlobalSetup]
    public void Setup() =>
        data = new(
            Enumerable.Range(0, 500)
                .Select(i => new JObject {["name"] = $"Argon.Package{i}"}));

    [Benchmark]
    public int RegexFilter() =>
        data.SelectTokens("$[?(@.name =~ /^Argon/)]").Count();
}
