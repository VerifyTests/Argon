// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class CreateJsonDeclaratively : TestFixtureBase
{
    #region CreateJsonDeclarativelyTypes

    public class Post
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public IList<string> Categories { get; set; }
    }

    #endregion

    static List<Post> GetPosts() =>
    [
        new()
        {
            Title = "Title!",
            Categories =
            [
                "Category1"
            ],
            Description = "Description!",
            Link = "Link!"
        }
    ];

    [Fact]
    public void Example()
    {
        #region CreateJsonDeclarativelyUsage

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

        // {
        //   "channel": {
        //     "title": "James Newton-King",
        //     "link": "http://james.newtonking.com",
        //     "description": "James Newton-King's blog.",
        //     "item": [
        //       {
        //         "title": "Json.NET 1.3 + New license + Now on CodePlex",
        //         "description": "Announcing the release of Json.NET 1.3, the MIT license and being available on CodePlex",
        //         "link": "http://james.newtonking.com/projects/json-net.aspx",
        //         "category": [
        //           "Json.NET",
        //           "CodePlex"
        //         ]
        //       },
        //       {
        //         "title": "LINQ to JSON beta",
        //         "description": "Announcing LINQ to JSON",
        //         "link": "http://james.newtonking.com/projects/json-net.aspx",
        //         "category": [
        //           "Json.NET",
        //           "LINQ"
        //         ]
        //       }
        //     ]
        //   }
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "channel": {
                "title": "James Newton-King",
                "link": "http://james.newtonking.com",
                "description": "James Newton-King's blog.",
                "item": [
                  {
                    "title": "Title!",
                    "description": "Description!",
                    "link": "Link!",
                    "category": [
                      "Category1"
                    ]
                  }
                ]
              }
            }
            """,
            rss.ToString());
    }
}