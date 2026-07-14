// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable UseIndexFromEndExpression
public class JTokenWriterTest : TestFixtureBase
{
    [Fact]
    public void WritesSpanPropertyNameAndValue()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartObject();
        writer.WritePropertyName("name".AsSpan());
        writer.WriteValue("value".AsSpan());
        writer.WriteEndObject();

        var token = (JObject) writer.Token!;
        Assert.Single(token.Properties());
        Assert.Equal("value", (string) token["name"]!);
    }

    [Fact]
    public void WritesSpanValueIntoArray()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartArray();
        writer.WriteValue("a".AsSpan());
        writer.WriteValue("b".AsSpan());
        writer.WriteEndArray();

        var array = (JArray) writer.Token!;
        Assert.Equal(2, array.Count);
        Assert.Equal("a", (string) array[0]!);
        Assert.Equal("b", (string) array[1]!);
    }

    [Fact]
    public void WritesNullByteArrayAsSingleNull()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartArray();
        writer.WriteValue((byte[]) null);
        writer.WriteEndArray();

        var array = (JArray) writer.Token!;
        Assert.Single(array);
        Assert.Equal(JTokenType.Null, array[0].Type);
    }

    [Fact]
    public void WritesNullUriInObjectWithoutThrowing()
    {
        using var writer = new JTokenWriter();
        writer.WriteStartObject();
        writer.WritePropertyName("uri");
        writer.WriteValue((Uri) null);
        writer.WriteEndObject();

        var token = (JObject) writer.Token!;
        Assert.Equal(JTokenType.Null, token["uri"]!.Type);
    }

    [Fact]
    public void ValueFormatting()
    {
        var data = "Hello world."u8.ToArray();

        JToken root;
        using (var jsonWriter = new JTokenWriter())
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue('@');
            jsonWriter.WriteValue("\r\n\t\f\b?{\\r\\n\"\'");
            jsonWriter.WriteValue(true);
            jsonWriter.WriteValue(10);
            jsonWriter.WriteValue(10.99);
            jsonWriter.WriteValue(0.99);
            jsonWriter.WriteValue(0.000000000000000001d);
            jsonWriter.WriteValue(0.000000000000000001m);
            jsonWriter.WriteValue((string) null);
            jsonWriter.WriteValue("This is a string.");
            jsonWriter.WriteNull();
            jsonWriter.WriteUndefined();
            jsonWriter.WriteValue(data);
            jsonWriter.WriteEndArray();

            root = jsonWriter.Token;
        }

        Assert.IsType<JArray>(root);
        Assert.Equal(13, root.Children().Count());
        Assert.Equal("@", (string) root[0]);
        Assert.Equal("\r\n\t\f\b?{\\r\\n\"\'", (string) root[1]);
        Assert.True((bool) root[2]);
        Assert.Equal(10, (int) root[3]);
        Assert.Equal(10.99, (double) root[4]);
        Assert.Equal(0.99, (double) root[5]);
        Assert.Equal(0.000000000000000001d, (double) root[6]);
        Assert.Equal(0.000000000000000001m, (decimal) root[7]);
        Assert.Null((string) root[8]);
        Assert.Equal("This is a string.", (string) root[9]);
        Assert.Null(((JValue) root[10]).Value);
        Assert.Null(((JValue) root[11]).Value);
        Assert.Equal(data, (byte[]) root[12]);
    }

    [Fact]
    public void State()
    {
        using JsonWriter jsonWriter = new JTokenWriter();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);

        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);

        jsonWriter.WritePropertyName("CPU");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);

        jsonWriter.WriteValue("Intel");
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);

        jsonWriter.WritePropertyName("Drives");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);

        jsonWriter.WriteStartArray();
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteValue("DVD read/writer");
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteValue(new BigInteger(123));
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteValue(Array.Empty<byte>());
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteEnd();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);

        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);
    }

    [Fact]
    public void CurrentToken()
    {
        using var jsonWriter = new JTokenWriter();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        Assert.Null(jsonWriter.CurrentToken);

        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal(jsonWriter.Token, jsonWriter.CurrentToken);

        var o = (JObject) jsonWriter.Token;

        jsonWriter.WritePropertyName("CPU");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal(o.Property("CPU"), jsonWriter.CurrentToken);

        jsonWriter.WriteValue("Intel");
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal(o["CPU"], jsonWriter.CurrentToken);

        jsonWriter.WritePropertyName("Drives");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal(o.Property("Drives"), jsonWriter.CurrentToken);

        jsonWriter.WriteStartArray();
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal(o["Drives"], jsonWriter.CurrentToken);

        var a = (JArray) jsonWriter.CurrentToken;

        jsonWriter.WriteValue("DVD read/writer");
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal(a[a.Count - 1], jsonWriter.CurrentToken);

        jsonWriter.WriteValue(new BigInteger(123));
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal(a[a.Count - 1], jsonWriter.CurrentToken);

        jsonWriter.WriteValue(Array.Empty<byte>());
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal(a[a.Count - 1], jsonWriter.CurrentToken);

        jsonWriter.WriteEnd();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal(a, jsonWriter.CurrentToken);

        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        Assert.Equal(o, jsonWriter.CurrentToken);
    }

    [Fact]
    public void WriteComment()
    {
        var writer = new JTokenWriter();

        writer.WriteStartArray();
        writer.WriteComment("fail");
        writer.WriteEndArray();

        XUnitAssert.AreEqualNormalized(
            """
            [
              /*fail*/]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteBigInteger()
    {
        var writer = new JTokenWriter();

        writer.WriteStartArray();
        writer.WriteValue(new BigInteger(123));
        writer.WriteEndArray();

        var i = (JValue) writer.Token[0];

        Assert.Equal(new BigInteger(123), i.Value);
        Assert.Equal(JTokenType.Integer, i.Type);

        XUnitAssert.AreEqualNormalized(
            """
            [
              123
            ]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteRaw()
    {
        var writer = new JTokenWriter();

        writer.WriteStartArray();
        writer.WriteRaw("fail");
        writer.WriteRaw("fail");
        writer.WriteEndArray();

        // this is a bug. write raw shouldn't be autocompleting like this
        // hard to fix without introducing Raw and RawValue token types
        // meh
        XUnitAssert.AreEqualNormalized(
            """
            [
              fail,
              fail
            ]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteTokenWithParent()
    {
        var o = new JObject
        {
            ["prop1"] = new JArray(1),
            ["prop2"] = 1
        };

        var writer = new JTokenWriter();

        writer.WriteStartArray();

        writer.WriteToken(o.CreateReader());

        Assert.Equal(WriteState.Array, writer.WriteState);

        writer.WriteEndArray();

        Console.WriteLine(writer.Token.ToString());

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "prop1": [
                  1
                ],
                "prop2": 1
              }
            ]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteTokenWithPropertyParent()
    {
        var v = new JValue(1);

        var writer = new JTokenWriter();

        writer.WriteStartObject();
        writer.WritePropertyName("Prop1");

        writer.WriteToken(v.CreateReader());

        Assert.Equal(WriteState.Object, writer.WriteState);

        writer.WriteEndObject();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Prop1": 1
            }
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteValueTokenWithParent()
    {
        var v = new JValue(1);

        var writer = new JTokenWriter();

        writer.WriteStartArray();

        writer.WriteToken(v.CreateReader());

        Assert.Equal(WriteState.Array, writer.WriteState);

        writer.WriteEndArray();

        XUnitAssert.AreEqualNormalized(
            """
            [
              1
            ]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteEmptyToken()
    {
        var o = new JObject();
        var reader = o.CreateReader();
        while (reader.Read())
        {
        }

        var writer = new JTokenWriter();

        writer.WriteStartArray();

        writer.WriteToken(reader);

        Assert.Equal(WriteState.Array, writer.WriteState);

        writer.WriteEndArray();

        XUnitAssert.AreEqualNormalized("[]", writer.Token.ToString());
    }

    [Fact]
    public void WriteRawValue()
    {
        var writer = new JTokenWriter();

        writer.WriteStartArray();
        writer.WriteRawValue("fail");
        writer.WriteRawValue("fail");
        writer.WriteEndArray();

        XUnitAssert.AreEqualNormalized(
            """
            [
              fail,
              fail
            ]
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteDuplicatePropertyName()
    {
        var writer = new JTokenWriter();

        writer.WriteStartObject();

        writer.WritePropertyName("prop1");
        writer.WriteStartObject();
        writer.WriteEndObject();

        writer.WritePropertyName("prop1");
        writer.WriteStartArray();
        writer.WriteEndArray();

        writer.WriteEndObject();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "prop1": []
            }
            """,
            writer.Token.ToString());
    }

    [Fact]
    public void WriteTokenDirect()
    {
        JToken token;

        using (var jsonWriter = new JTokenWriter())
        {
            jsonWriter.WriteToken(JsonToken.StartArray);
            jsonWriter.WriteToken(JsonToken.Integer, 1);
            jsonWriter.WriteToken(JsonToken.StartObject);
            jsonWriter.WriteToken(JsonToken.PropertyName, "integer");
            jsonWriter.WriteToken(JsonToken.Integer, int.MaxValue);
            jsonWriter.WriteToken(JsonToken.PropertyName, "null-string");
            jsonWriter.WriteToken(JsonToken.String, null);
            jsonWriter.WriteToken(JsonToken.EndObject);
            jsonWriter.WriteToken(JsonToken.EndArray);

            token = jsonWriter.Token;
        }

        Assert.Equal("""[1,{"integer":2147483647,"null-string":null}]""", token.ToString(Formatting.None));
    }
}