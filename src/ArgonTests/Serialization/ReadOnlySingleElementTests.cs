// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class ReadOnlySingleElementTests : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        IEnumerable<Target> objects =
        [
            new()
            {
                Property = "Value"
            }
        ];

        var serializedData = JsonConvert.SerializeObject(
            objects,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            });

        var result = JsonConvert.DeserializeObject<IEnumerable<Target>>(
            serializedData,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });

        var o = result.First();

        Assert.Equal("Value", o.Property);
    }

    class Target
    {
        public string Property { get; set; }
    }
}