// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// nullable annotations are enabled so that the union case parameters carry real nullability
// metadata, which is what decides whether a case can hold a null payload
#nullable enable

public class UnionTests : TestFixtureBase
{
    [Fact]
    public void SerializeNumberCase() =>
        Assert.Equal("42", JsonConvert.SerializeObject(new IntOrString(42)));

    [Fact]
    public void SerializeStringCase() =>
        Assert.Equal(
            """
            "hi"
            """,
            JsonConvert.SerializeObject(new IntOrString("hi")));

    [Fact]
    public void SerializeObjectCase() =>
        Assert.Equal(
            """{"Name":"Tibbles","Indoor":true}""",
            JsonConvert.SerializeObject(new UnionPet(new Cat("Tibbles", true))));

    [Fact]
    public void SerializeArrayCase() =>
        Assert.Equal("""["a","b"]""", JsonConvert.SerializeObject(new IntOrList(["a", "b"])));

    [Fact]
    public void DeserializeNumberCase() =>
        Assert.Equal(42, JsonConvert.DeserializeObject<IntOrString>("42").Value);

    [Fact]
    public void DeserializeStringCase() =>
        Assert.Equal(
            "hi",
            JsonConvert.DeserializeObject<IntOrString>(
                """
                "hi"
                """).Value);

    [Fact]
    public void DeserializeObjectCase() =>
        Assert.Equal(
            new Cat("Tibbles", true),
            JsonConvert.DeserializeObject<UnionPet>("""{"Name":"Tibbles","Indoor":true}""").Value);

    [Fact]
    public void DeserializeArrayCase() =>
        Assert.Equal(
            new List<string> {"a", "b"},
            JsonConvert.DeserializeObject<IntOrList>("""["a","b"]""").Value);

    // Cat and Dog both serialize as JSON objects, so the case is chosen by comparing the payload's
    // property names against each candidate
    [Fact]
    public void DeserializeAmbiguousObjectCases()
    {
        var cat = JsonConvert.DeserializeObject<UnionPet>("""{"Name":"Tibbles","Indoor":true}""");
        var dog = JsonConvert.DeserializeObject<UnionPet>("""{"Name":"Rex","GoodBoy":true}""");

        Assert.Equal(new Cat("Tibbles", true), cat.Value);
        Assert.Equal(new Dog("Rex", true), dog.Value);
    }

