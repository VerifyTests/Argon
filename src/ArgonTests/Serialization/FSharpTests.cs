// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Microsoft.FSharp.Collections;
// ReSharper disable UnusedVariable

public class FSharpTests : TestFixtureBase
{
    static JsonConverter[] converters =
    [
        new FSharpListConverter(),
        new FSharpMapConverter()
    ];

    [Fact]
    public void List()
    {
        var l = ListModule.OfSeq(
        [
            1,
            2,
            3
        ]);

        var json = JsonConvert.SerializeObject(l, Formatting.Indented, converters);

        XUnitAssert.AreEqualNormalized(
            """
            [
              1,
              2,
              3
            ]
            """,
            json);

        var l2 = JsonConvert.DeserializeObject<FSharpList<int>>(json, converters);

        Assert.Equal(l.Length, l2.Length);
        Assert.Equal(l, l2);
    }

    [Fact]
    public void Set()
    {
        var l = SetModule.OfSeq(
        [
            1,
            2,
            3
        ]);

        var json = JsonConvert.SerializeObject(l, Formatting.Indented, converters);

        XUnitAssert.AreEqualNormalized(
            """
            [
              1,
              2,
              3
            ]
            """,
            json);

        var l2 = JsonConvert.DeserializeObject<FSharpSet<int>>(json, converters);

        Assert.Equal(l.Count, l2.Count);
        Assert.Equal(l, l2);
    }

    [Fact]
    public void Map()
    {
        var m1 = MapModule.OfSeq(
            [
                Tuple.Create("one", 1),
                Tuple.Create("II", 2),
                Tuple.Create("3", 3)
            ]);

        var json = JsonConvert.SerializeObject(m1, Formatting.Indented, converters);

        var m2 = JsonConvert.DeserializeObject<FSharpMap<string, int>>(json, converters);

        Assert.Equal(m1.Count, m2.Count);
        Assert.Equal(1, m2["one"]);
        Assert.Equal(2, m2["II"]);
        Assert.Equal(3, m2["3"]);
    }

    // ReSharper disable once UnusedMember.Global
    void Converters(object target)
    {
        #region FSharpConvertersInstances

        var json = JsonConvert.SerializeObject(
            target,
            Formatting.Indented,
            FSharpConverters.Instances);

        var result = JsonConvert.DeserializeObject<Target>(
                         json,
                         FSharpConverters.Instances) ??
                     throw new ArgumentNullException("JsonConvert.DeserializeObject<Target>(json, FSharpConverters.Instances)");

        #endregion
    }

    class Target;

    [Fact]
    public void AddFSharpConverters()
    {
        #region AddFSharpConverters

        var settings = new JsonSerializerSettings();
        settings.AddFSharpConverters();

        #endregion
    }
}