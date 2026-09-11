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
        Assert.Equal(@"""hi""", JsonConvert.SerializeObject(new IntOrString("hi")));

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
        Assert.Equal("hi", JsonConvert.DeserializeObject<IntOrString>(@"""hi""").Value);

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
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<UnionPet>("""{"Unknown":1}"""));

        Assert.Contains("No case of union type", exception.Message);
        Assert.Contains("Cat", exception.Message);
        Assert.Contains("Dog", exception.Message);
    }

    [Fact]
    public void RoundTripThroughProperty()
    {
        var json = JsonConvert.SerializeObject(new HasUnion {Value = new(7)});

        Assert.Equal("""{"Value":7}""", json);
        Assert.Equal(7, JsonConvert.DeserializeObject<HasUnion>(json)!.Value.Value);
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
        Assert.Equal("null", JsonConvert.SerializeObject((IntOrString?) null));
        Assert.Null(JsonConvert.TryDeserializeObject<IntOrString?>("null"));
        Assert.Null(JsonConvert.TryDeserializeObject<int?>("null"));
    }

    // the built in converter is the last fallback, so a converter supplied by the caller wins
    [Fact]
    public void UserConverterWins()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = {new FixedUnionConverter()}
        };

        Assert.Equal(@"""fixed""", JsonConvert.SerializeObject(new IntOrString(42), settings));
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
