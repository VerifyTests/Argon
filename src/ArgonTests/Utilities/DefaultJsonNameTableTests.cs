// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class DefaultJsonNameTableTests : TestFixtureBase
{
    [Fact]
    public void ResolvesAllNamesAfterGrowth()
    {
        var table = new DefaultJsonNameTable();

        // more than the initial 32 buckets, forcing at least one Grow()
        var names = Enumerable.Range(0, 300)
            .Select(_ => $"property{_}")
            .ToList();

        foreach (var name in names)
        {
            Assert.Equal(name, table.Add(name));
        }

        foreach (var name in names)
        {
            var key = name.ToCharArray();
            Assert.Equal(name, table.Get(key, 0, key.Length));
        }
    }

    [Fact]
    public void IsThreadSafeUnderConcurrentAdd()
    {
        var table = new DefaultJsonNameTable();
        var names = Enumerable.Range(0, 400)
            .Select(_ => $"name{_}")
            .ToArray();

        // Add locks a dedicated object; before the fix it locked the entries array that Grow()
        // replaces, so concurrent adders spanning a resize could corrupt the shared table.
        Parallel.For(0, 32, _ =>
        {
            foreach (var name in names)
            {
                Assert.Equal(name, table.Add(name));
            }
        });

        foreach (var name in names)
        {
            var key = name.ToCharArray();
            Assert.Equal(name, table.Get(key, 0, key.Length));
        }
    }
}
