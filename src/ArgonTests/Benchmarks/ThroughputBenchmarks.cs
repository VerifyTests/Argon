// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

// Benchmarks covering ten changes that each remove a cost paid per value, so every one of them
// measures a whole serialize, deserialize or LINQ to JSON call over a few hundred items.
//
// The payloads are sized so that no result string reaches the large object heap. A string that
// big triggers full collections on its own schedule, which swamps what is being measured.

// DateTimeUtils.TryParseDateTime, TryParseDateTimeOffset and JsonTextReader.ParseReadString: an
// ISO 8601 date was copied out of the reader's buffer into a string, parsed by TryParseExact,
// which interprets its format string for every value, and then boxed twice on its way back to the
// serializer. The shapes the writer produces are now parsed straight from the buffer. Anything
// else still goes to the framework.
[MemoryDiagnoser]
public class IsoDateReadBenchmark
{
    string dateTimes;
    string dateTimeOffsets;
    string members;

    [GlobalSetup]
    public void Setup()
    {
        var start = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc);
        var offsetStart = new DateTimeOffset(2024, 3, 15, 10, 30, 45, TimeSpan.FromHours(10));

        // every second value has a fraction, as real timestamps do
        dateTimes = JsonConvert.SerializeObject(
            Enumerable.Range(0, 500)
                .Select(_ => start.AddSeconds(_ * 86399).AddMilliseconds(_ % 2 * 123))
                .ToList());

        dateTimeOffsets = JsonConvert.SerializeObject(
            Enumerable.Range(0, 500)
                .Select(_ => offsetStart.AddSeconds(_ * 86399))
                .ToList());

        members = JsonConvert.SerializeObject(
            Enumerable.Range(0, 200)
                .Select(_ => new DateModel
                {
                    Utc = start.AddHours(_),
                    Unspecified = new DateTime(2023, 1, 1).AddDays(_),
                    Offset = offsetStart.AddDays(_),
                    Optional = start.AddMinutes(_)
                })
                .ToList());
    }

    [Benchmark]
    public List<DateTime> DateTimes() =>
        JsonConvert.DeserializeObject<List<DateTime>>(dateTimes);

    [Benchmark]
    public List<DateTimeOffset> DateTimeOffsets() =>
        JsonConvert.DeserializeObject<List<DateTimeOffset>>(dateTimeOffsets);

    [Benchmark]
    public List<DateModel> DateMembers() =>
        JsonConvert.DeserializeObject<List<DateModel>>(members);

    public class DateModel
    {
        public DateTime Utc { get; set; }
        public DateTime Unspecified { get; set; }
        public DateTimeOffset Offset { get; set; }
        public DateTime? Optional { get; set; }
    }
}

// JsonConvert.SerializeObject and JToken.ToString: the JSON was written through a StringWriter,
// whose StringBuilder allocates a chain of chunks as large as the finished text and then copies
// them into the string. The text is now built in a pooled buffer, so the string is the only
// allocation that grows with the output.
[MemoryDiagnoser]
public class SerializeToStringBenchmark
{
    List<Item> items;
    JToken token;

    [GlobalSetup]
    public void Setup()
    {
        items = Enumerable.Range(0, 200)
            .Select(_ => new Item
            {
                Id = _,
                Name = $"Name {_}",
                Flag = _ % 2 == 0,
                Amount = _ * 1.5 + 0.25,
                Code = $"C{_}"
            })
            .ToList();

        token = JToken.FromObject(items);
    }

    [Benchmark]
    public string SerializeObject() =>
        JsonConvert.SerializeObject(items);

    [Benchmark]
    public string SerializeObjectIndented() =>
        JsonConvert.SerializeObject(items, Formatting.Indented);

    [Benchmark]
    public string TokenToString() =>
        token.ToString(Formatting.None);

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Flag { get; set; }
        public double Amount { get; set; }
        public string Code { get; set; }
    }
}

