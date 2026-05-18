// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable UnusedVariable
public class ReadTests : TestFixtureBase
{
    [Fact]
    public void Read_EmptyStream_ReturnsFalse()
    {
        var ms = new MemoryStream();
        var sr = new StreamReader(ms);

        var reader = new JsonTextReader(sr);
        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadAsInt32_IntegerTooLarge_ThrowsJsonReaderException()
    {
        var token = new JValue(long.MaxValue);

        var exception = Assert.Throws<JsonReaderException>(() => token.CreateReader().ReadAsInt32());
        Assert.Equal("Could not convert to integer: 9223372036854775807. Path ''.", exception.Message);
    }

    [Fact]
    public void ReadAsDecimal_IntegerTooLarge_ThrowsJsonReaderException()
    {
        var token = new JValue(double.MaxValue);

        XUnitAssert.Throws<JsonReaderException>(
            () => token.CreateReader().ReadAsDecimal(),
            "Could not convert to decimal: 1.79769313486232E+308. Path ''.",
            "Could not convert to decimal: 1.7976931348623157E+308. Path ''."
        );
    }

    [Fact]
    public void ReadAsInt32_BigIntegerValue_Success()
    {
        var token = new JValue(BigInteger.Parse("1"));

        var i = token.CreateReader().ReadAsInt32();
        Assert.Equal(1, i);
    }

    [Fact]
    public void ReadMissingInt64()
    {
        var json = "{ A: \"\", B: 1, C: , D: 1.23, E: 3.45, F: null }";

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();
        reader.Read();
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);
        Assert.Equal("C", reader.Value);

        reader.Read();
        Assert.Equal(JsonToken.Undefined, reader.TokenType);
        Assert.Null(reader.Value);
    }

