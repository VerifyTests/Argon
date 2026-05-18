// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Buffers;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class WriteBase64Benchmark
{
    const int Base64LineSize = 76;
    const int LineSizeInBytes = 57;

    byte[] data = null!;

    [Params(57, 570, 5700, 57000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        data = new byte[Size];
        new Random(42).NextBytes(data);
    }

    [Benchmark(Baseline = true)]
    public void OldToArray()
    {
        using var writer = new StringWriter();
        WriteBase64Old(writer, data);
    }

    [Benchmark]
    public void NewArrayPool()
    {
        using var writer = new StringWriter();
        WriteBase64New(writer, data);
    }

    static void WriteBase64Old(TextWriter writer, ReadOnlySpan<byte> buffer)
    {
        var charsLine = new char[Base64LineSize];
        var index = 0;
        do
        {
            var min = Math.Min(LineSizeInBytes, buffer.Length - index);
            var slice = buffer.Slice(index, min);
            var written = Convert.ToBase64CharArray(slice.ToArray(), 0, min, charsLine, 0);
            writer.Write(charsLine, 0, written);
            index += LineSizeInBytes;
        } while (index < buffer.Length);
    }

    static void WriteBase64New(TextWriter writer, ReadOnlySpan<byte> buffer)
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
}
