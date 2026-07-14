// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment

public class JValueTests : TestFixtureBase
{
    // CompareBigInteger used Math.Abs on the fractional part, discarding its sign and
    // inverting comparisons when the other operand had a negative fraction.
    [Fact]
    public void BigIntegerComparesCorrectlyAgainstNegativeFraction()
    {
        // -5 > -5.5
        Assert.True(new JValue(BigInteger.Parse("-5")).CompareTo(new(-5.5)) > 0);
        Assert.True(new JValue(-5.5).CompareTo(new(BigInteger.Parse("-5"))) < 0);

        // 5 < 5.5 (positive fraction still correct)
        Assert.True(new JValue(BigInteger.Parse("5")).CompareTo(new(5.5)) < 0);
        Assert.True(new JValue(5.5).CompareTo(new(BigInteger.Parse("5"))) > 0);

        // equal integers
        Assert.Equal(0, new JValue(BigInteger.Parse("5")).CompareTo(new(5.0)));
    }

    // GetHashCode/GetDeepHashCode hashed the boxed CLR value, so numerically-equal
    // JValues with different backing types produced different hashes, breaking hash-based
    // lookups through JToken.EqualityComparer.
    [Fact]
    public void EqualNumericValuesHaveEqualHashcodes()
    {
        var intBacked = (JValue) JToken.FromObject(-1);
        var longBacked = new JValue(-1L);
        Assert.True(intBacked.Equals(longBacked));
        Assert.Equal(intBacked.GetHashCode(), longBacked.GetHashCode());

        var decimalBacked = new JValue(1.5m);
        var doubleBacked = new JValue(1.5d);
        Assert.True(decimalBacked.Equals(doubleBacked));
        Assert.Equal(decimalBacked.GetHashCode(), doubleBacked.GetHashCode());
    }

    [Fact]
    public void UndefinedTests()
    {
        var v = JValue.CreateUndefined();

        Assert.Equal(JTokenType.Undefined, v.Type);
        Assert.Null(v.Value);

        Assert.Equal("", v.ToString());
        Assert.Equal("undefined", v.ToString(Formatting.None));
    }

    [Fact]
    public void ToObjectEnum()
    {
        var v = new JValue("OrdinalIgnoreCase").ToObject<StringComparison?>();
        Assert.Equal(StringComparison.OrdinalIgnoreCase, v.Value);

        v = JValue.CreateNull().ToObject<StringComparison?>();
        Assert.Null(v);

        v = new JValue(5).ToObject<StringComparison?>();
        Assert.Equal(StringComparison.OrdinalIgnoreCase, v.Value);

        v = new JValue(20).ToObject<StringComparison?>();
        Assert.Equal((StringComparison) 20, v.Value);

        v = new JValue(20).ToObject<StringComparison>();
        Assert.Equal((StringComparison) 20, v.Value);

        v = JsonConvert.DeserializeObject<StringComparison?>("20");
        Assert.Equal((StringComparison) 20, v.Value);

        v = JsonConvert.DeserializeObject<StringComparison>("20");
        Assert.Equal((StringComparison) 20, v.Value);
    }

    [Fact]
    public void FloatParseHandling()
    {
        var v = (JValue) JToken.ReadFrom(
            new JsonTextReader(new StringReader("9.9"))
            {
                FloatParseHandling = Argon.FloatParseHandling.Decimal
            });

        Assert.Equal(9.9m, v.Value);
        Assert.Equal(typeof(decimal), v.Value.GetType());
    }

    [Fact]
    public void ToObjectWithDefaultSettings()
    {
        try
        {
            JsonConvert.DefaultSettings = () => new()
            {
                Converters =
                {
                    new MetroStringConverter()
                }
            };

            var v = new JValue(":::STRING:::");
            var s = v.ToObject<string>();

            Assert.Equal("string", s);
        }
        finally
        {
            JsonConvert.DefaultSettings = null;
        }
    }

