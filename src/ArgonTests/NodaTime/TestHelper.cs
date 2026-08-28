// Copyright 2015 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NodaTime.Utility;
using Formatting = Argon.Formatting;

static class TestHelper
{
    internal static void AssertConversions<T>(T value, string expectedJson, JsonConverter converter)
    {
        var settings = new JsonSerializerSettings
        {
            Converters = {converter}
        };
        AssertConversions(value, expectedJson, settings);
    }

    internal static void AssertConversions<T>(T value, string expectedJson, JsonSerializerSettings settings)
    {
        var actualJson = JsonConvert.SerializeObject(value, Formatting.None, settings);
        Assert.Equal(expectedJson, actualJson);

        var deserializedValue = JsonConvert.DeserializeObject<T>(expectedJson, settings);
        Assert.Equal(value, deserializedValue);
    }

    internal static void AssertInvalidJson<T>(string json, JsonSerializerSettings settings)
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<T>(json, settings));
        Assert.IsType<InvalidNodaDataException>(exception.InnerException);
    }
}