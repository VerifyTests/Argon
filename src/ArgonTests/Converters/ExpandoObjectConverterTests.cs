// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class ExpandoObjectConverterTests : TestFixtureBase
{
    public class ExpandoContainer
    {
        public string Before { get; set; }
        public ExpandoObject Expando { get; set; }
        public string After { get; set; }
    }

    [Fact]
    public void SerializeExpandoObject()
    {
        var d = new ExpandoContainer
        {
            Before = "Before!",
            Expando = new(),
            After = "After!"
        };

        dynamic o = d.Expando;

        o.String = "String!";
        o.Integer = 234;
        o.Float = 1.23d;
        o.List = new List<string> {"First", "Second", "Third"};
        o.Object = new Dictionary<string, object>
        {
            {"First", 1}
        };

        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Before": "Before!",
              "Expando": {
                "String": "String!",
                "Integer": 234,
                "Float": 1.23,
                "List": [
                  "First",
                  "Second",
                  "Third"
                ],
                "Object": {
                  "First": 1
                }
              },
              "After": "After!"
            }
            """,
            json);
    }

    [Fact]
    public void SerializeNullExpandoObject()
    {
        var d = new ExpandoContainer();

        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Before": null,
              "Expando": null,
              "After": null
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeExpandoObject()
    {
        var json = """
            {
              "Before": "Before!",
              "Expando": {
                "String": "String!",
                "Integer": 234,
                "Float": 1.23,
                "List": [
                  "First",
                  "Second",
                  "Third"
                ],
                "Object": {
                  "First": 1
                }
              },
              "After": "After!"
            }
            """;

        var o = JsonConvert.DeserializeObject<ExpandoContainer>(json);

        Assert.Equal("Before!", o.Before);
        Assert.Equal("After!", o.After);
        Assert.NotNull(o.Expando);

        dynamic d = o.Expando;
        Assert.IsType<ExpandoObject>(d);

        Assert.Equal("String!", d.String);
        Assert.IsType<string>(d.String);

        Assert.Equal(234, d.Integer);
        Assert.IsType<long>(d.Integer);

        Assert.Equal(1.23, d.Float);
        Assert.IsType<double>(d.Float);

        Assert.NotNull(d.List);
        Assert.Equal(3, d.List.Count);
        Assert.IsType<List<object>>(d.List);

        Assert.Equal("First", d.List[0]);
        Assert.IsType<string>(d.List[0]);

        Assert.Equal("Second", d.List[1]);
        Assert.Equal("Third", d.List[2]);

        Assert.NotNull(d.Object);
        Assert.IsType<ExpandoObject>(d.Object);

        Assert.Equal(1, d.Object.First);
        Assert.IsType<long>(d.Object.First);
    }

    [Fact]
    public void DeserializeNullExpandoObject()
    {
        var json = """
            {
              "Before": null,
              "Expando": null,
              "After": null
            }
            """;

        var c = JsonConvert.DeserializeObject<ExpandoContainer>(json);

        Assert.Null(c.Expando);
    }
}
