// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.ComponentModel;
using TestObjects;
using Formatting = Argon.Formatting;
#if !NET6_0_OR_GREATER
using System.Runtime.Serialization.Json;
#endif

public class DefaultValueHandlingTests : TestFixtureBase
{
    // ReflectionUtils.GetDefaultValue(typeof(char)) returned a boxed int 0 instead of '\0',
    // so DefaultValueHandling.Ignore never omitted a default char member.
    [Fact]
    public void IgnoreOmitsDefaultChar()
    {
        var json = JsonConvert.SerializeObject(
            new HasChar(),
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});

        Assert.Equal("{}", json);
    }

    [Fact]
    public void IgnoreKeepsNonDefaultChar()
    {
        var json = JsonConvert.SerializeObject(
            new HasChar {C = 'a'},
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});

        Assert.Equal("""{"C":"a"}""", json);
    }

    public class HasChar
    {
        public char C { get; set; }
    }

    class DefaultValueWithConstructorAndRename(string text = DefaultValueWithConstructorAndRename.DefaultText)
    {
        public const string DefaultText = "...";

        [DefaultValue(DefaultText)] [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public readonly string Text = text;
    }

    [Fact]
    public void DefaultValueWithConstructorAndRenameTest()
    {
        var myObject = JsonConvert.DeserializeObject<DefaultValueWithConstructorAndRename>("{}");
        Assert.Equal(DefaultValueWithConstructorAndRename.DefaultText, myObject.Text);
    }

    class DefaultValueWithConstructor([JsonProperty(PropertyName = "myText")] string text = DefaultValueWithConstructor.DefaultText)
    {
        public const string DefaultText = "...";

        [DefaultValue(DefaultText)] [JsonProperty(PropertyName = "myText", DefaultValueHandling = DefaultValueHandling.Populate)]
        public readonly string Text = text;
    }

    [Fact]
    public void DefaultValueWithConstructorTest()
    {
        var myObject = JsonConvert.DeserializeObject<DefaultValueWithConstructor>("{}");
        Assert.Equal(DefaultValueWithConstructor.DefaultText, myObject.Text);
    }

    public class MyClass
    {
        [JsonIgnore] public MyEnum Status { get; set; }

        public string Data
        {
            get;
            set
            {
                field = value;
                if (field != null && field.StartsWith("Other"))
                {
                    Status = MyEnum.Other;
                }
            }
        }
    }

    public enum MyEnum
    {
        Default = 0,
        Other
    }

    [Fact]
    public void PopulateWithJsonIgnoreAttribute()
    {
        var json = "{\"Data\":\"Other with some more text\"}";

        var result = JsonConvert.DeserializeObject<MyClass>(json, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate});

        Assert.Equal(MyEnum.Other, result.Status);
    }

    public class WithEnumDefault
    {
        public EnumWithZero WithZero { get; set; }
        public enum EnumWithZero
        {
            Default = 0,
            Other
        }
        public EnumWithNegative WithNegative { get; set; }

        public enum EnumWithNegative
        {
            Other = -1,
            Default
        }
    }

    [Fact]
    public void EnumDefault()
    {
        var json = "{\"WithZero\":0,\"WithNegative\":0}";

        var target = new WithEnumDefault();
        var result = JsonConvert.SerializeObject(target);

        Assert.Equal(json, result);
    }

    [Fact]
    public void Include()
    {
        var invoice = new Invoice
        {
            Company = "Acme Ltd.",
            Amount = 50.0m,
            Paid = false,
            FollowUpDays = 30,
            FollowUpEmailAddress = string.Empty,
            PaidDate = null
        };

        var included = JsonConvert.SerializeObject(invoice,
            Formatting.Indented,
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Include});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Company": "Acme Ltd.",
              "Amount": 50.0,
              "Paid": false,
              "PaidDate": null,
              "FollowUpDays": 30,
              "FollowUpEmailAddress": ""
            }
            """,
            included);
    }

    [Fact]
    public void SerializeInvoice()
    {
        var invoice = new Invoice
        {
            Company = "Acme Ltd.",
            Amount = 50.0m,
            Paid = false,
            FollowUpDays = 30,
            FollowUpEmailAddress = string.Empty,
            PaidDate = null
        };

        var included = JsonConvert.SerializeObject(invoice,
            Formatting.Indented,
            new JsonSerializerSettings());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Company": "Acme Ltd.",
              "Amount": 50.0,
              "Paid": false,
              "PaidDate": null,
              "FollowUpDays": 30,
              "FollowUpEmailAddress": ""
            }
            """,
            included);

        var ignored = JsonConvert.SerializeObject(invoice,
            Formatting.Indented,
            new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Company": "Acme Ltd.",
              "Amount": 50.0
            }
            """,
            ignored);
    }

    [Fact]
    public void SerializeDefaultValueAttributeTest()
    {
        var json = JsonConvert.SerializeObject(new DefaultValueAttributeTestClass(),
            Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
        Assert.Equal("""{"TestField1":0,"TestProperty1":null}""", json);

        json = JsonConvert.SerializeObject(new DefaultValueAttributeTestClass {TestField1 = int.MinValue, TestProperty1 = "NotDefault"},
            Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
        Assert.Equal("""{"TestField1":-2147483648,"TestProperty1":"NotDefault"}""", json);

        json = JsonConvert.SerializeObject(new DefaultValueAttributeTestClass {TestField1 = 21, TestProperty1 = "NotDefault"},
            Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
        Assert.Equal("""{"TestProperty1":"NotDefault"}""", json);

        json = JsonConvert.SerializeObject(new DefaultValueAttributeTestClass {TestField1 = 21, TestProperty1 = "TestProperty1Value"},
            Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
        Assert.Equal("{}", json);
    }

    [Fact]
    public void DeserializeDefaultValueAttributeTest()
    {
        var json = "{}";

        var c = JsonConvert.DeserializeObject<DefaultValueAttributeTestClass>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate
        });
        Assert.Equal("TestProperty1Value", c.TestProperty1);

        c = JsonConvert.DeserializeObject<DefaultValueAttributeTestClass>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        });
        Assert.Equal("TestProperty1Value", c.TestProperty1);
    }

    public class DefaultHandler
    {
        [DefaultValue(-1)] public int field1;

        [DefaultValue("default")] public string field2;
    }

    [Fact]
    public void DeserializeIgnoreAndPopulate()
    {
        var c1 = JsonConvert.DeserializeObject<DefaultHandler>("{}", new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        });
        Assert.Equal(-1, c1.field1);
        Assert.Equal("default", c1.field2);

        var c2 = JsonConvert.DeserializeObject<DefaultHandler>("{'field1':-1,'field2':'default'}", new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        });
        Assert.Equal(-1, c2.field1);
        Assert.Equal("default", c2.field2);
    }

    [JsonObject]
    public class NetworkUser
    {
        [JsonProperty(PropertyName = "userId")]
        [DefaultValue(-1)]
        public long GlobalId { get; set; } = -1;

        [JsonProperty(PropertyName = "age")]
        [DefaultValue(0)]
        public int Age { get; set; } = 0;

        [JsonProperty(PropertyName = "amount")]
        [DefaultValue(0.0)]
        public decimal Amount { get; set; } = 0.0m;

        [JsonProperty(PropertyName = "floatUserId")]
        [DefaultValue(-1.0d)]
        public float FloatGlobalId { get; set; } = -1.0f;

        [JsonProperty(PropertyName = "firstName")]
        public string Firstname { get; set; }

        [JsonProperty(PropertyName = "lastName")]
        public string Lastname { get; set; }
    }

    [Fact]
    public void IgnoreNumberTypeDifferencesWithDefaultValue()
    {
        var user = new NetworkUser
        {
            Firstname = "blub"
        };

        var json = JsonConvert.SerializeObject(user, Formatting.None, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore});

        Assert.Equal("""{"firstName":"blub"}""", json);
    }

    [Fact]
    public void ApproxEquals()
    {
        Assert.True(MathUtils.ApproxEquals(0.0, 0.0));
        Assert.True(MathUtils.ApproxEquals(1000.0, 1000.0000000000001));

        Assert.False(MathUtils.ApproxEquals(1000.0, 1000.000000000001));
        Assert.False(MathUtils.ApproxEquals(0.0, 0.00001));
    }

    [Fact]
    public void EmitDefaultValueTest()
    {
        var c = new EmitDefaultValueClass();

#if !NET6_0_OR_GREATER
        var serializer = new DataContractJsonSerializer(typeof(EmitDefaultValueClass));

        var ms = new MemoryStream();
        serializer.WriteObject(ms, c);

        Assert.Equal("{}", Encoding.UTF8.GetString(ms.ToArray()));
#endif

        var json = JsonConvert.SerializeObject(c);

        Assert.Equal("{}", json);
    }

    [Fact]
    public void DefaultValueHandlingPropertyTest()
    {
        var c = new DefaultValueHandlingPropertyClass();

        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "IntInclude": 0,
              "IntDefault": 0
            }
            """,
            json);

        json = JsonConvert.SerializeObject(c, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "IntInclude": 0
            }
            """,
            json);

        json = JsonConvert.SerializeObject(c, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Include
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "IntInclude": 0,
              "IntDefault": 0
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeWithIgnore()
    {
        var json = "{'Value':null,'IntValue1':1,'IntValue2':0,'IntValue3':null}";

        var o = JsonConvert.DeserializeObject<DefaultValueHandlingDeserializeHolder>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        });

        Assert.Equal(int.MaxValue, o.IntValue1);
        Assert.Equal(int.MinValue, o.IntValue2);
        Assert.Equal(int.MaxValue, o.IntValue3);
        Assert.Equal("Derp!", o.ClassValue.Derp);
    }

    [Fact]
    public void DeserializeWithPopulate()
    {
        var json = "{}";

        var o = JsonConvert.DeserializeObject<DefaultValueHandlingDeserializePopulate>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate
        });

        Assert.Equal(1, o.IntValue1);
        Assert.Equal(0, o.IntValue2);
        Assert.Null(o.ClassValue);
    }

    [Fact]
    public void EmitDefaultValueIgnoreAndPopulate()
    {
        var str = "{}";
        var obj = JsonConvert.DeserializeObject<TestClass>(str, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        });

        Assert.Equal("fff", obj.Field1);
    }

    [Fact]
    public void PopulateTest()
    {
        var test = JsonConvert.DeserializeObject<PopulateWithNullJsonTest>("{\"IntValue\":null}");
        Assert.Equal(0, test.IntValue);
    }

    public class PopulateWithNullJsonTest
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(6)]
        public int IntValue { get; set; }
    }

    public sealed class FieldExportFormat
    {
        string format;
        ExportFormat? exportFormat;

        [JsonProperty]
        public ExportFormat? ExportFormat
        {
            get => exportFormat;
            set
            {
                if (!value.HasValue)
                {
                    throw new ArgumentNullException("ExportFormat");
                }

                exportFormat = value;
                format = null;
            }
        }

        [JsonProperty]
        public string Format
        {
            get => format;
            set
            {
                format = value;
                exportFormat = null;
            }
        }

        public FieldExportFormat(string format) =>
            Format = format;

        public FieldExportFormat(ExportFormat exportFormat) =>
            ExportFormat = exportFormat;

        [Argon.JsonConstructor]
        FieldExportFormat(string format, ExportFormat? exportFormat)
        {
            if (exportFormat.HasValue)
            {
                ExportFormat = exportFormat;
            }
            else
            {
                Format = format;
            }
        }
    }

    [Fact]
    public void DontSetPropertiesDefaultValueUsedInConstructor()
    {
        var json = """{"ExportFormat":0}""";

        var o = JsonConvert.DeserializeObject<FieldExportFormat>(json, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate
        });

        Assert.Equal(ExportFormat.Default, o.ExportFormat);
        Assert.Null(o.Format);
    }

    [DataContract]
    public class TestClass
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DataMember(EmitDefaultValue = false)]
        [DefaultValue("fff")]
        public string Field1 { set; get; }
    }

    public class DefaultValueHandlingDeserialize
    {
        public string Derp { get; set; }
    }

    public class DefaultValueHandlingDeserializeHolder
    {
        [DefaultValue(1)] public int IntValue1 { get; set; } = int.MaxValue;

        public int IntValue2 { get; set; } = int.MinValue;

        [DefaultValue(null)] public int IntValue3 { get; set; } = int.MaxValue;

        public DefaultValueHandlingDeserialize ClassValue { get; set; } = new()
        {
            Derp = "Derp!"
        };
    }

    public class DefaultValueHandlingDeserializePopulate
    {
        [DefaultValue(1)] public int IntValue1 { get; set; } = int.MaxValue;

        public int IntValue2 { get; set; } = int.MinValue;

        public DefaultValueHandlingDeserialize ClassValue { get; set; } = new()
        {
            Derp = "Derp!"
        };
    }

    public struct DefaultStruct
    {
        public string Default { get; set; }
    }

    public class DefaultValueHandlingPropertyClass
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int IntIgnore { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int IntInclude { get; set; }

        [JsonProperty] public int IntDefault { get; set; }
    }

    [DataContract]
    public class EmitDefaultValueClass
    {
        [DataMember(EmitDefaultValue = false)] public Guid Guid { get; set; }

        [DataMember(EmitDefaultValue = false)] public TimeSpan TimeSpan { get; set; }

        [DataMember(EmitDefaultValue = false)] public DateTime DateTime { get; set; }

        [DataMember(EmitDefaultValue = false)] public DateTimeOffset DateTimeOffset { get; set; }

        [DataMember(EmitDefaultValue = false)] public decimal Decimal { get; set; }

        [DataMember(EmitDefaultValue = false)] public int Integer { get; set; }

        [DataMember(EmitDefaultValue = false)] public double Double { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool Boolean { get; set; }

        [DataMember(EmitDefaultValue = false)] public DefaultStruct Struct { get; set; }

        [DataMember(EmitDefaultValue = false)] public StringComparison Enum { get; set; }

        [DataMember(EmitDefaultValue = false)] public Guid? NullableGuid { get; set; }

        [DataMember(EmitDefaultValue = false)] public TimeSpan? NullableTimeSpan { get; set; }

        [DataMember(EmitDefaultValue = false)] public DateTime? NullableDateTime { get; set; }

        [DataMember(EmitDefaultValue = false)] public DateTimeOffset? NullableDateTimeOffset { get; set; }

        [DataMember(EmitDefaultValue = false)] public decimal? NullableDecimal { get; set; }

        [DataMember(EmitDefaultValue = false)] public int? NullableInteger { get; set; }

        [DataMember(EmitDefaultValue = false)] public double? NullableDouble { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool? NullableBoolean { get; set; }

        [DataMember(EmitDefaultValue = false)] public DefaultStruct? NullableStruct { get; set; }

        [DataMember(EmitDefaultValue = false)] public StringComparison? NullableEnum { get; set; }

        [DataMember(EmitDefaultValue = false)] public object Object { get; set; }
    }

    public enum ExportFormat
    {
        Default = 0,
        Currency,
        Integer
    }
}