    [Fact]
    public void ChangeValue()
    {
        var v = new JValue(true);
        Assert.True((bool) v.Value);
        Assert.Equal(JTokenType.Boolean, v.Type);

        v.Value = "Pie";
        Assert.Equal("Pie", v.Value);
        Assert.Equal(JTokenType.String, v.Type);

        v.Value = null;
        Assert.Null(v.Value);
        Assert.Equal(JTokenType.Null, v.Type);

        v.Value = null;
        Assert.Null(v.Value);
        Assert.Equal(JTokenType.Null, v.Type);

        v.Value = "Pie";
        Assert.Equal("Pie", v.Value);
        Assert.Equal(JTokenType.String, v.Type);

        v.Value = DBNull.Value;
        Assert.Equal(DBNull.Value, v.Value);
        Assert.Equal(JTokenType.Null, v.Type);

        var data = Array.Empty<byte>();
        v.Value = data;

        Assert.Equal(data, v.Value);
        Assert.Equal(JTokenType.Bytes, v.Type);

        v.Value = StringComparison.OrdinalIgnoreCase;
        Assert.Equal(StringComparison.OrdinalIgnoreCase, v.Value);
        Assert.Equal(JTokenType.Integer, v.Type);

        v.Value = new Uri("http://json.codeplex.com/");
        Assert.Equal(new Uri("http://json.codeplex.com/"), v.Value);
        Assert.Equal(JTokenType.Uri, v.Type);

        v.Value = TimeSpan.FromDays(1);
        Assert.Equal(TimeSpan.FromDays(1), v.Value);
        Assert.Equal(JTokenType.TimeSpan, v.Type);

        var g = Guid.NewGuid();
        v.Value = g;
        Assert.Equal(g, v.Value);
        Assert.Equal(JTokenType.Guid, v.Type);

        var i = BigInteger.Parse("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990");
        v.Value = i;
        Assert.Equal(i, v.Value);
        Assert.Equal(JTokenType.Integer, v.Type);
    }

    [Fact]
    public void CreateComment()
    {
        var commentValue = JValue.CreateComment(null);
        Assert.Null(commentValue.Value);
        Assert.Equal(JTokenType.Comment, commentValue.Type);

        commentValue.Value = "Comment";
        Assert.Equal("Comment", commentValue.Value);
        Assert.Equal(JTokenType.Comment, commentValue.Type);
    }

    [Fact]
    public void CreateString()
    {
        var stringValue = JValue.CreateString(null);
        Assert.Null(stringValue.Value);
        Assert.Equal(JTokenType.String, stringValue.Type);
    }

    [Fact]
    public void JValueToString()
    {
        var v = new JValue(true);
        Assert.Equal("True", v.ToString());

        v = new("Blah"u8.ToArray());
        Assert.Equal("System.Byte[]", v.ToString(null, InvariantCulture));

        v = new("I am a string!");
        Assert.Equal("I am a string!", v.ToString());

        v = JValue.CreateNull();
        Assert.Equal("", v.ToString());

        v = JValue.CreateNull();
        Assert.Equal("", v.ToString(null, InvariantCulture));

        v = new(new DateTime(2000, 12, 12, 20, 59, 59, DateTimeKind.Utc), JTokenType.Date);
        Assert.Equal("12/12/2000 20:59:59", v.ToString(null, InvariantCulture));

        v = new(new Uri("http://json.codeplex.com/"));
        Assert.Equal("http://json.codeplex.com/", v.ToString(null, InvariantCulture));

        v = new(TimeSpan.FromDays(1));
        Assert.Equal("1.00:00:00", v.ToString(null, InvariantCulture));

        v = new(new Guid("B282ADE7-C520-496C-A448-4084F6803DE5"));
        Assert.Equal("b282ade7-c520-496c-a448-4084f6803de5", v.ToString(null, InvariantCulture));

        v = new(BigInteger.Parse("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990"));
        Assert.Equal("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990", v.ToString(null, InvariantCulture));
    }

    [Fact]
    public void JValueParse()
    {
        var v = (JValue) JToken.Parse("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990");

        Assert.Equal(JTokenType.Integer, v.Type);
        Assert.Equal(BigInteger.Parse("123456789999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999990"), v.Value);
    }

    [Fact]
    public void JValueIConvertible() =>
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        Assert.True(new JValue(0) is IConvertible);