    [Fact]
    public void DeserializeObjectCaseMatchingNeither()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<UnionPet>("""{"Unknown":1}"""));

        Assert.Contains("No case of union type", exception.Message);
        Assert.Contains("Cat", exception.Message);
        Assert.Contains("Dog", exception.Message);
    }

    [Fact]
    public void RoundTripThroughProperty()
    {
        var json = JsonConvert.SerializeObject(new HasUnion {Value = new(7)});

        Assert.Equal("""{"Value":7}""", json);
        Assert.Equal(7, JsonConvert.DeserializeObject<HasUnion>(json).Value.Value);
    }

    [Fact]
    public void CamelCaseAppliesToObjectCase()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var json = JsonConvert.SerializeObject(new UnionPet(new Cat("Tibbles", true)), settings);

        Assert.Equal("""{"name":"Tibbles","indoor":true}""", json);
        Assert.Equal(new Cat("Tibbles", true), JsonConvert.DeserializeObject<UnionPet>(json, settings).Value);
    }

    // no case of IntOrString accepts null, so null reads back as the zero initialized union rather
    // than constructing a case, mirroring the write side
    [Fact]
    public void DeserializeNullWithNoNullCase() =>
        Assert.Equal(default, JsonConvert.DeserializeObject<IntOrString>("null"));

    // the string case is declared nullable, so it is the one that takes a null payload
    [Fact]
    public void DeserializeNullWithNullCase() =>
        Assert.Null(JsonConvert.DeserializeObject<IntOrNullableString>("null").Value);

    [Fact]
    public void SerializeDefaultUnion() =>
        Assert.Equal("null", JsonConvert.SerializeObject(default(IntOrString)));

    // TryDeserializeObject is used because DeserializeObject<T> throws on a null result for any
    // nullable type, union or not
    [Fact]
    public void RoundTripNullableUnion()
    {
        Assert.Equal("null", JsonConvert.SerializeObject(null));
        Assert.Null(JsonConvert.TryDeserializeObject<IntOrString?>("null"));
        Assert.Null(JsonConvert.TryDeserializeObject<int?>("null"));
    }

    // the built in converter is the last fallback, so a converter supplied by the caller wins
    [Fact]
    public void UserConverterWins()
    {
        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new FixedUnionConverter()
            }
        };

        Assert.Equal(
            """
            "fixed"
            """,
            JsonConvert.SerializeObject(new IntOrString(42), settings));
    }

    // the marker is matched by full name because the attribute definition can come from a polyfill
    // compiled into the declaring assembly rather than from the target framework
    [Fact]
    public void UnionAttributeIsMatchableByFullName()
    {
        var names = typeof(IntOrString).GetCustomAttributesData()
            .Select(_ => _.AttributeType.FullName)
            .ToList();

        Assert.Contains("System.Runtime.CompilerServices.UnionAttribute", names);
    }

    // metadata properties describe the payload, so they are not compared against the cases
    [Fact]
    public void PreserveReferencesWithAmbiguousObjectCases()
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        var json = JsonConvert.SerializeObject(new UnionPet(new Dog("Rex", true)), settings);

        Assert.Equal("""{"$id":"1","Name":"Rex","GoodBoy":true}""", json);
        Assert.Equal(new Dog("Rex", true), JsonConvert.DeserializeObject<UnionPet>(json, settings).Value);
    }

    [Fact]
    public void SharedReferenceBetweenUnions()
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };
        var cat = new Cat("Tibbles", true);

        var json = JsonConvert.SerializeObject(new List<UnionPet> {new(cat), new(cat)}, settings);

        Assert.Contains("\"$ref\"", json);

        var result = JsonConvert.DeserializeObject<List<UnionPet>>(json, settings);

        Assert.Same(result[0].Value, result[1].Value);
    }

    [Theory]
    [InlineData(TypeNameHandling.Objects)]
    [InlineData(TypeNameHandling.All)]
    [InlineData(TypeNameHandling.Auto)]
    public void TypeNameSelectsObjectCase(TypeNameHandling handling)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = handling
        };

        var json = JsonConvert.SerializeObject(new UnionTwins(new TwinB("x")), settings);

        Assert.Contains("\"$type\"", json);
        Assert.Equal(new TwinB("x"), JsonConvert.DeserializeObject<UnionTwins>(json, settings).Value);
    }

    // TwinA and TwinB have the same properties, so without a type name the payload is ambiguous
    [Fact]
    public void IdenticalObjectCasesWithoutTypeName()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<UnionTwins>("""{"Name":"x"}"""));

        Assert.Contains("equally well", exception.Message);
    }

    [Fact]
    public void TypeNameOutsideTheCasesIsNotUsed()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        var json = $$"""{"$type":"{{typeof(Version).AssemblyQualifiedName}}","Name":"x"}""";

        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<UnionTwins>(json, settings));
    }

    [Theory]
    [InlineData(TypeNameHandling.Arrays)]
    [InlineData(TypeNameHandling.All)]
    [InlineData(TypeNameHandling.Auto)]
    public void TypeNameWrappedArrayCase(TypeNameHandling handling)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = handling
        };

        var json = JsonConvert.SerializeObject(new IntOrList(["a", "b"]), settings);

        Assert.Contains("\"$values\"", json);
        Assert.Equal(new List<string> {"a", "b"}, JsonConvert.DeserializeObject<IntOrList>(json, settings).Value);
    }

    [Fact]
    public void ReferenceWrappedArrayCase()
    {
        var settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };

        var json = JsonConvert.SerializeObject(new IntOrList(["a", "b"]), settings);

        Assert.Equal("""{"$id":"1","$values":["a","b"]}""", json);
        Assert.Equal(new List<string> {"a", "b"}, JsonConvert.DeserializeObject<IntOrList>(json, settings).Value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(1.5)]
    public void FloatingPointCase(double value)
    {
        var json = JsonConvert.SerializeObject(new DoubleOrBool(value));

        Assert.Equal(JsonConvert.SerializeObject(value), json);
        Assert.Equal(value, JsonConvert.DeserializeObject<DoubleOrBool>(json).Value);
    }

    // only NaN and the infinities are read from a string. any other string is not a number
    [Fact]
    public void OtherStringsDoNotSelectFloatingPointCase()
    {
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DoubleOrBool>("\"1.5\""));
        Assert.Equal("1.5", JsonConvert.DeserializeObject<DoubleOrString>("\"1.5\"").Value);
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DoubleOrString>("\"NaN\""));
    }

    [Fact]
    public void EnumCaseWithStringEnumConverter()
    {
        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter()
            }
        };

        var json = JsonConvert.SerializeObject(new EnumOrBool(DayOfWeek.Monday), settings);

        Assert.Equal(
            """
            "Monday"
            """,
            json);
        Assert.Equal(DayOfWeek.Monday, JsonConvert.DeserializeObject<EnumOrBool>(json, settings).Value);
        Assert.Equal(DayOfWeek.Tuesday, JsonConvert.DeserializeObject<EnumOrBool>("2", settings).Value);
        Assert.Equal(true, JsonConvert.DeserializeObject<EnumOrBool>("true", settings).Value);
    }

    // a converter decides its own JSON, so the case is matched whatever shape the payload has
    [Fact]
    public void CaseWithConverter()
    {
        var json = JsonConvert.SerializeObject(new MoneyOrInt(new Money(12, "USD")));

        Assert.Equal(
            """
            "12 USD"
            """,
            json);
        Assert.Equal(new Money(12, "USD"), JsonConvert.DeserializeObject<MoneyOrInt>(json).Value);
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<MoneyOrInt>("5"));
    }

    [Fact]
    public void LinqCases()
    {
        var o = JsonConvert.DeserializeObject<JObjectOrInt>("""{"a":1}""");

        Assert.Equal(1, (int) ((JObject) o.Value!)["a"]!);
        Assert.Equal(5, JsonConvert.DeserializeObject<JObjectOrInt>("5").Value);
        Assert.Equal("""{"a":1}""", JsonConvert.SerializeObject(o));
    }

    [Fact]
    public void ObjectCase()
    {
        Assert.Equal("text", JsonConvert.DeserializeObject<ObjectOrNothing>("\"text\"").Value);
        Assert.Equal(5L, JsonConvert.DeserializeObject<ObjectOrNothing>("5").Value);
    }

    // the payload is buffered before a case is chosen, so floats are parsed as double unless the
    // reader is told otherwise
    [Fact]
    public void DecimalCasePrecision()
    {
        const string json = "1.1234567890123456789012345";
        var settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        Assert.Equal(1.1234567890123456789012345m, JsonConvert.DeserializeObject<DecimalOrString>(json, settings).Value);
        Assert.NotEqual(1.1234567890123456789012345m, JsonConvert.DeserializeObject<DecimalOrString>(json).Value);
    }


