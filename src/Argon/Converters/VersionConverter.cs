// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Converts a <see cref="Version"/> to and from a string (e.g. <c>"1.2.3.4"</c>).
/// </summary>
public class VersionConverter :
    JsonConverter<Version>
{
    public override void WriteJson(JsonWriter writer, Version value, JsonSerializer serializer) =>
        writer.WriteValue(value.ToString());

    public override Version ReadJson(JsonReader reader, Type type, Version? existingValue, bool hasExisting, JsonSerializer serializer) =>
        new(reader.GetConverterString(type));
}