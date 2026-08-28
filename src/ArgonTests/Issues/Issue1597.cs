// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1597 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var json = """
            {
                "wish": 264,
                "collect": 7498,
                "doing": 385,
                "on_hold": 285,
                "dropped": 221
            }
            """;

        var o = JsonConvert.DeserializeObject<IReadOnlyDictionary<CollectionStatus, int>>(json);

        Assert.Equal(264, o[CollectionStatus.Wish]);
        Assert.Equal(7498, o[CollectionStatus.Collect]);
        Assert.Equal(385, o[CollectionStatus.Doing]);
        Assert.Equal(285, o[CollectionStatus.OnHold]);
        Assert.Equal(221, o[CollectionStatus.Dropped]);
    }

    [Fact]
    public void Test_WithNumbers()
    {
        var json = """
            {
                "0": 264,
                "1": 7498,
                "2": 385,
                "3": 285,
                "4": 221
            }
            """;

        var o = JsonConvert.DeserializeObject<IReadOnlyDictionary<CollectionStatus, int>>(json);

        Assert.Equal(264, o[CollectionStatus.Wish]);
        Assert.Equal(7498, o[CollectionStatus.Collect]);
        Assert.Equal(385, o[CollectionStatus.Doing]);
        Assert.Equal(285, o[CollectionStatus.OnHold]);
        Assert.Equal(221, o[CollectionStatus.Dropped]);
    }

    [Fact]
    public void Test_Serialize()
    {
        var o = new Dictionary<CollectionStatus, int>
        {
            [CollectionStatus.Wish] = 264,
            [CollectionStatus.Collect] = 7498,
            [CollectionStatus.Doing] = 385,
            [CollectionStatus.OnHold] = 285,
            [CollectionStatus.Dropped] = 221,
            [(CollectionStatus) int.MaxValue] = int.MaxValue
        };

        var json = JsonConvert.SerializeObject(o, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Wish": 264,
              "Collect": 7498,
              "Doing": 385,
              "on_hold": 285,
              "Dropped": 221,
              "2147483647": 2147483647
            }
            """,
            json);
    }

    public enum CollectionStatus
    {
        Wish,
        Collect,
        Doing,
        [EnumMember(Value = "on_hold")]
        OnHold,
        Dropped
    }
}