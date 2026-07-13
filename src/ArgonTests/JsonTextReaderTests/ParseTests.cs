// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ParseTests : TestFixtureBase
{
    [Fact]
    public void ParseAdditionalContent_Whitespace()
    {
        var json = """
            [
                "Small",
                "Medium",
                "Large"
            ]


            """;

        var reader = new JsonTextReader(new StringReader(json));
        while (reader.Read())
        {
        }
    }

    [Fact]
    public void ParsingQuotedPropertyWithControlCharacters()
    {
        var reader = new JsonTextReader(new StringReader(@"{'hi\r\nbye':1}"));
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);
        Assert.Equal("hi\r\nbye", reader.Value);
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);
        Assert.Equal(1L, reader.Value);
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
        Assert.False(reader.Read());
    }

    [Fact]
    public void ParseIntegers()
    {
        var reader = new JsonTextReader(new StringReader("1"));
        Assert.Equal(1, reader.ReadAsInt32());

        reader = new(new StringReader("-1"));
        Assert.Equal(-1, reader.ReadAsInt32());

        reader = new(new StringReader("0"));
        Assert.Equal(0, reader.ReadAsInt32());

        reader = new(new StringReader("-0"));
        Assert.Equal(0, reader.ReadAsInt32());

        reader = new(new StringReader(int.MaxValue.ToString()));
        Assert.Equal(int.MaxValue, reader.ReadAsInt32());

        reader = new(new StringReader(int.MinValue.ToString()));
        Assert.Equal(int.MinValue, reader.ReadAsInt32());

        reader = new(new StringReader(long.MaxValue.ToString()));
        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("JSON integer 9223372036854775807 is too large or small for an Int32. Path '', line 1, position 19.", exception.Message);

        reader = new(new StringReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd"));
        var exception1 = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing number: s. Path '', line 1, position 77.", exception1.Message);

        reader = new(new StringReader("1E-06"));
        var exception2 = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Input string '1E-06' is not a valid integer. Path '', line 1, position 5.", exception2.Message);

        reader = new(new StringReader("1.1"));
        var exception3 = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Input string '1.1' is not a valid integer. Path '', line 1, position 3.", exception3.Message);

        reader = new(new StringReader(""));
        Assert.Null(reader.ReadAsInt32());

        reader = new(new StringReader("-"));
        var exception4 = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Input string '-' is not a valid integer. Path '', line 1, position 1.", exception4.Message);
    }

    [Fact]
    public void ParseDecimals()
    {
        var reader = new JsonTextReader(new StringReader("1.1"));
        Assert.Equal(1.1m, reader.ReadAsDecimal());

        reader = new(new StringReader("-1.1"));
        Assert.Equal(-1.1m, reader.ReadAsDecimal());

        reader = new(new StringReader("0.0"));
        Assert.Equal(0.0m, reader.ReadAsDecimal());

        reader = new(new StringReader("-0.0"));
        Assert.Equal(0, reader.ReadAsDecimal());

        reader = new(new StringReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDecimal());
        Assert.Equal("Unexpected character encountered while parsing number: s. Path '', line 1, position 77.", exception.Message);

        reader = new(new StringReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd"))
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
        var exception1 = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected character encountered while parsing number: s. Path '', line 1, position 77.", exception1.Message);

        reader = new(new StringReader("1E-06"));
        Assert.Equal(0.000001m, reader.ReadAsDecimal());

        reader = new(new StringReader(""));
        Assert.Null(reader.ReadAsDecimal());

        reader = new(new StringReader("-"));
        var exception2 = Assert.Throws<JsonReaderException>(() => reader.ReadAsDecimal());
        Assert.Equal("Input string '-' is not a valid decimal. Path '', line 1, position 1.", exception2.Message);
    }

    [Fact]
    public void ParseDoubles()
    {
        var reader = new JsonTextReader(new StringReader("1.1"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(1.1d, reader.Value);

        reader = new(new StringReader("-1.1"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(-1.1d, reader.Value);

        reader = new(new StringReader("0.0"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(0.0d, reader.Value);

        reader = new(new StringReader("-0.0"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(-0.0d, reader.Value);

        reader = new(new StringReader("9999999999999999999999999999999999999999999999999999999999999999999999999999asdasdasd"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected character encountered while parsing number: s. Path '', line 1, position 77.", exception.Message);

        reader = new(new StringReader("1E-06"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(0.000001d, reader.Value);

        reader = new(new StringReader(""));
        Assert.False(reader.Read());

        reader = new(new StringReader("-"));
        var exception1 = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Input string '-' is not a valid number. Path '', line 1, position 1.", exception1.Message);

        reader = new(new StringReader("1.7976931348623157E+308"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(double.MaxValue, reader.Value);

        reader = new(new StringReader("-1.7976931348623157E+308"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(double.MinValue, reader.Value);

        reader = new(new StringReader("1E+309"));
#if !NET6_0_OR_GREATER
        var exception2 = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Input string '1E+309' is not a valid number. Path '', line 1, position 6.", exception2.Message);
#else
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(double.PositiveInfinity, reader.Value);
#endif

        reader = new(new StringReader("-1E+5000"));
#if !NET6_0_OR_GREATER
        var exception3 = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Input string '-1E+5000' is not a valid number. Path '', line 1, position 8.", exception3.Message);
#else
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(double.NegativeInfinity, reader.Value);
#endif

        reader = new(new StringReader("5.1231231E"));
        var exception4 = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Input string '5.1231231E' is not a valid number. Path '', line 1, position 10.", exception4.Message);

        reader = new(new StringReader("1E-23"));
        Assert.True(reader.Read());
        Assert.Equal(typeof(double), reader.ValueType);
        Assert.Equal(1e-23, reader.Value);
    }

    [Fact]
    public void ParseArrayWithMissingValues()
    {
        var json = "[,,, \n\r\n \0   \r  , ,    ]";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    [Fact]
    public void ParseBooleanWithNoExtraContent()
    {
        var json = "[true ";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public Task ParseContentDelimitedByNonStandardWhitespace()
    {
        var json = "\x00a0{\x00a0'h\x00a0i\x00a0'\x00a0:\x00a0[\x00a0true\x00a0,'2014-06-04T00:00:00Z'\x00a0]\x00a0/*\x00a0comment\x00a0*/\x00a0}\x00a0";
        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));
        return reader.VerifyReaderState();
    }

    [Fact]
    public void ParseObjectWithNoEnd()
    {
        var json = "{hi:1, ";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void ParseEmptyArray()
    {
        var json = "[]";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    [Fact]
    public void ParseEmptyObject()
    {
        var json = "{}";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ParseNumbers()
    {
        var json = "[0,1,2 , 3]";

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    internal const long InitialJavaScriptDateTicks = 621355968000000000;
}
