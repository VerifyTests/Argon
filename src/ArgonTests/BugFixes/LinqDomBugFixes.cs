// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class LinqDomBugFixes : TestFixtureBase
{
    // Bug: CompareBigInteger used Math.Abs on the fractional part, discarding its sign and
    // inverting comparisons when the other operand had a negative fraction.

    [Fact]
    public void BigInteger_compares_correctly_against_negative_fraction()
    {
        // -5 > -5.5
        Assert.True(new JValue(BigInteger.Parse("-5")).CompareTo(new(-5.5)) > 0);
        Assert.True(new JValue(-5.5).CompareTo(new(BigInteger.Parse("-5"))) < 0);

        // 5 < 5.5 (positive fraction still correct)
        Assert.True(new JValue(BigInteger.Parse("5")).CompareTo(new(5.5)) < 0);
        Assert.True(new JValue(5.5).CompareTo(new(BigInteger.Parse("5"))) > 0);

        // equal integers
        Assert.Equal(0, new JValue(BigInteger.Parse("5")).CompareTo(new(5.0)));
    }

    // Bug: GetHashCode/GetDeepHashCode hashed the boxed CLR value, so numerically-equal
    // JValues with different backing types produced different hashes, breaking hash-based
    // lookups through JToken.EqualityComparer.

    [Fact]
    public void Equal_numeric_values_have_equal_hashcodes()
    {
        var intBacked = (JValue) JToken.FromObject(-1);
        var longBacked = new JValue(-1L);
        Assert.True(intBacked.Equals(longBacked));
        Assert.Equal(intBacked.GetHashCode(), longBacked.GetHashCode());

        var decimalBacked = new JValue(1.5m);
        var doubleBacked = new JValue(1.5d);
        Assert.True(decimalBacked.Equals(doubleBacked));
        Assert.Equal(decimalBacked.GetHashCode(), doubleBacked.GetHashCode());
    }

    [Fact]
    public void EqualityComparer_matches_across_numeric_representations()
    {
        var comparer = JToken.EqualityComparer;
        var intBacked = JToken.FromObject(-1);
        var longBacked = new JValue(-1L);

        Assert.True(comparer.Equals(intBacked, longBacked));
        Assert.Equal(comparer.GetHashCode(intBacked), comparer.GetHashCode(longBacked));
    }

    // Bug: TryAddInternal incremented the insert index even when the child insert was skipped
    // (e.g. a comment token in a JObject), so the next insert ran past the end and threw.

    [Fact]
    public void Add_multicontent_with_comment_does_not_throw()
    {
        var o = new JObject();
        object[] content =
        [
            new JProperty("p1", 1),
            JValue.CreateComment("c"),
            new JProperty("p2", 2)
        ];
        o.Add(content);

        Assert.Equal(1, (int) o["p1"]);
        Assert.Equal(2, (int) o["p2"]);
    }

    [Fact]
    public void JObject_ctor_with_leading_comment_does_not_throw()
    {
        var o = new JObject(JValue.CreateComment("c"), new JProperty("a", 1));

        Assert.Equal(1, (int) o["a"]);
    }

    // Bug: JObject's explicit IDictionary<string, JToken>.Values threw NotImplementedException.

    [Fact]
    public void IDictionary_Values_returns_property_values()
    {
        IDictionary<string, JToken> dictionary = new JObject
        {
            ["a"] = 1,
            ["b"] = 2
        };

        var values = dictionary.Values
            .Select(_ => (int) _)
            .OrderBy(_ => _)
            .ToList();

        Assert.Equal(new[] {1, 2}, values);
    }
}
