// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
// ReSharper disable UnusedVariable
// ReSharper disable PossibleMultipleEnumeration

public class LinqToJsonTest : TestFixtureBase
{
    [Fact]
    public void EscapedQuotePath()
    {
        var v = new JValue(1);
        var o = new JObject
        {
            ["We're offline!"] = v
        };

        Assert.Equal(@"['We\'re offline!']", v.Path);
    }

    public class DemoClass
    {
        public decimal maxValue;
    }

    [Fact]
    public void ToObjectDecimal()
    {
        var jArray = JArray.Parse("[{ maxValue:10000000000000000000 }]");
        var list = jArray.ToObject<List<DemoClass>>();

        Assert.Equal(10000000000000000000m, list[0].maxValue);
    }

    [Fact]
    public void ToObjectFromGuidToString()
    {
        var token = new JValue(new Guid("91274484-3b20-48b4-9d18-7d936b2cb88f"));
        var value = token.ToObject<string>();
        Assert.Equal("91274484-3b20-48b4-9d18-7d936b2cb88f", value);
    }

    [Fact]
    public void ToObjectFromIntegerToString()
    {
        var token = new JValue(1234);
        var value = token.ToObject<string>();
        Assert.Equal("1234", value);
    }

    [Fact]
    public void ToObjectFromStringToInteger()
    {
        var token = new JValue("1234");
        var value = token.ToObject<int>();
        Assert.Equal(1234, value);
    }

    [Fact]
    public void FromObjectGuid()
    {
        var token1 = new JValue(Guid.NewGuid());
        var token2 = JToken.FromObject(token1);
        Assert.True(JToken.DeepEquals(token1, token2));
        Assert.Equal(token1.Type, token2.Type);
    }

    [Fact]
    public void FromObjectTimeSpan()
    {
        var token1 = new JValue(TimeSpan.FromDays(1));
        var token2 = JToken.FromObject(token1);
        Assert.True(JToken.DeepEquals(token1, token2));
        Assert.Equal(token1.Type, token2.Type);
    }

    [Fact]
    public void FromObjectUri()
    {
        var token1 = new JValue(new Uri("http://www.newtonsoft.com"));
        var token2 = JToken.FromObject(token1);
        Assert.True(JToken.DeepEquals(token1, token2));
        Assert.Equal(token1.Type, token2.Type);
    }

    [Fact]
    public void ToObject_Guid()
    {
        var anon = new JObject
        {
            ["id"] = Guid.NewGuid()
        };
        Assert.Equal(JTokenType.Guid, anon["id"].Type);

        var dict = anon.ToObject<Dictionary<string, JToken>>();
        Assert.Equal(JTokenType.Guid, dict["id"].Type);
    }

    public class TestClass_ULong
    {
        public ulong Value { get; set; }
    }

