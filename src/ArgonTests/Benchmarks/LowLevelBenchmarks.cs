// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class WriteEscapedJavaScriptString
{
    [Benchmark]
    public void Run()
    {
        var text = """
                   The general form of an HTML element is therefore: <tag attribute1="value1" attribute2="value2">content</tag>.
                   Some HTML elements are defined as empty elements and take the form <tag attribute1="value1" attribute2="value2" >.
                   Empty elements may enclose no content, for instance, the BR tag or the inline IMG tag.
                   The name of an HTML element is the name used in the tags.
                   Note that the end tag's name is preceded by a slash character, "/", and that in empty elements the end tag is neither required nor allowed.
                   If attributes are not mentioned, default values are used in each case.

                   The general form of an HTML element is therefore: <tag attribute1="value1" attribute2="value2">content</tag>.
                   Some HTML elements are defined as empty elements and take the form <tag attribute1="value1" attribute2="value2" >.
                   Empty elements may enclose no content, for instance, the BR tag or the inline IMG tag.
                   The name of an HTML element is the name used in the tags.
                   Note that the end tag's name is preceded by a slash character, "/", and that in empty elements the end tag is neither required nor allowed.
                   If attributes are not mentioned, default values are used in each case.

                   The general form of an HTML element is therefore: <tag attribute1="value1" attribute2="value2">content</tag>.
                   Some HTML elements are defined as empty elements and take the form <tag attribute1="value1" attribute2="value2" >.
                   Empty elements may enclose no content, for instance, the BR tag or the inline IMG tag.
                   The name of an HTML element is the name used in the tags.
                   Note that the end tag's name is preceded by a slash character, "/", and that in empty elements the end tag is neither required nor allowed.
                   If attributes are not mentioned, default values are used in each case.

                   """;

        using var writer = StringUtils.CreateStringWriter(text.Length);
        char[] buffer = null;
        JavaScriptUtils.WriteEscapedJavaScriptString(writer, text.AsSpan(), '"', true, JavaScriptUtils.DoubleQuoteEscapeFlags, EscapeHandling.Default, ref buffer);
    }
}