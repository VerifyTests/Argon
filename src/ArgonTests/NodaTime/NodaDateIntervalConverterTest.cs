// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using static TestHelper;
using Formatting = Argon.Formatting;

public class NodaDateIntervalConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        Converters = {NodaConverters.DateIntervalConverter, NodaConverters.LocalDateConverter},
    };

    readonly JsonSerializerSettings settingsCamelCase = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = {NodaConverters.DateIntervalConverter, NodaConverters.LocalDateConverter},
    };

    [Fact]
    public void RoundTrip()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);
        AssertConversions(dateInterval, "{\"Start\":\"2012-01-02\",\"End\":\"2013-06-07\"}", settings);
    }

    [Fact]
    public void RoundTrip_CamelCase()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);
        AssertConversions(dateInterval, "{\"start\":\"2012-01-02\",\"end\":\"2013-06-07\"}", settingsCamelCase);
    }

    [Fact]
    public void Serialize_InObject()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);

        var testObject = new TestObject {Interval = dateInterval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settings);

        var expectedJson = "{\"Interval\":{\"Start\":\"2012-01-02\",\"End\":\"2013-06-07\"}}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Serialize_InObject_CamelCase()
    {
        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var dateInterval = new DateInterval(startLocalDate, endLocalDate);

        var testObject = new TestObject {Interval = dateInterval};

        var json = JsonConvert.SerializeObject(testObject, Formatting.None, settingsCamelCase);

        var expectedJson = "{\"interval\":{\"start\":\"2012-01-02\",\"end\":\"2013-06-07\"}}";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize_InObject()
    {
        var json = "{\"Interval\":{\"Start\":\"2012-01-02\",\"End\":\"2013-06-07\"}}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settings);

        var interval = testObject.Interval;

        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var expectedInterval = new DateInterval(startLocalDate, endLocalDate);
        Assert.Equal(expectedInterval, interval);
    }

    [Fact]
    public void Deserialize_InObject_CamelCase()
    {
        var json = "{\"interval\":{\"start\":\"2012-01-02\",\"end\":\"2013-06-07\"}}";

        var testObject = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var interval = testObject.Interval;

        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var expectedInterval = new DateInterval(startLocalDate, endLocalDate);
        Assert.Equal(expectedInterval, interval);
    }

    [Fact]
    public void Deserialize_CaseInsensitive()
    {
        var json = "{\"Interval\":{\"Start\":\"2012-01-02\",\"End\":\"2013-06-07\"}}";

        var testObjectPascalCase = JsonConvert.DeserializeObject<TestObject>(json, settings);
        var testObjectCamelCase = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var intervalPascalCase = testObjectPascalCase.Interval;
        var intervalCamelCase = testObjectCamelCase.Interval;

        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var expectedInterval = new DateInterval(startLocalDate, endLocalDate);
        Assert.Equal(expectedInterval, intervalPascalCase);
        Assert.Equal(expectedInterval, intervalCamelCase);
    }

    [Fact]
    public void Deserialize_CaseInsensitive_CamelCase()
    {
        var json = "{\"interval\":{\"start\":\"2012-01-02\",\"end\":\"2013-06-07\"}}";

        var testObjectPascalCase = JsonConvert.DeserializeObject<TestObject>(json, settings);
        var testObjectCamelCase = JsonConvert.DeserializeObject<TestObject>(json, settingsCamelCase);

        var intervalPascalCase = testObjectPascalCase.Interval;
        var intervalCamelCase = testObjectCamelCase.Interval;

        var startLocalDate = new LocalDate(2012, 1, 2);
        var endLocalDate = new LocalDate(2013, 6, 7);
        var expectedInterval = new DateInterval(startLocalDate, endLocalDate);
        Assert.Equal(expectedInterval, intervalPascalCase);
        Assert.Equal(expectedInterval, intervalCamelCase);
    }

    public class TestObject
    {
        public DateInterval Interval { get; set; }
    }
}