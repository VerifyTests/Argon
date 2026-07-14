// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JTokenEqualityComparerTests : TestFixtureBase
{
    [Fact]
    public void MatchesAcrossNumericRepresentations()
    {
        var comparer = JToken.EqualityComparer;
        var intBacked = JToken.FromObject(-1);
        var longBacked = new JValue(-1L);

        Assert.True(comparer.Equals(intBacked, longBacked));
        Assert.Equal(comparer.GetHashCode(intBacked), comparer.GetHashCode(longBacked));
    }

    [Fact]
    public void CharAndStringHashConsistently()
    {
        var charValue = new JValue('a');
        var stringValue = new JValue("a");

        Assert.True(charValue.Equals(stringValue));

        var comparer = JToken.EqualityComparer;
        Assert.Equal(comparer.GetHashCode(charValue), comparer.GetHashCode(stringValue));

        var set = new HashSet<JToken>(comparer) {charValue};
        Assert.Contains(stringValue, set);
    }

    [Fact]
    public void CompareEmptyProperties()
    {
        var o1 = JObject.Parse("{}");
        o1.Add(new JProperty("hi"));

        var o2 = JObject.Parse("{}");
        o2.Add(new JProperty("hi"));

        var c = new JTokenEqualityComparer();
        Assert.True(c.Equals(o1, o2));

        o1["hi"] = 10;
        Assert.False(c.Equals(o1, o2));
    }

    [Fact]
    public void JValueDictionary()
    {
        var dic = new Dictionary<JToken, int>(JToken.EqualityComparer);
        var v11 = new JValue(1);
        var v12 = new JValue(1);

        dic[v11] = 1;
        dic[v12] += 1;
        Assert.Equal(2, dic[v11]);
    }

    [Fact]
    public void JArrayDictionary()
    {
        var dic = new Dictionary<JToken, int>(JToken.EqualityComparer);
        var v11 = new JArray();
        var v12 = new JArray();

        dic[v11] = 1;
        dic[v12] += 1;
        Assert.Equal(2, dic[v11]);
    }

    [Fact]
    public void JObjectDictionary()
    {
        var dic = new Dictionary<JToken, int>(JToken.EqualityComparer);
        var v11 = new JObject {{"Test", new JValue(1)}, {"Test1", new JValue(1)}};
        var v12 = new JObject {{"Test", new JValue(1)}, {"Test1", new JValue(1)}};

        dic[v11] = 1;
        dic[v12] += 1;
        Assert.Equal(2, dic[v11]);
    }
}