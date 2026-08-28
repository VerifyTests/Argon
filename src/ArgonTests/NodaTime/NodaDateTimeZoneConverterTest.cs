// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using NodaTime.TimeZones;
using Formatting = Argon.Formatting;

public class NodaDateTimeZoneConverterTest
{
    readonly JsonConverter converter = NodaConverters.CreateDateTimeZoneConverter(DateTimeZoneProviders.Tzdb);

    [Fact]
    public void Serialize()
    {
        var dateTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
        var json = JsonConvert.SerializeObject(dateTimeZone, Formatting.None, converter);
        var expectedJson = "\"America/Los_Angeles\"";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize()
    {
        var json = "\"America/Los_Angeles\"";
        var dateTimeZone = JsonConvert.DeserializeObject<DateTimeZone>(json, converter);
        var expectedDateTimeZone = DateTimeZoneProviders.Tzdb["America/Los_Angeles"];
        Assert.Equal(expectedDateTimeZone, dateTimeZone);
    }

    [Fact]
    public void Deserialize_TimeZoneNotFound()
    {
        var json = "\"America/DOES_NOT_EXIST\"";
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DateTimeZone>(json, converter));
        Assert.IsType<DateTimeZoneNotFoundException>(exception.InnerException);
    }
}