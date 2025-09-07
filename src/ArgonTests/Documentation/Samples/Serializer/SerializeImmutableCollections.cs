// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class SerializeImmutableCollections : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region SerializeImmutableCollections

        var l = ImmutableList.CreateRange(
        [
            "One",
            "II",
            "3"
        ]);

        var json = JsonConvert.SerializeObject(l, Formatting.Indented);
        // [
        //   "One",
        //   "II",
        //   "3"
        // ]

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            [
              "One",
              "II",
              "3"
            ]
            """,
            json);
    }
}