// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

/// <summary>
/// Json.NET converter for <see cref="DateInterval"/>.
/// </summary>
sealed class NodaIsoDateIntervalConverter :
    NodaConverterBase<DateInterval>
{
    protected override DateInterval ReadJsonImpl(JsonReader reader, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String)
        {
            throw new InvalidNodaDataException(
                $"Unexpected token parsing DateInterval. Expected String, got {reader.TokenType}.");
        }
        var text = reader.StringValue;
        var slash = text.IndexOf('/');
        if (slash == -1)
        {
            throw new InvalidNodaDataException("Expected ISO-8601-formatted date interval; slash was missing.");
        }

        var startText = text[..slash];
        if (startText == "")
        {
            throw new InvalidNodaDataException("Expected ISO-8601-formatted date interval; start date was missing.");
        }

        var endText = text[(slash + 1)..];
        if (endText == "")
        {
            throw new InvalidNodaDataException("Expected ISO-8601-formatted date interval; end date was missing.");
        }

        var pattern = LocalDatePattern.Iso;
        var start = pattern.Parse(startText).Value;
        var end = pattern.Parse(endText).Value;

        return [with(start, end)];
    }

    /// <summary>
    /// Serializes the date interval as start/end dates.
    /// </summary>
    /// <param name="writer">The writer to write JSON to</param>
    /// <param name="value">The date interval to serialize</param>
    /// <param name="serializer">The serializer for embedded serialization.</param>
    protected override void WriteJsonImpl(JsonWriter writer, DateInterval value, JsonSerializer serializer)
    {
        var pattern = LocalDatePattern.Iso;
        var text = pattern.Format(value.Start) + "/" + pattern.Format(value.End);
        writer.WriteValue(text);
    }
}
