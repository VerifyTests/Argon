// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

static class JsonTokenUtils
{
    internal static bool IsEndToken(this JsonToken token) =>
        token is JsonToken.EndObject or JsonToken.EndArray;

    internal static int EndTokenOffset(this JsonToken token)
    {
        if (token is JsonToken.EndObject or JsonToken.EndArray)
        {
            return 1;
        }

        return 0;
    }

    internal static bool IsStartToken(this JsonToken token) =>
        token is JsonToken.StartObject or JsonToken.StartArray;

    internal static bool IsPrimitiveToken(JsonToken token)
    {
        switch (token)
        {
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.String:
            case JsonToken.Boolean:
            case JsonToken.Undefined:
            case JsonToken.Null:
            case JsonToken.Date:
            case JsonToken.Bytes:
                return true;
            default:
                return false;
        }
    }
}