// JsonSerializerInternalWriter: the contract of every list item, dictionary value and member of
// an unsealed type was resolved from the contract resolver's cache, a ConcurrentDictionary lookup
// per value, although it is nearly always the contract already held for the declared type. That
// one is now used whenever the value is exactly the declared type.
//
// Written to a writer that discards its output, so that building the result string, which
// SerializeToStringBenchmark measures, is not part of the numbers.
[MemoryDiagnoser]
public class ItemContractBenchmark
{
    JsonSerializer serializer;
    int[] ints;
    List<string> strings;
    Dictionary<string, int> dictionary;
    List<Parent> objects;

    [GlobalSetup]
    public void Setup()
    {
        serializer = JsonSerializer.CreateDefault();

        ints = Enumerable.Range(0, 1000)
            .Select(_ => _ * 7919 % 100000)
            .ToArray();

        strings = Enumerable.Range(0, 1000)
            .Select(_ => $"some string value {_}")
            .ToList();

        dictionary = Enumerable.Range(0, 500)
            .ToDictionary(_ => $"Key{_}", _ => _ * 3);

        // neither class is sealed, which is the usual case for a model
        objects = Enumerable.Range(0, 200)
            .Select(_ => new Parent
            {
                Name = $"Parent {_}",
                Child = new()
                {
                    Street = $"{_} Main Street",
                    City = "Springfield"
                },
                Tags = ["alpha", "beta", "gamma"]
            })
            .ToList();
    }

    [Benchmark]
    public void Ints() =>
        Serialize(ints);

    [Benchmark]
    public void Strings() =>
        Serialize(strings);

    [Benchmark]
    public void DictionaryValues() =>
        Serialize(dictionary);

    [Benchmark]
    public void UnsealedObjects() =>
        Serialize(objects);

    void Serialize(object value)
    {
        using var writer = new JsonTextWriter(TextWriter.Null);
        serializer.Serialize(writer, value);
    }

    public class Parent
    {
        public string Name { get; set; }
        public Child Child { get; set; }
        public List<string> Tags { get; set; }
    }

    public class Child
    {
        public string Street { get; set; }
        public string City { get; set; }
    }
}

// JsonSerializerInternalReader.CreateObjectUsingCreatorWithParameters: every JSON property of an
// object built through a parameterized constructor got a context object of its own, held in a list
// that grew as they arrived. For a record that was about half of everything allocated. The
// contexts are now structs in a pooled array.
[MemoryDiagnoser]
public class RecordReadBenchmark
{
    string json;

    [GlobalSetup]
    public void Setup() =>
        json = JsonConvert.SerializeObject(
            Enumerable.Range(0, 200)
                .Select(_ => new Person(_, $"First{_}", $"Last{_}", $"person{_}@example.com", _ % 2 == 0, _ * 2.5 + 0.25, 638000000000000000L + _, "Springfield"))
                .ToList());

    [Benchmark]
    public List<Person> Records() =>
        JsonConvert.DeserializeObject<List<Person>>(json);

    public record Person(
        int Id,
        string First,
        string Last,
        string Email,
        bool Active,
        double Score,
        long Ticks,
        string City);
}

// JsonSerializerInternalReader.EnsureType: a value read for a Nullable<T> member arrives as a boxed
// T, which is not the member's type, so it was passed through Convert.ChangeType and came back as a
// second box of the same value. The first box is now used as it is.
[MemoryDiagnoser]
public class NullableMemberReadBenchmark
{
    string json;

    [GlobalSetup]
    public void Setup() =>
        json = JsonConvert.SerializeObject(
            Enumerable.Range(0, 200)
                .Select(_ => new Optional
                {
                    Count = _ * 11 + 100,
                    Total = 5000000000L + _,
                    Ratio = _ * 0.25 + 0.125,
                    Flag = _ % 2 == 0,
                    Price = _ * 1.5m + 0.25m,
                    Index = _ + 100,
                    Weight = _ * 3.5 + 1.25,
                    Enabled = _ % 3 == 0
                })
                .ToList());

    [Benchmark]
    public List<Optional> NullableMembers() =>
        JsonConvert.DeserializeObject<List<Optional>>(json);

    public class Optional
    {
        public int? Count { get; set; }
        public long? Total { get; set; }
        public double? Ratio { get; set; }
        public bool? Flag { get; set; }
        public decimal? Price { get; set; }
        public int? Index { get; set; }
        public double? Weight { get; set; }
        public bool? Enabled { get; set; }
    }
}

