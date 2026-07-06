// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class SerializerEngineBugFixes : TestFixtureBase
{
    // Bug: JsonConverter<T>.ReadJson returned null for any token whose Value is null,
    // including StartObject/StartArray, without consuming the token -> reader desync.

    [Fact]
    public void GenericConverter_reads_object_token_and_keeps_reader_aligned()
    {
        var holder = JsonConvert.DeserializeObject<Holder>("""{"Point":{"X":1,"Y":2},"After":42}""");

        Assert.NotNull(holder.Point);
        Assert.Equal(1, holder.Point.X);
        Assert.Equal(2, holder.Point.Y);
        Assert.Equal(42, holder.After);
    }

    [Fact]
    public void GenericConverter_still_returns_null_for_json_null()
    {
        var holder = JsonConvert.DeserializeObject<Holder>("""{"Point":null,"After":7}""");

        Assert.Null(holder.Point);
        Assert.Equal(7, holder.After);
    }

    public class Holder
    {
        [JsonConverter(typeof(PointConverter))]
        public Point Point { get; set; }

        public int After { get; set; }
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    class PointConverter : JsonConverter<Point>
    {
        public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Point ReadJson(JsonReader reader, Type type, Point existingValue, bool hasExisting, JsonSerializer serializer)
        {
            var o = JObject.Load(reader);
            return new()
            {
                X = (int) o["X"],
                Y = (int) o["Y"]
            };
        }
    }

    // Bug: OrderByKey packed StringComparer into the sort-key tuple, so ordering fell
    // back to the culture-sensitive default comparer. The fix uses OrdinalIgnoreCase,
    // making ordering deterministic and culture-independent.

    [Fact]
    public void OrderByKey_sorts_ordinal_ignore_case()
    {
        var target = new Dictionary<string, int>
        {
            {"_x", 5},
            {"ab", 4},
            {"@", 2},
            {"a-c", 3},
            {"#", 1}
        };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new OrderByKeyContractResolver()
        };

        var json = JsonConvert.SerializeObject(target, settings);

        Assert.Equal("""{"#":1,"@":2,"a-c":3,"ab":4,"_x":5}""", json);
    }

    class OrderByKeyContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type type)
        {
            var contract = base.CreateDictionaryContract(type);
            contract.OrderByKey = true;
            return contract;
        }
    }

    // Bug: JsonSerializerProxy (handed to converters) did not proxy ResolveContract
    // or Converters, so converters saw the default resolver and an empty converter list.

    [Fact]
    public void Converter_sees_registered_converters_and_configured_resolver()
    {
        CaptureConverter.Reset();

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        settings.Converters.Add(new CaptureConverter());

        JsonConvert.SerializeObject(new CaptureHolder(), settings);

        Assert.True(CaptureConverter.ConverterCount >= 1);
        Assert.Equal("someValue", CaptureConverter.ResolvedPropertyName);
    }

    public class CaptureHolder;

    public class Inner
    {
        public int SomeValue { get; set; }
    }

    class CaptureConverter : JsonConverter<CaptureHolder>
    {
        public static int ConverterCount;
        public static string ResolvedPropertyName;

        public static void Reset()
        {
            ConverterCount = 0;
            ResolvedPropertyName = null;
        }

        public override void WriteJson(JsonWriter writer, CaptureHolder value, JsonSerializer serializer)
        {
            ConverterCount = serializer.Converters.Count;
            var contract = (JsonObjectContract) serializer.ResolveContract(typeof(Inner));
            ResolvedPropertyName = contract.Properties[0].PropertyName;
            writer.WriteNull();
        }

        public override CaptureHolder ReadJson(JsonReader reader, Type type, CaptureHolder existingValue, bool hasExisting, JsonSerializer serializer) =>
            null;
    }
}
