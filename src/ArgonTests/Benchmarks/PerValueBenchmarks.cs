// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

// Benchmarks covering the medium and low priority items from the performance review in todo.md.
// The high priority items are covered by HotPathBenchmarks.

// Dictionary keys pay for a conversion per entry per call:
//  - DateTimeUtils.WriteDateTimeString allocated a 64 char array per call, and
//    GetDictionaryPropertyName wrapped it in a StringWriter plus a StringBuilder to get a string
//    back out. Both are gone: the date is formatted into a stackalloc buffer.
//  - NamingStrategy.GetDictionaryKey re-ran the case conversion for every entry of every
//    dictionary, and the same keys repeat across entries and across calls, so resolved keys are
//    now cached on the strategy.
[MemoryDiagnoser]
public class DictionaryKeyBenchmark
{
    Dictionary<DateTime, int> dates;
    Dictionary<DateTimeOffset, int> offsets;
    Dictionary<string, int> names;
    JsonSerializerSettings camelCase;

    [GlobalSetup]
    public void Setup()
    {
        var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        dates = Enumerable.Range(0, 100)
            .ToDictionary(_ => start.AddDays(_), _ => _);
        offsets = Enumerable.Range(0, 100)
            .ToDictionary(_ => new DateTimeOffset(start.AddDays(_)).ToOffset(TimeSpan.FromHours(10)), _ => _);

        // the same key set serialized repeatedly, which is what a cache can help with. The
        // names are the ones a dictionary keyed on a domain concept tends to have
        names = Enumerable.Range(0, 100)
            .ToDictionary(_ => $"SomeDictionaryKey{_}", _ => _);

        camelCase = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true
                }
            }
        };
    }

    [Benchmark]
    public string SerializeDateKeys() =>
        JsonConvert.SerializeObject(dates);

    [Benchmark]
    public string SerializeDateTimeOffsetKeys() =>
        JsonConvert.SerializeObject(offsets);

    [Benchmark]
    public string SerializeCamelCaseKeys() =>
        JsonConvert.SerializeObject(names, camelCase);
}

// JavaScriptUtils.ToEscapedJavaScriptString always built its result through a StringWriter over a
// StringBuilder, even for the common case of a string with nothing in it to escape. When the
// vectorized scan finds no char to escape the result is now copied out directly.
[MemoryDiagnoser]
public class EscapeFreeStringBenchmark
{
    string[] values;

    [GlobalSetup]
    public void Setup() =>
        values = Enumerable.Range(0, 100)
            .Select(_ => $"a typical value with no escapes in it at all {_}")
            .ToArray();

    [Benchmark]
    public string ToStringNoEscapes()
    {
        string last = null;
        foreach (var value in values)
        {
            last = JsonConvert.ToString(value);
        }

        return last;
    }
}

// JsonSerializerInternalReader.ResolveTypeName split every $type it read into its type and
// assembly halves, allocating a substring for each. A polymorphic payload repeats the same few
// type names, so the split is now memoized for the duration of the deserialization.
[MemoryDiagnoser]
public class TypeNameReadBenchmark
{
    string json;
    JsonSerializerSettings settings;

    [GlobalSetup]
    public void Setup()
    {
        settings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var items = Enumerable.Range(0, 200)
            .Select(object (_) => new TypeNameItem
            {
                Name = $"item{_}",
                Value = _
            })
            .ToArray();

        json = JsonConvert.SerializeObject(items, settings);
    }

    [Benchmark]
    public object DeserializeWithTypeNames() =>
        JsonConvert.DeserializeObject<object[]>(json, settings);

    public class TypeNameItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}

// Per value reader allocations:
//  - ReadDecimalString copied its input to a char array before handing it to the fallback parser
//    that handles exponent form decimals, which decimal.TryParse does not accept. The parsers
//    only ever index their input, so they take spans now.
//  - ReadAsBytes probes a 36 char string for a Guid before treating it as base64, and that probe
//    materialized the string even when the answer was base64.
[MemoryDiagnoser]
public class ReadValueBenchmark
{
    string decimals;
    string byteStrings;

