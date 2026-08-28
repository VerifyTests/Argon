// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

public class JsonConvertTest : TestFixtureBase
{
    [Fact]
    public void ToStringTimeSpanDoesNotPadWithNulls()
    {
        Assert.Equal("\"01:00:00\"", JsonConvert.ToString(TimeSpan.FromHours(1)));
        Assert.Equal("\"1.02:03:04\"", JsonConvert.ToString(new TimeSpan(1, 2, 3, 4)));
    }

    [Fact]
    public void ToStringObjectTimeSpanDoesNotPadWithNulls() =>
        Assert.Equal("\"01:00:00\"", JsonConvert.ToString((object) TimeSpan.FromHours(1)));

    [Fact]
    public void ToStringEnsureEscapedArrayLength()
    {
        const char nonAsciiChar = (char) 257;
        const char escapableNonQuoteAsciiChar = '\0';

        var value = nonAsciiChar + @"\" + escapableNonQuoteAsciiChar;

        var convertedValue = JsonConvert.ToString((object) value);
        Assert.Equal(
            $"""
             "{nonAsciiChar}\\\u0000"
             """,
            convertedValue);
    }

    [Fact]
    public void DefaultSettings()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented
            };

            var json = JsonConvert.SerializeObject(new {test = new[] {1, 2, 3}});

            XUnitAssert.AreEqualNormalized(
                """
                {
                  "test": [
                    1,
                    2,
                    3
                  ]
                }
                """,
                json);
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    public class NameTableTestClass
    {
        public string Value { get; set; }
    }

    public class NameTableTestClassConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            reader.Read();
            reader.Read();

            var jsonTextReader = (JsonTextReader) reader;
            Assert.NotNull(jsonTextReader.PropertyNameTable);

            var s = serializer.Deserialize<string>(reader);
            Assert.Equal("hi", s);
            Assert.NotNull(jsonTextReader.PropertyNameTable);

            var o = new NameTableTestClass
            {
                Value = s
            };

            return o;
        }

        public override bool CanConvert(Type type) =>
            type == typeof(NameTableTestClass);
    }

    [Fact]
    public void NameTableTest()
    {
        var sr = new StringReader("{'property':'hi'}");
        var jsonTextReader = new JsonTextReader(sr);

        Assert.Null(jsonTextReader.PropertyNameTable);

        var serializer = new JsonSerializer();
        serializer.Converters.Add(new NameTableTestClassConverter());
        var o = serializer.Deserialize<NameTableTestClass>(jsonTextReader);

        Assert.Null(jsonTextReader.PropertyNameTable);
        Assert.Equal("hi", o.Value);
    }

    public class CustomNameTable : JsonNameTable
    {
        public override string Get(char[] key, int start, int length) =>
            "_" + new string(key, start, length);

        public override string Add(string name) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void CustomNameTableTest()
    {
        var sr = new StringReader("{'property':'hi'}");
        var jsonTextReader = new JsonTextReader(sr);

        Assert.Null(jsonTextReader.PropertyNameTable);
        var nameTable = jsonTextReader.PropertyNameTable = new CustomNameTable();

        var serializer = new JsonSerializer();
        var o = serializer.Deserialize<Dictionary<string, string>>(jsonTextReader);
        Assert.Equal("hi", o["_property"]);

        Assert.Equal(nameTable, jsonTextReader.PropertyNameTable);
    }

    [Fact]
    public void DefaultSettings_Example()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var e = new Employee
            {
                FirstName = "Eric",
                LastName = "Example",
                BirthDate = new(1980, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                Department = "IT",
                JobTitle = "Web Dude"
            };

            var json = JsonConvert.SerializeObject(e);
            // {
            //   "firstName": "Eric",
            //   "lastName": "Example",
            //   "birthDate": "1980-04-20T00:00:00Z",
            //   "department": "IT",
            //   "jobTitle": "Web Dude"
            // }

            XUnitAssert.AreEqualNormalized(
                """
                {
                  "firstName": "Eric",
                  "lastName": "Example",
                  "birthDate": "1980-04-20T00:00:00Z",
                  "department": "IT",
                  "jobTitle": "Web Dude"
                }
                """,
                json);
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    [Fact]
    public void DefaultSettings_Override()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented
            };

            var json = JsonConvert.SerializeObject(
                new
                {
                    test = new[]
                    {
                        1,
                        2,
                        3
                    }
                },
                new JsonSerializerSettings
                {
                    Formatting = Formatting.None
                });

            Assert.Equal("""{"test":[1,2,3]}""", json);
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    [Fact]
    public void DefaultSettings_Create()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented
            };

            var l = new List<int> {1, 2, 3};

            var stringWriter = new StringWriter();
            var serializer = JsonSerializer.CreateDefault();
            serializer.Serialize(stringWriter, l);

            XUnitAssert.AreEqualNormalized(
                """
                [
                  1,
                  2,
                  3
                ]
                """,
                stringWriter.ToString());

            stringWriter = new();
            serializer.Formatting = Formatting.None;
            serializer.Serialize(stringWriter, l);

            Assert.Equal("[1,2,3]", stringWriter.ToString());

            stringWriter = new();
            serializer = new();
            serializer.Serialize(stringWriter, l);

            Assert.Equal("[1,2,3]", stringWriter.ToString());

            stringWriter = new();
            serializer = JsonSerializer.Create();
            serializer.Serialize(stringWriter, l);

            Assert.Equal("[1,2,3]", stringWriter.ToString());
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    [Fact]
    public void DefaultSettings_CreateWithSettings()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Formatting = Formatting.Indented
            };

            var l = new List<int> {1, 2, 3};

            var stringWriter = new StringWriter();
            var serializer = JsonSerializer.CreateDefault(new()
            {
                Converters = {new IntConverter()}
            });
            serializer.Serialize(stringWriter, l);

            XUnitAssert.AreEqualNormalized(
                """
                [
                  2,
                  4,
                  6
                ]
                """,
                stringWriter.ToString());

            stringWriter = new();
            serializer.Converters.Clear();
            serializer.Serialize(stringWriter, l);

            XUnitAssert.AreEqualNormalized(
                """
                [
                  1,
                  2,
                  3
                ]
                """,
                stringWriter.ToString());

            stringWriter = new();
            serializer = JsonSerializer.Create(new() {Formatting = Formatting.Indented});
            serializer.Serialize(stringWriter, l);

            XUnitAssert.AreEqualNormalized(
                """
                [
                  1,
                  2,
                  3
                ]
                """,
                stringWriter.ToString());
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    public class IntConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var i = (int) value;
            writer.WriteValue(i * 2);
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanConvert(Type type) =>
            type == typeof(int);
    }
    [Fact]
    public void DeserializeObject_Integer()
    {
        var result = JsonConvert.DeserializeObject("1");
        Assert.Equal(1L, result);
    }

    [Fact]
    public void EscapeJavaScriptString()
    {
        var result = JavaScriptUtils.ToEscapedJavaScriptString("How now brown cow?".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "How now brown cow?"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("How now 'brown' cow?".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "How now 'brown' cow?"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("How now <brown> cow?".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "How now <brown> cow?"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("How \r\nnow brown cow?".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "How \r\nnow brown cow?"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007"
            """,
            result);

        result =
            JavaScriptUtils.ToEscapedJavaScriptString("\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013"
            """,
            result);

        result =
            JavaScriptUtils.ToEscapedJavaScriptString(
                "\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f ".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f "
            """,
            result);

        result =
            JavaScriptUtils.ToEscapedJavaScriptString(
                "!\"#$%&\u0027()*+,-./0123456789:;\u003c=\u003e?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("^_`abcdefghijklmnopqrstuvwxyz{|}~".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "^_`abcdefghijklmnopqrstuvwxyz{|}~"
            """,
            result);

        var data =
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&\u0027()*+,-./0123456789:;\u003c=\u003e?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~".AsSpan();
        var expected =
            """
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
            """;

        result = JavaScriptUtils.ToEscapedJavaScriptString(data, '"', true, EscapeHandling.Default);
        Assert.Equal(expected, result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("Fred's cat.".AsSpan(), '\'', true, EscapeHandling.Default);
        Assert.Equal(@"'Fred\'s cat.'", result);

        result = JavaScriptUtils.ToEscapedJavaScriptString(
            """
            "How are you gentlemen?" said Cats.
            """.AsSpan(),
            '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "\"How are you gentlemen?\" said Cats."
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString(
            """
            "How are' you gentlemen?" said Cats.
            """.AsSpan(),
            '"',
            true,
            EscapeHandling.Default);
        Assert.Equal(
            """
            "\"How are' you gentlemen?\" said Cats."
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("""Fred's "cat".""".AsSpan(), '\'', true, EscapeHandling.Default);
        Assert.Equal("""'Fred\'s "cat".'""", result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("\u001farray\u003caddress".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "\u001farray<address"
            """,
            result);
    }

    [Fact]
    public void EscapeJavaScriptString_UnicodeLinefeeds()
    {
        var result = JavaScriptUtils.ToEscapedJavaScriptString("before\u0085after".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "before\u0085after"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("before\u2028after".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "before\u2028after"
            """,
            result);

        result = JavaScriptUtils.ToEscapedJavaScriptString("before\u2029after".AsSpan(), '"', true, EscapeHandling.Default);
        Assert.Equal(
            """
            "before\u2029after"
            """,
            result);
    }

    [Fact]
    public void ToStringInvalid()
    {
        var exception = Assert.Throws<ArgumentException>(() => JsonConvert.ToString(new Version(1, 0)));
        Assert.Equal("Unsupported type: System.Version. Use the JsonSerializer class to get the object's JSON representation.", exception.Message);
    }

    [Fact]
    public void GuidToString()
    {
        var guid = new Guid("BED7F4EA-1A96-11d2-8F08-00A0C9A6186D");
        var json = JsonConvert.ToString(guid);
        Assert.Equal(
            """
            "bed7f4ea-1a96-11d2-8f08-00a0c9a6186d"
            """,
            json);
    }

    [Fact]
    public void EnumToString()
    {
        var json = JsonConvert.ToString(StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal("1", json);
    }

    [Fact]
    public void ObjectToString()
    {
        object value = 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = 1.1;
        Assert.Equal("1.1", JsonConvert.ToString(value));

        value = 1.1m;
        Assert.Equal("1.1", JsonConvert.ToString(value));

        value = (float) 1.1;
        Assert.Equal("1.1", JsonConvert.ToString(value));

        value = (short) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (long) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (byte) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (uint) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (ushort) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (sbyte) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = (ulong) 1;
        Assert.Equal("1", JsonConvert.ToString(value));

        value = new DateTime(ParseTests.InitialJavaScriptDateTicks, DateTimeKind.Utc);
        Assert.Equal(
            """
            "1970-01-01T00:00:00Z"
            """,
            JsonConvert.ToString(value));

        value = new DateTimeOffset(ParseTests.InitialJavaScriptDateTicks, TimeSpan.Zero);
        Assert.Equal(
            """
            "1970-01-01T00:00:00+00:00"
            """,
            JsonConvert.ToString(value));

        Assert.Equal("null", JsonConvert.ToString((object)null));

#if !NET6_0_OR_GREATER
        value = DBNull.Value;
        Assert.Equal("null", JsonConvert.ToString(value));
#endif

        value = "I am a string";
        Assert.Equal(
            """
            "I am a string"
            """,
            JsonConvert.ToString(value));

        value = true;
        Assert.Equal("true", JsonConvert.ToString(value));

        value = 'c';
        Assert.Equal(
            """
            "c"
            """,
            JsonConvert.ToString(value));
    }

    [Fact]
    public void TestInvalidStrings()
    {
        var orig = """this is a string "that has quotes" """;

        var serialized = JsonConvert.SerializeObject(orig);

        // *** Make string invalid by stripping \" \"
        serialized = serialized.Replace(@"\""", "\"");

        var exception = Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<string>(serialized));
        Assert.Equal("Additional text encountered after finished reading JSON content: t. Path '', line 1, position 19.", exception.Message);
    }

    [Fact]
    public void DeserializeValueObjects()
    {
        var i = JsonConvert.DeserializeObject<int>("1");
        Assert.Equal(1, i);

        var d = JsonConvert.DeserializeObject<DateTimeOffset>(
            """
            "2013-08-14T04:38:31.000+0000"
            """);
        Assert.Equal(new(new(2013, 8, 14, 4, 38, 31, DateTimeKind.Utc)), d);

        var b = JsonConvert.DeserializeObject<bool>("true");
        Assert.True(b);

        var n = JsonConvert.TryDeserializeObject<object>("null");
        Assert.Null(n);

        var u = JsonConvert.TryDeserializeObject<object>("undefined");
        Assert.Null(u);
    }

    [Fact]
    public void FloatToString()
    {
        Assert.Equal("1.1", JsonConvert.ToString(1.1));
        Assert.Equal("1.11", JsonConvert.ToString(1.11));
        Assert.Equal("1.111", JsonConvert.ToString(1.111));
        Assert.Equal("1.1111", JsonConvert.ToString(1.1111));
        Assert.Equal("1.11111", JsonConvert.ToString(1.11111));
        Assert.Equal("1.111111", JsonConvert.ToString(1.111111));
        Assert.Equal("1.0", JsonConvert.ToString(1.0));
        Assert.Equal("1.0", JsonConvert.ToString(1d));
        Assert.Equal("-1.0", JsonConvert.ToString(-1d));
        Assert.Equal("1.01", JsonConvert.ToString(1.01));
        Assert.Equal("1.001", JsonConvert.ToString(1.001));
        Assert.Equal(JsonConvert.PositiveInfinity, JsonConvert.ToString(double.PositiveInfinity));
        Assert.Equal(JsonConvert.NegativeInfinity, JsonConvert.ToString(double.NegativeInfinity));
        Assert.Equal(JsonConvert.NaN, JsonConvert.ToString(double.NaN));
    }

    [Fact]
    public void DecimalToString()
    {
        Assert.Equal("1.1", JsonConvert.ToString(1.1m));
        Assert.Equal("1.11", JsonConvert.ToString(1.11m));
        Assert.Equal("1.111", JsonConvert.ToString(1.111m));
        Assert.Equal("1.1111", JsonConvert.ToString(1.1111m));
        Assert.Equal("1.11111", JsonConvert.ToString(1.11111m));
        Assert.Equal("1.111111", JsonConvert.ToString(1.111111m));
        Assert.Equal("1.0", JsonConvert.ToString(1.0m));
        Assert.Equal("-1.0", JsonConvert.ToString(-1.0m));
        Assert.Equal("-1.0", JsonConvert.ToString(-1m));
        Assert.Equal("1.0", JsonConvert.ToString(1m));
        Assert.Equal("1.01", JsonConvert.ToString(1.01m));
        Assert.Equal("1.001", JsonConvert.ToString(1.001m));
        Assert.Equal("79228162514264337593543950335.0", JsonConvert.ToString(decimal.MaxValue));
        Assert.Equal("-79228162514264337593543950335.0", JsonConvert.ToString(decimal.MinValue));
    }

    [Fact]
    public void StringEscaping()
    {
        var v = "It's a good day\r\n\"sunshine\"";

        var json = JsonConvert.ToString(v);
        Assert.Equal(
            """
            "It's a good day\r\n\"sunshine\""
            """,
            json);
    }

    [Fact]
    public void ToEscapeHandling()
    {
        var v = "<b>hi €</b>";

        var json = JsonConvert.ToString(v, '"');
        Assert.Equal(@"""<b>hi " + '\u20AC' + @"</b>""", json);

        json = JsonConvert.ToString(v, '"', EscapeHandling.EscapeHtml);
        Assert.Equal(@"""\u003cb\u003ehi " + '\u20AC' + @"\u003c/b\u003e""", json);

        json = JsonConvert.ToString(v, '"', EscapeHandling.EscapeNonAscii);
        Assert.Equal(
            """
            "<b>hi \u20ac</b>"
            """,
            json);

        json = JsonConvert.ToString(v, '"', EscapeHandling.None);
        Assert.Equal(
            """
            "<b>hi €</b>"
            """,
            json);
    }

    [Fact]
    public void WriteDateTime()
    {
        var result = TestDateTime("DateTime Max", DateTime.MaxValue);
        Assert.Equal("9999-12-31T23:59:59.9999999", result.IsoDateRoundtrip);

        var year2000local = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Local);
        var localToUtcDate = year2000local.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK");

        result = TestDateTime("DateTime Local", year2000local);
        Assert.Equal("2000-01-01T01:01:01" + year2000local.GetOffset(), result.IsoDateRoundtrip);

        var millisecondsLocal = new DateTime(2000, 1, 1, 1, 1, 1, 999, DateTimeKind.Local);
        localToUtcDate = millisecondsLocal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK");

        result = TestDateTime("DateTime Local with milliseconds", millisecondsLocal);
        Assert.Equal("2000-01-01T01:01:01.999" + millisecondsLocal.GetOffset(), result.IsoDateRoundtrip);

        var ticksLocal = new DateTime(636556897826822481, DateTimeKind.Local);
        localToUtcDate = ticksLocal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK");

        result = TestDateTime("DateTime Local with ticks", ticksLocal);
        Assert.Equal("2018-03-03T16:03:02.6822481" + ticksLocal.GetOffset(), result.IsoDateRoundtrip);

        var year2000Unspecified = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);

        result = TestDateTime("DateTime Unspecified", year2000Unspecified);
        Assert.Equal("2000-01-01T01:01:01", result.IsoDateRoundtrip);

        var year2000Utc = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);
        var utcTolocalDate = year2000Utc.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss");

        result = TestDateTime("DateTime Utc", year2000Utc);
        Assert.Equal("2000-01-01T01:01:01Z", result.IsoDateRoundtrip);

        var unixEpoc = new DateTime(621355968000000000, DateTimeKind.Utc);
        utcTolocalDate = unixEpoc.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss");

        result = TestDateTime("DateTime Unix Epoc", unixEpoc);
        Assert.Equal("1970-01-01T00:00:00Z", result.IsoDateRoundtrip);

        result = TestDateTime("DateTime Min", DateTime.MinValue);
        Assert.Equal("0001-01-01T00:00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTime Default", default(DateTime));
        Assert.Equal("0001-01-01T00:00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset TimeSpan Zero", new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero));
        Assert.Equal("2000-01-01T01:01:01+00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset TimeSpan 1 hour", new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(1)));
        Assert.Equal("2000-01-01T01:01:01+01:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset TimeSpan 1.5 hour", new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(1.5)));
        Assert.Equal("2000-01-01T01:01:01+01:30", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset TimeSpan 13 hour", new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(13)));
        Assert.Equal("2000-01-01T01:01:01+13:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset TimeSpan with ticks", new DateTimeOffset(634663873826822481, TimeSpan.Zero));
        Assert.Equal("2012-03-03T16:03:02.6822481+00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset Min", DateTimeOffset.MinValue);
        Assert.Equal("0001-01-01T00:00:00+00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset Max", DateTimeOffset.MaxValue);
        Assert.Equal("9999-12-31T23:59:59.9999999+00:00", result.IsoDateRoundtrip);

        result = TestDateTime("DateTimeOffset Default", default(DateTimeOffset));
        Assert.Equal("0001-01-01T00:00:00+00:00", result.IsoDateRoundtrip);
    }

    public class DateTimeResult
    {
        public string IsoDateRoundtrip { get; set; }
    }

    static DateTimeResult TestDateTime<T>(string name, T value)
    {
        Console.WriteLine(name);

        var result = new DateTimeResult
        {
            IsoDateRoundtrip = TestDateTimeFormat(value)
        };

        TestDateTimeFormat(value, new IsoDateTimeConverter());

        if (value is DateTime)
        {
            Console.WriteLine(XmlConvert.ToString((DateTime) (object) value, XmlDateTimeSerializationMode.RoundtripKind));
        }
        else
        {
            Console.WriteLine(XmlConvert.ToString((DateTimeOffset) (object) value));
        }

        var ms = new MemoryStream();
        var s = new DataContractSerializer(typeof(T));
        s.WriteObject(ms, value);
        var json = Encoding.UTF8.GetString(ms.ToArray(), 0, Convert.ToInt32(ms.Length));
        Console.WriteLine(json);

        Console.WriteLine();

        return result;
    }

    static string TestDateTimeFormat<T>(T value)
    {
        string date;

        if (value is DateTime)
        {
            date = JsonConvert.ToString((DateTime) (object) value);
        }
        else
        {
            date = JsonConvert.ToString((DateTimeOffset) (object) value);
        }

        var parsed = JsonConvert.DeserializeObject<T>(date);
        if (!value.Equals(parsed))
        {
            var valueTicks = GetTicks(value);
            var parsedTicks = GetTicks(parsed);

            valueTicks = valueTicks / 10000 * 10000;

            Assert.Equal(valueTicks, parsedTicks);
        }

        return date.Trim('"');
    }

    static void TestDateTimeFormat<T>(T value, JsonConverter converter)
    {
        var date = Write(value, converter);

        Console.WriteLine(converter.GetType().Name + ": " + date);

        var parsed = Read<T>(date, converter);

        try
        {
            Assert.Equal(value, parsed);
        }
        catch (Exception)
        {
            // JavaScript ticks aren't as precise, recheck after rounding
            var valueTicks = GetTicks(value);
            var parsedTicks = GetTicks(parsed);

            valueTicks = valueTicks / 10000 * 10000;

            Assert.Equal(valueTicks, parsedTicks);
        }
    }

    public static long GetTicks(object value) =>
        value is DateTime time ? time.Ticks : ((DateTimeOffset) value).Ticks;

    public static string Write(object value, JsonConverter converter)
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        converter.WriteJson(jsonWriter, value, null);

        jsonWriter.Flush();
        return stringWriter.ToString();
    }

    public static T Read<T>(string text, JsonConverter converter)
    {
        var reader = new JsonTextReader(new StringReader(text));
        reader.ReadAsString();

        return (T) converter.ReadJson(reader, typeof(T), null, null);
    }

    [Fact]
    public void DeserializeObject()
    {
        var json = """
            {
                'Name': 'Bad Boys',
                'ReleaseDate': '1995-4-7T00:00:00',
                'Genres': [
                  'Action',
                  'Comedy'
                ]
            }
            """;

        var m = JsonConvert.DeserializeObject<Movie>(json);

        var name = m.Name;
        // Bad Boys

        Assert.Equal("Bad Boys", m.Name);
    }

    [Fact]
    public void MaximumDateTimeOffsetLength()
    {
        var dt = new DateTimeOffset(2000, 12, 31, 20, 59, 59, new(0, 11, 33, 0, 0));
        dt = dt.AddTicks(9999999);

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        jsonWriter.WriteValue(dt);
        jsonWriter.Flush();

        Assert.Equal(
            """
            "2000-12-31T20:59:59.9999999+11:33"
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void MaximumDateTimeLength()
    {
        var dt = new DateTime(2000, 12, 31, 20, 59, 59, DateTimeKind.Local);
        dt = dt.AddTicks(9999999);

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);

        jsonWriter.WriteValue(dt);
        jsonWriter.Flush();
    }

    [Fact]
    public void IntegerLengthOverflows()
    {
        // Maximum javascript number length (in characters) is 380
        var o = JObject.Parse("""{"biginteger":""" + new string('9', 380) + "}");
        var v = (JValue) o["biginteger"];
        Assert.Equal(JTokenType.Integer, v.Type);
        Assert.Equal(typeof(BigInteger), v.Value.GetType());
        Assert.Equal(BigInteger.Parse(new('9', 380)), (BigInteger) v.Value);

        var messages = $"JSON integer {new string('9', 381)} is too large to parse. Path 'biginteger', line 1, position 395.";
        var json = """{"biginteger":""" + new string('9', 381) + "}";
        var exception = Assert.Throws<JsonReaderException>(() => JObject.Parse(json));
        Assert.Equal(messages, exception.Message);
    }

#if false
        [Fact]
        public void StackOverflowTest()
        {
            StringBuilder sb = new StringBuilder();

            int depth = 900;
            for (int i = 0; i < depth; i++)
            {
                sb.Append("{'A':");
            }

            // invalid json
            sb.Append("{***}");
            for (int i = 0; i < depth; i++)
            {
                sb.Append("}");
            }

            string json = sb.ToString();
            JsonSerializer serializer = new JsonSerializer() { };
            serializer.Deserialize<Nest>(new JsonTextReader(new StringReader(json)));
        }
#endif

    public class Nest
    {
        public Nest A { get; set; }
    }

    //[Fact]
    //[Ignore("https://github.com/dotnet/roslyn/issues/36974")]
    //public void JsonConverterConstructor_OverloadsWithBaseTypes()
    //{
    //    OverloadWithBaseType value = new OverloadWithBaseType();
    //    string json = JsonConvert.SerializeObject(value);

    //    Assert.AreEqual("{\"Overload\":\"IList<string>\"}", json);
    //}

    [Fact]
    public void CustomDoubleRounding()
    {
        var measurements = new Measurements
        {
            Positions = [57.724227689317019, 60.440934405753069, 63.444192925248643, 66.813119113482557, 70.4496501404433],
            Gain = 12345.67895111213
        };

        var json = JsonConvert.SerializeObject(measurements);

        Assert.Equal("{\"Positions\":[57.72,60.44,63.44,66.81,70.45],\"Gain\":12345.68}", json);
    }

    public class Measurements
    {
        [JsonProperty(ItemConverterType = typeof(RoundingJsonConverter))]
        public List<double> Positions { get; set; }

        [JsonConverter(typeof(RoundingJsonConverter))]
        public double Gain { get; set; }
    }

    public class RoundingJsonConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanConvert(Type type) =>
            type == typeof(double);

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue(Math.Round((double) value, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void GenericBaseClassSerialization()
    {
        var json = JsonConvert.SerializeObject(new NonGenericChildClass());
        Assert.Equal("{\"Data\":null}", json);
    }

    // ReSharper disable once UnusedTypeParameter
    public class GenericBaseClass<O, T>
    {
        public virtual T Data { get; set; }
    }

    public class GenericIntermediateClass<O> : GenericBaseClass<O, string>
    {
        public override string Data { get; set; }
    }

    public class NonGenericChildClass : GenericIntermediateClass<int>;

    [Fact]
    public void ShouldNotPopulateReadOnlyEnumerableObjectWithNonDefaultConstructor()
    {
        object actual = JsonConvert.DeserializeObject<HasReadOnlyEnumerableObject>("{\"foo\":{}}");
        Assert.NotNull(actual);
    }

    [Fact]
    public void ShouldNotPopulateReadOnlyEnumerableObjectWithDefaultConstructor()
    {
        object actual = JsonConvert.DeserializeObject<HasReadOnlyEnumerableObjectAndDefaultConstructor>("{\"foo\":{}}");
        Assert.NotNull(actual);
    }

    [Fact]
    public void ShouldNotPopulateConstructorArgumentEnumerableObject()
    {
        object actual = JsonConvert.DeserializeObject<AcceptsEnumerableObjectToConstructor>("{\"foo\":{}}");
        Assert.NotNull(actual);
    }

    [Fact]
    public void ShouldNotPopulateEnumerableObjectProperty()
    {
        object actual = JsonConvert.DeserializeObject<HasEnumerableObject>("{\"foo\":{}}");
        Assert.NotNull(actual);
    }

    [Fact]
    public void ShouldNotPopulateReadOnlyDictionaryObjectWithNonDefaultConstructor()
    {
        object actual = JsonConvert.DeserializeObject<HasReadOnlyDictionary>("{\"foo\":{'key':'value'}}");
        Assert.NotNull(actual);
    }

    public sealed class HasReadOnlyDictionary
    {
        [JsonProperty("foo")] public IReadOnlyDictionary<string, string> Foo { get; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        [Argon.JsonConstructor]
        public HasReadOnlyDictionary([JsonProperty("bar")] int bar)
        {
        }
    }

    public sealed class HasReadOnlyEnumerableObject
    {
        [JsonProperty("foo")] public EnumerableWithConverter Foo { get; } = new();

        [Argon.JsonConstructor]
        public HasReadOnlyEnumerableObject([JsonProperty("bar")] int bar)
        {
        }
    }

    public sealed class HasReadOnlyEnumerableObjectAndDefaultConstructor
    {
        [JsonProperty("foo")] public EnumerableWithConverter Foo { get; } = new();

        [Argon.JsonConstructor]
        public HasReadOnlyEnumerableObjectAndDefaultConstructor()
        {
        }
    }

    public sealed class AcceptsEnumerableObjectToConstructor
    {
        [Argon.JsonConstructor]
        public AcceptsEnumerableObjectToConstructor
        (
            [JsonProperty("foo")] EnumerableWithConverter foo,
            [JsonProperty("bar")] int bar
        )
        {
        }
    }

    public sealed class HasEnumerableObject
    {
        [JsonProperty("foo")] public EnumerableWithConverter Foo { get; set; } = new();

        [Argon.JsonConstructor]
        public HasEnumerableObject([JsonProperty("bar")] int bar)
        {
        }
    }

    [JsonConverter(typeof(Converter))]
    public sealed class EnumerableWithConverter : IEnumerable<int>
    {
        public sealed class Converter : JsonConverter
        {
            public override bool CanConvert(Type type) =>
                type == typeof(Foo);

            public override object ReadJson
                (JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
            {
                reader.Skip();
                return new EnumerableWithConverter();
            }

            public override void WriteJson
                (JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    [Fact]
    public void ShouldNotRequireIgnoredPropertiesWithItemsRequired()
    {
        var json = """
            {
              "exp": 1483228800,
              "active": true
            }
            """;
        var value = JsonConvert.DeserializeObject<ItemsRequiredObjectWithIgnoredProperty>(json);
        Assert.NotNull(value);
        Assert.Equal(value.Expiration, new(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.True(value.Active);
    }

    [JsonObject(ItemRequired = Required.Always)]
    public sealed class ItemsRequiredObjectWithIgnoredProperty
    {
        static readonly DateTime s_unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [JsonProperty("exp")]
        int _expiration
        {
            get => (int) (Expiration - s_unixEpoch).TotalSeconds;
            set => Expiration = s_unixEpoch.AddSeconds(value);
        }

        public bool Active { get; set; }

        [JsonIgnore] public DateTime Expiration { get; set; }
    }
}