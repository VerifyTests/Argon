// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using static TestHelper;
using Formatting = Argon.Formatting;

public class NodaIntervalConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        ContractResolver = new DefaultContractResolver(),
        Converters = {NodaConverters.IntervalConverter, NodaConverters.InstantConverter},
    };

    readonly JsonSerializerSettings settingsCamelCase = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = {NodaConverters.IntervalConverter, NodaConverters.InstantConverter},
    };

    [Fact]
    public void RoundTrip()
    {
        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5) + Duration.FromMilliseconds(670);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10) + Duration.FromNanoseconds(123456789);
        var interval = new Interval(startInstant, endInstant);
        AssertConversions(interval, "{\"Start\":\"2012-01-02T03:04:05.67Z\",\"End\":\"2013-06-07T08:09:10.123456789Z\"}", settings);
    }

    [Fact]
    public void RoundTrip_Infinite()
    {
        var instant = Instant.FromUtc(2013, 6, 7, 8, 9, 10) + Duration.FromNanoseconds(123456789);
        AssertConversions(new Interval(null, instant), "{\"End\":\"2013-06-07T08:09:10.123456789Z\"}", settings);
        AssertConversions(new Interval(instant, null), "{\"Start\":\"2013-06-07T08:09:10.123456789Z\"}", settings);
        AssertConversions(new Interval(null, null), "{}", settings);
    }

    [Fact]
    public void Serialize_InObject()
    {
        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var interval = new Interval(startInstant, endInstant);

        var testObject = new TestObject {Interval = interval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settings);

        var expectedJson = "{\"Interval\":{\"Start\":\"2012-01-02T03:04:05Z\",\"End\":\"2013-06-07T08:09:10Z\"}}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Serialize_InObject_CamelCase()
    {
        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var interval = new Interval(startInstant, endInstant);

        var testObject = new TestObject {Interval = interval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settingsCamelCase);

        var expectedJson = "{\"interval\":{\"start\":\"2012-01-02T03:04:05Z\",\"end\":\"2013-06-07T08:09:10Z\"}}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize_InObject()
    {
        var json = "{\"Interval\":{\"Start\":\"2012-01-02T03:04:05Z\",\"End\":\"2013-06-07T08:09:10Z\"}}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settings);

        var interval = testObject.Interval;

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, interval);
    }

    [Fact]
    public void Deserialize_InObject_CamelCase()
    {
        var json = "{\"interval\":{\"start\":\"2012-01-02T03:04:05Z\",\"end\":\"2013-06-07T08:09:10Z\"}}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var interval = testObject.Interval;

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, interval);
    }

    [Fact]
    public void Deserialize_CaseInsensitive()
    {
        var json = "{\"Interval\":{\"Start\":\"2012-01-02T03:04:05Z\",\"End\":\"2013-06-07T08:09:10Z\"}}";

        var testObjectPascalCase = JsonConvert.DeserializeObject<TestObject>(json, settings);
        var testObjectCamelCase = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var intervalPascalCase = testObjectPascalCase.Interval;
        var intervalCamelCase = testObjectCamelCase.Interval;

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, intervalPascalCase);
        Assert.Equal(expectedInterval, intervalCamelCase);
    }

    [Fact]
    public void Deserialize_CaseInsensitive_CamelCase()
    {
        var json = "{\"interval\":{\"start\":\"2012-01-02T03:04:05Z\",\"end\":\"2013-06-07T08:09:10Z\"}}";

        var testObjectPascalCase = JsonConvert.DeserializeObject<TestObject>(json, settings);
        var testObjectCamelCase = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var intervalPascalCase = testObjectPascalCase.Interval;
        var intervalCamelCase = testObjectCamelCase.Interval;

        var startInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var endInstant = Instant.FromUtc(2013, 6, 7, 8, 9, 10);
        var expectedInterval = new Interval(startInstant, endInstant);
        Assert.Equal(expectedInterval, intervalPascalCase);
        Assert.Equal(expectedInterval, intervalCamelCase);
    }

    public class TestObject
    {
        public Interval Interval { get; set; }
    }
}