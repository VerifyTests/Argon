// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Provides methods for converting between .NET types and JSON types.
/// </summary>
/// <example>
/// <code lang="cs" source="..\src\Tests\Documentation\SerializationTests.cs" region="SerializeObject" title="Serializing and Deserializing JSON with JsonConvert" />
/// </example>
public static class JsonConvert
{
    /// <summary>
    /// Gets or sets a function that creates default <see cref="JsonSerializerSettings" />.
    /// Default settings are automatically used by serialization methods on <see cref="JsonConvert" />,
    /// and <see cref="JToken.ToObject{T}()" /> and <see cref="JToken.FromObject(object)" /> on <see cref="JToken" />.
    /// To serialize without using any default settings create a <see cref="JsonSerializer" /> with
    /// <see cref="JsonSerializer.Create()" />.
    /// </summary>
    public static Func<JsonSerializerSettings>? DefaultSettings { get; set; }

    /// <summary>
    /// Represents JavaScript's boolean value <c>true</c> as a string. This field is read-only.
    /// </summary>
    public static readonly string True = "true";

    /// <summary>
    /// Represents JavaScript's boolean value <c>false</c> as a string. This field is read-only.
    /// </summary>
    public static readonly string False = "false";

    /// <summary>
    /// Represents JavaScript's <c>null</c> as a string. This field is read-only.
    /// </summary>
    public static readonly string Null = "null";

    /// <summary>
    /// Represents JavaScript's <c>undefined</c> as a string. This field is read-only.
    /// </summary>
    public static readonly string Undefined = "undefined";

    /// <summary>
    /// Represents JavaScript's positive infinity as a string. This field is read-only.
    /// </summary>
    public static readonly string PositiveInfinity = "Infinity";

    /// <summary>
    /// Represents JavaScript's negative infinity as a string. This field is read-only.
    /// </summary>
    public static readonly string NegativeInfinity = "-Infinity";

    /// <summary>
    /// Represents JavaScript's <c>NaN</c> as a string. This field is read-only.
    /// </summary>
    public static readonly string NaN = "NaN";

    /// <summary>
    /// Converts the <see cref="DateTime" /> to its JSON string representation.
    /// </summary>
    public static string ToString(DateTime value)
    {
        using var writer = StringUtils.CreateStringWriter(64);
        writer.Write('"');
        DateTimeUtils.WriteDateTimeString(writer, value);
        writer.Write('"');
        return writer.ToString();
    }

    /// <summary>
    /// Converts the <see cref="DateTimeOffset" /> to its JSON string representation.
    /// </summary>
    public static string ToString(DateTimeOffset value)
    {
        using var writer = StringUtils.CreateStringWriter(64);
        writer.Write('"');
        DateTimeUtils.WriteDateTimeOffsetString(writer, value);
        writer.Write('"');
        return writer.ToString();
    }

    /// <summary>
    /// Converts the <see cref="Boolean" /> to its JSON string representation.
    /// </summary>
    public static string ToString(bool value) =>
        value ? True : False;

    /// <summary>
    /// Converts the <see cref="Char" /> to its JSON string representation.
    /// </summary>
    public static string ToString(char value) =>
        ToString(new[]{value}.AsSpan());

    /// <summary>
    /// Converts the <see cref="Enum" /> to its JSON string representation.
    /// </summary>
    public static string ToString(Enum value) =>
        value.ToString("D");

