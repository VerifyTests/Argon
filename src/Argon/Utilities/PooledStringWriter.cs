// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

/// <summary>
/// A <see cref="TextWriter" /> that accumulates into a single pooled buffer.
/// </summary>
/// <remarks>
/// Serializing to a string went through a StringWriter, whose StringBuilder allocates a chain of
/// chunks as large as the finished text and then copies them into the string. Here the text is
/// built in a rented buffer, so the string is the only allocation that grows with the output.
/// </remarks>
sealed class PooledStringWriter() :
    TextWriter(InvariantCulture)
{
    // a buffer larger than this is dropped rather than returned, so that serializing one very
    // large document does not leave the pool holding on to its memory
    const int maxPooledLength = 1024 * 1024;

    // the longest array the runtime will allocate
    const int maxArrayLength = 0x7FFFFFC7;

    char[] buffer = BufferUtils.RentBuffer(1024);
    int length;

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        var chars = buffer;
        var position = length;
        if ((uint) position < (uint) chars.Length)
        {
            chars[position] = value;
            length = position + 1;
            return;
        }

        Grow(1);
        buffer[length++] = value;
    }

    public override void Write(char[] source, int index, int count)
    {
        if (count > buffer.Length - length)
        {
            Grow(count);
        }

        Array.Copy(source, index, buffer, length, count);
        length += count;
    }

    public override void Write(string? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.Length > buffer.Length - length)
        {
            Grow(value.Length);
        }

        value.CopyTo(0, buffer, length, value.Length);
        length += value.Length;
    }

#if NET6_0_OR_GREATER
    public override void Write(CharSpan value)
    {
        if (value.Length > buffer.Length - length)
        {
            Grow(value.Length);
        }

        value.CopyTo(buffer.AsSpan(length));
        length += value.Length;
    }
#endif

    void Grow(int needed)
    {
        var required = length + (long) needed;
        if (required > maxArrayLength)
        {
            throw new OutOfMemoryException();
        }

        // doubling keeps a long run of small appends from copying the text over and over
        var size = Math.Min(Math.Max(required, buffer.Length * 2L), maxArrayLength);
        var larger = BufferUtils.RentBuffer((int) size);
        Array.Copy(buffer, larger, length);
        Release(buffer);
        buffer = larger;
    }

    static void Release(char[] chars)
    {
        if (chars.Length is > 0 and <= maxPooledLength)
        {
            BufferUtils.ReturnBuffer(chars);
        }
    }

    public override string ToString() =>
        new(buffer, 0, length);

    protected override void Dispose(bool disposing)
    {
        Release(buffer);
        buffer = [];
        length = 0;
        base.Dispose(disposing);
    }
}
