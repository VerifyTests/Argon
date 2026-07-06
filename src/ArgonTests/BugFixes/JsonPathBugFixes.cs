// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JsonPathBugFixes : TestFixtureBase
{
    // Bug: FieldMultipleFilter threw for ErrorWhenNoMatch unconditionally (missing else), so
    // $['a','b'] errored even when the properties existed.

    [Fact]
    public void SelectTokens_multiple_existing_fields_does_not_error()
    {
        var o = JObject.Parse("""{"a":1,"b":2}""");

        var values = o.SelectTokens("$['a','b']", errorWhenNoMatch: true)
            .Select(_ => (int) _)
            .ToList();

        Assert.Equal(new[] {1, 2}, values);
    }

    [Fact]
    public void SelectTokens_multiple_with_missing_field_still_errors()
    {
        var o = JObject.Parse("""{"a":1}""");

        Assert.Throws<JsonException>(() =>
            o.SelectTokens("$['a','missing']", errorWhenNoMatch: true).ToList());
    }

    // Bug: filter numeric literals were only terminated by space or ')', so && / || directly
    // after a number failed to parse.

    [Fact]
    public void Filter_with_no_spaces_around_logical_and()
    {
        var a = JArray.Parse("""[{"a":1,"b":2},{"a":1,"b":9}]""");

        var matches = a.SelectTokens("$[?(@.a==1&&@.b==2)]").ToList();

        Assert.Single(matches);
        Assert.Equal(2, (int) matches[0]["b"]);
    }

    [Fact]
    public void Filter_with_no_spaces_around_logical_or()
    {
        var a = JArray.Parse("""[{"a":1},{"a":5},{"a":9}]""");

        var matches = a.SelectTokens("$[?(@.a==1||@.a==5)]").ToList();

        Assert.Equal(2, matches.Count);
    }

    // Bug: GetTokenIndex had no negative-index guard, so $[-1] crashed with
    // ArgumentOutOfRangeException instead of returning null / throwing JsonException.

    [Fact]
    public void Negative_array_index_returns_null()
    {
        var a = new JArray(1, 2, 3);

        Assert.Null(a.SelectToken("$[-1]"));
    }

    [Fact]
    public void Negative_array_index_with_error_throws_jsonexception()
    {
        var a = new JArray(1, 2, 3);

        Assert.Throws<JsonException>(() => a.SelectToken("$[-1]", errorWhenNoMatch: true));
    }
}
