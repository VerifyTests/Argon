// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

readonly struct StringReference(char[] chars, int startIndex, int length)
{
    public char this[int i] => Chars[i];

    public char[] Chars { get; } = chars;

    public int StartIndex { get; } = startIndex;

    public int Length { get; } = length;

    public ReadOnlySpan<char> AsSpan() =>
        Chars.AsSpan(StartIndex, Length);

    public override string ToString() =>
        new(Chars, StartIndex, Length);
}