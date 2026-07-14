// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class DateTimeUtilsTests : TestFixtureBase
{
    [Fact]
    public void RoundTripDateTimeMinAndMax()
    {
        RoundtripDateIso(DateTime.MinValue);
        RoundtripDateIso(DateTime.MaxValue);
    }

    static StringReference CreateStringReference(string s) =>
        new(s.ToCharArray(), 0, s.Length);

    static void RoundtripDateIso(DateTime value)
    {
        var stringWriter = new StringWriter();
        DateTimeUtils.WriteDateTimeString(stringWriter, value);
        var minDateText = stringWriter.ToString();

        DateTimeUtils.TryParseDateTime(minDateText, out var parsedDt);

        Assert.Equal(value, parsedDt);
    }

#if NET8_0_OR_GREATER

    [Fact]
    public void DatesShouldBeTheSameAsStj()
    {
        var target = new
        {
            dateTimeLocal = new DateTime(2000, 10, 10, 1, 1, 1, DateTimeKind.Local),
            dateTimeNow = DateTime.Now,
            dateTimeUtcNow = DateTime.UtcNow,
            dateTimeUtc = new DateTime(2000, 10, 10, 1, 1, 1, DateTimeKind.Utc),
            dateTimeUnspecified = new DateTime(2000, 10, 10, 1, 1, 1, DateTimeKind.Unspecified),
            dateTimeOffsetNow = DateTimeOffset.Now,
            dateTimeOffsetUtcNow = DateTimeOffset.UtcNow
        };
        var resultStj = System.Text.Json.JsonSerializer.Serialize(target);

        var result = JsonConvert.SerializeObject(target);
        Assert.Equal(result, resultStj);
    }

#endif

    [Fact]
    public void Parse24HourDateTimeOffset()
    {
        Assert.True(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-12-15T24:00:00Z"), out var dt));
        Assert.Equal(new(2000, 12, 16, 0, 0, 0, TimeSpan.Zero), dt);

        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-12-15T24:01:00Z"), out dt));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-12-15T24:00:01Z"), out dt));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-12-15T24:00:00.0000001Z"), out dt));
    }

    // the ISO date parser accepts zone offsets up to +/-99:99, then the DateTimeOffset
    // constructor threw instead of the parse returning false.
    [Fact]
    public void TryParseDateTimeOffsetIsoRejectsOutOfRangeOffset()
    {
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-01-01T00:00:00+15:00"), out _));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-01-01T00:00:00-15:00"), out _));
        Assert.False(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-01-01T00:00:00+13:99"), out _));

        // +/-14:00 is the maximum offset DateTimeOffset allows.
        Assert.True(DateTimeUtils.TryParseDateTimeOffsetIso(CreateStringReference("2000-01-01T00:00:00+14:00"), out _));
    }
}