// JValue.WriteTo: with converters supplied, every value in a token tree asked every converter in
// turn whether it converts the value's type. A tree has thousands of values but only a handful of
// value types, so the answer is now remembered per type: for the duration of one WriteTo, and for
// the whole run when it is the serializer writing the tokens.
[MemoryDiagnoser]
public class JTokenConverterWriteBenchmark
{
    JToken token;
    JsonConverter[] converters;
    JsonSerializerSettings settings;

    [GlobalSetup]
    public void Setup()
    {
        token = JToken.FromObject(
            Enumerable.Range(0, 200)
                .Select(_ => new
                {
                    Id = _,
                    Name = $"Name {_}",
                    Flag = _ % 2 == 0,
                    Amount = _ * 1.5 + 0.25,
                    Code = $"C{_}"
                })
                .ToList());

        // twenty converters of the kinds an application registers for its own types, none of
        // which match anything a JValue holds
        converters =
        [
            new StringEnumConverter(),
            new VersionConverter(),
            new RegexConverter(),
            new KeyValuePairConverter(),
            new ExpandoObjectConverter(),
            new EncodingConverter(),
            new UnusedConverter<Exception>(),
            new UnusedConverter<Type>(),
            new UnusedConverter<CultureInfo>(),
            new UnusedConverter<FileSystemInfo>(),
            new UnusedConverter<MemberInfo>(),
            new UnusedConverter<Assembly>(),
            new UnusedConverter<Task>(),
            new UnusedConverter<Thread>(),
            new UnusedConverter<Delegate>(),
            new UnusedConverter<TimeZoneInfo>(),
            new UnusedConverter<StringBuilder>(),
            new UnusedConverter<Stream>(),
            new UnusedConverter<TextWriter>(),
            new UnusedConverter<Version>()
        ];

        settings = new()
        {
            Converters = converters.ToList()
        };
    }

    [Benchmark]
    public string ToStringWithConverters() =>
        token.ToString(Formatting.None, converters);

    [Benchmark]
    public string SerializeWithConverters() =>
        JsonConvert.SerializeObject(token, settings);

    [Benchmark]
    public string ToStringWithoutConverters() =>
        token.ToString(Formatting.None);

    class UnusedConverter<T> :
        JsonConverter<T>
    {
        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer) =>
            throw new NotSupportedException();

        public override T ReadJson(JsonReader reader, Type type, T existingValue, bool hasExisting, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}

// JPropertyKeyedCollection and JContainer.ReadContentFrom: every JObject built a dictionary of its
// properties on the first insert. For an object with a handful of properties that costs more
// memory than the properties do, and more time than comparing the names would. An object is now
// searched as a list until it passes eight properties. Loading from a reader also adds each token
// directly rather than through Add, which first works out what kind of content it was handed.
//
// The wide objects are past the threshold, to show what objects that still get a dictionary cost.
[MemoryDiagnoser]
public class SmallJObjectBenchmark
{
    string json;
    string wideJson;
    JArray tokens;

    [GlobalSetup]
    public void Setup()
    {
        json = JsonConvert.SerializeObject(
            Enumerable.Range(0, 300)
                .Select(_ => new
                {
                    Id = _,
                    Name = $"Name {_}",
                    Flag = _ % 2 == 0,
                    Amount = _ * 1.5 + 0.25,
                    Code = $"C{_}"
                })
                .ToList());

        wideJson = JsonConvert.SerializeObject(
            Enumerable.Range(0, 120)
                .Select(_ => new
                {
                    P01 = _,
                    P02 = $"two {_}",
                    P03 = _ % 2 == 0,
                    P04 = _ * 1.5 + 0.25,
                    P05 = $"five {_}",
                    P06 = _ + 6,
                    P07 = $"seven {_}",
                    P08 = _ % 3 == 0,
                    P09 = _ * 2.5 + 0.5,
                    P10 = $"ten {_}",
                    P11 = _ + 11,
                    P12 = $"twelve {_}"
                })
                .ToList());

        tokens = JArray.Parse(json);
    }

    [Benchmark]
    public JToken Parse() =>
        JToken.Parse(json);

