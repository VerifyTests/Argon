// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1322 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("123", "2017-05-19T11:00:59")
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented);

        var v1 = JsonConvert.DeserializeObject<IList<KeyValuePair<string, string>>>(json);

        Assert.Equal("123", v1[0].Key);
        Assert.Equal("2017-05-19T11:00:59", v1[0].Value);
    }
}