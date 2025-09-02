// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Converts an <see cref="Enum"/> to and from its name string value.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class StringEnumConverter :
    JsonConverter
{
    /// <summary>
    /// Gets or sets the naming strategy used to resolve how enum text is written.
    /// </summary>
    public NamingStrategy? NamingStrategy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether integer values are allowed when serializing and deserializing.
    /// The default value is <c>true</c>.
    /// </summary>
    public bool AllowIntegerValues { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringEnumConverter"/> class.
    /// </summary>
    public StringEnumConverter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringEnumConverter"/> class.
    /// </summary>
    public StringEnumConverter(NamingStrategy namingStrategy, bool allowIntegerValues = true)
    {
        NamingStrategy = namingStrategy;
        AllowIntegerValues = allowIntegerValues;
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (EnumUtils.TryToString(value.GetType(), value, NamingStrategy, out var enumName))
        {
            writer.WriteValue(enumName);
            return;
        }

        if (AllowIntegerValues)
        {
            writer.WriteValue(value);
            return;
        }

        var enumValue = (Enum)value;
        throw JsonSerializationException.Create(null, writer.ContainerPath, $"Integer value {enumValue.ToString("D")} is not allowed.", null);
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        var nullable = type.IsNullableType();
        if (reader.TokenType == JsonToken.Null)
        {
            if (nullable)
            {
                return null;
            }

            throw JsonSerializationException.Create(reader, $"Cannot convert null value to {type}.");
        }

        var t = nullable ? Nullable.GetUnderlyingType(type)! : type;

        try
        {
            if (reader.TokenType == JsonToken.String)
            {
                var enumText = reader.Value?.ToString();

                if (enumText.IsNullOrEmpty() && nullable)
                {
                    return null;
                }

                return EnumUtils.ParseEnum(t, NamingStrategy, enumText!, !AllowIntegerValues);
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                if (AllowIntegerValues)
                {
                    return ConvertUtils.ConvertOrCast(reader.Value, t);
                }

                throw JsonSerializationException.Create(reader, $"Integer value {reader.Value} is not allowed.");
            }
        }
        catch (Exception exception)
        {
            throw JsonSerializationException.Create(reader, $"Error converting value {MiscellaneousUtils.ToString(reader.Value)} to type '{type}'.", exception);
        }

        // we don't actually expect to get here.
        throw JsonSerializationException.Create(reader, $"Unexpected token {reader.TokenType} when parsing enum.");
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type type)
    {
        if (type.IsNullableType())
        {
            return Nullable.GetUnderlyingType(type)!.IsEnum;
        }

        return type.IsEnum;
    }
}