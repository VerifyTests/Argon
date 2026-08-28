// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using static TestHelper;
using Formatting = Argon.Formatting;

/// <summary>
/// The same tests as NodaDateIntervalConverterTest, but using the ISO-based interval converter.
/// </summary>
public class NodaIsoDateIntervalConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        Converters =
        {
            NodaConverters.IsoDateIntervalConverter,
            NodaConverters.LocalDateConverter
        }
    };

    [Fact]
    public void RoundTrip()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);
        AssertConversions(dateInterval, "\"2012-01-02/2013-06-07\"", settings);
    }

    [Theory]
    [InlineData("\"2012-01-022013-06-07\"")]
    public void InvalidJson(string json) =>
        AssertInvalidJson<DateInterval>(json, settings);

    [Fact]
    public void Serialize_InObject()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);

        var testObject = new TestObject {Interval = dateInterval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settings);

        var expectedJson = "{\"Interval\":\"2012-01-02/2013-06-07\"}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize_InObject()
    {
        var json = "{\"Interval\":\"2012-01-02/2013-06-07\"}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settings);

        var interval = testObject.Interval;

        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var expectedInterval = new DateInterval(startLocalDate, endLocalDate);
        Assert.Equal(expectedInterval, interval);
    }

    public class TestObject
    {
        public DateInterval Interval { get; set; }
    }
}