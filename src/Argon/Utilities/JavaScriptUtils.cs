// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class JavaScriptUtils
{
    internal static readonly bool[] SingleQuoteEscapeFlags = new bool[128];
    internal static readonly bool[] DoubleQuoteEscapeFlags = new bool[128];
    static readonly bool[] htmlEscapeFlags = new bool[128];
    static readonly bool[] noEscapeFlags = new bool[128];

    const int unicodeTextLength = 6;

    static JavaScriptUtils()
    {
        var escapeChars = new List<char>
        {
            '\n', '\r', '\t', '\\', '\f', '\b'
        };
        for (var i = 0; i < ' '; i++)
        {
            escapeChars.Add((char) i);
        }

        foreach (var escapeChar in escapeChars.Union(['\'']))
        {
            SingleQuoteEscapeFlags[escapeChar] = true;
        }

        foreach (var escapeChar in escapeChars.Union(['"']))
        {
            DoubleQuoteEscapeFlags[escapeChar] = true;
        }

        foreach (var escapeChar in escapeChars.Union(['"', '\'', '<', '>', '&']))
        {
            htmlEscapeFlags[escapeChar] = true;
        }
    }

    const string escapedUnicodeText = "!";

    public static bool[] GetCharEscapeFlags(EscapeHandling escapeHandling, char quoteChar)
    {
        if (escapeHandling == EscapeHandling.None)
        {
            return noEscapeFlags;
        }

        if (escapeHandling == EscapeHandling.EscapeHtml)
        {
            return htmlEscapeFlags;
        }

        if (quoteChar == '"')
        {
            return DoubleQuoteEscapeFlags;
        }

        return SingleQuoteEscapeFlags;
    }

    public static bool ShouldEscapeJavaScriptString(string? s)
    {
        if (s == null)
        {
            return false;
        }

        foreach (var ch in s)
        {
            if (ch >= htmlEscapeFlags.Length || htmlEscapeFlags[ch])
            {
                return true;
            }
        }

        return false;
    }

    public static void WriteEscapedJavaScriptString(TextWriter writer, CharSpan value, char delimiter, bool appendDelimiters, bool[] escapeFlags, EscapeHandling escapeHandling, ref char[]? buffer)
    {
        // leading delimiter
        if (appendDelimiters)
        {
            writer.Write(delimiter);
        }

        if (value.Length > 0)
        {
            WriteEscapedJavaScriptNonNullString(writer, value, escapeFlags, escapeHandling, ref buffer);
        }

        // trailing delimiter
        if (appendDelimiters)
        {
            writer.Write(delimiter);
        }
    }

    static void WriteEscapedJavaScriptNonNullString(TextWriter writer, CharSpan value, bool[] escapeFlags, EscapeHandling escapeHandling, ref char[]? buffer)
    {
        if (escapeHandling == EscapeHandling.None)
        {
            writer.Write(value);
            return;
        }

        var lastWritePosition = FirstCharToEscape(value, escapeFlags, escapeHandling);
        if (lastWritePosition == -1)
        {
            writer.Write(value);
            return;
        }

        if (lastWritePosition != 0)
        {
            if (buffer == null || buffer.Length < lastWritePosition)
            {
                buffer = BufferUtils.EnsureBufferSize(lastWritePosition, buffer);
            }

            // write unchanged chars at start of text.
            value.Slice(0, lastWritePosition).CopyTo(buffer);
            writer.Write(buffer, 0, lastWritePosition);
        }

        int length;
        for (var i = lastWritePosition; i < value.Length; i++)
        {
            var c = value[i];

            if (c < escapeFlags.Length && !escapeFlags[c])
            {
                continue;
            }

            string? escapedValue;

            switch (c)
            {
                case '\t':
                    escapedValue = @"\t";
                    break;
                case '\n':
                    escapedValue = @"\n";
                    break;
                case '\r':
                    escapedValue = @"\r";
                    break;
                case '\f':
                    escapedValue = @"\f";
                    break;
                case '\b':
                    escapedValue = @"\b";
                    break;
                case '\\':
                    escapedValue = @"\\";
                    break;
                case '\u0085': // Next Line
                    escapedValue = @"\u0085";
                    break;
                case '\u2028': // Line Separator
                    escapedValue = @"\u2028";
                    break;
                case '\u2029': // Paragraph Separator
                    escapedValue = @"\u2029";
                    break;
                default:
                    if (c >= escapeFlags.Length && escapeHandling != EscapeHandling.EscapeNonAscii)
                    {
                        escapedValue = null;
                        break;
                    }

                    if (escapeHandling != EscapeHandling.EscapeHtml)
                    {
                        if (c == '\'')
                        {
                            escapedValue = @"\'";
                            break;
                        }

                        if (c == '"')
                        {
                            escapedValue = "\\\"";
                            break;
                        }
                    }

                    if (buffer == null || buffer.Length < unicodeTextLength)
                    {
                        buffer = BufferUtils.EnsureBufferSize(unicodeTextLength, buffer);
                    }

                    StringUtils.ToCharAsUnicode(c, buffer);

                    // slightly hacky but it saves multiple conditions in if test
                    escapedValue = escapedUnicodeText;

                    break;
            }

            if (escapedValue == null)
            {
                continue;
            }

            // Safe to use ReferenceEquals: escapedValue is either null (handled above),
            // a string literal from the switch branches, or the escapedUnicodeText sentinel
            // assigned directly at line 199. No other branch produces a string equal to "!".
            var isEscapedUnicodeText = ReferenceEquals(escapedValue, escapedUnicodeText);

            if (i > lastWritePosition)
            {
                length = i - lastWritePosition + (isEscapedUnicodeText ? unicodeTextLength : 0);
                var start = isEscapedUnicodeText ? unicodeTextLength : 0;

                if (buffer == null || buffer.Length < length)
                {
                    var newBuffer = BufferUtils.RentBuffer(length);

                    // the unicode text is already in the buffer
                    // copy it over when creating new buffer
                    if (isEscapedUnicodeText)
                    {
                        MiscellaneousUtils.Assert(buffer != null, "Write buffer should never be null because it is set when the escaped unicode text is encountered.");

                        Array.Copy(buffer, newBuffer, unicodeTextLength);
                    }

                    BufferUtils.ReturnBuffer(buffer);

                    buffer = newBuffer;
                }

                var count = length - start;
                value.Slice(lastWritePosition, count).CopyTo(buffer.AsSpan(start: start, length: count));

                // write unchanged chars before writing escaped text
                writer.Write(buffer, start, count);
            }

            lastWritePosition = i + 1;
            if (isEscapedUnicodeText)
            {
                writer.Write(buffer!, 0, unicodeTextLength);
            }
            else
            {
                writer.Write(escapedValue);
            }
        }

        MiscellaneousUtils.Assert(lastWritePosition != 0);
        length = value.Length - lastWritePosition;
        if (length > 0)
        {
            if (buffer == null || buffer.Length < length)
            {
                buffer = BufferUtils.EnsureBufferSize(length, buffer);
            }

            value.Slice(lastWritePosition, length).CopyTo(buffer);

            // write remaining text
            writer.Write(buffer, 0, length);
        }
    }

    public static string ToEscapedJavaScriptString(CharSpan value, char delimiter, bool appendDelimiters, EscapeHandling escapeHandling)
    {
        var escapeFlags = GetCharEscapeFlags(escapeHandling, delimiter);

        using var w = StringUtils.CreateStringWriter(value.Length);
        char[]? buffer = null;
        WriteEscapedJavaScriptString(w, value, delimiter, appendDelimiters, escapeFlags, escapeHandling, ref buffer);
        return w.ToString();
    }

    static int FirstCharToEscape(CharSpan value, bool[] escapeFlags, EscapeHandling escapeHandling)
    {
        for (var i = 0; i != value.Length; i++)
        {
            var c = value[i];

            if (c < escapeFlags.Length)
            {
                if (escapeFlags[c])
                {
                    return i;
                }
            }
            else if (escapeHandling == EscapeHandling.EscapeNonAscii)
            {
                return i;
            }
            else
            {
                switch (c)
                {
                    case '\u0085':
                    case '\u2028':
                    case '\u2029':
                        return i;
                }
            }
        }

        return -1;
    }
}