// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class GenericJsonConverterTests : TestFixtureBase
{
    // JsonConverter<T>.ReadJson returned null for any token whose Value is null,
    // including StartObject/StartArray, without consuming the token -> reader desync.
    [Fact]
    public void ReadsObjectTokenAndKeepsReaderAligned()
    {
        var holder = JsonConvert.DeserializeObject<Holder>("""{"Point":{"X":1,"Y":2},"After":42}""");

        Assert.NotNull(holder.Point);
        Assert.Equal(1, holder.Point.X);
        Assert.Equal(2, holder.Point.Y);
        Assert.Equal(42, holder.After);
    }

    [Fact]
    public void StillReturnsNullForJsonNull()
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

    public class TestGenericConverter : JsonConverter<string>
    {
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
            writer.WriteValue(value);

        public override string ReadJson(JsonReader reader, Type type, string existingValue, bool hasExisting, JsonSerializer serializer) =>
            (string) reader.Value + existingValue;
    }

    [Fact]
    public void WriteJsonObject()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        var converter = new TestGenericConverter();
        converter.WriteJson(jsonWriter, (object) "String!", null);

        Assert.Equal(
            """
            "String!"
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void WriteJsonGeneric()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        var converter = new TestGenericConverter();
        converter.WriteJson(jsonWriter, "String!", null);

        Assert.Equal(
            """
            "String!"
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void ReadJsonGenericExistingValueNull()
    {
        var sr = new StringReader("'String!'");
        var jsonReader = new JsonTextReader(sr);
        jsonReader.Read();

        var converter = new TestGenericConverter();
        var s = converter.ReadJson(jsonReader, typeof(string), null, false, null);

        Assert.Equal("String!", s);
    }

    [Fact]
    public void ReadJsonGenericExistingValueString()
    {
        var sr = new StringReader("'String!'");
        var jsonReader = new JsonTextReader(sr);
        jsonReader.Read();

        var converter = new TestGenericConverter();
        var s = converter.ReadJson(jsonReader, typeof(string), "Existing!", true, null);

        Assert.Equal("String!Existing!", s);
    }

    [Fact]
    public void ReadJsonObjectExistingValueNull()
    {
        var sr = new StringReader("'String!'");
        var jsonReader = new JsonTextReader(sr);
        jsonReader.Read();

        var converter = new TestGenericConverter();
        var s = (string) converter.ReadJson(jsonReader, typeof(string), null, null);

        Assert.Equal("String!", s);
    }

    [Fact]
    public void ReadJsonObjectExistingValueWrongType()
    {
        var sr = new StringReader("'String!'");
        var jsonReader = new JsonTextReader(sr);
        jsonReader.Read();

        var converter = new TestGenericConverter();

        var exception = Assert.Throws<JsonSerializationException>(() => converter.ReadJson(jsonReader, typeof(string), 12345, null));
        Assert.Equal("Converter cannot read JSON with the specified existing value. System.String is required.", exception.Message);
    }
}