    /// <summary>
    /// Converts the <see cref="Int32" /> to its JSON string representation.
    /// </summary>
    public static string ToString(int value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="Int16" /> to its JSON string representation.
    /// </summary>
    public static string ToString(short value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="UInt16" /> to its JSON string representation.
    /// </summary>
    public static string ToString(ushort value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="UInt32" /> to its JSON string representation.
    /// </summary>
    public static string ToString(uint value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="Int64" />  to its JSON string representation.
    /// </summary>
    public static string ToString(long value) =>
        value.ToString(null, InvariantCulture);

    static string ToStringInternal(BigInteger value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="UInt64" /> to its JSON string representation.
    /// </summary>
    public static string ToString(ulong value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="Single" /> to its JSON string representation.
    /// </summary>
    public static string ToString(float value) =>
        EnsureDecimalPlace(value, value.ToString("R", InvariantCulture));

    internal static string ToString(float value, FloatFormatHandling handling, char quoteChar, bool nullable, string format)
    {
        var text = value.ToString(format, InvariantCulture);
        return EnsureFloatFormat(value, EnsureDecimalPlace(value, text), handling, quoteChar, nullable);
    }

    static string EnsureFloatFormat(double value, string text, FloatFormatHandling handling, char quoteChar, bool nullable)
    {
        if (handling == FloatFormatHandling.Symbol ||
            !(double.IsInfinity(value) || double.IsNaN(value)))
        {
            return text;
        }

        if (handling == FloatFormatHandling.DefaultValue)
        {
            if (nullable)
            {
                return Null;
            }

            return "0.0";
        }

        return quoteChar + text + quoteChar;
    }

    /// <summary>
    /// Converts the <see cref="Double" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Double" />.</returns>
    public static string ToString(double value) =>
        EnsureDecimalPlace(value, value.ToString("R", InvariantCulture));

    internal static string ToString(double value, FloatFormatHandling handling, char quoteChar, bool nullable, string format)
    {
        var text = value.ToString(format, InvariantCulture);
        return EnsureFloatFormat(value, EnsureDecimalPlace(value, text), handling, quoteChar, nullable);
    }

    static string EnsureDecimalPlace(double value, string text)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value) ||
            text.IndexOf('.') != -1 ||
            text.IndexOf('E') != -1 ||
            text.IndexOf('e') != -1)
        {
            return text;
        }

        return $"{text}.0";
    }

    static string EnsureDecimalPlace(string text)
    {
        if (text.IndexOf('.') != -1)
        {
            return text;
        }

        return $"{text}.0";
    }

    /// <summary>
    /// Converts the <see cref="Byte" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Byte" />.</returns>
    public static string ToString(byte value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="SByte" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="SByte" />.</returns>
    public static string ToString(sbyte value) =>
        value.ToString(null, InvariantCulture);

    /// <summary>
    /// Converts the <see cref="Decimal" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Decimal" />.</returns>
    public static string ToString(decimal value) =>
        EnsureDecimalPlace(value.ToString(null, InvariantCulture));

    /// <summary>
    /// Converts the <see cref="Guid" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Guid" />.</returns>
    public static string ToString(Guid value)
    {
        var text = value.ToString("D", InvariantCulture);
        return $"\"{text}\"";
    }

    /// <summary>
    /// Converts the <see cref="TimeSpan" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="TimeSpan" />.</returns>
    public static string ToString(TimeSpan value)
    {
        Span<char> destination = stackalloc char[26];
        value.TryFormat(destination, out _, ['c']);
        return ToString(destination, '"');
    }

    /// <summary>
    /// Converts the <see cref="Uri" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Uri" />.</returns>
    public static string ToString(Uri? value)
    {
        if (value == null)
        {
            return Null;
        }

        return ToString(value, '"');
    }

    internal static string ToString(Uri value, char quoteChar) =>
        ToString(value.OriginalString.AsSpan(), quoteChar);

    /// <summary>
    /// Converts the <see cref="String" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(string value) =>
        ToString(value.AsSpan());

    /// <param name="delimiter">The string delimiter character.</param>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(string value, char delimiter) =>
        ToString(value.AsSpan(), delimiter);

    /// <summary>
    /// Converts the <see cref="String" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(string value, char delimiter, EscapeHandling escapeHandling) =>
        ToString(value.AsSpan(), delimiter, escapeHandling);

    /// <summary>
    /// Converts the <see cref="String" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(CharSpan value) =>
        ToString(value, '"');

    /// <summary>
    /// Converts the <see cref="String" /> to its JSON string representation.
    /// </summary>
    /// <param name="delimiter">The string delimiter character.</param>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(CharSpan value, char delimiter) =>
        ToString(value, delimiter, EscapeHandling.Default);

    /// <summary>
    /// Converts the <see cref="String" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="String" />.</returns>
    public static string ToString(CharSpan value, char delimiter, EscapeHandling escapeHandling)
    {
        if (delimiter != '"' &&
            delimiter != '\'')
        {
            throw new ArgumentException("Delimiter must be a single or double quote.", nameof(delimiter));
        }

        return JavaScriptUtils.ToEscapedJavaScriptString(value, delimiter, true, escapeHandling);
    }

    /// <summary>
    /// Converts the <see cref="Object" /> to its JSON string representation.
    /// </summary>
    /// <returns>A JSON string representation of the <see cref="Object" />.</returns>
    public static string ToString(object? value)
    {
        if (value == null)
        {
            return Null;
        }

        var typeCode = ConvertUtils.GetTypeCode(value.GetType());

        switch (typeCode)
        {
            case PrimitiveTypeCode.String:
                return ToString((string) value);
            case PrimitiveTypeCode.Char:
                return ToString((char) value);
            case PrimitiveTypeCode.Boolean:
                return ToString((bool) value);
            case PrimitiveTypeCode.SByte:
                return ToString((sbyte) value);
            case PrimitiveTypeCode.Int16:
                return ToString((short) value);
            case PrimitiveTypeCode.UInt16:
                return ToString((ushort) value);
            case PrimitiveTypeCode.Int32:
                return ToString((int) value);
            case PrimitiveTypeCode.Byte:
                return ToString((byte) value);
            case PrimitiveTypeCode.UInt32:
                return ToString((uint) value);
            case PrimitiveTypeCode.Int64:
                return ToString((long) value);
            case PrimitiveTypeCode.UInt64:
                return ToString((ulong) value);
            case PrimitiveTypeCode.Single:
                return ToString((float) value);
            case PrimitiveTypeCode.Double:
                return ToString((double) value);
            case PrimitiveTypeCode.DateTime:
                return ToString((DateTime) value);
            case PrimitiveTypeCode.Decimal:
                return ToString((decimal) value);
            case PrimitiveTypeCode.DBNull:
                return Null;
            case PrimitiveTypeCode.DateTimeOffset:
                return ToString((DateTimeOffset) value);
            case PrimitiveTypeCode.Guid:
                return ToString((Guid) value);
            case PrimitiveTypeCode.Uri:
                return ToString((Uri) value);
            case PrimitiveTypeCode.TimeSpan:
                return ToString((TimeSpan) value);
            case PrimitiveTypeCode.BigInteger:
                return ToStringInternal((BigInteger) value);
        }

        throw new ArgumentException($"Unsupported type: {value.GetType()}. Use the JsonSerializer class to get the object's JSON representation.");
    }

    #region Serialize

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value) =>
        SerializeObject(value, null, (JsonSerializerSettings?) null);

    /// <summary>
    /// Serializes the specified object to a JSON string using formatting.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, Formatting formatting) =>
        SerializeObject(value, formatting, (JsonSerializerSettings?) null);

    /// <summary>
    /// Serializes the specified object to a JSON string using a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, params JsonConverter[] converters)
    {
        JsonSerializerSettings? settings = null;
        if (converters is {Length: > 0})
        {
            settings = new() {Converters = converters.ToList()};
        }

        return SerializeObject(value, null, settings);
    }

    /// <summary>
    /// Serializes the specified object to a JSON string using formatting and a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, Formatting formatting, params JsonConverter[] converters)
    {
        JsonSerializerSettings? settings = null;
        if (converters is {Length: > 0})
        {
            settings = new() {Converters = converters.ToList()};
        }

        return SerializeObject(value, null, formatting, settings);
    }

    /// <summary>
    /// Serializes the specified object to a JSON string using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to serialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, JsonSerializerSettings? settings) =>
        SerializeObject(value, null, settings);

    /// <summary>
    /// Serializes the specified object to a JSON string using a type, formatting and <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to serialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    /// <param name="type">
    /// The type of the value being serialized.
    /// This parameter is used when <see cref="JsonSerializer.TypeNameHandling" /> is <see cref="TypeNameHandling.Auto" />
    /// to write out the type name if the type of the value does not match.
    /// Specifying the type is optional.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, Type? type, JsonSerializerSettings? settings)
    {
        var serializer = JsonSerializer.CreateDefault(settings);

        return SerializeObjectInternal(value, type, serializer);
    }

    /// <summary>
    /// Serializes the specified object to a JSON string using formatting and <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to serialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, Formatting formatting, JsonSerializerSettings? settings) =>
        SerializeObject(value, null, formatting, settings);

    /// <summary>
    /// Serializes the specified object to a JSON string using a type, formatting and <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to serialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    /// <param name="type">
    /// The type of the value being serialized.
    /// This parameter is used when <see cref="JsonSerializer.TypeNameHandling" /> is <see cref="TypeNameHandling.Auto" />
    /// to write out the type name if the type of the value does not match.
    /// Specifying the type is optional.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static string SerializeObject(object? value, Type? type, Formatting formatting, JsonSerializerSettings? settings)
    {
        var serializer = JsonSerializer.CreateDefault(settings);
        serializer.Formatting = formatting;

        return SerializeObjectInternal(value, type, serializer);
    }

    static string SerializeObjectInternal(object? value, Type? type, JsonSerializer serializer)
    {
        var builder = new StringBuilder(256);
        var stringWriter = new StringWriter(builder, InvariantCulture);
        using (var jsonWriter = new JsonTextWriter(stringWriter)
               {
                   Formatting = serializer.Formatting.GetValueOrDefault()
               })
        {
            serializer.Serialize(jsonWriter, value, type);
        }

        return stringWriter.ToString();
    }

    #endregion

    #region Deserialize

    /// <summary>
    /// Deserializes the JSON to a .NET object.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object DeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value) =>
        DeserializeObject(value, null, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to a .NET object.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? TryDeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value) =>
        TryDeserializeObject(value, null, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to a .NET object using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object DeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        JsonSerializerSettings settings) =>
        DeserializeObject(value, null, settings);

    /// <summary>
    /// Deserializes the JSON to a .NET object using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? TryDeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        JsonSerializerSettings settings) =>
        TryDeserializeObject(value, null, settings);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object DeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type type) =>
        DeserializeObject(value, type, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? TryDeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type type) =>
        TryDeserializeObject(value, type, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T DeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value) =>
        DeserializeObject<T>(value, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T? TryDeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value) =>
        TryDeserializeObject<T>(value, (JsonSerializerSettings?) null);

    /// <summary>
    /// Deserializes the JSON to the given anonymous type.
    /// </summary>
    /// <typeparam name="T">
    /// The anonymous type to deserialize to. This can't be specified
    /// traditionally and must be inferred from the anonymous type passed
    /// as a parameter.
    /// </typeparam>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T DeserializeAnonymousType<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        T anonymousTypeObject) =>
        DeserializeObject<T>(value);

    /// <summary>
    /// Deserializes the JSON to the given anonymous type.
    /// </summary>
    /// <typeparam name="T">
    /// The anonymous type to deserialize to. This can't be specified
    /// traditionally and must be inferred from the anonymous type passed
    /// as a parameter.
    /// </typeparam>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T? TryDeserializeAnonymousType<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        T anonymousTypeObject) =>
        TryDeserializeObject<T>(value);

    /// <summary>
    /// Deserializes the JSON to the given anonymous type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <typeparam name="T">
    /// The anonymous type to deserialize to. This can't be specified
    /// traditionally and must be inferred from the anonymous type passed
    /// as a parameter.
    /// </typeparam>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T DeserializeAnonymousType<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        T anonymousTypeObject,
        JsonSerializerSettings settings) =>
        DeserializeObject<T>(value, settings);

    /// <summary>
    /// Deserializes the JSON to the given anonymous type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <typeparam name="T">
    /// The anonymous type to deserialize to. This can't be specified
    /// traditionally and must be inferred from the anonymous type passed
    /// as a parameter.
    /// </typeparam>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T? TryDeserializeAnonymousType<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        T anonymousTypeObject,
        JsonSerializerSettings settings) =>
        TryDeserializeObject<T>(value, settings);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T DeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        params JsonConverter[] converters) =>
        (T) DeserializeObject(value, typeof(T), converters);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T? TryDeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        params JsonConverter[] converters) =>
        (T?) TryDeserializeObject(value, typeof(T), converters);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T DeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        JsonSerializerSettings? settings) =>
        (T) DeserializeObject(value, typeof(T), settings);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static T? TryDeserializeObject<T>(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        JsonSerializerSettings? settings) =>
        (T?) TryDeserializeObject(value, typeof(T), settings);

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object DeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type type,
        params JsonConverter[] converters)
    {
        var settings = GetSettingsForConverter(converters);

        return DeserializeObject(value, type, settings);
    }

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using a collection of <see cref="JsonConverter" />.
    /// </summary>
    [DebuggerStepThrough]
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? TryDeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type type,
        params JsonConverter[] converters)
    {
        var settings = GetSettingsForConverter(converters);

        return TryDeserializeObject(value, type, settings);
    }

    static JsonSerializerSettings? GetSettingsForConverter(JsonConverter[] converters)
    {
        if (converters is {Length: > 0})
        {
            return new() {Converters = converters.ToList()};
        }

        return null;
    }

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object DeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type? type,
        JsonSerializerSettings? settings)
    {
        var result = TryDeserializeObject(value, type, settings);
        if (result==null)
        {
            throw new($"The value resulted in null. Value: {value}");
        }

        return result;
    }

    /// <summary>
    /// Deserializes the JSON to the specified .NET type using <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="settings">
    /// The <see cref="JsonSerializerSettings" /> used to deserialize the object.
    /// If this is <c>null</c>, default serialization settings will be used.
    /// </param>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static object? TryDeserializeObject(
        [StringSyntax(StringSyntaxAttribute.Json)]
        string value,
        Type? type,
        JsonSerializerSettings? settings)
    {
        var serializer = JsonSerializer.CreateDefault(settings);

        // by default DeserializeObject should check for additional content
        if (!serializer.IsCheckAdditionalContentSet())
        {
            serializer.CheckAdditionalContent = true;
        }

        using var reader = new JsonTextReader(new StringReader(value));
        return serializer.TryDeserialize(reader, type);
    }

    #endregion
}