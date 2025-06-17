// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.


// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression

static class ConvertUtils
{
    static readonly FrozenDictionary<Type, PrimitiveTypeCode> TypeCodeMap =
        new Dictionary<Type, PrimitiveTypeCode>
        {
            {typeof(char), PrimitiveTypeCode.Char},
            {typeof(char?), PrimitiveTypeCode.CharNullable},
            {typeof(bool), PrimitiveTypeCode.Boolean},
            {typeof(bool?), PrimitiveTypeCode.BooleanNullable},
            {typeof(sbyte), PrimitiveTypeCode.SByte},
            {typeof(sbyte?), PrimitiveTypeCode.SByteNullable},
            {typeof(short), PrimitiveTypeCode.Int16},
            {typeof(short?), PrimitiveTypeCode.Int16Nullable},
            {typeof(ushort), PrimitiveTypeCode.UInt16},
            {typeof(ushort?), PrimitiveTypeCode.UInt16Nullable},
            {typeof(int), PrimitiveTypeCode.Int32},
            {typeof(int?), PrimitiveTypeCode.Int32Nullable},
            {typeof(byte), PrimitiveTypeCode.Byte},
            {typeof(byte?), PrimitiveTypeCode.ByteNullable},
            {typeof(uint), PrimitiveTypeCode.UInt32},
            {typeof(uint?), PrimitiveTypeCode.UInt32Nullable},
            {typeof(long), PrimitiveTypeCode.Int64},
            {typeof(long?), PrimitiveTypeCode.Int64Nullable},
            {typeof(ulong), PrimitiveTypeCode.UInt64},
            {typeof(ulong?), PrimitiveTypeCode.UInt64Nullable},
            {typeof(float), PrimitiveTypeCode.Single},
            {typeof(float?), PrimitiveTypeCode.SingleNullable},
            {typeof(double), PrimitiveTypeCode.Double},
            {typeof(double?), PrimitiveTypeCode.DoubleNullable},
            {typeof(DateTime), PrimitiveTypeCode.DateTime},
            {typeof(DateTime?), PrimitiveTypeCode.DateTimeNullable},
            {typeof(DateTimeOffset), PrimitiveTypeCode.DateTimeOffset},
            {typeof(DateTimeOffset?), PrimitiveTypeCode.DateTimeOffsetNullable},
            {typeof(decimal), PrimitiveTypeCode.Decimal},
            {typeof(decimal?), PrimitiveTypeCode.DecimalNullable},
            {typeof(Guid), PrimitiveTypeCode.Guid},
            {typeof(Guid?), PrimitiveTypeCode.GuidNullable},
            {typeof(TimeSpan), PrimitiveTypeCode.TimeSpan},
            {typeof(TimeSpan?), PrimitiveTypeCode.TimeSpanNullable},
            {typeof(BigInteger), PrimitiveTypeCode.BigInteger},
            {typeof(BigInteger?), PrimitiveTypeCode.BigIntegerNullable},
            {typeof(Uri), PrimitiveTypeCode.Uri},
            {typeof(string), PrimitiveTypeCode.String},
            {typeof(byte[]), PrimitiveTypeCode.Bytes},
            {typeof(DBNull), PrimitiveTypeCode.DBNull}
        }
        .ToFrozenDictionary();

    static readonly TypeInformation[] PrimitiveTypeCodes =
    [
        // need all of these. lookup against the index with TypeCode value
        new(typeof(object), PrimitiveTypeCode.Empty),
        new(typeof(object), PrimitiveTypeCode.Object),
        new(typeof(object), PrimitiveTypeCode.DBNull),
        new(typeof(bool), PrimitiveTypeCode.Boolean),
        new(typeof(char), PrimitiveTypeCode.Char),
        new(typeof(sbyte), PrimitiveTypeCode.SByte),
        new(typeof(byte), PrimitiveTypeCode.Byte),
        new(typeof(short), PrimitiveTypeCode.Int16),
        new(typeof(ushort), PrimitiveTypeCode.UInt16),
        new(typeof(int), PrimitiveTypeCode.Int32),
        new(typeof(uint), PrimitiveTypeCode.UInt32),
        new(typeof(long), PrimitiveTypeCode.Int64),
        new(typeof(ulong), PrimitiveTypeCode.UInt64),
        new(typeof(float), PrimitiveTypeCode.Single),
        new(typeof(double), PrimitiveTypeCode.Double),
        new(typeof(decimal), PrimitiveTypeCode.Decimal),
        new(typeof(DateTime), PrimitiveTypeCode.DateTime),
        new(typeof(object), PrimitiveTypeCode.Empty), // no 17 in TypeCode for some reason
        new(typeof(string), PrimitiveTypeCode.String)
    ];

