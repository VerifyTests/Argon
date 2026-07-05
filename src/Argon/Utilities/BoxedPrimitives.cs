// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class BoxedPrimitives
{
    internal static object Get(bool value) => value ? BooleanTrue : BooleanFalse;

    internal static readonly object BooleanTrue = true;
    internal static readonly object BooleanFalse = false;

    internal static object Get(int value) => value switch
    {
        -1 => Int32_M1,
        0 => Int32_0,
        1 => Int32_1,
        2 => Int32_2,
        3 => Int32_3,
        4 => Int32_4,
        5 => Int32_5,
        6 => Int32_6,
        7 => Int32_7,
        8 => Int32_8,
        _ => value,
    };

    // integers tend to be weighted towards a handful of low numbers; we could argue
    // for days over the "correct" range to have special handling, but I'm arbitrarily
    // mirroring the same decision as the IL opcodes, which has M1 thru 8
    static readonly object Int32_M1 = -1;
    static readonly object Int32_0 = 0;
    static readonly object Int32_1 = 1;
    static readonly object Int32_2 = 2;
    static readonly object Int32_3 = 3;
    static readonly object Int32_4 = 4;
    static readonly object Int32_5 = 5;
    static readonly object Int32_6 = 6;
    static readonly object Int32_7 = 7;
    static readonly object Int32_8 = 8;

    internal static object Get(long value) => value switch
    {
        -1 => Int64_M1,
        0 => Int64_0,
        1 => Int64_1,
        2 => Int64_2,
        3 => Int64_3,
        4 => Int64_4,
        5 => Int64_5,
        6 => Int64_6,
        7 => Int64_7,
        8 => Int64_8,
        _ => value,
    };

    static readonly object Int64_M1 = -1L;
    static readonly object Int64_0 = 0L;
    static readonly object Int64_1 = 1L;
    static readonly object Int64_2 = 2L;
    static readonly object Int64_3 = 3L;
    static readonly object Int64_4 = 4L;
    static readonly object Int64_5 = 5L;
    static readonly object Int64_6 = 6L;
    static readonly object Int64_7 = 7L;
    static readonly object Int64_8 = 8L;

    internal static object Get(decimal value)
    {
        // Decimals can contain trailing zeros. For example 1 vs 1.0. Unfortunately, Equals doesn't check for trailing zeros.
        // There isn't a way to find out if a decimal has trailing zeros in older frameworks without calling ToString.
        // Don't provide a cached boxed decimal value in older frameworks.

#if NET6_0_OR_GREATER
        // Number of bits scale is shifted by.
        const int ScaleShift = 16;

        if (value == decimal.Zero)
        {
            Span<int> bits = stackalloc int[4];
            decimal.GetBits(value, bits);
            // Preserve the sign of negative zero (-0.0m). The cached boxes are positive zero,
            // so only use them for positive zero - consistent across scales and with Get(double).
            if ((bits[3] & int.MinValue) == 0)
            {
                var scale = (byte) (bits[3] >> ScaleShift);
                // Only use cached boxed value if value is zero and there is zero or one trailing zeros.
                if (scale == 0)
                {
                    return DecimalZero;
                }

                if (scale == 1)
                {
                    return DecimalZeroWithTrailingZero;
                }
            }
        }
#endif

        return value;
    }

#if NET6_0_OR_GREATER
    static readonly object DecimalZero = decimal.Zero;
    static readonly object DecimalZeroWithTrailingZero = 0.0m;
#endif

    internal static object Get(double value)
    {
        if (value == 0.0d)
        {
            // Double supports -0.0. Detection logic from https://stackoverflow.com/a/4739883/11829.
            if (double.IsNegativeInfinity(1.0 / value))
            {
                return DoubleNegativeZero;
            }

            return DoubleZero;
        }

        if (double.IsInfinity(value))
        {
            if (double.IsPositiveInfinity(value))
            {
                return DoublePositiveInfinity;
            }

            return DoubleNegativeInfinity;
        }

        if (double.IsNaN(value))
        {
            return DoubleNaN;
        }

        return value;
    }

    internal static readonly object DoubleNaN = double.NaN;
    internal static readonly object DoublePositiveInfinity = double.PositiveInfinity;
    internal static readonly object DoubleNegativeInfinity = double.NegativeInfinity;
    internal static readonly object DoubleZero = 0.0d;
    internal static readonly object DoubleNegativeZero = -0.0d;
}