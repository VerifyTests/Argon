// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using Argon.NodaTime;
using NodaTime;
using Formatting = Argon.Formatting;

public class NodaAnnualDateConverterTest
{
    readonly JsonSerializerSettings settings = new()
    {
        Converters = {NodaConverters.AnnualDateConverter},
    };

    [Fact]
    public void Serialize_NonNullableType()
    {
        var annualDate = new AnnualDate(07, 01);
        var json = JsonConvert.SerializeObject(annualDate, Formatting.None, settings);
        var expectedJson = "\"07-01\"";
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void Serialize_NullableType_NonNullValue()
    {
        AnnualDate? annualDate = new AnnualDate(07, 01);
        var json = JsonConvert.SerializeObject(annualDate, Formatting.None, settings);
        var expectedJson = "\"07-01\"";
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
        var json = "\"07-01\"";
        var annualDate = JsonConvert.DeserializeObject<AnnualDate>(json, settings);
        var expectedAnnualDate = new AnnualDate(07, 01);
        Assert.Equal(expectedAnnualDate, annualDate);
    }

    [Fact]
    public void Deserialize_ToNullableType_NonNullValue()
    {
        var json = "\"07-01\"";
        var annualDate = JsonConvert.DeserializeObject<AnnualDate?>(json, settings);
        AnnualDate? expectedAnnualDate = new AnnualDate(07, 01);
        Assert.Equal(expectedAnnualDate, annualDate);
    }

    [Fact]
    public void Deserialize_ToNullableType_NullValue()
    {
        var json = "null";
        var annualDate = JsonConvert.TryDeserializeObject<AnnualDate?>(json, settings);
        Assert.Null(annualDate);
    }
}