#if NET11_0_OR_GREATER

    [Theory]
    [InlineData(42)]
    [InlineData("hi")]
    public void MatchesSystemTextJson(object value)
    {
        var union = value is int number ? new IntOrString(number) : new IntOrString((string) value);

        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(union),
            JsonConvert.SerializeObject(union));
    }

#endif

    public class FixedUnionConverter : JsonConverter
    {
        public override bool CanConvert(Type type) =>
            type == typeof(IntOrString);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue("fixed");

        public override object ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer) =>
            new IntOrString(0);
    }

    public class HasUnion
    {
        public IntOrString Value { get; set; }
    }
}

public union IntOrString(int, string);

public union IntOrNullableString(int, string?);

public union IntOrList(int, List<string>);

public union UnionPet(Cat, Dog);

public record Cat(string Name, bool Indoor);

public record Dog(string Name, bool GoodBoy);

public union UnionTwins(TwinA, TwinB);

public record TwinA(string Name);

public record TwinB(string Name);

public union DoubleOrBool(double, bool);

public union DoubleOrString(double, string);

public union EnumOrBool(DayOfWeek, bool);

public union MoneyOrInt(Money, int);

public union JObjectOrInt(JObject, int);

public union ObjectOrNothing(object);

public union DecimalOrString(decimal, string);

[JsonConverter(typeof(MoneyConverter))]
public record Money(decimal Amount, string Currency);

public class MoneyConverter : JsonConverter
{
    public override bool CanConvert(Type type) =>
        type == typeof(Money);

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var money = (Money) value;
        writer.WriteValue($"{money.Amount.ToString(InvariantCulture)} {money.Currency}");
    }

    public override object ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.Value is not string text)
        {
            throw new JsonSerializationException("Expected a string.");
        }

        var parts = text.Split(' ');
        return new Money(decimal.Parse(parts[0], InvariantCulture), parts[1]);
    }
}
