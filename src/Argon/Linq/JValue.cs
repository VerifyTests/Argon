// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression

namespace Argon;

/// <summary>
/// Represents a value in JSON (string, integer, date, etc).
/// </summary>
public class JValue :
    JToken,
    IEquatable<JValue>,
    IFormattable,
    IComparable,
    IComparable<JValue>,
    IConvertible
{
    JTokenType valueType;
    object? value;

    internal JValue(object? value, JTokenType type)
    {
        this.value = value;
        valueType = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class from another <see cref="JValue" /> object.
    /// </summary>
    /// <param name="other">A <see cref="JValue" /> object to copy from.</param>
    public JValue(JValue other)
        : this(other.Value, other.Type) =>
        SetLineInfo(other, null);

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(long value) :
        this(BoxedPrimitives.Get(value), JTokenType.Integer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(decimal value) :
        this(BoxedPrimitives.Get(value), JTokenType.Float)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(char value) :
        this(value, JTokenType.String)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(ulong value) :
        this(value, JTokenType.Integer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(double value) :
        this(BoxedPrimitives.Get(value), JTokenType.Float)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(float value) :
        this(value, JTokenType.Float)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(DateTime value) :
        this(value, JTokenType.Date)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(DateTimeOffset value) :
        this(value, JTokenType.Date)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(bool value) :
        this(BoxedPrimitives.Get(value), JTokenType.Boolean)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(string? value) :
        this(value, JTokenType.String)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(Guid value) :
        this(value, JTokenType.Guid)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(Uri? value) :
        this(value, value == null ? JTokenType.Null : JTokenType.Uri)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(TimeSpan value) :
        this(value, JTokenType.TimeSpan)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JValue" /> class with the given value.
    /// </summary>
    public JValue(object? value) :
        this(value, GetValueType(null, value))
    {
    }

    internal override bool DeepEquals(JToken node)
    {
        if (node is not JValue other)
        {
            return false;
        }

        return other == this ||
               ValuesEquals(this, other);
    }

    /// <summary>
    /// Gets a value indicating whether this token has child tokens.
    /// </summary>
    public override bool HasValues => false;

    static int CompareBigInteger(BigInteger i1, object i2)
    {
        var result = i1.CompareTo(ConvertUtils.ToBigInteger(i2));

        if (result != 0)
        {
            return result;
        }

        // converting a fractional number to a BigInteger will lose the fraction
        // check for fraction if result is two numbers are equal
        if (i2 is decimal d1)
        {
            return 0m.CompareTo(Math.Abs(d1 - Math.Truncate(d1)));
        }

        if (i2 is double or float)
        {
            var d = Convert.ToDouble(i2, InvariantCulture);
            return 0d.CompareTo(Math.Abs(d - Math.Truncate(d)));
        }

        return result;
    }

    internal static int Compare(JTokenType valueType, object? objA, object? objB)
    {
        if (objA == objB)
        {
            return 0;
        }

        if (objB == null)
        {
            return 1;
        }

        if (objA == null)
        {
            return -1;
        }

        switch (valueType)
        {
            case JTokenType.Integer:
            {
                if (objA is BigInteger integerA)
                {
                    return CompareBigInteger(integerA, objB);
                }

                if (objB is BigInteger integerB)
                {
                    return -CompareBigInteger(integerB, objA);
                }

                if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
                {
                    return Convert.ToDecimal(objA, InvariantCulture).CompareTo(Convert.ToDecimal(objB, InvariantCulture));
                }

                if (objA is float || objB is float || objA is double || objB is double)
                {
                    return CompareFloat(objA, objB);
                }

                return Convert.ToInt64(objA, InvariantCulture).CompareTo(Convert.ToInt64(objB, InvariantCulture));
            }
            case JTokenType.Float:
            {
                if (objA is BigInteger integerA)
                {
                    return CompareBigInteger(integerA, objB);
                }

                if (objB is BigInteger integerB)
                {
                    return -CompareBigInteger(integerB, objA);
                }

                if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
                {
                    return Convert.ToDecimal(objA, InvariantCulture).CompareTo(Convert.ToDecimal(objB, InvariantCulture));
                }

                return CompareFloat(objA, objB);
            }
            case JTokenType.Comment:
            case JTokenType.String:
            case JTokenType.Raw:
                var s1 = Convert.ToString(objA, InvariantCulture);
                var s2 = Convert.ToString(objB, InvariantCulture);

                return string.CompareOrdinal(s1, s2);
            case JTokenType.Boolean:
                var b1 = Convert.ToBoolean(objA, InvariantCulture);
                var b2 = Convert.ToBoolean(objB, InvariantCulture);

                return b1.CompareTo(b2);
            case JTokenType.Date:
                if (objA is DateTime dateA)
                {
                    DateTime dateB;

                    if (objB is DateTimeOffset offsetB)
                    {
                        dateB = offsetB.DateTime;
                    }
                    else
                    {
                        dateB = Convert.ToDateTime(objB, InvariantCulture);
                    }

                    return dateA.CompareTo(dateB);
                }
                else
                {
                    var offsetA = (DateTimeOffset) objA;
                    if (objB is not DateTimeOffset offsetB)
                    {
                        offsetB = new(Convert.ToDateTime(objB, InvariantCulture));
                    }

                    return offsetA.CompareTo(offsetB);
                }
            case JTokenType.Bytes:
                if (objB is not byte[] bytesB)
                {
                    throw new ArgumentException("Object must be of type byte[].");
                }

                var bytesA = (byte[]) objA;

                return MiscellaneousUtils.ByteArrayCompare(bytesA, bytesB);
            case JTokenType.Guid:
                if (objB is not Guid guid2)
                {
                    throw new ArgumentException("Object must be of type Guid.");
                }

                var guid1 = (Guid) objA;

                return guid1.CompareTo(guid2);
            case JTokenType.Uri:
                var uri2 = objB as Uri;
                if (uri2 == null)
                {
                    throw new ArgumentException("Object must be of type Uri.");
                }

                var uri1 = (Uri) objA;

                return Comparer<string>.Default.Compare(uri1.ToString(), uri2.ToString());
            case JTokenType.TimeSpan:
                if (objB is not TimeSpan timeSpan)
                {
                    throw new ArgumentException("Object must be of type TimeSpan.");
                }

                var ts1 = (TimeSpan) objA;

                return ts1.CompareTo(timeSpan);
            default:
                throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(valueType), valueType, $"Unexpected value type: {valueType}");
        }
    }

    static int CompareFloat(object objA, object objB)
    {
        var d1 = Convert.ToDouble(objA, InvariantCulture);
        var d2 = Convert.ToDouble(objB, InvariantCulture);

        // take into account possible floating point errors
        if (MathUtils.ApproxEquals(d1, d2))
        {
            return 0;
        }

        return d1.CompareTo(d2);
    }

    static bool Operation(ExpressionType operation, object? objA, object? objB, out object? result)
    {
        if (objA is string || objB is string)
        {
            if (operation is ExpressionType.Add or ExpressionType.AddAssign)
            {
                // ReSharper disable RedundantToStringCall
                result = objA?.ToString() + objB?.ToString();
                // ReSharper restore RedundantToStringCall
                return true;
            }
        }

        if (objA is BigInteger || objB is BigInteger)
        {
            if (objA == null || objB == null)
            {
                result = null;
                return true;
            }

            // not that this will lose the fraction
            // BigInteger doesn't have operators with non-integer types
            var i1 = ConvertUtils.ToBigInteger(objA);
            var i2 = ConvertUtils.ToBigInteger(objB);

            switch (operation)
            {
                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                    result = BigInteger.Add(i1, i2);
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                    result = BigInteger.Subtract(i1, i2);
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                    result = BigInteger.Multiply(i1, i2);
                    return true;
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                    result = BigInteger.Divide(i1, i2);
                    return true;
            }
        }
        else if (objA is ulong || objB is ulong || objA is decimal || objB is decimal)
        {
            if (objA == null || objB == null)
            {
                result = null;
                return true;
            }

            var d1 = Convert.ToDecimal(objA, InvariantCulture);
            var d2 = Convert.ToDecimal(objB, InvariantCulture);

            switch (operation)
            {
                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                    result = d1 + d2;
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                    result = d1 - d2;
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                    result = d1 * d2;
                    return true;
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                    result = d1 / d2;
                    return true;
            }
        }
        else if (objA is float || objB is float || objA is double || objB is double)
        {
            if (objA == null || objB == null)
            {
                result = null;
                return true;
            }

            var d1 = Convert.ToDouble(objA, InvariantCulture);
            var d2 = Convert.ToDouble(objB, InvariantCulture);

            switch (operation)
            {
                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                    result = d1 + d2;
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                    result = d1 - d2;
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                    result = d1 * d2;
                    return true;
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                    result = d1 / d2;
                    return true;
            }
        }
        else if (objA is int or uint or long or short or ushort or sbyte or byte ||
                 objB is int or uint or long or short or ushort or sbyte or byte)
        {
            if (objA == null || objB == null)
            {
                result = null;
                return true;
            }

            var l1 = Convert.ToInt64(objA, InvariantCulture);
            var l2 = Convert.ToInt64(objB, InvariantCulture);

            switch (operation)
            {
                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                    result = l1 + l2;
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                    result = l1 - l2;
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                    result = l1 * l2;
                    return true;
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                    result = l1 / l2;
                    return true;
            }
        }

        result = null;
        return false;
    }

    internal override JToken CloneToken() =>
        new JValue(this);

    /// <summary>
    /// Creates a <see cref="JValue" /> comment with the given value.
    /// </summary>
    /// <returns>A <see cref="JValue" /> comment with the given value.</returns>
    public static JValue CreateComment(string? value) =>
        new(value, JTokenType.Comment);

    /// <summary>
    /// Creates a <see cref="JValue" /> string with the given value.
    /// </summary>
    /// <returns>A <see cref="JValue" /> string with the given value.</returns>
    public static JValue CreateString(string? value) =>
        new(value, JTokenType.String);

    /// <summary>
    /// Creates a <see cref="JValue" /> null value.
    /// </summary>
    /// <returns>A <see cref="JValue" /> null value.</returns>
    public static JValue CreateNull() =>
        new(null, JTokenType.Null);

    /// <summary>
    /// Creates a <see cref="JValue" /> undefined value.
    /// </summary>
    /// <returns>A <see cref="JValue" /> undefined value.</returns>
    public static JValue CreateUndefined() =>
        new(null, JTokenType.Undefined);

    static JTokenType GetValueType(JTokenType? current, object? value)
    {
        if (value == null)
        {
            return JTokenType.Null;
        }

        if (value == DBNull.Value)
        {
            return JTokenType.Null;
        }

        if (value is string)
        {
            return GetStringValueType(current);
        }

        if (value is long or int or short or sbyte or ulong or uint or ushort or byte)
        {
            return JTokenType.Integer;
        }

        if (value is Enum)
        {
            return JTokenType.Integer;
        }

        if (value is BigInteger)
        {
            return JTokenType.Integer;
        }

        if (value is double or float or decimal)
        {
            return JTokenType.Float;
        }

        if (value is DateTime)
        {
            return JTokenType.Date;
        }

        if (value is DateTimeOffset)
        {
            return JTokenType.Date;
        }

        if (value is byte[])
        {
            return JTokenType.Bytes;
        }

        if (value is bool)
        {
            return JTokenType.Boolean;
        }

        if (value is Guid)
        {
            return JTokenType.Guid;
        }

        if (value is Uri)
        {
            return JTokenType.Uri;
        }

        if (value is TimeSpan)
        {
            return JTokenType.TimeSpan;
        }

        throw new ArgumentException($"Could not determine JSON object type for type {value.GetType()}.");
    }

    static JTokenType GetStringValueType(JTokenType? current)
    {
        if (current == null)
        {
            return JTokenType.String;
        }

        switch (current.GetValueOrDefault())
        {
            case JTokenType.Comment:
            case JTokenType.String:
            case JTokenType.Raw:
                return current.GetValueOrDefault();
            default:
                return JTokenType.String;
        }
    }

    /// <summary>
    /// Gets the node type for this <see cref="JToken" />.
    /// </summary>
    public override JTokenType Type => valueType;

    /// <summary>
    /// Gets or sets the underlying token value.
    /// </summary>
    public object GetValue()
    {
        if (value == null)
        {
            throw new("Cannot GetValue when underlying value is null");
        }

        return value!;
    }

    /// <summary>
    /// Gets or sets the underlying token value.
    /// </summary>
    public object? Value
    {
        get => value;
        set
        {
            var currentType = this.value?.GetType();
            var newType = value?.GetType();

            if (currentType != newType)
            {
                valueType = GetValueType(valueType, value);
            }

            this.value = value;
        }
    }

    /// <summary>
    /// Writes this token to a <see cref="JsonWriter" />.
    /// </summary>
    /// <param name="converters">A collection of <see cref="JsonConverter" />s which will be used when writing the token.</param>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
    {
        if (converters is {Length: > 0} && value != null)
        {
            var matchingConverter = JsonSerializer.GetMatchingConverter(converters, value.GetType());
            if (matchingConverter is {CanWrite: true})
            {
                matchingConverter.WriteJson(writer, value, JsonSerializer.CreateDefault());
                return;
            }
        }

        switch (valueType)
        {
            case JTokenType.Comment:
                writer.WriteComment(value?.ToString());
                return;
            case JTokenType.Raw:
                writer.WriteRawValue(value?.ToString());
                return;
            case JTokenType.Null:
                writer.WriteNull();
                return;
            case JTokenType.Undefined:
                writer.WriteUndefined();
                return;
            case JTokenType.Integer:
                if (value is int i)
                {
                    writer.WriteValue(i);
                }
                else if (value is long l)
                {
                    writer.WriteValue(l);
                }
                else if (value is ulong ul)
                {
                    writer.WriteValue(ul);
                }
                else if (value is BigInteger integer)
                {
                    writer.WriteValue(integer);
                }
                else
                {
                    writer.WriteValue(Convert.ToInt64(value, InvariantCulture));
                }

                return;
            case JTokenType.Float:
                if (value is decimal dec)
                {
                    writer.WriteValue(dec);
                }
                else if (value is double d)
                {
                    writer.WriteValue(d);
                }
                else if (value is float f)
                {
                    writer.WriteValue(f);
                }
                else
                {
                    writer.WriteValue(Convert.ToDouble(value, InvariantCulture));
                }

                return;
            case JTokenType.String:
                writer.WriteValue(value?.ToString());
                return;
            case JTokenType.Boolean:
                writer.WriteValue(Convert.ToBoolean(value, InvariantCulture));
                return;
            case JTokenType.Date:
                if (value is DateTimeOffset offset)
                {
                    writer.WriteValue(offset);
                }
                else
                {
                    writer.WriteValue(Convert.ToDateTime(value, InvariantCulture));
                }

                return;
            case JTokenType.Bytes:
                writer.WriteValue((byte[]?) value);
                return;
            case JTokenType.Guid:
                writer.WriteValue((Guid?) value);
                return;
            case JTokenType.TimeSpan:
                writer.WriteValue((TimeSpan?) value);
                return;
            case JTokenType.Uri:
                writer.WriteValue((Uri?) value);
                return;
        }

        throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(Type), valueType, "Unexpected token type.");
    }

    internal override int GetDeepHashCode()
    {
        var valueHashCode = value?.GetHashCode() ?? 0;

        // GetHashCode on an enum boxes so cast to int
        return ((int) valueType).GetHashCode() ^ valueHashCode;
    }

    static bool ValuesEquals(JValue v1, JValue v2) =>
        v1 == v2 || (v1.valueType == v2.valueType && Compare(v1.valueType, v1.value, v2.value) == 0);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(JValue? other) =>
        other != null &&
        ValuesEquals(this, other);

    /// <summary>
    /// Determines whether the specified <see cref="Object" /> is equal to the current <see cref="Object" />.
    /// </summary>
    /// <param name="obj">The <see cref="Object" /> to compare with the current <see cref="Object" />.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="Object" /> is equal to the current <see cref="Object" />; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is JValue v)
        {
            return Equals(v);
        }

        return false;
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="Object" />.
    /// </returns>
    public override int GetHashCode()
    {
        if (value == null)
        {
            return 0;
        }

        return value.GetHashCode();
    }

    /// <summary>
    /// Returns a <see cref="String" /> that represents this instance.
    /// </summary>
    /// <remarks>
    /// <c>ToString()</c> returns a non-JSON string value for tokens with a type of <see cref="JTokenType.String" />.
    /// If you want the JSON for all token types then you should use <see cref="WriteTo(JsonWriter, JsonConverter[])" />.
    /// </remarks>
    /// <returns>
    /// A <see cref="String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value.ToString()!;
    }

    /// <summary>
    /// Returns a <see cref="String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="String" /> that represents this instance.
    /// </returns>
    public string ToString(string format) =>
        ToString(format, InvariantCulture);

    /// <summary>
    /// Returns a <see cref="String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="String" /> that represents this instance.
    /// </returns>
    public string ToString(IFormatProvider? formatProvider) =>
        ToString(null, formatProvider);

    /// <summary>
    /// Returns a <see cref="String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="String" /> that represents this instance.
    /// </returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, formatProvider);
        }

        return value.ToString()!;
    }

    /// <summary>
    /// Returns the <see cref="DynamicMetaObject" /> responsible for binding operations performed on this object.
    /// </summary>
    /// <param name="parameter">The expression tree representation of the runtime value.</param>
    /// <returns>
    /// The <see cref="DynamicMetaObject" /> to bind this object.
    /// </returns>
    protected override DynamicMetaObject GetMetaObject(Expression parameter)
    {
#if HAVE_COMPONENT_MODEL
        if (!DynamicIsSupported)
        {
            throw new NotSupportedException(DynamicNotSupportedMessage);
        }
#endif
#pragma warning disable IL2026, IL3050
        return new DynamicProxyMetaObject<JValue>(parameter, this, new JValueDynamicProxy());
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    class JValueDynamicProxy :
        DynamicProxy<JValue>
    {
        public override bool TryConvert(JValue instance, ConvertBinder binder, [NotNullWhen(true)] out object? result)
        {
            if (binder.Type == typeof(JValue) || binder.Type == typeof(JToken))
            {
                result = instance;
                return true;
            }

            var value = instance.Value;

            if (value == null)
            {
                result = null;
                return binder.Type.IsNullable();
            }

            result = ConvertUtils.Convert(value, binder.Type);
            return true;
        }

        public override bool TryBinaryOperation(JValue instance, BinaryOperationBinder binder, object arg, [NotNullWhen(true)] out object? result)
        {
            var compareValue = arg is JValue value ? value.Value : arg;

            switch (binder.Operation)
            {
                case ExpressionType.Equal:
                    result = Compare(instance.Type, instance.Value, compareValue) == 0;
                    return true;
                case ExpressionType.NotEqual:
                    result = Compare(instance.Type, instance.Value, compareValue) != 0;
                    return true;
                case ExpressionType.GreaterThan:
                    result = Compare(instance.Type, instance.Value, compareValue) > 0;
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    result = Compare(instance.Type, instance.Value, compareValue) >= 0;
                    return true;
                case ExpressionType.LessThan:
                    result = Compare(instance.Type, instance.Value, compareValue) < 0;
                    return true;
                case ExpressionType.LessThanOrEqual:
                    result = Compare(instance.Type, instance.Value, compareValue) <= 0;
                    return true;
                case ExpressionType.Add:
                case ExpressionType.AddAssign:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractAssign:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.Divide:
                case ExpressionType.DivideAssign:
                    if (Operation(binder.Operation, instance.Value, compareValue, out result))
                    {
                        result = new JValue(result);
                        return true;
                    }

                    break;
            }

            result = null;
            return false;
        }
    }

    int IComparable.CompareTo(object? obj)
    {
        if (obj == null)
        {
            return 1;
        }

        JTokenType comparisonType;
        object? otherValue;
        if (obj is JValue value)
        {
            otherValue = value.Value;
            comparisonType = valueType == JTokenType.String && valueType != value.valueType
                ? value.valueType
                : valueType;
        }
        else
        {
            otherValue = obj;
            comparisonType = valueType;
        }

        return Compare(comparisonType, this.value, otherValue);
    }

    /// <summary>
    /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
    /// </summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings:
    /// Value
    /// Meaning
    /// Less than zero
    /// This instance is less than <paramref name="obj" />.
    /// Zero
    /// This instance is equal to <paramref name="obj" />.
    /// Greater than zero
    /// This instance is greater than <paramref name="obj" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="obj" /> is not of the same type as this instance.
    /// </exception>
    public int CompareTo(JValue? obj)
    {
        if (obj == null)
        {
            return 1;
        }

        var comparisonType = valueType == JTokenType.String &&
                             valueType != obj.valueType
            ? obj.valueType
            : valueType;

        return Compare(comparisonType, value, obj.value);
    }

    TypeCode IConvertible.GetTypeCode()
    {
        if (value == null)
        {
            return TypeCode.Empty;
        }

        if (value is IConvertible convertible)
        {
            return convertible.GetTypeCode();
        }

        return TypeCode.Object;
    }

    bool IConvertible.ToBoolean(IFormatProvider? provider) =>
        (bool) this;

    char IConvertible.ToChar(IFormatProvider? provider) =>
        (char) this;

    sbyte IConvertible.ToSByte(IFormatProvider? provider) =>
        (sbyte) this;

    byte IConvertible.ToByte(IFormatProvider? provider) =>
        (byte) this;

    short IConvertible.ToInt16(IFormatProvider? provider) =>
        (short) this;

    ushort IConvertible.ToUInt16(IFormatProvider? provider) =>
        (ushort) this;

    int IConvertible.ToInt32(IFormatProvider? provider) =>
        (int) this;

    uint IConvertible.ToUInt32(IFormatProvider? provider) =>
        (uint) this;

    long IConvertible.ToInt64(IFormatProvider? provider) =>
        (long) this;

    ulong IConvertible.ToUInt64(IFormatProvider? provider) =>
        (ulong) this;

    float IConvertible.ToSingle(IFormatProvider? provider) =>
        (float) this;

    double IConvertible.ToDouble(IFormatProvider? provider) =>
        (double) this;

    decimal IConvertible.ToDecimal(IFormatProvider? provider) =>
        (decimal) this;

    DateTime IConvertible.ToDateTime(IFormatProvider? provider) =>
        (DateTime) this;

    object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
#if NET7_0_OR_GREATER
        if (!SerializationIsSupported)
        {
            throw new NotSupportedException(SerializationNotSupportedMessage);
        }
#endif
#pragma warning disable IL2026, IL3050
        return ToObject(conversionType)!;
#pragma warning restore IL2026, IL3050
    }
}