    [Fact]
    public void FromObject_ULongMaxValue()
    {
        var instance = new TestClass_ULong
        {
            Value = ulong.MaxValue
        };
        var output = JObject.FromObject(instance);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Value": 18446744073709551615
            }
            """,
            output.ToString());
    }

    public class TestClass_Byte
    {
        public byte Value { get; set; }
    }

    [Fact]
    public void FromObject_ByteMaxValue()
    {
        var instance = new TestClass_Byte
        {
            Value = byte.MaxValue
        };
        var output = JObject.FromObject(instance);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Value": 255
            }
            """,
            output.ToString());
    }

    [Fact]
    public void ToObject_Base64AndGuid()
    {
        var o = JObject.Parse("{'responseArray':'AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA'}");
        var data = o["responseArray"].ToObject<byte[]>();
        var expected = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAABAAAA");

        Assert.Equal(expected, data);

        o = JObject.Parse("{'responseArray':'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA'}");
        data = o["responseArray"].ToObject<byte[]>();
        expected = new Guid("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAABAAAA").ToByteArray();

        Assert.Equal(expected, data);
    }

    [Fact]
    public void IncompleteContainers()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JArray.Parse("[1,"));
        Assert.Equal("Unexpected end of content while loading JArray. Path '[0]', line 1, position 3.", exception.Message);

        var exception1 = Assert.Throws<JsonReaderException>(() => JArray.Parse("[1"));
        Assert.Equal("Unexpected end of content while loading JArray. Path '[0]', line 1, position 2.", exception1.Message);

        var exception2 = Assert.Throws<JsonReaderException>(() => JObject.Parse("{'key':1,"));
        Assert.Equal("Unexpected end of content while loading JObject. Path 'key', line 1, position 9.", exception2.Message);

        var exception3 = Assert.Throws<JsonReaderException>(() => JObject.Parse("{'key':1"));
        Assert.Equal("Unexpected end of content while loading JObject. Path 'key', line 1, position 8.", exception3.Message);
    }

    [Fact]
    public void EmptyJEnumerableCount()
    {
        var tokens = new JEnumerable<JToken>();

        Assert.Empty(tokens);
    }

    [Fact]
    public void EmptyJEnumerableAsEnumerable()
    {
        IEnumerable tokens = new JEnumerable<JToken>();

        Assert.Empty(tokens.Cast<JToken>());
    }

    [Fact]
    public void EmptyJEnumerableEquals()
    {
        var tokens1 = new JEnumerable<JToken>();
        var tokens2 = new JEnumerable<JToken>();

        Assert.True(tokens1.Equals(tokens2));

        object o1 = new JEnumerable<JToken>();
        object o2 = new JEnumerable<JToken>();

        Assert.True(o1.Equals(o2));
    }

    [Fact]
    public void EmptyJEnumerableGetHashCode()
    {
        var tokens = new JEnumerable<JToken>();

        Assert.Equal(0, tokens.GetHashCode());
    }

    [Fact]
    public void CommentsAndReadFrom()
    {
        var textReader = new StringReader(
            """
            [
                // hi
                1,
                2,
                3
            ]
            """);

        var jsonReader = new JsonTextReader(textReader);
        var a = (JArray) JToken.ReadFrom(jsonReader, new()
        {
            CommentHandling = CommentHandling.Load
        });

        Assert.Equal(4, a.Count);
        Assert.Equal(JTokenType.Comment, a[0].Type);
        Assert.Equal(" hi", ((JValue) a[0]).Value);
    }

    [Fact]
    public void CommentsAndReadFrom_IgnoreComments()
    {
        var textReader = new StringReader(
            """
            [
                // hi
                1,
                2,
                3
            ]
            """);

        var jsonReader = new JsonTextReader(textReader);
        var a = (JArray) JToken.ReadFrom(jsonReader);

        Assert.Equal(3, a.Count);
        Assert.Equal(JTokenType.Integer, a[0].Type);
        Assert.Equal(1L, ((JValue) a[0]).Value);
    }

    [Fact]
    public void StartingCommentAndReadFrom()
    {
        var textReader = new StringReader(
            """
            // hi
            [
                1,
                2,
                3
            ]
            """);

        var jsonReader = new JsonTextReader(textReader);
        var v = (JValue) JToken.ReadFrom(jsonReader, new()
        {
            CommentHandling = CommentHandling.Load
        });

        Assert.Equal(JTokenType.Comment, v.Type);

        IJsonLineInfo lineInfo = v;
        Assert.True(lineInfo.HasLineInfo());
        Assert.Equal(1, lineInfo.LineNumber);
        Assert.Equal(5, lineInfo.LinePosition);
    }

    [Fact]
    public void StartingCommentAndReadFrom_IgnoreComments()
    {
        var textReader = new StringReader(
            """
            // hi
            [
                1,
                2,
                3
            ]
            """);

        var jsonReader = new JsonTextReader(textReader);
        var a = (JArray) JToken.ReadFrom(jsonReader, new()
        {
            CommentHandling = CommentHandling.Ignore
        });

        Assert.Equal(JTokenType.Array, a.Type);

        IJsonLineInfo lineInfo = a;
        Assert.True(lineInfo.HasLineInfo());
        Assert.Equal(2, lineInfo.LineNumber);
        Assert.Equal(1, lineInfo.LinePosition);
    }

    [Fact]
    public void StartingUndefinedAndReadFrom()
    {
        var textReader = new StringReader(
            """
            undefined
            [
                1,
                2,
                3
            ]
            """);

        var jsonReader = new JsonTextReader(textReader);
        var v = (JValue) JToken.ReadFrom(jsonReader);

        Assert.Equal(JTokenType.Undefined, v.Type);

        IJsonLineInfo lineInfo = v;
        Assert.True(lineInfo.HasLineInfo());
        Assert.Equal(1, lineInfo.LineNumber);
        Assert.Equal(9, lineInfo.LinePosition);
    }

    [Fact]
    public void StartingEndArrayAndReadFrom()
    {
        var textReader = new StringReader("[]");

        var jsonReader = new JsonTextReader(textReader);
        jsonReader.Read();
        jsonReader.Read();

        var exception = Assert.Throws<JsonReaderException>(() => JToken.ReadFrom(jsonReader));
        Assert.Equal("Error reading JToken from JsonReader. Unexpected token: EndArray. Path '', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void JPropertyPath()
    {
        var o = new JObject
        {
            {
                "person", new JObject
                {
                    {
                        "$id", 1
                    }
                }
            }
        };

        var idProperty = o["person"]["$id"].Parent;
        Assert.Equal("person.$id", idProperty.Path);
    }

    [Fact]
    public void EscapedPath()
    {
        var json = """
            {
              "frameworks": {
                "NET5_0_OR_GREATER": {
                  "dependencies": {
                    "System.Xml.ReaderWriter": {
                      "source": "NuGet"
                    }
                  }
                }
              }
            }
            """;

        var o = JObject.Parse(json);

        var v1 = o["frameworks"]["NET5_0_OR_GREATER"]["dependencies"]["System.Xml.ReaderWriter"]["source"];

        Assert.Equal("frameworks.NET5_0_OR_GREATER.dependencies['System.Xml.ReaderWriter'].source", v1.Path);

        var v2 = o.SelectToken(v1.Path);

        Assert.Equal(v1, v2);
    }

    [Fact]
    public void EscapedPathTests()
    {
        EscapedPathAssert("this has spaces", "['this has spaces']");
        EscapedPathAssert("(RoundBraces)", "['(RoundBraces)']");
        EscapedPathAssert("[SquareBraces]", "['[SquareBraces]']");
        EscapedPathAssert("this.has.dots", "['this.has.dots']");
    }

    static void EscapedPathAssert(string propertyName, string expectedPath)
    {
        var v1 = int.MaxValue;
        var value = new JValue(v1);

        var o = new JObject(new JProperty(propertyName, value));

        Assert.Equal(expectedPath, value.Path);

        var selectedValue = (JValue) o.SelectToken(value.Path);

        Assert.Equal(value, selectedValue);
    }

    [Fact]
    public void ForEach()
    {
        var items = new JArray(new JObject(new JProperty("name", "value!")));

        foreach (JObject friend in items)
        {
            XUnitAssert.AreEqualNormalized(
                """
                {
                  "name": "value!"
                }
                """,
                friend.ToString());
        }
    }

    [Fact]
    public void DoubleValue()
    {
        var j = JArray.Parse("[-1E+4,100.0e-2]");

        var value = (double) j[0];
        Assert.Equal(-10000d, value);

        value = (double) j[1];
        Assert.Equal(1d, value);
    }

    [Fact]
    public void Manual()
    {
        var array = new JArray();
        var text = new JValue("Manual text");
        var date = new JValue(new DateTime(2000, 5, 23));

        array.Add(text);
        array.Add(date);

        var json = array.ToString();
        // [
        //   "Manual text",
        //   "\/Date(958996800000+1200)\/"
        // ]
    }

    [Fact]
    public void LinqToJsonDeserialize()
    {
        var o = new JObject(
            new JProperty("Name", "John Smith"),
            new JProperty("BirthDate", new DateTime(1983, 3, 20))
        );

        var serializer = new JsonSerializer();
        var p = serializer.Deserialize<Person>(new JTokenReader(o));

        Assert.Equal("John Smith", p.Name);
    }

    [Fact]
    public void ObjectParse()
    {
        var json = """
            {
                CPU: 'Intel',
                Drives: [
                  'DVD read/writer',
                  "500 gigabyte hard drive"
                ]
            }
            """;

        var o = JObject.Parse(json);
        var properties = o.Properties().ToList();

        Assert.Equal("CPU", properties[0].Name);
        Assert.Equal("Intel", (string) properties[0].Value);
        Assert.Equal("Drives", properties[1].Name);

        var list = (JArray) properties[1].Value;
        Assert.Equal(2, list.Children().Count());
        Assert.Equal("DVD read/writer", (string) list.Children().ElementAt(0));
        Assert.Equal("500 gigabyte hard drive", (string) list.Children().ElementAt(1));

        var parameterValues =
            (from p in o.Properties()
                where p.Value is JValue
                select ((JValue) p.Value).Value).ToList();

        Assert.Single(parameterValues);
        Assert.Equal("Intel", parameterValues[0]);
    }

    [Fact]
    public void CreateLongArray()
    {
        var json = "[0,1,2,3,4,5,6,7,8,9]";

        var a = JArray.Parse(json);
        var list = a.Values<int>().ToList();

        var expected = new List<int>
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        };

        Assert.Equal(expected, list);
    }

    [Fact]
    public void GoogleSearchAPI()
    {
        #region GoogleJson

        var json = """
            {
                results:
                    [
                        {
                            GsearchResultClass:"GwebSearch",
                            unescapedUrl : "http://www.google.com/",
                            url : "http://www.google.com/",
                            visibleUrl : "www.google.com",
                            cacheUrl :
            "http://www.google.com/search?q=cache:zhool8dxBV4J:www.google.com",
                            title : "Google",
                            titleNoFormatting : "Google",
                            content : "Enables users to search the Web, Usenet, and
            images. Features include PageRank,   caching and translation of
            results, and an option to find similar pages."
                        },
                        {
                            GsearchResultClass:"GwebSearch",
                            unescapedUrl : "http://news.google.com/",
                            url : "http://news.google.com/",
                            visibleUrl : "news.google.com",
                            cacheUrl :
            "http://www.google.com/search?q=cache:Va_XShOz_twJ:news.google.com",
                            title : "Google News",
                            titleNoFormatting : "Google News",
                            content : "Aggregated headlines and a search engine of many of the world's news sources."
                        },

                        {
                            GsearchResultClass:"GwebSearch",
                            unescapedUrl : "http://groups.google.com/",
                            url : "http://groups.google.com/",
                            visibleUrl : "groups.google.com",
                            cacheUrl :
            "http://www.google.com/search?q=cache:x2uPD3hfkn0J:groups.google.com",
                            title : "Google Groups",
                            titleNoFormatting : "Google Groups",
                            content : "Enables users to search and browse the Usenet
            archives which consist of over 700   million messages, and post new
            comments."
                        },

                        {
                            GsearchResultClass:"GwebSearch",
                            unescapedUrl : "http://maps.google.com/",
                            url : "http://maps.google.com/",
                            visibleUrl : "maps.google.com",
                            cacheUrl :
            "http://www.google.com/search?q=cache:dkf5u2twBXIJ:maps.google.com",
                            title : "Google Maps",
                            titleNoFormatting : "Google Maps",
                            content : "Provides directions, interactive maps, and
            satellite/aerial imagery of the United   States. Can also search by
            keyword such as type of business."
                        }
                    ],

                adResults:
                    [
                        {
                            GsearchResultClass:"GwebSearch.ad",
                            title : "Gartner Symposium/ITxpo",
                            content1 : "Meet brilliant Gartner IT analysts",
                            content2 : "20-23 May 2007- Barcelona, Spain",
                            visibleUrl : "www.gartner.com"
                        }
                    ]
            }

            """;

        #endregion

        var o = JObject.Parse(json);

        var resultObjects = o["results"].Children<JObject>().ToList();

        Assert.Equal(32, resultObjects.Properties().Count());

        Assert.Equal(32, resultObjects.Values().Count());

        Assert.Equal(4, resultObjects.Values("GsearchResultClass").Count());

        Assert.Equal(5, o.PropertyValues().Cast<JArray>().Children().Count());

        var resultUrls = o["results"].Children().Values<string>("url").ToList();

        var expectedUrls = new List<string>
        {
            "http://www.google.com/",
            "http://news.google.com/",
            "http://groups.google.com/",
            "http://maps.google.com/"
        };

        Assert.Equal(expectedUrls, resultUrls);

        var descendants = o.Descendants().ToList();
        Assert.Equal(83, descendants.Count);
    }

    [Fact]
    public void JTokenToString()
    {
        var json = """
            {
              CPU: 'Intel',
              Drives: [
                'DVD read/writer',
                "500 gigabyte hard drive"
              ]
            }
            """;

        var o = JObject.Parse(json);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "CPU": "Intel",
              "Drives": [
                "DVD read/writer",
                "500 gigabyte hard drive"
              ]
            }
            """,
            o.ToString());

        var list = o.Value<JArray>("Drives");

        XUnitAssert.AreEqualNormalized(
            """
            [
              "DVD read/writer",
              "500 gigabyte hard drive"
            ]
            """, list.ToString());

        var cpuProperty = o.Property("CPU");
        Assert.Equal(
            """
            "CPU": "Intel"
            """,
            cpuProperty.ToString());

        var drivesProperty = o.Property("Drives");
        XUnitAssert.AreEqualNormalized(
            """
            "Drives": [
              "DVD read/writer",
              "500 gigabyte hard drive"
            ]
            """,
            drivesProperty.ToString());
    }

    [Fact]
    public void JTokenToStringTypes()
    {
        var json = """
            {
                Color:2,
                Width:1.1,
                Employees:999,
                RoomsPerFloor:[1,2,3,4,5,6,7,8,9],
                Open:false,
                Symbol:'@',
                Mottos:[
                    'Hello World',
                    null,
                    ' '],
                Cost:100980.1,
                Escape:"\r\n\t\f\b?{\\r\\n\"'",
                product:
                [
                    {
                        Name:'Rocket',
                        Price:0
                    },
                    {
                        Name:'Alien',
                        Price:0
                    }
                ]
            }
            """;

        var o = JObject.Parse(json);

        Assert.Equal(
            """
            "Width": 1.1
            """,
            o.Property("Width").ToString());
        Assert.Equal("1.1", ((JValue) o.Property("Width").Value).ToString(InvariantCulture));
        Assert.Equal(
            """
            "Open": false
            """,
            o.Property("Open").ToString());
        Assert.Equal("False", o.Property("Open").Value.ToString());

        json = "[null,undefined]";

        var a = JArray.Parse(json);
        XUnitAssert.AreEqualNormalized(
            """
            [
              null,
              undefined
            ]
            """,
            a.ToString());
        Assert.Equal("", a.Children().ElementAt(0).ToString());
        Assert.Equal("", a.Children().ElementAt(1).ToString());
    }

    [Fact]
    public void CreateJTokenTree()
    {
        var o =
            new JObject(
                new JProperty("Test1", "Test1Value"),
                new JProperty("Test2", "Test2Value"),
                new JProperty("Test3", "Test3Value"),
                new JProperty("Test4", null)
            );

        Assert.Equal(4, o.Properties().Count());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Test1": "Test1Value",
              "Test2": "Test2Value",
              "Test3": "Test3Value",
              "Test4": null
            }
            """,
            o.ToString());

        var a =
            new JArray(
                o,
                new DateTime(2000, 10, 10, 0, 0, 0, DateTimeKind.Utc),
                55,
                new JArray(
                    "1",
                    2,
                    3.0,
                    new DateTime(4, 5, 6, 7, 8, 9, DateTimeKind.Utc)
                )
            );

        Assert.Equal(4, a.Count);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Test1": "Test1Value",
                "Test2": "Test2Value",
                "Test3": "Test3Value",
                "Test4": null
              },
              "2000-10-10T00:00:00Z",
              55,
              [
                "1",
                2,
                3.0,
                "0004-05-06T07:08:09Z"
              ]
            ]
            """,
            a.ToString());
    }

    class Post
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public IList<string> Categories { get; set; }
    }

    static List<Post> GetPosts() =>
    [
        new()
        {
            Title = "LINQ to JSON beta",
            Description = "Announcing LINQ to JSON",
            Link = "http://james.newtonking.com/projects/json-net.aspx",
            Categories =
            [
                "Json.NET",
                "LINQ"
            ]
        },

        new()
        {
            Title = "Json.NET 1.3 + New license + Now on CodePlex",
            Description = "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
            Link = "http://james.newtonking.com/projects/json-net.aspx",
            Categories =
            [
                "Json.NET",
                "CodePlex"
            ]
        }
    ];

    [Fact]
    public void FromObjectExample()
    {
        var p = new Post
        {
            Title = "How to use FromObject",
            Categories = ["LINQ to JSON"]
        };

        // serialize Post to JSON then parse JSON – SLOW!
        //JObject o = JObject.Parse(JsonConvert.SerializeObject(p));

        // create JObject directly from the Post
        var o = JObject.FromObject(p);

        o["Title"] = $"{o["Title"]} - Super effective!";

        var json = o.ToString();
        // {
        //   "Title": "How to use FromObject - It's super effective!",
        //   "Categories": [
        //     "LINQ to JSON"
        //   ]
        // }

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Title": "How to use FromObject - Super effective!",
              "Description": null,
              "Link": null,
              "Categories": [
                "LINQ to JSON"
              ]
            }
            """,
            json);
    }

    [Fact]
    public void QueryingExample()
    {
        var posts = JArray.Parse(
            """
            [
              {
                'Title': 'JSON Serializer Basics',
                'Date': '2013-12-21T00:00:00',
                'Categories': []
              },
              {
                'Title': 'Querying LINQ to JSON',
                'Date': '2014-06-03T00:00:00',
                'Categories': [
                  'LINQ to JSON'
                ]
              }
            ]
            """);

        var serializerBasics = posts
            .Single(p => (string) p["Title"] == "JSON Serializer Basics");
        // JSON Serializer Basics

        var since2012 = posts
            .Where(p => (DateTime) p["Date"] > new DateTime(2012, 1, 1)).ToList();
        // JSON Serializer Basics
        // Querying LINQ to JSON

        var linqToJson = posts
            .Where(_ => _["Categories"].Any(c => (string) c == "LINQ to JSON")).ToList();
        // Querying LINQ to JSON

        Assert.NotNull(serializerBasics);
        Assert.Equal(2, since2012.Count);
        Assert.Single(linqToJson);
    }

    [Fact]
    public void CreateJTokenTreeNested()
    {
        var posts = GetPosts();

        var rss =
            new JObject(
                new JProperty("channel",
                    new JObject(
                        new JProperty("title", "James Newton-King"),
                        new JProperty("link", "http://james.newtonking.com"),
                        new JProperty("description", "James Newton-King's blog."),
                        new JProperty("item",
                            new JArray(
                                from p in posts
                                orderby p.Title
                                select new JObject(
                                    new JProperty("title", p.Title),
                                    new JProperty("description", p.Description),
                                    new JProperty("link", p.Link),
                                    new JProperty("category",
                                        new JArray(
                                            from c in p.Categories
                                            select new JValue(c)))))))));

        XUnitAssert.AreEqualNormalized(
            """
            {
              "channel": {
                "title": "James Newton-King",
                "link": "http://james.newtonking.com",
                "description": "James Newton-King's blog.",
                "item": [
                  {
                    "title": "Json.NET 1.3 + New license + Now on CodePlex",
                    "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "CodePlex"
                    ]
                  },
                  {
                    "title": "LINQ to JSON beta",
                    "description": "Announcing LINQ to JSON",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "LINQ"
                    ]
                  }
                ]
              }
            }
            """,
            rss.ToString());

        var postTitles =
            (
                from p in rss["channel"]["item"]
                select p.Value<string>("title")
            )
            .ToList();

        Assert.Equal("Json.NET 1.3 + New license + Now on CodePlex", postTitles.ElementAt(0));
        Assert.Equal("LINQ to JSON beta", postTitles.ElementAt(1));

        var categories =
            (
                from c in rss["channel"]["item"].Children()["category"].Values<string>()
                group c by c
                into g
                orderby g.Count() descending
                select new
                {
                    Category = g.Key,
                    Count = g.Count()
                }
            )
            .ToList();

        Assert.Equal("Json.NET", categories.ElementAt(0).Category);
        Assert.Equal(2, categories.ElementAt(0).Count);
        Assert.Equal("CodePlex", categories.ElementAt(1).Category);
        Assert.Equal(1, categories.ElementAt(1).Count);
        Assert.Equal("LINQ", categories.ElementAt(2).Category);
        Assert.Equal(1, categories.ElementAt(2).Count);
    }

    [Fact]
    public void BasicQuerying()
    {
        var json = """
                   {
                   "channel": {
                     "title": "James Newton-King",
                     "link": "http://james.newtonking.com",
                     "description": "James Newton-King's blog.",
                     "item": [
                       {
                         "title": "Json.NET 1.3 + New license + Now on CodePlex",
                         "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                         "link": "http://james.newtonking.com/projects/json-net.aspx",
                         "category": [
                           "Json.NET",
                           "CodePlex"
                         ]
                       },
                       {
                         "title": "LINQ to JSON beta",
                         "description": "Announcing LINQ to JSON",
                         "link": "http://james.newtonking.com/projects/json-net.aspx",
                         "category": [
                           "Json.NET",
                           "LINQ"
                         ]
                       }
                     ]
                   }
                   }
                   """;

        var o = JObject.Parse(json);

        Assert.Null(o["purple"]);
        Assert.Null(o.Value<string>("purple"));

        Assert.IsType<JArray>(o["channel"]["item"]);

        Assert.Equal(2, o["channel"]["item"].Children()["title"].Count());
        Assert.Empty(o["channel"]["item"].Children()["monkey"]);

        Assert.Equal("Json.NET 1.3 + New license + Now on CodePlex", (string) o["channel"]["item"][0]["title"]);

        var expected = new[]
        {
            "Json.NET 1.3 + New license + Now on CodePlex",
            "LINQ to JSON beta"
        };
        var actual = o["channel"]["item"].Children().Values<string>("title").ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JObjectIntIndex()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var o = new JObject();
            Assert.Null(o[0]);
        });
        Assert.Equal("Accessed JObject values with invalid key value: 0. Object property name expected.", exception.Message);
    }

    [Fact]
    public void JArrayStringIndex()
    {
        var exception = Assert.Throws<Exception>(() =>
        {
            var a = new JArray();
            Assert.Null(a["purple"]);
        });
        Assert.Equal("""Accessed JArray values with invalid key value: "purple". Int32 array index expected.""", exception.Message);
    }

    [Fact]
    public void ToStringJsonConverter()
    {
        var o =
            new JObject(
                new JProperty("Test1", new DateTime(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc)),
                new JProperty("Test2", new DateTimeOffset(2000, 10, 15, 5, 5, 5, new(11, 11, 0))),
                new JProperty("Test3", "Test3Value"),
                new JProperty("Test4", null)
            );

        var serializer = new JsonSerializer();
        var stringWriter = new StringWriter();
        JsonWriter jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };
        serializer.Serialize(jsonWriter, o);

        var json = stringWriter.ToString();

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Test1": "2000-10-15T05:05:05Z",
              "Test2": "2000-10-15T05:05:05+11:11",
              "Test3": "Test3Value",
              "Test4": null
            }
            """,
            json);
    }

    [Fact]
    public void DateTimeOffset()
    {
        var testDates = new List<DateTimeOffset>
        {
            new(new(100, 1, 1, 1, 1, 1, DateTimeKind.Utc)),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.Zero),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(13)),
            new(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(-3.5))
        };

        var serializer = new JsonSerializer();

        JTokenWriter jsonWriter;
        using (jsonWriter = new())
        {
            serializer.Serialize(jsonWriter, testDates);
        }

        Assert.Equal(4, jsonWriter.Token.Children().Count());
    }

    [Fact]
    public void FromObject()
    {
        var posts = GetPosts();

        var o = JObject.FromObject(new
        {
            channel = new
            {
                title = "James Newton-King",
                link = "http://james.newtonking.com",
                description = "James Newton-King's blog.",
                item =
                    from p in posts
                    orderby p.Title
                    select new
                    {
                        title = p.Title,
                        description = p.Description,
                        link = p.Link,
                        category = p.Categories
                    }
            }
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "channel": {
                "title": "James Newton-King",
                "link": "http://james.newtonking.com",
                "description": "James Newton-King's blog.",
                "item": [
                  {
                    "title": "Json.NET 1.3 + New license + Now on CodePlex",
                    "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "CodePlex"
                    ]
                  },
                  {
                    "title": "LINQ to JSON beta",
                    "description": "Announcing LINQ to JSON",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "LINQ"
                    ]
                  }
                ]
              }
            }
            """,
            o.ToString());

        Assert.IsType<JObject>(o);
        Assert.IsType<JObject>(o["channel"]);
        Assert.Equal("James Newton-King", (string) o["channel"]["title"]);
        Assert.Equal(2, o["channel"]["item"].Children().Count());

        var a = JArray.FromObject(new List<int>
        {
            0,
            1,
            2,
            3,
            4
        });
        Assert.IsType<JArray>(a);
        Assert.Equal(5, a.Count);
    }

    [Fact]
    public void FromAnonDictionary()
    {
        var posts = GetPosts();

        var o = JObject.FromObject(new
        {
            channel = new Dictionary<string, object>
            {
                {
                    "title", "James Newton-King"
                },
                {
                    "link", "http://james.newtonking.com"
                },
                {
                    "description", "James Newton-King's blog."
                },
                {
                    "item", from p in posts
                    orderby p.Title
                    select new
                    {
                        title = p.Title,
                        description = p.Description,
                        link = p.Link,
                        category = p.Categories
                    }
                }
            }
        });

        XUnitAssert.AreEqualNormalized(
            """
            {
              "channel": {
                "title": "James Newton-King",
                "link": "http://james.newtonking.com",
                "description": "James Newton-King's blog.",
                "item": [
                  {
                    "title": "Json.NET 1.3 + New license + Now on CodePlex",
                    "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "CodePlex"
                    ]
                  },
                  {
                    "title": "LINQ to JSON beta",
                    "description": "Announcing LINQ to JSON",
                    "link": "http://james.newtonking.com/projects/json-net.aspx",
                    "category": [
                      "Json.NET",
                      "LINQ"
                    ]
                  }
                ]
              }
            }
            """,
            o.ToString());

        Assert.IsType<JObject>(o);
        Assert.IsType<JObject>(o["channel"]);
        Assert.Equal("James Newton-King", (string) o["channel"]["title"]);
        Assert.Equal(2, o["channel"]["item"].Children().Count());

        var a = JArray.FromObject(new List<int>
        {
            0,
            1,
            2,
            3,
            4
        });
        Assert.IsType<JArray>(a);
        Assert.Equal(5, a.Count);
    }

    [Fact]
    public void AsJEnumerable()
    {
        var o =
            new JObject(
                new JProperty("Test1", new DateTime(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc)),
                new JProperty("Test2", "Test2Value"),
                new JProperty("Test3", null)
            );

        var enumerable = o.AsJEnumerable();
        Assert.NotNull(enumerable);
        Assert.Equal(o, enumerable);

        var d = enumerable["Test1"].Value<DateTime>();

        Assert.Equal(new(2000, 10, 15, 5, 5, 5, DateTimeKind.Utc), d);
    }

    [Fact]
    public void CovariantIJEnumerable()
    {
        IEnumerable<JObject> o =
        [
            JObject.FromObject(new
            {
                First = 1,
                Second = 2
            }),
            JObject.FromObject(new
            {
                First = 1,
                Second = 2
            })
        ];

        IJEnumerable<JToken> values = o.Properties();
        Assert.Equal(4, values.Count());
    }

    [Fact]
    public void LinqCast()
    {
        JToken olist = JArray.Parse("[12,55]");

        var list1 = olist.AsEnumerable().Values<int>().ToList();

        Assert.Equal(12, list1[0]);
        Assert.Equal(55, list1[1]);
    }

    [Fact]
    public void ChildrenExtension()
    {
        var json = """
                   [
                       {
                         "title": "James Newton-King",
                         "link": "http://james.newtonking.com",
                         "description": "James Newton-King's blog.",
                         "item": [
                           {
                             "title": "Json.NET 1.3 + New license + Now on CodePlex",
                             "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                             "link": "http://james.newtonking.com/projects/json-net.aspx",
                             "category": [
                               "Json.NET",
                               "CodePlex"
                             ]
                           },
                           {
                             "title": "LINQ to JSON beta",
                             "description": "Announcing LINQ to JSON",
                             "link": "http://james.newtonking.com/projects/json-net.aspx",
                             "category": [
                               "Json.NET",
                               "LINQ"
                             ]
                           }
                         ]
                       },
                       {
                         "title": "James Newton-King",
                         "link": "http://james.newtonking.com",
                         "description": "James Newton-King's blog.",
                         "item": [
                           {
                             "title": "Json.NET 1.3 + New license + Now on CodePlex",
                             "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
                             "link": "http://james.newtonking.com/projects/json-net.aspx",
                             "category": [
                               "Json.NET",
                               "CodePlex"
                             ]
                           },
                           {
                             "title": "LINQ to JSON beta",
                             "description": "Announcing LINQ to JSON",
                             "link": "http://james.newtonking.com/projects/json-net.aspx",
                             "category": [
                               "Json.NET",
                               "LINQ"
                             ]
                           }
                         ]
                       }
                     ]
                   """;

        var o = JArray.Parse(json);

        Assert.Equal(4, o.Children()["item"].Children()["title"].Count());
        var expected = new[]
        {
            "Json.NET 1.3 + New license + Now on CodePlex",
            "LINQ to JSON beta",
            "Json.NET 1.3 + New license + Now on CodePlex",
            "LINQ to JSON beta"
        };
        var actual = o.Children()["item"].Children()["title"].Values<string>().ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UriGuidTimeSpanTestClassEmptyTest()
    {
        var c1 = new UriGuidTimeSpanTestClass();
        var o = JObject.FromObject(c1);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Guid": "00000000-0000-0000-0000-000000000000",
              "NullableGuid": null,
              "TimeSpan": "00:00:00",
              "NullableTimeSpan": null,
              "Uri": null
            }
            """,
            o.ToString());

        var c2 = o.ToObject<UriGuidTimeSpanTestClass>();
        Assert.Equal(c1.Guid, c2.Guid);
        Assert.Equal(c1.NullableGuid, c2.NullableGuid);
        Assert.Equal(c1.TimeSpan, c2.TimeSpan);
        Assert.Equal(c1.NullableTimeSpan, c2.NullableTimeSpan);
        Assert.Equal(c1.Uri, c2.Uri);
    }

    [Fact]
    public void UriGuidTimeSpanTestClassValuesTest()
    {
        var c1 = new UriGuidTimeSpanTestClass
        {
            Guid = new("1924129C-F7E0-40F3-9607-9939C531395A"),
            NullableGuid = new Guid("9E9F3ADF-E017-4F72-91E0-617EBE85967D"),
            TimeSpan = TimeSpan.FromDays(1),
            NullableTimeSpan = TimeSpan.FromHours(1),
            Uri = new("http://testuri.com")
        };
        var o = JObject.FromObject(c1);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Guid": "1924129c-f7e0-40f3-9607-9939c531395a",
              "NullableGuid": "9e9f3adf-e017-4f72-91e0-617ebe85967d",
              "TimeSpan": "1.00:00:00",
              "NullableTimeSpan": "01:00:00",
              "Uri": "http://testuri.com"
            }
            """,
            o.ToString());

        var c2 = o.ToObject<UriGuidTimeSpanTestClass>();
        Assert.Equal(c1.Guid, c2.Guid);
        Assert.Equal(c1.NullableGuid, c2.NullableGuid);
        Assert.Equal(c1.TimeSpan, c2.TimeSpan);
        Assert.Equal(c1.NullableTimeSpan, c2.NullableTimeSpan);
        Assert.Equal(c1.Uri, c2.Uri);

        var j = JsonConvert.SerializeObject(c1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(j, o.ToString());
    }

    [Fact]
    public void ParseWithPreceedingComments()
    {
        var json = "/* blah */ {'hi':'hi!'}";
        var o = JObject.Parse(json);
        Assert.Equal("hi!", (string) o["hi"]);

        json = "/* blah */ ['hi!']";
        var a = JArray.Parse(json);
        Assert.Equal("hi!", (string) a[0]);
    }

    //TODO:
    // [Fact]
    // public void ExceptionFromOverloadWithJValue()
    // {
    //     dynamic name = new JValue("Matthew Doig");
    //
    //     var users = new Dictionary<string, string>();
    //
    //     // unfortunately there doesn't appear to be a way around this
    //     var exception = Assert.Throws<RuntimeBinderException>(() =>
    //     {
    //         users.Add("name2", name);
    //         Assert.Equal("Matthew Doig", users["name2"]);
    //     });
    //     Assert.Equal("The best overloaded method match for 'System.Collections.Generic.Dictionary<string,string>.Add(string, string)' has some invalid arguments", exception.Message);
    // }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FooBar
    {
        [EnumMember(Value = "SOME_VALUE")]
        SomeValue,

        [EnumMember(Value = "SOME_OTHER_VALUE")]
        SomeOtherValue
    }

    public class MyObject
    {
        public FooBar FooBar { get; set; }
    }

    [Fact]
    public void ToObject_Enum_Converter()
    {
        var o = JObject.Parse("{'FooBar':'SOME_OTHER_VALUE'}");

        var e = o["FooBar"].ToObject<FooBar>();
        Assert.Equal(FooBar.SomeOtherValue, e);
    }

    public enum FooBarNoEnum
    {
        [EnumMember(Value = "SOME_VALUE")]
        SomeValue,

        [EnumMember(Value = "SOME_OTHER_VALUE")]
        SomeOtherValue
    }

    public class MyObjectNoEnum
    {
        public FooBarNoEnum FooBarNoEnum { get; set; }
    }

    [Fact]
    public void ToObject_Enum_NoConverter()
    {
        var o = JObject.Parse("{'FooBarNoEnum':'SOME_OTHER_VALUE'}");

        var e = o["FooBarNoEnum"].ToObject<FooBarNoEnum>();
        Assert.Equal(FooBarNoEnum.SomeOtherValue, e);
    }

    [Fact]
    public void SerializeWithNoRedundantIdPropertiesTest()
    {
        var dic1 = new Dictionary<string, object>();
        var dic2 = new Dictionary<string, object>();
        var dic3 = new Dictionary<string, object>();
        var list1 = new List<object>();
        var list2 = new List<object>();

        dic1.Add("list1", list1);
        dic1.Add("list2", list2);
        dic1.Add("dic1", dic1);
        dic1.Add("dic2", dic2);
        dic1.Add("dic3", dic3);
        dic1.Add("integer", 12345);

        list1.Add("A string!");
        list1.Add(dic1);
        list1.Add(new List<object>());

        dic3.Add("dic3", dic3);

        var json = SerializeWithNoRedundantIdProperties(dic1);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "list1": [
                "A string!",
                {
                  "$ref": "1"
                },
                []
              ],
              "list2": [],
              "dic1": {
                "$ref": "1"
              },
              "dic2": {},
              "dic3": {
                "$id": "3",
                "dic3": {
                  "$ref": "3"
                }
              },
              "integer": 12345
            }
            """,
            json);
    }

    static string SerializeWithNoRedundantIdProperties(object o)
    {
        var writer = new JTokenWriter();
        var serializer = JsonSerializer.Create(new()
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });
        serializer.Serialize(writer, o);

        var token = writer.Token;

        if (token is JContainer container)
        {
            // find all the $id properties in the JSON
            var ids = container.Descendants().OfType<JProperty>().Where(d => d.Name == "$id").ToList();

            if (ids.Count > 0)
            {
                // find all the $ref properties in the JSON
                var refs = container.Descendants().OfType<JProperty>().Where(d => d.Name == "$ref").ToList();

                foreach (var idProperty in ids)
                {
                    // check whether the $id property is used by a $ref
                    var idUsed = refs.Any(r => idProperty.Value.ToString() == r.Value.ToString());

                    if (!idUsed)
                    {
                        // remove unused $id
                        idProperty.Remove();
                    }
                }
            }
        }

        return token.ToString();
    }

    [Fact]
    public void HashCodeTests()
    {
        var o1 = new JObject
        {
            ["prop"] = 1
        };
        var o2 = new JObject
        {
            ["prop"] = 1
        };

        Assert.False(ReferenceEquals(o1, o2));
        Assert.False(Equals(o1, o2));
        Assert.False(o1.GetHashCode() == o2.GetHashCode());
        Assert.True(o1.GetDeepHashCode() == o2.GetDeepHashCode());
        Assert.True(JToken.DeepEquals(o1, o2));

        var a1 = new JArray
        {
            1
        };
        var a2 = new JArray
        {
            1
        };

        Assert.False(ReferenceEquals(a1, a2));
        Assert.False(Equals(a1, a2));
        Assert.False(a1.GetHashCode() == a2.GetHashCode());
        Assert.True(a1.GetDeepHashCode() == a2.GetDeepHashCode());
        Assert.True(JToken.DeepEquals(a1, a2));
    }
}