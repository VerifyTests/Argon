// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime.Utility;
using Formatting = Argon.Formatting;

public class NodaConverterBaseTest
{
    [Fact]
    public void Serialize_NonNullValue()
    {
        var converter = new TestConverter();

        JsonConvert.SerializeObject(5, Formatting.None, converter);
    }

    [Fact]
    public void Serialize_NullValue()
    {
        var converter = new TestConverter();

        JsonConvert.SerializeObject(null, Formatting.None, converter);
    }

    [Fact]
    public void Deserialize_NullableType_NullValue()
    {
        var converter = new TestConverter();

        Assert.Null(JsonConvert.TryDeserializeObject<int?>("null", converter));
    }

    [Fact]
    public void Deserialize_ReferenceType_NullValue()
    {
        var converter = new TestStringConverter();

        Assert.Null(JsonConvert.TryDeserializeObject<string>("null", converter));
    }

    [Fact]
    public void Deserialize_NullableType_NonNullValue()
    {
        var converter = new TestConverter();

        Assert.Equal(5, JsonConvert.DeserializeObject<int?>("\"5\"", converter));
    }

    [Fact]
    public void Deserialize_NonNullableType_NullValue()
    {
        var converter = new TestConverter();

        Assert.Throws<InvalidNodaDataException>(() => JsonConvert.DeserializeObject<int>("null", converter));
    }

    [Fact]
    public void Deserialize_NonNullableType_NonNullValue()
    {
        var converter = new TestConverter();

        Assert.Equal(5, JsonConvert.DeserializeObject<int>("\"5\"", converter));
    }

    [Fact]
    public void CanConvert_ValidValues()
    {
        var converter = new TestConverter();

        Assert.True(converter.CanConvert(typeof(int)));
        Assert.True(converter.CanConvert(typeof(int?)));
    }

    [Fact]
    public void CanConvert_InvalidValues()
    {
        var converter = new TestConverter();

        Assert.False(converter.CanConvert(typeof(uint)));
    }

    [Fact]
    public void CanConvert_Inheritance()
    {
        var converter = new TestInheritanceConverter();

        Assert.True(converter.CanConvert(typeof(MemoryStream)));
    }

    class TestConverter : NodaConverterBase<int>
    {
        protected override int ReadJsonImpl(JsonReader reader, JsonSerializer serializer) =>
            int.Parse(reader.Value.ToString());

        protected override void WriteJsonImpl(JsonWriter writer, int value, JsonSerializer serializer) =>
            writer.WriteValue(value.ToString());
    }

    class TestStringConverter : NodaConverterBase<string>
    {
        protected override string ReadJsonImpl(JsonReader reader, JsonSerializer serializer) =>
            reader.Value.ToString();

        protected override void WriteJsonImpl(JsonWriter writer, string value, JsonSerializer serializer) =>
            writer.WriteValue(value);
    }

    /// <summary>
    /// Just use for CanConvert testing...
    /// </summary>
    class TestInheritanceConverter : NodaConverterBase<Stream>
    {
        protected override Stream ReadJsonImpl(JsonReader reader, JsonSerializer serializer) =>
            throw new NotImplementedException();

        protected override void WriteJsonImpl(JsonWriter writer, Stream value, JsonSerializer serializer) =>
            throw new NotImplementedException();
    }
}