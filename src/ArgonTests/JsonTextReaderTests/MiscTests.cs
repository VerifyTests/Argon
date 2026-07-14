// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class MiscTests : TestFixtureBase
{
    [Fact]
    public void CloseReleasesBufferAndIsIdempotent()
    {
        var reader = new JsonTextReader(new StringReader("123"));
        reader.Read();
        reader.Close();

        // After close the pooled buffer must be released so a second close cannot
        // return the same array to ArrayPool.Shared a second time.
        var field = typeof(JsonTextReader).GetField("charBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Empty((char[]) field.GetValue(reader));

        // second close must be a no-op (no double-return, no throw)
        reader.Close();
        Assert.Empty((char[]) field.GetValue(reader));
    }

    [Fact]
    public void DisposeAfterExplicitCloseDoesNotDoubleReturn()
    {
        var reader = new JsonTextReader(new StringReader("123"));
        reader.Read();
        reader.Close();

        // Disposing after an explicit Close must not return the buffer again.
        ((IDisposable) reader).Dispose();

        var field = typeof(JsonTextReader).GetField("charBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Empty((char[]) field.GetValue(reader));
    }

    [Fact]
    public void ReadWithSupportMultipleContentCommaDelimited()
    {
        var json = "{ 'name': 'Admin' },{ 'name': 'Publisher' },1,null,[],,'string'";

        var reader = new JsonTextReader(new StringReader(json))
        {
            SupportMultipleContent = true
        };

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Null, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Undefined, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public void LineInfoAndNewLines()
    {
        var json = "{}";

        var jsonTextReader = new JsonTextReader(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);
        Assert.Equal(1, jsonTextReader.LineNumber);
        Assert.Equal(1, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());

        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);
        Assert.Equal(1, jsonTextReader.LineNumber);
        Assert.Equal(2, jsonTextReader.LinePosition);

        json = "\n{\"a\":\"bc\"}";

        jsonTextReader = new(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(1, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(5, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(9, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(10, jsonTextReader.LinePosition);

        json = "\n{\"a\":\n\"bc\",\"d\":true\n}";

        jsonTextReader = new(new StringReader(json));

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.StartObject, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(1, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);
        Assert.Equal(2, jsonTextReader.LineNumber);
        Assert.Equal(5, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.String, jsonTextReader.TokenType);
        Assert.Equal(3, jsonTextReader.LineNumber);
        Assert.Equal(4, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.PropertyName, jsonTextReader.TokenType);
        Assert.Equal(3, jsonTextReader.LineNumber);
        Assert.Equal(9, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.Boolean, jsonTextReader.TokenType);
        Assert.Equal(3, jsonTextReader.LineNumber);
        Assert.Equal(13, jsonTextReader.LinePosition);

        Assert.True(jsonTextReader.Read());
        Assert.Equal(JsonToken.EndObject, jsonTextReader.TokenType);
        Assert.Equal(4, jsonTextReader.LineNumber);
        Assert.Equal(1, jsonTextReader.LinePosition);
    }

    [Fact]
    public void UnescapeDoubleQuotes()
    {
        var json = "{\"recipe_id\":\"12\",\"recipe_name\":\"Apocalypse Leather Armors\",\"recipe_text\":\"#C16------------------------------\\r\\n#C12Ingredients #C20\\r\\n#C16------------------------------\\r\\n\\r\\na piece of Leather Armor\\r\\n( ie #L \\\"Enhanced Leather Armor Boots\\\" \\\"85644\\\" )\\r\\n<img src=rdb:\\/\\/13264>\\r\\n\\r\\n#L \\\"Hacker Tool\\\" \\\"87814\\\"\\r\\n<img src=rdb:\\/\\/99282>\\r\\n\\r\\n#L \\\"Clanalizer\\\" \\\"208313\\\"\\r\\n<img src=rdb:\\/\\/156479>\\r\\n\\r\\n#C16------------------------------\\r\\n#C12Recipe #C16\\r\\n#C16------------------------------#C20\\r\\n\\r\\nHacker Tool\\r\\n#C15+#C20\\r\\na piece of Leather Armor\\r\\n#C15=#C20\\r\\n<img src=rdb:\\/\\/13264>\\r\\na piece of Hacked Leather Armor\\r\\n( ie : #L \\\"Hacked Leather Armor Boots\\\" \\\"245979\\\" )\\r\\n#C16Skills: |  BE  |#C20\\r\\n\\r\\n#C14------------------------------#C20\\r\\n\\r\\nClanalizer\\r\\n#C15+#C20\\r\\na piece of Hacked Leather Armor\\r\\n#C15=#C20\\r\\n<img src=rdb:\\/\\/13264>\\r\\na piece of Apocalypse Leather Armor\\r\\n( ie : #L \\\"Apocalypse Leather Armor Boots\\\" \\\"245966\\\" )\\r\\n#C16Skills: |  ??  |#C20\\r\\n\\r\\n#C16------------------------------\\r\\n#C12Details#C16\\r\\n#C16------------------------------#C20\\r\\n\\r\\n#L \\\"Apocalypse Leather Armor Boots\\\" \\\"245967\\\"\\r\\n#L \\\"Apocalypse Leather Armor Gloves\\\" \\\"245969\\\"\\r\\n#L \\\"Apocalypse Leather Armor Helmet\\\" \\\"245975\\\"\\r\\n#L \\\"Apocalypse Leather Armor Pants\\\" \\\"245971\\\"\\r\\n#L \\\"Apocalypse Leather Armor Sleeves\\\" \\\"245973\\\"\\r\\n#L \\\"Apocalypse Leather Body Armor\\\" \\\"245965\\\"\\r\\n\\r\\n#C16------------------------------\\r\\n#C12Comments#C16\\r\\n#C16------------------------------#C20\\r\\n\\r\\nNice froob armor.. but ugleh!\\r\\n\\r\\n\",\"recipe_author\":null}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);
        Assert.Equal("recipe_text", reader.Value);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.Equal("#C16------------------------------\r\n#C12Ingredients #C20\r\n#C16------------------------------\r\n\r\na piece of Leather Armor\r\n( ie #L \"Enhanced Leather Armor Boots\" \"85644\" )\r\n<img src=rdb://13264>\r\n\r\n#L \"Hacker Tool\" \"87814\"\r\n<img src=rdb://99282>\r\n\r\n#L \"Clanalizer\" \"208313\"\r\n<img src=rdb://156479>\r\n\r\n#C16------------------------------\r\n#C12Recipe #C16\r\n#C16------------------------------#C20\r\n\r\nHacker Tool\r\n#C15+#C20\r\na piece of Leather Armor\r\n#C15=#C20\r\n<img src=rdb://13264>\r\na piece of Hacked Leather Armor\r\n( ie : #L \"Hacked Leather Armor Boots\" \"245979\" )\r\n#C16Skills: |  BE  |#C20\r\n\r\n#C14------------------------------#C20\r\n\r\nClanalizer\r\n#C15+#C20\r\na piece of Hacked Leather Armor\r\n#C15=#C20\r\n<img src=rdb://13264>\r\na piece of Apocalypse Leather Armor\r\n( ie : #L \"Apocalypse Leather Armor Boots\" \"245966\" )\r\n#C16Skills: |  ??  |#C20\r\n\r\n#C16------------------------------\r\n#C12Details#C16\r\n#C16------------------------------#C20\r\n\r\n#L \"Apocalypse Leather Armor Boots\" \"245967\"\r\n#L \"Apocalypse Leather Armor Gloves\" \"245969\"\r\n#L \"Apocalypse Leather Armor Helmet\" \"245975\"\r\n#L \"Apocalypse Leather Armor Pants\" \"245971\"\r\n#L \"Apocalypse Leather Armor Sleeves\" \"245973\"\r\n#L \"Apocalypse Leather Body Armor\" \"245965\"\r\n\r\n#C16------------------------------\r\n#C12Comments#C16\r\n#C16------------------------------#C20\r\n\r\nNice froob armor.. but ugleh!\r\n\r\n", reader.Value);
    }

    [Fact]
    public void SurrogatePairValid()
    {
        var json = """{ "MATHEMATICAL ITALIC CAPITAL ALPHA": "\uD835\uDEE2" }""";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        var s = reader.Value.ToString();
        Assert.Equal(2, s.Length);

        var stringInfo = new StringInfo(s);
        Assert.Equal(1, stringInfo.LengthInTextElements);
    }

    [Fact]
    public void SurrogatePairReplacement()
    {
        // existing good surrogate pair
        Assert.Equal("ABC \ud800\udc00 DEF", ReadString("ABC \\ud800\\udc00 DEF"));

        // invalid surrogates (two high back-to-back)
        Assert.Equal("ABC \ufffd\ufffd DEF", ReadString("ABC \\ud800\\ud800 DEF"));

        // invalid surrogates (two high back-to-back)
        Assert.Equal("ABC \ufffd\ufffd\u1234 DEF", ReadString("ABC \\ud800\\ud800\\u1234 DEF"));

        // invalid surrogates (three high back-to-back)
        Assert.Equal("ABC \ufffd\ufffd\ufffd DEF", ReadString("ABC \\ud800\\ud800\\ud800 DEF"));

        // invalid surrogates (high followed by a good surrogate pair)
        Assert.Equal("ABC \ufffd\ud800\udc00 DEF", ReadString("ABC \\ud800\\ud800\\udc00 DEF"));

        // invalid high surrogate at end of string
        Assert.Equal("ABC \ufffd", ReadString("ABC \\ud800"));

        // high surrogate not followed by low surrogate
        Assert.Equal("ABC \ufffd DEF", ReadString("ABC \\ud800 DEF"));

        // low surrogate not preceded by high surrogate
        Assert.Equal("ABC \ufffd\ufffd DEF", ReadString("ABC \\udc00\\ud800 DEF"));

        // make sure unencoded invalid surrogate characters don't make it through
        Assert.Equal("\ufffd\ufffd\ufffd", ReadString("\udc00\ud800\ud800"));

        Assert.Equal("ABC \ufffd\b", ReadString("ABC \\ud800\\b"));
        Assert.Equal("ABC \ufffd ", ReadString("ABC \\ud800 "));
        Assert.Equal("ABC \b\ufffd", ReadString("ABC \\b\\ud800"));
    }

    static string ReadString(string input)
    {
        var ms = new MemoryStream(
            Encoding.UTF8.GetBytes(
                $"""
                 "{input}"
                 """));

        var reader = new JsonTextReader(new StreamReader(ms));
        reader.Read();

        var s = (string) reader.Value;

        return s;
    }

    [Fact]
    public void CloseInput()
    {
        var ms = new MemoryStream();
        var reader = new JsonTextReader(new StreamReader(ms));

        Assert.True(ms.CanRead);
        reader.Close();
        Assert.False(ms.CanRead);

        ms = new();
        reader = new(new StreamReader(ms)) {CloseInput = false};

        Assert.True(ms.CanRead);
        reader.Close();
        Assert.True(ms.CanRead);
    }

    [Fact]
    public void YahooFinance()
    {
        var input = """
            {
                "matches" : [
                {"t":"C", "n":"Citigroup Inc.", "e":"NYSE", "id":"662713"}
                ,{"t":"CHL", "n":"China Mobile Ltd. (ADR)", "e":"NYSE", "id":"660998"}
                ,{"t":"PTR", "n":"PetroChina Company Limited (ADR)", "e":"NYSE", "id":"664536"}
                ,{"t":"RIO", "n":"Companhia Vale do Rio Doce (ADR)", "e":"NYSE", "id":"671472"}
                ,{"t":"RIOPR", "n":"Companhia Vale do Rio Doce (ADR)", "e":"NYSE", "id":"3512643"}
                ,{"t":"CSCO", "n":"Cisco Systems, Inc.", "e":"NASDAQ", "id":"99624"}
                ,{"t":"CVX", "n":"Chevron Corporation", "e":"NYSE", "id":"667226"}
                ,{"t":"TM", "n":"Toyota Motor Corporation (ADR)", "e":"NYSE", "id":"655880"}
                ,{"t":"JPM", "n":"JPMorgan Chase \\x26 Co.", "e":"NYSE", "id":"665639"}
                ,{"t":"COP", "n":"ConocoPhillips", "e":"NYSE", "id":"1691168"}
                ,{"t":"LFC", "n":"China Life Insurance Company Ltd. (ADR)", "e":"NYSE", "id":"688679"}
                ,{"t":"NOK", "n":"Nokia Corporation (ADR)", "e":"NYSE", "id":"657729"}
                ,{"t":"KO", "n":"The Coca-Cola Company", "e":"NYSE", "id":"6550"}
                ,{"t":"VZ", "n":"Verizon Communications Inc.", "e":"NYSE", "id":"664887"}
                ,{"t":"AMX", "n":"America Movil S.A.B de C.V. (ADR)", "e":"NYSE", "id":"665834"}],
                "all" : false
            }
            """;

        using var jsonReader = new JsonTextReader(new StringReader(input));
        while (jsonReader.Read())
        {
        }
    }

    [Fact]
    public async Task Depth()
    {
        var input = """
            {
              value:'Purple',
              array:[1,2,'2014-06-04T00:00:00Z'],
              subobject:{prop:1,proparray:[1]}
            }
            """;

        var sr = new StringReader(input);

        using var reader = new JsonTextReader(sr);
        await reader.VerifyReaderState();
    }

    [Fact]
    public void AppendCharsWhileReadingNull()
    {
        var json = """
            [
              {
                "$id": "1",
                "Name": "e1",
                "Manager": null
              },
              {
                "$id": "2",
                "Name": "e2",
                "Manager": null
              },
              {
                "$ref": "1"
              },
              {
                "$ref": "2"
              }
            ]
            """;

        var reader = new JsonTextReader(new StringReader(json), 129);

        for (var i = 0; i < 15; i++)
        {
            reader.Read();
        }

        reader.Read();
        Assert.Equal(JsonToken.Null, reader.TokenType);
    }

    [Fact]
    public void AppendCharsWhileReadingNewLine()
    {
        var json = """

            {
              "description": "A person",
              "type": "object",
              "properties":
              {
                "name": {"type":"string"},
                "hobbies": {
                  "type": "array",
                  "items": {"type":"string"}
                }
              }
            }

            """;

        var reader = new JsonTextReader(new StringReader(json), 129);

        for (var i = 0; i < 14; i++)
        {
            Assert.True(reader.Read());
        }

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);
        Assert.Equal("type", reader.Value);
    }

    [Fact]
    public void WriteReadWrite()
    {
        var stringWriter = new StringWriter();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            jsonWriter.WriteStartArray();
            jsonWriter.WriteValue(true);

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("integer");
            jsonWriter.WriteValue(99);
            jsonWriter.WritePropertyName("string");
            jsonWriter.WriteValue("how now brown cow?");
            jsonWriter.WritePropertyName("array");

            jsonWriter.WriteStartArray();
            for (var i = 0; i < 5; i++)
            {
                jsonWriter.WriteValue(i);
            }

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("decimal");
            jsonWriter.WriteValue(990.00990099m);
            jsonWriter.WriteEndObject();

            jsonWriter.WriteValue(5);
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();

            jsonWriter.WriteValue("This is a string.");
            jsonWriter.WriteNull();
            jsonWriter.WriteNull();
            jsonWriter.WriteEndArray();
        }

        var json = stringWriter.ToString();

        var serializer = new JsonSerializer();

        var jsonObject = serializer.Deserialize(new JsonTextReader(new StringReader(json)));

        stringWriter = new();

        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = Formatting.Indented
               })
        {
            serializer.Serialize(jsonWriter, jsonObject);
        }

        Assert.Equal(json, stringWriter.ToString());
    }

    [Fact]
    public void LongStringTest()
    {
        var length = 20000;
        var json = $"""["{new string(' ', length)}"]""";

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        reader.Read();
        Assert.Equal(JsonToken.String, reader.TokenType);
        Assert.Equal(typeof(string), reader.ValueType);
        Assert.Equal(20000, reader.Value.ToString().Length);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.False(reader.Read());
        Assert.Equal(JsonToken.None, reader.TokenType);
    }

    [Fact]
    public void EscapedUnicodeText()
    {
        var json = """["\u003c","\u5f20"]""";

        var reader = new JsonTextReader(new StringReader(json), 2);

        reader.Read();
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        reader.Read();
        Assert.Equal("<", reader.Value);

        reader.Read();
        Assert.Equal(24352, Convert.ToInt32(Convert.ToChar((string) reader.Value)));

        reader.Read();
        Assert.Equal(JsonToken.EndArray, reader.TokenType);
    }

    [Fact]
    public void SupportMultipleContent()
    {
        var reader = new JsonTextReader(new StringReader("""{'prop1':[1]} 1 2 "name" [][]null {}{} 1.1"""))
        {
            SupportMultipleContent = true
        };

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.PropertyName, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Integer, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.String, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndArray, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Null, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.StartObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.EndObject, reader.TokenType);

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Float, reader.TokenType);

        Assert.False(reader.Read());
    }

    [Fact]
    public Task SingleLineComments()
    {
        var json = $$"""
                     //comment*//*hi*/
                     {//comment
                     Name://comment
                     true//comment after true{{StringUtils.CarriageReturn}},//comment after comma{{StringUtils.CarriageReturnLineFeed}}'ExpiryDate':'2014-06-04T00:00:00Z',
                             Price: 3.99,
                             Sizes: //comment
                     [//comment

                               'Small'//comment
                     ]//comment
                     }//comment
                     //comment 1
                     """;

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        return reader.VerifyReaderState();
    }

    [Fact]
    public void JustSinglelineComment()
    {
        var json = "//comment";

        var reader = new JsonTextReader(new StreamReader(new SlowStream(json, new UTF8Encoding(false), 1)));

        Assert.True(reader.Read());
        Assert.Equal(JsonToken.Comment, reader.TokenType);
        Assert.Equal("comment", reader.Value);

        Assert.False(reader.Read());
    }

    [Fact]
    public void ScientificNotation()
    {
        var json = "[0e-10,0E-10,0.25e-5,0.3e10,6.0221418e23]";

        var reader = new JsonTextReader(new StringReader(json));

        reader.Read();

        reader.Read();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0d, reader.Value);

        reader.Read();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0d, reader.Value);

        reader.Read();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0.0000025d, reader.Value);

        reader.Read();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(3000000000d, reader.Value);

        reader.Read();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(602214180000000000000000d, reader.Value);

        reader.Read();

        reader = new(new StringReader(json));

        reader.Read();

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0m, reader.Value);

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0m, reader.Value);

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(0.0000025m, reader.Value);

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(3000000000m, reader.Value);

        reader.ReadAsDecimal();
        Assert.Equal(JsonToken.Float, reader.TokenType);
        Assert.Equal(602214180000000000000000m, reader.Value);

        reader.Read();
    }

    [Fact]
    public void WriteReadBoundaryDecimals()
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        jsonWriter.WriteStartArray();
        jsonWriter.WriteValue(decimal.MaxValue);
        jsonWriter.WriteValue(decimal.MinValue);
        jsonWriter.WriteEndArray();

        var json = stringWriter.ToString();

        var sr = new StringReader(json);
        var reader = new JsonTextReader(sr);

        Assert.True(reader.Read());

        var max = reader.ReadAsDecimal();
        Assert.Equal(decimal.MaxValue, max);

        var min = reader.ReadAsDecimal();
        Assert.Equal(decimal.MinValue, min);

        Assert.True(reader.Read());
    }

