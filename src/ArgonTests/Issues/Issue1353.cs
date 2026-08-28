// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1353 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var d1 = new ConcurrentDictionary<string, string>();
        d1.TryAdd("key!", "value!");

        var json = JsonConvert.SerializeObject(d1, Formatting.Indented);

        var d2 = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(json);

        Assert.Single(d2);
        Assert.Equal("value!", d2["key!"]);
    }
}