    [Fact]
    public void Last()
    {
        var v = new JValue(true);
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var last = v.Last;
        });
        Assert.Equal("Cannot access child value on Argon.JValue.", exception.Message);
    }

    [Fact]
    public void Children()
    {
        var v = new JValue(true);
        var c = v.Children();
        Assert.Equal(JEnumerable<JToken>.Empty, c);
    }

    [Fact]
    public void First()
    {
        var v = new JValue(true);
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var first = v.First;
        });
        Assert.Equal("Cannot access child value on Argon.JValue.", exception.Message);
    }

    [Fact]
    public void Item()
    {
        var v = new JValue(true);
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var first = v[0];
        });
        Assert.Equal("Cannot access child value on Argon.JValue.", exception.Message);
    }

    [Fact]
    public void Values()
    {
        var v = new JValue(true);
        var exception = Assert.Throws<InvalidOperationException>(() => v.Values<int>());
        Assert.Equal("Cannot access child value on Argon.JValue.", exception.Message);
    }

    [Fact]
    public void RemoveParentNull()
    {
        var v = new JValue(true);
        var exception = Assert.Throws<InvalidOperationException>(() => v.Remove());
        Assert.Equal("The parent is missing.", exception.Message);
    }

    [Fact]
    public void Root()
    {
        var v = new JValue(true);
        Assert.Equal(v, v.Root);
    }

    [Fact]
    public void Previous()
    {
        var v = new JValue(true);
        Assert.Null(v.Previous);
    }

    [Fact]
    public void Next()
    {
        var v = new JValue(true);
        Assert.Null(v.Next);
    }

    [Fact]
    public void DeepEquals()
    {
        Assert.True(JToken.DeepEquals(new JValue(5L), new JValue(5)));
        Assert.False(JToken.DeepEquals(new JValue(5M), new JValue(5)));
        Assert.True(JToken.DeepEquals(new JValue((ulong) long.MaxValue), new JValue(long.MaxValue)));
        Assert.False(JToken.DeepEquals(new JValue(0.102410241024102424m), new JValue(0.102410241024102425m)));
    }

    [Fact]
    public void HasValues() =>
        Assert.False(new JValue(5L).HasValues);

    [Fact]
    public void SetValue()
    {
        JToken t = new JValue(5L);
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            t[0] = new JValue(3);
        });
        Assert.Equal("Cannot set child value on Argon.JValue.", exception.Message);
    }

    [Fact]
    public void CastNullValueToNonNullable()
    {
        var v = JValue.CreateNull();
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var i = (int) v;
        });
        Assert.Equal("Can not convert Null to Int32.", exception.Message);
    }

    [Fact]
    public void ConvertValueToCompatibleType()
    {
        var c = new JValue(1).Value<IComparable>();
        Assert.Equal(1L, c);
    }

    [Fact]
    public void ConvertValueToFormattableType()
    {
        var f = new JValue(1).Value<IFormattable>();
        Assert.Equal(1L, f);

        Assert.Equal("01", f.ToString("00", InvariantCulture));
    }

    [Fact]
    public void Ordering()
    {
        var o = new JObject(
            new JProperty("Integer", new JValue(1)),
            new JProperty("Float", new JValue(1.2d)),
            new JProperty("Decimal", new JValue(1.1m))
        );

        var orderedValues = o.Values().Cast<JValue>().OrderBy(v => v).Select(v => v.Value).ToList();

        Assert.Equal(1L, orderedValues[0]);
        Assert.Equal(1.1m, orderedValues[1]);
        Assert.Equal(1.2d, orderedValues[2]);
    }

    [Fact]
    public void WriteSingle()
    {
        var f = 5.2f;
        var value = new JValue(f);

        var json = value.ToString(Formatting.None);

        Assert.Equal("5.2", json);
    }

    public class Rate
    {
        public decimal Compoundings { get; set; }
    }

    readonly Rate rate = new()
    {
        Compoundings = 12.166666666666666666666666667m
    };

    [Fact]
    public void WriteFullDecimalPrecision()
    {
        var jTokenWriter = new JTokenWriter();
        new JsonSerializer().Serialize(jTokenWriter, rate);
        var json = jTokenWriter.Token.ToString();
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Compoundings": 12.166666666666666666666666667
            }
            """,
            json);
    }

    [Fact]
    public void RoundTripDecimal()
    {
        var jTokenWriter = new JTokenWriter();
        new JsonSerializer().Serialize(jTokenWriter, rate);
        var rate2 = new JsonSerializer().Deserialize<Rate>(new JTokenReader(jTokenWriter.Token));

        Assert.Equal(rate.Compoundings, rate2.Compoundings);
    }

    public class ObjectWithDateTimeOffset
    {
        public DateTimeOffset DateTimeOffset { get; set; }
    }

    [Fact]
    public void SetDateTimeOffsetProperty()
    {
        var dateTimeOffset = new DateTimeOffset(new DateTime(2000, 1, 1), TimeSpan.FromHours(3));
        var json = JsonConvert.SerializeObject(
            new ObjectWithDateTimeOffset
            {
                DateTimeOffset = dateTimeOffset
            });

        var o = JObject.Parse(json);
        o.Property("DateTimeOffset").Value = dateTimeOffset;
    }

    [Fact]
    public void ConvertsToBoolean() =>
        Assert.True(Convert.ToBoolean(new JValue(true)));

    [Fact]
    public void ConvertsToBoolean_String() =>
        Assert.True(Convert.ToBoolean(new JValue("true")));

    [Fact]
    public void ConvertsToInt32() =>
        Assert.Equal(int.MaxValue, Convert.ToInt32(new JValue(int.MaxValue)));

    [Fact]
    public void ConvertsToInt32_BigInteger() =>
        Assert.Equal(123, Convert.ToInt32(new JValue(BigInteger.Parse("123"))));

    [Fact]
    public void ConvertsToChar() =>
        Assert.Equal('c', Convert.ToChar(new JValue('c')));

    [Fact]
    public void ConvertsToSByte() =>
        Assert.Equal(sbyte.MaxValue, Convert.ToSByte(new JValue(sbyte.MaxValue)));

    [Fact]
    public void ConvertsToByte() =>
        Assert.Equal(byte.MaxValue, Convert.ToByte(new JValue(byte.MaxValue)));

    [Fact]
    public void ConvertsToInt16() =>
        Assert.Equal(short.MaxValue, Convert.ToInt16(new JValue(short.MaxValue)));

    [Fact]
    public void ConvertsToUInt16() =>
        Assert.Equal(ushort.MaxValue, Convert.ToUInt16(new JValue(ushort.MaxValue)));

    [Fact]
    public void ConvertsToUInt32() =>
        Assert.Equal(uint.MaxValue, Convert.ToUInt32(new JValue(uint.MaxValue)));

    [Fact]
    public void ConvertsToInt64() =>
        Assert.Equal(long.MaxValue, Convert.ToInt64(new JValue(long.MaxValue)));

    [Fact]
    public void ConvertsToUInt64() =>
        Assert.Equal(ulong.MaxValue, Convert.ToUInt64(new JValue(ulong.MaxValue)));

    [Fact]
    public void ConvertsToSingle() =>
        Assert.Equal(float.MaxValue, Convert.ToSingle(new JValue(float.MaxValue)));

    [Fact]
    public void ConvertsToDouble() =>
        Assert.Equal(double.MaxValue, Convert.ToDouble(new JValue(double.MaxValue)));

    [Fact]
    public void ConvertsToDecimal() =>
        Assert.Equal(decimal.MaxValue, Convert.ToDecimal(new JValue(decimal.MaxValue)));

    [Fact]
    public void ConvertsToDecimal_Int64() =>
        Assert.Equal(123, Convert.ToDecimal(new JValue(123)));

    [Fact]
    public void ConvertsToString_Decimal() =>
        Assert.Equal("79228162514264337593543950335", Convert.ToString(new JValue(decimal.MaxValue)));

    [Fact]
    public void ConvertsToString_Uri() =>
        Assert.Equal("http://www.google.com/", Convert.ToString(new JValue(new Uri("http://www.google.com"))));

    [Fact]
    public void ConvertsToString_Null() =>
        Assert.Equal(string.Empty, Convert.ToString(JValue.CreateNull()));

    [Fact]
    public void ConvertsToString_Guid()
    {
        var g = new Guid("0B5D4F85-E94C-4143-94C8-35F2AAEBB100");

        Assert.Equal("0b5d4f85-e94c-4143-94c8-35f2aaebb100", Convert.ToString(new JValue(g)));
    }

    [Fact]
    public void ConvertsToType() =>
        Assert.Equal(int.MaxValue, Convert.ChangeType(new JValue(int.MaxValue), typeof(int), InvariantCulture));

    [Fact]
    public void ConvertsToDateTime() =>
        Assert.Equal(new(2013, 02, 01, 01, 02, 03, 04), Convert.ToDateTime(new JValue(new DateTime(2013, 02, 01, 01, 02, 03, 04))));

    [Fact]
    public void ConvertsToDateTime_DateTimeOffset()
    {
        var offset = new DateTimeOffset(2013, 02, 01, 01, 02, 03, 04, TimeSpan.Zero);

        Assert.Equal(new(2013, 02, 01, 01, 02, 03, 04), Convert.ToDateTime(new JValue(offset)));
    }

    [Fact]
    public void ExplicitConversionTest()
    {
        const string example = "Hello";
        dynamic obj = new
        {
            data = Encoding.UTF8.GetBytes(example)
        };
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            using (TextWriter tw = new StreamWriter(ms))
            {
                JsonSerializer.Create().Serialize(tw, obj);
                tw.Flush();
                bytes = ms.ToArray();
            }
        }

        dynamic o = JObject.Parse(Encoding.UTF8.GetString(bytes));
        var dataBytes = (byte[]) o.data;
        Assert.Equal(example, Encoding.UTF8.GetString(dataBytes));
    }

    [Fact]
    public void GetTypeCode()
    {
        IConvertible v = new JValue(new Guid("0B5D4F85-E94C-4143-94C8-35F2AAEBB100"));
        Assert.Equal(TypeCode.Object, v.GetTypeCode());

        v = new JValue(new Uri("http://www.google.com"));
        Assert.Equal(TypeCode.Object, v.GetTypeCode());

        v = new JValue(new BigInteger(3));
        Assert.Equal(TypeCode.Object, v.GetTypeCode());

        v = new JValue(new DateTimeOffset(2000, 12, 12, 12, 12, 12, TimeSpan.Zero));
        Assert.Equal(TypeCode.Object, v.GetTypeCode());
    }

    [Fact]
    public void ToType()
    {
        IConvertible v = new JValue(9.0m);

        var i = (int) v.ToType(typeof(int), InvariantCulture);
        Assert.Equal(9, i);

        var bi = (BigInteger) v.ToType(typeof(BigInteger), InvariantCulture);
        Assert.Equal(new(9), bi);
    }

    [Fact]
    public void ToStringFormat()
    {
        var v = new JValue(new DateTime(2013, 02, 01, 01, 02, 03, 04));

        Assert.Equal("2013", v.ToString("yyyy"));
    }

    [Fact]
    public void ToStringNewTypes()
    {
        var a = new JArray(
            new JValue(new DateTimeOffset(2013, 02, 01, 01, 02, 03, 04, TimeSpan.FromHours(1))),
            new JValue(new BigInteger(5)),
            new JValue(1.1f)
        );

        XUnitAssert.AreEqualNormalized(
            """
            [
              "2013-02-01T01:02:03.004+01:00",
              5,
              1.1
            ]
            """,
            a.ToString());
    }

    [Fact]
    public void ToStringUri()
    {
        var a = new JArray(
            new JValue(new Uri("http://james.newtonking.com")),
            new JValue(new Uri("http://james.newtonking.com/install?v=7.0.1"))
        );

        XUnitAssert.AreEqualNormalized(
            """
            [
              "http://james.newtonking.com",
              "http://james.newtonking.com/install?v=7.0.1"
            ]
            """,
            a.ToString());
    }

    public class ReadOnlyStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotSupportedException();

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer) =>
            $"{reader.Value}!";

        public override bool CanConvert(Type type) =>
            type == typeof(string);

        public override bool CanWrite => false;
    }

    [Fact]
    public void ReadOnlyConverterTest()
    {
        var o = new JObject(new JProperty("name", "Hello World"));

        var json = o.ToString(Formatting.Indented, new ReadOnlyStringConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Hello World"
            }
            """,
            json);
    }

    [Fact]
    public void EnumTests()
    {
        var v = new JValue(StringComparison.Ordinal);
        Assert.Equal(JTokenType.Integer, v.Type);

        var s = v.ToString();
        Assert.Equal("Ordinal", s);

        var e = v.ToObject<StringComparison>();
        Assert.Equal(StringComparison.Ordinal, e);

        dynamic d = new JValue(StringComparison.CurrentCultureIgnoreCase);
        var e2 = (StringComparison) d;
        Assert.Equal(StringComparison.CurrentCultureIgnoreCase, e2);

        string s1 = d.ToString();
        Assert.Equal("CurrentCultureIgnoreCase", s1);

        var s2 = (string) d;
        Assert.Equal("CurrentCultureIgnoreCase", s2);

        d = new JValue("OrdinalIgnoreCase");
        var e3 = (StringComparison) d;
        Assert.Equal(StringComparison.OrdinalIgnoreCase, e3);

        v = new("ORDINAL");
        d = v;
        var e4 = (StringComparison) d;
        Assert.Equal(StringComparison.Ordinal, e4);

        var e5 = v.ToObject<StringComparison>();
        Assert.Equal(StringComparison.Ordinal, e5);

        v = new((int) StringComparison.OrdinalIgnoreCase);
        Assert.Equal(JTokenType.Integer, v.Type);
        var e6 = v.ToObject<StringComparison>();
        Assert.Equal(StringComparison.OrdinalIgnoreCase, e6);

        // does not support EnumMember. breaking change to add
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            d = new JValue("value_a");
            var e7 = (TestObjects.EnumA) d;
            Assert.Equal(TestObjects.EnumA.ValueA, e7);
        });
        Assert.Equal("Requested value 'value_a' was not found.", exception.Message);
    }

    public enum EnumA
    {
        [EnumMember(Value = "value_a")]
        ValueA
    }

    [Fact]
    public void CompareTo_MismatchedTypes()
    {
        var v1 = new JValue(1);
        var v2 = new JValue("2");

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(1.5);
        v2 = new("2");

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(1.5m);
        v2 = new("2");

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(1.5m);
        v2 = new(2);

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(1.5m);
        v2 = new(2.1);

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(2);
        v2 = new("2");

        Assert.Equal(0, v1.CompareTo(v2));
        Assert.Equal(0, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(0, v2.CompareTo(v1));
        Assert.Equal(0, ((IComparable) v2).CompareTo(v1));

        v1 = new(2);
        v2 = new(2m);

        Assert.Equal(0, v1.CompareTo(v2));
        Assert.Equal(0, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(0, v2.CompareTo(v1));
        Assert.Equal(0, ((IComparable) v2).CompareTo(v1));

        v1 = new(2f);
        v2 = new(2m);

        Assert.Equal(0, v1.CompareTo(v2));
        Assert.Equal(0, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(0, v2.CompareTo(v1));
        Assert.Equal(0, ((IComparable) v2).CompareTo(v1));

        v1 = new(2);
        v2 = new("10");

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new(2);
        v2 = new((object) null);

        Assert.Equal(1, v1.CompareTo(v2));
        Assert.Equal(1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(-1, v2.CompareTo(v1));
        Assert.Equal(-1, ((IComparable) v2).CompareTo(v1));

        v1 = new("2");
        v2 = new((object) null);

        Assert.Equal(1, v1.CompareTo(v2));
        Assert.Equal(1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(-1, v2.CompareTo(v1));
        Assert.Equal(-1, ((IComparable) v2).CompareTo(v1));

        v1 = new((object) null);
        v2 = new("2");

        Assert.Equal(-1, v1.CompareTo(v2));
        Assert.Equal(-1, ((IComparable) v1).CompareTo(v2));
        Assert.Equal(1, v2.CompareTo(v1));
        Assert.Equal(1, ((IComparable) v2).CompareTo(v1));

        v1 = new("2");
        v2 = null;

        // ReSharper disable ExpressionIsAlwaysNull
        Assert.Equal(1, v1.CompareTo(v2));
        Assert.Equal(1, ((IComparable) v1).CompareTo(v2));
        // ReSharper restore ExpressionIsAlwaysNull

        v1 = new((object) null);
        v2 = null;

        // ReSharper disable ExpressionIsAlwaysNull
        Assert.Equal(1, v1.CompareTo(v2));
        Assert.Equal(1, ((IComparable) v1).CompareTo(v2));
        // ReSharper restore ExpressionIsAlwaysNull
    }
}