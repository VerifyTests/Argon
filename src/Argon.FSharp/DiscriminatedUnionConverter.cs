// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Converts a F# discriminated union.
/// </summary>
public class DiscriminatedUnionConverter :
    JsonConverter
{
    const string casePropertyName = "Case";
    const string fieldsPropertyName = "Fields";

    static readonly ThreadSafeStore<Type, Union> unionCache = new(CreateUnion);
    static readonly ThreadSafeStore<Type, Type> unionTypeLookupCache = new(CreateUnionTypeLookup);

    static Type CreateUnionTypeLookup(Type type)
    {
        // this lookup is because cases with fields are derived from union type
        // need to get declaring type to avoid duplicate Unions in cache

        var caseInfo = FSharpType.GetUnionCases(type, null)[0];
        return caseInfo.DeclaringType;
    }

    static Union CreateUnion(Type type)
    {
        var u = new Union(FSharpValue.PreComputeUnionTagReader(type, null), []);

        foreach (var unionCaseInfo in FSharpType.GetUnionCases(type, null))
        {
            var unionCase = new UnionCase(
                unionCaseInfo.Tag,
                unionCaseInfo.Name,
                unionCaseInfo.GetFields(),
                FSharpValue.PreComputeUnionReader(unionCaseInfo, null),
                FSharpValue.PreComputeUnionConstructor(unionCaseInfo, null));

            u.Cases.Add(unionCase);
        }

        return u;
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var resolver = serializer.ContractResolver as DefaultContractResolver;

        var unionType = unionTypeLookupCache.Get(value.GetType());
        var union = unionCache.Get(unionType);

        var tag = union.TagReader.Invoke(value);
        var caseInfo = union.Cases.Single(_ => _.Tag == tag);

        writer.WriteStartObject();
        writer.WritePropertyName(resolver?.GetResolvedPropertyName(casePropertyName) ?? casePropertyName);
        writer.WriteValue(caseInfo.Name);
        if (caseInfo.Fields is {Length: > 0})
        {
            var fields = caseInfo.FieldReader.Invoke(value);

            writer.WritePropertyName(resolver?.GetResolvedPropertyName(fieldsPropertyName) ?? fieldsPropertyName);
            writer.WriteStartArray();
            foreach (var field in fields)
            {
                serializer.Serialize(writer, field);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        UnionCase? caseInfo = null;
        string? caseName = null;
        JArray? fields = null;

        // start object
        reader.ReadAndAssert();

        while (reader.TokenType == JsonToken.PropertyName)
        {
            var propertyName = (string) reader.GetValue();
            if (string.Equals(propertyName, casePropertyName, StringComparison.OrdinalIgnoreCase))
            {
                reader.ReadAndAssert();

                var union = unionCache.Get(type);

                caseName = (string) reader.GetValue();

                caseInfo = union.Cases.SingleOrDefault(_ => _.Name == caseName);

                if (caseInfo == null)
                {
                    throw JsonSerializationException.Create(reader, $"No union type found with the name '{caseName}'.");
                }
            }
            else if (string.Equals(propertyName, fieldsPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                reader.ReadAndAssert();
                if (reader.TokenType != JsonToken.StartArray)
                {
                    throw JsonSerializationException.Create(reader, "Union fields must been an array.");
                }

                fields = (JArray)JToken.ReadFrom(reader);
            }
            else
            {
                throw JsonSerializationException.Create(reader, $"Unexpected property '{propertyName}' found when reading union.");
            }

            reader.ReadAndAssert();
        }

        if (caseInfo == null)
        {
            throw JsonSerializationException.Create(reader, $"No '{casePropertyName}' property with union name found.");
        }

        var typedFieldValues = new object[caseInfo.Fields.Length];

        if (caseInfo.Fields.Length > 0 && fields == null)
        {
            throw JsonSerializationException.Create(reader, $"No '{fieldsPropertyName}' property with union fields found.");
        }

        if (fields != null)
        {
            if (caseInfo.Fields.Length != fields.Count)
            {
                throw JsonSerializationException.Create(reader, $"The number of field values does not match the number of properties defined by union '{caseName}'.");
            }

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var fieldProperty = caseInfo.Fields[i];

                typedFieldValues[i] = field.ToObject(fieldProperty.PropertyType, serializer)!;
            }
        }

        return caseInfo.Constructor.Invoke(typedFieldValues);
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    // memoized: CanConvert runs for every value (de)serialized once this converter is
    // registered, and FSharpType.IsUnion does attribute reflection on each call
    static readonly ThreadSafeStore<Type, bool> isUnionCache = new(_ => FSharpType.IsUnion(_, null));

    public override bool CanConvert(Type type) =>
        isUnionCache.Get(type);
}