#if !NET6_0_OR_GREATER
    [Fact]
    public void LinePositionOnNewLine()
    {
        var json1 = "{'a':'bc'}";

        var reader = new JsonTextReader(new StringReader(json1));

        Assert.True(reader.Read());
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(1, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(5, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(9, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(1, reader.LineNumber);
        Assert.Equal(10, reader.LinePosition);

        Assert.False(reader.Read());

        var json2 = "\n{'a':'bc'}";

        reader = new(new StringReader(json2));

        Assert.True(reader.Read());
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal(1, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal(5, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal(9, reader.LinePosition);

        Assert.True(reader.Read());
        Assert.Equal(2, reader.LineNumber);
        Assert.Equal(10, reader.LinePosition);

        Assert.False(reader.Read());
    }
#endif

    [Fact]
    public void DisposeSuppressesFinalization()
    {
        UnmanagedResourceFakingJsonReader.CreateAndDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Assert.Equal(1, UnmanagedResourceFakingJsonReader.DisposalCalls);
    }

    [Fact]
    public void InvalidUnicodeSequence()
    {
        var json = @"{'prop':'\u123!'}";

        var reader = new JsonTextReader(new StringReader(json));

        Assert.True(reader.Read());
        Assert.True(reader.Read());

        var exception = Assert.Throws<JsonReaderException>(() => reader.Read());
        Assert.Equal(@"Invalid Unicode escape sequence: \u123!. Path 'prop', line 1, position 11.", exception.Message);
    }
}