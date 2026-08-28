// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// Benchmarks covering the hot path fixes from the performance review in todo.md.
//
// The reader string scan and the escape writer fixes are already measured by
// ReaderBenchmarks.ReadStringHeavy and WriterBenchmarks.WriteEscapedStrings/WriteCleanStrings,
// and the JsonPath regex fix by JsonPathRegexBenchmark, so they are not repeated here.

// JsonWriter.InternalWritePropertyName(CharSpan): the span overload used to call ToString on the
// span so the name could be stored for path tracking, allocating exactly as much as the string
// overload did and defeating the point of the API. The chars are now copied into a buffer that is
// reused by every property written at that depth, so a whole object costs one buffer rather than
// one string per property.
[MemoryDiagnoser]
public class SpanPropertyNameBenchmark
{
    string source;
    (int Start, int Length)[] slices;

    [GlobalSetup]
    public void Setup()
    {
        // names sliced out of a larger buffer, which is what the span overload exists for
        var builder = new StringBuilder();
        var found = new List<(int, int)>();
        foreach (var index in Enumerable.Range(0, 20))
        {
            var name = $"someProperty{index}";
            found.Add((builder.Length, name.Length));
            builder.Append(name);
        }

        source = builder.ToString();
        slices = found.ToArray();
    }

    [Benchmark]
    public string WriteSpanPropertyNames()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartObject();
            foreach (var slice in slices)
            {
                writer.WritePropertyName(source.AsSpan(slice.Start, slice.Length));
                writer.WriteValue(1);
            }

            writer.WriteEndObject();
        }

        return stringWriter.ToString();
    }
}

// DefaultJsonNameTable.Get: the interned name comparison behind every property name lookup during
// deserialization was a per character loop, now a vectorized SequenceEqual.
[MemoryDiagnoser]
public class NameTableGetBenchmark
{
    DefaultJsonNameTable table;
    char[] buffer;
    (int Start, int Length)[] lookups;

    [GlobalSetup]
    public void Setup()
    {
        table = new();
        var builder = new StringBuilder();
        var found = new List<(int, int)>();
        foreach (var index in Enumerable.Range(0, 64))
        {
            // long enough for the comparison to be worth vectorizing, which is the realistic
            // shape for the camel/pascal case property names of a typical model
            var name = $"aFairlyLongPropertyName{index}";
            table.Add(name);
            found.Add((builder.Length, name.Length));
            builder.Append(name);
        }

        buffer = builder.ToString().ToCharArray();
        lookups = found.ToArray();
    }

    [Benchmark]
    public string GetInternedNames()
    {
        string last = null;
        foreach (var lookup in lookups)
        {
            last = table.Get(buffer, lookup.Start, lookup.Length);
        }

        return last;
    }
}

// JsonSerializerInternalBase.GetMatchingConverter: the registered converter list was rescanned,
// calling virtual CanConvert on each converter, for every value written and every property,
// collection item and dictionary entry read. Resolution is now memoized for the duration of a
// single serialize or deserialize call.
[MemoryDiagnoser]
public class ConverterLookupBenchmark
{
    JsonSerializerSettings settings;
    ConverterModel[] models;
    string json;

    [GlobalSetup]
    public void Setup()
    {
        settings = new()
        {
            Converters =
            {
                new VersionConverter(),
                new StringEnumConverter(),
                new IsoDateTimeConverter(),
                new EncodingConverter(),
                new StringBuilderConverter(),
                new TimeZoneInfoConverter()
            }
        };

        models = Enumerable.Range(0, 100)
            .Select(_ => new ConverterModel
            {
                Name = $"item{_}",
                Value = _,
                Ratio = _ * 1.5,
                Flag = _ % 2 == 0
            })
            .ToArray();

        json = JsonConvert.SerializeObject(models, settings);
    }

    [Benchmark]
    public string SerializeWithConverters() =>
        JsonConvert.SerializeObject(models, settings);

    [Benchmark]
    public ConverterModel[] DeserializeWithConverters() =>
        JsonConvert.DeserializeObject<ConverterModel[]>(json, settings);

    public class ConverterModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public double Ratio { get; set; }
        public bool Flag { get; set; }
    }
}