    [Fact]
    public void ReadAsInt32WithUndefined()
    {
        var reader = new JsonTextReader(new StringReader("undefined"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: u. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsBoolean()
    {
        var json = """
                   [
                     1,
                     0,
                     1.1,
                     0.0,
                     0.000000000001,
                     9999999999,
                     -9999999999,
                     9999999999999999999999999999999999999999999999999999999999999999999999,
                     -9999999999999999999999999999999999999999999999999999999999999999999999,
                     'true',
                     'TRUE',
                     'false',
                     'FALSE',
                     // comment!
                     /* comment! */
                     '',
                     null
                   ]
                   """;

        var reader = new JsonTextReader(new StringReader(json), 10);

        Assert.True(reader.Read());
        Assert.Equal("", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[0]", reader.Path);

        Assert.False(reader.ReadAsBoolean());
        Assert.Equal("[1]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[2]", reader.Path);

        Assert.False(reader.ReadAsBoolean());
        Assert.Equal("[3]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[4]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[5]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[6]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[7]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[8]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[9]", reader.Path);

        Assert.True(reader.ReadAsBoolean());
        Assert.Equal("[10]", reader.Path);

        Assert.False(reader.ReadAsBoolean());
        Assert.Equal("[11]", reader.Path);

        Assert.False(reader.ReadAsBoolean());
        Assert.Equal("[12]", reader.Path);

        Assert.Null(reader.ReadAsBoolean());
        Assert.Equal("[13]", reader.Path);

        Assert.Null(reader.ReadAsBoolean());
        Assert.Equal("[14]", reader.Path);

        Assert.Null(reader.ReadAsBoolean());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
        Assert.Equal("", reader.Path);

        Assert.Null(reader.ReadAsBoolean());
        Assert.Equal(JsonToken.None, reader.TokenType);
        Assert.Equal("", reader.Path);
    }

    [Fact]
    public void ReadAsBoolean_NullChar()
    {
        var json = "\0true\0\0";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.ReadAsBoolean());
        Assert.Null(reader.ReadAsBoolean());
    }

    [Fact]
    public void ReadAsBytes()
    {
        var data = "Hello world"u8.ToArray();

        var json = $"""
                    "{Convert.ToBase64String(data)}"
                    """;

        var reader = new JsonTextReader(new StringReader(json));

        var result = reader.ReadAsBytes();

        Assert.Equal(data, result);
    }

    [Fact]
    public void ReadAsBooleanNoContent()
    {
        var reader = new JsonTextReader(new StringReader(""));

        Assert.Null(reader.ReadAsBoolean());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public void ReadAsBytesIntegerArrayWithComments()
    {
        var reader = new JsonTextReader(new StringReader("[/*hi*/1/*hi*/,2/*hi*/]"));

        var data = reader.ReadAsBytes();
        Assert.Equal(2, data.Length);
        Assert.Equal(1, data[0]);
        Assert.Equal(2, data[1]);
    }

    [Fact]
    public void ReadUnicode()
    {
        var json = """{"Message":"Hi,I\u0092ve send you smth"}""";

        var reader = new JsonTextReader(new StringReader(json), 5);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);
        Assert.Equal("Message", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(@"Hi,Ive send you smth", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.False(reader.Read());
    }

    //TODO: fails when run shared
// #if !RELEASE
//     [Fact]
//     public void ReadLargeObjects()
//     {
//         const int nrItems = 2;
//         const int length = 1200;
//         const int largeBufferLength = 2048;
//
//         var apostrophe = Encoding.ASCII.GetBytes(@"""").First();
//         var openingBracket = Encoding.ASCII.GetBytes(@"[").First();
//         var comma = Encoding.ASCII.GetBytes(@",").First();
//         var closingBracket = Encoding.ASCII.GetBytes(@"]").First();
//
//         using var ms = new MemoryStream();
//         ms.WriteByte(openingBracket);
//         for (var i = 0; i < nrItems; i++)
//         {
//             ms.WriteByte(apostrophe);
//
//             for (var j = 0; j <= length; j++)
//             {
//                 var current = Convert.ToByte(j % 10 + 48);
//                 ms.WriteByte(current);
//             }
//
//             ms.WriteByte(apostrophe);
//             if (i < nrItems - 1)
//             {
//                 ms.WriteByte(comma);
//             }
//         }
//
//         ms.WriteByte(closingBracket);
//         ms.Seek(0, SeekOrigin.Begin);
//
//         var reader = new JsonTextReader(new StreamReader(ms));
//         reader.LargeBufferLength = largeBufferLength;
//
//         Assert.True(reader.Read());
//         Assert.Equal(JsonToken.StartArray, reader.TokenType);
//
//         Assert.True(reader.Read());
//         Assert.Equal(JsonToken.String, reader.TokenType);
//         Assert.Equal(largeBufferLength, reader.CharBufferLength);
//
//         Assert.True(reader.Read());
//         Assert.Equal(JsonToken.String, reader.TokenType);
//         // buffer has been shifted before reading the second string
//         Assert.Equal(largeBufferLength, reader.CharBufferLength);
//
//         Assert.True(reader.Read());
//         Assert.Equal(JsonToken.EndArray, reader.TokenType);
//
//         Assert.False(reader.Read());
//     }
// #endif

    [Fact]
    public void ReadSingleBytes()
    {
        var s = new StringReader(
            """
            "SGVsbG8gd29ybGQu"
            """);
        var reader = new JsonTextReader(s);

        var data = reader.ReadAsBytes();
        Assert.NotNull(data);

        var text = Encoding.UTF8.GetString(data, 0, data.Length);
        Assert.Equal("Hello world.", text);
    }

    [Fact]
    public void ReadAsBytesNoContent()
    {
        var reader = new JsonTextReader(new StringReader(""));

        Assert.Null(reader.ReadAsBytes());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffsetNoContent()
    {
        var reader = new JsonTextReader(new StringReader(""));

        Assert.Null(reader.ReadAsDateTimeOffset());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffset()
    {
        var json = "{Offset:'2000-01-01T00:00:00.000+06'}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDateTimeOffset();
        Assert.Equal(JsonToken.Date, reader.TokenType);
        Assert.Equal(typeof(DateTimeOffset), reader.ValueType);
        Assert.Equal(new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(6)), reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffsetNegative()
    {
        var json = "{Offset:'2000-01-01T00:00:00.000-06'}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDateTimeOffset();
        Assert.Equal(JsonToken.Date, reader.TokenType);
        Assert.Equal(typeof(DateTimeOffset), reader.ValueType);
        Assert.Equal(new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(-6)), reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffsetBadString()
    {
        var json = """{"Offset":"blablahbla"}""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTimeOffset());
        Assert.Equal("Could not convert string to DateTimeOffset: blablahbla. Path 'Offset', line 1, position 22.", exception.Message);
    }

    [Fact]
    public void ReadAsDateTimeOffsetHoursOnly()
    {
        var json = "{Offset:'2000-01-01T00:00:00.000+06'}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDateTimeOffset();
        Assert.Equal(JsonToken.Date, reader.TokenType);
        Assert.Equal(typeof(DateTimeOffset), reader.ValueType);
        Assert.Equal(new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(6)), reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffsetWithMinutes()
    {
        var json = "{Offset:'2000-01-01T00:00:00.000-0631'}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDateTimeOffset();
        Assert.Equal(JsonToken.Date, reader.TokenType);
        Assert.Equal(typeof(DateTimeOffset), reader.ValueType);
        Assert.Equal(new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(-6).Add(TimeSpan.FromMinutes(-31))), reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadAsDateTimeOffsetIsoDate()
    {
        var json = """{"Offset":"2011-08-01T21:25Z"}""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDateTimeOffset();
        Assert.Equal(JsonToken.Date, reader.TokenType);
        Assert.Equal(typeof(DateTimeOffset), reader.ValueType);
        Assert.Equal(new DateTimeOffset(new DateTime(2011, 8, 1, 21, 25, 0, DateTimeKind.Utc), TimeSpan.Zero), reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadAsDecimalInt()
    {
        var json = """{"Name":1}""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(typeof(decimal), reader.ValueType);
        Assert.Equal(1m, reader.Value);
    }

    [Fact]
    public void ReadAsIntDecimal()
    {
        var json = """{"Name": 1.1}""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Input string '1.1' is not a valid integer. Path 'Name', line 1, position 12.", exception.Message);
    }

    [Fact]
    public void ReadAsDecimal()
    {
        var json = """{"decimal":-7.92281625142643E+28}""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        var d = reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(typeof(decimal), reader.ValueType);
        Assert.Equal(-79228162514264300000000000000m, d);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);
    }

    [Fact]
    public void ReadBufferOnControlChar()
    {
        var json = """
                   [
                     {
                       Name: 'Jim',
                       BirthDate: '2000-01-01T00:00:00',
                       LastModified: '2000-01-01T00:00:00'
                     },
                     {
                       Name: 'Jim',
                       BirthDate: '2000-01-01T00:00:00',
                       LastModified: '2000-01-01T00:00:00'
                     }
                   ]
                   """;

        var reader = new JsonTextReader(new StringReader(json), 5);

        for (var i = 0; i < 13; i++)
        {
            reader.Read();
        }

        Assert.True(reader.Read());
        Assert.Equal("2000-01-01T00:00:00", reader.Value);
    }

    [Fact]
    public void ReadBufferOnEndComment()
    {
        var json = """
                   /*comment*/ { /*comment*/
                     Name: /*comment*/ 'Apple' /*comment*/, /*comment*/
                     ExpiryDate: '2013-08-14T04:38:31.000+01',
                     Price: 3.99,
                     Sizes: /*comment*/ [ /*comment*/
                       'Small', /*comment*/
                       'Medium' /*comment*/,
                       /*comment*/ 'Large'
                     /*comment*/ ] /*comment*/
                   } /*comment*/
                   """;

        var reader = new JsonTextReader(new StringReader(json), 5);

        for (var i = 0; i < 26; i++)
        {
            Assert.True(reader.Read());
        }

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadAsDouble_Null()
    {
        var reader = new JsonTextReader(new StringReader("null"));
        Assert.Null(reader.ReadAsDouble());
    }

    [Fact]
    public void ReadAsInt32_QuotedNumber_Success()
    {
        var reader = new JsonTextReader(new StringReader("'42'"));
        Assert.Equal(42, reader.ReadAsInt32());
    }

    [Fact]
    public void ReadAsDecimal_QuotedNumber_Failure()
    {
        var reader = new JsonTextReader(new StringReader("'blah'"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDecimal());
        Assert.Equal("Could not convert string to decimal: blah. Path '', line 1, position 6.", exception.Message);
    }

    [Fact]
    public void ReadAsDouble_Success()
    {
        var reader = new JsonTextReader(new StringReader("'12.34'"));
        Assert.Equal(12.34d, reader.ReadAsDouble());
    }

    [Fact]
    public void ReadAsDouble_AllowThousands()
    {
        var reader = new JsonTextReader(new StringReader("'1,112.34'"));
        Assert.Equal(1112.34d, reader.ReadAsDouble());
    }

    [Fact]
    public void ReadAsDouble_Failure()
    {
        var reader = new JsonTextReader(new StringReader("['Trump',1]"));

        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDouble());
        Assert.Equal("Could not convert string to double: Trump. Path '[0]', line 1, position 8.", exception.Message);

        Assert.Equal(1d, reader.ReadAsDouble());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadAsString_Boolean()
    {
        var reader = new JsonTextReader(new StringReader("{\"Test1\":false}"));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var s = reader.ReadAsString();
        Assert.Equal("false", s);

        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void Read_Boolean_Failure()
    {
        var reader = new JsonTextReader(new StringReader("{\"Test1\":false1}"));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Error parsing boolean value. Path 'Test1', line 1, position 14.", exception.Message);

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadAsString_Boolean_Failure()
    {
        var reader = new JsonTextReader(new StringReader("{\"Test1\":false1}"));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected character encountered while parsing value: 1. Path 'Test1', line 1, position 14.", exception.Message);

        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadValue_EmptyString_Position()
    {
        var json = "['','','','','','','']";

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();
        reader.ReadAsInt32();
        Assert.Equal("[0]", reader.Path);
        reader.ReadAsDecimal();
        Assert.Equal("[1]", reader.Path);
        reader.ReadAsDateTime();
        Assert.Equal("[2]", reader.Path);
        reader.ReadAsDateTimeOffset();
        Assert.Equal("[3]", reader.Path);
        reader.ReadAsString();
        Assert.Equal("[4]", reader.Path);
        reader.ReadAsBytes();
        Assert.Equal("[5]", reader.Path);
        reader.ReadAsDouble();
        Assert.Equal("[6]", reader.Path);

        Assert.Null(reader.ReadAsString());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.Null(reader.ReadAsString());
        Assert.Equal(JsonToken.None, reader.TokenType);

        Assert.Null(reader.ReadAsBytes());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public void ReadValueComments()
    {
        var json = """/*comment*/[/*comment*/1/*comment*/,2,/*comment*//*comment*/"three"/*comment*/,"four"/*comment*/,null,/*comment*/null,3.99,1.1/*comment*/,''/*comment*/]/*comment*/""";

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.Equal(1, reader.ReadAsInt32());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.Equal(2, reader.ReadAsInt32());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.Equal("three", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.Equal("four", reader.ReadAsString());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.Null(reader.ReadAsString());
        Assert.Equal(JsonToken.Null, reader.TokenType);

        Assert.Null(reader.ReadAsInt32());
        Assert.Equal(JsonToken.Null, reader.TokenType);

        Assert.Equal(3.99m, reader.ReadAsDecimal());
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.Equal(1.1m, reader.ReadAsDecimal());
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.Empty(reader.ReadAsBytes()!);
        Assert.Equal(JsonToken.Bytes, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.Null(reader.ReadAsInt32());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public Task ReadContentDelimitedByComments()
    {
        var json = """
                   /*comment*/{/*comment*/Name:/*comment*/true/*comment*/,/*comment*/
                   ExpiryDate:'2014-06-04T00:00:00Z',
                   Price: 3.99,
                   Sizes:/*comment*/[/*comment*/
                     'Small'/*comment*/]/*comment*/}/*comment*/
                   """;

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));
        return reader.VerifyReaderState();
    }

    [Fact]
    public void ReadNullIntLineNumberAndPosition()
    {
        var json = """
                   [
                     1,
                     2,
                     3,
                     null
                   ]
                   """;

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        reader.Read();
        Assert.Equal(1, reader.LineNumber);

        reader.ReadAsInt32();
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal("[0]", reader.Path);

        reader.ReadAsInt32();
        Assert.Equal(3, reader.LineNumber);
        Assert.Equal("[1]", reader.Path);

        reader.ReadAsInt32();
        Assert.Equal(4, reader.LineNumber);
        Assert.Equal("[2]", reader.Path);

        reader.ReadAsInt32();
        Assert.Equal(5, reader.LineNumber);
        Assert.Equal("[3]", reader.Path);

        reader.Read();
        Assert.Equal(6, reader.LineNumber);
        Assert.Equal(string.Empty, reader.Path);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadingFromSlowStream()
    {
        var json = "[false, true, true, false, 'test!', 1.11, 0e-10, 0E-10, 0.25e-5, 0.3e10, 6.0221418e23, 'Purple\\r \\n monkey\\'s:\\tdishwasher']";

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        Assert.True(reader.Read());

        Assert.True(reader.Read());
        Assert.False((bool) reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Boolean, reader.TokenType);
        Assert.True((bool) reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Boolean, reader.TokenType);
        Assert.True((bool) reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Boolean, reader.TokenType);
        Assert.False((bool) reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal("test!", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(1.11d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0.0000025d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(3000000000d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(602214180000000000000000d, reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(reader.Value, "Purple\r \n monkey's:\tdishwasher");

        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadCommentInsideArray()
    {
        var json = """
            {
                "projects": [
                    "src",
                    //"
                    "test"
                ]
            }
            """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));
        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartArray, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);
        Assert.Equal("src", jsonTextReader.Value);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.Comment, jsonTextReader.TokenType);
        Assert.Equal(@"""", jsonTextReader.Value);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);
        Assert.Equal("test", jsonTextReader.Value);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndArray, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);
    }

    [Fact]
    public void ReadAsBytes_Base64AndGuid()
    {
        var jsonTextReader = new JsonTextReader(new StringReader("'AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA'"));
        var data = jsonTextReader.ReadAsBytes();
        var expected = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA");

        Assert.Equal(expected, data);

        jsonTextReader = new(new StringReader("'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA'"));
        data = jsonTextReader.ReadAsBytes();
        expected = new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA").ToByteArray();

        Assert.Equal(expected, data);
    }

    [Fact]
    public void ReadSingleQuoteInsideDoubleQuoteString()
    {
        var json = """{"NameOfStore":"Forest's Bakery And Cafe"}""";

        var jsonTextReader = new JsonTextReader(new StringReader(json));
        jsonTextReader.Read();
        jsonTextReader.Read();
        jsonTextReader.Read();

        Assert.Equal("Forest's Bakery And Cafe", jsonTextReader.Value);
    }

    [Fact]
    public void ReadMultilineString()
    {
        var json = """
                   "first line
                   second line
                   third line"
                   """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);

        Assert.Equal(
            """
            first line
            second line
            third line
            """,
            jsonTextReader.Value);
    }

    [Fact]
    public void ReadBigInteger()
    {
        var json = """
            {
                ParentId: 1,
                ChildId: 333333333333333333333333333333333333333,
            }
            """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.Integer, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.Integer, jsonTextReader.TokenType);
        Assert.Equal(typeof(BigInteger), jsonTextReader.ValueType);
        Assert.Equal(BigInteger.Parse("333333333333333333333333333333333333333"), jsonTextReader.Value);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);

        Assert.False(jsonTextReader.Read());

        var o = JObject.Parse(json);
        var i = (BigInteger) ((JValue) o["ChildId"]).Value;
        Assert.Equal(BigInteger.Parse("333333333333333333333333333333333333333"), i);
    }

    [Fact]
    public void ReadBadMSDateAsString()
    {
        var json = """
            {
                ChildId: '\/Date(9467082_PIE_340000-0631)\/'
            }
            """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);
        Assert.Equal("/Date(9467082_PIE_340000-0631)/", jsonTextReader.Value);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);

        Assert.False(jsonTextReader.Read());
    }

    [Fact]
    public void ReadingIndented()
    {
        var input = """
            {
              CPU: 'Intel',
              Drives: [
                'DVD read/writer',
                "500 gigabyte hard drive"
              ]
            }
            """;

        var sr = new StringReader(input);

        using var jsonReader = new JsonTextReader(sr, 5);

        Assert.Equal(JsonToken.None, jsonReader.TokenType);
        Assert.Equal(0, jsonReader.LineNumber);
        Assert.Equal(0, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.StartObject, jsonReader.TokenType);
        Assert.Equal(1, jsonReader.LineNumber);
        Assert.Equal(1, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.PropertyName, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, "CPU");
        Assert.Equal(2, jsonReader.LineNumber);
        Assert.Equal(6, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.String, jsonReader.TokenType);
        Assert.Equal("Intel", jsonReader.Value);
        Assert.Equal(2, jsonReader.LineNumber);
        Assert.Equal(14, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.PropertyName, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, "Drives");
        Assert.Equal(3, jsonReader.LineNumber);
        Assert.Equal(9, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.StartArray, jsonReader.TokenType);
        Assert.Equal(3, jsonReader.LineNumber);
        Assert.Equal(11, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.String, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, "DVD read/writer");
        Assert.Equal('\'', jsonReader.QuoteChar);
        Assert.Equal(4, jsonReader.LineNumber);
        Assert.Equal(21, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.String, jsonReader.TokenType);
        Assert.Equal(jsonReader.Value, "500 gigabyte hard drive");
        Assert.Equal('"', jsonReader.QuoteChar);
        Assert.Equal(5, jsonReader.LineNumber);
        Assert.Equal(29, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.EndArray, jsonReader.TokenType);
        Assert.Equal(6, jsonReader.LineNumber);
        Assert.Equal(3, jsonReader.LinePosition);

        jsonReader.Read();
        Assert.Equal(JsonToken.EndObject, jsonReader.TokenType);
        Assert.Equal(7, jsonReader.LineNumber);
        Assert.Equal(1, jsonReader.LinePosition);

        Assert.False(jsonReader.Read());
    }

    [Fact]
    public void ReadLongString()
    {
        var s = new string('a', 10000);
        var reader = new JsonTextReader(new StringReader($"'{s}'"));
        reader.Read();

        Assert.Equal(s, reader.Value);
    }

    [Fact]
    public void ReadLongJsonArray()
    {
        var valueCount = 10000;
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteStartArray();
        for (var i = 0; i < valueCount; i++)
        {
            jsonWriter.WriteValue(i);
        }

        jsonWriter.WriteEndArray();

        var json = stringWriter.ToString();

        var reader = new JsonTextReader(new StringReader(json));
        Assert.True(reader.Read());
        for (var i = 0; i < valueCount; i++)
        {
            Assert.True(reader.Read());
            Assert.Equal((long) i, reader.Value);
        }

        Assert.True(reader.Read());
        Assert.False(reader.Read());
    }

    [Fact]
    public void NullCharReading()
    {
        var json = "\0{\0'\0h\0i\0'\0:\0[\01\0,\0'\0'\0\0,\0null\0]\0,\0do\0:true\0}\0\0/*\0sd\0f\0*/\0/*\0sd\0f\0*/ \0";
        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Null, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Boolean, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);
        Assert.Equal("\0sd\0f\0", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);
        Assert.Equal("\0sd\0f\0", reader.Value);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadNullTerminatorStrings()
    {
        var reader = new JsonTextReader(new StringReader("'h\0i'"));
        Assert.True(reader.Read());

        Assert.Equal("h\0i", reader.Value);
    }

    [Fact]
    public void ReadBytesNoStartWithUnexpectedEnd()
    {
        var reader = new JsonTextReader(new StringReader("[  "));
        Assert.True(reader.Read());

        Assert.Null(reader.ReadAsBytes());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public Task ReadNewLines()
    {
        var newLinesText = $"{StringUtils.CarriageReturn}{StringUtils.CarriageReturnLineFeed}{StringUtils.LineFeed}{StringUtils.CarriageReturnLineFeed} {StringUtils.CarriageReturn}{StringUtils.CarriageReturnLineFeed}";

        var json =
            $"{newLinesText}{{{newLinesText}'{newLinesText}name1{newLinesText}'{newLinesText}:{newLinesText}[{newLinesText}'2014-06-04T00:00:00Z'{newLinesText},{newLinesText}1.1111{newLinesText}]{newLinesText},{newLinesText}name2{newLinesText}:{newLinesText}{{{newLinesText}}}{newLinesText}}}{newLinesText}";

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));
        return reader.VerifyReaderState();
    }

    [Fact]
    public void ReadBytesFollowingNumberInArray()
    {
        var helloWorld = "Hello world!";
        var helloWorldData = Encoding.UTF8.GetBytes(helloWorld);

        var reader = new JsonTextReader(new StringReader($"[1,'{Convert.ToBase64String(helloWorldData)}']"));
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);
        var data = reader.ReadAsBytes();
        Assert.Equal(helloWorldData, data);
        Assert.Equal(JsonToken.Bytes, reader.TokenType);
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadBytesFollowingNumberInObject()
    {
        var helloWorld = "Hello world!";
        var helloWorldData = Encoding.UTF8.GetBytes(helloWorld);

        var reader = new JsonTextReader(new StringReader($"{{num:1,data:'{Convert.ToBase64String(helloWorldData)}'}}"));
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);
        Assert.True(reader.Read());
        var data = reader.ReadAsBytes();
        Assert.Equal(helloWorldData, data);
        Assert.Equal(JsonToken.Bytes, reader.TokenType);
        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadingEscapedStrings()
    {
        var input = "{value:'Purple\\r \\n monkey\\'s:\\tdishwasher'}";

        var sr = new StringReader(input);

        using var jsonReader = new JsonTextReader(sr);
        Assert.Equal(0, jsonReader.Depth);

        jsonReader.Read();
        Assert.Equal(JsonToken.StartObject, jsonReader.TokenType);
        Assert.Equal(0, jsonReader.Depth);

        jsonReader.Read();
        Assert.Equal(JsonToken.PropertyName, jsonReader.TokenType);
        Assert.Equal(1, jsonReader.Depth);

        jsonReader.Read();
        Assert.Equal(JsonToken.String, jsonReader.TokenType);
        Assert.Equal("Purple\r \n monkey's:\tdishwasher", jsonReader.Value);
        Assert.Equal('\'', jsonReader.QuoteChar);
        Assert.Equal(1, jsonReader.Depth);

        jsonReader.Read();
        Assert.Equal(JsonToken.EndObject, jsonReader.TokenType);
        Assert.Equal(0, jsonReader.Depth);
    }

    [Fact]
    public void ReadNewlineLastCharacter()
    {
        var input = """
            {
              CPU: 'Intel',
              Drives: [ /* Com*ment */
                'DVD read/writer',
                "500 gigabyte hard drive"
              ]
            }

            """;

        var o = JsonConvert.DeserializeObject(input);
    }

    [Fact]
    public void ReadRandomJson()
    {
        var json = """
            [
              true,
              {
                "integer": 99,
                "string": "how now brown cow?",
                "array": [
                  0,
                  1,
                  2,
                  3,
                  4,
                  {
                    "decimal": 990.00990099
                  },
                  5
                ]
              },
              "This is a string.",
              null,
              null
            ]
            """;

        var reader = new JsonTextReader(new StringReader(json));
        while (reader.Read())
        {
        }
    }

    [Fact]
    public void ThrowOnDuplicateKeysDeserializing()
    {
        var json = """
                     {
                       "a": 1,
                       "b": [
                         {
                           "c": {
                             "d": 1,
                             "d": "2"
                           }
                         }
                       ]
                     }
                   """;

        var settings = new JsonLoadSettings();

        var reader = new JsonTextReader(new StringReader(json));
        Assert.Throws<JsonReaderException>(() => JToken.ReadFrom(reader, settings));
    }

    [Fact]
    public void MaxDepth_GreaterThanDefault()
    {
        var json = NestedJson.Build(150);

        var reader = new JsonTextReader(new StringReader(json))
        {
            MaxDepth = 150
        };

        while (reader.Read())
        {
        }
    }

    [Fact]
    public void MaxDepth_Null()
    {
        var json = NestedJson.Build(150);

        var reader = new JsonTextReader(new StringReader(json))
        {
            MaxDepth = null
        };

        while (reader.Read())
        {
        }
    }

    [Fact]
    public void MaxDepth_MaxValue()
    {
        var json = NestedJson.Build(150);

        var reader = new JsonTextReader(new StringReader(json))
        {
            MaxDepth = int.MaxValue
        };

        while (reader.Read())
        {
        }
    }
}