// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;
using WriteState = Argon.WriteState;

public class JsonTextWriterTest : TestFixtureBase
{
    [Fact]
    public void WriteValueStringBuilderMultichunkWritesSingleStringInArray()
    {
        var builder = new StringBuilder();
        builder.Append('a', 10);
        builder.Append('b', 9000);

        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            writer.WriteValue(builder);
            writer.WriteEndArray();
        }

        Assert.Equal($"[\"{builder}\"]", stringWriter.ToString());
    }

    [Fact]
    public void WriteValueStringBuilderMultichunkWritesSingleStringAsProperty()
    {
        var builder = new StringBuilder();
        builder.Append('x', 10000);

        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("p");
            writer.WriteValue(builder);
            writer.WriteEndObject();
        }

        Assert.Equal($"{{\"p\":\"{builder}\"}}", stringWriter.ToString());
    }

    [Fact]
    public void WriteValueStringBuilderNullWritesNull()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteValue((StringBuilder) null);
        }

        Assert.Equal("null", stringWriter.ToString());
    }

    [Fact]
    public void WriteValueCharHonorsQuoteChar()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter) {QuoteChar = '\''})
        {
            writer.WriteValue('c');
        }

        Assert.Equal("'c'", stringWriter.ToString());
    }

    [Fact]
    public void WriteValueCharHonorsQuoteValue()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter) {QuoteValue = false})
        {
            writer.WriteValue('c');
        }

        Assert.Equal("c", stringWriter.ToString());
    }

    [Fact]
    public void WriteValueCharDefaultIsDoubleQuoted()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteValue('c');
        }

        Assert.Equal("\"c\"", stringWriter.ToString());
    }

    [Fact]
    public void WritesSpanComment()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteStartArray();
            writer.WriteComment("hello".AsSpan());
            writer.WriteEndArray();
        }

        Assert.Contains("/*hello*/", stringWriter.ToString());
    }

    [Fact]
    public void WritesSpanWhitespace()
    {
        var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter))
        {
            writer.WriteWhitespace("   ".AsSpan());
            writer.WriteValue(1);
        }

        Assert.Equal("   1", stringWriter.ToString());
    }

