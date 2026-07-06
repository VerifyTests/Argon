// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class SerializerEngineBenchmarks
{
    List<EnginePoco> pocoList;
    string pocoJson;
    JsonSerializerSettings converterSettings;
    Dictionary<string, string> dictionary;
    Dictionary<string, int> snakeDictionary;
    JsonSerializerSettings snakeSettings;
    List<UriHolder> uriHolders;
    string enumJson;
    DayOfWeek[] enumArray;

    [GlobalSetup]
    public void Setup()
    {
        pocoList = Enumerable.Range(0, 20).Select(i => new EnginePoco
        {
            Name = $"name{i}",
            Description = $"some description text {i}",
            Count = i,
            Value = i * 1.5,
            Ratio = 0.25m,
            Enabled = (i & 1) == 0
        }).ToList();
        pocoJson = JsonConvert.SerializeObject(pocoList);

        converterSettings = new();
        converterSettings.Converters.Add(new NoOpConverter1());
        converterSettings.Converters.Add(new NoOpConverter2());
        converterSettings.Converters.Add(new NoOpConverter3());

        dictionary = Enumerable.Range(0, 50).ToDictionary(i => $"key_number_{i}", i => $"value {i}");
        snakeDictionary = Enumerable.Range(0, 50).ToDictionary(i => $"already_snake_key_{i}", i => i);
        snakeSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(processDictionaryKeys: true, overrideSpecifiedNames: false)
            }
        };

        uriHolders = Enumerable.Range(0, 20).Select(i => new UriHolder {Url = new($"https://example.com/{i}")}).ToList();
        enumJson = "[" + string.Join(",", Enumerable.Range(0, 100).Select(i => i % 3)) + "]";
        enumArray = Enumerable.Range(0, 100).Select(i => (DayOfWeek) (i % 7)).ToArray();
    }

    [Benchmark]
    public string SerializePocoPlain() =>
        JsonConvert.SerializeObject(pocoList);

    [Benchmark]
    public string SerializePocoWithConverters() =>
        JsonConvert.SerializeObject(pocoList, converterSettings);

    [Benchmark]
    public List<EnginePoco> DeserializePocoWithConverters() =>
        JsonConvert.DeserializeObject<List<EnginePoco>>(pocoJson, converterSettings);

    [Benchmark]
    public string SerializeDictionary() =>
        JsonConvert.SerializeObject(dictionary);

    [Benchmark]
    public string SerializeSnakeCaseDictionaryKeys() =>
        JsonConvert.SerializeObject(snakeDictionary, snakeSettings);

    [Benchmark]
    public string SerializeUriProperties() =>
        JsonConvert.SerializeObject(uriHolders);

    [Benchmark]
    public DayOfWeek[] DeserializeEnumsFromIntegers() =>
        JsonConvert.DeserializeObject<DayOfWeek[]>(enumJson);

    [Benchmark]
    public string SerializeEnumsAsNames() =>
        JsonConvert.SerializeObject(enumArray, new StringEnumConverter());

    public class EnginePoco
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Count { get; set; }
        public double Value { get; set; }
        public decimal Ratio { get; set; }
        public bool Enabled { get; set; }
    }

    public class UriHolder
    {
        public Uri Url { get; set; }
    }

    class NoOpConverter1 : JsonConverter
    {
        public override bool CanConvert(Type type) => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();
        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) => throw new NotSupportedException();
    }

    class NoOpConverter2 : NoOpConverter1;

    class NoOpConverter3 : NoOpConverter1;
}
