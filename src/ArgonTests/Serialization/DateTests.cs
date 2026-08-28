// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if NET6_0_OR_GREATER

using Formatting = Argon.Formatting;

public class DateTests : TestFixtureBase
{
    [Fact]
    public void Serialize()
    {
        var d = new Date(2000, 12, 29);
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        Assert.Equal(
            """
            "2000-12-29"
            """,
            json);
    }

    [Fact]
    public void SerializeDefault()
    {
        Date d = default;
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        Assert.Equal(
            """
            "0001-01-01"
            """,
            json);
    }

    [Fact]
    public void SerializeMaxValue()
    {
        var d = Date.MaxValue;
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        Assert.Equal(
            """
            "9999-12-31"
            """,
            json);
    }

    [Fact]
    public void SerializeMinValue()
    {
        var d = Date.MinValue;
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        Assert.Equal(
            """
            "0001-01-01"
            """,
            json);
    }

    [Fact]
    public void SerializeNullable_Null()
    {
        var json = JsonConvert.SerializeObject(null, Formatting.Indented);

        Assert.Equal("null", json);
    }

    [Fact]
    public void SerializeNullable_Value()
    {
        Date? d = new Date(2000, 12, 29);
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        Assert.Equal(
            """
            "2000-12-29"
            """,
            json);
    }

    [Fact]
    public Task SerializeList()
    {
        var d = new List<Date>
        {
            new(2000, 12, 29)
        };
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        return VerifyJson(json);
    }

    [Fact]
    public Task SerializeList_Nullable()
    {
        var d = new List<Date?>
        {
            new Date(2000, 12, 29),
            null
        };
        var json = JsonConvert.SerializeObject(d, Formatting.Indented);

        return VerifyJson(json);
    }

    [Fact]
    public void Deserialize()
    {
        var d = JsonConvert.DeserializeObject<Date>(
            """
            "2000-12-29"
            """);

        Assert.Equal(new(2000, 12, 29), d);
    }

    [Fact]
    public void DeserializeDefault()
    {
        var d = JsonConvert.DeserializeObject<Date>(
            """
            "0001-01-01"
            """);

        Assert.Equal(default, d);
    }

    [Fact]
    public void DeserializeMaxValue()
    {
        var d = JsonConvert.DeserializeObject<Date>(
            """
            "9999-12-31"
            """);

        Assert.Equal(Date.MaxValue, d);
    }

    [Fact]
    public void DeserializeMinValue()
    {
        var d = JsonConvert.DeserializeObject<Date>(
            """
            "0001-01-01"
            """);

        Assert.Equal(Date.MinValue, d);
    }

    [Fact]
    public void DeserializeNullable_Null()
    {
        var d = JsonConvert.TryDeserializeObject<Date?>("null");

        Assert.Null(d);
    }

    [Fact]
    public void DeserializeNullable_Value()
    {
        var d = JsonConvert.DeserializeObject<Date?>(
            """
            "2000-12-29"
            """);

        Assert.Equal(new Date(2000, 12, 29), d);
    }

    [Fact]
    public void DeserializeList()
    {
        var l = JsonConvert.DeserializeObject<IList<Date>>(
            """
            [
                "2000-12-29"
            ]
            """);

        Assert.Single(l);
        Assert.Equal(new(2000, 12, 29), l[0]);
    }

    [Fact]
    public void DeserializeList_Nullable()
    {
        var l = JsonConvert.DeserializeObject<IList<Date?>>(
            """
            [
                "2000-12-29",
                null
            ]
            """);

        Assert.Equal(2, l.Count);
        Assert.Equal(new Date(2000, 12, 29), l[0]);
        Assert.Null(l[1]);
    }
}
#endif
