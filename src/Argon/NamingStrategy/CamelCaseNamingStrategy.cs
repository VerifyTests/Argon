// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// A camel case naming strategy.
/// </summary>
public class CamelCaseNamingStrategy :
    NamingStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CamelCaseNamingStrategy" /> class.
    /// </summary>
    /// <param name="processDictionaryKeys">
    /// A flag indicating whether dictionary keys should be processed.
    /// </param>
    /// <param name="overrideSpecifiedNames">
    /// A flag indicating whether explicitly specified property names should be processed,
    /// e.g. a property name customized with a <see cref="JsonPropertyAttribute" />.
    /// </param>
    public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
    {
        ProcessDictionaryKeys = processDictionaryKeys;
        OverrideSpecifiedNames = overrideSpecifiedNames;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CamelCaseNamingStrategy" /> class.
    /// </summary>
    public CamelCaseNamingStrategy()
    {
    }

    /// <summary>
    /// Resolves the specified property name.
    /// </summary>
    protected override string ResolvePropertyName(string name) =>
        ToCamelCase(name);

    internal static string ToCamelCase(string s)
    {
        if (s.IsNullOrEmpty() || !char.IsUpper(s[0]))
        {
            return s;
        }

        return string.Create(s.Length, s, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            for (var i = 0; i < span.Length; i++)
            {
                var ch = span[i];
                if (i == 1 && !char.IsUpper(ch))
                {
                    break;
                }

                var hasNext = i + 1 < span.Length;
                if (i > 0 && hasNext && !char.IsUpper(span[i + 1]))
                {
                    // if the next character is a space, which is not considered uppercase
                    // (otherwise we wouldn't be here...)
                    // we want to ensure that the following:
                    // 'FOO bar' is rewritten as 'foo bar', and not as 'foO bar'
                    // The code was written in such a way that the first word in uppercase
                    // ends when if finds an uppercase letter followed by a lowercase letter.
                    // now a ' ' (space, (char)32) is considered not upper
                    // but in that case we still want our current character to become lowercase
                    if (char.IsSeparator(span[i + 1]))
                    {
                        span[i] = char.ToLower(ch, InvariantCulture);
                    }

                    break;
                }

                span[i] = char.ToLower(ch, InvariantCulture);
            }
        });
    }
}