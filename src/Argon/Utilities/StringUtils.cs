// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class StringUtils
{
    public const string CarriageReturnLineFeed = "\r\n";
    public const char CarriageReturn = '\r';
    public const char LineFeed = '\n';
    public const char Tab = '\t';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) =>
        string.IsNullOrEmpty(value);

    public static StringWriter CreateStringWriter(int capacity)
    {
        var stringBuilder = new StringBuilder(capacity);
        return new(stringBuilder, InvariantCulture);
    }

    public static void ToCharAsUnicode(char c, char[] buffer)
    {
        buffer[0] = '\\';
        buffer[1] = 'u';
        buffer[2] = MathUtils.IntToHex((c >> 12) & '\x000f');
        buffer[3] = MathUtils.IntToHex((c >> 8) & '\x000f');
        buffer[4] = MathUtils.IntToHex((c >> 4) & '\x000f');
        buffer[5] = MathUtils.IntToHex(c & '\x000f');
    }

    public static JsonProperty? ForgivingCaseSensitiveFind(this JsonPropertyCollection source, string testValue)
    {
        if (source.Count == 0)
        {
            return null;
        }

        var caseInsensitiveResults = source.Where(_ => string.Equals(_.PropertyName, testValue, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (caseInsensitiveResults.Length == 0)
        {
            return null;
        }

        if (caseInsensitiveResults.Length == 1)
        {
            return caseInsensitiveResults[0];
        }

        // multiple results returned. now filter using case sensitivity
        var caseSensitiveResults = source.Where(_ => string.Equals(_.PropertyName, testValue, StringComparison.Ordinal))
            .ToArray();
        if (caseSensitiveResults.Length == 0)
        {
            return null;
        }

        if (caseSensitiveResults.Length == 1)
        {
            return caseSensitiveResults[0];
        }

        throw new("Multiple matches found for testValue");
    }

    public static string ToSnakeCase(string s) =>
        ToSeparatedCase(s, '_');

    public static string ToKebabCase(string s) =>
        ToSeparatedCase(s, '-');

    enum SeparatedCaseState
    {
        Start,
        Lower,
        Upper,
        NewWord
    }

    static string ToSeparatedCase(string s, char separator)
    {
        if (IsNullOrEmpty(s))
        {
            return s;
        }

        var stringBuilder = new StringBuilder(s.Length + s.Length / 4);
        var state = SeparatedCaseState.Start;

        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == ' ')
            {
                if (state != SeparatedCaseState.Start)
                {
                    state = SeparatedCaseState.NewWord;
                }
            }
            else if (char.IsUpper(s[i]))
            {
                switch (state)
                {
                    case SeparatedCaseState.Upper:
                        var hasNext = i + 1 < s.Length;
                        if (i > 0 && hasNext)
                        {
                            var nextChar = s[i + 1];
                            if (!char.IsUpper(nextChar) && nextChar != separator)
                            {
                                stringBuilder.Append(separator);
                            }
                        }

                        break;
                    case SeparatedCaseState.Lower:
                    case SeparatedCaseState.NewWord:
                        stringBuilder.Append(separator);
                        break;
                }

                var c = char.ToLower(s[i], InvariantCulture);
                stringBuilder.Append(c);

                state = SeparatedCaseState.Upper;
            }
            else if (s[i] == separator)
            {
                stringBuilder.Append(separator);
                state = SeparatedCaseState.Start;
            }
            else
            {
                if (state == SeparatedCaseState.NewWord)
                {
                    stringBuilder.Append(separator);
                }

                stringBuilder.Append(s[i]);
                state = SeparatedCaseState.Lower;
            }
        }

        return stringBuilder.ToString();
    }

    public static string Trim(this string s, int start, int length)
    {
        // References: https://referencesource.microsoft.com/#mscorlib/system/string.cs,2691
        // https://referencesource.microsoft.com/#mscorlib/system/string.cs,1226
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var end = start + length - 1;
        if (end >= s.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        for (; start < end; start++)
        {
            if (!char.IsWhiteSpace(s[start]))
            {
                break;
            }
        }

        for (; end >= start; end--)
        {
            if (!char.IsWhiteSpace(s[end]))
            {
                break;
            }
        }

        return s.Substring(start, end - start + 1);
    }
}