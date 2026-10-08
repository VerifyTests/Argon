// Copyright 2014 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

namespace Argon.NodaTime;

/// <summary>
/// Json.NET converter for <see cref="Interval"/>.
/// </summary>
sealed class NodaIsoIntervalConverter :
    NodaConverterBase<Interval>
{
    protected override Interval ReadJsonImpl(JsonReader reader, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String)
        {
            throw new InvalidNodaDataException(
                $"Unexpected token parsing Interval. Expected String, got {reader.TokenType}.");
        }

        var text = (string) reader.GetValue();
        var slash = text.IndexOf('/');
        if (slash == -1)
        {
            throw new InvalidNodaDataException("Expected ISO-8601-formatted interval; slash was missing.");
        }

        var startText = text[..slash];
        var endText = text[(slash + 1)..];
        var pattern = InstantPattern.ExtendedIso;
        var start = startText == "" ? (Instant?) null : pattern.Parse(startText).Value;
        var end = endText == "" ? (Instant?) null : pattern.Parse(endText).Value;

        return new(start, end);
    }

    /// <summary>
    /// Serializes the interval as start/end instants.
    /// </summary>
    /// <param name="writer">The writer to write JSON to</param>
    /// <param name="value">The interval to serialize</param>
    /// <param name="serializer">The serializer for embedded serialization.</param>
    protected override void WriteJsonImpl(JsonWriter writer, Interval value, JsonSerializer serializer)
    {
        var pattern = InstantPattern.ExtendedIso;
        var text = (value.HasStart ? pattern.Format(value.Start) : "") + "/" + (value.HasEnd ? pattern.Format(value.End) : "");
        writer.WriteValue(text);
    }
}