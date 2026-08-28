// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Microsoft.FSharp.Reflection;
using TestObjects;
using Formatting = Argon.Formatting;

public class DiscriminatedUnionConverterTests : TestFixtureBase
{
    static DiscriminatedUnionConverter unionConverter = new();

    public class DoubleDoubleConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var d = (double) value;

            writer.WriteValue(d * 2);
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            var d = (double) reader.Value;

            return d / 2;
        }

        public override bool CanConvert(Type type) =>
            type == typeof(double);
    }

    [Fact]
    public void SerializeUnionWithConverter()
    {
        var json = JsonConvert.SerializeObject(Shape.NewRectangle(10.0, 5.0), new DoubleDoubleConverter(), unionConverter);

        Assert.Equal("""{"Case":"Rectangle","Fields":[20.0,10.0]}""", json);

        var c = JsonConvert.DeserializeObject<Shape>(json, new DoubleDoubleConverter(), unionConverter);
        Assert.True(c.IsRectangle);

        var r = (Shape.Rectangle) c;

        Assert.Equal(5.0, r.length);
        Assert.Equal(10.0, r.width);
    }

    [Fact]
    public void SerializeBasicUnion()
    {
        var json = JsonConvert.SerializeObject(Currency.AUD, unionConverter);

        Assert.Equal("""{"Case":"AUD"}""", json);
    }

    [Fact]
    public void SerializePerformance()
    {
        var values = new List<Shape>
        {
            Shape.NewRectangle(10.0, 5.0),
            Shape.NewCircle(7.5)
        };

        JsonConvert.SerializeObject(values, Formatting.Indented, unionConverter);

        var ts = new Stopwatch();
        ts.Start();

        for (var i = 0; i < 100; i++)
        {
            JsonConvert.SerializeObject(values, unionConverter);
        }

        ts.Stop();

        Console.WriteLine(ts.Elapsed.TotalSeconds);
    }

    [Fact]
    public void DeserializePerformance()
    {
        var json = """
                   [
                     {"Case":"Rectangle","Fields":[10.0,5.0]},
                     {"Case":"Rectangle","Fields":[10.0,5.0]},
                     {"Case":"Rectangle","Fields":[10.0,5.0]},
                     {"Case":"Rectangle","Fields":[10.0,5.0]},
                     {"Case":"Rectangle","Fields":[10.0,5.0]}
                   ]
                   """;

        JsonConvert.DeserializeObject<List<Shape>>(json, unionConverter);

        var ts = new Stopwatch();
        ts.Start();

        for (var i = 0; i < 100; i++)
        {
            JsonConvert.DeserializeObject<List<Shape>>(json, unionConverter);
        }

        ts.Stop();

        Console.WriteLine(ts.Elapsed.TotalSeconds);
    }

    [Fact]
    public void SerializeUnionWithFields()
    {
        var json = JsonConvert.SerializeObject(Shape.NewRectangle(10.0, 5.0), unionConverter);

        Assert.Equal("""{"Case":"Rectangle","Fields":[10.0,5.0]}""", json);
    }

    [Fact]
    public void DeserializeBasicUnion()
    {
        var c = JsonConvert.DeserializeObject<Currency>("""{"Case":"AUD"}""", unionConverter);
        Assert.Equal(Currency.AUD, c);

        c = JsonConvert.DeserializeObject<Currency>("""{"Case":"EUR"}""", unionConverter);
        Assert.Equal(Currency.EUR, c);

        c = JsonConvert.TryDeserializeObject<Currency>("null", unionConverter);
        Assert.Null(c);
    }

    [Fact]
    public void DeserializeUnionWithFields()
    {
        var c = JsonConvert.DeserializeObject<Shape>("""{"Case":"Rectangle","Fields":[10.0,5.0]}""", unionConverter);
        Assert.True(c.IsRectangle);

        var r = (Shape.Rectangle) c;

        Assert.Equal(5.0, r.length);
        Assert.Equal(10.0, r.width);
    }

    public class Union
    {
        public List<UnionCase> Cases;
        public Converter<object, int> TagReader { get; set; }
    }

    public class UnionCase
    {
        public int Tag;
        public string Name;
        public PropertyInfo[] Fields;
        public Converter<object, object[]> FieldReader;
        public Converter<object[], object> Constructor;
    }

    static Union CreateUnion(Type type)
    {
        var u = new Union
        {
            TagReader = s => FSharpValue.PreComputeUnionTagReader(type, null).Invoke(s),
            Cases = []
        };

        var cases = FSharpType.GetUnionCases(type, null);

        foreach (var unionCaseInfo in cases)
        {
            var unionCase = new UnionCase
            {
                Tag = unionCaseInfo.Tag,
                Name = unionCaseInfo.Name,
                Fields = unionCaseInfo.GetFields(),
                FieldReader = s => FSharpValue.PreComputeUnionReader(unionCaseInfo, null).Invoke(s),
                Constructor = s => FSharpValue.PreComputeUnionConstructor(unionCaseInfo, null).Invoke(s)
            };

            u.Cases.Add(unionCase);
        }

        return u;
    }

    [Fact]
    public void Serialize()
    {
        var value = Shape.NewRectangle(10.0, 5.0);

        var union = CreateUnion(value.GetType());

        var tag = union.TagReader.Invoke(value);

        var caseInfo = union.Cases.Single(_ => _.Tag == tag);

        var fields = caseInfo.FieldReader.Invoke(value);

        Assert.Equal(10d, fields[0]);
        Assert.Equal(5d, fields[1]);
    }

    [Fact]
    public void Deserialize()
    {
        var union = CreateUnion(typeof(Shape.Rectangle));

        var caseInfo = union.Cases.Single(_ => _.Name == "Rectangle");

        var value = (Shape.Rectangle) caseInfo.Constructor.Invoke([10.0, 5.0]);

        Assert.Equal("TestObjects.Shape+Rectangle", value.ToString());
        Assert.Equal(10, value.width);
        Assert.Equal(5, value.length);
    }

    [Fact]
    public void DeserializeBasicUnion_NoMatch()
    {
        var value = """{"Case":"abcdefg","Fields":[]}""";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>(value, unionConverter));
        Assert.Equal("No union type found with the name 'abcdefg'. Path 'Case', line 1, position 17.", exception.Message);
    }

    [Fact]
    public void DeserializeBasicUnion_MismatchedFieldCount()
    {
        var value = """{"Case":"AUD","Fields":[1]}""";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>(value, unionConverter));
        Assert.Equal("The number of field values does not match the number of properties defined by union 'AUD'. Path '', line 1, position 27.", exception.Message);
    }

    [Fact]
    public void DeserializeBasicUnion_NoCaseName()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>("""{"Fields":[1]}""", unionConverter));
        Assert.Equal("No 'Case' property with union name found. Path '', line 1, position 14.", exception.Message);
    }

    [Fact]
    public void DeserializeBasicUnion_UnexpectedEnd()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>("""{"Case":""", unionConverter));
        Assert.Equal("Unexpected end when reading JSON. Path 'Case', line 1, position 8.", exception.Message);
    }

    [Fact]
    public void DeserializeBasicUnion_FieldsObject()
    {
        var value = """{"Case":"AUD","Fields":{}}""";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>(value, unionConverter));
        Assert.Equal("Union fields must been an array. Path 'Fields', line 1, position 24.", exception.Message);
    }

    [Fact]
    public void DeserializeBasicUnion_UnexpectedProperty()
    {
        var value = """{"Case123":"AUD"}""";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Currency>(value, unionConverter));
        Assert.Equal("Unexpected property 'Case123' found when reading union. Path 'Case123', line 1, position 11.", exception.Message);
    }

    [Fact]
    public void SerializeUnionWithTypeNameHandlingAndReferenceTracking()
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All
        };
        settings.Converters.Add(unionConverter);
        var json = JsonConvert.SerializeObject(Shape.NewRectangle(10.0, 5.0), settings);

        Assert.Equal("""{"Case":"Rectangle","Fields":[10.0,5.0]}""", json);

        var c = JsonConvert.DeserializeObject<Shape>(json, unionConverter);
        Assert.True(c.IsRectangle);

        var r = (Shape.Rectangle) c;

        Assert.Equal(5.0, r.length);
        Assert.Equal(10.0, r.width);
    }
}