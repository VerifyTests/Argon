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

        // the union is the declared type, so TypeNameHandling.Auto writes the type of the case
        serializer.Serialize(writer, caseValue, value.GetType());
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

        if (token is JObject payload)
        {
            if (TryReadTypedCase(payload, unionType, info, serializer, out var typed))
            {
                return typed;
            }

            if (UnionInfo.GetMetadataProperty(payload, JsonTypeReflector.RefPropertyName, serializer) is JValue {Type: JTokenType.String})
            {
                return ReadReference(payload, unionType, info, serializer);
            }
        }

        var unionCase = info.ResolveCase(token, serializer);
        return Construct(unionType, info, unionCase, token.ToObject(unionCase.CaseType, serializer));
    }

    // with TypeNameHandling enabled a $type picks the case, which is what tells apart cases that
    // share a JSON shape
    static bool TryReadTypedCase(JObject payload, Type unionType, UnionInfo info, JsonSerializer serializer, [NotNullWhen(true)] out object? result)
    {
        result = null;

        if (serializer.TypeNameHandling.GetValueOrDefault() == TypeNameHandling.None ||
            UnionInfo.GetMetadataProperty(payload, JsonTypeReflector.TypePropertyName, serializer) is not JValue {Type: JTokenType.String, Value: string typeName})
        {
            return false;
        }

        var key = ReflectionUtils.SplitFullyQualifiedTypeName(typeName);
        var binder = serializer.SerializationBinder ?? DefaultSerializationBinder.Instance;

        Type? type;
        try
        {
            type = binder.BindToType(key.Assembly, key.Type);
        }
        catch
        {
            // the name may be a closed type discriminator rather than a type name. reading the
            // selected case reports a name that can not be resolved at all
            return false;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (type == null ||
            info.FindCase(type) is not { } unionCase)
        {
            return false;
        }

        result = Construct(unionType, info, unionCase, payload.ToObject(type, serializer));
        return true;
    }

    static object ReadReference(JObject payload, Type unionType, UnionInfo info, JsonSerializer serializer)
    {
        var referenced = payload.ToObject(typeof(object), serializer);
        if (referenced == null ||
            info.FindCase(referenced.GetType()) is not { } unionCase)
        {
            throw new JsonSerializationException($"Referenced value does not match any case of union type '{unionType}'.");
        }

        return unionCase.Constructor(referenced);
    }

    static object Construct(Type unionType, UnionInfo info, UnionInfo.Case unionCase, object? value)
    {
        if (value != null)
        {
            return unionCase.Constructor(value);
        }

        if (info.NullCase == null)
        {
            throw new JsonSerializationException($"Union type '{unionType}' does not accept a null case value.");
        }

        return info.NullCase.Constructor([null]);
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
