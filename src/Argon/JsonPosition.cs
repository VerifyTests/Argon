// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

enum JsonContainerType
{
    None = 0,
    Object = 1,
    Array = 2
}

struct JsonPosition(JsonContainerType type)
{
    internal JsonContainerType Type = type;
    internal int Position = -1;
    internal string? PropertyName = null;
    internal bool HasIndex = TypeHasIndex(type);

    int CalculateLength() =>
        Type switch
        {
            JsonContainerType.Object => PropertyName!.Length + 5,
            JsonContainerType.Array => MathUtils.IntLength((ulong) Position) + 2,
            _ => throw new ArgumentOutOfRangeException(nameof(Type))
        };

    #if NET8_0_OR_GREATER
    static readonly SearchValues<char> specialCharacters = SearchValues.Create(". '/\"[]()\t\n\r\f\b\\\u0085\u2028\u2029");
    #else
    static readonly char[] specialCharacters = ['.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t', '\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029'];
    #endif
    void WriteTo(StringBuilder builder, ref StringWriter? writer, ref char[]? buffer)
    {
        switch (Type)
        {
            case JsonContainerType.Object:
                var propertyName = PropertyName!.AsSpan();
                if (propertyName.IndexOfAny(specialCharacters) != -1)
                {
                    builder.Append("['");

                    writer ??= new(builder);

                    JavaScriptUtils.WriteEscapedJavaScriptString(writer, propertyName, '\'', false, JavaScriptUtils.SingleQuoteEscapeFlags, EscapeHandling.Default, ref buffer);

                    builder.Append("']");
                }
                else
                {
                    if (builder.Length > 0)
                    {
                        builder.Append('.');
                    }

                    builder.Append(propertyName);
                }

                break;
            case JsonContainerType.Array:
                builder.Append('[');
                builder.Append(Position);
                builder.Append(']');
                break;
        }
    }

    static bool TypeHasIndex(JsonContainerType type) =>
        type is JsonContainerType.Array;

    internal static string BuildPath(List<JsonPosition> positions, JsonPosition? currentPosition)
    {
        var capacity = 0;
        for (var i = 0; i < positions.Count; i++)
        {
            capacity += positions[i].CalculateLength();
        }

        if (currentPosition != null)
        {
            capacity += currentPosition.GetValueOrDefault().CalculateLength();
        }

        var builder = new StringBuilder(capacity);
        StringWriter? writer = null;
        char[]? buffer = null;
        try
        {
            foreach (var state in positions)
            {
                state.WriteTo(builder, ref writer, ref buffer);
            }

            currentPosition?.WriteTo(builder, ref writer, ref buffer);

            return builder.ToString();
        }
        finally
        {
            // WriteTo rents a pooled buffer when escaping property names with special characters; return it
            BufferUtils.ReturnBuffer(buffer);
        }
    }

    internal static string FormatMessage(IJsonLineInfo? lineInfo, string path, string message)
    {
        // don't add a fullstop and space when message ends with a new line
        if (!message.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            message = message.Trim();

            if (!message.EndsWith('.'))
            {
                message += '.';
            }

            message += ' ';
        }

        message += $"Path '{path}'";

        if (lineInfo != null && lineInfo.HasLineInfo())
        {
            message += $", line {lineInfo.LineNumber}, position {lineInfo.LinePosition}";
        }

        message += '.';

        return message;
    }
}