#if !RELEASE
    [Fact]
    public void BufferErroringWithInvalidSize()
    {
        var o = new JObject
        {
            {"BodyHtml", $"<h3>Title!</h3>{Environment.NewLine}{new string(' ', 100)}<p>Content!</p>"}
        };

        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            o.WriteTo(jsonWriter);
        }

        var result = o.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "BodyHtml": "<h3>Title!</h3>\r\n                                                                                                    <p>Content!</p>"
            }
            """, result);
    }
#endif

    [Fact]
    public void NewLine()
    {
        var ms = new MemoryStream();

        using (var streamWriter = new StreamWriter(ms, new UTF8Encoding(false))
               {
                   NewLine = "\n"
               })
        using (var jsonWriter = new JsonTextWriter(streamWriter)
               {
                   CloseOutput = true,
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("prop");
            jsonWriter.WriteValue(true);
            jsonWriter.WriteEndObject();
        }

        var data = ms.ToArray();

        var json = Encoding.UTF8.GetString(data, 0, data.Length);

        XUnitAssert.EqualsNormalized(
            """
            {
              "prop": true
            }
            """,
            json);
    }

    [Fact]
    public void QuoteNameAndStrings()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            QuoteName = false
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteValue("value");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal(
            """{name:"value"}""",
            stringWriter.ToString());
    }

    [Fact]
    public void QuoteValueAndStrings()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            QuoteValue = false
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name");
        jsonWriter.WriteValue("value");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal(
            """{"name":value}""",
            stringWriter.ToString());
    }

    [Fact]
    public void EscapeHandlingNone()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.None
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name\"€");
        jsonWriter.WriteValue("\u5f20\"");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal("""{"name"€":"张""}""", stringWriter.ToString());
    }

    [Fact]
    public void EscapeHandlingDefault()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.Default
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name\"€");
        jsonWriter.WriteValue("\"€");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal("""{"name\"€":"\"€"}""", stringWriter.ToString());
    }

    [Fact]
    public void EscapeHandlingEscapeHtml()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeHtml
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name\"€");
        jsonWriter.WriteValue("\"€");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal("""{"name\u0022€":"\u0022€"}""", stringWriter.ToString());
    }

    [Fact]
    public void EscapeHandlingEscapeNonAscii()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeNonAscii
        };

        jsonWriter.WriteStartObject();

        jsonWriter.WritePropertyName("name\"€");
        jsonWriter.WriteValue("\"€");

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        Assert.Equal("""{"name\"\u20ac":"\"\u20ac"}""", stringWriter.ToString());
    }

    [Fact]
    public void CloseOutput()
    {
        var ms = new MemoryStream();
        var writer = new JsonTextWriter(new StreamWriter(ms));

        Assert.True(ms.CanRead);
        writer.Close();
        Assert.False(ms.CanRead);

        ms = new();
        writer = new(new StreamWriter(ms)) {CloseOutput = false};

        Assert.True(ms.CanRead);
        writer.Close();
        Assert.True(ms.CanRead);
    }

    [Fact]
    public void WriteIConvertible()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteValue(new ConvertibleInt(1));

        Assert.Equal("1", stringWriter.ToString());
    }

    [Fact]
    public void ValueFormatting()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter))
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
            jsonWriter.WriteValue((object) null);
            jsonWriter.WriteValue("This is a string.");
            jsonWriter.WriteNull();
            jsonWriter.WriteUndefined();
            jsonWriter.WriteEndArray();
        }

        var expected = """["@","\r\n\t\f\b?{\\r\\n\"'",true,10,10.99,0.99,1E-18,0.000000000000000001,null,null,"This is a string.",null,undefined]""";
        var result = stringWriter.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NullableValueFormatting()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue((char?) null);
            jsonWriter.WriteValue((char?) 'c');
            jsonWriter.WriteValue((bool?) null);
            jsonWriter.WriteValue((bool?) true);
            jsonWriter.WriteValue((byte?) null);
            jsonWriter.WriteValue((byte?) 1);
            jsonWriter.WriteValue((sbyte?) null);
            jsonWriter.WriteValue((sbyte?) 1);
            jsonWriter.WriteValue((short?) null);
            jsonWriter.WriteValue((short?) 1);
            jsonWriter.WriteValue((ushort?) null);
            jsonWriter.WriteValue((ushort?) 1);
            jsonWriter.WriteValue((int?) null);
            jsonWriter.WriteValue((int?) 1);
            jsonWriter.WriteValue((uint?) null);
            jsonWriter.WriteValue((uint?) 1);
            jsonWriter.WriteValue((long?) null);
            jsonWriter.WriteValue((long?) 1);
            jsonWriter.WriteValue((ulong?) null);
            jsonWriter.WriteValue((ulong?) 1);
            jsonWriter.WriteValue((double?) null);
            jsonWriter.WriteValue((double?) 1.1);
            jsonWriter.WriteValue((float?) null);
            jsonWriter.WriteValue((float?) 1.1);
            jsonWriter.WriteValue((decimal?) null);
            jsonWriter.WriteValue((decimal?) 1.1m);
            jsonWriter.WriteValue((DateTime?) null);
            jsonWriter.WriteValue((DateTime?) new DateTime(ParseTests.InitialJavaScriptDateTicks, DateTimeKind.Utc));
            jsonWriter.WriteValue((DateTimeOffset?) null);
            jsonWriter.WriteValue((DateTimeOffset?) new DateTimeOffset(ParseTests.InitialJavaScriptDateTicks, TimeSpan.Zero));
            jsonWriter.WriteEndArray();
        }

        var json = stringWriter.ToString();

        var expected = """[null,"c",null,true,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1,null,1.1,null,1.1,null,1.1,null,"1970-01-01T00:00:00Z",null,"1970-01-01T00:00:00+00:00"]""";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void WriteValueObjectWithNullable()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            char? value = 'c';

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue((object) value);
            jsonWriter.WriteEndArray();
        }

        var json = stringWriter.ToString();
        var expected = """["c"]""";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void WriteValueObjectWithUnsupportedValue()
    {
        var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteStartArray();
        var exception = Assert.Throws<JsonWriterException>(() => jsonWriter.WriteValue(new Version(1, 1, 1, 1)));
        Assert.Equal("Unsupported type: System.Version. Use the JsonSerializer class to get the object's JSON representation. Path ''.", exception.Message);
    }

    [Fact]
    public void StringEscaping()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(
                """
                "These pretzels are making me thirsty!"
                """);
            jsonWriter.WriteValue("Jeff's house was burninated.");
            jsonWriter.WriteValue("1. You don't talk about fight club.\r\n2. You don't talk about fight club.");
            jsonWriter.WriteValue("35% of\t statistics\n are made\r up.");
            jsonWriter.WriteEndArray();
        }

        var expected = """["\"These pretzels are making me thirsty!\"","Jeff's house was burninated.","1. You don't talk about fight club.\r\n2. You don't talk about fight club.","35% of\t statistics\n are made\r up."]""";
        var result = stringWriter.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteEnd()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.WriteEndObject();
            Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        }

        var expected = """
            {
              "CPU": "Intel",
              "PSU": "500W",
              "Drives": [
                "DVD read/writer"
                /*(broken)*/,
                "500 gigabyte hard drive",
                "200 gigabyte hard drive"
              ]
            }
            """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void CloseWithRemainingContent()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.Close();
        }

        var expected = """
            {
              "CPU": "Intel",
              "PSU": "500W",
              "Drives": [
                "DVD read/writer"
                /*(broken)*/,
                "500 gigabyte hard drive",
                "200 gigabyte hard drive"
              ]
            }
            """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void Indenting()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("CPU");
            jsonWriter.WriteValue("Intel");
            jsonWriter.WritePropertyName("PSU");
            jsonWriter.WriteValue("500W");
            jsonWriter.WritePropertyName("Drives");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue("DVD read/writer");
            jsonWriter.WriteComment("(broken)");
            jsonWriter.WriteValue("500 gigabyte hard drive");
            jsonWriter.WriteValue("200 gigabyte hard drive");
            jsonWriter.WriteEnd();
            jsonWriter.WriteEndObject();
            Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        }

        // {
        //   "CPU": "Intel",
        //   "PSU": "500W",
        //   "Drives": [
        //     "DVD read/writer"
        //     /*(broken)*/,
        //     "500 gigabyte hard drive",
        //     "200 gigabyte hard drive"
        //   ]
        // }

        var expected = """
            {
              "CPU": "Intel",
              "PSU": "500W",
              "Drives": [
                "DVD read/writer"
                /*(broken)*/,
                "500 gigabyte hard drive",
                "200 gigabyte hard drive"
              ]
            }
            """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void State()
    {
        var stringWriter = new StringWriter();

        using var jsonWriter = new JsonTextWriter(stringWriter);
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);

        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("", jsonWriter.Path);

        jsonWriter.WritePropertyName("CPU");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("CPU", jsonWriter.Path);

        jsonWriter.WriteValue("Intel");
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("CPU", jsonWriter.Path);

        jsonWriter.WritePropertyName("Drives");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("Drives", jsonWriter.Path);

        jsonWriter.WriteStartArray();
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);

        jsonWriter.WriteValue("DVD read/writer");
        Assert.Equal(WriteState.Array, jsonWriter.WriteState);
        Assert.Equal("Drives[0]", jsonWriter.Path);

        jsonWriter.WriteEnd();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        Assert.Equal("Drives", jsonWriter.Path);

        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);
        Assert.Equal("", jsonWriter.Path);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_Symbol()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = """
                       [
                         NaN,
                         Infinity,
                         -Infinity,
                         NaN,
                         Infinity,
                         -Infinity
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_Zero()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
                   FloatFormatHandling = FloatFormatHandling.DefaultValue
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteValue((double?) double.NaN);
            jsonWriter.WriteValue((double?) double.PositiveInfinity);
            jsonWriter.WriteValue((double?) double.NegativeInfinity);
            jsonWriter.WriteValue((float?) float.NaN);
            jsonWriter.WriteValue((float?) float.PositiveInfinity);
            jsonWriter.WriteValue((float?) float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = """
                       [
                         0.0,
                         0.0,
                         0.0,
                         0.0,
                         0.0,
                         0.0,
                         null,
                         null,
                         null,
                         null,
                         null,
                         null
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_String()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
                   FloatFormatHandling = FloatFormatHandling.String
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = """
                       [
                         "NaN",
                         "Infinity",
                         "-Infinity",
                         "NaN",
                         "Infinity",
                         "-Infinity"
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void FloatingPointNonFiniteNumbers_QuoteChar()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
                   FloatFormatHandling = FloatFormatHandling.String,
                   QuoteChar = '\''
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteEndArray();

            jsonWriter.Flush();
        }

        var expected = """
                       [
                         'NaN',
                         'Infinity',
                         '-Infinity',
                         'NaN',
                         'Infinity',
                         '-Infinity'
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInStart()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented,
                   FloatFormatHandling = FloatFormatHandling.Symbol
               })
        {
            jsonWriter.WriteRaw("[1,2,3,4,5]");
            jsonWriter.WriteWhitespace("  ");
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteEndArray();
        }

        var expected = """
                       [1,2,3,4,5]  [
                         NaN
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInArray()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteRaw(",[1,2,3,4,5]");
            jsonWriter.WriteRaw(",[1,2,3,4,5]");
            jsonWriter.WriteValue(float.NaN);
            jsonWriter.WriteEndArray();
        }

        var expected = """
                       [
                         NaN,[1,2,3,4,5],[1,2,3,4,5],
                         NaN
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public void WriteRawInObject()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteRaw(
                """
                "PropertyName":[1,2,3,4,5]
                """);
            jsonWriter.WriteEnd();
        }

        var expected = """{"PropertyName":[1,2,3,4,5]}""";
        var result = stringWriter.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteRawStringBuilderInObject()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteRaw(
                new StringBuilder(
                    """
                    "PropertyName":[1,2,3,4,5]
                    """));
            jsonWriter.WriteEnd();
        }

        var expected = """{"PropertyName":[1,2,3,4,5]}""";
        var result = stringWriter.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteToken()
    {
        var reader = new JsonTextReader(new StringReader("[1,2,3,4,5]"));
        reader.Read();
        reader.Read();

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteToken(reader);

        Assert.Equal("1", stringWriter.ToString());
    }

    [Fact]
    public void WriteRawValue()
    {
        var textWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(textWriter))
        {
            var i = 0;
            var rawJson = "[1,2]";

            jsonWriter.WriteStartObject();

            while (i < 3)
            {
                jsonWriter.WritePropertyName($"d{i}");
                jsonWriter.WriteRawValue(rawJson);

                i++;
            }

            jsonWriter.WriteEndObject();
        }

        Assert.Equal("""{"d0":[1,2],"d1":[1,2],"d2":[1,2]}""", textWriter.ToString());
    }

    [Fact]
    public void WriteFloatingPointNumber()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.FloatFormatHandling = FloatFormatHandling.Symbol;

            jsonWriter.WriteStartArray();

            jsonWriter.WriteValue(0.0);
            jsonWriter.WriteValue(0f);
            jsonWriter.WriteValue(0.1);
            jsonWriter.WriteValue(1.0);
            jsonWriter.WriteValue(1.000001);
            jsonWriter.WriteValue(0.000001);
            jsonWriter.WriteValue(double.Epsilon);
            jsonWriter.WriteValue(double.PositiveInfinity);
            jsonWriter.WriteValue(double.NegativeInfinity);
            jsonWriter.WriteValue(double.NaN);
            jsonWriter.WriteValue(double.MaxValue);
            jsonWriter.WriteValue(double.MinValue);
            jsonWriter.WriteValue(float.PositiveInfinity);
            jsonWriter.WriteValue(float.NegativeInfinity);
            jsonWriter.WriteValue(float.NaN);

            jsonWriter.WriteEndArray();
        }

#if NET6_0_OR_GREATER
        Assert.Equal("[0.0,0.0,0.1,1.0,1.000001,1E-06,5E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN]", stringWriter.ToString());
#else
        Assert.Equal("[0.0,0.0,0.1,1.0,1.000001,1E-06,4.94065645841247E-324,Infinity,-Infinity,NaN,1.7976931348623157E+308,-1.7976931348623157E+308,Infinity,-Infinity,NaN]", stringWriter.ToString());
#endif
    }

    [Fact]
    public void WriteIntegerNumber()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter) {Formatting = Formatting.Indented})
        {
            jsonWriter.WriteStartArray();

            jsonWriter.WriteValue(int.MaxValue);
            jsonWriter.WriteValue(int.MinValue);
            jsonWriter.WriteValue(0);
            jsonWriter.WriteValue(-0);
            jsonWriter.WriteValue(9L);
            jsonWriter.WriteValue(9UL);
            jsonWriter.WriteValue(long.MaxValue);
            jsonWriter.WriteValue(long.MinValue);
            jsonWriter.WriteValue(ulong.MaxValue);
            jsonWriter.WriteValue(ulong.MinValue);
            jsonWriter.WriteValue((ulong) uint.MaxValue - 1);
            jsonWriter.WriteValue((ulong) uint.MaxValue);
            jsonWriter.WriteValue((ulong) uint.MaxValue + 1);

            jsonWriter.WriteEndArray();
        }

        Console.WriteLine(stringWriter.ToString());

        XUnitAssert.AreEqualNormalized(
            """
            [
              2147483647,
              -2147483648,
              0,
              0,
              9,
              9,
              9223372036854775807,
              -9223372036854775808,
              18446744073709551615,
              0,
              4294967294,
              4294967295,
              4294967296
            ]
            """, stringWriter.ToString());
    }

    [Fact]
    public void WriteTokenDirect()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter))
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
        }

        Assert.Equal("""[1,{"integer":2147483647,"null-string":null}]""", stringWriter.ToString());
    }

    [Fact]
    public async Task WriteTokenDirect_BadValue()
    {
        var stringWriter = new StringWriter();

        using var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteToken(JsonToken.StartArray);

        await Throws(() => jsonWriter.WriteToken(JsonToken.Integer, "three"))
            .UniqueForTargetFrameworkAndVersion()
            .IgnoreStackTrace();
    }

    [Fact]
    public void TokenTypeOutOfRange()
    {
        using var jsonWriter = new JsonTextWriter(new StringWriter());
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => jsonWriter.WriteToken((JsonToken) int.MinValue));
        Assert.Equal("token", ex.ParamName);
    }

    [Fact]
    public void BadWriteEndArray()
    {
        var stringWriter = new StringWriter();

        using var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteStartArray();

        jsonWriter.WriteValue(0.0);

        jsonWriter.WriteEndArray();
        var exception = Assert.Throws<JsonWriterException>(() => jsonWriter.WriteEndArray());
        Assert.Equal("No token to close. Path ''.", exception.Message);
    }

    [Fact]
    public void InvalidQuoteChar()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var stringWriter = new StringWriter();

            using var jsonWriter = new JsonTextWriter(stringWriter)
            {
                QuoteChar = '*'
            };
        });
        Assert.Equal(@"Invalid JavaScript string quote character. Valid quote characters are ' and "".", exception.Message);
    }

    [Fact]
    public void WriteSingleBytes()
    {
        var stringWriter = new StringWriter();

        var text = "Hello world.";
        var data = Encoding.UTF8.GetBytes(text);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteValue(data);
        }

        var expected = """
                       "SGVsbG8gd29ybGQu"
                       """;
        var result = stringWriter.ToString();

        Assert.Equal(expected, result);

        var d2 = Convert.FromBase64String(result.Trim('"'));

        Assert.Equal(text, Encoding.UTF8.GetString(d2, 0, d2.Length));
    }

    [Fact]
    public void WriteBytesInArray()
    {
        var stringWriter = new StringWriter();

        var text = "Hello world.";
        var data = Encoding.UTF8.GetBytes(text);

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(data);
            jsonWriter.WriteValue(data);
            jsonWriter.WriteValue((object) data);
            jsonWriter.WriteValue((byte[]) null);
            jsonWriter.WriteValue((Uri) null);
            jsonWriter.WriteEndArray();
        }

        var expected = """
                       [
                         "SGVsbG8gd29ybGQu",
                         "SGVsbG8gd29ybGQu",
                         "SGVsbG8gd29ybGQu",
                         null,
                         null
                       ]
                       """;
        var result = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(expected, result);
    }

    [Fact]
    public Task Path()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartArray();
            Assert.Equal("", jsonWriter.Path);
            jsonWriter.WriteStartObject();
            Assert.Equal("[0]", jsonWriter.Path);
            jsonWriter.WritePropertyName("Property1");
            Assert.Equal("[0].Property1", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1", jsonWriter.Path);
            jsonWriter.WriteValue(1);
            Assert.Equal("[0].Property1[0]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1][0]", jsonWriter.Path);
            jsonWriter.WriteStartArray();
            Assert.Equal("[0].Property1[1][0][0]", jsonWriter.Path);
            jsonWriter.WriteEndObject();
            Assert.Equal("[0]", jsonWriter.Path);
            jsonWriter.WriteStartObject();
            Assert.Equal("[1]", jsonWriter.Path);
            jsonWriter.WritePropertyName("Property2");
            Assert.Equal("[1].Property2", jsonWriter.Path);
            jsonWriter.WriteNull();
            Assert.Equal("[1].Property2", jsonWriter.Path);
        }

        return Verify(stringWriter);
    }

    [Fact]
    public void PathWithSpanPropertyNames()
    {
        // the span overload does not materialize the name into a string, it copies the chars into
        // a buffer reused across the properties written at that depth, so the path has to stay
        // correct as names change length, as depths are pushed and popped, and when the two
        // overloads are mixed
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("Property1".AsSpan());
            Assert.Equal("[0].Property1", jsonWriter.Path);
            jsonWriter.WriteValue(1);

            // shorter name over a buffer a longer one already wrote into
            jsonWriter.WritePropertyName("P2".AsSpan());
            Assert.Equal("[0].P2", jsonWriter.Path);

            // the parent name has to survive being pushed and popped
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Child".AsSpan());
            Assert.Equal("[0].P2.Child", jsonWriter.Path);
            jsonWriter.WriteValue(2);
            jsonWriter.WriteEndObject();

            jsonWriter.WritePropertyName("AfterNested".AsSpan());
            Assert.Equal("[0].AfterNested", jsonWriter.Path);
            jsonWriter.WriteValue(3);

            // names with special characters take the escaping branch of the path builder
            jsonWriter.WritePropertyName("has space".AsSpan());
            Assert.Equal("[0]['has space']", jsonWriter.Path);
            jsonWriter.WriteValue(4);

            // a slice of a larger buffer must not pick up the chars around it
            jsonWriter.WritePropertyName("XXXSlicedXXX".AsSpan(3, 6));
            Assert.Equal("[0].Sliced", jsonWriter.Path);
            jsonWriter.WriteValue(5);

            // back to the string overload at the same depth
            jsonWriter.WritePropertyName("Plain");
            Assert.Equal("[0].Plain", jsonWriter.Path);
            jsonWriter.WriteValue(6);

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndArray();
        }

        Assert.Equal(
            """[{"Property1":1,"P2":{"Child":2},"AfterNested":3,"has space":4,"Sliced":5,"Plain":6}]""",
            stringWriter.ToString());
    }

    [Fact]
    public void SpanPropertyNameInExceptionPath()
    {
        // the deferred name is read back when an error path is built, which is the only thing
        // that consumes it
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("badProperty".AsSpan());
        // nest so the position holding the span name is on the stack that ContainerPath builds from
        jsonWriter.WriteStartObject();

        var exception = Assert.Throws<JsonWriterException>(() => jsonWriter.WriteValue(new Version(1, 2)));

        Assert.Contains("Path 'badProperty'", exception.Message);
    }

    [Fact]
    public Task BuildStateArray()
    {
        var stateArray = JsonWriter.BuildStateArray();
        return Verify(stateArray);
    }

    [Fact]
    public void HtmlEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeHtml
        };

        var script = """<script type="text/javascript">alert('hi');</script>""";

        jsonWriter.WriteValue(script);

        var json = stringWriter.ToString();

        Assert.Equal(
            """
            "\u003cscript type=\u0022text/javascript\u0022\u003ealert(\u0027hi\u0027);\u003c/script\u003e"
            """,
            json);

        var reader = new JsonTextReader(new StringReader(json));

        Assert.Equal(script, reader.ReadAsString());
    }

    [Fact]
    public void NonAsciiEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.EscapeNonAscii
        };

        var unicode = "\u5f20";

        jsonWriter.WriteValue(unicode);

        var json = stringWriter.ToString();

        Assert.Equal(8, json.Length);
        Assert.Equal(
            """
            "\u5f20"
            """,
            json);

        var reader = new JsonTextReader(new StringReader(json));

        Assert.Equal(unicode, reader.ReadAsString());

        stringWriter = new();
        jsonWriter = new(stringWriter)
        {
            EscapeHandling = EscapeHandling.Default
        };

        jsonWriter.WriteValue(unicode);

        json = stringWriter.ToString();

        Assert.Equal(3, json.Length);
        Assert.Equal("\"\u5f20\"", json);
    }

    [Fact]
    public void NoEscapeHandling()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            EscapeHandling = EscapeHandling.None
        };

        var unicode = "\u5f20";

        jsonWriter.WriteValue(unicode);

        var json = stringWriter.ToString();

        Assert.Equal(3, json.Length);
        Assert.Equal(
            """
            "张"
            """,
            json);

        var reader = new JsonTextReader(new StringReader(json));

        Assert.Equal(unicode, reader.ReadAsString());

        stringWriter = new();
        jsonWriter = new(stringWriter)
        {
            EscapeHandling = EscapeHandling.Default
        };

        jsonWriter.WriteValue(unicode);

        json = stringWriter.ToString();

        Assert.Equal(3, json.Length);
        Assert.Equal("\"\u5f20\"", json);
    }

    [Fact]
    public void WriteEndOnProperty()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            QuoteChar = '\''
        };

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("Blah");
        jsonWriter.WriteEnd();

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void WriteEndOnProperty_Close()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            QuoteChar = '\''
        };

        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("Blah");
        jsonWriter.Close();

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void WriteEndOnProperty_Dispose()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   QuoteChar = '\''
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Blah");
        }

        Assert.Equal("{'Blah':null}", stringWriter.ToString());
    }

    [Fact]
    public void AutoCompleteOnClose_False()
    {
        var stringWriter = new StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   AutoCompleteOnClose = false,
                   QuoteChar = '\''
               })
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Blah");
        }

        Assert.Equal("{'Blah':", stringWriter.ToString());
    }

    [Fact]
    public void QuoteChar()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented,
            QuoteChar = '\''
        };

        jsonWriter.WriteStartArray();

        jsonWriter.WriteValue(new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc));
        jsonWriter.WriteValue(new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));

        jsonWriter.WriteValue([1, 2, 3]);
        jsonWriter.WriteValue(TimeSpan.Zero);
        jsonWriter.WriteValue(new Uri("http://www.google.com/"));
        jsonWriter.WriteValue(Guid.Empty);

        jsonWriter.WriteEnd();

        XUnitAssert.AreEqualNormalized(
            """
            [
              '2000-01-01T01:01:01Z',
              '2000-01-01T01:01:01+00:00',
              'AQID',
              '00:00:00',
              'http://www.google.com/',
              '00000000-0000-0000-0000-000000000000'
            ]
            """, stringWriter.ToString());
    }

    [Fact]
    public void CustomJsonTextWriterTests()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new CustomJsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };
        jsonWriter.WriteStartObject();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        jsonWriter.WritePropertyName("Property1");
        Assert.Equal(WriteState.Property, jsonWriter.WriteState);
        Assert.Equal("Property1", jsonWriter.Path);
        jsonWriter.WriteNull();
        Assert.Equal(WriteState.Object, jsonWriter.WriteState);
        jsonWriter.WriteEndObject();
        Assert.Equal(WriteState.Start, jsonWriter.WriteState);

        XUnitAssert.AreEqualNormalized(
            """
            {{{
              "1ytreporP": NULL!!!
            }}}
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void QuoteDictionaryNames()
    {
        var d = new Dictionary<string, int>
        {
            {"a", 1}
        };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var serializer = JsonSerializer.Create(settings);
        using var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter)
               {
                   QuoteName = false
               })
        {
            serializer.Serialize(writer, d);
            writer.Close();
        }

        XUnitAssert.AreEqualNormalized(
            """
            {
              a: 1
            }
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void QuoteDictionaryValues()
    {
        var d = new Dictionary<string, string>
        {
            {"a", "b"}
        };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var serializer = JsonSerializer.Create(settings);
        using var stringWriter = new StringWriter();
        using (var writer = new JsonTextWriter(stringWriter)
               {
                   QuoteValue = false
               })
        {
            serializer.Serialize(writer, d);
            writer.Close();
        }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "a": b
            }
            """,
            stringWriter.ToString());
    }

    [Fact]
    public Task WriteComments()
    {
        var json = $$"""
            //comment*//*hi*/
            {//comment
            Name://comment
            true//comment after true{{StringUtils.CarriageReturn}}
            ,//comment after comma{{StringUtils.CarriageReturnLineFeed}}
            ExpiryDate: '2014-06-04T00:00:00Z',
                    Price: 3.99,
                    Sizes: //comment
            [//comment

                      "Small"//comment
            ]//comment
            }//comment
            //comment 1
            """;

        var reader = new JsonTextReader(new StringReader(json));

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };

        jsonWriter.WriteToken(reader, true);

        return Verify(stringWriter.ToString());
    }

    [Fact]
    public void DisposeSuppressesFinalization()
    {
        UnmanagedResourceFakingJsonWriter.CreateAndDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.Equal(1, UnmanagedResourceFakingJsonWriter.DisposalCalls);
    }
}

