// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class KebabCaseNamingStrategyTests : TestFixtureBase
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
            NamingStrategy = new KebabCaseNamingStrategy()
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = contractResolver
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Name!",
              "birth-date": "2000-11-20T23:55:44Z",
              "last-modified": "2000-11-20T23:55:44Z"
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
            NamingStrategy = new KebabCaseNamingStrategy
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
        var p = o.Property("the-field");

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
            NamingStrategy = new KebabCaseNamingStrategy()
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
              "expiry-date": "2010-12-20T18:01:00Z",
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
    public void DynamicKebabCasePropertyNames()
    {
        dynamic o = new SnakeCaseNamingStrategyTests.TestDynamicObject();
        o.Text = "Text!";
        o.Integer = int.MaxValue;

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new KebabCaseNamingStrategy
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
              "child-object": null
            }
            """,
            json);
    }

    [Fact]
    public void DictionaryKebabCasePropertyNames_Disabled()
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
            NamingStrategy = new KebabCaseNamingStrategy()
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
    public void DictionaryKebabCasePropertyNames_Enabled()
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
            NamingStrategy = new KebabCaseNamingStrategy
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
            Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new KebabCaseNamingStrategy(true, true)
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
    public void ToKebabCaseTest()
    {
        Assert.Equal("url-value", StringUtils.ToKebabCase("URLValue"));
        Assert.Equal("url", StringUtils.ToKebabCase("URL"));
        Assert.Equal("id", StringUtils.ToKebabCase("ID"));
        Assert.Equal("i", StringUtils.ToKebabCase("I"));
        Assert.Equal("", StringUtils.ToKebabCase(""));
        Assert.Null(StringUtils.ToKebabCase(null));
        Assert.Equal("person", StringUtils.ToKebabCase("Person"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase("iPhone"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase("IPhone"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase("I Phone"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase("I  Phone"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase(" IPhone"));
        Assert.Equal("i-phone", StringUtils.ToKebabCase(" IPhone "));
        Assert.Equal("is-cia", StringUtils.ToKebabCase("IsCIA"));
        Assert.Equal("vm-q", StringUtils.ToKebabCase("VmQ"));
        Assert.Equal("xml2-json", StringUtils.ToKebabCase("Xml2Json"));
        Assert.Equal("ke-ba-bc-as-e", StringUtils.ToKebabCase("KeBaBcAsE"));
        Assert.Equal("ke-b--a-bc-as-e", StringUtils.ToKebabCase("KeB--aBcAsE"));
        Assert.Equal("ke-b--a-bc-as-e", StringUtils.ToKebabCase("KeB-- aBcAsE"));
        Assert.Equal("already-kebab-case-", StringUtils.ToKebabCase("already-kebab-case- "));
        Assert.Equal("is-json-property", StringUtils.ToKebabCase("IsJSONProperty"));
        Assert.Equal("shouting-case", StringUtils.ToKebabCase("SHOUTING-CASE"));
        Assert.Equal("9999-12-31-t23:59:59.9999999-z", StringUtils.ToKebabCase("9999-12-31T23:59:59.9999999Z"));
        Assert.Equal("hi!!-this-is-text.-time-to-test.", StringUtils.ToKebabCase("Hi!! This is text. Time to test."));
    }
}