// JsonSerializerInternalWriter.WriteTypeProperty: the $type string was rebuilt for every object
// written, concatenating the type and assembly names and then re-parsing the result through
// RemoveAssemblyDetails. Polymorphic payloads repeat the same few types, so it is now cached for
// the duration of the serialization.
[MemoryDiagnoser]
public class TypeNameWriteBenchmark
{
    object[] items;
    JsonSerializerSettings settings;

    [GlobalSetup]
    public void Setup()
    {
        settings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        items = Enumerable.Range(0, 200)
            .Select(object (_) => new TypeNameItem
            {
                Name = $"item{_}",
                Value = _
            })
            .ToArray();
    }

    [Benchmark]
    public string SerializeWithTypeNames() =>
        JsonConvert.SerializeObject(items, settings);

    public class TypeNameItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}

// JsonObjectContract.IndexOfCreatorParameter: locating the argument slot for a matched
// constructor parameter was an IndexOf scan of CreatorParameters, so every object built through a
// parameterized constructor was quadratic in its parameter count. Records and other immutable
// types with a wide constructor paid the most, so this uses a deliberately wide one.
[MemoryDiagnoser]
public class WideCreatorBenchmark
{
    string json;

    [GlobalSetup]
    public void Setup() =>
        json = JsonConvert.SerializeObject(
            new WideRecord(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16));

    [Benchmark]
    public WideRecord DeserializeWideRecord() =>
        JsonConvert.DeserializeObject<WideRecord>(json);

    public record WideRecord(
        int A1, int A2, int A3, int A4,
        int A5, int A6, int A7, int A8,
        int A9, int A10, int A11, int A12,
        int A13, int A14, int A15, int A16);
}

// JsonSerializerInternalReader.CalculatePropertyDetails: the contract for an existing property
// value was resolved twice, so populating an object paid two GetType calls and two contract
// lookups for every read only property that gets populated in place.
[MemoryDiagnoser]
public class PopulateExistingBenchmark
{
    string json;

    [GlobalSetup]
    public void Setup() =>
        json = JsonConvert.SerializeObject(
            new PopulateTarget
            {
                Numbers = {1, 2, 3, 4, 5},
                Names = {"one", "two", "three"},
                Map = {["a"] = 1, ["b"] = 2},
                Nested = {Numbers = {9, 8, 7}}
            });

    [Benchmark]
    public PopulateTarget PopulateExistingValues() =>
        JsonConvert.DeserializeObject<PopulateTarget>(json);

    public class PopulateTarget
    {
        // read only and initialized by the constructor, so the serializer populates the existing
        // instance in place rather than replacing it, which is the path that resolved the
        // existing value's contract twice
        public List<int> Numbers { get; } = [];
        public List<string> Names { get; } = [];
        public Dictionary<string, int> Map { get; } = [];
        public NestedTarget Nested { get; } = new();
    }

    public class NestedTarget
    {
        public List<int> Numbers { get; } = [];
    }
}

// Linq to JSON hot paths:
//  - JObject.Properties() built a LINQ Cast wrapper plus a boxed enumerator per call
//  - JTokenWriter.WriteValue(int) boxed at the call site while the other numeric overloads
//    already used the shared BoxedPrimitives boxes
//  - JArray's indexer reached the backing list through a virtual property and an interface
//    dispatch rather than indexing it directly
[MemoryDiagnoser]
public class JTokenHotPathBenchmark
{
    JObject document;
    int[] numbers;
    JArray array;

    [GlobalSetup]
    public void Setup()
    {
        document = new(
            Enumerable.Range(0, 50)
                .Select(_ => new JProperty($"property{_}", _)));

        // weighted to the small values BoxedPrimitives caches, as JSON payloads tend to be
        numbers = Enumerable.Range(0, 500)
            .Select(_ => _ % 9)
            .ToArray();

        array = [with(Enumerable.Range(0, 500))];
    }

    [Benchmark]
    public int EnumerateProperties()
    {
        var total = 0;
        foreach (var property in document.Properties())
        {
            total += property.Name.Length;
        }

        return total;
    }

    [Benchmark]
    public JToken IntArrayFromObject() =>
        JToken.FromObject(numbers);

    [Benchmark]
    public int IndexArray()
    {
        var total = 0;
        for (var index = 0; index < array.Count; index++)
        {
            total += (int) array[index];
        }

        return total;
    }
}
