// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1574 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var c = new TestClass();
        var json = JsonConvert.SerializeObject(c, Formatting.Indented);

        Assert.Equal("{}", json);
    }

    public enum ServerType { STUN, TURN };

    public class TestClass
    {
        [JsonIgnore]
        public IEnumerable<ServerType> ServerTypes => Enum.GetValues(typeof(ServerType)).Cast<ServerType>();
    }
}