// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Converts a C# union to and from JSON.
/// </summary>
/// <remarks>
/// A union is written as its active case with no wrapper and no type discriminator, matching
/// System.Text.Json. Reading picks the case whose JSON shape matches the payload; when several
/// cases share the object shape they are told apart by comparing property names.
/// </remarks>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class UnionConverter :
    JsonConverter
{
    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    public override bool CanConvert(Type type) =>
        UnionInfo.IsUnion(type);

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var caseValue = UnionInfo.Get(value.GetType())
            .GetValue(value);

        if (caseValue == null)
        {
            writer.WriteNull();
            return;
        }

        serializer.Serialize(writer, caseValue);
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        var unionType = Nullable.GetUnderlyingType(type) ?? type;
        var info = UnionInfo.Get(unionType);

        if (reader.TokenType is JsonToken.Null or JsonToken.Undefined)
        {
            return ReadNull(type, unionType, info);
        }

        var token = JToken.ReadFrom(reader);
        var unionCase = info.ResolveCase(token, serializer);
        var caseValue = token.ToObject(unionCase.CaseType, serializer);

        return unionCase.Constructor(caseValue);
    }

    static object? ReadNull(Type type, Type unionType, UnionInfo info)
    {
        // a union declared as Nullable<T> reads JSON null as null rather than as a case holding null
        if (type != unionType)
        {
            return null;
        }

        var nullCase = info.NullCase;
        if (nullCase != null)
        {
            return nullCase.Constructor([null]);
        }

        // no case can hold null, so mirror the write side, which emits null for a union that holds
        // no value: a reference typed union reads back as null, a struct union as its zero
        // initialized default
        if (!unionType.IsValueType)
        {
            return null;
        }

        return Activator.CreateInstance(unionType);
    }
}