    [Benchmark]
    public JToken ParseWide() =>
        JToken.Parse(wideJson);

    [Benchmark]
    public JToken DeepClone() =>
        tokens.DeepClone();

    [Benchmark]
    public long LookupByName()
    {
        long total = 0;
        foreach (var token in tokens)
        {
            var item = (JObject) token;
            total += (int) item["Id"];
            total += ((string) item["Code"]).Length;
            if (item["Missing"] != null)
            {
                total++;
            }
        }

        return total;
    }
}

// JsonTextReader: the spaces of an indent were consumed one at a time, each with a trip around the
// switch of whichever method was reading. A run of spaces is now stepped over in a single loop.
[MemoryDiagnoser]
public class IndentedReadBenchmark
{
    string indented;

    [GlobalSetup]
    public void Setup()
    {
        var items = Enumerable.Range(0, 100)
            .Select(_ => new
            {
                Id = _,
                Name = $"Name {_}",
                Address = new
                {
                    Street = $"{_} Main Street",
                    City = "Springfield",
                    Location = new
                    {
                        Latitude = _ * 0.5,
                        Longitude = _ * 0.25
                    }
                },
                Tags = new[] {"alpha", "beta", "gamma"}
            })
            .ToList();

        indented = JsonConvert.SerializeObject(items, Formatting.Indented);
    }

    [Benchmark]
    public int ReadIndented()
    {
        using var reader = new JsonTextReader(new StringReader(indented));
        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        return count;
    }
}

// JsonSerializerInternalReader.PopulateObject: every property name was found by hashing it, although
// a serializer writes an object's properties in declaration order and the reader's name table hands
// back the very string the property was created with. The property after the previous match is now
// tried first, by reference.
//
// The reversed read never matches the guess, to show what a wrong one costs.
[MemoryDiagnoser]
public class OrderedPropertyReadBenchmark
{
    string inOrder;
    string reversed;

    [GlobalSetup]
    public void Setup()
    {
        inOrder = JsonConvert.SerializeObject(
            Enumerable.Range(0, 300)
                .Select(_ => new Item
                {
                    Id = _,
                    Name = $"Name {_}",
                    Flag = _ % 2 == 0,
                    Amount = _ * 1.5 + 0.25,
                    Code = $"C{_}"
                })
                .ToList());

        reversed = JsonConvert.SerializeObject(
            Enumerable.Range(0, 300)
                .Select(_ => new
                {
                    Code = $"C{_}",
                    Amount = _ * 1.5 + 0.25,
                    Flag = _ % 2 == 0,
                    Name = $"Name {_}",
                    Id = _
                })
                .ToList());
    }

    [Benchmark]
    public List<Item> DeclarationOrder() =>
        JsonConvert.DeserializeObject<List<Item>>(inOrder);

    [Benchmark]
    public List<Item> ReverseOrder() =>
        JsonConvert.DeserializeObject<List<Item>>(reversed);

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Flag { get; set; }
        public double Amount { get; set; }
        public string Code { get; set; }
    }
}

// EnumUtils.ParseEnum: a parsed enum name was boxed with Enum.ToObject, which goes through
// reflection, for every value read. The box for each enum value is now created once and shared.
[MemoryDiagnoser]
public class EnumNameReadBenchmark
{
    JsonSerializerSettings settings;
    string json;

    [GlobalSetup]
    public void Setup()
    {
        settings = new()
        {
            Converters =
            {
                new StringEnumConverter()
            }
        };

        json = JsonConvert.SerializeObject(
            Enumerable.Range(0, 300)
                .Select(_ => new Item
                {
                    Color = (Color) (_ % 5),
                    Accent = (Color) ((_ + 2) % 5),
                    Day = (DayOfWeek) (_ % 7)
                })
                .ToList(),
            settings);
    }

    [Benchmark]
    public List<Item> EnumNames() =>
        JsonConvert.DeserializeObject<List<Item>>(json, settings);

    public class Item
    {
        public Color Color { get; set; }
        public Color Accent { get; set; }
        public DayOfWeek Day { get; set; }
    }

    public enum Color
    {
        Red,
        Green,
        Blue,
        Yellow,
        Purple
    }
}
