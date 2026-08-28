// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using Formatting = Argon.Formatting;

public class NodaInstantConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        Converters = {NodaConverters.InstantConverter},
    };

    [Fact]
    public void Serialize_NonNullableType()
    {
        var instant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var json = JsonConvert.SerializeObject(instant, Formatting.None, settings);
        var expectedJson = "\"2012-01-02T03:04:05Z\"";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Serialize_NullableType_NonNullValue()
    {
        Instant? instant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        var json = JsonConvert.SerializeObject(instant, Formatting.None, settings);
        var expectedJson = "\"2012-01-02T03:04:05Z\"";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Serialize_NullableType_NullValue()
    {
        var json = JsonConvert.SerializeObject(null, Formatting.None, settings);
        var expectedJson = "null";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Deserialize_ToNonNullableType()
    {
        var json = "\"2012-01-02T03:04:05Z\"";
        var instant = JsonConvert.DeserializeObject<Instant>(json, settings);
        var expectedInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        Assert.Equal(expectedInstant, instant);
    }

    [Fact]
    public void Deserialize_ToNullableType_NonNullValue()
    {
        var json = "\"2012-01-02T03:04:05Z\"";
        var instant = JsonConvert.DeserializeObject<Instant?>(json, settings);
        Instant? expectedInstant = Instant.FromUtc(2012, 1, 2, 3, 4, 5);
        Assert.Equal(expectedInstant, instant);
    }

    [Fact]
    public void Deserialize_ToNullableType_NullValue()
    {
        var json = "null";
        var instant = JsonConvert.TryDeserializeObject<Instant?>(json, settings);
        Assert.Null(instant);
    }

    [Fact]
    public void Serialize_EquivalentToIsoDateTimeConverter()
    {
        var dateTime = new DateTime(2012, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var instant = Instant.FromDateTimeUtc(dateTime);
        var jsonDateTime = JsonConvert.SerializeObject(dateTime, new IsoDateTimeConverter());
        var jsonInstant = JsonConvert.SerializeObject(instant, Formatting.None, settings);
        Assert.Equal(jsonDateTime, jsonInstant);
    }
}