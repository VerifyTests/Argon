// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ExceptionHandlingTests : TestFixtureBase
{
    [Fact]
    public void ReadAsInt32NumberErrorSetsUndefinedToken()
    {
        var reader = new JsonTextReader(new StringReader("[123456789012345]"));
        reader.Read(); // StartArray

        Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());

        // The error-recovery token must be Undefined, distinguishable from a real JSON null.
        Assert.Equal(JsonToken.Undefined, reader.TokenType);
    }

    [Fact]
    public void ReadAsInt32InvalidIntegerSetsUndefinedToken()
    {
        var reader = new JsonTextReader(new StringReader("[1.5]"));
        reader.Read(); // StartArray

        Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());

        Assert.Equal(JsonToken.Undefined, reader.TokenType);
    }

    [Fact]
    public void RejectsNonPositiveBufferSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JsonTextReader(new StringReader("1"), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JsonTextReader(new StringReader("1"), -5));
    }

    [Fact]
    public void ReadAsBytes_MissingComma()
    {
        var data = "Hello world"u8.ToArray();

        var json = $"['{Convert.ToBase64String(data)}' '{Convert.ToBase64String(data)}']";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(data, reader.ReadAsBytes());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("After parsing a value an unexpected character was encountered: '. Path '[0]', line 1, position 20.", exception.Message);
    }

    [Fact]
    public void ReadAsInt32_MissingComma()
    {
        var json = "[0 1 2]";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(0, (int) reader.ReadAsInt32());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("After parsing a value an unexpected character was encountered: 1. Path '[0]', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void ReadAsBoolean_MissingComma()
    {
        var json = "[true false true]";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.ReadAsBoolean());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBoolean());
        Assert.Equal("After parsing a value an unexpected character was encountered: f. Path '[0]', line 1, position 6.", exception.Message);
    }

    [Fact]
    public void ReadAsDateTime_MissingComma()
    {
        var json = "['2017-02-04T00:00:00Z' '2018-02-04T00:00:00Z' '2019-02-04T00:00:00Z']";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(new(2017, 2, 4, 0, 0, 0, DateTimeKind.Utc), reader.ReadAsDateTime());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("After parsing a value an unexpected character was encountered: '. Path '[0]', line 1, position 24.", exception.Message);
    }

    [Fact]
    public void ReadAsDateTimeOffset_MissingComma()
    {
        var json = "['2017-02-04T00:00:00Z' '2018-02-04T00:00:00Z' '2019-02-04T00:00:00Z']";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(new(2017, 2, 4, 0, 0, 0, TimeSpan.Zero), reader.ReadAsDateTimeOffset());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTimeOffset());
        Assert.Equal("After parsing a value an unexpected character was encountered: '. Path '[0]', line 1, position 24.", exception.Message);
    }

    [Fact]
    public void ReadAsString_MissingComma()
    {
        var json = "['2017-02-04T00:00:00Z' '2018-02-04T00:00:00Z' '2019-02-04T00:00:00Z']";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal("2017-02-04T00:00:00Z", reader.ReadAsString());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("After parsing a value an unexpected character was encountered: '. Path '[0]', line 1, position 24.", exception.Message);
    }

    [Fact]
    public void Read_MissingComma()
    {
        var json = "[0 1 2]";
        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("After parsing a value an unexpected character was encountered: 1. Path '[0]', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndAfterReadingN()
    {
        var reader = new JsonTextReader(new StringReader("n"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndAfterReadingNu()
    {
        var reader = new JsonTextReader(new StringReader("nu"));
        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndOfHex()
    {
        var reader = new JsonTextReader(new StringReader(@"'h\u123"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end while parsing Unicode escape sequence. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndOfControlCharacter()
    {
        var reader = new JsonTextReader(new StringReader(@"'h\"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unterminated string. Expected delimiter: '. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void ReadInvalidNonBase10Number()
    {
        var json = "0aq2dun13.hod";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected character encountered while parsing number: q. Path '', line 1, position 2.", exception.Message);

        reader = new(new StringReader(json));

        var exception1 = Assert.Throws<JsonReaderException>(() => reader.ReadAsDecimal());
        Assert.Equal("Unexpected character encountered while parsing number: q. Path '', line 1, position 2.", exception1.Message);

        reader = new(new StringReader(json));

        var exception2 = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing number: q. Path '', line 1, position 2.", exception2.Message);
    }

    [Fact]
    public void ThrowErrorWhenParsingUnquoteStringThatStartsWithNE()
    {
        const string json = """{ "ItemName": "value", "u":netanelsalinger,"r":9 }""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected character encountered while parsing value: n. Path 'u', line 1, position 27.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndOfString()
    {
        var reader = new JsonTextReader(new StringReader("'hi"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unterminated string. Expected delimiter: '. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void UnexpectedEndTokenWhenParsingOddEndToken()
    {
        var reader = new JsonTextReader(new StringReader("{}}"));
        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Additional text encountered after finished reading JSON content: }. Path '', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void ResetJsonTextReaderErrorCount()
    {
        var toggleReaderError = new ToggleReaderError(new StringReader("{'first':1,'second':2,'third':3}"));
        var jsonTextReader = new JsonTextReader(toggleReaderError);

        Assert.True(jsonTextReader.Read());

        toggleReaderError.Error = true;

        var exception = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception.Message);
        var exception1 = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception1.Message);

        toggleReaderError.Error = false;

        Assert.True(jsonTextReader.Read());
        Assert.Equal("first", jsonTextReader.Value);

        toggleReaderError.Error = true;

        var exception2 = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception2.Message);

        toggleReaderError.Error = false;

        Assert.True(jsonTextReader.Read());
        Assert.Equal(1L, jsonTextReader.Value);

        toggleReaderError.Error = true;

        var exception3 = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception3.Message);
        var exception4 = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception4.Message);
        var exception5 = Assert.Throws<Exception>(() => jsonTextReader.Read());
        Assert.Equal("Read error", exception5.Message);

        toggleReaderError.Error = false;
    }

    [Fact]
    public void MatchWithInsufficientCharacters()
    {
        var reader = new JsonTextReader(new StringReader("nul"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void MatchWithWrongCharacters()
    {
        var reader = new JsonTextReader(new StringReader("nulz"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Error parsing null value. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void MatchWithNoTrailingSeparator()
    {
        var reader = new JsonTextReader(new StringReader("nullz"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Error parsing null value. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void UnclosedComment()
    {
        var reader = new JsonTextReader(new StringReader("/* sdf"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end while parsing comment. Path '', line 1, position 6.", exception.Message);
    }

    [Fact]
    public void BadCommentStart()
    {
        var reader = new JsonTextReader(new StringReader("/sdf"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Error parsing comment. Expected: *, got s. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void MissingColon()
    {
        var json = """
            {
                "A" : true,
                "B" "
            """;

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.Boolean, reader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal(@"Invalid character after parsing property name. Expected ':' but got: "". Path 'A', line 3, position 8.", exception.Message);
    }

    [Fact]
    public void ParseAdditionalContent_Comma()
    {
        var json = """
            [
                "Small",
                "Medium",
                "Large"
            ],
            """;

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() =>
        {
            while (reader.Read())
            {
            }
        });
        Assert.Equal("Additional text encountered after finished reading JSON content: ,. Path '', line 5, position 1.", exception.Message);
    }

    [Fact]
    public void ParseAdditionalContent_Text()
    {
        var json = """
            [
                "Small",
                "Medium",
                "Large"
            ]content
            """;

        var reader = new JsonTextReader(new StringReader(json), 2);

        reader.Read();
        Assert.Equal(1, reader.LineNumber);

        reader.Read();
        Assert.Equal(2, reader.LineNumber);

        reader.Read();
        Assert.Equal(3, reader.LineNumber);

        reader.Read();
        Assert.Equal(4, reader.LineNumber);

        reader.Read();
        Assert.Equal(5, reader.LineNumber);

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Additional text encountered after finished reading JSON content: c. Path '', line 5, position 1.", exception.Message);
    }

    [Fact]
    public void ParseAdditionalContent_WhitespaceThenText()
    {
        var json = "'hi' a";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() =>
        {
            while (reader.Read())
            {
            }
        });
        Assert.Equal("Additional text encountered after finished reading JSON content: a. Path '', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void ParseIncompleteCommentSeparator()
    {
        var reader = new JsonTextReader(new StringReader("true/"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Error parsing boolean value. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void ReadBadCharInArray()
    {
        var reader = new JsonTextReader(new StringReader("[}"));

        reader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected character encountered while parsing value: }. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsBytesNoContentWrappedObject()
    {
        var reader = new JsonTextReader(new StringReader("{"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadBytesEmptyWrappedObject()
    {
        var reader = new JsonTextReader(new StringReader("{}"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Error reading bytes. Unexpected token: StartObject. Path '', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void ReadIntegerWithError()
    {
        var json = """
            {
                ChildId: 333333333333333333333333333333333333333
            }
            """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => jsonTextReader.ReadAsInt32());
        Assert.Equal("JSON integer 333333333333333333333333333333333333333 is too large or small for an Int32. Path 'ChildId', line 2, position 52.", exception.Message);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);

        Assert.False(jsonTextReader.Read());
    }

    [Fact]
    public void ReadIntegerWithErrorInArray()
    {
        var json = """
                   [
                     333333333333333333333333333333333333333,
                     3.3,
                     ,
                     0f
                   ]
                   """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartArray, jsonTextReader.TokenType);

        var exception = Assert.Throws<JsonReaderException>(() => jsonTextReader.ReadAsInt32());
        Assert.Equal("JSON integer 333333333333333333333333333333333333333 is too large or small for an Int32. Path '[0]', line 2, position 41.", exception.Message);

        var exception1 = Assert.Throws<JsonReaderException>(() => jsonTextReader.ReadAsInt32());
        Assert.Equal("Input string '3.3' is not a valid integer. Path '[1]', line 3, position 5.", exception1.Message);

        var exception2 = Assert.Throws<JsonReaderException>(() => jsonTextReader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[2]', line 4, position 3.", exception2.Message);

        var exception3 = Assert.Throws<JsonReaderException>(() => jsonTextReader.ReadAsInt32());
        Assert.Equal("Input string '0f' is not a valid integer. Path '[3]', line 5, position 4.", exception3.Message);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndArray, jsonTextReader.TokenType);

        Assert.False(jsonTextReader.Read());
    }

    [Fact]
    public void ReadBytesWithError()
    {
        var json = """
            {
                ChildId: '123'
            }
            """;

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);

        try
        {
            jsonTextReader.ReadAsBytes();
        }
        catch (FormatException)
        {
        }

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);

        Assert.False(jsonTextReader.Read());
    }

    [Fact]
    public void ReadInt32Overflow()
    {
        long i = int.MaxValue;

        var reader = new JsonTextReader(new StringReader(i.ToString(InvariantCulture)));
        reader.Read();
        Assert.Equal(typeof(long), reader.ValueType);

        for (var j = 1; j < 1000; j++)
        {
            var total = j + i;
            var messages = $"JSON integer {total} is too large or small for an Int32. Path '', line 1, position 10.";
            reader = new(new StringReader(total.ToString(InvariantCulture)));
            var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
            Assert.Equal(messages, exception.Message);
        }
    }

    [Fact]
    public void ReadInt32Overflow_Negative()
    {
        long i = int.MinValue;

        var reader = new JsonTextReader(new StringReader(i.ToString(InvariantCulture)));
        reader.Read();
        Assert.Equal(typeof(long), reader.ValueType);
        Assert.Equal(i, reader.Value);

        for (var j = 1; j < 1000; j++)
        {
            var total = -j + i;
            var messages = $"JSON integer {total} is too large or small for an Int32. Path '', line 1, position 11.";
            reader = new(new StringReader(total.ToString(InvariantCulture)));
            var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
            Assert.Equal(messages, exception.Message);
        }
    }

    [Fact]
    public void ReadInt64Overflow()
    {
        var i = new BigInteger(long.MaxValue);

        var reader = new JsonTextReader(new StringReader(i.ToString(InvariantCulture)));
        reader.Read();
        Assert.Equal(typeof(long), reader.ValueType);

        for (var j = 1; j < 1000; j++)
        {
            var total = BigInteger.Add(i, j);

            reader = new(new StringReader(total.ToString(InvariantCulture)));
            reader.Read();

            Assert.Equal(typeof(BigInteger), reader.ValueType);
        }
    }

    [Fact]
    public void ReadInt64Overflow_Negative()
    {
        var i = new BigInteger(long.MinValue);

        var reader = new JsonTextReader(new StringReader(i.ToString(InvariantCulture)));
        reader.Read();
        Assert.Equal(typeof(long), reader.ValueType);

        for (var j = 1; j < 1000; j++)
        {
            var total = BigInteger.Add(i, -j);

            reader = new(new StringReader(total.ToString(InvariantCulture)));
            reader.Read();

            Assert.Equal(typeof(BigInteger), reader.ValueType);
        }
    }

    [Fact]
    public void ReadAsString_Null_AdditionalBadData()
    {
        var json = "nullllll";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Error parsing null value. Path '', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void ReadAsBoolean_AdditionalBadData()
    {
        var json = "falseeeee";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBoolean());
        Assert.Equal("Unexpected character encountered while parsing value: e. Path '', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void ReadAsString_AdditionalBadData()
    {
        var json = "falseeeee";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected character encountered while parsing value: e. Path '', line 1, position 5.", exception.Message);
    }

    [Fact]
    public void ReadAsBoolean_UnexpectedEnd()
    {
        var json = "tru";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBoolean());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void ReadAsBoolean_BadData()
    {
        var json = "pie";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBoolean());
        Assert.Equal("Unexpected character encountered while parsing value: p. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsString_BadData()
    {
        var json = "pie";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected character encountered while parsing value: p. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsDouble_BadData()
    {
        var json = "pie";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDouble());
        Assert.Equal("Unexpected character encountered while parsing value: p. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsDouble_Boolean()
    {
        var json = "true";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDouble());
        Assert.Equal("Unexpected character encountered while parsing value: t. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsBytes_BadData()
    {
        var json = "pie";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected character encountered while parsing value: p. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsBytesIntegerArrayWithNoEnd()
    {
        var reader = new JsonTextReader(new StringReader("[1"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected end when reading bytes. Path '[0]', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void ReadAsBytesArrayWithBadContent()
    {
        var reader = new JsonTextReader(new StringReader("[1.0]"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected token when reading bytes: Float. Path '[0]', line 1, position 4.", exception.Message);
    }

    [Fact]
    public void ReadAsBytes_CommaErrors()
    {
        var reader = new JsonTextReader(new StringReader("[,'']"));
        reader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.", exception.Message);

        Assert.Empty(reader.ReadAsBytes()!);
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadAsBytes_InvalidEndArray()
    {
        var reader = new JsonTextReader(new StringReader("]"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsBytes_CommaErrors_Multiple()
    {
        var reader = new JsonTextReader(new StringReader("['',,'']"));
        reader.Read();
        Assert.Empty(reader.ReadAsBytes());

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 5.", exception.Message);

        Assert.Empty(reader.ReadAsBytes()!);
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadBytesWithBadCharacter()
    {
        var reader = new JsonTextReader(new StringReader("true"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unexpected character encountered while parsing value: t. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadBytesWithUnexpectedEnd()
    {
        var helloWorld = "Hello world!";
        var helloWorldData = Encoding.UTF8.GetBytes(helloWorld);

        var reader = new JsonTextReader(new StringReader($"'{Convert.ToBase64String(helloWorldData)}"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsBytes());
        Assert.Equal("Unterminated string. Expected delimiter: '. Path '', line 1, position 17.", exception.Message);
    }

    [Fact]
    public void ReadAsDateTime_BadData()
    {
        var json = "pie";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: p. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadAsDateTime_Boolean()
    {
        var json = "true";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: t. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadInt32WithBadCharacter()
    {
        var reader = new JsonTextReader(new StringReader("true"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: t. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadNumberValue_CommaErrors()
    {
        var reader = new JsonTextReader(new StringReader("[,1]"));
        reader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.", exception.Message);

        Assert.Equal(1, reader.ReadAsInt32());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadNumberValue_InvalidEndArray()
    {
        var reader = new JsonTextReader(new StringReader("]"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadNumberValue_CommaErrors_Multiple()
    {
        var reader = new JsonTextReader(new StringReader("[1,,1]"));
        reader.Read();
        reader.ReadAsInt32();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsInt32());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 4.", exception.Message);

        Assert.Equal(1, reader.ReadAsInt32());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadAsString_UnexpectedEnd()
    {
        var json = "tru";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void ReadAsString_Null_UnexpectedEnd()
    {
        var json = "nul";

        var reader = new JsonTextReader(new StringReader(json));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected end when reading JSON. Path '', line 1, position 3.", exception.Message);
    }

    [Fact]
    public void ReadStringValue_InvalidEndArray()
    {
        var reader = new JsonTextReader(new StringReader("]"));

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: ]. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void ReadStringValue_CommaErrors()
    {
        var reader = new JsonTextReader(new StringReader("[,'']"));
        reader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 2.", exception.Message);

        Assert.Equal(string.Empty, reader.ReadAsString());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadStringValue_CommaErrors_Multiple()
    {
        var reader = new JsonTextReader(new StringReader("['',,'']"));
        reader.Read();
        reader.ReadAsInt32();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsString());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[1]', line 1, position 5.", exception.Message);

        Assert.Equal(string.Empty, reader.ReadAsString());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ReadStringValue_Numbers_NotString()
    {
        var reader = new JsonTextReader(new StringReader("[56,56]"));
        reader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: 5. Path '', line 1, position 2.", exception.Message);

        var exception1 = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: 6. Path '', line 1, position 3.", exception1.Message);

        var exception2 = Assert.Throws<JsonReaderException>(() => reader.ReadAsDateTime());
        Assert.Equal("Unexpected character encountered while parsing value: ,. Path '[0]', line 1, position 4.", exception2.Message);

        Assert.Equal(56, reader.ReadAsInt32());
        Assert.True(reader.Read());
    }

    [Fact]
    public void ErrorReadingComment()
    {
        var json = "/";

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end while parsing comment. Path '', line 1, position 1.", exception.Message);
    }

    [Fact]
    public void EscapedPathInExceptionMessage()
    {
        var json = """
            {
              "frameworks": {
                "NET5_0_OR_GREATER": {
                  "dependencies": {
                    "System.Xml.ReaderWriter": {
                      "source": !!! !!!
                    }
                  }
                }
              }
            }
            """;

        var reader = new JsonTextReader(new StringReader(json));
        var exception = Assert.Throws<JsonReaderException>(() =>
        {
            while (reader.Read())
            {
            }
        });
        Assert.Equal("Unexpected character encountered while parsing value: !. Path 'frameworks.NET5_0_OR_GREATER.dependencies['System.Xml.ReaderWriter'].source', line 6, position 20.", exception.Message);
    }

    [Fact]
    public void MaxDepth()
    {
        var json = "[[]]";

        var reader = new JsonTextReader(new StringReader(json))
        {
            MaxDepth = 1
        };

        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => Assert.True(reader.Read()));
        Assert.Equal("The reader's MaxDepth of 1 has been exceeded. Path '[0]', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void MaxDepthDoesNotRecursivelyError()
    {
        var json = "[[[[]]],[[]]]";

        var reader = new JsonTextReader(new StringReader(json))
        {
            MaxDepth = 1
        };

        Assert.True(reader.Read());
        Assert.Equal(0, reader.Depth);

        var exception = Assert.Throws<JsonReaderException>(() => Assert.True(reader.Read()));
        Assert.Equal("The reader's MaxDepth of 1 has been exceeded. Path '[0]', line 1, position 2.", exception.Message);
        Assert.Equal(1, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(3, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(3, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.Depth);

        var exception1 = Assert.Throws<JsonReaderException>(() => Assert.True(reader.Read()));
        Assert.Equal("The reader's MaxDepth of 1 has been exceeded. Path '[1]', line 1, position 9.", exception1.Message);
        Assert.Equal(1, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.Depth);

        Assert.True(reader.Read());
        Assert.Equal(0, reader.Depth);

        Assert.False(reader.Read());
    }

    [Fact]
    public void UnexpectedEndWhenParsingUnquotedProperty()
    {
        JsonReader reader = new JsonTextReader(new StringReader("{aww"));
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal("Unexpected end while parsing unquoted property name. Path '', line 1, position 4.", exception.Message);
    }
}