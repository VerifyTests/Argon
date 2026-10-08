// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

static class Base64Encoder
{
    const int Base64LineSize = 76;
    const int LineSizeInBytes = 57;
#if NET6_0_OR_GREATER
    public static void WriteBase64(this TextWriter writer, ReadOnlySpan<byte> buffer)
    {
        var index = 0;
        Span<char> target = stackalloc char[Base64LineSize];
        do
        {
            var min = Math.Min(LineSizeInBytes, buffer.Length - index);
            var slice = buffer.Slice(index, min);
            Convert.TryToBase64Chars(slice, target, out var charsWritten);
            writer.Write(target[..charsWritten]);
            index += LineSizeInBytes;
        } while (index < buffer.Length);
    }
#else
    public static void WriteBase64(this TextWriter writer, ReadOnlySpan<byte> buffer)
    {
        var charsLine = new char[Base64LineSize];
        var bytesLine = ArrayPool<byte>.Shared.Rent(LineSizeInBytes);
        try
        {
            var index = 0;
            do
            {
                var min = Math.Min(LineSizeInBytes, buffer.Length - index);
                buffer.Slice(index, min).CopyTo(bytesLine);
                var written = Convert.ToBase64CharArray(bytesLine, 0, min, charsLine, 0);
                writer.Write(charsLine, 0, written);
                index += LineSizeInBytes;
            } while (index < buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytesLine);
        }
    }
#endif
}