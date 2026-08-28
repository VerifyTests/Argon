// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class RegexConverterTests : TestFixtureBase
{
    public class RegexTestClass
    {
        public Regex Regex { get; set; }
    }

    [Fact]
    public void SerializeToText()
    {
        var regex = new Regex("abc", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var json = JsonConvert.SerializeObject(regex, Formatting.Indented, new RegexConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Pattern": "abc",
              "Options": 513
            }
            """,
            json);
    }

    [Fact]
    public void SerializeCamelCaseAndStringEnums()
    {
        var regex = new Regex("abc", RegexOptions.IgnoreCase);

        var json = JsonConvert.SerializeObject(
            regex,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                Converters =
                {
                    new RegexConverter(),
                    new StringEnumConverter
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                },
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "pattern": "abc",
              "options": "ignoreCase"
            }
            """,
            json);
    }

    [Fact]
    public void DeserializeCamelCaseAndStringEnums()
    {
        var json = """
            {
              "pattern": "abc",
              "options": "ignoreCase"
            }
            """;

        var regex = JsonConvert.DeserializeObject<Regex>(json, new JsonSerializerSettings
        {
            Converters = {new RegexConverter()}
        });

        Assert.Equal("abc", regex.ToString());
        Assert.Equal(RegexOptions.IgnoreCase, regex.Options);
    }

    [Fact]
    public void DeserializeISerializeRegexJson()
    {
        var json = """
            {
                "Regex": {
                  "pattern": "(hi)",
                  "options": 5,
                  "matchTimeout": -10000
                }
            }
            """;

        var r = JsonConvert.DeserializeObject<RegexTestClass>(json);

        Assert.Equal("(hi)", r.Regex.ToString());
        Assert.Equal(RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, r.Regex.Options);
    }

    [Fact]
    public void DeserializeStringRegex()
    {
        var json = """
            {
              "Regex": "\/abc\/"
            }
            """;

        var c = JsonConvert.DeserializeObject<RegexTestClass>(json, new JsonSerializerSettings
        {
            Converters = {new RegexConverter()}
        });

        Assert.Equal("abc", c.Regex.ToString());
        Assert.Equal(RegexOptions.None, c.Regex.Options);
    }

    [Fact]
    public void DeserializeStringRegex_NoStartSlash_Error()
    {
        var json = """
            {
              "Regex": "abc\/"
            }
            """;

        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new RegexConverter()
            }
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RegexTestClass>(json, settings));
        Assert.Equal("Regex pattern must be enclosed by slashes. Path 'Regex', line 2, position 18.", exception.Message);
    }

    [Fact]
    public void DeserializeStringRegex_NoEndSlash_Error()
    {
        var json = """
            {
              "Regex": "\/abc"
            }
            """;

        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new RegexConverter()
            }
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RegexTestClass>(json, settings));
        Assert.Equal("Regex pattern must be enclosed by slashes. Path 'Regex', line 2, position 18.", exception.Message);
    }

    [Fact]
    public void DeserializeStringRegex_NoStartAndEndSlashes_Error()
    {
        var json = """
                   {
                     "Regex": "abc"
                   }
                   """;

        var settings = new JsonSerializerSettings
        {
            Converters =
            {
                new RegexConverter()
            }
        };
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<RegexTestClass>(json, settings));
        Assert.Equal("Regex pattern must be enclosed by slashes. Path 'Regex', line 2, position 16.", exception.Message);
    }

    [Fact]
    public void DeserializeFromText()
    {
        var json = """
            {
              "Pattern": "abc",
              "Options": 513
            }
            """;

        var newRegex = JsonConvert.DeserializeObject<Regex>(json, new RegexConverter());
        Assert.Equal("abc", newRegex.ToString());
        Assert.Equal(RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, newRegex.Options);
    }

    [Fact]
    public void ConvertEmptyRegexJson()
    {
        var regex = new Regex("");

        var json = JsonConvert.SerializeObject(new RegexTestClass {Regex = regex}, Formatting.Indented, new RegexConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Regex": {
                "Pattern": "",
                "Options": 0
              }
            }
            """,
            json);

        var newRegex = JsonConvert.DeserializeObject<RegexTestClass>(json, new RegexConverter());
        Assert.Equal("", newRegex.Regex.ToString());
        Assert.Equal(RegexOptions.None, newRegex.Regex.Options);
    }

    public class SimpleClassWithRegex
    {
        public Regex RegProp { get; set; }
    }

    [Fact]
    public void DeserializeNullRegex()
    {
        var json = JsonConvert.SerializeObject(new SimpleClassWithRegex {RegProp = null});
        Assert.Equal("""{"RegProp":null}""", json);

        var obj = JsonConvert.DeserializeObject<SimpleClassWithRegex>(json);
        Assert.Null(obj.RegProp);
    }
}
