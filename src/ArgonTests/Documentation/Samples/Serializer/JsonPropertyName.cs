// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class JsonPropertyName : TestFixtureBase
{
    #region JsonPropertyNameTypes

    public class Videogame
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("release_date")] public DateTime ReleaseDate { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonPropertyNameUsage

        var starcraft = new Videogame
        {
            Name = "Starcraft",
            ReleaseDate = new(1998, 1, 1)
        };

        var json = JsonConvert.SerializeObject(starcraft, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "name": "Starcraft",
        //   "release_date": "1998-01-01T00:00:00"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "name": "Starcraft",
              "release_date": "1998-01-01T00:00:00"
            }
            """,
            json);
    }
}