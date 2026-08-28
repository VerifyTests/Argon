// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class Issue0198 : TestFixtureBase
{
    [Fact]
    public void Test_List()
    {
#pragma warning disable IDE0028
        IEnumerable<TestClass1> objects = new List<TestClass1>
#pragma warning restore IDE0028
        {
            new()
            {
                Prop1 = new HashSet<TestClass2>
                {
                    new()
                    {
                        MyProperty1 = "Test1",
                        MyProperty2 = "Test2",
                    }
                },
#pragma warning disable IDE0028
                Prop2 = new List<string>
#pragma warning restore IDE0028
                {
                    "Test1",
                    "Test1"
                },
                Prop3 = new HashSet<TestClass2>
                {
                    new()
                    {
                        MyProperty1 = "Test1",
                        MyProperty2 = "Test2",
                    }
                },
            }
        };

        var serializedData = JsonConvert.SerializeObject(
            objects,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            });

        var a = JsonConvert.DeserializeObject<IEnumerable<TestClass1>>(
            serializedData,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

        var o = a.First();

        Assert.Single(o.Prop1);
        Assert.Single(o.Prop2);
        Assert.Single(o.Prop3);
    }

    [Fact]
    public void Test_Collection()
    {
        var c = new TestClass3
        {
            Prop1 = new Dictionary<string, string>
            {
                ["key"] = "value"
            }
        };

        var serializedData = JsonConvert.SerializeObject(
            c,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            });

        var a = JsonConvert.DeserializeObject<TestClass3>(serializedData, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        Assert.Single(a.Prop1);
    }

    class TestClass1 : AbstractClass
    {
        public ICollection<TestClass2> Prop1 { get; set; } = new HashSet<TestClass2>();
        public ICollection<string> Prop2 { get; set; } = new HashSet<string>();
    }

    class TestClass2
    {
        public string MyProperty1 { get; set; }
        public string MyProperty2 { get; set; }
    }

    abstract class AbstractClass
    {
        public ICollection<TestClass2> Prop3 { get; set; } = [];
    }

    class TestClass3
    {
        public IDictionary<string, string> Prop1 { get; set; } = new ModelStateDictionary<string>();
    }
}