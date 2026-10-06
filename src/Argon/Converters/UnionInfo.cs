// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

/// <summary>
/// Reflected metadata for a C# union type.
/// </summary>
/// <remarks>
/// The C# compiler applies <c>System.Runtime.CompilerServices.UnionAttribute</c> to a union
/// declaration, but the attribute definition comes from either the target framework or a polyfill
/// compiled into the declaring assembly. Every assembly can therefore hold its own distinct copy of
/// the attribute, so the marker is matched by full name rather than through a compile time type
/// reference. For the same reason the active case is read through the conventional public
/// <c>object Value</c> property instead of through <c>IUnion</c>, which the compiler documents as
/// optional.
/// </remarks>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
class UnionInfo
{
    const string unionAttributeName = "UnionAttribute";
    const string unionAttributeFullName = "System.Runtime.CompilerServices.UnionAttribute";
    const string valuePropertyName = "Value";

    static readonly ThreadSafeStore<Type, bool> isUnionCache = new(DetectUnion);
    static readonly ThreadSafeStore<Type, UnionInfo> infoCache = new(Create);

    /// <summary>
    /// The JSON shapes a union case can be read from, used to match an incoming payload to a case.
    /// </summary>
    [Flags]
    public enum Shape
    {
        Object = 1,
        Array = 2,
        String = 4,
        Number = 8,
        Boolean = 16,
        Any = Object | Array | String | Number | Boolean
    }

    public record Case(Type CaseType, bool AcceptsNull, ObjectConstructor Constructor);

    public static bool IsUnion(Type type) =>
        isUnionCache.Get(type);

    public static UnionInfo Get(Type type) =>
        infoCache.Get(type);

    static bool DetectUnion(Type type)
    {
        foreach (var data in type.GetCustomAttributesData())
        {
            var attributeType = data.AttributeType;
            // Name is compared before FullName because FullName allocates for constructed and nested types
            if (attributeType is {Name: unionAttributeName, FullName: unionAttributeFullName})
            {
                return true;
            }
        }

        return false;
    }

    static UnionInfo Create(Type unionType)
    {
        var cases = new List<Case>();
        var nullability = new NullabilityInfoContext();

        foreach (var constructor in unionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != 1)
            {
                continue;
            }

            var parameter = parameters[0];
            var caseType = parameter.ParameterType;
            if (caseType.IsByRef ||
                caseType.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
                cases.Any(_ => _.CaseType == caseType))
            {
                continue;
            }

            cases.Add(
                new(
                    caseType,
                    AcceptsNull(parameter, caseType, nullability),
                    DelegateFactory.CreateParameterizedConstructor(constructor)));
        }

        SortMostDerivedFirst(cases);

