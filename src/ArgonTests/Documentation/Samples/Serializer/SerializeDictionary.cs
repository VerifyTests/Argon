// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeDictionary : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region SerializeDictionary

        var points = new Dictionary<string, int>
        {
            {"James", 9001},
            {"Jo", 3474},
            {"Jess", 11926}
        };

        var json = JsonConvert.SerializeObject(points, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "James": 9001,
        //   "Jo": 3474,
        //   "Jess": 11926
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "James": 9001,
              "Jo": 3474,
              "Jess": 11926
            }
            """,
            json);
    }
}