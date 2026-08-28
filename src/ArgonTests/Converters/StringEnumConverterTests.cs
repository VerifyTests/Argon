// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class StringEnumConverterTests : TestFixtureBase
{
    public class EnumClass
    {
        public StoreColor StoreColor { get; set; }
        public StoreColor? NullableStoreColor1 { get; set; }
        public StoreColor? NullableStoreColor2 { get; set; }
    }

    public class EnumContainer<T>
    {
        public T Enum { get; set; }
    }

    [Flags]
    public enum FlagsTestEnum
    {
        Default = 0,
        First = 1,
        Second = 2
    }

    public enum NegativeEnum
    {
        Negative = -1,
        Zero = 0,
        Positive = 1
    }

    [Flags]
    public enum NegativeFlagsEnum
    {
        NegativeFour = -4,
        NegativeTwo = -2,
        NegativeOne = -1,
        Zero = 0,
        One = 1,
        Two = 2,
        Four = 4
    }

    public enum NamedEnum
    {
        [EnumMember(Value = "@first")] First,

        [EnumMember(Value = "@second")] Second,
        Third
    }

    public enum NamedEnumDuplicate
    {
        [EnumMember(Value = "Third")] First,

        [EnumMember(Value = "@second")] Second,
        Third
    }

    public enum NamedEnumWithComma
    {
        [EnumMember(Value = "@first")] First,

        [EnumMember(Value = "@second")] Second,

        [EnumMember(Value = ",third")] Third,

        [EnumMember(Value = ",")] JustComma
    }

    public class NegativeEnumClass
    {
        public NegativeEnum Value1 { get; set; }
        public NegativeEnum Value2 { get; set; }
    }

    public class NegativeFlagsEnumClass
    {
        public NegativeFlagsEnum Value1 { get; set; }
        public NegativeFlagsEnum Value2 { get; set; }
    }

    public enum CamelCaseEnumNew
    {
        This,
        Is,
        CamelCase
    }

    public enum SnakeCaseEnumNew
    {
        This,
        Is,
        SnakeCase
    }

    public enum NotAllowIntegerValuesEnum
    {
        Foo = 0,
        Bar = 1
    }

    public enum AllowIntegerValuesEnum
    {
        Foo = 0,
        Bar = 1
    }

    [Fact]
    public void NamingStrategyAndCamelCaseText()
    {
        var converter = new StringEnumConverter();
        Assert.Null(converter.NamingStrategy);
        converter.NamingStrategy = new CamelCaseNamingStrategy();
        Assert.NotNull(converter.NamingStrategy);
        Assert.Equal(typeof(CamelCaseNamingStrategy), converter.NamingStrategy.GetType());
    }

    [Fact]
    public void Serialize_CamelCaseFromAttribute()
    {
        var json = JsonConvert.SerializeObject(CamelCaseEnumNew.CamelCase, new StringEnumConverter(new CamelCaseNamingStrategy()));
        Assert.Equal(
            """
            "camelCase"
            """,
            json);
    }

    [Fact]
    public void Deserialize_CamelCaseFromAttribute()
    {
        var e = JsonConvert.DeserializeObject<CamelCaseEnumNew>(
            """
            "camelCase"
            """,
            new StringEnumConverter(new CamelCaseNamingStrategy()));
        Assert.Equal(CamelCaseEnumNew.CamelCase, e);
    }

    [Fact]
    public void Serialize_SnakeCaseFromAttribute()
    {
        var json = JsonConvert.SerializeObject(
            SnakeCaseEnumNew.SnakeCase,
            new StringEnumConverter(new SnakeCaseNamingStrategy()));
        Assert.Equal(
            """
            "snake_case"
            """,
            json);
    }

    [Fact]
    public void Deserialize_SnakeCaseFromAttribute()
    {
        var e = JsonConvert.DeserializeObject<SnakeCaseEnumNew>(
            """
            "snake_case"
            """,
            new StringEnumConverter(new SnakeCaseNamingStrategy()));
        Assert.Equal(SnakeCaseEnumNew.SnakeCase, e);
    }

    [Fact]
    public void Deserialize_NotAllowIntegerValuesFromAttribute()
    {
        var converter = new StringEnumConverter(new CamelCaseNamingStrategy(), false);
        Assert.Throws<JsonSerializationException>((Action) (() =>
        {
            JsonConvert.DeserializeObject<NotAllowIntegerValuesEnum>(
                """
                "9"
                """,
                converter);
        }));
    }

    [Fact]
    public void Deserialize_AllowIntegerValuesAttribute()
    {
        var e = JsonConvert.DeserializeObject<AllowIntegerValuesEnum>(
            """
            "9"
            """,
            new StringEnumConverter(new CamelCaseNamingStrategy()));
        Assert.Equal(9, (int) e);
    }

    [Fact]
    public void NamedEnumDuplicateTest()
    {
        var c = new EnumContainer<NamedEnumDuplicate>
        {
            Enum = NamedEnumDuplicate.First
        };

        var converter = new StringEnumConverter();
        var exception = Assert.Throws<InvalidOperationException>(() => JsonConvert.SerializeObject(c, Formatting.Indented, converter));
        Assert.Equal("Enum name 'Third' already exists on enum 'NamedEnumDuplicate'.", exception.Message);
    }

    [Fact]
    public void SerializeNameEnumTest()
    {
        var c = new EnumContainer<NamedEnum>
        {
            Enum = NamedEnum.First
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented, new StringEnumConverter());
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enum": "@first"
            }
            """,
            json);

        c = new()
        {
            Enum = NamedEnum.Third
        };

        json = JsonConvert.SerializeObject(c, Formatting.Indented, new StringEnumConverter());
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enum": "Third"
            }
            """,
            json);
    }

    [Fact]
    public void NamedEnumCommaTest()
    {
        var c = new EnumContainer<NamedEnumWithComma>
        {
            Enum = NamedEnumWithComma.Third
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented, new StringEnumConverter());
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enum": ",third"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<EnumContainer<NamedEnumWithComma>>(json, new StringEnumConverter());
        Assert.Equal(NamedEnumWithComma.Third, c2.Enum);
    }

    [Fact]
    public void NamedEnumCommaTest2()
    {
        var c = new EnumContainer<NamedEnumWithComma>
        {
            Enum = NamedEnumWithComma.JustComma
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented, new StringEnumConverter());
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enum": ","
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<EnumContainer<NamedEnumWithComma>>(json, new StringEnumConverter());
        Assert.Equal(NamedEnumWithComma.JustComma, c2.Enum);
    }

    [Fact]
    public void NamedEnumCommaCaseInsensitiveTest()
    {
        var c2 = JsonConvert.DeserializeObject<EnumContainer<NamedEnumWithComma>>(
            """{"Enum":",THIRD"}""",
            new StringEnumConverter());
        Assert.Equal(NamedEnumWithComma.Third, c2.Enum);
    }

    [Fact]
    public void DeserializeNameEnumTest()
    {
        var json = """
            {
              "Enum": "@first"
            }
            """;

        var c = JsonConvert.DeserializeObject<EnumContainer<NamedEnum>>(json, new StringEnumConverter());
        Assert.Equal(NamedEnum.First, c.Enum);

        json = """
            {
              "Enum": "Third"
            }
            """;

        c = JsonConvert.DeserializeObject<EnumContainer<NamedEnum>>(json, new StringEnumConverter());
        Assert.Equal(NamedEnum.Third, c.Enum);
    }

    [Fact]
    public void SerializeEnumClass()
    {
        var enumClass = new EnumClass
        {
            StoreColor = StoreColor.Red,
            NullableStoreColor1 = StoreColor.White,
            NullableStoreColor2 = null
        };

        var json = JsonConvert.SerializeObject(enumClass, Formatting.Indented, new StringEnumConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "StoreColor": "Red",
              "NullableStoreColor1": "White",
              "NullableStoreColor2": null
            }
            """,
            json);
    }

    [Fact]
    public void SerializeEnumClassWithCamelCase()
    {
        var enumClass = new EnumClass
        {
            StoreColor = StoreColor.Red,
            NullableStoreColor1 = StoreColor.DarkGoldenrod,
            NullableStoreColor2 = null
        };

        var json = JsonConvert.SerializeObject(enumClass, Formatting.Indented, new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "StoreColor": "red",
              "NullableStoreColor1": "darkGoldenrod",
              "NullableStoreColor2": null
            }
            """,
            json);
    }

    [Fact]
    public void SerializeEnumClassUndefined()
    {
        var enumClass = new EnumClass
        {
            StoreColor = (StoreColor) 1000,
            NullableStoreColor1 = (StoreColor) 1000,
            NullableStoreColor2 = null
        };

        var json = JsonConvert.SerializeObject(enumClass, Formatting.Indented, new StringEnumConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "StoreColor": 1000,
              "NullableStoreColor1": 1000,
              "NullableStoreColor2": null
            }
            """,
            json);
    }

    [Fact]
    public void SerializeFlagEnum()
    {
        var enumClass = new EnumClass
        {
            StoreColor = StoreColor.Red | StoreColor.White,
            NullableStoreColor1 = StoreColor.White & StoreColor.Yellow,
            NullableStoreColor2 = StoreColor.Red | StoreColor.White | StoreColor.Black
        };

        var json = JsonConvert.SerializeObject(enumClass, Formatting.Indented, new StringEnumConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "StoreColor": "Red, White",
              "NullableStoreColor1": 0,
              "NullableStoreColor2": "Black, Red, White"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeNegativeFlagsEnum()
    {
        var negativeEnumClass = new NegativeFlagsEnumClass
        {
            Value1 = NegativeFlagsEnum.NegativeFour | NegativeFlagsEnum.NegativeTwo,
            Value2 = NegativeFlagsEnum.Two | NegativeFlagsEnum.Four
        };

        var json = JsonConvert.SerializeObject(negativeEnumClass, Formatting.Indented, new StringEnumConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Value1": "NegativeTwo",
              "Value2": "Two, Four"
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeNegativeFlagsEnum()
    {
        var json = """
            {
              "Value1": "NegativeFour,NegativeTwo",
              "Value2": "NegativeFour,Four"
            }
            """;

        var negativeEnumClass = JsonConvert.DeserializeObject<NegativeFlagsEnumClass>(json, new StringEnumConverter());

        Assert.Equal(NegativeFlagsEnum.NegativeFour | NegativeFlagsEnum.NegativeTwo, negativeEnumClass.Value1);
        Assert.Equal(NegativeFlagsEnum.NegativeFour | NegativeFlagsEnum.Four, negativeEnumClass.Value2);
    }

    [Fact]
    public void SerializeNegativeEnum()
    {
        var negativeEnumClass = new NegativeEnumClass
        {
            Value1 = NegativeEnum.Negative,
            Value2 = (NegativeEnum) int.MinValue
        };

        var json = JsonConvert.SerializeObject(negativeEnumClass, Formatting.Indented, new StringEnumConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Value1": "Negative",
              "Value2": -2147483648
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeNegativeEnum()
    {
        var json = """
            {
              "Value1": "Negative",
              "Value2": -2147483648
            }
            """;

        var negativeEnumClass = JsonConvert.DeserializeObject<NegativeEnumClass>(json, new StringEnumConverter());

        Assert.Equal(NegativeEnum.Negative, negativeEnumClass.Value1);
        Assert.Equal((NegativeEnum) int.MinValue, negativeEnumClass.Value2);
    }

    [Fact]
    public void DeserializeFlagEnum()
    {
        var json = """
            {
              "StoreColor": "Red, White",
              "NullableStoreColor1": 0,
              "NullableStoreColor2": "black, Red, White"
            }
            """;

        var enumClass = JsonConvert.DeserializeObject<EnumClass>(json, new StringEnumConverter());

        Assert.Equal(StoreColor.Red | StoreColor.White, enumClass.StoreColor);
        Assert.Equal((StoreColor) 0, enumClass.NullableStoreColor1);
        Assert.Equal(StoreColor.Red | StoreColor.White | StoreColor.Black, enumClass.NullableStoreColor2);
    }

    [Fact]
    public void DeserializeEnumClass()
    {
        var json = """
            {
              "StoreColor": "Red",
              "NullableStoreColor1": "White",
              "NullableStoreColor2": null
            }
            """;

        var enumClass = JsonConvert.DeserializeObject<EnumClass>(json, new StringEnumConverter());

        Assert.Equal(StoreColor.Red, enumClass.StoreColor);
        Assert.Equal(StoreColor.White, enumClass.NullableStoreColor1);
        Assert.Null(enumClass.NullableStoreColor2);
    }

    [Fact]
    public void DeserializeEnumClassUndefined()
    {
        var json = """
            {
              "StoreColor": 1000,
              "NullableStoreColor1": 1000,
              "NullableStoreColor2": null
            }
            """;

        var enumClass = JsonConvert.DeserializeObject<EnumClass>(json, new StringEnumConverter());

        Assert.Equal((StoreColor) 1000, enumClass.StoreColor);
        Assert.Equal((StoreColor) 1000, enumClass.NullableStoreColor1);
        Assert.Null(enumClass.NullableStoreColor2);
    }

    [Fact]
    public void CamelCaseTextFlagEnumSerialization()
    {
        var c = new EnumContainer<FlagsTestEnum>
        {
            Enum = FlagsTestEnum.First | FlagsTestEnum.Second
        };

        var json = JsonConvert.SerializeObject(c, Formatting.Indented, new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()});
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Enum": "first, second"
            }
            """,
            json);
    }

    [Fact]
    public void CamelCaseTextFlagEnumDeserialization()
    {
        var json = """
            {
              "Enum": "first, second"
            }
            """;

        var c = JsonConvert.DeserializeObject<EnumContainer<FlagsTestEnum>>(json, new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()});
        Assert.Equal(FlagsTestEnum.First | FlagsTestEnum.Second, c.Enum);
    }

    [Fact]
    public void DeserializeEmptyStringIntoNullable()
    {
        var json = """
            {
              "StoreColor": "Red",
              "NullableStoreColor1": "White",
              "NullableStoreColor2": ""
            }
            """;

        var c = JsonConvert.DeserializeObject<EnumClass>(json, new StringEnumConverter());
        Assert.Null(c.NullableStoreColor2);
    }

    [Fact]
    public void DeserializeInvalidString()
    {
        var json = "{ \"Value\" : \"Three\" }";

        var serializer = new JsonSerializer();
        serializer.Converters.Add(new StringEnumConverter());
        var reader = new JsonTextReader(new StringReader(json));
        var exception = Assert.Throws<JsonSerializationException>(() => serializer.Deserialize<Bucket>(reader));
        Assert.Equal("""Error converting value "Three" to type 'StringEnumConverterTests+MyEnum'. Path 'Value', line 1, position 19.""", exception.Message);
    }

    public class Bucket
    {
        public MyEnum Value;
    }

    public enum MyEnum
    {
        Alpha,
        Beta
    }

    [Fact]
    public void DeserializeIntegerButNotAllowed()
    {
        var json = "{ \"Value\" : 123 }";

        try
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter {AllowIntegerValues = false});
            serializer.Deserialize<Bucket>(new JsonTextReader(new StringReader(json)));
        }
        catch (JsonSerializationException exception)
        {
            Assert.Equal("Error converting value 123 to type 'StringEnumConverterTests+MyEnum'. Path 'Value', line 1, position 15.", exception.Message);
            Assert.Equal("Integer value 123 is not allowed. Path 'Value', line 1, position 15.", exception.InnerException.Message);

            return;
        }

        Assert.Fail();
    }

    [Fact]
    public void EnumMemberPlusFlags()
    {
        var lfoo =
            new List<Foo>
            {
                Foo.Bat | Foo.SerializeAsBaz,
                Foo.FooBar,
                Foo.Bat,
                Foo.SerializeAsBaz,
                Foo.FooBar | Foo.SerializeAsBaz,
                (Foo) int.MaxValue
            };

        var json1 = JsonConvert.SerializeObject(lfoo, Formatting.Indented, new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()});

        XUnitAssert.AreEqualNormalized(
            """
            [
              "Bat, baz",
              "foo_bar",
              "Bat",
              "baz",
              "foo_bar, baz",
              2147483647
            ]
            """, json1);

        var foos = JsonConvert.DeserializeObject<List<Foo>>(json1);

        Assert.Equal(6, foos.Count);
        Assert.Equal(Foo.Bat | Foo.SerializeAsBaz, foos[0]);
        Assert.Equal(Foo.FooBar, foos[1]);
        Assert.Equal(Foo.Bat, foos[2]);
        Assert.Equal(Foo.SerializeAsBaz, foos[3]);
        Assert.Equal(Foo.FooBar | Foo.SerializeAsBaz, foos[4]);
        Assert.Equal((Foo) int.MaxValue, foos[5]);

        var lbar = new List<Bar> {Bar.FooBar, Bar.Bat, Bar.SerializeAsBaz};

        var json2 = JsonConvert.SerializeObject(lbar, Formatting.Indented, new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()});

        XUnitAssert.AreEqualNormalized(
            """
            [
              "foo_bar",
              "Bat",
              "baz"
            ]
            """, json2);

        var bars = JsonConvert.DeserializeObject<List<Bar>>(json2);

        Assert.Equal(3, bars.Count);
        Assert.Equal(Bar.FooBar, bars[0]);
        Assert.Equal(Bar.Bat, bars[1]);
        Assert.Equal(Bar.SerializeAsBaz, bars[2]);
    }

    [Fact]
    public void DuplicateNameEnumTest()
    {
        var converters = new StringEnumConverter();
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DuplicateNameEnum>("'foo_bar'", converters));
        Assert.Equal("""Error converting value "foo_bar" to type 'DuplicateNameEnum'. Path '', line 1, position 9.""", exception.Message);
    }

    // Define other methods and classes here
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    enum Foo
    {
        [EnumMember(Value = "foo_bar")] FooBar = 0x01,
        Bat = 0x02,

        [EnumMember(Value = "baz")] SerializeAsBaz = 0x4
    }

    [JsonConverter(typeof(StringEnumConverter))]
    enum Bar
    {
        [EnumMember(Value = "foo_bar")] FooBar,
        Bat,

        [EnumMember(Value = "baz")] SerializeAsBaz
    }

    [Fact]
    public void DataContractSerializerDuplicateNameEnumTest()
    {
        var stream = new MemoryStream();
        var serializer = new DataContractSerializer(typeof(DuplicateEnumNameTestClass));

        var target = new DuplicateEnumNameTestClass
        {
            Value = DuplicateNameEnum.foo_bar,
            Value2 = DuplicateNameEnum2.foo_bar_NOT_USED
        };
        var exception = Assert.Throws<InvalidDataContractException>(() => serializer.WriteObject(stream, target));
        Assert.Equal("Type 'DuplicateNameEnum' contains two members 'foo_bar' 'and 'FooBar' with the same name 'foo_bar'. Multiple members with the same name in one type are not supported. Consider changing one of the member names using EnumMemberAttribute attribute.", exception.Message);
    }

    [Fact]
    public void EnumMemberWithNumbers()
    {
        var converter = new StringEnumConverter();

        var e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"1\"", converter);

        Assert.Equal(NumberNamesEnum.second, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"2\"", converter);

        Assert.Equal(NumberNamesEnum.first, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"3\"", converter);

        Assert.Equal(NumberNamesEnum.third, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"-4\"", converter);

        Assert.Equal(NumberNamesEnum.fourth, e);
    }

    [Fact]
    public void EnumMemberWithNumbers_NoIntegerValues()
    {
        var converter = new StringEnumConverter {AllowIntegerValues = false};

        var e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"1\"", converter);

        Assert.Equal(NumberNamesEnum.second, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"2\"", converter);

        Assert.Equal(NumberNamesEnum.first, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"3\"", converter);

        Assert.Equal(NumberNamesEnum.third, e);

        e = JsonConvert.DeserializeObject<NumberNamesEnum>("\"-4\"", converter);

        Assert.Equal(NumberNamesEnum.fourth, e);
    }

    [Fact]
    public void AllowIntegerValueAndStringNumber()
    {
        var converter = new StringEnumConverter
        {
            AllowIntegerValues = false
        };
        var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StoreColor>("\"1\"", converter));

        Assert.Equal("Integer string '1' is not allowed.", ex.InnerException.Message);
    }

    [Fact]
    public void AllowIntegerValueAndNegativeStringNumber()
    {
        var converter = new StringEnumConverter
        {
            AllowIntegerValues = false
        };
        var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StoreColor>("\"-1\"", converter));

        Assert.Equal("Integer string '-1' is not allowed.", ex.InnerException.Message);
    }

    [Fact]
    public void AllowIntegerValueAndPositiveStringNumber()
    {
        var converter = new StringEnumConverter
        {
            AllowIntegerValues = false
        };
        var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StoreColor>("\"+1\"", converter));

        Assert.Equal("Integer string '+1' is not allowed.", ex.InnerException.Message);
    }

    [Fact]
    public void AllowIntegerValueAndDash()
    {
        var converter = new StringEnumConverter
        {
            AllowIntegerValues = false
        };
        var ex = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<StoreColor>("\"-\"", converter));

        Assert.Equal("Requested value '-' was not found.", ex.InnerException.Message);
    }

    [Fact]
    public void AllowIntegerValueAndNonNamedValue()
    {
        var converter = new StringEnumConverter {AllowIntegerValues = false};
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject((StoreColor) 999, converter));
        Assert.Equal("Integer value 999 is not allowed. Path ''.", exception.Message);
    }

    public enum EnumWithDifferentCases
    {
        M,
        m
    }

    [Fact]
    public void SerializeEnumWithDifferentCases()
    {
        var json = JsonConvert.SerializeObject(EnumWithDifferentCases.M, new StringEnumConverter());

        Assert.Equal(
            """
            "M"
            """,
            json);

        json = JsonConvert.SerializeObject(EnumWithDifferentCases.m, new StringEnumConverter());

        Assert.Equal(
            """
            "m"
            """,
            json);
    }

    [Fact]
    public void DeserializeEnumWithDifferentCases()
    {
        var e = JsonConvert.DeserializeObject<EnumWithDifferentCases>(
            """
            "M"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumWithDifferentCases.M, e);

        e = JsonConvert.DeserializeObject<EnumWithDifferentCases>(
            """
            "m"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumWithDifferentCases.m, e);
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnumMemberDoesNotMatchName
    {
        [EnumMember(Value = "first_value")] First
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive_ByEnumMemberValue_UpperCase()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberDoesNotMatchName>(
            """
            "FIRST_VALUE"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberDoesNotMatchName.First, e);
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive_ByEnumMemberValue_MixedCase()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberDoesNotMatchName>(
            """
            "First_Value"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberDoesNotMatchName.First, e);
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive_ByName_LowerCase()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberDoesNotMatchName>(
            """
            "first"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberDoesNotMatchName.First, e);
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive_ByName_UpperCase()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberDoesNotMatchName>(
            """
            "FIRST"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberDoesNotMatchName.First, e);
    }

    [Fact]
    public void DeserializeEnumCaseInsensitive_FromAttribute()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberDoesNotMatchName>(
            """
            "FIRST_VALUE"
            """);
        Assert.Equal(EnumMemberDoesNotMatchName.First, e);
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum EnumMemberWithDiffrentCases
    {
        [EnumMember(Value = "first_value")] First,
        [EnumMember(Value = "second_value")] first
    }

    [Fact]
    public void DeserializeEnumMemberWithDifferentCasing_ByEnumMemberValue_First()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberWithDiffrentCases>(
            """
            "first_value"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberWithDiffrentCases.First, e);
    }

    [Fact]
    public void DeserializeEnumMemberWithDifferentCasing_ByEnumMemberValue_Second()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberWithDiffrentCases>(
            """
            "second_value"
            """,
            new StringEnumConverter());
        Assert.Equal(EnumMemberWithDiffrentCases.first, e);
    }

    [DataContract(Name = "DateFormats")]
    public enum EnumMemberWithDifferentCases
    {
        [EnumMember(Value = "M")] Month,
        [EnumMember(Value = "m")] Minute
    }

    [Fact]
    public void SerializeEnumMemberWithDifferentCases()
    {
        var json = JsonConvert.SerializeObject(EnumMemberWithDifferentCases.Month, new StringEnumConverter());

        Assert.Equal(
            """
            "M"
            """,
            json);

        json = JsonConvert.SerializeObject(EnumMemberWithDifferentCases.Minute, new StringEnumConverter());

        Assert.Equal(
            """
            "m"
            """,
            json);
    }

    [Fact]
    public void DeserializeEnumMemberWithDifferentCases()
    {
        var e = JsonConvert.DeserializeObject<EnumMemberWithDifferentCases>(
            """
            "M"
            """,
            new StringEnumConverter());

        Assert.Equal(EnumMemberWithDifferentCases.Month, e);

        e = JsonConvert.DeserializeObject<EnumMemberWithDifferentCases>(
            """
            "m"
            """,
            new StringEnumConverter());

        Assert.Equal(EnumMemberWithDifferentCases.Minute, e);
    }
}

[DataContract]
public class DuplicateEnumNameTestClass
{
    [DataMember] public DuplicateNameEnum Value { get; set; }

    [DataMember] public DuplicateNameEnum2 Value2 { get; set; }
}

[DataContract]
public enum NumberNamesEnum
{
    [EnumMember(Value = "2")] first,
    [EnumMember(Value = "1")] second,
    [EnumMember(Value = "3")] third,
    [EnumMember(Value = "-4")] fourth
}

[DataContract]
public enum DuplicateNameEnum
{
    [EnumMember] first = 0,

    [EnumMember] foo_bar = 1,

    [EnumMember(Value = "foo_bar")] FooBar = 2,

    [EnumMember] foo_bar_NOT_USED = 3
}

[DataContract]
public enum DuplicateNameEnum2
{
    [EnumMember] first = 0,

    [EnumMember(Value = "foo_bar")] FooBar = 1,

    [EnumMember] foo_bar = 2,

    [EnumMember(Value = "TEST")] foo_bar_NOT_USED = 3
}