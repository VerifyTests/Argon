// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class CamelCaseNamingStrategyTests : TestFixtureBase
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
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = contractResolver
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Name!",
              "birthDate": "2000-11-20T23:55:44Z",
              "lastModified": "2000-11-20T23:55:44Z"
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
            NamingStrategy = new CamelCaseNamingStrategy
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
        var p = o.Property("theField");

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
            Sizes = ["Small", "Medium", "Large"]
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        var json =
            JsonConvert.SerializeObject(
                product,
                Formatting.Indented,
                new JsonSerializerSettings {ContractResolver = contractResolver}
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
              "expiryDate": "2010-12-20T18:01:00Z",
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
    public void DynamicCamelCasePropertyNames()
    {
        dynamic o = new SnakeCaseNamingStrategyTests.TestDynamicObject();
        o.Text = "Text!";
        o.Integer = int.MaxValue;

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
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
              "childObject": null
            }
            """,
            json);
    }

    [Fact]
    public void DictionaryCamelCasePropertyNames_Disabled()
    {
        var values = new Dictionary<string, string>
        {
            {"First", "Value1!"},
            {"Second", "Value2!"}
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
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
    public void DictionaryCamelCasePropertyNames_Enabled()
    {
        var values = new Dictionary<string, string>
        {
            {"First", "Value1!"},
            {"Second", "Value2!"}
        };

        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
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

    [Fact]
    public void ToCamelCaseTest()
    {
        Assert.Equal("urlValue", CamelCaseNamingStrategy.ToCamelCase("URLValue"));
        Assert.Equal("url", CamelCaseNamingStrategy.ToCamelCase("URL"));
        Assert.Equal("id", CamelCaseNamingStrategy.ToCamelCase("ID"));
        Assert.Equal("i", CamelCaseNamingStrategy.ToCamelCase("I"));
        Assert.Equal("", CamelCaseNamingStrategy.ToCamelCase(""));
        Assert.Null(CamelCaseNamingStrategy.ToCamelCase(null));
        Assert.Equal("person", CamelCaseNamingStrategy.ToCamelCase("Person"));
        Assert.Equal("iPhone", CamelCaseNamingStrategy.ToCamelCase("iPhone"));
        Assert.Equal("iPhone", CamelCaseNamingStrategy.ToCamelCase("IPhone"));
        Assert.Equal("i Phone", CamelCaseNamingStrategy.ToCamelCase("I Phone"));
        Assert.Equal("i  Phone", CamelCaseNamingStrategy.ToCamelCase("I  Phone"));
        Assert.Equal(" IPhone", CamelCaseNamingStrategy.ToCamelCase(" IPhone"));
        Assert.Equal(" IPhone ", CamelCaseNamingStrategy.ToCamelCase(" IPhone "));
        Assert.Equal("isCIA", CamelCaseNamingStrategy.ToCamelCase("IsCIA"));
        Assert.Equal("vmQ", CamelCaseNamingStrategy.ToCamelCase("VmQ"));
        Assert.Equal("xml2Json", CamelCaseNamingStrategy.ToCamelCase("Xml2Json"));
        Assert.Equal("snAkEcAsE", CamelCaseNamingStrategy.ToCamelCase("SnAkEcAsE"));
        Assert.Equal("snA__kEcAsE", CamelCaseNamingStrategy.ToCamelCase("SnA__kEcAsE"));
        Assert.Equal("snA__ kEcAsE", CamelCaseNamingStrategy.ToCamelCase("SnA__ kEcAsE"));
        Assert.Equal("already_snake_case_ ", CamelCaseNamingStrategy.ToCamelCase("already_snake_case_ "));
        Assert.Equal("isJSONProperty", CamelCaseNamingStrategy.ToCamelCase("IsJSONProperty"));
        Assert.Equal("shoutinG_CASE", CamelCaseNamingStrategy.ToCamelCase("SHOUTING_CASE"));
        Assert.Equal("9999-12-31T23:59:59.9999999Z", CamelCaseNamingStrategy.ToCamelCase("9999-12-31T23:59:59.9999999Z"));
        Assert.Equal("hi!! This is text. Time to test.", CamelCaseNamingStrategy.ToCamelCase("Hi!! This is text. Time to test."));
        Assert.Equal("building", CamelCaseNamingStrategy.ToCamelCase("BUILDING"));
        Assert.Equal("building Property", CamelCaseNamingStrategy.ToCamelCase("BUILDING Property"));
        Assert.Equal("building Property", CamelCaseNamingStrategy.ToCamelCase("Building Property"));
        Assert.Equal("building PROPERTY", CamelCaseNamingStrategy.ToCamelCase("BUILDING PROPERTY"));
    }
}