// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable UnusedVariable
// ReSharper disable PossibleMultipleEnumeration
namespace Argon.Tests.Documentation;

public class LinqToJsonTests : TestFixtureBase
{
    [Fact]
    public void LinqToJsonBasic()
    {
        #region LinqToJsonBasic

        var o = JObject.Parse(
            """
            {
              'CPU': 'Intel',
              'Drives': [
                'DVD read/writer',
                '500 gigabyte hard drive'
              ]
            }
            """);

        var cpu = (string) o["CPU"];
        // Intel

        var firstDrive = (string) o["Drives"][0];
        // DVD read/writer

        var allDrives = o["Drives"]
            .Select(t => (string) t)
            .ToList();
        // DVD read/writer
        // 500 gigabyte hard drive

        #endregion
    }

    [Fact]
    public void LinqToJsonCreateNormal()
    {
        #region LinqToJsonCreateNormal

        var array = new JArray();
        var text = new JValue("Manual text");
        var date = new JValue(new DateTime(2000, 5, 23));

        array.Add(text);
        array.Add(date);

        var json = array.ToString();
        // [
        //   "Manual text",
        //   "2000-05-23T00:00:00"
        // ]

        #endregion
    }

    public class Post
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public IList<string> Categories { get; set; }
    }

    static List<Post> GetPosts() => [];

    [Fact]
    public void LinqToJsonCreateDeclaratively()
    {
        #region LinqToJsonCreateDeclaratively

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

        Console.WriteLine(rss.ToString());

        //{
        //  "channel": {
        //    "title": "James Newton-King",
        //    "link": "http://james.newtonking.com",
        //    "description": "James Newton-King\'s blog.",
        //    "item": [
        //      {
        //        "title": "Json.NET 1.3 + New license + Now on CodePlex",
        //        "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
        //        "link": "http://james.newtonking.com/projects/json-net.aspx",
        //        "category": [
        //          "Json.NET",
        //          "CodePlex"
        //        ]
        //      },
        //      {
        //        "title": "LINQ to JSON beta",
        //        "description": "Announcing LINQ to JSON",
        //        "link": "http://james.newtonking.com/projects/json-net.aspx",
        //        "category": [
        //          "Json.NET",
        //          "LINQ"
        //        ]
        //      }
        //    ]
        //  }
        //}

        #endregion
    }

    [Fact]
    public void LinqToJsonCreateFromObject()
    {
        var posts = GetPosts();

        #region LinqToJsonCreateFromObject

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

        #endregion
    }

    [Fact]
    public void LinqToJsonCreateParse()
    {
        #region LinqToJsonCreateParse

        var json = """
                   {
                     CPU: 'Intel',
                     Drives: [
                       'DVD read/writer',
                       '500 gigabyte hard drive'
                     ]
                   }
                   """;

        var o = JObject.Parse(json);

        #endregion
    }

    [Fact]
    public void LinqToJsonCreateParseArray()
    {
        #region LinqToJsonCreateParseArray

        var json = """
                   [
                     'Small',
                     'Medium',
                     'Large'
                   ]
                   """;

        var a = JArray.Parse(json);

        #endregion
    }

    public static class File
    {
        public static StreamReader OpenText(string path) =>
            new(new MemoryStream("{}"u8.ToArray()));
    }

    [Fact]
    public void LinqToJsonReadObject()
    {
        #region LinqToJsonReadObject

        using var reader = File.OpenText(@"c:\person.json");
        var o = (JObject) JToken.ReadFrom(new JsonTextReader(reader));
        // do stuff

        #endregion
    }

    [Fact]
    public void LinqToJsonSimpleQuerying()
    {
        #region LinqToJsonSimpleQuerying

        var json = """
                   {
                     'channel': {
                       'title': 'James Newton-King',
                       'link': 'http://james.newtonking.com',
                       'description': 'James Newton-King\'s blog.',
                       'item': [
                         {
                           'title': 'Json.NET 1.3 + New license + Now on CodePlex',
                           'description': 'Announcing the release of Json.NET 1.3, the MIT license and the source on CodePlex',
                           'link': 'http://james.newtonking.com/projects/json-net.aspx',
                           'categories': [
                             'Json.NET',
                             'CodePlex'
                           ]
                         },
                         {
                           'title': 'LINQ to JSON beta',
                           'description': 'Announcing LINQ to JSON',
                           'link': 'http://james.newtonking.com/projects/json-net.aspx',
                           'categories': [
                             'Json.NET',
                             'LINQ'
                           ]
                         }
                       ]
                     }
                   }
                   """;

        var rss = JObject.Parse(json);

        var rssTitle = (string) rss["channel"]["title"];
        // James Newton-King

        var itemTitle = (string) rss["channel"]["item"][0]["title"];
        // Json.NET 1.3 + New license + Now on CodePlex

        var categories = (JArray) rss["channel"]["item"][0]["categories"];
        // ["Json.NET", "CodePlex"]

        var categoriesText = categories
            .Select(c => (string) c)
            .ToList();
        // Json.NET
        // CodePlex

        #endregion
    }

    [Fact]
    public void LinqToJsonQuerying()
    {
        var rss = JObject.Parse(
            """
            {
              'channel': {
                'title': 'James Newton-King',
                'link': 'http://james.newtonking.com',
                'description': 'James Newton-King\'s blog.',
                'item': [
                  {
                    'title': 'Json.NET 1.3 + New license + Now on CodePlex',
                    'description': 'Announcing the release of Json.NET 1.3, the MIT license and the source on CodePlex',
                    'link': 'http://james.newtonking.com/projects/json-net.aspx',
                    'categories': [
                      'Json.NET',
                      'CodePlex'
                    ]
                  },
                  {
                    'title': 'LINQ to JSON beta',
                    'description': 'Announcing LINQ to JSON',
                    'link': 'http://james.newtonking.com/projects/json-net.aspx',
                    'categories': [
                      'Json.NET',
                      'LINQ'
                    ]
                  }
                ]
              }
            }
            """);

        #region LinqToJsonQuerying

        var postTitles =
            (
                from p in rss["channel"]["item"]
                select (string) p["title"]
            )
            .ToList();

        foreach (var item in postTitles)
        {
            Console.WriteLine(item);
        }

        //LINQ to JSON beta
        //Json.NET 1.3 + New license + Now on CodePlex

        var categories =
            (
                from c in rss["channel"]["item"]
                    .SelectMany(_ => _["categories"])
                    .Values<string>()
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

        foreach (var c in categories)
        {
            Console.WriteLine($"{c.Category} - Count: {c.Count}");
        }

        //Json.NET - Count: 2
        //LINQ - Count: 1
        //CodePlex - Count: 1

        #endregion

        Assert.Equal(2, postTitles.Count);
        Assert.Equal(3, categories.Count);
    }

    #region LinqToJsonDeserializeObject

    public class Shortie
    {
        public string Original { get; set; }
        public string Shortened { get; set; }
        public string Short { get; set; }
        public ShortieException Error { get; set; }
    }

    public class ShortieException
    {
        public int Code { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion

    [Fact]
    public void LinqToJsonDeserializeExample()
    {
        #region LinqToJsonDeserializeExample

        var jsonText = """
                       {
                         'short': {
                           'original': 'http://www.foo.com/',
                           'short': 'krehqk',
                           'error': {
                             'code': 0,
                             'msg': 'No action taken'
                           }
                         }
                       }
                       """;

        var json = JObject.Parse(jsonText);

        var shortie = new Shortie
        {
            Original = (string) json["short"]["original"],
            Short = (string) json["short"]["short"],
            Error = new()
            {
                Code = (int) json["short"]["error"]["code"],
                ErrorMessage = (string) json["short"]["error"]["msg"]
            }
        };

        Console.WriteLine(shortie.Original);
        // http://www.foo.com/

        Console.WriteLine(shortie.Error.ErrorMessage);
        // No action taken

        #endregion

        Assert.Equal("http://www.foo.com/", shortie.Original);
        Assert.Equal("No action taken", shortie.Error.ErrorMessage);
    }

    [Fact]
    public void SelectTokenSimple()
    {
        var o = JObject.Parse(
            """
            {
              'Stores': [
                'Lambton Quay',
                'Willis Street'
              ],
              'Manufacturers': [
                {
                  'Name': 'Acme Co',
                  'Products': [
                    {
                      'Name': 'Anvil',
                      'Price': 50
                    }
                  ]
                },
                {
                  'Name': 'Contoso',
                  'Products': [
                    {
                      'Name': 'Elbow Grease',
                      'Price': 99.95
                    },
                    {
                      'Name': 'Headlight Fluid',
                      'Price': 4
                    }
                  ]
                }
              ]
            }
            """);

        #region SelectTokenSimple

        var name = (string) o.SelectToken("Manufacturers[0].Name");

        #endregion

        Assert.Equal("Acme Co", name);
    }

    [Fact]
    public void SelectTokenComplex()
    {
        #region SelectTokenComplex

        var o = JObject.Parse(
            """
            {
              'Stores': [
                'Lambton Quay',
                'Willis Street'
              ],
              'Manufacturers': [
                {
                  'Name': 'Acme Co',
                  'Products': [
                    {
                      'Name': 'Anvil',
                      'Price': 50
                    }
                  ]
                },
                {
                  'Name': 'Contoso',
                  'Products': [
                    {
                      'Name': 'Elbow Grease',
                      'Price': 99.95
                    },
                    {
                      'Name': 'Headlight Fluid',
                      'Price': 4
                    }
                  ]
                }
              ]
            }
            """);

        var name = (string) o.SelectToken("Manufacturers[0].Name");
        // Acme Co

        var productPrice = (decimal) o.SelectToken("Manufacturers[0].Products[0].Price");
        // 50

        var productName = (string) o.SelectToken("Manufacturers[1].Products[0].Name");
        // Elbow Grease

        #endregion

        Assert.Equal("Acme Co", name);
        Assert.Equal(50m, productPrice);
        Assert.Equal("Elbow Grease", productName);
    }

    [Fact]
    public void SelectTokenLinq()
    {
        var o = JObject.Parse(
            """
            {
              'Stores': [
                'Lambton Quay',
                'Willis Street'
              ],
              'Manufacturers': [
                {
                  'Name': 'Acme Co',
                  'Products': [
                    {
                      'Name': 'Anvil',
                      'Price': 50
                    }
                  ]
                },
                {
                  'Name': 'Contoso',
                  'Products': [
                    {
                      'Name': 'Elbow Grease',
                      'Price': 99.95
                    },
                    {
                      'Name': 'Headlight Fluid',
                      'Price': 4
                    }
                  ]
                }
              ]
            }
            """);

        #region SelectTokenLinq

        var storeNames = o
            .SelectToken("Stores")!
            .Select(s => (string) s)
            .ToList();
        // Lambton Quay
        // Willis Street

        var firstProductNames = o["Manufacturers"]!
            .Select(m => (string) m.SelectToken("Products[1].Name"))
            .ToList();
        // null
        // Headlight Fluid

        var totalPrice = o["Manufacturers"]
            .Sum(m => (decimal) m.SelectToken("Products[0].Price"));
        // 149.95

        #endregion

        Assert.Equal(2, storeNames.Count);
        Assert.Equal(2, firstProductNames.Count);
        // the prices are read from JSON as doubles, and converting a double to decimal is
        // correctly rounded from .NET 11 on, so the sum carries the binary expansion of 99.95
        Assert.Equal(149.95m, Math.Round(totalPrice, 2));
    }
}