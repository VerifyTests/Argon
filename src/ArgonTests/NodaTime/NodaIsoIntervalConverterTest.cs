// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using static TestHelper;
using Formatting = Argon.Formatting;

/// <summary>
/// The same tests as NodaIntervalConverterTest, but using the ISO-based interval converter.
/// </summary>
public class NodaIsoIntervalConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        Converters =
        {
            NodaConverters.IsoIntervalConverter
        }
    };

    [Fact]
    public void RoundTrip()
    {
        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5) + Duration.FromMilliseconds(670);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10) + Duration.FromNanoseconds(123456789);
        var interval = new Interval(startInstant, endInstant);
        AssertConversions(interval, "\"2012-01-02T03:04:05.67Z/2013-06-07T08:09:10.123456789Z\"", settings);
    }

    [Fact]
    public void RoundTrip_Infinite()
    {
        var instant = Instant.FromUtc(2013, 6, 7, 8, 9, 10) + Duration.FromNanoseconds(123456789);
        AssertConversions(new Interval(null, instant), "\"/2013-06-07T08:09:10.123456789Z\"", settings);
        AssertConversions(new Interval(instant, null), "\"2013-06-07T08:09:10.123456789Z/\"", settings);
        AssertConversions(new Interval(null, null), "\"/\"", settings);
    }

    [Fact]
    public void DeserializeComma()
    {
        // Comma is deliberate, to show that we can parse a comma decimal separator too.
        var json = "\"2012-01-02T03:04:05.670Z/2013-06-07T08:09:10,1234567Z\"";

        var interval = JsonConvert.DeserializeObject<Interval>(json, settings);

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5) + Duration.FromMilliseconds(670);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10) + Duration.FromTicks(1234567);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, interval);
    }

    [Theory]
    [InlineData("\"2012-01-02T03:04:05Z2013-06-07T08:09:10Z\"")]
    public void InvalidJson(string json) =>
        AssertInvalidJson<Interval>(json, settings);

    [Fact]
    public void Serialize_InObject()
    {
        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var interval = new Interval(startInstant, endInstant);

        var testObject = new TestObject {Interval = interval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settings);

        var expectedJson = "{\"Interval\":\"2012-01-02T03:04:05Z/2013-06-07T08:09:10Z\"}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize_InObject()
    {
        var json = "{\"Interval\":\"2012-01-02T03:04:05Z/2013-06-07T08:09:10Z\"}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settings);

        var interval = testObject.Interval;

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, interval);
    }

    public class TestObject
    {
        public Interval Interval { get; set; }
    }
}