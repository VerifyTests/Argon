// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class SnakeCaseNamingStrategyTests : TestFixtureBase
{
    [Fact]
    public void JsonConvertSerializerSettings()
    {
        var person = new Person
        {
            BirthDate = new(2000, 11, 20, 23, 55, 44, DateTimeKind.Utc),
            LastModified = new(2000, 11, 20, 23, 55, 44, DateTimeKind.Utc),
            Name = "Name!"
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = contractResolver
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Name!",
              "birth_date": "2000-11-20T23:55:44Z",
              "last_modified": "2000-11-20T23:55:44Z"
            }
            """,
            json);

        var deserializedPerson = JsonConvert.DeserializeObject<Person>(json, new JsonSerializerSettings
        {
            ContractResolver = contractResolver
        });

        Assert.Equal(person.BirthDate, deserializedPerson.BirthDate);
        Assert.Equal(person.LastModified, deserializedPerson.LastModified);
        Assert.Equal(person.Name, deserializedPerson.Name);

        json = JsonConvert.SerializeObject(person, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "Name!",
              "BirthDate": "2000-11-20T23:55:44Z",
              "LastModified": "2000-11-20T23:55:44Z"
            }
            """,
            json);
    }

    [Fact]
    public void JTokenWriter_OverrideSpecifiedName()
    {
        var ignoreAttributeOnClassTestClass = new JsonIgnoreAttributeOnClassTestClass
        {
            Field = int.MinValue
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy
            {
                OverrideSpecifiedNames = true
            }
        };

        var serializer = new JsonSerializer
        {
            ContractResolver = contractResolver
        };

        var writer = new JTokenWriter();

        serializer.Serialize(writer, ignoreAttributeOnClassTestClass);

        var o = (JObject) writer.Token;
        var p = o.Property("the_field");

        Assert.Equal(int.MinValue, (int) p.Value);
    }

    [Fact]
    public void BlogPostExample()
    {
        var product = new Product
        {
            ExpiryDate = new(2010, 12, 20, 18, 1, 0, DateTimeKind.Utc),
            Name = "Widget",
            Price = 9.99m,
            Sizes =
            [
                "Small",
                "Medium",
                "Large"
            ]
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        var json =
            JsonConvert.SerializeObject(
                product,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    ContractResolver = contractResolver
                }
            );

        //{
        //  "name": "Widget",
        //  "expiryDate": "\/Date(1292868060000)\/",
        //  "price": 9.99,
        //  "sizes": [
        //    "Small",
        //    "Medium",
        //    "Large"
        //  ]
        //}

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Widget",
              "expiry_date": "2010-12-20T18:01:00Z",
              "price": 9.99,
              "sizes": [
                "Small",
                "Medium",
                "Large"
              ]
            }
            """,
            json);
    }

    [Fact]
    public void DynamicSnakeCasePropertyNames()
    {
        dynamic o = new TestDynamicObject();
        o.Text = "Text!";
        o.Integer = int.MaxValue;

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy
            {
                ProcessDictionaryKeys = true
            }
        };

        string json = JsonConvert.SerializeObject(o, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "explicit": false,
              "text": "Text!",
              "integer": 2147483647,
              "int": 0,
              "child_object": null
            }
            """,
            json);
    }

    public class DynamicChildObject
    {
        public string Text { get; set; }
        public int Integer { get; set; }
    }

    public class TestDynamicObject : DynamicObject
    {
        public int Int;

        [JsonProperty]
        public bool Explicit;

        public DynamicChildObject ChildObject { get; set; }

        internal Dictionary<string, object> Members { get; } = [];

        public override IEnumerable<string> GetDynamicMemberNames() =>
            Members.Keys.Union(["Int", "ChildObject"]);

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var targetType = binder.Type;

            if (targetType == typeof(IDictionary<string, object>) ||
                targetType == typeof(IDictionary))
            {
                result = new Dictionary<string, object>(Members);
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder) =>
            Members.Remove(binder.Name);

        public override bool TryGetMember(GetMemberBinder binder, out object result) =>
            Members.TryGetValue(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Members[binder.Name] = value;
            return true;
        }
    }

    [Fact]
    public void DictionarySnakeCasePropertyNames_Disabled()
    {
        var values = new Dictionary<string, string>
        {
            {
                "First", "Value1!"
            },
            {
                "Second", "Value2!"
            }
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "First": "Value1!",
              "Second": "Value2!"
            }
            """,
            json);
    }

    [Fact]
    public void DictionarySnakeCasePropertyNames_Enabled()
    {
        var values = new Dictionary<string, string>
        {
            {
                "First", "Value1!"
            },
            {
                "Second", "Value2!"
            }
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy
            {
                ProcessDictionaryKeys = true
            }
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "first": "Value1!",
              "second": "Value2!"
            }
            """,
            json);
    }

    public class DictionaryAttributeNamingStrategyTestClass : Dictionary<string, string>;

    [Fact]
    public void JsonDictionaryAttribute_NamingStrategyType()
    {
        var c = new DictionaryAttributeNamingStrategyTestClass
        {
            ["Key1"] = "Value1!",
            ["Key2"] = "Value2!"
        };

        var json = JsonConvert.SerializeObject(
            c,
            Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(true, true)
                }
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "key1": "Value1!",
              "key2": "Value2!"
            }
            """,
            json);
    }

    [Fact]
    public void ToSnakeCaseTest()
    {
        Assert.Equal("url_value", StringUtils.ToSnakeCase("URLValue"));
        Assert.Equal("url", StringUtils.ToSnakeCase("URL"));
        Assert.Equal("id", StringUtils.ToSnakeCase("ID"));
        Assert.Equal("i", StringUtils.ToSnakeCase("I"));
        Assert.Equal("", StringUtils.ToSnakeCase(""));
        Assert.Null(StringUtils.ToSnakeCase(null));
        Assert.Equal("person", StringUtils.ToSnakeCase("Person"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase("iPhone"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase("IPhone"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase("I Phone"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase("I  Phone"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase(" IPhone"));
        Assert.Equal("i_phone", StringUtils.ToSnakeCase(" IPhone "));
        Assert.Equal("is_cia", StringUtils.ToSnakeCase("IsCIA"));
        Assert.Equal("vm_q", StringUtils.ToSnakeCase("VmQ"));
        Assert.Equal("xml2_json", StringUtils.ToSnakeCase("Xml2Json"));
        Assert.Equal("sn_ak_ec_as_e", StringUtils.ToSnakeCase("SnAkEcAsE"));
        Assert.Equal("sn_a__k_ec_as_e", StringUtils.ToSnakeCase("SnA__kEcAsE"));
        Assert.Equal("sn_a__k_ec_as_e", StringUtils.ToSnakeCase("SnA__ kEcAsE"));
        Assert.Equal("already_snake_case_", StringUtils.ToSnakeCase("already_snake_case_ "));
        Assert.Equal("is_json_property", StringUtils.ToSnakeCase("IsJSONProperty"));
        Assert.Equal("shouting_case", StringUtils.ToSnakeCase("SHOUTING_CASE"));
        Assert.Equal("9999-12-31_t23:59:59.9999999_z", StringUtils.ToSnakeCase("9999-12-31T23:59:59.9999999Z"));
        Assert.Equal("hi!!_this_is_text._time_to_test.", StringUtils.ToSnakeCase("Hi!! This is text. Time to test."));
    }
}