// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class FloatTests : TestFixtureBase
{
    // BoxedPrimitives.Get(decimal) returned a cached positive-zero box for -0.0m, so the
    // negative-zero sign was dropped at scale 0/1 but kept at higher scales.
    [Fact]
    public void NegativeZeroDecimalPreservesSignAcrossScales()
    {
        Assert.True(HasNegativeSignBit(ReadDecimal("-0.0")));
        Assert.True(HasNegativeSignBit(ReadDecimal("-0.00")));
        Assert.False(HasNegativeSignBit(ReadDecimal("0.0")));

        // ToString is unaffected (decimal never renders a negative-zero sign).
        Assert.Equal("0.0", ReadDecimal("-0.0").ToString(InvariantCulture));
    }

    static bool HasNegativeSignBit(decimal value) =>
        (decimal.GetBits(value)[3] & int.MinValue) != 0;

    static decimal ReadDecimal(string json)
    {
        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = Argon.FloatParseHandling.Decimal
        };
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.Float)
            {
                return (decimal) reader.Value;
            }
        }

        throw new InvalidOperationException("no float token");
    }

    [Fact]
    public void Float_ReadAsString_Exact()
    {
        const string testJson = "{float: 0.0620}";

        var reader = new JsonTextReader(new StringReader(testJson));
        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var s = reader.ReadAsString();
        Assert.Equal("0.0620", s);
    }

    [Fact]
    public void Float_NaN_Read()
    {
        const string testJson = "{float: NaN}";

        var reader = new JsonTextReader(new StringReader(testJson));

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.True(reader.Read());

        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(double.NaN, reader.Value);

        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void Float_NaN_ReadAsInt32()
    {
        const string testJson = "{float: NaN}";

        var reader = new JsonTextReader(new StringReader(testJson));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Cannot read NaN value. Path 'float', line 1, position 11.", exception.Message);
    }

    [Fact]
    public void Float_NaNAndInfinity_ReadAsDouble()
    {
        const string testJson = """
                                [
                                  NaN,
                                  Infinity,
                                  -Infinity
                                ]
                                """;

        var reader = new JsonTextReader(new StringReader(testJson));

        Assert.True(reader.Read());

        Assert.Equal(double.NaN, reader.ReadAsDouble());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(double.NaN, reader.Value);

        Assert.Equal(double.PositiveInfinity, reader.ReadAsDouble());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(double.PositiveInfinity, reader.Value);

        Assert.Equal(double.NegativeInfinity, reader.ReadAsDouble());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(double.NegativeInfinity, reader.Value);

        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void Float_NaNAndInfinity_ReadAsString()
    {
        const string testJson = """
                                [
                                  NaN,
                                  Infinity,
                                  -Infinity
                                ]
                                """;

        var reader = new JsonTextReader(new StringReader(testJson));

        Assert.True(reader.Read());

        Assert.Equal(JsonConvert.NaN, reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(JsonConvert.NaN, reader.Value);

        Assert.Equal(JsonConvert.PositiveInfinity, reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(JsonConvert.PositiveInfinity, reader.Value);

        Assert.Equal(JsonConvert.NegativeInfinity, reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(JsonConvert.NegativeInfinity, reader.Value);

        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void FloatParseHandling_ReadAsString()
    {
        var json = "[9223372036854775807, 1.7976931348623157E+308, 792281625142643375935439503.35, 792281625142643375935555555555555555555555555555555555555555555555555439503.35]";

        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = Argon.FloatParseHandling.Decimal
        };

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.Equal("9223372036854775807", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal("9223372036854775807", reader.Value);

        Assert.Equal("1.7976931348623157E+308", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal("1.7976931348623157E+308", reader.Value);

        Assert.Equal("792281625142643375935439503.35", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal("792281625142643375935439503.35", reader.Value);

        Assert.Equal("792281625142643375935555555555555555555555555555555555555555555555555439503.35", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal("792281625142643375935555555555555555555555555555555555555555555555555439503.35", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    [Fact]
    public void FloatParseHandling()
    {
        var json = "[1.0,1,9.9,1E-06]";

        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = Argon.FloatParseHandling.Decimal
        };

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(1.0m, reader.Value);
        Assert.Equal(typeof(decimal), reader.ValueType);
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(1L, reader.Value);
        Assert.Equal(typeof(long), reader.ValueType);
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(9.9m, reader.Value);
        Assert.Equal(typeof(decimal), reader.ValueType);
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(0.000001m, reader.Value);
        Assert.Equal(typeof(decimal), reader.ValueType);
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    [Fact]
    public void FloatParseHandling_NaN()
    {
        var json = "[NaN]";

        var reader = new JsonTextReader(new StringReader(json))
        {
            FloatParseHandling = Argon.FloatParseHandling.Decimal
        };

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Cannot read NaN value. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers()
    {
        var input = """
                    [
                      NaN,
                      Infinity,
                      -Infinity
                    ]
                    """;

        var sr = new StringReader(input);

        using var jsonReader = new JsonTextReader(sr);
        jsonReader.Read();
        Assert.Equal(JsonToken.StartArray, jsonReader.TokenType);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, double.NaN);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, double.PositiveInfinity);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, double.NegativeInfinity);

        jsonReader.Read();
        Assert.Equal(JsonToken.EndArray, jsonReader.TokenType);
    }

    [Fact]
    public void ReadFloatingPointNumber()
    {
        var json = "[0.0,0.0,0.1,1.0,1.000001,1E-06,4.94065645841247E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN,0e-10,0.25e-5,0.3e10]";

        using var jsonReader = new JsonTextReader(new StringReader(json));
        jsonReader.Read();
        Assert.Equal(JsonToken.StartArray, jsonReader.TokenType);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(0.0, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(0.0, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(0.1, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(1.0, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(1.000001, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(1E-06, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(4.94065645841247E-324, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.PositiveInfinity, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.NegativeInfinity, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.NaN, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.MaxValue, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.MinValue, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.PositiveInfinity, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.NegativeInfinity, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(double.NaN, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(0d, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(0.0000025d, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.Float, jsonReader.TokenType);
        Assert.Equal(3000000000d, jsonReader.Value);

        jsonReader.Read();
        Assert.Equal(JsonToken.EndArray, jsonReader.TokenType);
    }
}