// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if NET6_0_OR_GREATER

public class MemoryTests : TestFixtureBase
{
    public class Holder
    {
        public Memory<int> Numbers { get; set; }
        public ReadOnlyMemory<string> Names { get; set; }
        public Memory<byte> Bytes { get; set; }
        public ReadOnlyMemory<byte> ReadOnlyBytes { get; set; }
        public Memory<int>? Optional { get; set; }
    }

    [Fact]
    public void RoundTrip()
    {
        var holder = new Holder
        {
            Numbers = new[] {1, 2, 3},
            Names = new[] {"a", "b"},
            Bytes = new byte[] {1, 2, 3},
            ReadOnlyBytes = new byte[] {4, 5},
            Optional = new[] {9}
        };

        var json = JsonConvert.SerializeObject(holder);

        Assert.Equal("""{"Numbers":[1,2,3],"Names":["a","b"],"Bytes":"AQID","ReadOnlyBytes":"BAU=","Optional":[9]}""", json);

        var result = JsonConvert.DeserializeObject<Holder>(json);

        Assert.Equal(new[] {1, 2, 3}, result.Numbers.ToArray());
        Assert.Equal(new[] {"a", "b"}, result.Names.ToArray());
        Assert.Equal(new byte[] {1, 2, 3}, result.Bytes.ToArray());
        Assert.Equal(new byte[] {4, 5}, result.ReadOnlyBytes.ToArray());
        Assert.Equal(new[] {9}, result.Optional!.Value.ToArray());
    }

    [Fact]
    public void EmptyAndNull()
    {
        var json = JsonConvert.SerializeObject(new Holder());

        Assert.Equal("""{"Numbers":[],"Names":[],"Bytes":"","ReadOnlyBytes":"","Optional":null}""", json);

        var result = JsonConvert.DeserializeObject<Holder>(json);

        Assert.True(result.Numbers.IsEmpty);
        Assert.True(result.Bytes.IsEmpty);
        Assert.Null(result.Optional);
    }

    [Fact]
    public void OnlyTheSliceIsWritten()
    {
        var numbers = new[] {1, 2, 3, 4, 5}.AsMemory(1, 3);
        var bytes = new byte[] {1, 2, 3, 4, 5}.AsMemory(1, 3);

        Assert.Equal("[2,3,4]", JsonConvert.SerializeObject(numbers));
        Assert.Equal(
            """
            "AgME"
            """,
            JsonConvert.SerializeObject(bytes));
    }

    [Fact]
    public void RootValues()
    {
        Assert.Equal(new[] {1, 2, 3}, JsonConvert.DeserializeObject<Memory<int>>("[1,2,3]").ToArray());
        Assert.Equal(new[] {1, 2, 3}, JsonConvert.DeserializeObject<ReadOnlyMemory<int>>("[1,2,3]").ToArray());
        Assert.Equal(new byte[] {1, 2, 3}, JsonConvert.DeserializeObject<ReadOnlyMemory<byte>>("\"AQID\"").ToArray());
        // byte[] can be read from an array of numbers too
        Assert.Equal(new byte[] {1, 2, 3}, JsonConvert.DeserializeObject<Memory<byte>>("[1,2,3]").ToArray());
    }

    [Fact]
    public void ItemsUseTheirOwnContracts()
    {
        var memory = new Memory<Version>([new(1, 2), new(3, 4)]);

        var json = JsonConvert.SerializeObject(memory);

        Assert.Equal("""["1.2","3.4"]""", json);
        Assert.Equal(memory.ToArray(), JsonConvert.DeserializeObject<Memory<Version>>(json).ToArray());
    }

    [Fact]
    public void TypeNameHandlingRoundTrip()
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        var holder = new Holder
        {
            Numbers = new[] {1, 2},
            Bytes = new byte[] {1, 2, 3}
        };

        var json = JsonConvert.SerializeObject(holder, settings);
        var result = JsonConvert.DeserializeObject<Holder>(json, settings);

        Assert.Equal(new[] {1, 2}, result.Numbers.ToArray());
        Assert.Equal(new byte[] {1, 2, 3}, result.Bytes.ToArray());
    }
}

#endif
