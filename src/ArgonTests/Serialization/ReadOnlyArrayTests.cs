// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ReadOnlyArrayTests : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        IEnumerable<Target> objects =
        [
            new()
            {
                Property = "Value0"
            },
            new()
            {
                Property = "Value1"
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
            }).ToArray();

        Assert.Equal("Value0", result[0].Property);
        Assert.Equal("Value1", result[1].Property);
        Assert.Equal(2, result.Length);
    }

    class Target
    {
        public string Property { get; set; }
    }
}