public class CustomJsonTextWriter(TextWriter textWriter) : JsonTextWriter(textWriter)
{
    protected readonly TextWriter writer = textWriter;

    public override void WritePropertyName(string name) =>
        WritePropertyName(name, true);

    public override void WritePropertyName(string name, bool escape)
    {
        SetWriteState(JsonToken.PropertyName, name);

        if (QuoteName)
        {
            writer.Write(QuoteChar);
        }

        writer.Write(new string(name.Reverse().ToArray()));

        if (QuoteName)
        {
            writer.Write(QuoteChar);
        }

        writer.Write(':');
    }

    public override void WriteNull()
    {
        SetWriteState(JsonToken.Null, null);

        writer.Write("NULL!!!");
    }

    public override void WriteStartObject()
    {
        SetWriteState(JsonToken.StartObject, null);

        writer.Write("{{{");
    }

    public override void WriteEndObject() =>
        SetWriteState(JsonToken.EndObject, null);

    protected override void WriteEnd(JsonToken token)
    {
        if (token == JsonToken.EndObject)
        {
            writer.Write("}}}");
        }
        else
        {
            base.WriteEnd(token);
        }
    }
}

public class UnmanagedResourceFakingJsonWriter : JsonWriter
{
    public static int DisposalCalls;

    public static void CreateAndDispose() =>
        ((IDisposable) new UnmanagedResourceFakingJsonWriter()).Dispose();

    public UnmanagedResourceFakingJsonWriter() =>
        DisposalCalls = 0;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ++DisposalCalls;
    }

    ~UnmanagedResourceFakingJsonWriter() =>
        Dispose(false);

    public override void Flush() =>
        throw new NotImplementedException();

    public override void WriteRaw(char? json) =>
        throw new NotImplementedException();

    public override void WriteRaw(string json) =>
        throw new NotImplementedException();

    public override void WriteRaw(StringBuilder json) =>
        throw new NotImplementedException();

    public override void WriteRaw(CharSpan json) =>
        throw new NotImplementedException();
}