        return new(unionType, cases, FindValueAccessor(unionType));
    }

    static bool AcceptsNull(ParameterInfo parameter, Type caseType, NullabilityInfoContext nullability)
    {
        if (Nullable.GetUnderlyingType(caseType) != null)
        {
            return true;
        }

        if (caseType.IsValueType)
        {
            return false;
        }

        return nullability.Create(parameter).WriteState != NullabilityState.NotNull;
    }

    // a case whose type derives from another case's type has to be considered first, so that the
    // nearest declared case wins when a value satisfies more than one of them
    static void SortMostDerivedFirst(List<Case> cases)
    {
        for (var index = 0; index < cases.Count; index++)
        {
            for (var candidate = index + 1; candidate < cases.Count; candidate++)
            {
                var current = cases[index].CaseType;
                var other = cases[candidate].CaseType;
                if (current != other &&
                    current.IsAssignableFrom(other))
                {
                    (cases[index], cases[candidate]) = (cases[candidate], cases[index]);
                }
            }
        }
    }

    static Func<object, object?>? FindValueAccessor(Type unionType)
    {
        var property = unionType.GetProperty(valuePropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null ||
            property.PropertyType != typeof(object) ||
            property.GetMethod is not {IsPublic: true} ||
            property.GetIndexParameters().Length > 0)
        {
            return null;
        }

        return DelegateFactory.CreateGet<object>(property);
    }

    readonly Type unionType;
    readonly Func<object, object?>? valueAccessor;

    UnionInfo(Type unionType, IReadOnlyList<Case> cases, Func<object, object?>? valueAccessor)
    {
        this.unionType = unionType;
        this.valueAccessor = valueAccessor;
        Cases = cases;
        NullCase = cases.FirstOrDefault(_ => _.AcceptsNull);
    }

    public IReadOnlyList<Case> Cases { get; }

    /// <summary>
    /// The first case that can hold a null payload, or <c>null</c> when no case accepts one.
    /// </summary>
    public Case? NullCase { get; }

    public object? GetValue(object union)
    {
        if (valueAccessor == null)
        {
            throw new JsonSerializationException($"Union type '{unionType}' does not expose a public 'object Value' property so its value cannot be read.");
        }

        return valueAccessor(union);
    }

    /// <summary>
    /// The nearest case that can hold a value of the given runtime type.
    /// </summary>
    public Case? FindCase(Type valueType)
    {
        foreach (var unionCase in Cases)
        {
            var caseType = unionCase.CaseType;
            if (caseType.IsAssignableFrom(valueType) ||
                Nullable.GetUnderlyingType(caseType) == valueType)
            {
                return unionCase;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads a metadata property the same way the serializer would: anywhere in the object when
    /// reading ahead, otherwise only from the run of metadata properties at its start.
    /// </summary>
    public static JToken? GetMetadataProperty(JObject payload, string name, JsonSerializer serializer)
    {
        var handling = serializer.MetadataPropertyHandling;
        if (handling == MetadataPropertyHandling.Ignore)
        {
            return null;
        }

        if (handling == MetadataPropertyHandling.ReadAhead)
        {
            return payload[name];
        }

        foreach (var property in payload.Properties())
        {
            if (property.Name == name)
            {
                return property.Value;
            }

            if (!IsMetadataName(property.Name))
            {
                break;
            }
        }

        return null;
    }

    static bool IsMetadataName(string name) =>
        name is
            JsonTypeReflector.TypePropertyName or
            JsonTypeReflector.IdPropertyName or
            JsonTypeReflector.RefPropertyName or
            JsonTypeReflector.ArrayValuesPropertyName;

    public Case ResolveCase(JToken token, JsonSerializer serializer)
    {
        var shape = GetShape(token.Type);

        // an array written with reference or type metadata is wrapped in an object holding $values
        if (token is JObject wrapper &&
            GetMetadataProperty(wrapper, JsonTypeReflector.ArrayValuesPropertyName, serializer) is JArray)
        {
            shape = Shape.Array;
        }

        var isNonFinite = token is JValue {Type: JTokenType.String, Value: string text} &&
                          (text == JsonConvert.NaN ||
                           text == JsonConvert.PositiveInfinity ||
                           text == JsonConvert.NegativeInfinity);

        var candidates = new List<Case>();
        foreach (var unionCase in Cases)
        {
            if ((GetShape(unionCase.CaseType, serializer, isNonFinite) & shape) != 0)
            {
                candidates.Add(unionCase);
            }
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var shapeName = shape.ToString().ToLowerInvariant();

        if (candidates.Count == 0)
        {
            throw new JsonSerializationException($"No case of union type '{unionType}' is serialized as a JSON {shapeName}. Cases: {CaseNames(Cases)}.");
        }

        if (token is JObject payload &&
            shape == Shape.Object)
        {
            return ResolveStructurally(payload, candidates, serializer);
        }

        throw new JsonSerializationException($"Multiple cases of union type '{unionType}' are serialized as a JSON {shapeName} so the payload is ambiguous. Candidates: {CaseNames(candidates)}.");
    }

    // several cases share the object shape, so they are told apart by comparing the payload's
    // property names against each candidate's resolved properties
    Case ResolveStructurally(JObject payload, List<Case> candidates, JsonSerializer serializer)
    {
        Case? best = null;
        var bestMatched = -1;
        var tied = false;

        foreach (var unionCase in candidates)
        {
            // a case read by a converter has no property names to compare against
            if (serializer.ResolveContract(unionCase.CaseType) is not JsonObjectContract contract ||
                GetReadConverter(contract, serializer) != null)
            {
                continue;
            }

            var matched = 0;
            var unknown = false;
            var readMetadata = serializer.MetadataPropertyHandling != MetadataPropertyHandling.Ignore;
            foreach (var property in payload.Properties())
            {
                // $id, $type and the like describe the payload rather than belong to a case
                if (readMetadata &&
                    IsMetadataName(property.Name))
                {
                    continue;
                }

                if (contract.Properties.GetClosestMatchProperty(property.Name) == null)
                {
                    unknown = true;
                    break;
                }

                matched++;
            }

            if (unknown)
            {
                continue;
            }

            if (matched > bestMatched)
            {
                best = unionCase;
                bestMatched = matched;
                tied = false;
            }
            else if (matched == bestMatched)
            {
                tied = true;
            }
        }

        if (best == null)
        {
            throw new JsonSerializationException($"No case of union type '{unionType}' has properties matching the JSON object. Candidates: {CaseNames(candidates)}.");
        }

        if (tied)
        {
            throw new JsonSerializationException($"The JSON object matches multiple cases of union type '{unionType}' equally well. Candidates: {CaseNames(candidates)}.");
        }

        return best;
    }

    static string CaseNames(IReadOnlyList<Case> cases) =>
        string.Join(", ", cases.Select(_ => $"'{_.CaseType}'"));

    static Shape GetShape(JTokenType tokenType) =>
        tokenType switch
        {
            JTokenType.Array => Shape.Array,
            JTokenType.Integer or JTokenType.Float => Shape.Number,
            JTokenType.Boolean => Shape.Boolean,
            JTokenType.String or JTokenType.Date or JTokenType.Guid or JTokenType.Uri or JTokenType.TimeSpan or JTokenType.Bytes => Shape.String,
            _ => Shape.Object
        };

    static JsonConverter? GetReadConverter(JsonContract contract, JsonSerializer serializer)
    {
        var converter = contract.Converter ??
                        JsonSerializer.GetMatchingConverter(serializer.Converters, contract.UnderlyingType) ??
                        contract.InternalConverter;
        if (converter is {CanRead: true})
        {
            return converter;
        }

        return null;
    }

    static Shape GetShape(Type caseType, JsonSerializer serializer, bool isNonFinite)
    {
        var contract = serializer.ResolveContract(caseType);
        var type = contract.NonNullableUnderlyingType;

        var converter = GetReadConverter(contract, serializer);
        if (converter is StringEnumConverter &&
            type.IsEnum)
        {
            return Shape.String | Shape.Number;
        }

        // a converter decides its own JSON, so nothing can be assumed about the shape it reads
        if (converter != null ||
            type == typeof(object))
        {
            return Shape.Any;
        }

        switch (contract.ContractType)
        {
            case JsonContractType.Array:
                return Shape.Array;
            case JsonContractType.Object:
            case JsonContractType.Dictionary:
            case JsonContractType.Dynamic:
                return Shape.Object;
            case JsonContractType.String:
                return Shape.String;
            case JsonContractType.Linq:
                return GetLinqShape(type);
        }

        // NaN and the infinities are written as strings, so those strings select a floating
        // point case. no other string does
        if (isNonFinite &&
            (type == typeof(double) || type == typeof(float)))
        {
            return Shape.String;
        }

        return GetPrimitiveShape(type);
    }

    static Shape GetLinqShape(Type type)
    {
        if (type == typeof(JObject))
        {
            return Shape.Object;
        }

        if (type == typeof(JArray))
        {
            return Shape.Array;
        }

        return Shape.Any;
    }

    static Shape GetPrimitiveShape(Type type)
    {
        if (type.IsEnum)
        {
            return Shape.Number;
        }

        return ConvertUtils.GetTypeCode(type) switch
        {
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.BooleanNullable => Shape.Boolean,
            PrimitiveTypeCode.Char or PrimitiveTypeCode.CharNullable or
                PrimitiveTypeCode.DateTime or PrimitiveTypeCode.DateTimeNullable or
                PrimitiveTypeCode.DateTimeOffset or PrimitiveTypeCode.DateTimeOffsetNullable or
                PrimitiveTypeCode.Guid or PrimitiveTypeCode.GuidNullable or
                PrimitiveTypeCode.TimeSpan or PrimitiveTypeCode.TimeSpanNullable or
                PrimitiveTypeCode.Uri or PrimitiveTypeCode.String or
                PrimitiveTypeCode.Bytes => Shape.String,
            _ => Shape.Number
        };
    }
}
