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

#if NET8_0_OR_GREATER
        // must run after the flag arrays are populated (field initializers would run first)
        singleQuoteSearchValues = BuildSearchValues(SingleQuoteEscapeFlags);
        doubleQuoteSearchValues = BuildSearchValues(DoubleQuoteEscapeFlags);
        htmlSearchValues = BuildSearchValues(htmlEscapeFlags);
#endif
    }

    const string escapedUnicodeText = "!";

#if NET8_0_OR_GREATER
    static readonly SearchValues<char> singleQuoteSearchValues;
    static readonly SearchValues<char> doubleQuoteSearchValues;
    static readonly SearchValues<char> htmlSearchValues;

    static SearchValues<char> BuildSearchValues(bool[] flags)
    {
        var chars = new List<char>();
        for (var i = 0; i < flags.Length; i++)
        {
            if (flags[i])
            {
                chars.Add((char) i);
            }
        }

        // high chars FirstCharToEscape treats as escapes outside the flag range
        chars.Add('\u0085');
        chars.Add('\u2028');
        chars.Add('\u2029');
        return SearchValues.Create(chars.ToArray());
    }

    static SearchValues<char>? TryGetSearchValues(bool[] escapeFlags)
    {
        if (ReferenceEquals(escapeFlags, DoubleQuoteEscapeFlags))
        {
            return doubleQuoteSearchValues;
        }

        if (ReferenceEquals(escapeFlags, SingleQuoteEscapeFlags))
        {
            return singleQuoteSearchValues;
        }

        if (ReferenceEquals(escapeFlags, htmlEscapeFlags))
        {
            return htmlSearchValues;
        }

        return null;
    }
#endif

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
            // write unchanged chars at start of text.
            writer.Write(value.Slice(0, lastWritePosition));
        }

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
                // write unchanged chars before writing escaped text
                writer.Write(value.Slice(lastWritePosition, i - lastWritePosition));
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
        if (value.Length - lastWritePosition > 0)
        {
            // write remaining text
            writer.Write(value.Slice(lastWritePosition));
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
#if NET8_0_OR_GREATER
        // vectorized scan for the fixed escape sets; the scalar loop stays for short
        // strings (vector setup costs more than it saves there) and for EscapeNonAscii,
        // where every non-ascii char is an escape target
        if (value.Length >= 16 &&
            escapeHandling != EscapeHandling.EscapeNonAscii)
        {
            var searchValues = TryGetSearchValues(escapeFlags);
            if (searchValues != null)
            {
                return value.IndexOfAny(searchValues);
            }
        }
#endif
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
