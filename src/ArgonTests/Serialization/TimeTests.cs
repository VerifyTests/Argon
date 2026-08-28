#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using Formatting = Argon.Formatting;

#if NET6_0_OR_GREATER
public class TimeTests : TestFixtureBase
{
    [Fact]
    public void Serialize()
    {
        var t = new Time(23, 59, 59);
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "23:59:59"
            """,
            json);
    }

    [Fact]
    public void Serialize_Milliseconds()
    {
        var t = new Time(23, 59, 59, 999);
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "23:59:59.999"
            """,
            json);
    }

    [Fact]
    public void SerializeDefault()
    {
        Time t = default;
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "00:00:00"
            """,
            json);
    }

    [Fact]
    public void SerializeMaxValue()
    {
        var t = Time.MaxValue;
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "23:59:59.9999999"
            """,
            json);
    }

    [Fact]
    public void SerializeMinValue()
    {
        var t = Time.MinValue;
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "00:00:00"
            """,
            json);
    }

    [Fact]
    public void SerializeNullable_Null()
    {
        Time? t = default;
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal("null", json);
    }

    [Fact]
    public void SerializeNullable_Value()
    {
        Time? t = new Time(23, 59, 59, 999);
        var json = JsonConvert.SerializeObject(t, Formatting.Indented);

        Assert.Equal(
            """
            "23:59:59.999"
            """,
            json);
    }

    [Fact]
    public Task SerializeList()
    {
        var list = new List<Time>
        {
            new(23, 59, 59)
        };
        var json = JsonConvert.SerializeObject(list, Formatting.Indented);

        return VerifyJson(json);
    }

    [Fact]
    public Task SerializeList_Nullable()
    {
        var list = new List<Time?>
        {
            new Time(23, 59, 59),
            null
        };
        var json = JsonConvert.SerializeObject(list, Formatting.Indented);

        return VerifyJson(json);
    }

    [Fact]
    public void Deserialize()
    {
        var t = JsonConvert.DeserializeObject<Time>(
            """
            "23:59:59"
            """);

        Assert.Equal(new(23, 59, 59), t);
    }

    [Fact]
    public void DeserializeDefault()
    {
        var t = JsonConvert.DeserializeObject<Time>(
            """
            "00:00:00"
            """);

        Assert.Equal(default, t);
    }

    [Fact]
    public void DeserializeMaxValue()
    {
        var t = JsonConvert.DeserializeObject<Time>(
            """
            "23:59:59.9999999"
            """);

        Assert.Equal(Time.MaxValue, t);
    }

    [Fact]
    public void DeserializeMinValue()
    {
        var t = JsonConvert.DeserializeObject<Time>(
            """
            "00:00:00"
            """);

        Assert.Equal(Time.MinValue, t);
    }

    [Fact]
    public void DeserializeNullable_Null()
    {
        var t = JsonConvert.TryDeserializeObject<Time?>("null");

        Assert.Null(t);
    }

    [Fact]
    public void DeserializeNullable_Value()
    {
        var t = JsonConvert.DeserializeObject<Time?>(
            """
            "23:59:59"
            """);

        Assert.Equal(new Time(23, 59, 59), t);
    }

    [Fact]
    public void DeserializeList()
    {
        var l = JsonConvert.DeserializeObject<IList<Time>>(
            """
            [
                "23:59:59"
            ]
            """);

        Assert.Single(l);
        Assert.Equal(new(23, 59, 59), l[0]);
    }

    [Fact]
    public void DeserializeList_Nullable()
    {
        var l = JsonConvert.DeserializeObject<IList<Time?>>(
            """
            [
                "23:59:59",
                null
            ]
            """);

        Assert.Equal(2, l.Count);
        Assert.Equal(new Time(23, 59, 59), l[0]);
        Assert.Null(l[1]);
    }
}
#endif
