// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

static class ConverterReaderExtensions
{
    // Reads the current token as a string, throwing a JsonSerializationException that carries the
    // JSON path/line info when the token is not a string. A direct (string)reader.Value cast would
    // otherwise surface a raw InvalidCastException (or null-reference) with no context.
    public static string GetConverterString(this JsonReader reader, Type targetType)
    {
        if (reader.Value is string value)
        {
            return value;
        }

        throw JsonSerializationException.Create(reader, $"Unexpected token {reader.TokenType} when parsing a {targetType.Name}. Expected a string.");
    }
}
