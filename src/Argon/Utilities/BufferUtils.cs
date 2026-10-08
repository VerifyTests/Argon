// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

static class BufferUtils
{
    public static char[] RentBuffer(int size) =>
        ArrayPool<char>.Shared.Rent(size);

    public static void ReturnBuffer(char[]? buffer)
    {
        if (buffer != null)
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    public static char[] EnsureBufferSize(int size, char[]? buffer)
    {
        if (buffer != null)
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        return ArrayPool<char>.Shared.Rent(size);
    }
}