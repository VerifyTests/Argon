// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class CreateJsonAnonymousObject : TestFixtureBase
{
    #region CreateJsonAnonymousObjectTypes

    public class Post
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public IList<string> Categories { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region CreateJsonAnonymousObjectUsage

        var posts = new List<Post>
        {
            new()
            {
                Title = "Episode VII",
                Description = "Episode VII production",
                Categories =
                [
                    "episode-vii",
                    "movie"
                ],
                Link = "episode-vii-production.aspx"
            }
        };

        var o = JObject.FromObject(new
        {
            channel = new
            {
                title = "Star Wars",
                link = "http://www.starwars.com",
                description = "Star Wars blog.",
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

        Console.WriteLine(o.ToString());
        // {
        //   "channel": {
        //     "title": "Star Wars",
        //     "link": "http://www.starwars.com",
        //     "description": "Star Wars blog.",
        //     "item": [
        //       {
        //         "title": "Episode VII",
        //         "description": "Episode VII production",
        //         "link": "episode-vii-production.aspx",
        //         "category": [
        //           "episode-vii",
        //           "movie"
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
                "title": "Star Wars",
                "link": "http://www.starwars.com",
                "description": "Star Wars blog.",
                "item": [
                  {
                    "title": "Episode VII",
                    "description": "Episode VII production",
                    "link": "episode-vii-production.aspx",
                    "category": [
                      "episode-vii",
                      "movie"
                    ]
                  }
                ]
              }
            }
            """,
            o.ToString());
    }
}