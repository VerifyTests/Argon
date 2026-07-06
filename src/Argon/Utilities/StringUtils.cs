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
        // single allocation-free pass; runs per unmatched member for every object
        // deserialized through a parameterized constructor
        JsonProperty? caseInsensitiveMatch = null;
        var caseInsensitiveCount = 0;
        JsonProperty? caseSensitiveMatch = null;
        var caseSensitiveCount = 0;

        foreach (var property in source.List)
        {
            if (!string.Equals(property.PropertyName, testValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            caseInsensitiveCount++;
            caseInsensitiveMatch ??= property;

            if (string.Equals(property.PropertyName, testValue, StringComparison.Ordinal))
            {
                caseSensitiveCount++;
                caseSensitiveMatch ??= property;
            }
        }

        switch (caseInsensitiveCount)
        {
            case 0:
                return null;
            case 1:
                return caseInsensitiveMatch;
        }

        // multiple case-insensitive results. now filter using case sensitivity
        switch (caseSensitiveCount)
        {
            case 0:
                return null;
            case 1:
                return caseSensitiveMatch;
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

        // no upper-case char and no space means the transform is the identity:
        // lower chars, digits and separators are copied through verbatim.
        // Common for dictionary keys that are already snake/kebab cased.
        var needsTransform = false;
        foreach (var ch in s)
        {
            if (ch == ' ' || char.IsUpper(ch))
            {
                needsTransform = true;
                break;
            }
        }

        if (!needsTransform)
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