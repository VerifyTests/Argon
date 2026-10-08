// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Builds a string. Unlike <see cref="System.Text.StringBuilder" /> this class lets you reuse its internal buffer.
/// </summary>
struct StringBuffer
{
    public int Position { get; set; }

    public bool IsEmpty => InternalBuffer == null;

    public StringBuffer(int initialSize) : this(BufferUtils.RentBuffer(initialSize))
    {
    }

    StringBuffer(char[] buffer)
    {
        InternalBuffer = buffer;
        Position = 0;
    }

    public void Append(char value)
    {
        // test if the buffer array is large enough to take the value
        if (Position == InternalBuffer!.Length)
        {
            EnsureSize(1);
        }

        // set value and increment position
        InternalBuffer![Position++] = value;
    }

    public void Append(char[] buffer, int startIndex, int count)
    {
        if (Position + count >= InternalBuffer!.Length)
        {
            EnsureSize(count);
        }

        Array.Copy(buffer, startIndex, InternalBuffer, Position, count);

        Position += count;
    }

    public void Clear()
    {
        if (InternalBuffer != null)
        {
            BufferUtils.ReturnBuffer(InternalBuffer);
            InternalBuffer = null;
        }

        Position = 0;
    }

    void EnsureSize(int appendLength)
    {
        var newBuffer = BufferUtils.RentBuffer((Position + appendLength) * 2);

        if (InternalBuffer != null)
        {
            Array.Copy(InternalBuffer, newBuffer, Position);
            BufferUtils.ReturnBuffer(InternalBuffer);
        }

        InternalBuffer = newBuffer;
    }

    public override string ToString() =>
        ToString(0, Position);

    public string ToString(int start, int length) =>
        // TODO: validation
        new(InternalBuffer!, start, length);

    public char[]? InternalBuffer { get; private set; }
}