// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class CamelCasePropertyNamesContractResolverTests : TestFixtureBase
{
    [Fact]
    public void EnsureContractsShared()
    {
        var resolver1 = new CamelCasePropertyNamesContractResolver();
        var contract1 = (JsonObjectContract) resolver1.ResolveContract(typeof(CamelCasePropertyNamesContractResolverTests));

        var resolver2 = new CamelCasePropertyNamesContractResolver();
        var contract2 = (JsonObjectContract) resolver2.ResolveContract(typeof(CamelCasePropertyNamesContractResolverTests));

        Assert.True(ReferenceEquals(contract1, contract2));

        var nt1 = resolver1.GetNameTable();
        var nt2 = resolver2.GetNameTable();

        Assert.True(ReferenceEquals(nt1, nt2));
    }

    [Fact]
    public void JsonConvertSerializerSettings()
    {
        var person = new Person
        {
            BirthDate = new(2000, 11, 20, 23, 55, 44, DateTimeKind.Utc),
            LastModified = new(2000, 11, 20, 23, 55, 44, DateTimeKind.Utc),
            Name = "Name!"
        };

        var json = JsonConvert.SerializeObject(person, Formatting.Indented, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
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
            ContractResolver = new CamelCasePropertyNamesContractResolver()
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
    public void JTokenWriter()
    {
        var ignoreAttributeOnClassTestClass = new JsonIgnoreAttributeOnClassTestClass
        {
            Field = int.MinValue
        };

        var serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var writer = new JTokenWriter();

        serializer.Serialize(writer, ignoreAttributeOnClassTestClass);

        var o = (JObject) writer.Token;
        var p = o.Property("theField");

        Assert.Equal(int.MinValue, (int) p.Value);

        // ReSharper disable once UnusedVariable
        var json = o.ToString();
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

        var json =
            JsonConvert.SerializeObject(
                product,
                Formatting.Indented,
                new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()}
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

        string json = JsonConvert.SerializeObject(o, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
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
    public void DictionaryCamelCasePropertyNames()
    {
        var values = new Dictionary<string, string>
        {
            {"First", "Value1!"},
            {"Second", "Value2!"}
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
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
}