    [GlobalSetup]
    public void Setup()
    {
        // exponent form, which is the path that falls through decimal.TryParse to Argon's parser
        decimals = $"[{string.Join(",", Enumerable.Range(1, 200).Select(_ => $"\"{_}6.014e-05\""))}]";

        // 27 bytes base64 encodes to exactly 36 chars, the length of a Guid in D format, so
        // every one of these takes the Guid probe before being decoded as base64
        var random = new byte[27];
        byteStrings = $"[{string.Join(",", Enumerable.Range(0, 200).Select(_ =>
        {
            random[0] = (byte) _;
            return $"\"{Convert.ToBase64String(random)}\"";
        }))}]";
    }

    [Benchmark]
    public decimal ReadExponentDecimals()
    {
        using var reader = new JsonTextReader(new StringReader(decimals));
        reader.Read();
        decimal total = 0;
        // ReadAsDecimal on a string token is what reaches ReadDecimalString, and exponent form
        // is what falls through decimal.TryParse to the parser that took the copy
        while (reader.ReadAsDecimal() is { } value)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int ReadBase64Strings()
    {
        using var reader = new JsonTextReader(new StringReader(byteStrings));
        reader.Read();
        var total = 0;
        while (reader.ReadAsBytes() is { } bytes)
        {
            total += bytes.Length;
        }

        return total;
    }
}

// JsonTextReader.ReadNumberIntoBuffer walks a number a char at a time through a 28 case switch.
// Replacing that with a vectorized IndexOfAnyExcept over a SearchValues of the number chars was
// measured and turned down: it costs more than it saves on the short numbers JSON is mostly made
// of. See todo.md. This stays as the cost profile of number reading by length, for whoever picks
// that up next.
[MemoryDiagnoser]
public class NumberScanBenchmark
{
    string json;

    // digits per number, spanning ids and small quantities through to high precision decimals
    [Params(1, 3, 8, 18)]
    public int Digits { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var number = Digits <= 2
            ? new string('7', Digits)
            : $"{new string('7', Digits - 2)}.7";
        json = $"[{string.Join(",", Enumerable.Repeat(number, 500))}]";
    }

    [Benchmark]
    public int ReadNumbers()
    {
        using var reader = new JsonTextReader(new StringReader(json));
        var count = 0;
        while (reader.Read())
        {
            if (reader.TokenType is JsonToken.Integer or JsonToken.Float)
            {
                count++;
            }
        }

        return count;
    }
}

// JTokenWriter.WritePropertyName removes any property of the same name before adding the new
// one, and the add then re-checked for a duplicate name before the keyed collection hashed the
// name a third time to index it. The check the writer's own remove already guarantees is skipped.
[MemoryDiagnoser]
public class JTokenPropertyWriteBenchmark
{
    WideModel model;

    [GlobalSetup]
    public void Setup() =>
        model = new();

    [Benchmark]
    public JToken FromObjectWide() =>
        JToken.FromObject(model);

    public class WideModel
    {
        public string P01 { get; set; } = "one";
        public string P02 { get; set; } = "two";
        public string P03 { get; set; } = "three";
        public string P04 { get; set; } = "four";
        public int P05 { get; set; } = 5;
        public int P06 { get; set; } = 6;
        public int P07 { get; set; } = 7;
        public int P08 { get; set; } = 8;
        public bool P09 { get; set; } = true;
        public bool P10 { get; set; }
        public double P11 { get; set; } = 11.5;
        public double P12 { get; set; } = 12.5;
        public string P13 { get; set; } = "thirteen";
        public string P14 { get; set; } = "fourteen";
        public string P15 { get; set; } = "fifteen";
        public string P16 { get; set; } = "sixteen";
    }
}

// JSONPath filters walked their input through JToken's enumerator, which goes through Children()
// and boxes an enumerator for every token the filter is handed. Both filters walk the container's
// children directly now. The path itself is parsed once and cached, but the parse also built a
// StringBuilder per number and per quoted string, which slicing the expression avoids.
[MemoryDiagnoser]
public class JsonPathFilterBenchmark
{
    JObject document;
    int counter;

    [GlobalSetup]
    public void Setup() =>
        document = new()
        {
            ["store"] = new JObject
            {
                ["book"] = new JArray(
                    Enumerable.Range(0, 200)
                        .Select(_ => new JObject
                        {
                            ["title"] = $"book{_}",
                            ["category"] = _ % 2 == 0 ? "fiction" : "reference",
                            ["price"] = _ % 40
                        }))
            }
        };

    [Benchmark]
    public int QueryFilter() =>
        document.SelectTokens("$.store.book[?(@.price > 20)]").Count();

    [Benchmark]
    public int WildcardIndexFilter() =>
        document.SelectTokens("$.store.book[*].title").Count();

    [Benchmark]
    public int ParseAndQuery()
    {
        // a fresh path string each time, so the parse is not served from the path cache
        counter++;
        return document.SelectTokens($"$.store.book[?(@.category == 'fiction' && @.price > {counter % 20})]").Count();
    }
}