    public static PrimitiveTypeCode GetTypeCode(Type type) =>
        GetTypeCode(type, out _);

    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "Nullable<T> instantiated over primitive types are kept by TypeCodeMap")]
    public static PrimitiveTypeCode GetTypeCode(Type type, out bool isEnum)
    {
        if (TypeCodeMap.TryGetValue(type, out var typeCode))
        {
            isEnum = false;
            return typeCode;
        }

        if (type.IsEnum)
        {
            isEnum = true;
            return GetTypeCode(Enum.GetUnderlyingType(type));
        }

        var nonNullable = Nullable.GetUnderlyingType(type);
        if (nonNullable is {IsEnum: true})
        {
            var nullableUnderlyingType = typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(nonNullable));
            isEnum = true;
            return GetTypeCode(nullableUnderlyingType);
        }

        isEnum = false;
        return PrimitiveTypeCode.Object;
    }

    public static TypeInformation GetTypeInformation(IConvertible convertible) =>
        PrimitiveTypeCodes[(int) convertible.GetTypeCode()];

    public static bool IsConvertible(this Type type) =>
        type.IsAssignableTo<IConvertible>();

    public static TimeSpan ParseTimeSpan(string input) =>
        TimeSpan.Parse(input, InvariantCulture);

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static class CastConverters
    {
        public static readonly ThreadSafeStore<Tuple<Type, Type>, Func<object?, object?>?> Instance = new(CreateCastConverter);
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static Func<object?, object?>? CreateCastConverter(Tuple<Type, Type> t)
    {
        var initialType = t.Item1;
        var targetType = t.Item2;
        Type[] types = [initialType];
        var castMethodInfo = targetType.GetMethod("op_Implicit", types)
                             ?? targetType.GetMethod("op_Explicit", types);

        if (castMethodInfo == null)
        {
            return null;
        }

        var call = DelegateFactory.CreateMethodCall<object?>(castMethodInfo);

        return o => call(null, o);
    }

    internal static BigInteger ToBigInteger(object value)
    {
        if (value is BigInteger integer)
        {
            return integer;
        }

        if (value is string s)
        {
            return BigInteger.Parse(s, InvariantCulture);
        }

        if (value is float f)
        {
            return new(f);
        }

        if (value is double d)
        {
            return new(d);
        }

        if (value is decimal @decimal)
        {
            return new(@decimal);
        }

        if (value is int i)
        {
            return new(i);
        }

        if (value is long l)
        {
            return new(l);
        }

        if (value is uint u)
        {
            return new(u);
        }

        if (value is ulong @ulong)
        {
            return new(@ulong);
        }

        if (value is byte[] bytes)
        {
            return new(bytes);
        }

        throw new InvalidCastException($"Cannot convert {value.GetType()} to BigInteger.");
    }

    public static object FromBigInteger(BigInteger i, Type targetType)
    {
        if (targetType == typeof(decimal))
        {
            return (decimal) i;
        }

        if (targetType == typeof(double))
        {
            return (double) i;
        }

        if (targetType == typeof(float))
        {
            return (float) i;
        }

        if (targetType == typeof(ulong))
        {
            return (ulong) i;
        }

        if (targetType == typeof(bool))
        {
            return i != 0;
        }

        try
        {
            return System.Convert.ChangeType((long) i, targetType, InvariantCulture);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Can not convert from BigInteger to {targetType}.", exception);
        }
    }

    enum ConvertResult
    {
        Success = 0,
        CannotConvertNull = 1,
        NotInstantiableType = 2,
        NoValidConversion = 3
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object Convert(object initialValue, Type targetType) =>
        TryConvertInternal(initialValue, targetType, out var value) switch
        {
            ConvertResult.Success =>
                value!,
            ConvertResult.CannotConvertNull =>
                throw new($"Can not convert null {initialValue.GetType()} into non-nullable {targetType}."),
            ConvertResult.NotInstantiableType =>
                throw new ArgumentException($"Target type {targetType} is not a value type or a non-abstract class.", nameof(targetType)),
            ConvertResult.NoValidConversion =>
                throw new InvalidOperationException($"Can not convert from {initialValue.GetType()} to {targetType}."),
            _ =>
                throw new InvalidOperationException("Unexpected conversion result.")
        };

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static bool TryConvert(object initialValue, Type targetType, out object? value)
    {
        try
        {
            if (TryConvertInternal(initialValue, targetType, out value) == ConvertResult.Success)
            {
                return true;
            }

            value = null;
            return false;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static ConvertResult TryConvertInternal(object initialValue, Type targetType, out object? value)
    {
        if (targetType.IsNullableType())
        {
            targetType = Nullable.GetUnderlyingType(targetType)!;
        }

        var initialType = initialValue.GetType();

        if (targetType == initialType)
        {
            value = initialValue;
            return ConvertResult.Success;
        }

        // use Convert.ChangeType if both types are IConvertible
        if (initialValue is IConvertible convertible && IsConvertible(targetType))
        {
            if (targetType.IsEnum)
            {
                if (initialValue is string)
                {
                    value = Enum.Parse(targetType, initialValue.ToString()!, true);
                    return ConvertResult.Success;
                }

                if (IsInteger(initialValue))
                {
                    value = Enum.ToObject(targetType, initialValue);
                    return ConvertResult.Success;
                }
            }

            value = convertible.ToType(targetType, InvariantCulture);
            return ConvertResult.Success;
        }

        if (initialValue is DateTime dt && targetType == typeof(DateTimeOffset))
        {
            value = new DateTimeOffset(dt);
            return ConvertResult.Success;
        }

        if (initialValue is byte[] bytes && targetType == typeof(Guid))
        {
            value = new Guid(bytes);
            return ConvertResult.Success;
        }

        if (initialValue is Guid guid && targetType == typeof(byte[]))
        {
            value = guid.ToByteArray();
            return ConvertResult.Success;
        }

        if (initialValue is string s)
        {
            if (targetType == typeof(Guid))
            {
                value = new Guid(s);
                return ConvertResult.Success;
            }

            if (targetType == typeof(Uri))
            {
                value = new Uri(s, UriKind.RelativeOrAbsolute);
                return ConvertResult.Success;
            }

            if (targetType == typeof(TimeSpan))
            {
                value = ParseTimeSpan(s);
                return ConvertResult.Success;
            }

            if (targetType == typeof(byte[]))
            {
                value = System.Convert.FromBase64String(s);
                return ConvertResult.Success;
            }

            if (targetType == typeof(Version))
            {
                if (Version.TryParse(s, out var result))
                {
                    value = result;
                    return ConvertResult.Success;
                }

                value = null;
                return ConvertResult.NoValidConversion;
            }

            if (targetType.IsAssignableTo<Type>())
            {
                value = Type.GetType(s, true);
                return ConvertResult.Success;
            }
#if NET6_0_OR_GREATER
            if (targetType == typeof(Date))
            {
                value = Date.ParseExact(s, "yyyy'-'MM'-'dd", InvariantCulture);
                return ConvertResult.Success;
            }
            if (targetType == typeof(Time))
            {
                value = Time.ParseExact(s, "HH':'mm':'ss.FFFFFFF", InvariantCulture);
                return ConvertResult.Success;
            }
#endif
        }

        if (targetType == typeof(BigInteger))
        {
            value = ToBigInteger(initialValue);
            return ConvertResult.Success;
        }

        if (initialValue is BigInteger integer)
        {
            value = FromBigInteger(integer, targetType);
            return ConvertResult.Success;
        }

        // see if source or target types have a TypeConverter that converts between the two
        var toConverter = TypeDescriptor.GetConverter(initialType);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (toConverter != null && toConverter.CanConvertTo(targetType))
        {
            value = toConverter.ConvertTo(null, InvariantCulture, initialValue, targetType);
            return ConvertResult.Success;
        }

        var fromConverter = TypeDescriptor.GetConverter(targetType);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (fromConverter != null && fromConverter.CanConvertFrom(initialType))
        {
            value = fromConverter.ConvertFrom(null, InvariantCulture, initialValue);
            return ConvertResult.Success;
        }

        // handle DBNull
        if (initialValue == DBNull.Value)
        {
            if (targetType.IsNullable())
            {
                value = EnsureTypeAssignable(null, initialType, targetType);
                return ConvertResult.Success;
            }

            // cannot convert null to non-nullable
            value = null;
            return ConvertResult.CannotConvertNull;
        }

        if (targetType.IsInterface || targetType.IsGenericTypeDefinition || targetType.IsAbstract)
        {
            value = null;
            return ConvertResult.NotInstantiableType;
        }

        value = null;
        return ConvertResult.NoValidConversion;
    }

    #region ConvertOrCast

    /// <summary>
    /// Converts the value to the specified type. If the value is unable to be converted, the
    /// value is checked whether it assignable to the specified type.
    /// </summary>
    /// <param name="initialValue">The value to convert.</param>
    /// <param name="targetType">The type to convert or cast the value to.</param>
    /// <returns>
    /// The converted type. If conversion was unsuccessful, the initial value
    /// is returned if assignable to the target type.
    /// </returns>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? ConvertOrCast(object? initialValue, Type targetType)
    {
        if (targetType == typeof(object))
        {
            return initialValue;
        }

        if (initialValue == null)
        {
            return null;
        }

        if (TryConvert(initialValue, targetType, out var convertedValue))
        {
            return convertedValue;
        }

        return EnsureTypeAssignable(initialValue, initialValue.GetType(), targetType);
    }

    #endregion

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static object? EnsureTypeAssignable(object? value, Type initialType, Type targetType)
    {
        if (value == null)
        {
            if (targetType.IsNullable())
            {
                return null;
            }
        }
        else
        {
            var valueType = value.GetType();

            if (targetType.IsAssignableFrom(valueType))
            {
                return value;
            }

            var castConverter = CastConverters.Instance.Get(new(valueType, targetType));
            if (castConverter != null)
            {
                return castConverter(value);
            }
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        throw new ArgumentException($"Could not cast or convert from {initialType.ToString() ?? "{null}"} to {targetType}.");
    }

    public static bool IsInteger(object value) =>
        GetTypeCode(value.GetType()) switch
        {
            PrimitiveTypeCode.SByte => true,
            PrimitiveTypeCode.Byte => true,
            PrimitiveTypeCode.Int16 => true,
            PrimitiveTypeCode.UInt16 => true,
            PrimitiveTypeCode.Int32 => true,
            PrimitiveTypeCode.UInt32 => true,
            PrimitiveTypeCode.Int64 => true,
            PrimitiveTypeCode.UInt64 => true,
            _ => false
        };

    public static ParseResult Int32TryParse(char[] chars, int start, int length, out int value)
    {
        value = 0;

        if (length == 0)
        {
            return ParseResult.Invalid;
        }

        var isNegative = chars[start] == '-';

        if (isNegative)
        {
            // text just a negative sign
            if (length == 1)
            {
                return ParseResult.Invalid;
            }

            start++;
            length--;
        }

        var end = start + length;

        // Int32.MaxValue and MinValue are 10 chars
        // Or is 10 chars and start is greater than two
        // Need to improve this!
        if (length > 10 || (length == 10 && chars[start] - '0' > 2))
        {
            // invalid result takes precedence over overflow
            for (var i = start; i < end; i++)
            {
                var c = chars[i] - '0';

                if (c is < 0 or > 9)
                {
                    return ParseResult.Invalid;
                }
            }

            return ParseResult.Overflow;
        }

        for (var i = start; i < end; i++)
        {
            var c = chars[i] - '0';

            if (c is < 0 or > 9)
            {
                return ParseResult.Invalid;
            }

            var newValue = 10 * value - c;

            // overflow has caused the number to loop around
            if (newValue > value)
            {
                i++;

                // double check the rest of the string that there wasn't anything invalid
                // invalid result takes precedence over overflow result
                for (; i < end; i++)
                {
                    c = chars[i] - '0';

                    if (c is < 0 or > 9)
                    {
                        return ParseResult.Invalid;
                    }
                }

                return ParseResult.Overflow;
            }

            value = newValue;
        }

        // go from negative to positive to avoids overflow
        // negative can be slightly bigger than positive
        if (!isNegative)
        {
            // negative integer can be one bigger than positive
            if (value == int.MinValue)
            {
                return ParseResult.Overflow;
            }

            value = -value;
        }

        return ParseResult.Success;
    }

    public static ParseResult Int64TryParse(char[] chars, int start, int length, out long value)
    {
        value = 0;

        if (length == 0)
        {
            return ParseResult.Invalid;
        }

        var isNegative = chars[start] == '-';

        if (isNegative)
        {
            // text just a negative sign
            if (length == 1)
            {
                return ParseResult.Invalid;
            }

            start++;
            length--;
        }

        var end = start + length;

        // Int64.MaxValue and MinValue are 19 chars
        if (length > 19)
        {
            // invalid result takes precedence over overflow
            for (var i = start; i < end; i++)
            {
                var c = chars[i] - '0';

                if (c is < 0 or > 9)
                {
                    return ParseResult.Invalid;
                }
            }

            return ParseResult.Overflow;
        }

        for (var i = start; i < end; i++)
        {
            var c = chars[i] - '0';

            if (c is < 0 or > 9)
            {
                return ParseResult.Invalid;
            }

            var newValue = 10 * value - c;

            // overflow has caused the number to loop around
            if (newValue > value)
            {
                i++;

                // double check the rest of the string that there wasn't anything invalid
                // invalid result takes precedence over overflow result
                for (; i < end; i++)
                {
                    c = chars[i] - '0';

                    if (c is < 0 or > 9)
                    {
                        return ParseResult.Invalid;
                    }
                }

                return ParseResult.Overflow;
            }

            value = newValue;
        }

        // go from negative to positive to avoids overflow
        // negative can be slightly bigger than positive
        if (!isNegative)
        {
            // negative integer can be one bigger than positive
            if (value == long.MinValue)
            {
                return ParseResult.Overflow;
            }

            value = -value;
        }

        return ParseResult.Success;
    }

    public static ParseResult DecimalTryParse(char[] chars, int start, int length, out decimal value)
    {
        value = 0M;
        const decimal decimalMaxValueHi28 = 7922816251426433759354395033M;
        const ulong decimalMaxValueHi19 = 7922816251426433759UL;
        const ulong decimalMaxValueLo9 = 354395033UL;
        const char decimalMaxValueLo1 = '5';

        if (length == 0)
        {
            return ParseResult.Invalid;
        }

        var isNegative = chars[start] == '-';
        if (isNegative)
        {
            // text just a negative sign
            if (length == 1)
            {
                return ParseResult.Invalid;
            }

            start++;
            length--;
        }

        var i = start;
        var end = start + length;
        var numDecimalStart = end;
        var numDecimalEnd = end;
        var exponent = 0;
        var hi19 = 0UL;
        var lo10 = 0UL;
        var mantissaDigits = 0;
        var exponentFromMantissa = 0;
        char? digit29 = null;
        bool? storeOnly28Digits = null;
        for (; i < end; i++)
        {
            var c = chars[i];
            switch (c)
            {
                case '.':
                    if (i == start)
                    {
                        return ParseResult.Invalid;
                    }

                    if (i + 1 == end)
                    {
                        return ParseResult.Invalid;
                    }

                    if (numDecimalStart != end)
                    {
                        // multiple decimal points
                        return ParseResult.Invalid;
                    }

                    numDecimalStart = i + 1;
                    break;
                case 'e':
                case 'E':
                    if (i == start)
                    {
                        return ParseResult.Invalid;
                    }

                    if (i == numDecimalStart)
                    {
                        // E follows decimal point
                        return ParseResult.Invalid;
                    }

                    i++;
                    if (i == end)
                    {
                        return ParseResult.Invalid;
                    }

                    if (numDecimalStart < end)
                    {
                        numDecimalEnd = i - 1;
                    }

                    c = chars[i];
                    var exponentNegative = false;
                    switch (c)
                    {
                        case '-':
                            exponentNegative = true;
                            i++;
                            break;
                        case '+':
                            i++;
                            break;
                    }

                    // parse 3 digit
                    for (; i < end; i++)
                    {
                        c = chars[i];
                        if (c is < '0' or > '9')
                        {
                            return ParseResult.Invalid;
                        }

                        var newExponent = 10 * exponent + (c - '0');
                        // stops updating exponent when overflowing
                        if (exponent < newExponent)
                        {
                            exponent = newExponent;
                        }
                    }

                    if (exponentNegative)
                    {
                        exponent = -exponent;
                    }

                    break;
                default:
                    if (c is < '0' or > '9')
                    {
                        return ParseResult.Invalid;
                    }

                    if (i == start && c == '0')
                    {
                        i++;
                        if (i != end)
                        {
                            c = chars[i];
                            if (c == '.')
                            {
                                goto case '.';
                            }

                            if (c is 'e' or 'E')
                            {
                                goto case 'E';
                            }

                            return ParseResult.Invalid;
                        }
                    }

                    if (mantissaDigits < 29 && (mantissaDigits != 28 || !(storeOnly28Digits ?? (storeOnly28Digits = hi19 > decimalMaxValueHi19 || (hi19 == decimalMaxValueHi19 && (lo10 > decimalMaxValueLo9 || (lo10 == decimalMaxValueLo9 && c > decimalMaxValueLo1)))).GetValueOrDefault())))
                    {
                        if (mantissaDigits < 19)
                        {
                            hi19 = hi19 * 10UL + (ulong) (c - '0');
                        }
                        else
                        {
                            lo10 = lo10 * 10UL + (ulong) (c - '0');
                        }

                        ++mantissaDigits;
                    }
                    else
                    {
                        digit29 ??= c;
                        ++exponentFromMantissa;
                    }

                    break;
            }
        }

        exponent += exponentFromMantissa;

        // correct the decimal point
        exponent -= numDecimalEnd - numDecimalStart;

        if (mantissaDigits <= 19)
        {
            value = hi19;
        }
        else
        {
            value = hi19 / new decimal(1, 0, 0, false, (byte) (mantissaDigits - 19)) + lo10;
        }

        if (exponent > 0)
        {
            mantissaDigits += exponent;
            if (mantissaDigits > 29)
            {
                return ParseResult.Overflow;
            }

            if (mantissaDigits == 29)
            {
                if (exponent > 1)
                {
                    value /= new decimal(1, 0, 0, false, (byte) (exponent - 1));
                    if (value > decimalMaxValueHi28)
                    {
                        return ParseResult.Overflow;
                    }
                }
                else if (value == decimalMaxValueHi28 && digit29 > decimalMaxValueLo1)
                {
                    return ParseResult.Overflow;
                }

                value *= 10M;
            }
            else
            {
                value /= new decimal(1, 0, 0, false, (byte) exponent);
            }
        }
        else
        {
            if (digit29 >= '5' && exponent >= -28)
            {
                ++value;
            }

            if (exponent < 0)
            {
                if (mantissaDigits + exponent + 28 <= 0)
                {
                    value = isNegative ? -0M : 0M;
                    return ParseResult.Success;
                }

                if (exponent >= -28)
                {
                    value *= new decimal(1, 0, 0, false, (byte) -exponent);
                }
                else
                {
                    value /= 1e28M;
                    value *= new decimal(1, 0, 0, false, (byte) (-exponent - 28));
                }
            }
        }

        if (isNegative)
        {
            value = -value;
        }

        return ParseResult.Success;
    }

    public static bool TryConvertGuid(string s, out Guid g) =>
        // GUID has to have format 00000000-0000-0000-0000-000000000000
        Guid.TryParseExact(s, "D", out g);

    public static bool TryHexTextToInt(char[] text, int start, int end, out int value)
    {
        value = 0;

        for (var i = start; i < end; i++)
        {
            var ch = text[i];
            int chValue;

            if (ch <= 57 && ch >= 48)
            {
                chValue = ch - 48;
            }
            else if (ch <= 70 && ch >= 65)
            {
                chValue = ch - 55;
            }
            else if (ch <= 102 && ch >= 97)
            {
                chValue = ch - 87;
            }
            else
            {
                value = 0;
                return false;
            }

            value += chValue << ((end - 1 - i) * 4);
        }

        return true;
    }
}